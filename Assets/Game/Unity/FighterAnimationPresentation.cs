using System;
using System.Collections.Generic;
using Game.Core;

namespace Game.Unity
{
    public enum FighterAnimationPose { Idle, Run, Boost, Guard, Parry, Recoil }

    /// <summary>A cell in the four-column, six-row fighter sheet.</summary>
    public readonly struct FighterAnimationFrame
    {
        public FighterAnimationPose Pose { get; }
        public int Row { get; }
        public int Column { get; }

        public FighterAnimationFrame(FighterAnimationPose pose, int column)
        {
            Pose = pose;
            Row = (int)pose;
            Column = column;
        }
    }

    /// <summary>Cosmetic, tick-based pose selection; never changes authoritative state.</summary>
    public sealed class FighterAnimationPresentation
    {
        private const double RecoilSeconds = 0.24;
        private const double SuccessfulParrySeconds = 0.16;
        private const int FramesPerSecond = 12;
        private const int Columns = 4;
        private const double RunVelocityThreshold = 0.05;

        private readonly int _fighterId;
        private long _recoilStartTick;
        private long _recoilEndTick;
        private long _parryStartTick;
        private long _parryEndTick;

        public FighterAnimationPresentation(int fighterId)
        {
            if (fighterId != SimulationState.LeftFighterId && fighterId != SimulationState.RightFighterId)
                throw new ArgumentOutOfRangeException(nameof(fighterId));
            _fighterId = fighterId;
            Reset();
        }

        public void Reset()
        {
            _recoilStartTick = _parryStartTick = -1;
            _recoilEndTick = _parryEndTick = -1;
        }

        public void Observe(IReadOnlyList<SimulationEvent> events, int tickRate)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));
            if (tickRate <= 0) throw new ArgumentOutOfRangeException(nameof(tickRate));

            for (int i = 0; i < events.Count; i++)
            {
                SimulationEvent item = events[i];
                if (item.Type == SimulationEventType.RoundStarted)
                {
                    Reset();
                    continue;
                }
                if (item.Type == SimulationEventType.PerfectParry && item.TargetId == _fighterId)
                    Extend(ref _parryStartTick, ref _parryEndTick, item.Tick,
                        DurationTicks(SuccessfulParrySeconds, tickRate));
                else if ((item.Type == SimulationEventType.BodyImpact && item.TargetId == _fighterId) ||
                         (item.Type == SimulationEventType.DividerRebound && item.EntityId == _fighterId) ||
                         (item.Type == SimulationEventType.FloorHazardHit && item.EntityId == _fighterId) ||
                         (item.Type == SimulationEventType.GuardBroken && item.EntityId == _fighterId))
                    Extend(ref _recoilStartTick, ref _recoilEndTick, item.Tick,
                        DurationTicks(RecoilSeconds, tickRate));
            }
        }

        public FighterAnimationFrame Sample(FighterState fighter, long tick, int tickRate,
            bool useFuelBoost, double guardHoldSeconds)
        {
            if (fighter == null) throw new ArgumentNullException(nameof(fighter));
            if (fighter.Id != _fighterId) throw new ArgumentException("Fighter ID does not match selector.", nameof(fighter));
            if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
            if (tickRate <= 0) throw new ArgumentOutOfRangeException(nameof(tickRate));
            if (double.IsNaN(guardHoldSeconds) || double.IsInfinity(guardHoldSeconds) || guardHoldSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(guardHoldSeconds));

            // Defeat has no sheet row yet. Hold the last recoil cell until a defeat asset exists.
            if (fighter.Eliminated) return new FighterAnimationFrame(FighterAnimationPose.Recoil, Columns - 1);
            if (fighter.GuardBreakTicksRemaining > 0 || fighter.DividerReboundDistanceRemaining > 0 ||
                InWindow(tick, _recoilStartTick, _recoilEndTick))
                return new FighterAnimationFrame(FighterAnimationPose.Recoil,
                    FrameColumn(tick, tickRate,
                        InWindow(tick, _recoilStartTick, _recoilEndTick) ? _recoilStartTick : -1));
            if (fighter.ParryTicksRemaining > 0 || InWindow(tick, _parryStartTick, _parryEndTick))
                return new FighterAnimationFrame(FighterAnimationPose.Parry,
                    FrameColumn(tick, tickRate,
                        InWindow(tick, _parryStartTick, _parryEndTick) ? _parryStartTick : -1));
            if (useFuelBoost ? fighter.FuelBoostActive :
                fighter.DashTicksRemaining > 0 || fighter.DividerLoadTicksRemaining > 0)
                return new FighterAnimationFrame(FighterAnimationPose.Boost, FrameColumn(tick, tickRate));
            if (fighter.DefenseHeldPrevious && fighter.DefenseHoldTicks >= DurationTicks(guardHoldSeconds, tickRate))
                return new FighterAnimationFrame(FighterAnimationPose.Guard, FrameColumn(tick, tickRate));
            // The current sheet has no jump row. Ascending uses boost cell 0, descending cell 2.
            if (!fighter.Grounded)
                return new FighterAnimationFrame(FighterAnimationPose.Boost, fighter.Velocity.Y < 0 ? 2 : 0);
            if (Math.Abs(fighter.Velocity.X) > RunVelocityThreshold)
                return new FighterAnimationFrame(FighterAnimationPose.Run, FrameColumn(tick, tickRate));
            return new FighterAnimationFrame(FighterAnimationPose.Idle, FrameColumn(tick, tickRate));
        }

        private static bool InWindow(long tick, long start, long end) => start >= 0 && tick >= start && tick < end;

        private static int DurationTicks(double seconds, int tickRate) =>
            Math.Max(1, SimMath.SecondsToTicks(seconds, tickRate));

        private static int FrameColumn(long tick, int tickRate, long start = -1)
        {
            long elapsed = start >= 0 && tick >= start ? tick - start : tick;
            return (int)((elapsed * FramesPerSecond / tickRate) % Columns);
        }

        private static void Extend(ref long start, ref long end, long tick, int duration)
        {
            if (tick < start) return;
            start = tick;
            end = tick + duration;
        }
    }
}
