using System;
using System.Collections.Generic;
using Game.Core;

internal static class FloorHazardTests
{
    public static void Run()
    {
        ScheduleAndMirrorScale();
        FiveCyclePatternCoversGround();
        TuningValidation();
        LethalContactIgnoresDefense();
        AirAvoidanceAndOncePerBurst();
        OppositePatchCanHitAcrossRestingCenter();
        CircleRectangleCornerContact();
        DisabledAndLethalNoPosture();
        WarningNeverKills();
        ProjectileCanStillKillOtherFighterOnHazardTick();
        FireResolvesWinAndSameTickDraw();
        RoundClockResetAndSnapshotReplay();
    }

    private static CombatTuning Tuning(double width = 100, bool enabled = true,
        bool fuel = false, double fighterRadius = 1.5) => new CombatTuning(
            new MovementTuning(useFuelBoost: fuel), new ProjectileTuning(),
            new DefenseTuning(), new DividerTuning(),
            new ArenaTuning(widthUnits: width, fighterRadiusUnits: fighterRadius),
            new MatchTuning(startImmediatelyForTesting: true), new FloorHazardTuning(enabled: enabled));

    private static SimulationState Active(long elapsed = 480, double width = 100)
    {
        var state = new SimulationState { Phase = MatchPhase.Active };
        state.FloorHazard.ElapsedActiveTicks = elapsed;
        state.Fighter(0).Position = state.Fighter(0).PreviousPosition =
            state.Fighter(0).TickStartPosition = new Vec2(width * 0.25, 2.5);
        state.Fighter(1).Position = state.Fighter(1).PreviousPosition =
            state.Fighter(1).TickStartPosition = new Vec2(width * 0.75, 2.5);
        return state;
    }

    private static List<SimulationEvent> Apply(SimulationState state, CombatTuning tuning,
        FighterInput left = default, FighterInput right = default)
    {
        var events = new List<SimulationEvent>();
        FloorHazardSystem.Apply(state, new SimulationInput(state.Tick, left, right),
            tuning, new SimulationConfig(), events);
        return events;
    }

    private static void ScheduleAndMirrorScale()
    {
        foreach (double width in new[] { 100.0, 125.0, 150.0, 180.0 })
        {
            CombatTuning tuning = Tuning(width);
            SimulationState state = Active(0, width);
            FloorHazardFrame frame = FloorHazardSystem.Sample(state, tuning, new SimulationConfig());
            Assert(frame.Phase == FloorHazardPhase.Dormant && frame.TicksRemaining == 360,
                "Opening quiet period must last six active seconds.");
            state.FloorHazard.ElapsedActiveTicks = 359;
            Assert(FloorHazardSystem.Sample(state, tuning, new SimulationConfig()).TicksRemaining == 1,
                "The last quiet tick must remain dormant.");
            state.FloorHazard.ElapsedActiveTicks = 360;
            frame = FloorHazardSystem.Sample(state, tuning, new SimulationConfig());
            Assert(frame.Phase == FloorHazardPhase.Warning && frame.TicksRemaining == 120 &&
                frame.CycleIndex == 0, "Warning must start at active tick 360.");
            Assert(Near(frame.LeftCenterX, width * 0.25) && Near(frame.RightCenterX, width * 0.75) &&
                Near(frame.HalfWidth, width * 0.06) && Near(frame.HeightUnits, 1.5),
                "First mirrored patches must scale from each half and retain the configured flame height.");
            state.FloorHazard.ElapsedActiveTicks = 479;
            Assert(FloorHazardSystem.Sample(state, tuning, new SimulationConfig()).Phase == FloorHazardPhase.Warning,
                "The last warning tick must remain warning.");
            state.FloorHazard.ElapsedActiveTicks = 480;
            frame = FloorHazardSystem.Sample(state, tuning, new SimulationConfig());
            Assert(frame.Phase == FloorHazardPhase.Active && frame.TicksRemaining == 75,
                "Flame must begin after two seconds and last 75 ticks.");
            state.FloorHazard.ElapsedActiveTicks = 554;
            Assert(FloorHazardSystem.Sample(state, tuning, new SimulationConfig()).Phase == FloorHazardPhase.Active,
                "The final flame tick must remain active.");
            state.FloorHazard.ElapsedActiveTicks = 555;
            Assert(FloorHazardSystem.Sample(state, tuning, new SimulationConfig()).Phase == FloorHazardPhase.Dormant,
                "The flame must cease at its exact endpoint.");
            double[] fractions = { .5, .12, .88, .32, .68, .5 };
            for (int cycle = 1; cycle < fractions.Length; cycle++)
            {
                state.FloorHazard.ElapsedActiveTicks = 360 + cycle * 600;
                frame = FloorHazardSystem.Sample(state, tuning, new SimulationConfig());
                double fraction = fractions[cycle];
                Assert(frame.Phase == FloorHazardPhase.Warning && frame.CycleIndex == cycle &&
                    Near(frame.LeftCenterX, width / 2 * fraction) &&
                    Near(frame.RightCenterX, width - frame.LeftCenterX),
                    "Five patch positions must rotate deterministically with exact mirror symmetry.");
            }
        }
    }

    private static void FiveCyclePatternCoversGround()
    {
        foreach (double width in new[] { 100.0, 125.0, 150.0, 180.0 })
        {
            CombatTuning tuning = Tuning(width);
            double radius = tuning.Arena.FighterRadiusUnits;
            double halfArena = width / 2;
            var intervals = new List<(double Low, double High)>();
            for (int cycle = 0; cycle < 5; cycle++)
            {
                SimulationState state = Active(480 + cycle * 600, width);
                FloorHazardFrame frame = FloorHazardSystem.Sample(state, tuning, new SimulationConfig());
                Assert(frame.Phase == FloorHazardPhase.Active &&
                    Near(frame.LeftCenterX + frame.RightCenterX, width),
                    "Each cycle must activate both mirrored patches together.");
                Assert(frame.LeftCenterX - frame.HalfWidth >= -1e-9 &&
                    frame.LeftCenterX + frame.HalfWidth <= halfArena + 1e-9,
                    "Each patch must remain inside its arena half.");
                intervals.Add((frame.LeftCenterX - frame.HalfWidth - radius,
                    frame.LeftCenterX + frame.HalfWidth + radius));
            }
            intervals.Sort((a, b) => a.Low.CompareTo(b.Low));
            double coveredUntil = radius;
            foreach (var interval in intervals)
            {
                Assert(interval.Low <= coveredUntil + 1e-9,
                    "A legal grounded center must not fit between all five fire patches.");
                coveredUntil = Math.Max(coveredUntil, interval.High);
            }
            Assert(coveredUntil >= halfArena - radius - 1e-9,
                "The five-cycle union must reach the rope-side grounded limit.");
        }

        CombatTuning cornerTuning = Tuning();
        SimulationState corner = Active(1080);
        corner.Fighter(0).Position = new Vec2(2.5, 1.5);
        corner.Fighter(1).Position = new Vec2(97.5, 1.5);
        Assert(Count(Apply(corner, cornerTuning), SimulationEventType.FighterEliminated) == 2,
            "The near-wall burst must end stationary camping at both floor corners.");
    }

    private static void TuningValidation()
    {
        Assert(!CombatTuning.Default.FloorHazard.Enabled, "Core hazard must default to disabled.");
        ExpectInvalid(() => new FloorHazardTuning(warningSeconds: 8, activeSeconds: 3));
        ExpectInvalid(() => new FloorHazardTuning(patchWidthFraction: 0));
        ExpectInvalid(() => new FloorHazardTuning(heightUnits: double.NaN));
    }

    private static void LethalContactIgnoresDefense()
    {
        CombatTuning tuning = Tuning();
        SimulationState state = Active();
        FighterState left = state.Fighter(0);
        FighterState right = state.Fighter(1);
        left.Posture = right.Posture = 10;
        left.ParryTicksRemaining = 8;
        left.DefenseHoldTicks = 100;
        left.DashTicksRemaining = 12;
        left.DashInvulnerabilityTicksRemaining = 12;
        left.Velocity = new Vec2(8, 9);
        List<SimulationEvent> events = Apply(state, tuning,
            new FighterInput(Vec2.Zero, defensePressed: true, defenseHeld: true),
            new FighterInput(Vec2.Zero, defenseHeld: true));
        Assert(Count(events, SimulationEventType.FloorHazardHit) == 2 &&
            Count(events, SimulationEventType.FighterEliminated) == 2 &&
            Count(events, SimulationEventType.PerfectParry) == 0 &&
            Count(events, SimulationEventType.GuardBlocked) == 0,
            "Ground fire must hit both mirrored fighters through guard, parry and invulnerability.");
        Assert(left.Eliminated && right.Eliminated && Near(left.Posture, 10) &&
            Near(right.Posture, 10) && left.Velocity.Equals(new Vec2(8, 9)),
            "Fire must eliminate without adding posture or launching the fighter.");
        Assert(events[0].EntityId == 0 && events[1].EntityId == 0 &&
            events[1].TargetId == -1 && events[0].Position.Equals(left.Position),
            "Fire and elimination events must identify the fighter and use a nonprojectile cause.");
    }

    private static void AirAvoidanceAndOncePerBurst()
    {
        CombatTuning tuning = Tuning();
        SimulationState state = Active();
        state.Fighter(1).Position = new Vec2(75, 4.0001);
        Assert(Count(Apply(state, tuning), SimulationEventType.FloorHazardHit) == 1,
            "A fighter with body bottom above the flame must avoid it.");
        state.Fighter(0).Position = new Vec2(25, 2.5);
        state.FloorHazard.ElapsedActiveTicks = 554;
        Assert(Count(Apply(state, tuning), SimulationEventType.FloorHazardHit) == 0,
            "The same fighter cannot be hit twice in one active burst.");
        SimulationState nextBurst = Active(1080);
        nextBurst.Fighter(0).Position = new Vec2(12.5, 2.5);
        nextBurst.Fighter(1).Position = new Vec2(75, 10);
        Assert(Count(Apply(nextBurst, tuning), SimulationEventType.FloorHazardHit) == 1,
            "A later mirrored burst must use its new patch positions.");
    }

    private static void OppositePatchCanHitAcrossRestingCenter()
    {
        CombatTuning tuning = Tuning();
        // The third cycle places patches at 44 and 56. A stretched rope
        // can let fighters cross the resting center; isolate that contact here.
        SimulationState state = Active(1680);
        state.Fighter(0).Position = new Vec2(54, 2.5);
        state.Fighter(1).Position = new Vec2(46, 2.5);
        FloorHazardFrame frame = FloorHazardSystem.Sample(state, tuning, new SimulationConfig());
        Assert(frame.Phase == FloorHazardPhase.Active &&
            Near(frame.LeftCenterX, 44) && Near(frame.RightCenterX, 56),
            "Fixture must place the fire patches near the rope on opposite sides.");
        List<SimulationEvent> events = Apply(state, tuning);
        Assert(Count(events, SimulationEventType.FloorHazardHit) == 2 &&
            state.Fighter(0).Eliminated && state.Fighter(1).Eliminated,
            "Each fighter must be hit by either visible patch, regardless of team side.");
    }

    private static void CircleRectangleCornerContact()
    {
        CombatTuning tuning = Tuning(fighterRadius: 2.5);
        var samples = new[]
        {
            (Position: new Vec2(33.5, 4), Hits: false),
            (Position: new Vec2(25, 4), Hits: true),
            (Position: new Vec2(33.5, 1.5), Hits: true)
        };
        foreach (var sample in samples)
        {
            SimulationState state = Active();
            state.Fighter(0).Position = sample.Position;
            state.Fighter(1).Position = new Vec2(75, 10);
            FloorHazardFrame frame = FloorHazardSystem.Sample(state, tuning, new SimulationConfig());
            Assert(Near(frame.LeftCenterX + frame.HalfWidth, 31) && Near(frame.HeightUnits, 1.5),
                "Geometry fixture must place the patch corner at (31, 1.5).");
            Assert((Count(Apply(state, tuning), SimulationEventType.FloorHazardHit) == 1) == sample.Hits,
                "Circle against fire rectangle must reject diagonal corner misses and include edge tangencies.");
        }
    }

    private static void DisabledAndLethalNoPosture()
    {
        SimulationState disabled = Active();
        Assert(Count(Apply(disabled, Tuning(enabled: false)), SimulationEventType.FloorHazardHit) == 0 &&
            FloorHazardSystem.Sample(disabled, Tuning(enabled: false), new SimulationConfig()).Phase == FloorHazardPhase.Dormant,
            "Core defaults must leave the hazard disabled.");
        SimulationState state = Active();
        state.Fighter(0).Posture = 99;
        state.Fighter(1).Position = new Vec2(75, 10);
        List<SimulationEvent> events = Apply(state, Tuning());
        Assert(Near(state.Fighter(0).Posture, 99) && state.Fighter(0).Eliminated &&
            Count(events, SimulationEventType.GuardBroken) == 0 &&
            Count(events, SimulationEventType.FighterEliminated) == 1,
            "Fire must eliminate on contact without touching posture or guard break.");
    }

    private static void WarningNeverKills()
    {
        CombatTuning tuning = Tuning();
        SimulationState state = Active(479);
        Assert(Count(Apply(state, tuning), SimulationEventType.FighterEliminated) == 0 &&
            !state.Fighter(0).Eliminated && !state.Fighter(1).Eliminated,
            "The final warning tick must remain harmless.");
        state.FloorHazard.ElapsedActiveTicks = 480;
        Assert(Count(Apply(state, tuning), SimulationEventType.FighterEliminated) == 2,
            "The first active fire tick must be lethal.");
    }

    private static void ProjectileCanStillKillOtherFighterOnHazardTick()
    {
        CombatTuning tuning = Tuning();
        SimulationState state = Active();
        FighterState right = state.Fighter(1);
        right.Position = right.PreviousPosition = right.TickStartPosition = new Vec2(75, 10);
        state.Projectiles.Add(new ProjectileState
        {
            Id = 7, Position = new Vec2(75, 10), PreviousPosition = new Vec2(75, 10),
            Velocity = new Vec2(24, 0)
        });
        var events = new List<SimulationEvent>();
        CombatSystem.Advance(state, new SimulationInput(state.Tick, default, default),
            tuning, new SimulationConfig(), events);
        Assert(state.Fighter(0).Eliminated && right.Eliminated &&
            Count(events, SimulationEventType.FloorHazardHit) == 1 &&
            Count(events, SimulationEventType.FighterEliminated) == 2,
            "Fire elimination must not suppress a later projectile kill of the other fighter.");
        Assert(events[1].Type == SimulationEventType.FighterEliminated &&
            events[1].EntityId == 0 && events[1].TargetId == -1 &&
            events.Exists(item => item.Type == SimulationEventType.FighterEliminated &&
                item.EntityId == 1 && item.TargetId == 7),
            "Elimination events must distinguish fire from projectile causes.");
    }

    private static void FireResolvesWinAndSameTickDraw()
    {
        CombatTuning tuning = Tuning();
        var draw = new MatchSimulation(tuning: tuning);
        SimulationState state = draw.CaptureSnapshot();
        state.FloorHazard.ElapsedActiveTicks = 480;
        draw.RestoreSnapshot(state);
        var replay = new MatchSimulation(tuning: tuning);
        replay.RestoreSnapshot(draw.CaptureSnapshot());
        var input = new SimulationInput(draw.Tick, default, default);
        IReadOnlyList<SimulationEvent> drawEvents = draw.Step(input);
        replay.Step(input);
        state = draw.CaptureSnapshot();
        Assert(Count(drawEvents, SimulationEventType.FloorHazardHit) == 2 &&
            Count(drawEvents, SimulationEventType.RoundDraw) == 1 &&
            state.Phase == MatchPhase.RoundResult && state.DrawCount == 1 &&
            state.Fighter(0).RoundWins == 0 && state.Fighter(1).RoundWins == 0 &&
            draw.CaptureSnapshot().ComputeChecksum() == replay.CaptureSnapshot().ComputeChecksum(),
            "Same-step mirrored fire kills must draw without points and replay deterministically.");

        var win = new MatchSimulation(tuning: tuning);
        state = win.CaptureSnapshot();
        state.FloorHazard.ElapsedActiveTicks = 480;
        state.Fighter(1).Position = state.Fighter(1).PreviousPosition =
            state.Fighter(1).TickStartPosition = new Vec2(75, 10);
        win.RestoreSnapshot(state);
        IReadOnlyList<SimulationEvent> winEvents = win.Step(new SimulationInput(win.Tick, default, default));
        state = win.CaptureSnapshot();
        Assert(Count(winEvents, SimulationEventType.RoundWon) == 1 &&
            Count(winEvents, SimulationEventType.RoundDraw) == 0 &&
            state.RoundWinnerId == 1 && state.Fighter(1).RoundWins == 1 &&
            state.Fighter(0).RoundWins == 0,
            "A single fire kill must award the surviving opponent the round.");
    }

    private static void RoundClockResetAndSnapshotReplay()
    {
        CombatTuning tuning = Tuning();
        var countdownTuning = new CombatTuning(new MovementTuning(), new ProjectileTuning(),
            new DefenseTuning(), new DividerTuning(), new ArenaTuning(),
            floorHazard: new FloorHazardTuning(enabled: true));
        var countdown = new MatchSimulation(tuning: countdownTuning);
        for (int i = 0; i < 60; i++)
            countdown.Step(new SimulationInput(countdown.Tick, default, default));
        Assert(countdown.CaptureSnapshot().FloorHazard.ElapsedActiveTicks == 0,
            "Round countdown must not consume the fire quiet period.");
        var first = new MatchSimulation(tuning: tuning);
        SimulationState state = first.CaptureSnapshot();
        state.FloorHazard.ElapsedActiveTicks = 359;
        first.RestoreSnapshot(state);
        var second = new MatchSimulation(tuning: tuning);
        second.RestoreSnapshot(first.CaptureSnapshot());
        SimulationState detached = first.CaptureSnapshot();
        detached.FloorHazard.LastHitCycle[0] = 99;
        Assert(first.CaptureSnapshot().FloorHazard.LastHitCycle[0] == -1,
            "Snapshot capture must clone the hazard hit ledger.");
        detached.FloorHazard.LastHitCycle = new long[1];
        try
        {
            first.RestoreSnapshot(detached);
            throw new InvalidOperationException("Malformed hazard ledgers must be rejected on restore.");
        }
        catch (ArgumentException) { }
        for (int i = 0; i < 8; i++)
        {
            var input = new SimulationInput(first.Tick, default, default);
            first.Step(input);
            second.Step(input);
            Assert(first.CaptureSnapshot().ComputeChecksum() == second.CaptureSnapshot().ComputeChecksum(),
                "Replay after a warning boundary must preserve hazard state and checksum.");
        }
        state = first.CaptureSnapshot();
        state.FloorHazard.ElapsedActiveTicks = 480;
        first.RestoreSnapshot(state);
        first.Step(new SimulationInput(first.Tick, default, default));
        state = first.CaptureSnapshot();
        Assert(state.Phase == MatchPhase.RoundResult && state.DrawCount == 1,
            "Simultaneous eliminations must resolve as a draw.");
        long clock = state.FloorHazard.ElapsedActiveTicks;
        first.Step(new SimulationInput(first.Tick, default, default));
        Assert(first.CaptureSnapshot().FloorHazard.ElapsedActiveTicks == clock,
            "Result time must not advance the hazard clock.");
        while (first.Phase == MatchPhase.RoundResult)
            first.Step(new SimulationInput(first.Tick, default, default));
        state = first.CaptureSnapshot();
        Assert(state.FloorHazard.ElapsedActiveTicks == 0 &&
            state.FloorHazard.LastHitCycle[0] == -1 && state.FloorHazard.LastHitCycle[1] == -1 &&
            FloorHazardSystem.Sample(state, tuning, new SimulationConfig()).TicksRemaining == 360,
            "A drawn round must restart the full quiet period and hit ledger.");
    }

    private static int Count(IEnumerable<SimulationEvent> events, SimulationEventType type)
    {
        int count = 0;
        foreach (SimulationEvent item in events) if (item.Type == type) count++;
        return count;
    }

    private static bool Near(double left, double right) => Math.Abs(left - right) < 1e-9;
    private static void ExpectInvalid(Action action)
    {
        try { action(); }
        catch (ArgumentOutOfRangeException) { return; }
        throw new InvalidOperationException("Invalid hazard tuning must be rejected.");
    }
    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
