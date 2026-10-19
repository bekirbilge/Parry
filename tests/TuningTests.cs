using System;
using Game.Core;

internal static class TuningTests
{
    public static void Run()
    {
        DefaultTuningMatchesDesignBaselines();
        RejectsInvalidGroupsAndRelationships();
        AllowsSingleHitGuardBreakTuning();
    }

    private static void DefaultTuningMatchesDesignBaselines()
    {
        CombatTuning tuning = CombatTuning.Default;
        Assert(MovementTuning.MaximumDashCharges == 2, "Dash charge cap must remain locked at two.");
        Assert(tuning.Movement.RunSpeedUnitsPerSecond == 24 && tuning.Movement.JumpSpeedUnitsPerSecond == 36 &&
            tuning.Movement.DashSpeedUnitsPerSecond == 80, "Playtest movement speed defaults must be applied.");
        Assert(tuning.Movement.GroundAccelerationUnitsPerSecondSquared == 240 &&
            tuning.Movement.AirAccelerationUnitsPerSecondSquared == 140 &&
            tuning.Movement.GroundFrictionUnitsPerSecondSquared == 300 &&
            tuning.Movement.GravityUnitsPerSecondSquared == 60,
            "Playtest movement control defaults must preserve gravity and faster response.");
        Assert(tuning.Movement.DashDurationSeconds == 0.15, "Dash duration baseline must match the design.");
        Assert(tuning.Movement.DashInvulnerabilitySeconds == 0.08, "Dash invulnerability baseline must match the design.");
        Assert(tuning.Projectile.SpawnIntervalSeconds == 15 && tuning.Projectile.MaximumActiveCount == 6, "Projectile schedule must match the design baseline.");
        Assert(tuning.Projectile.SpawnWarningSeconds == 3, "Projectile warning baseline must match the design.");
        Assert(tuning.Projectile.SpeedMultiplierAtLevel(3) == 1.45, "Critical projectile speed multiplier must match the design.");
        Assert(tuning.GetBlockPostureDamage(1) == 38, "Half posture points round away from zero.");
        Assert(tuning.GetBlockPostureDamage(2) == 45 && tuning.GetBlockPostureDamage(3) == 60, "Block posture table must match the design.");
        Assert(tuning.Defense.GuardIdleRecoveryPerSecond > tuning.Defense.UnguardedIdleRecoveryPerSecond, "Guard recovery must be faster.");
        Assert(tuning.Divider.MaximumDeflectionUnits == 8 && tuning.Arena.WidthUnits == 100, "Arena and divider baseline must match the design.");
    }

    private static void RejectsInvalidGroupsAndRelationships()
    {
        ExpectThrows<ArgumentOutOfRangeException>(() => new MovementTuning(dashDurationSeconds: 0.1, dashInvulnerabilitySeconds: 0.2));
        ExpectThrows<ArgumentOutOfRangeException>(() => new ProjectileTuning(spawnWarningSeconds: double.NaN));
        ExpectThrows<ArgumentOutOfRangeException>(() => new DefenseTuning(guardIdleRecoveryPerSecond: 4, unguardedIdleRecoveryPerSecond: 5));
        ExpectThrows<ArgumentOutOfRangeException>(() => new CombatTuning(
            new MovementTuning(), new ProjectileTuning(), new DefenseTuning(),
            new DividerTuning(maximumDeflectionUnits: 49), new ArenaTuning()));
        ExpectThrows<ArgumentOutOfRangeException>(() => new CombatTuning(
            new MovementTuning(), new ProjectileTuning(), new DefenseTuning(),
            new DividerTuning(maximumDeflectionUnits: 5.1, jumpingDeflectionUnits: 5,
                dashingDeflectionUnits: 5),
            new ArenaTuning(widthUnits: 20, heightUnits: 20, fighterRadiusUnits: 2.5)));
    }

    private static void AllowsSingleHitGuardBreakTuning()
    {
        var tuning = new CombatTuning(
            new MovementTuning(), new ProjectileTuning(),
            new DefenseTuning(baseBlockPostureDamage: 60),
            new DividerTuning(), new ArenaTuning());
        Assert(tuning.GetBlockPostureDamage(3) == 120,
            "Damage above maximum posture must remain a legal guard-break tuning value.");
    }

    private static void ExpectThrows<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
