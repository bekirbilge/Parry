using System;

namespace Game.Core
{
    /// <summary>Accumulates elapsed time and emits a bounded number of whole simulation ticks.</summary>
    public sealed class FixedTickClock
    {
        private readonly SimulationConfig _config;
        private double _accumulatorSeconds;

        public long Tick { get; private set; }
        public double InterpolationAlpha => (_accumulatorSeconds % _config.TickDurationSeconds) / _config.TickDurationSeconds;

        public FixedTickClock(SimulationConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>Emits a bounded number of fixed ticks without skipping accumulated simulation time.</summary>
        public int Advance(double elapsedSeconds, Action<long> onTick)
        {
            if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds) || elapsedSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds), "Elapsed time must be finite and non-negative.");
            }

            if (onTick == null)
            {
                throw new ArgumentNullException(nameof(onTick));
            }

            _accumulatorSeconds += elapsedSeconds;
            int emitted = 0;
            double tickDuration = _config.TickDurationSeconds;
            double roundingTolerance = tickDuration * 1e-9;
            while (_accumulatorSeconds + roundingTolerance >= tickDuration && emitted < _config.MaxCatchUpTicks)
            {
                _accumulatorSeconds -= tickDuration;
                if (_accumulatorSeconds < 0 && _accumulatorSeconds >= -roundingTolerance)
                {
                    _accumulatorSeconds = 0;
                }

                Tick++;
                emitted++;
                onTick(Tick);
            }

            return emitted;
        }
    }
}
