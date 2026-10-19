#nullable enable
using System;

namespace Game.Core
{
    /// <summary>Central source for the initial combat and arena tuning hypotheses.</summary>
    public sealed class CombatTuning
    {
        public MovementTuning Movement { get; }
        public ProjectileTuning Projectile { get; }
        public DefenseTuning Defense { get; }
        public DividerTuning Divider { get; }
        public ArenaTuning Arena { get; }
        public MatchTuning Match { get; }

        public static CombatTuning Default { get; } = new CombatTuning(
            new MovementTuning(), new ProjectileTuning(), new DefenseTuning(),
            new DividerTuning(), new ArenaTuning());

        public CombatTuning(
            MovementTuning movement,
            ProjectileTuning projectile,
            DefenseTuning defense,
            DividerTuning divider,
            ArenaTuning arena,
            MatchTuning? match = null)
        {
            Movement = movement ?? throw new ArgumentNullException(nameof(movement));
            Projectile = projectile ?? throw new ArgumentNullException(nameof(projectile));
            Defense = defense ?? throw new ArgumentNullException(nameof(defense));
            Divider = divider ?? throw new ArgumentNullException(nameof(divider));
            Arena = arena ?? throw new ArgumentNullException(nameof(arena));
            Match = match ?? new MatchTuning();

            if (Divider.MaximumDeflectionUnits > Arena.WidthUnits / 2.0 - 2 * Arena.FighterRadiusUnits)
            {
                throw new ArgumentOutOfRangeException(nameof(divider), "Divider stretch must leave a fighter diameter within each half of the arena.");
            }

            if (Projectile.MaximumActiveCount < ProjectileTuning.InitialProjectileCount)
            {
                throw new ArgumentOutOfRangeException(nameof(projectile), "Projectile cap must allow the round's initial projectile.");
            }

            if (!Match.StartImmediatelyForTesting && Match.RoundCountdownSeconds < Projectile.SpawnWarningSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(match), "Round countdown must permit the initial projectile warning.");
            }

        }

        public int GetBlockPostureDamage(int projectileLevel)
        {
            return RoundPostureDamage(Defense.BaseBlockPostureDamage * Projectile.BlockPostureMultiplierAtLevel(projectileLevel));
        }

        public static int RoundPostureDamage(double amount)
        {
            if (!TuningValidation.IsFinite(amount) || amount < 0 || amount > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Posture damage must be finite and non-negative.");
            }

            return (int)Math.Round(amount, MidpointRounding.AwayFromZero);
        }
    }

    public sealed class MovementTuning
    {
        public const int MaximumDashCharges = 2;

        public double RunSpeedUnitsPerSecond { get; }
        public double JumpSpeedUnitsPerSecond { get; }
        public double DashDurationSeconds { get; }
        public double DashInvulnerabilitySeconds { get; }
        public double DashRechargeSeconds { get; }
        public double PerfectParryWindowSeconds { get; }
        public double GravityUnitsPerSecondSquared { get; }
        public double GroundAccelerationUnitsPerSecondSquared { get; }
        public double AirAccelerationUnitsPerSecondSquared { get; }
        public double GroundFrictionUnitsPerSecondSquared { get; }
        public double DashSpeedUnitsPerSecond { get; }
        public double JumpReleaseGravityMultiplier { get; }
        public double CoyoteTimeSeconds { get; }
        public double JumpBufferSeconds { get; }
        public double DashCancelAfterSeconds { get; }

        public MovementTuning(
            double runSpeedUnitsPerSecond = 24.0,
            double jumpSpeedUnitsPerSecond = 36.0,
            double dashDurationSeconds = 0.15,
            double dashInvulnerabilitySeconds = 0.08,
            double dashRechargeSeconds = 1.0,
            double perfectParryWindowSeconds = 0.10,
            double gravityUnitsPerSecondSquared = 60.0,
            double groundAccelerationUnitsPerSecondSquared = 240.0,
            double airAccelerationUnitsPerSecondSquared = 140.0,
            double groundFrictionUnitsPerSecondSquared = 300.0,
            double dashSpeedUnitsPerSecond = 80.0,
            double jumpReleaseGravityMultiplier = 2.0,
            double coyoteTimeSeconds = 0.08,
            double jumpBufferSeconds = 0.10,
            double dashCancelAfterSeconds = 0.08)
        {
            RunSpeedUnitsPerSecond = TuningValidation.InRange(runSpeedUnitsPerSecond, nameof(runSpeedUnitsPerSecond), 0.1, 100.0);
            JumpSpeedUnitsPerSecond = TuningValidation.InRange(jumpSpeedUnitsPerSecond, nameof(jumpSpeedUnitsPerSecond), 0.1, 100.0);
            DashDurationSeconds = TuningValidation.InRange(dashDurationSeconds, nameof(dashDurationSeconds), 0.01, 2.0);
            DashInvulnerabilitySeconds = TuningValidation.InRange(dashInvulnerabilitySeconds, nameof(dashInvulnerabilitySeconds), 0.0, 2.0);
            if (DashInvulnerabilitySeconds > DashDurationSeconds)
            {
                throw new ArgumentOutOfRangeException(nameof(dashInvulnerabilitySeconds), "Dash invulnerability cannot outlast the dash.");
            }

            DashRechargeSeconds = TuningValidation.InRange(dashRechargeSeconds, nameof(dashRechargeSeconds), 0.05, 60.0);
            PerfectParryWindowSeconds = TuningValidation.InRange(perfectParryWindowSeconds, nameof(perfectParryWindowSeconds), 0.01, 1.0);
            GravityUnitsPerSecondSquared = TuningValidation.InRange(gravityUnitsPerSecondSquared, nameof(gravityUnitsPerSecondSquared), 0.1, 1000.0);
            GroundAccelerationUnitsPerSecondSquared = TuningValidation.InRange(groundAccelerationUnitsPerSecondSquared, nameof(groundAccelerationUnitsPerSecondSquared), 0.1, 1000.0);
            AirAccelerationUnitsPerSecondSquared = TuningValidation.InRange(airAccelerationUnitsPerSecondSquared, nameof(airAccelerationUnitsPerSecondSquared), 0.1, 1000.0);
            GroundFrictionUnitsPerSecondSquared = TuningValidation.InRange(groundFrictionUnitsPerSecondSquared, nameof(groundFrictionUnitsPerSecondSquared), 0.1, 1000.0);
            DashSpeedUnitsPerSecond = TuningValidation.InRange(dashSpeedUnitsPerSecond, nameof(dashSpeedUnitsPerSecond), 0.1, 1000.0);
            JumpReleaseGravityMultiplier = TuningValidation.InRange(jumpReleaseGravityMultiplier, nameof(jumpReleaseGravityMultiplier), 1.0, 10.0);
            CoyoteTimeSeconds = TuningValidation.InRange(coyoteTimeSeconds, nameof(coyoteTimeSeconds), 0.0, 1.0);
            JumpBufferSeconds = TuningValidation.InRange(jumpBufferSeconds, nameof(jumpBufferSeconds), 0.0, 1.0);
            DashCancelAfterSeconds = TuningValidation.InRange(dashCancelAfterSeconds, nameof(dashCancelAfterSeconds), 0.0, DashDurationSeconds);
        }
    }

    public sealed class ProjectileTuning
    {
        public const int MaximumPowerLevel = 3;
        public const int InitialProjectileCount = 1;

        public int MaximumActiveCount { get; }
        public double SpawnIntervalSeconds { get; }
        public double SpawnWarningSeconds { get; }
        public double BaseSpeedUnitsPerSecond { get; }
        public double PowerDecaySecondsPerLevel { get; }
        public double MaximumParrySteeringDegrees { get; }
        public double SpawnAngleDegrees { get; }
        public double SpawnPortalInsetUnits { get; }

        public double Level1SpeedMultiplier { get; }
        public double Level2SpeedMultiplier { get; }
        public double Level3SpeedMultiplier { get; }
        public double Level1BlockPostureMultiplier { get; }
        public double Level2BlockPostureMultiplier { get; }
        public double Level3BlockPostureMultiplier { get; }

        public ProjectileTuning(
            int maximumActiveCount = 6,
            double spawnIntervalSeconds = 15.0,
            double spawnWarningSeconds = 3.0,
            double baseSpeedUnitsPerSecond = 24.0,
            double powerDecaySecondsPerLevel = 1.0,
            double maximumParrySteeringDegrees = 30.0,
            double level1SpeedMultiplier = 1.15,
            double level2SpeedMultiplier = 1.30,
            double level3SpeedMultiplier = 1.45,
            double level1BlockPostureMultiplier = 1.25,
            double level2BlockPostureMultiplier = 1.50,
            double level3BlockPostureMultiplier = 2.0,
            double spawnAngleDegrees = 45.0,
            double spawnPortalInsetUnits = 2.0)
        {
            if (maximumActiveCount < InitialProjectileCount || maximumActiveCount > 64)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumActiveCount), "Projectile cap must be between one and 64.");
            }

            MaximumActiveCount = maximumActiveCount;
            SpawnIntervalSeconds = TuningValidation.InRange(spawnIntervalSeconds, nameof(spawnIntervalSeconds), 0.1, 3600.0);
            SpawnWarningSeconds = TuningValidation.InRange(spawnWarningSeconds, nameof(spawnWarningSeconds), 0.0, SpawnIntervalSeconds);
            BaseSpeedUnitsPerSecond = TuningValidation.InRange(baseSpeedUnitsPerSecond, nameof(baseSpeedUnitsPerSecond), 0.1, 1000.0);
            PowerDecaySecondsPerLevel = TuningValidation.InRange(powerDecaySecondsPerLevel, nameof(powerDecaySecondsPerLevel), 0.01, 60.0);
            MaximumParrySteeringDegrees = TuningValidation.InRange(maximumParrySteeringDegrees, nameof(maximumParrySteeringDegrees), 0.0, 180.0);
            SpawnAngleDegrees = TuningValidation.InRange(spawnAngleDegrees, nameof(spawnAngleDegrees), 1.0, 89.0);
            SpawnPortalInsetUnits = TuningValidation.InRange(spawnPortalInsetUnits, nameof(spawnPortalInsetUnits), 0.1, 10.0);

            Level1SpeedMultiplier = TuningValidation.InRange(level1SpeedMultiplier, nameof(level1SpeedMultiplier), 1.0, 10.0);
            Level2SpeedMultiplier = TuningValidation.InRange(level2SpeedMultiplier, nameof(level2SpeedMultiplier), Level1SpeedMultiplier, 10.0);
            Level3SpeedMultiplier = TuningValidation.InRange(level3SpeedMultiplier, nameof(level3SpeedMultiplier), Level2SpeedMultiplier, 10.0);
            Level1BlockPostureMultiplier = TuningValidation.InRange(level1BlockPostureMultiplier, nameof(level1BlockPostureMultiplier), 1.0, 10.0);
            Level2BlockPostureMultiplier = TuningValidation.InRange(level2BlockPostureMultiplier, nameof(level2BlockPostureMultiplier), Level1BlockPostureMultiplier, 10.0);
            Level3BlockPostureMultiplier = TuningValidation.InRange(level3BlockPostureMultiplier, nameof(level3BlockPostureMultiplier), Level2BlockPostureMultiplier, 10.0);
        }

        public double SpeedMultiplierAtLevel(int level)
        {
            switch (level)
            {
                case 0: return 1.0;
                case 1: return Level1SpeedMultiplier;
                case 2: return Level2SpeedMultiplier;
                case 3: return Level3SpeedMultiplier;
                default: throw new ArgumentOutOfRangeException(nameof(level));
            }
        }

        public double BlockPostureMultiplierAtLevel(int level)
        {
            switch (level)
            {
                case 0: return 1.0;
                case 1: return Level1BlockPostureMultiplier;
                case 2: return Level2BlockPostureMultiplier;
                case 3: return Level3BlockPostureMultiplier;
                default: throw new ArgumentOutOfRangeException(nameof(level));
            }
        }
    }

    public sealed class DefenseTuning
    {
        public double MaximumPosture { get; }
        public int BaseBlockPostureDamage { get; }
        public double GuardIdleRecoveryPerSecond { get; }
        public double UnguardedIdleRecoveryPerSecond { get; }
        public double HitRecoveryDelaySeconds { get; }
        public double GuardBreakDurationSeconds { get; }
        public int JumpBodyPostureDamage { get; }
        public int DashBodyPostureDamage { get; }
        public double GuardHoldThresholdSeconds { get; }
        public double WalkingBodyPushUnitsPerSecond { get; }
        public double JumpBodyKnockbackUnitsPerSecond { get; }
        public double DashBodyKnockbackUnitsPerSecond { get; }

        public DefenseTuning(
            double maximumPosture = 100.0,
            int baseBlockPostureDamage = 30,
            double guardIdleRecoveryPerSecond = 15.0,
            double unguardedIdleRecoveryPerSecond = 5.0,
            double hitRecoveryDelaySeconds = 0.25,
            double guardBreakDurationSeconds = 0.6,
            int jumpBodyPostureDamage = 15,
            int dashBodyPostureDamage = 25,
            double guardHoldThresholdSeconds = 0.10,
            double walkingBodyPushUnitsPerSecond = 3.0,
            double jumpBodyKnockbackUnitsPerSecond = 14.0,
            double dashBodyKnockbackUnitsPerSecond = 26.0)
        {
            MaximumPosture = TuningValidation.InRange(maximumPosture, nameof(maximumPosture), 1.0, 10000.0);
            if (baseBlockPostureDamage < 1 || baseBlockPostureDamage > 10000)
            {
                throw new ArgumentOutOfRangeException(nameof(baseBlockPostureDamage));
            }

            BaseBlockPostureDamage = baseBlockPostureDamage;
            GuardIdleRecoveryPerSecond = TuningValidation.InRange(guardIdleRecoveryPerSecond, nameof(guardIdleRecoveryPerSecond), 0.0, 1000.0);
            UnguardedIdleRecoveryPerSecond = TuningValidation.InRange(unguardedIdleRecoveryPerSecond, nameof(unguardedIdleRecoveryPerSecond), 0.0, 1000.0);
            if (GuardIdleRecoveryPerSecond <= UnguardedIdleRecoveryPerSecond)
            {
                throw new ArgumentOutOfRangeException(nameof(guardIdleRecoveryPerSecond), "Guard idle recovery must exceed unguarded idle recovery.");
            }

            HitRecoveryDelaySeconds = TuningValidation.InRange(hitRecoveryDelaySeconds, nameof(hitRecoveryDelaySeconds), 0.0, 60.0);
            GuardBreakDurationSeconds = TuningValidation.InRange(guardBreakDurationSeconds, nameof(guardBreakDurationSeconds), 0.01, 30.0);
            if (jumpBodyPostureDamage < 0 || jumpBodyPostureDamage > 10000)
            {
                throw new ArgumentOutOfRangeException(nameof(jumpBodyPostureDamage));
            }

            if (dashBodyPostureDamage < 0 || dashBodyPostureDamage > 10000)
            {
                throw new ArgumentOutOfRangeException(nameof(dashBodyPostureDamage));
            }

            JumpBodyPostureDamage = jumpBodyPostureDamage;
            DashBodyPostureDamage = dashBodyPostureDamage;
            GuardHoldThresholdSeconds = TuningValidation.InRange(guardHoldThresholdSeconds, nameof(guardHoldThresholdSeconds), 0.0, 1.0);
            WalkingBodyPushUnitsPerSecond = TuningValidation.InRange(walkingBodyPushUnitsPerSecond, nameof(walkingBodyPushUnitsPerSecond), 0.0, 100.0);
            JumpBodyKnockbackUnitsPerSecond = TuningValidation.InRange(jumpBodyKnockbackUnitsPerSecond, nameof(jumpBodyKnockbackUnitsPerSecond), 0.0, 100.0);
            DashBodyKnockbackUnitsPerSecond = TuningValidation.InRange(dashBodyKnockbackUnitsPerSecond, nameof(dashBodyKnockbackUnitsPerSecond), 0.0, 100.0);
        }
    }

    public sealed class DividerTuning
    {
        public double MaximumDeflectionUnits { get; }
        public double WalkingDeflectionUnits { get; }
        public double JumpingDeflectionUnits { get; }
        public double DashingDeflectionUnits { get; }
        public double DashReboundDistanceUnits { get; }
        public double SpringStrength { get; }
        public double Damping { get; }
        public double DashReboundSpeedUnitsPerSecond { get; }
        public double DeformationReachUnits { get; }

        public DividerTuning(
            double maximumDeflectionUnits = 8.0,
            double walkingDeflectionUnits = 3.5,
            double jumpingDeflectionUnits = 5.5,
            double dashingDeflectionUnits = 8.0,
            double dashReboundDistanceUnits = 18.0,
            double springStrength = 24.0,
            double damping = 8.0,
            double dashReboundSpeedUnitsPerSecond = 45.0,
            double deformationReachUnits = 14.0)
        {
            MaximumDeflectionUnits = TuningValidation.InRange(maximumDeflectionUnits, nameof(maximumDeflectionUnits), 0.0, 100.0);
            WalkingDeflectionUnits = TuningValidation.InRange(walkingDeflectionUnits, nameof(walkingDeflectionUnits), 0.0, MaximumDeflectionUnits);
            JumpingDeflectionUnits = TuningValidation.InRange(jumpingDeflectionUnits, nameof(jumpingDeflectionUnits), 0.0, MaximumDeflectionUnits);
            DashingDeflectionUnits = TuningValidation.InRange(dashingDeflectionUnits, nameof(dashingDeflectionUnits), 0.0, MaximumDeflectionUnits);
            DashReboundDistanceUnits = TuningValidation.InRange(dashReboundDistanceUnits, nameof(dashReboundDistanceUnits), 0.0, 1000.0);
            SpringStrength = TuningValidation.InRange(springStrength, nameof(springStrength), 0.0, 10000.0);
            Damping = TuningValidation.InRange(damping, nameof(damping), 0.0, 10000.0);
            DashReboundSpeedUnitsPerSecond = TuningValidation.InRange(dashReboundSpeedUnitsPerSecond, nameof(dashReboundSpeedUnitsPerSecond), 0.0, 1000.0);
            DeformationReachUnits = TuningValidation.InRange(deformationReachUnits, nameof(deformationReachUnits), 0.1, 100.0);
        }
    }

    public sealed class ArenaTuning
    {
        public double WidthUnits { get; }
        public double HeightUnits { get; }
        public double FighterRadiusUnits { get; }
        public double ProjectileRadiusUnits { get; }

        public ArenaTuning(
            double widthUnits = 100.0,
            double heightUnits = 56.25,
            double fighterRadiusUnits = 1.5,
            double projectileRadiusUnits = 0.6)
        {
            WidthUnits = TuningValidation.InRange(widthUnits, nameof(widthUnits), 10.0, 1000.0);
            HeightUnits = TuningValidation.InRange(heightUnits, nameof(heightUnits), 10.0, 1000.0);
            FighterRadiusUnits = TuningValidation.InRange(fighterRadiusUnits, nameof(fighterRadiusUnits), 0.1, Math.Min(WidthUnits, HeightUnits) / 4.0);
            ProjectileRadiusUnits = TuningValidation.InRange(projectileRadiusUnits, nameof(projectileRadiusUnits), 0.01, FighterRadiusUnits);
        }
    }

    public sealed class MatchTuning
    {
        public const int RoundWinsRequired = 2;
        public double RoundCountdownSeconds { get; }
        public double RoundResultSeconds { get; }
        public bool StartImmediatelyForTesting { get; }

        public MatchTuning(double roundCountdownSeconds = 3.0, double roundResultSeconds = 1.0,
            bool startImmediatelyForTesting = false)
        {
            RoundCountdownSeconds = TuningValidation.InRange(roundCountdownSeconds, nameof(roundCountdownSeconds), 0.0, 10.0);
            RoundResultSeconds = TuningValidation.InRange(roundResultSeconds, nameof(roundResultSeconds), 0.0, 10.0);
            StartImmediatelyForTesting = startImmediatelyForTesting;
        }
    }

    internal static class TuningValidation
    {
        public static double InRange(double value, string parameterName, double minimum, double maximum)
        {
            if (!IsFinite(value) || value < minimum || value > maximum)
            {
                throw new ArgumentOutOfRangeException(parameterName, "Value must be finite and within the supported range.");
            }

            return value;
        }

        public static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
