using System;
using System.Collections.Generic;
using Game.Core;

internal static class DividerBodyTests
{
    private static readonly CombatTuning Tuning = CombatTuning.Default;
    private static readonly SimulationConfig Config = new SimulationConfig();

    public static void Run()
    {
        MembraneBoundsAndReturns();
        EqualDashesRecoilSymmetrically();
        DiagonalReboundPreservesVerticalMomentumAndTravelsTunedDistance();
        BodyAttackDamagesOnceAndRewritesMotion();
        BodyIframeSuppressesPostureButNotImpulse();
        WalkingContactHasNoPostureDamage();
    }

    private static void MembraneBoundsAndReturns()
    {
        SimulationState state = FreshAtDivider();
        FighterState left = state.Fighter(0);
        FighterState right = state.Fighter(1);
        var events = new List<SimulationEvent>();
        for (int i = 0; i < 30; i++)
        {
            Step(state, new FighterInput(new Vec2(1, 0)), default, events);
            AssertBounds(state);
        }
        Assert(state.Divider.Deflection > 0 && state.Divider.Deflection <= Tuning.Divider.MaximumDeflectionUnits,
            "A single walking press must stretch the bounded spring.");
        double stretched = state.Divider.Deflection;
        for (int i = 0; i < 90; i++)
        {
            Step(state, default, default, events);
            AssertBounds(state);
        }
        Assert(Math.Abs(state.Divider.Deflection) < stretched,
            "The membrane must return when pressure ends.");
        Assert(left.Position.X + Tuning.Arena.FighterRadiusUnits <=
            right.Position.X - Tuning.Arena.FighterRadiusUnits + Tuning.Divider.MaximumDeflectionUnits * 2,
            "Players must stay ordered across the membrane.");
    }

    private static void EqualDashesRecoilSymmetrically()
    {
        SimulationState state = FreshAtDivider();
        state.Fighter(0).Position = new Vec2(47, 10);
        state.Fighter(1).Position = new Vec2(53, 10);
        var events = new List<SimulationEvent>();
        Step(state, new FighterInput(new Vec2(1, 0), dashPressed: true),
            new FighterInput(new Vec2(-1, 0), dashPressed: true), events);
        for (int i = 0; i < 4 && state.Fighter(0).DividerReboundDistanceRemaining == 0; i++)
            Step(state, new FighterInput(new Vec2(1, 0)),
                new FighterInput(new Vec2(-1, 0)), events);
        Assert(Math.Abs(state.Divider.Deflection) < 1e-9,
            "Equal opposite dash pressure must cancel membrane deflection.");
        Assert(state.Fighter(0).DividerReboundDistanceRemaining > 0 &&
            state.Fighter(1).DividerReboundDistanceRemaining > 0,
            "Equal dashes must both recoil.");
        Assert(Math.Abs(state.Fighter(0).Velocity.X + state.Fighter(1).Velocity.X) < 1e-9,
            "Equal dash recoil must be symmetric.");
        AssertBounds(state);
    }

    private static void DiagonalReboundPreservesVerticalMomentumAndTravelsTunedDistance()
    {
        SimulationState state = FreshAtDivider();
        FighterState left = state.Fighter(0);
        left.Position = new Vec2(47, 10);
        var events = new List<SimulationEvent>();
        Step(state, new FighterInput(new Vec2(1, 1), dashPressed: true), default, events);
        for (int i = 0; i < 8 && left.DividerReboundDistanceRemaining <= 0; i++)
            Step(state, new FighterInput(new Vec2(1, 1)), default, events);
        Assert(left.DividerReboundDistanceRemaining > 0, "Diagonal dash must rebound from the membrane.");
        Assert(Math.Abs(left.Velocity.Y - Tuning.Movement.DashSpeedUnitsPerSecond / Math.Sqrt(2)) < 1e-9,
            "Membrane recoil must preserve diagonal dash vertical momentum.");
        double impactX = left.Position.X;
        for (int i = 0; i < 90 && left.DividerReboundDistanceRemaining > 1e-9; i++)
        {
            Step(state, default, default, events);
            AssertBounds(state);
        }
        double traveled = impactX - left.Position.X;
        Assert(left.DividerReboundDistanceRemaining <= 1e-9 &&
            Math.Abs(traveled - Tuning.Divider.DashReboundDistanceUnits) < 1e-6 &&
            traveled >= 15 && traveled <= 20,
            "Neutral rebound must travel the tuned 18 units back into the fighter's half.");
    }

    private static void BodyAttackDamagesOnceAndRewritesMotion()
    {
        SimulationState state = FreshAtDivider();
        FighterState left = state.Fighter(0);
        FighterState right = state.Fighter(1);
        left.Position = new Vec2(47, 10);
        right.Position = new Vec2(51.5, 10);
        left.PreviousPosition = left.Position;
        right.PreviousPosition = right.Position;
        left.Position = new Vec2(49, 10);
        right.Position = new Vec2(51.5, 10);
        left.DashSequence = 1;
        left.DashDirection = new Vec2(1, 0);
        left.DashTicksRemaining = 3;
        var events = new List<SimulationEvent>();
        Assert(BodySystem.TryFindNextContact(state, 0, Tuning, out BodyContact contact),
            "Swept dash contact must be found before overlapping endpoints.");
        BodySystem.ApplyContact(state, contact, Tuning, Config, events);
        Assert(right.Posture == Tuning.Defense.DashBodyPostureDamage,
            "Dash body hit must apply the tuned posture once.");
        Assert(left.LastDashBodyHitSequence == left.DashSequence,
            "Dash hit sequence must be consumed.");
        Assert(left.MotionStartFraction == contact.Fraction &&
            left.PreviousPosition.X < left.Position.X + 1,
            "Body impact must replace the remaining motion segment.");
        Assert(!BodySystem.TryFindNextContact(state, contact.Fraction, Tuning, out _),
            "A resolved contact cannot immediately repeat within the same tick.");
        state.Tick++;
        left.PreviousPosition = left.Position = new Vec2(48.5, 10);
        right.PreviousPosition = right.Position = new Vec2(51.5, 10);
        BodySystem.ApplyContact(state, new BodyContact(0, new Vec2(1, 0)),
            Tuning, Config, events);
        Assert(right.Posture == Tuning.Defense.DashBodyPostureDamage,
            "The same dash sequence must not damage on a later tick.");
        AssertBounds(state);
    }

    private static void BodyIframeSuppressesPostureButNotImpulse()
    {
        SimulationState state = FreshAtDivider();
        FighterState left = state.Fighter(0);
        FighterState right = state.Fighter(1);
        left.Position = left.PreviousPosition = new Vec2(48.5, 10);
        right.Position = right.PreviousPosition = new Vec2(51.5, 10);
        left.DashSequence = 1;
        left.DashDirection = new Vec2(1, 0);
        left.DashTicksRemaining = 3;
        right.DashInvulnerabilityTicksRemaining = 2;
        Assert(BodySystem.TryFindNextContact(state, 0, Tuning, out BodyContact contact),
            "Active dash pressing a touching body must resolve contact.");
        BodySystem.ApplyContact(state, contact, Tuning, Config, new List<SimulationEvent>());
        Assert(right.Posture == 0 && right.Velocity.X > 0,
            "Body iframe must prevent posture but retain physical impulse.");
    }

    private static void WalkingContactHasNoPostureDamage()
    {
        SimulationState state = FreshAtDivider();
        FighterState left = state.Fighter(0);
        FighterState right = state.Fighter(1);
        left.Grounded = right.Grounded = true;
        left.Position = new Vec2(48.5, 10);
        left.PreviousPosition = new Vec2(48, 10);
        right.Position = right.PreviousPosition = new Vec2(51.5, 10);
        Assert(BodySystem.TryFindNextContact(state, 0, Tuning, out BodyContact contact),
            "Walking closure at the membrane must produce a contact.");
        BodySystem.ApplyContact(state, contact, Tuning, Config, new List<SimulationEvent>());
        Assert(left.Posture == 0 && right.Posture == 0,
            "Walking push must never deal posture damage.");
    }

    private static SimulationState FreshAtDivider()
    {
        var state = new SimulationState { Tick = 1, Phase = MatchPhase.Active };
        state.Fighter(0).Position = new Vec2(48.5, Tuning.Arena.FighterRadiusUnits);
        state.Fighter(1).Position = new Vec2(75, Tuning.Arena.FighterRadiusUnits);
        foreach (FighterState fighter in state.Fighters)
        {
            fighter.Grounded = true;
            fighter.DashCharges = 2;
        }
        return state;
    }

    private static void Step(SimulationState state, FighterInput left, FighterInput right,
        IList<SimulationEvent> events)
    {
        MovementSystem.AdvanceKinematics(state, new SimulationInput(state.Tick, left, right),
            Tuning, Config, events);
        state.Tick++;
    }

    private static void AssertBounds(SimulationState state)
    {
        double center = Tuning.Arena.WidthUnits / 2 + state.Divider.Deflection;
        double radius = Tuning.Arena.FighterRadiusUnits;
        Assert(state.Fighter(0).Position.X <= center - radius + 1e-9,
            "Left fighter must not cross the membrane.");
        Assert(state.Fighter(1).Position.X >= center + radius - 1e-9,
            "Right fighter must not cross the membrane.");
        Assert(Math.Abs(state.Divider.Deflection) <= Tuning.Divider.MaximumDeflectionUnits + 1e-9,
            "Membrane deflection must stay bounded.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
