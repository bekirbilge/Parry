using System;
using System.Collections.Generic;
using Game.Core;

internal static class ParryOwnershipTests
{
    public static void Run()
    {
        ParryProtectsOnlyItsImmediateOverlap();
        AnotherProjectileRemainsDangerousDuringSafety();
        MovingBoostPassesThroughProtectedShot();
        ReturningShotKillsAfterSeparation();
        ReturningShotCanBeParriedForNormalReward();
        CloseOpponentParryReplacesSafety();
        OverlappingParryHalosDoNotRepeatInOneTick();
        SameTickWallReturnLosesSafety();
        GuardAndDecayDoNotCreateContactSafety();
        DecayRampSurvivesGuardAndWall();
        EnemyParryResetsDecayRamp();
        SnapshotDistinguishesActiveAndEndedSafety();
        ChargedSpeedCoversScreenBeforeDecay();
    }

    private static SimulationState Fresh()
    {
        var state = new SimulationState { Phase = MatchPhase.Active };
        state.Fighter(0).Position = state.Fighter(0).PreviousPosition = new Vec2(25, 10);
        state.Fighter(1).Position = state.Fighter(1).PreviousPosition = new Vec2(75, 20);
        state.Fighter(0).DashCharges = 0;
        state.Fighter(0).Posture = 99;
        state.Fighter(0).PostureRecoveryDelayTicksRemaining = 1000;
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

    private static void ParryProtectsOnlyItsImmediateOverlap()
    {
        SimulationState state = Fresh();
        ProjectileState shot = state.Projectiles[0];
        List<SimulationEvent> parry = Step(state, new FighterInput(Vec2.Zero, defensePressed: true));
        Assert(Count(parry, SimulationEventType.PerfectParry) == 1 &&
            shot.LastParrierId == 0 && shot.ParryContactMask == 1 &&
            shot.PowerLevel == 1 && state.Fighter(0).DashCharges == 1,
            "The successful parry must protect only its immediate projectile overlap and award one charge.");

        // Pressing again during the same overlap must neither charge nor redirect repeatedly.
        List<SimulationEvent> overlap = Step(state, new FighterInput(Vec2.Zero, defensePressed: true));
        Assert(Count(overlap, SimulationEventType.PerfectParry) == 0 &&
            Count(overlap, SimulationEventType.OwnedProjectileRedirected) == 0 &&
            state.Fighter(0).DashCharges == 1 && shot.PowerLevel == 1,
            "A continuous parry overlap must not grant a second reward.");
        Assert(shot.LastParrierId == -1 && shot.ParryContactMask == 0,
            "Contact safety must end as soon as the shot clears the 3.9-unit expanded reach.");
    }

    private static void ReturningShotKillsAfterSeparation()
    {
        SimulationState state = Fresh();
        ProjectileState shot = state.Projectiles[0];
        Step(state, new FighterInput(Vec2.Zero, defensePressed: true));
        Step(state);
        Assert(shot.LastParrierId == -1, "A departing parry must release safety before the return.");
        bool bounced = false;
        bool killed = false;
        for (int i = 0; i < 180 && !killed; i++)
        {
            List<SimulationEvent> events = Step(state);
            bounced |= Count(events, SimulationEventType.ProjectileBounced) > 0;
            killed |= Count(events, SimulationEventType.FighterEliminated) > 0;
        }
        Assert(bounced && killed && state.Fighter(0).Eliminated &&
            !state.Fighter(1).Eliminated && shot.LastParrierId == -1,
            "A naturally wall-returned shot must kill its former parrier after clearance.");
    }

    private static void AnotherProjectileRemainsDangerousDuringSafety()
    {
        SimulationState state = Fresh();
        Step(state, new FighterInput(Vec2.Zero, defensePressed: true));
        Assert(state.Projectiles[0].LastParrierId == 0,
            "The first shot must still be within active parry contact.");
        state.Fighter(0).ParryTicksRemaining = 0;
        state.Projectiles.Add(new ProjectileState
        {
            Id = 2, Position = new Vec2(22.7, 10), Velocity = new Vec2(24, 0)
        });
        List<SimulationEvent> events = Step(state);
        Assert(Count(events, SimulationEventType.FighterEliminated) == 1 &&
            state.Fighter(0).Eliminated && state.Projectiles[1].LastParrierId == -1,
            "Safety from one parry must not protect against an unrelated projectile.");
    }

    private static void MovingBoostPassesThroughProtectedShot()
    {
        SimulationState state = Fresh();
        ProjectileState shot = state.Projectiles[0];
        Step(state, new FighterInput(Vec2.Zero, defensePressed: true));
        FighterState fighter = state.Fighter(0);
        fighter.ParryTicksRemaining = 0;
        fighter.PreviousPosition = new Vec2(25, 10);
        fighter.Position = new Vec2(27.3, 10);
        fighter.DashTicksRemaining = 2;
        fighter.DashInvulnerabilityTicksRemaining = 0;
        List<SimulationEvent> events = Step(state);
        Assert(Count(events, SimulationEventType.FighterEliminated) == 0 &&
            Count(events, SimulationEventType.PerfectParry) == 0 &&
            !fighter.Eliminated && shot.LastParrierId == 0,
            "A moving boost can cross its own protected shot only while contact safety remains active.");
    }

    private static void ReturningShotCanBeParriedForNormalReward()
    {
        SimulationState state = Fresh();
        ProjectileState shot = state.Projectiles[0];
        Step(state, new FighterInput(Vec2.Zero, defensePressed: true));
        Step(state);
        bool bounced = false;
        for (int i = 0; i < 180 && !(bounced && shot.Position.X < 31); i++)
        {
            List<SimulationEvent> events = Step(state);
            bounced |= Count(events, SimulationEventType.ProjectileBounced) > 0;
        }
        Assert(bounced && shot.Position.X < 31 && !state.Fighter(0).Eliminated,
            "The wall return must approach the former parrier while still alive.");
        int charges = state.Fighter(0).DashCharges;
        int parries = 0;
        int redirects = 0;
        for (int i = 0; i < 8 && parries == 0; i++)
        {
            List<SimulationEvent> returned = Step(state, i == 0
                ? new FighterInput(Vec2.Zero, defensePressed: true) : default);
            parries += Count(returned, SimulationEventType.PerfectParry);
            redirects += Count(returned, SimulationEventType.OwnedProjectileRedirected);
        }
        Assert(parries == 1 && redirects == 0 &&
            shot.PowerLevel == 2 && state.Fighter(0).DashCharges == charges + 1 &&
            !state.Fighter(0).Eliminated,
            "A cleared return must be a fresh parry with ordinary power and charge rewards.");
    }

    private static void GuardAndDecayDoNotCreateContactSafety()
    {
        SimulationState guard = Fresh();
        guard.Fighter(0).DefenseHeldPrevious = true;
        guard.Fighter(0).DefenseHoldTicks = 30;
        guard.Fighter(0).Posture = 0;
        List<SimulationEvent> blocked = Step(guard,
            new FighterInput(Vec2.Zero, defenseHeld: true));
        Assert(Count(blocked, SimulationEventType.GuardBlocked) == 1 &&
            guard.Projectiles[0].LastParrierId == -1,
            "Ordinary guard must not grant projectile contact safety.");

        SimulationState state = Fresh();
        ProjectileState shot = state.Projectiles[0];
        Step(state, new FighterInput(Vec2.Zero, defensePressed: true));
        shot.DecayTicksRemaining = 1;
        Step(state);
        Assert(shot.PowerLevel == 0 && shot.SpeedDecaying && shot.LastParrierId == -1,
            "Power decay and speed easing must not preserve safety after separation.");
    }

    private static void CloseOpponentParryReplacesSafety()
    {
        SimulationState state = Fresh();
        FighterState right = state.Fighter(1);
        state.Fighter(0).Position = state.Fighter(0).PreviousPosition = new Vec2(48.5, 10);
        right.Position = right.PreviousPosition = new Vec2(54, 10);
        state.Projectiles[0].Position = new Vec2(50.8, 10);
        right.DashInvulnerabilityTicksRemaining = 2;
        Step(state, new FighterInput(Vec2.Zero, defensePressed: true));
        Assert(state.Projectiles[0].LastParrierId == 0 && !right.Eliminated,
            "The first close parry must leave the outgoing shot safe for the left fighter.");
        state.Fighter(0).ParryTicksRemaining = 0;
        state.Fighter(0).DashInvulnerabilityTicksRemaining = 2;
        List<SimulationEvent> returned = Step(state, right:
            new FighterInput(Vec2.Zero, defensePressed: true));
        Assert(Count(returned, SimulationEventType.PerfectParry) == 1 &&
            state.Projectiles[0].LastParrierId == 1 &&
            (state.Projectiles[0].ParryContactMask & 1) == 0,
            "The opponent's close parry must replace contact safety and clear the old overlap.");
    }

    private static void OverlappingParryHalosDoNotRepeatInOneTick()
    {
        SimulationState state = Fresh();
        state.Fighter(0).Position = state.Fighter(0).PreviousPosition = new Vec2(48.5, 10);
        state.Fighter(1).Position = state.Fighter(1).PreviousPosition = new Vec2(51.5, 10);
        state.Projectiles[0].Position = new Vec2(50.7, 10);
        List<SimulationEvent> events = Step(state,
            new FighterInput(Vec2.Zero, defensePressed: true),
            new FighterInput(Vec2.Zero, defensePressed: true));
        Assert(Count(events, SimulationEventType.PerfectParry) == 2 &&
            state.Projectiles[0].PowerLevel == 2,
            "Overlapping active parry halos must resolve within one tick without repeat rewards.");
    }

    private static void SameTickWallReturnLosesSafety()
    {
        SimulationState state = Fresh();
        FighterState left = state.Fighter(0);
        left.Position = left.PreviousPosition = new Vec2(5, 10);
        state.Projectiles[0].Position = new Vec2(2, 10);
        state.Projectiles[0].Velocity = new Vec2(-72, 0);
        state.Projectiles[0].LastParrierId = 0;
        state.Projectiles[0].ParryContactMask = 1;
        // A legal active-contact snapshot: the shot is three units from its parrier.
        // At 10 Hz it exits 3.9 reach, bounces at the left wall, then reaches the body.
        var events = new List<SimulationEvent>();
        CombatSystem.Advance(state, new SimulationInput(state.Tick, default, default),
            CombatTuning.Default, new SimulationConfig(tickRateHz: 10), events);
        Assert(Count(events, SimulationEventType.ProjectileBounced) == 1 &&
            Count(events, SimulationEventType.FighterEliminated) == 1 &&
            left.Eliminated && state.Projectiles[0].LastParrierId == -1,
            "A wall return in the clearance tick must be dangerous after leaving expanded reach.");
    }

    private static void DecayRampSurvivesGuardAndWall()
    {
        SimulationState guard = Fresh();
        ProjectileState shot = guard.Projectiles[0];
        shot.PowerLevel = 1;
        shot.DecayTicksRemaining = 1;
        shot.Velocity = new Vec2(-72, 0);
        guard.Fighter(0).DefenseHeldPrevious = true;
        guard.Fighter(0).DefenseHoldTicks = 30;
        guard.Fighter(0).Posture = 0;
        List<SimulationEvent> events = Step(guard,
            new FighterInput(Vec2.Zero, defenseHeld: true));
        Assert(Count(events, SimulationEventType.GuardBlocked) == 1 &&
            shot.PowerLevel == 0 && shot.SpeedDecaying &&
            Math.Abs(shot.Velocity.Length - 71.8) < 1e-9 && shot.LastParrierId == -1,
            "Guard must preserve the speed decay ramp without granting safety.");

        SimulationState wall = Fresh();
        shot = wall.Projectiles[0];
        shot.Position = new Vec2(0.9, 30);
        shot.Velocity = new Vec2(-72, 0);
        shot.PowerLevel = 1;
        shot.DecayTicksRemaining = 1;
        events = Step(wall);
        Assert(Count(events, SimulationEventType.ProjectileBounced) == 1 &&
            shot.PowerLevel == 0 && shot.SpeedDecaying &&
            Math.Abs(shot.Velocity.Length - 71.8) < 1e-9 && shot.LastParrierId == -1,
            "Wall bounce must preserve the speed decay ramp without granting safety.");
    }

    private static void EnemyParryResetsDecayRamp()
    {
        SimulationState state = Fresh();
        ProjectileState shot = state.Projectiles[0];
        shot.PowerLevel = 2;
        shot.DecayTicksRemaining = 100;
        shot.SpeedDecaying = true;
        shot.Velocity = new Vec2(-95, 0);
        List<SimulationEvent> events = Step(state,
            new FighterInput(Vec2.Zero, defensePressed: true));
        Assert(Count(events, SimulationEventType.PerfectParry) == 1 &&
            shot.PowerLevel == 3 && shot.DecayTicksRemaining == 240 &&
            !shot.SpeedDecaying && Math.Abs(shot.Velocity.Length - 96) < 1e-9,
            "A fresh parry must reset the speed ramp and decay timer at its new power level.");
    }

    private static void SnapshotDistinguishesActiveAndEndedSafety()
    {
        SimulationState state = Fresh();
        Step(state, new FighterInput(Vec2.Zero, defensePressed: true));
        Assert(SimulationState.SnapshotVersion == 7 && state.Projectiles[0].LastParrierId == 0,
            "Snapshot version 7 must capture active contact safety.");
        SimulationState active = state.Clone();
        ulong activeChecksum = active.ComputeChecksum();
        var resumed = new MatchSimulation();
        resumed.RestoreSnapshot(active);
        Assert(resumed.CaptureSnapshot().ComputeChecksum() == activeChecksum,
            "Snapshot restoration must preserve active contact safety exactly.");
        SimulationState endedFieldOnly = active.Clone();
        endedFieldOnly.Projectiles[0].LastParrierId = -1;
        Assert(endedFieldOnly.ComputeChecksum() != activeChecksum &&
            active.Projectiles[0].LastParrierId == 0,
            "The replay hash must distinguish active and ended safety with all other fields equal.");
        Step(state);
        Assert(state.Projectiles[0].LastParrierId == -1 && active.Projectiles[0].LastParrierId == 0,
            "Natural clearance must not mutate the saved active snapshot.");
    }

    private static void ChargedSpeedCoversScreenBeforeDecay()
    {
        CombatTuning tuning = CombatTuning.Default;
        double speed = tuning.Projectile.BaseSpeedUnitsPerSecond * tuning.Projectile.SpeedMultiplierAtLevel(1);
        double horizontalTravel = speed / Math.Sqrt(2) * tuning.Projectile.PowerDecaySecondsPerLevel;
        Assert(horizontalTravel >= tuning.Arena.WidthUnits * .75,
            "Even a diagonal first parry must retain charged speed long enough for 75% arena-width travel.");
    }

    private static int Count(List<SimulationEvent> events, SimulationEventType type)
    {
        int count = 0;
        foreach (SimulationEvent item in events) if (item.Type == type) count++;
        return count;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
