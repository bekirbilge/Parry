using System;
using Game.Core;

internal static class ContractTests
{
    public static void Run()
    {
        SweepsMovingCirclesWithoutTunneling();
        SnapshotIsDeepAndChecksumTracksState();
        InitialWarningFitsCountdown();
        ResidualFighterPathSamplesCorrectly();
    }

    private static void ResidualFighterPathSamplesCorrectly()
    {
        var fighter = new FighterState
        {
            PreviousPosition = new Vec2(4, 8), Position = new Vec2(10, 20), MotionStartFraction = 0.25
        };
        Vec2 halfway = SimMath.FighterPositionAt(fighter, 0.625);
        Assert(halfway.X == 7 && halfway.Y == 14 && SimMath.FighterPositionAt(fighter, 0).X == 4 &&
            SimMath.FighterPositionAt(fighter, 1).X == 10,
            "Contact resweeps must interpolate the remaining path from its own starting fraction.");
    }

    private static void SweepsMovingCirclesWithoutTunneling()
    {
        bool hit = SimMath.SweepCircles(new Vec2(0, 0), new Vec2(10, 0), 1,
            new Vec2(6, 0), new Vec2(-2, 0), 1, out double fraction, out Vec2 normal);
        Assert(hit && Math.Abs(fraction - 1.0 / 3.0) < 1e-12,
            "Relative sweep must find moving-circle contact between endpoints.");
        Assert(normal.X < -0.999 && Math.Abs(normal.Y) < 1e-12,
            "Sweep normal must point from target toward the moving circle.");
        Assert(!SimMath.SweepCircles(new Vec2(0, 0), new Vec2(-10, 0), 1,
            new Vec2(6, 0), Vec2.Zero, 1, out _, out _),
            "Separating circles must not report a contact.");
    }

    private static void SnapshotIsDeepAndChecksumTracksState()
    {
        var state = new SimulationState();
        state.Fighter(0).Position = new Vec2(20, 3);
        state.Projectiles.Add(new ProjectileState { Id = 7, Position = new Vec2(45, 40) });
        state.Divider.NodeDeflections[1] = 2;
        state.Divider.NodeVelocities[1] = 3;
        ulong originalHash = state.ComputeChecksum();
        SimulationState snapshot = state.Clone();
        Assert(snapshot.ComputeChecksum() == originalHash, "Clone must preserve every checksummed field.");
        snapshot.Fighter(0).Position = new Vec2(25, 3);
        snapshot.Projectiles[0].PowerLevel = 2;
        snapshot.Divider.NodeDeflections[1] = -2;
        snapshot.Divider.NodeVelocities[1] = -3;
        Assert(state.Fighter(0).Position.X == 20 && state.Projectiles[0].PowerLevel == 0 &&
            state.Divider.NodeDeflections[1] == 2 && state.Divider.NodeVelocities[1] == 3,
            "Snapshot must not share mutable fighter, projectile, or divider objects.");
        Assert(state.ComputeChecksum() == originalHash && snapshot.ComputeChecksum() != originalHash,
            "State checksum must change when authoritative fields change.");
        ulong changedDeflectionHash = snapshot.ComputeChecksum();
        snapshot.Divider.NodeVelocities[1] = 4;
        Assert(snapshot.ComputeChecksum() != changedDeflectionHash,
            "Node velocity must contribute to the authoritative checksum.");
    }

    private static void InitialWarningFitsCountdown()
    {
        var tuning = CombatTuning.Default;
        Assert(tuning.Match.RoundCountdownSeconds >= tuning.Projectile.SpawnWarningSeconds,
            "The first projectile must receive its full warning before activation.");
        Assert(SimMath.SecondsToTicks(0.15, 60) == 9 && SimMath.SecondsToTicks(0.08, 60) == 5,
            "Second-to-tick conversion must preserve baseline dash windows.");
        ExpectThrows<ArgumentOutOfRangeException>(() => new CombatTuning(
            new MovementTuning(), new ProjectileTuning(), new DefenseTuning(),
            new DividerTuning(), new ArenaTuning(), new MatchTuning(roundCountdownSeconds: 2)));
        var immediate = new CombatTuning(new MovementTuning(), new ProjectileTuning(),
            new DefenseTuning(), new DividerTuning(), new ArenaTuning(),
            new MatchTuning(roundCountdownSeconds: 0, startImmediatelyForTesting: true));
        Assert(immediate.Match.StartImmediatelyForTesting && immediate.Match.RoundCountdownSeconds == 0,
            "Testing mode may start before the projectile warning finishes.");
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
