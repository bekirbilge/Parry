using System;
using System.Collections.Generic;
using Game.Core;

internal static class LargeFighterTests
{
    private static readonly SimulationConfig Config = new SimulationConfig();
    private static readonly CombatTuning Tuning = new CombatTuning(
        new MovementTuning(useFuelBoost: true), new ProjectileTuning(),
        new DefenseTuning(), new DividerTuning(),
        new ArenaTuning(fighterRadiusUnits: 2.5, projectileRadiusUnits: 0.9),
        new MatchTuning(roundCountdownSeconds: 0, startImmediatelyForTesting: true));

    public static void Run()
    {
        SpawnAndFloorRespectFiveUnitDiameter();
        PhysicalHitAndParryUseEnlargedRadius();
        ParrySafetyExpiresAfterSeparation();
        ExpandedPhoneParryCoversNewReachAndStillExpires();
        MirroredBoostsLoadAndRemainConstrainedByRope();
        EnlargedBodyDashDamagesOncePerSequence();
    }

    private static void SpawnAndFloorRespectFiveUnitDiameter()
    {
        var sim = new MatchSimulation(Config, Tuning);
        SimulationState state = sim.CaptureSnapshot();
        Assert(state.Fighter(0).Position.Y == 2.5 && state.Fighter(1).Position.Y == 2.5,
            "Both enlarged fighters must spawn tangent to the floor.");
        foreach (FighterState fighter in state.Fighters)
        {
            fighter.Position = new Vec2(fighter.Position.X, -10);
            fighter.PreviousPosition = fighter.Position;
            DividerSystem.ConstrainFighter(fighter, state, Tuning);
            Assert(fighter.Position.Y == 2.5, "The floor must constrain the five-unit body.");
            fighter.Position = new Vec2(fighter.Position.X, 100);
            fighter.PreviousPosition = fighter.Position;
            DividerSystem.ConstrainFighter(fighter, state, Tuning);
            Assert(fighter.Position.Y == Tuning.Arena.HeightUnits - 2.5,
                "The ceiling must constrain the five-unit body.");
        }
    }

    private static void PhysicalHitAndParryUseEnlargedRadius()
    {
        SimulationState hit = Fresh();
        hit.Projectiles.Add(Shot(1, 3.3));
        List<SimulationEvent> hitEvents = Combat(hit);
        Assert(hit.Fighter(0).Eliminated && Has(hitEvents, SimulationEventType.FighterEliminated),
            "Physical contact at 3.3 units must hit a 2.5-radius fighter and 0.9-radius projectile.");

        SimulationState reach = Fresh();
        reach.Projectiles.Add(Shot(2, 4.5));
        List<SimulationEvent> parryEvents = Combat(reach,
            new FighterInput(Vec2.Zero, defensePressed: true));
        Assert(!reach.Fighter(0).Eliminated && Has(parryEvents, SimulationEventType.PerfectParry),
            "The 1.5-unit extra parry reach must extend the enlarged physical radius.");

        SimulationState outside = Fresh();
        outside.Projectiles.Add(Shot(3, 5.5));
        List<SimulationEvent> outsideEvents = Combat(outside,
            new FighterInput(Vec2.Zero, defensePressed: true));
        Assert(!Has(outsideEvents, SimulationEventType.PerfectParry) &&
            !outside.Fighter(0).Eliminated,
            "A shot outside enlarged reach must remain outside parry contact.");
    }

    private static void ParrySafetyExpiresAfterSeparation()
    {
        SimulationState state = Fresh();
        ProjectileState shot = Shot(4, 3.3);
        state.Projectiles.Add(shot);
        Combat(state, new FighterInput(Vec2.Zero, defensePressed: true));
        Assert(shot.ParryContactMask != 0 && shot.LastParrierId == 0,
            "A successful enlarged parry must grant contact safety.");
        shot.Position = new Vec2(40, 20);
        shot.PreviousPosition = shot.Position;
        Combat(state);
        Assert(shot.ParryContactMask == 0 && shot.LastParrierId == -1,
            "Safety must clear once the shot leaves enlarged parry reach.");
        state.Fighter(0).ParryTicksRemaining = 0;
        shot.Position = new Vec2(28.3, 20);
        shot.PreviousPosition = shot.Position;
        shot.Velocity = new Vec2(-24, 0);
        List<SimulationEvent> returned = Combat(state);
        Assert(state.Fighter(0).Eliminated && Has(returned, SimulationEventType.FighterEliminated),
            "The returned shot must be lethal after enlarged contact safety clears.");
    }

    private static void ExpandedPhoneParryCoversNewReachAndStillExpires()
    {
        var tuning = new CombatTuning(Tuning.Movement, Tuning.Projectile,
            new DefenseTuning(parryExtraReachUnits: 3.5), Tuning.Divider, Tuning.Arena, Tuning.Match);
        SimulationState state = Fresh();
        FighterState fighter = state.Fighter(0);
        fighter.Posture = 90;
        fighter.PostureRecoveryDelayTicksRemaining = 30;
        fighter.FuelSeconds = 0;
        ProjectileState shot = Shot(40, 5.5);
        state.Projectiles.Add(shot);
        var events = new List<SimulationEvent>();
        CombatSystem.Advance(state, new SimulationInput(0,
            new FighterInput(Vec2.Zero, defensePressed: true), default), tuning, Config, events);
        Assert(Has(events, SimulationEventType.PerfectParry) && !fighter.Eliminated,
            "The phone trial must parry within radius6 plus projectile0.9, beyond prior reach4.9.");
        Assert(fighter.Posture == 90 && Math.Abs(fighter.FuelSeconds - 0.3) < 1e-8,
            "Larger parry grants exactly one boost refund without posture damage or healing.");
        Assert(shot.PowerLevel == 1 && shot.LastParrierId == 0,
            "Expanded parry still grants normal power and immediate contact safety.");
        state.Tick++;
        shot.Position = new Vec2(25 + 8, 20);
        shot.Velocity = Vec2.Zero;
        events.Clear();
        CombatSystem.Advance(state, new SimulationInput(state.Tick, default, default), tuning, Config, events);
        Assert(shot.LastParrierId == -1 && shot.ParryContactMask == 0,
            "Expanded contact safety must still expire when the shot clears radius6.9.");
        fighter.ParryTicksRemaining = 0;
        shot.Position = new Vec2(28.3, 20);
        state.Tick++;
        events.Clear();
        CombatSystem.Advance(state, new SimulationInput(state.Tick, default, default), tuning, Config, events);
        Assert(fighter.Eliminated, "A fresh unguarded return remains lethal after expanded safety ends.");

        SimulationState outside = Fresh();
        outside.Projectiles.Add(Shot(41, 7.5));
        events.Clear();
        CombatSystem.Advance(outside, new SimulationInput(0,
            new FighterInput(Vec2.Zero, defensePressed: true), default), tuning, Config, events);
        Assert(!Has(events, SimulationEventType.PerfectParry),
            "The larger trial must not catch projectiles outside radius6.9.");
    }

    private static void MirroredBoostsLoadAndRemainConstrainedByRope()
    {
        foreach (int id in new[] { 0, 1 })
        {
            SimulationState state = Fresh();
            FighterState fighter = state.Fighter(id);
            fighter.Position = fighter.PreviousPosition = new Vec2(id == 0 ? 46.5 : 53.5, 10);
            fighter.Grounded = false;
            fighter.FuelSeconds = Tuning.Movement.FuelCapacitySeconds;
            bool loaded = false, recoiled = false;
            for (int tick = 0; tick < 45; tick++)
            {
                var events = new List<SimulationEvent>();
                var input = new FighterInput(new Vec2(id == 0 ? 1 : -1, 0),
                    dashPressed: tick == 0, dashHeld: true);
                var step = new SimulationInput(state.Tick, id == 0 ? input : default,
                    id == 1 ? input : default);
                MovementSystem.AdvanceKinematics(state, step, Tuning, Config, events);
                loaded |= fighter.DividerLoadTicksRemaining > 0;
                recoiled |= Has(events, SimulationEventType.DividerRebound);
                double limit = DividerSystem.FighterCenterLimitX(state, Tuning, id, fighter.Position.Y);
                Assert(id == 0 ? fighter.Position.X <= limit + 1e-6 : fighter.Position.X >= limit - 1e-6,
                    "An enlarged fighter must stay on its side of the deformed rope.");
                state.Tick++;
            }
            Assert(loaded && recoiled, "Both enlarged fighters must load and recoil from the rope: " +
                "id=" + id + " loaded=" + loaded + " recoiled=" + recoiled +
                " x=" + fighter.Position.X + " fuel=" + fighter.FuelSeconds);
        }
    }

    private static void EnlargedBodyDashDamagesOncePerSequence()
    {
        SimulationState state = Fresh();
        FighterState left = state.Fighter(0);
        FighterState right = state.Fighter(1);
        left.Grounded = right.Grounded = true;
        left.PreviousPosition = new Vec2(42, 20);
        left.Position = new Vec2(47, 20);
        right.PreviousPosition = right.Position = new Vec2(51, 20);
        left.DashSequence = 1;
        left.DashDirection = new Vec2(1, 0);
        left.DashTicksRemaining = 3;
        var events = new List<SimulationEvent>();
        Assert(BodySystem.TryFindNextContact(state, 0, Tuning, out BodyContact contact),
            "A five-unit body must detect swept dash contact.");
        BodySystem.ApplyContact(state, contact, Tuning, Config, events);
        Assert(right.Posture == Tuning.Defense.DashBodyPostureDamage &&
            left.LastDashBodyHitSequence == left.DashSequence,
            "The enlarged dash must apply one configured body hit.");
        state.Tick++;
        left.PreviousPosition = left.Position = new Vec2(46, 20);
        right.PreviousPosition = right.Position = new Vec2(51, 20);
        BodySystem.ApplyContact(state, new BodyContact(0, new Vec2(1, 0)), Tuning, Config, events);
        Assert(right.Posture == Tuning.Defense.DashBodyPostureDamage,
            "An ongoing enlarged dash must not repeat posture damage.");
    }

    private static SimulationState Fresh()
    {
        var state = new SimulationState { Phase = MatchPhase.Active };
        state.Fighter(0).Position = state.Fighter(0).PreviousPosition = new Vec2(25, 20);
        state.Fighter(1).Position = state.Fighter(1).PreviousPosition = new Vec2(75, 40);
        return state;
    }

    private static ProjectileState Shot(int id, double distance) => new ProjectileState
    {
        Id = id, Position = new Vec2(25 + distance, 20), Velocity = new Vec2(-24, 0)
    };

    private static List<SimulationEvent> Combat(SimulationState state, FighterInput left = default)
    {
        var events = new List<SimulationEvent>();
        CombatSystem.Advance(state, new SimulationInput(state.Tick, left, default), Tuning, Config, events);
        state.Tick++;
        return events;
    }

    private static bool Has(List<SimulationEvent> events, SimulationEventType type) =>
        events.Exists(e => e.Type == type);

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
