using System;

namespace Game.Core
{
    /// <summary>Validated values shared by the authoritative fixed-step simulation.</summary>
    public sealed class SimulationConfig
    {
        public int TickRateHz { get; }
        public int MaxCatchUpTicks { get; }

        public double TickDurationSeconds => 1.0 / TickRateHz;

        public SimulationConfig(int tickRateHz = 60, int maxCatchUpTicks = 8)
        {
            if (tickRateHz <= 0 || tickRateHz > 1000)
            {
                throw new ArgumentOutOfRangeException(nameof(tickRateHz), "Tick rate must be between 1 and 1000 Hz.");
            }

            if (maxCatchUpTicks <= 0 || maxCatchUpTicks > 1000)
            {
                throw new ArgumentOutOfRangeException(nameof(maxCatchUpTicks), "Catch-up tick limit must be between 1 and 1000.");
            }

            TickRateHz = tickRateHz;
            MaxCatchUpTicks = maxCatchUpTicks;
        }
    }
}
