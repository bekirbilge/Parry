using System;
using Game.Core;

namespace Game.Unity
{
    public static class FloorFirePresentation
    {
        /// <summary>Keep active flames visible on the frozen round-result frame without advancing hazards.</summary>
        public static FloorHazardFrame DisplayFrame(SimulationState state, CombatTuning tuning,
            SimulationConfig config)
        {
            var frame = FloorHazardSystem.Sample(state, tuning, config);
            if (state.Phase != MatchPhase.RoundResult && state.Phase != MatchPhase.MatchComplete) return frame;
            var frozen = state.Clone();
            frozen.Phase = MatchPhase.Active;
            frozen.FloorHazard.ElapsedActiveTicks = Math.Max(0, frozen.FloorHazard.ElapsedActiveTicks - 1);
            var last = FloorHazardSystem.Sample(frozen, tuning, config);
            return last.Phase == FloorHazardPhase.Active ? last : frame;
        }

        /// <summary>Two bright pulses in the warning interval; no repeated time-based blinking.</summary>
        public static bool WarningFlashLit(int ticksRemaining, int warningTicks)
        {
            if (warningTicks <= 0 || ticksRemaining <= 0 || ticksRemaining > warningTicks) return false;
            int elapsed = warningTicks - ticksRemaining;
            int half = Math.Max(1, warningTicks / 2);
            int pulse = Math.Max(1, (int)Math.Ceiling(half * .4));
            return elapsed < 2 * half && elapsed % half < pulse;
        }
    }
}
