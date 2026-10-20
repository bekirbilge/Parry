using System;
using System.Collections.Generic;
using Game.Core;

internal static class ParryOwnershipTests
{
    public static void Run()
    {
        OwnershipTransfersOnlyOnParry();
        OwnedShotsPassThroughWithoutAnActiveParry();
        RedirectKeepsOwnershipSpeedAndNaturalDecay();
        ImmunitySurvivesWallsAndDecay();
        OwnershipSnapshotsAreIndependent();
        ChargedSpeedCoversScreenBeforeDecay();
    }

    private static SimulationState Fresh()
    {
        var state = new SimulationState { Phase = MatchPhase.Active };
        foreach (FighterState f in state.Fighters)
        {
            f.Position = f.PreviousPosition = new Vec2(f.Id == 0 ? 25 : 75, 10);
            f.DashCharges = 0;
            f.Posture = 99;
            f.PostureRecoveryDelayTicksRemaining = 1000;
        }
        state.Projectiles.Add(new ProjectileState
        {
            Id = 1, Position = new Vec2(27.3, 10), Velocity = new Vec2(-24, 0)
        });
        return state;
    }

    private static List<SimulationEvent> Step(SimulationState state,
        FighterInput left = default, FighterInput right = default)
    {
        var events = new List<SimulationEvent>();
        CombatSystem.Advance(state, new SimulationInput(state.Tick, left, right),
            CombatTuning.Default, new SimulationConfig(), events);
        state.Tick++;
        return events;
    }

    private static void OwnershipTransfersOnlyOnParry()
    {
        SimulationState state = Fresh();
        ProjectileState shot = state.Projectiles[0];
        Step(state, new FighterInput(Vec2.Zero, defensePressed: true));
        Assert(shot.LastParrierId == 0 && shot.PowerLevel == 1 && state.Fighter(0).DashCharges == 1,
            "A parry claims only that projectile and restores one charge.");

        // Returning an owned shot can be aimed without earning another parry reward.
        shot.Position = new Vec2(27.3, 10);
        shot.Velocity = new Vec2(-36, 0);
        shot.ReflectedContactMask = 0;
        List<SimulationEvent> ownEvents = Step(state, new FighterInput(Vec2.Zero, defensePressed: true));
        Assert(Count(ownEvents, SimulationEventType.OwnedProjectileRedirected) == 1 &&
            Count(ownEvents, SimulationEventType.PerfectParry) == 0 &&
            shot.LastParrierId == 0 && shot.PowerLevel == 1 && shot.Velocity.X > 0 &&
            shot.DecayTicksRemaining == 239 && state.Fighter(0).DashCharges == 1 &&
            !state.Fighter(0).Eliminated,
            "An intentional owned redirect must keep ownership and normal power decay without parry rewards.");

        shot.Position = new Vec2(72.7, 10);
        shot.Velocity = new Vec2(36, 0);
        Step(state, right: new FighterInput(Vec2.Zero, defensePressed: true));
        Assert(shot.LastParrierId == 1 && shot.PowerLevel == 2 && state.Fighter(1).DashCharges == 1,
            "The opponent's parry transfers immunity and increases power.");

        shot.Position = new Vec2(27.3, 10);
        shot.Velocity = new Vec2(-42, 0);
        state.Fighter(0).ParryTicksRemaining = 0;
        Step(state);
        Assert(state.Fighter(0).Eliminated && !state.Fighter(1).Eliminated,
            "The previous owner becomes vulnerable as soon as the opponent returns the shot.");

        SimulationState unclaimed = Fresh();
        Step(unclaimed);
        Assert(unclaimed.Fighter(0).Eliminated,
            "Unclaimed projectiles remain lethal to unguarded fighters.");
    }

    private static void OwnedShotsPassThroughWithoutAnActiveParry()
    {
        foreach (bool heldGuard in new[] { false, true })
        {
            SimulationState state = Fresh();
            FighterState owner = state.Fighter(0);
            ProjectileState shot = state.Projectiles[0];
            shot.LastParrierId = owner.Id;
            shot.PowerLevel = 2;
            shot.DecayTicksRemaining = 100;
            if (heldGuard)
            {
                owner.DefenseHeldPrevious = true;
                owner.DefenseHoldTicks = 30;
            }
            List<SimulationEvent> events = Step(state,
                heldGuard ? new FighterInput(Vec2.Zero, defenseHeld: true) : default);
            Assert(events.Count == 0 && shot.Velocity.X < 0 && shot.LastParrierId == owner.Id &&
                shot.PowerLevel == 2 && shot.DecayTicksRemaining == 99 &&
                owner.DashCharges == 0 && owner.Posture == 99 && !owner.Eliminated,
                "An idle or already held guard must let an owned shot pass without effects.");
        }
    }

    private static void RedirectKeepsOwnershipSpeedAndNaturalDecay()
    {
        SimulationState state = Fresh();
        FighterState owner = state.Fighter(0);
        ProjectileState shot = state.Projectiles[0];
        shot.LastParrierId = owner.Id;
        shot.PowerLevel = 2;
        shot.DecayTicksRemaining = 2;
        shot.Velocity = new Vec2(-84, 0);
        List<SimulationEvent> events = Step(state,
            new FighterInput(new Vec2(1, 1), defensePressed: true));
        Assert(Count(events, SimulationEventType.OwnedProjectileRedirected) == 1 &&
            Count(events, SimulationEventType.PerfectParry) == 0 &&
            shot.LastParrierId == owner.Id && shot.PowerLevel == 2 &&
            shot.DecayTicksRemaining == 1 && Math.Abs(shot.Velocity.Length - 84) < 1e-9 &&
            shot.Velocity.X > 0 && owner.DashCharges == 0 && owner.Posture == 99,
            "Steered owned redirect preserves speed, owner and remaining decay time.");
        Step(state);
        Assert(shot.PowerLevel == 1 && shot.LastParrierId == owner.Id,
            "Redirected power must decay on its original schedule.");
    }

    private static int Count(List<SimulationEvent> events, SimulationEventType type)
    {
        int count = 0;
        foreach (SimulationEvent item in events) if (item.Type == type) count++;
        return count;
    }

    private static void ImmunitySurvivesWallsAndDecay()
    {
        SimulationState state = Fresh();
        ProjectileState shot = state.Projectiles[0];
        shot.LastParrierId = 0;
        shot.PowerLevel = 1;
        shot.DecayTicksRemaining = 1;
        shot.Position = new Vec2(1, 10);
        shot.Velocity = new Vec2(-36, 0);
        for (int tick = 0; tick < 120; tick++) Step(state);
        Assert(shot.LastParrierId == 0 && shot.PowerLevel == 0 && shot.Position.X > 25 &&
            !state.Fighter(0).Eliminated && state.Fighter(0).Posture == 99,
            "Wall return and power decay must not revoke ownership immunity or damage posture.");

        // Guard reflection changes trajectory but does not claim ownership.
        shot.Position = new Vec2(72.7, 10);
        shot.Velocity = new Vec2(24, 0);
        state.Fighter(1).DefenseHeldPrevious = true;
        state.Fighter(1).DefenseHoldTicks = 30;
        state.Fighter(1).Posture = 0;
        Step(state, right: new FighterInput(Vec2.Zero, defenseHeld: true));
        Assert(shot.LastParrierId == 0 && shot.Velocity.X < 0,
            "An ordinary guard must not transfer projectile ownership.");
    }

    private static void OwnershipSnapshotsAreIndependent()
    {
        SimulationState state = Fresh();
        state.Projectiles[0].LastParrierId = 0;
        SimulationState copy = state.Clone();
        ulong checksum = state.ComputeChecksum();
        copy.Projectiles[0].LastParrierId = 1;
        Assert(state.Projectiles[0].LastParrierId == 0 && state.ComputeChecksum() == checksum &&
            copy.ComputeChecksum() != checksum, "Ownership must be cloned and included in replay checksums.");
        var resumed = new MatchSimulation();
        resumed.RestoreSnapshot(state);
        Assert(resumed.CaptureSnapshot().ComputeChecksum() == checksum,
            "Snapshot restoration must retain projectile ownership.");
    }

    private static void ChargedSpeedCoversScreenBeforeDecay()
    {
        CombatTuning tuning = CombatTuning.Default;
        double speed = tuning.Projectile.BaseSpeedUnitsPerSecond * tuning.Projectile.SpeedMultiplierAtLevel(1);
        double horizontalTravel = speed / Math.Sqrt(2) * tuning.Projectile.PowerDecaySecondsPerLevel;
        Assert(horizontalTravel >= tuning.Arena.WidthUnits * .75,
            "Even a diagonal first parry must retain charged speed long enough for 75% arena-width travel.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
