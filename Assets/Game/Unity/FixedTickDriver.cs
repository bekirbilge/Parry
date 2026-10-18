using System;
using Game.Core;
using UnityEngine;

namespace Game.Unity
{
    /// <summary>Runs one authoritative core tick for each Unity fixed update.</summary>
    public sealed class FixedTickDriver : MonoBehaviour
    {
        private FixedTickClock _clock;
        private double _tickDurationSeconds;

        /// <summary>Raised after the core clock advances one tick.</summary>
        public event Action<long> TickAdvanced;

        /// <summary>The number of core ticks completed since this driver awoke.</summary>
        public long Tick => _clock?.Tick ?? 0;

        /// <summary>The fractional core tick remaining for render interpolation.</summary>
        public double InterpolationAlpha => _clock?.InterpolationAlpha ?? 0;

        private void Awake()
        {
            var config = new SimulationConfig();
            _tickDurationSeconds = config.TickDurationSeconds;
            Time.fixedDeltaTime = (float)_tickDurationSeconds;
            _clock = new FixedTickClock(config);
        }

        private void FixedUpdate()
        {
            // Use the exact core duration, avoiding float conversion drift across fixed steps.
            _clock.Advance(_tickDurationSeconds, OnTick);
        }

        private void OnTick(long tick)
        {
            TickAdvanced?.Invoke(tick);
        }
    }
}
