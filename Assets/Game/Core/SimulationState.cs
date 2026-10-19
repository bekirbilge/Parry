using System;
using System.Collections.Generic;

namespace Game.Core
{
    public enum MatchPhase { Countdown, Active, RoundResult, MatchComplete }

    /// <summary>Mutable authoritative state. Fighter IDs 0 and 1 never change.</summary>
    public sealed class FighterState
    {
        public int Id;
        public Vec2 Position;
        public Vec2 TickStartPosition;
        public Vec2 PreviousPosition;
        public double MotionStartFraction;
        public Vec2 Velocity;
        public Vec2 DashDirection;
        public int FacingSign;
        public bool Grounded;
        public bool JumpHeldPrevious;
        public int JumpBufferTicksRemaining;
        public int CoyoteTicksRemaining;
        public int AirSequence;
        public int LastJumpBodyHitAirSequence = -1;
        public int DashCharges;
        public int DashRechargeTicksRemaining;
        public int DashTicksRemaining;
        public int DashInvulnerabilityTicksRemaining;
        public int DashSequence;
        public int LastDashBodyHitSequence = -1;
        public double DividerReboundDistanceRemaining;
        public bool DefenseHeldPrevious;
        public int DefenseHoldTicks;
        public int ParryTicksRemaining;
        public int GuardBreakTicksRemaining;
        public int PostureRecoveryDelayTicksRemaining;
        public double Posture;
        public long PostureTouchedTick = -1;
        public bool Eliminated;
        public int RoundWins;

        public FighterState Clone() => (FighterState)MemberwiseClone();
    }

    public sealed class ProjectileState
    {
        public int Id;
        public Vec2 Position;
        public Vec2 PreviousPosition;
        public Vec2 Velocity;
        public int PowerLevel;
        public int DecayTicksRemaining;
        public long LastContactTick = -1;
        public int LastContactFighterId = -1;
        public int ReflectedContactMask;

        public ProjectileState Clone() => (ProjectileState)MemberwiseClone();
    }

    /// <summary>Positive deflection moves the membrane toward fighter 1's half.</summary>
    public sealed class DividerState
    {
        public double[] NodeDeflections = new double[DividerSystem.RopePointCount];
        public double[] NodeVelocities = new double[DividerSystem.RopePointCount];
        public double LeftPressure;
        public double RightPressure;

        public DividerState Clone()
        {
            var clone = (DividerState)MemberwiseClone();
            clone.NodeDeflections = (double[])NodeDeflections.Clone();
            clone.NodeVelocities = (double[])NodeVelocities.Clone();
            return clone;
        }
    }

    /// <summary>Versioned, deep-clonable state for same-runtime replay comparison.</summary>
    public sealed class SimulationState
    {
        public const int SnapshotVersion = 2;
        public const int LeftFighterId = 0;
        public const int RightFighterId = 1;

        public long Tick;
        public MatchPhase Phase = MatchPhase.Countdown;
        public int PhaseTicksRemaining;
        public int RoundNumber = 1;
        public int DrawCount;
        public int RoundWinnerId = -1;
        public int MatchWinnerId = -1;
        public long NextSpawnTick;
        public bool SpawnWarningIssued;
        public int SpawnSequence;
        public int NextProjectileId;
        public long LastBodyContactTick = -1;
        public FighterState[] Fighters =
        {
            new FighterState { Id = LeftFighterId, FacingSign = 1 },
            new FighterState { Id = RightFighterId, FacingSign = -1 }
        };
        public List<ProjectileState> Projectiles = new List<ProjectileState>();
        public DividerState Divider = new DividerState();

        public FighterState Fighter(int id)
        {
            if (id != LeftFighterId && id != RightFighterId) throw new ArgumentOutOfRangeException(nameof(id));
            return Fighters[id];
        }

        public SimulationState Clone()
        {
            var clone = (SimulationState)MemberwiseClone();
            clone.Fighters = new[] { Fighters[0].Clone(), Fighters[1].Clone() };
            clone.Projectiles = new List<ProjectileState>(Projectiles.Count);
            foreach (ProjectileState projectile in Projectiles) clone.Projectiles.Add(projectile.Clone());
            clone.Divider = Divider.Clone();
            return clone;
        }

        /// <summary>FNV-1a over state fields and IEEE-754 bits; only same-runtime repeatability is asserted.</summary>
        public ulong ComputeChecksum()
        {
            var hash = new StateHash(14695981039346656037UL);
            hash.Add(SnapshotVersion);
            hash.Add(Tick);
            hash.Add((int)Phase);
            hash.Add(PhaseTicksRemaining);
            hash.Add(RoundNumber);
            hash.Add(DrawCount);
            hash.Add(RoundWinnerId);
            hash.Add(MatchWinnerId);
            hash.Add(NextSpawnTick);
            hash.Add(SpawnWarningIssued);
            hash.Add(SpawnSequence);
            hash.Add(NextProjectileId);
            hash.Add(LastBodyContactTick);
            hash.Add(Fighters.Length);
            foreach (FighterState fighter in Fighters)
            {
                hash.Add(fighter.Id);
                hash.Add(fighter.Position);
                hash.Add(fighter.TickStartPosition);
                hash.Add(fighter.PreviousPosition);
                hash.Add(fighter.MotionStartFraction);
                hash.Add(fighter.Velocity);
                hash.Add(fighter.DashDirection);
                hash.Add(fighter.FacingSign);
                hash.Add(fighter.Grounded);
                hash.Add(fighter.JumpHeldPrevious);
                hash.Add(fighter.JumpBufferTicksRemaining);
                hash.Add(fighter.CoyoteTicksRemaining);
                hash.Add(fighter.AirSequence);
                hash.Add(fighter.LastJumpBodyHitAirSequence);
                hash.Add(fighter.DashCharges);
                hash.Add(fighter.DashRechargeTicksRemaining);
                hash.Add(fighter.DashTicksRemaining);
                hash.Add(fighter.DashInvulnerabilityTicksRemaining);
                hash.Add(fighter.DashSequence);
                hash.Add(fighter.LastDashBodyHitSequence);
                hash.Add(fighter.DividerReboundDistanceRemaining);
                hash.Add(fighter.DefenseHeldPrevious);
                hash.Add(fighter.DefenseHoldTicks);
                hash.Add(fighter.ParryTicksRemaining);
                hash.Add(fighter.GuardBreakTicksRemaining);
                hash.Add(fighter.PostureRecoveryDelayTicksRemaining);
                hash.Add(fighter.Posture);
                hash.Add(fighter.PostureTouchedTick);
                hash.Add(fighter.Eliminated);
                hash.Add(fighter.RoundWins);
            }

            foreach (double deflection in Divider.NodeDeflections) hash.Add(deflection);
            foreach (double velocity in Divider.NodeVelocities) hash.Add(velocity);
            hash.Add(Divider.LeftPressure);
            hash.Add(Divider.RightPressure);
            hash.Add(Projectiles.Count);
            var orderedProjectiles = Projectiles.ToArray();
            Array.Sort(orderedProjectiles, (a, b) => a.Id.CompareTo(b.Id));
            foreach (ProjectileState projectile in orderedProjectiles)
            {
                hash.Add(projectile.Id);
                hash.Add(projectile.Position);
                hash.Add(projectile.PreviousPosition);
                hash.Add(projectile.Velocity);
                hash.Add(projectile.PowerLevel);
                hash.Add(projectile.DecayTicksRemaining);
                hash.Add(projectile.LastContactTick);
                hash.Add(projectile.LastContactFighterId);
                hash.Add(projectile.ReflectedContactMask);
            }

            return hash.Value;
        }

        private struct StateHash
        {
            private ulong _value;
            public ulong Value => _value;

            public StateHash(ulong seed) { _value = seed; }

            public void Add(bool value) => Add(value ? 1 : 0);
            public void Add(double value) => Add(BitConverter.DoubleToInt64Bits(value));
            public void Add(Vec2 value) { Add(value.X); Add(value.Y); }
            public void Add(int value) => Add((long)value);

            public void Add(long value)
            {
                unchecked
                {
                    ulong bits = (ulong)value;
                    for (int i = 0; i < 8; i++)
                    {
                        _value ^= (byte)(bits >> (i * 8));
                        _value *= 1099511628211UL;
                    }
                }
            }
        }
    }
}
