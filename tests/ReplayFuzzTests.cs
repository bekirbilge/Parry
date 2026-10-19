using System;
using Game.Core;

internal static class ReplayFuzzTests
{
    public static void Run()
    {
        RepeatedInputsAndSnapshotResumeMatch();
        LongFuzzyInputRunStaysValid();
        HundredsOfDeterministicBotRounds();
    }

    private static void RepeatedInputsAndSnapshotResumeMatch()
    {
        var first = new MatchSimulation();
        var second = new MatchSimulation();
        uint random = 0x9e3779b9;
        MatchSimulation? resumed = null;
        for (int tick = 0; tick < 18000; tick++)
        {
            var input = new SimulationInput(first.Tick, RandomInput(ref random), RandomInput(ref random));
            first.Step(input);
            second.Step(input);
            SimulationState a = first.CaptureSnapshot();
            Assert(a.ComputeChecksum() == second.CaptureSnapshot().ComputeChecksum(),
                "Repeated input streams must produce identical state hashes on this runtime.");
            if (tick == 1000)
            {
                resumed = new MatchSimulation();
                resumed.RestoreSnapshot(a);
                a.Fighter(0).Position = Vec2.Zero;
                a.Divider.NodeDeflections[1] = -99;
                a.Projectiles.Clear();
                Assert(first.CaptureSnapshot().ComputeChecksum() == resumed.CaptureSnapshot().ComputeChecksum(),
                    "Capturing and restoring snapshots must retain no mutable aliases.");
            }
            else if (resumed != null)
            {
                resumed.Step(input);
                Assert(first.CaptureSnapshot().ComputeChecksum() == resumed.CaptureSnapshot().ComputeChecksum(),
                    "A resumed replay must continue identically after a deep snapshot restore.");
            }
            if (first.Phase == MatchPhase.MatchComplete)
            {
                first = new MatchSimulation();
                second = new MatchSimulation();
                if (resumed != null) resumed = new MatchSimulation();
            }
        }
    }

    private static void LongFuzzyInputRunStaysValid()
    {
        uint random = 0x12345678;
        var simulation = new MatchSimulation();
        for (int tick = 0; tick < 60000; tick++)
        {
            simulation.Step(new SimulationInput(simulation.Tick, RandomInput(ref random), RandomInput(ref random)));
            ValidateState(simulation.CaptureSnapshot(), simulation.Tuning);
            if (simulation.Phase == MatchPhase.MatchComplete) simulation = new MatchSimulation();
        }
    }

    private static void HundredsOfDeterministicBotRounds()
    {
        var simulation = new MatchSimulation();
        int rounds = 0;
        int totalTicks = 0;
        uint random = 0xabcdef01;
        const int requiredRounds = 240;
        while (rounds < requiredRounds && totalTicks < 1500000)
        {
            FighterInput left = BotInput(simulation.Tick, 0, ref random);
            FighterInput right = BotInput(simulation.Tick, 1, ref random);
            foreach (SimulationEvent item in simulation.Step(new SimulationInput(simulation.Tick, left, right)))
                if (item.Type == SimulationEventType.RoundWon || item.Type == SimulationEventType.RoundDraw) rounds++;
            ValidateState(simulation.CaptureSnapshot(), simulation.Tuning);
            if (simulation.Phase == MatchPhase.MatchComplete) simulation = new MatchSimulation();
            totalTicks++;
        }
        Assert(rounds >= requiredRounds, "Deterministic bots must finish hundreds of actual rounds without a timeout rule.");
        Console.WriteLine("Completed " + rounds + " deterministic bot rounds in " + totalTicks + " ticks.");
    }

    private static FighterInput BotInput(long tick, int id, ref uint random)
    {
        uint bits = Next(ref random);
        double direction = ((tick / (id == 0 ? 97 : 131) + id) & 1) == 0 ? 1 : -1;
        bool jump = tick % (id == 0 ? 139 : 167) == 0;
        bool defense = bits % 173 == 0;
        return new FighterInput(new Vec2(direction, (bits & 31) == 0 ? 1 : 0),
            jump, tick % 180 < 30, bits % 113 == 0, defense, defense);
    }

    private static FighterInput RandomInput(ref uint random)
    {
        uint bits = Next(ref random);
        // Exercise clamping alongside diagonals, held buttons and simultaneous edges.
        double x = ((int)(bits & 1023) - 511) / 256.0;
        double y = ((int)((bits >> 10) & 1023) - 511) / 256.0;
        return new FighterInput(new Vec2(x, y), (bits & (1u << 20)) != 0,
            (bits & (1u << 21)) != 0, (bits & (1u << 22)) != 0,
            (bits & (1u << 23)) != 0, (bits & (1u << 24)) != 0);
    }

    private static uint Next(ref uint state)
    {
        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return state;
    }

    private static void ValidateState(SimulationState state, CombatTuning tuning)
    {
        const double epsilon = 1e-7;
        double radius = tuning.Arena.FighterRadiusUnits;
        for (int i = 0; i < DividerSystem.RopePointCount; i++)
            Assert(Finite(state.Divider.NodeDeflections[i]) && Finite(state.Divider.NodeVelocities[i]) &&
                Math.Abs(state.Divider.NodeDeflections[i]) <= tuning.Divider.MaximumDeflectionUnits + epsilon &&
                (i != 0 && i != DividerSystem.RopePointCount - 1 ||
                 state.Divider.NodeDeflections[i] == 0 && state.Divider.NodeVelocities[i] == 0),
                "Pinned spring nodes must remain finite and bounded.");
        foreach (FighterState fighter in state.Fighters)
        {
            Assert(Finite(fighter.Position.X) && Finite(fighter.Position.Y) &&
                Finite(fighter.Velocity.X) && Finite(fighter.Velocity.Y) && Finite(fighter.Posture),
                "Every fighter position, velocity and posture must remain finite.");
            Assert(fighter.Position.X >= radius - epsilon && fighter.Position.X <= tuning.Arena.WidthUnits - radius + epsilon &&
                fighter.Position.Y >= radius - epsilon && fighter.Position.Y <= tuning.Arena.HeightUnits - radius + epsilon,
                "Fighters must remain inside arena collision bounds.");
            double limit = DividerSystem.FighterCenterLimitX(state, tuning, fighter.Id, fighter.Position.Y);
            Assert(fighter.Id == 0 ? fighter.Position.X <= limit + epsilon : fighter.Position.X >= limit - epsilon,
                "Neither fighter circle may cross the local rope polyline.");
            Assert(fighter.DashCharges >= 0 && fighter.DashCharges <= 2 && fighter.Posture >= 0 && fighter.Posture <= tuning.Defense.MaximumPosture,
                "Posture and charge counts must remain in their supported ranges.");
        }
        Assert(state.Projectiles.Count <= tuning.Projectile.MaximumActiveCount, "Active projectile cap must hold throughout fuzzing.");
        radius = tuning.Arena.ProjectileRadiusUnits;
        foreach (ProjectileState projectile in state.Projectiles)
            Assert(Finite(projectile.Position.X) && Finite(projectile.Position.Y) &&
                Finite(projectile.Velocity.X) && Finite(projectile.Velocity.Y) &&
                projectile.Position.X >= radius - epsilon && projectile.Position.X <= tuning.Arena.WidthUnits - radius + epsilon &&
                projectile.Position.Y >= radius - epsilon && projectile.Position.Y <= tuning.Arena.HeightUnits - radius + epsilon &&
                projectile.PowerLevel >= 0 && projectile.PowerLevel <= 3,
                "Permanent ricochets must stay finite, in bounds, and within power levels zero through three.");
    }

    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
