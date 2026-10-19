using System;
using System.Collections.Generic;
using Game.Core;

internal static class DividerBodyTests
{
    private static readonly CombatTuning Tuning = CombatTuning.Default;
    private static readonly SimulationConfig Config = new SimulationConfig();

    public static void Run()
    {
        RopeAnchorsAndLocalPressureAtDifferentHeights();
        HeightSwitchDoesNotRelocateExistingBend();
        CircleClearanceMatchesVisiblePolyline();
        SweptVerticalDashCannotThreadLocalBend();
        MembraneBoundsAndReturns();
        EqualDashesRecoilSymmetrically();
        DiagonalReboundPreservesVerticalMomentumAndTravelsTunedDistance();
        BodyAttackDamagesOnceAndRewritesMotion();
        BodyIframeSuppressesPostureButNotImpulse();
        WalkingContactHasNoPostureDamage();
    }

    private static void SweptVerticalDashCannotThreadLocalBend()
    {
        foreach (int id in new[] { 0, 1 })
        {
            SimulationState state = FreshAtDivider();
            double peakY = DividerSystem.GetRopePoint(state, Tuning, 9).Y;
            for (int node = 1; node < DividerSystem.RopePointCount - 1; node++)
            {
                double y = DividerSystem.GetRopePoint(state, Tuning, node).Y;
                double weight = Math.Max(0, 1 - Math.Abs(y - peakY) / Tuning.Divider.DeformationReachUnits);
                state.Divider.NodeDeflections[node] = (id == 0 ? -8 : 8) * weight;
            }
            FighterState fighter = state.Fighter(id);
            double x = id == 0 ? 40.60 : 59.40;
            Vec2 start = new Vec2(x, peakY - 2.0 / 3.0);
            Vec2 end = new Vec2(x + (id == 0 ? 0.05 : -0.05), peakY + 2.0 / 3.0);
            Assert(id == 0 ? x <= DividerSystem.FighterCenterLimitX(state, Tuning, id, start.Y) &&
                end.X <= DividerSystem.FighterCenterLimitX(state, Tuning, id, end.Y) :
                x >= DividerSystem.FighterCenterLimitX(state, Tuning, id, start.Y) &&
                end.X >= DividerSystem.FighterCenterLimitX(state, Tuning, id, end.Y),
                "The regression must have legal endpoints on the fighter's side.");
            fighter.PreviousPosition = start;
            fighter.Position = end;
            fighter.Velocity = new Vec2(id == 0 ? 80 : -80, 80);
            fighter.DashDirection = new Vec2(id == 0 ? 1 : -1, 0);
            fighter.DashTicksRemaining = 1;
            DividerSystem.ConstrainFighter(fighter, state, Tuning);
            Assert(id == 0 ? fighter.Position.X < x - 0.1 : fighter.Position.X > x + 0.1,
                "A vertical dash through a local peak must receive a bounded outward correction.");
            Assert(fighter.Position.Y == end.Y && fighter.Velocity.Y == 80,
                "Swept rope contact must preserve diagonal or vertical momentum.");
            Assert(fighter.DividerReboundDistanceRemaining == Tuning.Divider.DashReboundDistanceUnits &&
                (id == 0 ? fighter.Velocity.X < 0 : fighter.Velocity.X > 0),
                "A swept-only inward dash contact must trigger outward rebound.");
            for (int step = 0; step <= 100; step++)
            {
                double t = step / 100.0;
                Vec2 center = fighter.PreviousPosition + (fighter.Position - fighter.PreviousPosition) * t;
                double radius = Tuning.Arena.FighterRadiusUnits;
                for (int sample = 0; sample <= 40; sample++)
                {
                    double y = center.Y - radius + sample * radius / 20.0;
                    double extent = Math.Sqrt(Math.Max(0, radius * radius - (y - center.Y) * (y - center.Y)));
                    double rope = DividerSystem.CenterXAtHeight(state, Tuning, y);
                    Assert(id == 0 ? center.X + extent <= rope + 1e-7 :
                        center.X - extent >= rope - 1e-7,
                        "Every sampled fighter circle along the tick path must clear the visible rope.");
                }
            }
        }
    }

    private static void RopeAnchorsAndLocalPressureAtDifferentHeights()
    {
        SimulationState state = FreshAtDivider();
        FighterState left = state.Fighter(0);
        FighterState right = state.Fighter(1);
        right.Position = new Vec2(51.5, 25);
        right.Grounded = false;
        var events = new List<SimulationEvent>();
        for (int tick = 0; tick < 20; tick++)
        {
            left.Position = new Vec2(DividerSystem.FighterCenterLimitX(state, Tuning, 0, left.Position.Y), left.Position.Y);
            right.Position = new Vec2(DividerSystem.FighterCenterLimitX(state, Tuning, 1, right.Position.Y), right.Position.Y);
            left.Velocity = new Vec2(4, 0);
            right.Velocity = new Vec2(-4, 0);
            DividerSystem.AdvanceAndConstrain(state, Tuning, Config, events);
        }
        Vec2 bottom = DividerSystem.GetRopePoint(state, Tuning, 0);
        Vec2 top = DividerSystem.GetRopePoint(state, Tuning, DividerSystem.RopePointCount - 1);
        Assert(bottom.X == 50 && bottom.Y == 0 && top.X == 50 && top.Y == Tuning.Arena.HeightUnits,
            "The rope must remain pinned at both arena boundaries.");
        Assert(state.Divider.NodeDeflections[1] > 0 && state.Divider.NodeDeflections[8] < 0,
            "Opposing pressure at separate heights must produce local bends in both directions.");
        Assert(Math.Abs(state.Divider.NodeDeflections[1]) > Math.Abs(state.Divider.NodeDeflections[4]) &&
            Math.Abs(state.Divider.NodeDeflections[8]) > Math.Abs(state.Divider.NodeDeflections[13]),
            "Pressure must decay with vertical distance from each contact.");
        for (int i = 0; i < DividerSystem.RopePointCount; i++)
        {
            Vec2 point = DividerSystem.GetRopePoint(state, Tuning, i);
            Assert(Math.Abs(DividerSystem.CenterXAtHeight(state, Tuning, point.Y) - point.X) < 1e-9,
                "Render samples and collision interpolation must share the same nodes.");
        }
        AssertBounds(state);
    }

    private static void HeightSwitchDoesNotRelocateExistingBend()
    {
        SimulationState state = FreshAtDivider();
        FighterState left = state.Fighter(0);
        var events = new List<SimulationEvent>();
        for (int tick = 0; tick < 25; tick++)
        {
            left.Position = new Vec2(DividerSystem.FighterCenterLimitX(state, Tuning, 0, left.Position.Y), left.Position.Y);
            left.Velocity = new Vec2(4, 0);
            DividerSystem.AdvanceAndConstrain(state, Tuning, Config, events);
        }
        double oldLow = state.Divider.NodeDeflections[1];
        double oldHigh = state.Divider.NodeDeflections[9];
        left.Position = new Vec2(DividerSystem.FighterCenterLimitX(state, Tuning, 0, 28), 28);
        left.Grounded = false;
        left.Velocity = new Vec2(4, 0);
        DividerSystem.AdvanceAndConstrain(state, Tuning, Config, events);
        Assert(Math.Abs(state.Divider.NodeDeflections[1] - oldLow) < 0.1 &&
            Math.Abs(state.Divider.NodeDeflections[9] - oldHigh) < 0.1,
            "A contact-height switch must evolve fixed nodes without relocating a bend instantly.");
    }

    private static void CircleClearanceMatchesVisiblePolyline()
    {
        SimulationState state = FreshAtDivider();
        state.Divider.NodeDeflections[1] = 3.5;
        state.Divider.NodeDeflections[2] = 2.5;
        state.Divider.NodeDeflections[8] = -4;
        foreach (FighterState fighter in state.Fighters)
        {
            fighter.Position = new Vec2(fighter.Id == 0 ? 60 : 40, fighter.Id == 0 ? 1.5 : 25);
            DividerSystem.ConstrainFighter(fighter, state, Tuning);
            double radius = Tuning.Arena.FighterRadiusUnits;
            for (int sample = 0; sample <= 200; sample++)
            {
                double y = fighter.Position.Y - radius + sample * radius / 100;
                if (y < 0 || y > Tuning.Arena.HeightUnits) continue;
                double extent = Math.Sqrt(Math.Max(0, radius * radius -
                    (y - fighter.Position.Y) * (y - fighter.Position.Y)));
                double rope = DividerSystem.CenterXAtHeight(state, Tuning, y);
                Assert(fighter.Id == 0 ? fighter.Position.X + extent <= rope + 1e-8 :
                    fighter.Position.X - extent >= rope - 1e-8,
                    "No part of a fighter circle may cross the rendered rope at any sampled height.");
            }
        }
        Assert(state.Fighter(0).Position.X < DividerSystem.CenterXAtHeight(state, Tuning, 1.5) - 1.5,
            "The bottom anchor slope must limit ground-level movement conservatively.");
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
        Assert(state.Divider.NodeDeflections[1] > 0 && state.Divider.NodeDeflections[1] <= Tuning.Divider.MaximumDeflectionUnits,
            "A single walking press must stretch the bounded spring.");
        double stretched = state.Divider.NodeDeflections[1];
        for (int i = 0; i < 90; i++)
        {
            Step(state, default, default, events);
            AssertBounds(state);
        }
        Assert(Math.Abs(state.Divider.NodeDeflections[1]) < stretched,
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
        Assert(Math.Abs(state.Divider.NodeDeflections[1]) < 1e-9,
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
            fighter.DashCharges = MovementTuning.MaximumDashCharges;
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
        double radius = Tuning.Arena.FighterRadiusUnits;
        Assert(state.Fighter(0).Position.X <= DividerSystem.FighterCenterLimitX(state, Tuning, 0,
            state.Fighter(0).Position.Y) + 1e-9,
            "Left fighter must not cross the membrane.");
        Assert(state.Fighter(1).Position.X >= DividerSystem.FighterCenterLimitX(state, Tuning, 1,
            state.Fighter(1).Position.Y) - 1e-9,
            "Right fighter must not cross the membrane.");
        for (int i = 0; i < DividerSystem.RopePointCount; i++)
            Assert(Math.Abs(state.Divider.NodeDeflections[i]) <= Tuning.Divider.MaximumDeflectionUnits + 1e-9,
                "Membrane deflection must stay bounded.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
