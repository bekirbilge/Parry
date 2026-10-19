using System;
using System.Collections.Generic;
using Game.Core;

internal static class MatchTests
{
    public static void Run()
    {
        ValidatesAndClampsInputs();
        ImmediateStartAllowsMovementBeforeFirstWarnedSpawn();
        CountdownScheduleCapAndPermanence();
        DrawIsIndependentOfProjectileEnumeration();
        RoundResetAndFirstToTwo();
    }

    private static void ImmediateStartAllowsMovementBeforeFirstWarnedSpawn()
    {
        var tuning = new CombatTuning(new MovementTuning(), new ProjectileTuning(),
            new DefenseTuning(), new DividerTuning(), new ArenaTuning(),
            new MatchTuning(roundCountdownSeconds: 0, startImmediatelyForTesting: true));
        var simulation = new MatchSimulation(tuning: tuning);
        Assert(simulation.Phase == MatchPhase.Active && simulation.CaptureSnapshot().Projectiles.Count == 0,
            "Testing mode must be active before the first input without a projectile.");
        IReadOnlyList<SimulationEvent> events = simulation.Step(new SimulationInput(simulation.Tick,
            new FighterInput(new Vec2(1, 0), jumpPressed: true, jumpHeld: true), default));
        SimulationState state = simulation.CaptureSnapshot();
        Assert(Count(events, SimulationEventType.RoundStarted) == 1 &&
            Count(events, SimulationEventType.SpawnWarning) == 1 &&
            Count(events, SimulationEventType.ProjectileSpawned) == 0,
            "First playable tick must start the round and warn once without spawning.");
        Assert(state.Fighter(0).Position.X > 25 && state.Fighter(0).Position.Y > 1.5 &&
            state.Projectiles.Count == 0, "Run and jump must respond on the first tick.");

        for (int tick = 1; tick < 180; tick++)
        {
            events = simulation.Step(new SimulationInput(simulation.Tick, default, default));
            Assert(Count(events, SimulationEventType.RoundStarted) == 0 &&
                Count(events, SimulationEventType.ProjectileSpawned) == 0,
                "The first projectile must wait the full 180 ticks without restarting the round.");
        }
        Vec2 beforeSpawn = simulation.CaptureSnapshot().Fighter(0).Position;
        events = simulation.Step(new SimulationInput(simulation.Tick, default, default));
        state = simulation.CaptureSnapshot();
        Assert(Count(events, SimulationEventType.ProjectileSpawned) == 1 &&
            Count(events, SimulationEventType.RoundStarted) == 0 && state.Projectiles.Count == 1,
            "The first projectile must spawn after the full three-second warning.");
        Assert(Math.Abs(state.Fighter(0).Position.X - beforeSpawn.X) < 1 &&
            Math.Abs(state.Fighter(0).Position.Y - beforeSpawn.Y) < 1,
            "First spawn must preserve the fighter's current position.");

        state.Fighter(1).Eliminated = true;
        simulation.RestoreSnapshot(state);
        simulation.Step(new SimulationInput(simulation.Tick, default, default));
        while (simulation.Phase == MatchPhase.RoundResult)
            simulation.Step(new SimulationInput(simulation.Tick, default, default));
        state = simulation.CaptureSnapshot();
        Assert(state.Phase == MatchPhase.Active && state.RoundNumber == 2 && state.Projectiles.Count == 0,
            "Testing mode must reactivate immediately when the next round resets.");
        long firstPlayableTick = simulation.Tick;
        events = simulation.Step(new SimulationInput(firstPlayableTick, new FighterInput(new Vec2(1, 0)), default));
        Assert(Count(events, SimulationEventType.RoundStarted) == 1 &&
            Count(events, SimulationEventType.SpawnWarning) == 1 &&
            Count(events, SimulationEventType.ProjectileSpawned) == 0 &&
            simulation.CaptureSnapshot().Fighter(0).Position.X > 25,
            "The reset round must emit one start and warning while accepting movement immediately.");
        Assert(simulation.CaptureSnapshot().NextSpawnTick == firstPlayableTick + 180,
            "A reset round must preserve the full warning interval from its first playable tick.");
    }

    private static void ValidatesAndClampsInputs()
    {
        var simulation = new MatchSimulation();
        ulong original = simulation.CaptureSnapshot().ComputeChecksum();
        ExpectThrows<ArgumentException>(() => simulation.Step(new SimulationInput(1, default, default)));
        ExpectThrows<ArgumentException>(() => simulation.Step(new SimulationInput(0,
            new FighterInput(new Vec2(double.NaN, 0)), default)));
        ExpectThrows<ArgumentException>(() => simulation.Step(new SimulationInput(0,
            default, new FighterInput(new Vec2(0, double.PositiveInfinity)))));
        Assert(simulation.CaptureSnapshot().ComputeChecksum() == original, "Rejected inputs must not advance or mutate state.");
        var clamped = new MatchSimulation();
        var reference = new MatchSimulation();
        StartActive(clamped);
        StartActive(reference);
        clamped.Step(new SimulationInput(clamped.Tick, new FighterInput(new Vec2(999, -999)), default));
        reference.Step(new SimulationInput(reference.Tick, new FighterInput(new Vec2(1, -1)), default));
        Assert(clamped.CaptureSnapshot().ComputeChecksum() == reference.CaptureSnapshot().ComputeChecksum(),
            "Finite input axes must clamp to the supported range before any system consumes them.");
    }

    private static void CountdownScheduleCapAndPermanence()
    {
        var simulation = new MatchSimulation();
        var warningTicks = new List<long>();
        var spawnTicks = new List<long>();
        var signs = new List<double>();
        int priorCount = 0;
        for (int i = 0; i <= 60 * 125; i++)
        {
            SimulationState snapshot = simulation.CaptureSnapshot();
            foreach (FighterState fighter in snapshot.Fighters) fighter.DashInvulnerabilityTicksRemaining = 1000000;
            simulation.RestoreSnapshot(snapshot);
            foreach (SimulationEvent item in simulation.Step(new SimulationInput(simulation.Tick, default, default)))
            {
                if (item.Type == SimulationEventType.SpawnWarning) warningTicks.Add(item.Tick);
                if (item.Type == SimulationEventType.ProjectileSpawned)
                {
                    spawnTicks.Add(item.Tick);
                    signs.Add(item.Value);
                    Assert(Math.Abs(item.Position.X - 50) < 1e-12 && item.Position.Y > 50,
                        "All projectiles originate in the top-center portal.");
                }
            }
            snapshot = simulation.CaptureSnapshot();
            Assert(snapshot.Projectiles.Count >= priorCount && snapshot.Projectiles.Count <= 6,
                "Projectiles persist indefinitely and cannot exceed the cap.");
            priorCount = snapshot.Projectiles.Count;
            Assert(snapshot.Phase == MatchPhase.Active || snapshot.Phase == MatchPhase.Countdown,
                "Invulnerable scheduling fixture must not finish its round.");
        }
        Assert(spawnTicks.Count == 6 && warningTicks.Count == 6, "Exactly six warned spawns must occur even after two minutes.");
        for (int i = 0; i < 6; i++)
        {
            Assert(spawnTicks[i] == 180 + i * 900 && warningTicks[i] == i * 900,
                "Spawns must occur at active time 0/15/30/45/60/75 seconds with full three-second warnings.");
            Assert(signs[i] == (i % 2 == 0 ? -1 : 1), "Spawn trajectories must alternate mirrored directions.");
        }
    }

    private static void DrawIsIndependentOfProjectileEnumeration()
    {
        var forward = new MatchSimulation();
        StartActive(forward);
        SimulationState state = forward.CaptureSnapshot();
        state.Projectiles.Clear();
        state.Projectiles.Add(new ProjectileState { Id = 10, Position = new Vec2(21, 1.5), Velocity = new Vec2(240, 0) });
        state.Projectiles.Add(new ProjectileState { Id = 11, Position = new Vec2(79, 1.5), Velocity = new Vec2(-480, 0) });
        forward.RestoreSnapshot(state);
        state.Projectiles.Reverse();
        var reverse = new MatchSimulation();
        reverse.RestoreSnapshot(state);
        IReadOnlyList<SimulationEvent> a = forward.Step(new SimulationInput(forward.Tick, default, default));
        IReadOnlyList<SimulationEvent> b = reverse.Step(new SimulationInput(reverse.Tick, default, default));
        SimulationState result = forward.CaptureSnapshot();
        Assert(result.Phase == MatchPhase.RoundResult && result.DrawCount == 1 && result.RoundWinnerId == -1 &&
            result.Fighter(0).RoundWins == 0 && result.Fighter(1).RoundWins == 0,
            "Both deaths anywhere within one tick must draw without a round point.");
        Assert(result.ComputeChecksum() == reverse.CaptureSnapshot().ComputeChecksum(),
            "Reversing projectile storage cannot alter a same-tick draw.");
        Assert(Count(a, SimulationEventType.RoundDraw) == 1 && Count(b, SimulationEventType.RoundDraw) == 1,
            "A draw emits exactly one round outcome.");
    }

    private static void RoundResetAndFirstToTwo()
    {
        var simulation = new MatchSimulation();
        StartActive(simulation);
        SimulationState state = simulation.CaptureSnapshot();
        state.Fighter(1).Eliminated = true;
        state.Fighter(0).Posture = 88;
        state.Fighter(0).DashCharges = 0;
        state.Fighter(0).DashRechargeTicksRemaining = 27;
        state.Fighter(0).Velocity = new Vec2(4, 8);
        state.Divider.NodeDeflections[1] = 4;
        state.Divider.NodeVelocities[1] = 9;
        simulation.RestoreSnapshot(state);
        simulation.Step(new SimulationInput(simulation.Tick, default, default));
        Assert(simulation.CaptureSnapshot().Fighter(0).RoundWins == 1, "A survivor receives one round point.");
        while (simulation.Phase == MatchPhase.RoundResult) simulation.Step(new SimulationInput(simulation.Tick, default, default));
        state = simulation.CaptureSnapshot();
        Assert(state.RoundNumber == 2 && state.Fighter(0).RoundWins == 1 && state.Projectiles.Count == 0 &&
            state.Divider.NodeDeflections[1] == 0 && state.Divider.NodeVelocities[1] == 0 && state.SpawnSequence == 0,
            "A new round keeps scores and resets projectiles, divider and scheduler.");
        foreach (FighterState fighter in state.Fighters)
            Assert(fighter.Posture == 0 && fighter.Velocity.LengthSquared == 0 && !fighter.Eliminated &&
                fighter.DashCharges == 2 && fighter.DashRechargeTicksRemaining == 0 &&
                fighter.ParryTicksRemaining == 0 && fighter.GuardBreakTicksRemaining == 0 &&
                fighter.Position.Y == 1.5 && fighter.Position.X == (fighter.Id == 0 ? 25 : 75),
                "Round reset restores baseline fighter position, posture, charges and timers.");
        while (simulation.Phase == MatchPhase.Countdown) simulation.Step(new SimulationInput(simulation.Tick, default, default));
        state = simulation.CaptureSnapshot();
        Assert(state.Projectiles[0].Velocity.X > 0, "The next round mirrors the opening trajectory.");
        state.Fighter(1).Eliminated = true;
        simulation.RestoreSnapshot(state);
        IReadOnlyList<SimulationEvent> events = simulation.Step(new SimulationInput(simulation.Tick, default, default));
        state = simulation.CaptureSnapshot();
        Assert(state.Phase == MatchPhase.MatchComplete && state.MatchWinnerId == 0 && state.Fighter(0).RoundWins == 2 &&
            Count(events, SimulationEventType.MatchWon) == 1, "The second round win completes the match exactly once.");
        events = simulation.Step(new SimulationInput(simulation.Tick, default, default));
        Assert(events.Count == 0 && simulation.CaptureSnapshot().Fighter(0).RoundWins == 2,
            "Completed matches cannot grant more wins.");
    }

    internal static void StartActive(MatchSimulation simulation)
    {
        SimulationState state = simulation.CaptureSnapshot();
        state.Phase = MatchPhase.Active;
        state.PhaseTicksRemaining = 0;
        state.NextSpawnTick = 1000000000;
        simulation.RestoreSnapshot(state);
    }

    private static int Count(IReadOnlyList<SimulationEvent> events, SimulationEventType type)
    {
        int count = 0;
        foreach (SimulationEvent item in events) if (item.Type == type) count++;
        return count;
    }

    private static void ExpectThrows<TException>(Action action) where TException : Exception
    {
        try { action(); }
        catch (TException) { return; }
        throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
