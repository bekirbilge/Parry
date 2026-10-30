using System;
using System.Collections.Generic;
using Game.Core;

namespace Game.Unity
{
    /// <summary>Short-lived visual history sampled after an authoritative step.</summary>
    public sealed class CombatEffectsPresentation
    {
        public const int MaximumTrailPoints = 6;
        public const int MaximumProjectiles = 64;
        public const int MaximumFlashes = 8;
        public const double FlashDurationSeconds = .18;

        public readonly struct ParryFlash
        {
            public readonly Vec2 Position;
            public readonly int FighterId;
            public readonly double Strength;
            public readonly long Tick;

            public ParryFlash(Vec2 position, int fighterId, double strength, long tick)
            {
                Position = position;
                FighterId = fighterId;
                Strength = strength;
                Tick = tick;
            }
        }

        private readonly List<ParryFlash> _flashes = new List<ParryFlash>(MaximumFlashes);
        private readonly IReadOnlyList<ParryFlash> _visibleFlashes;
        private readonly Dictionary<int, List<Vec2>> _trails = new Dictionary<int, List<Vec2>>();
        private readonly Dictionary<int, IReadOnlyList<Vec2>> _visibleTrails =
            new Dictionary<int, IReadOnlyList<Vec2>>();
        private readonly List<int> _removedIds = new List<int>();
        private static readonly IReadOnlyList<Vec2> EmptyTrail = Array.Empty<Vec2>();
        private long _lastTick = -1;
        private int _lastRound = -1;

        public CombatEffectsPresentation()
        {
            _visibleFlashes = _flashes.AsReadOnly();
        }

        public IReadOnlyList<ParryFlash> Flashes => _visibleFlashes;

        public IReadOnlyList<Vec2> ProjectileTrail(int projectileId)
        {
            IReadOnlyList<Vec2> trail;
            return _visibleTrails.TryGetValue(projectileId, out trail) ? trail : EmptyTrail;
        }

        public void Reset()
        {
            _flashes.Clear();
            _trails.Clear();
            _visibleTrails.Clear();
            _removedIds.Clear();
            _lastTick = -1;
            _lastRound = -1;
        }

        public void Observe(SimulationState state, IReadOnlyList<SimulationEvent> events, int tickRate)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (events == null) throw new ArgumentNullException(nameof(events));
            if (tickRate <= 0) throw new ArgumentOutOfRangeException(nameof(tickRate));

            bool roundStarted = false;
            for (int i = 0; i < events.Count; i++)
                if (events[i].Type == SimulationEventType.RoundStarted) roundStarted = true;

            if (state.Tick < _lastTick || state.RoundNumber != _lastRound)
                Reset();
            if (state.Tick == _lastTick) return;
            if (roundStarted)
            {
                _flashes.Clear();
                _trails.Clear();
                _visibleTrails.Clear();
            }

            int lifetime = Math.Max(1, SimMath.SecondsToTicks(FlashDurationSeconds, tickRate));
            _flashes.RemoveAll(flash => state.Tick < flash.Tick || state.Tick - flash.Tick >= lifetime);
            for (int i = 0; i < events.Count; i++)
            {
                SimulationEvent item = events[i];
                if (item.Type != SimulationEventType.PerfectParry) continue;
                if (_flashes.Count == MaximumFlashes) _flashes.RemoveAt(0);
                _flashes.Add(new ParryFlash(item.Position, item.TargetId, 1.0, item.Tick));
            }

            _removedIds.Clear();
            foreach (int id in _trails.Keys)
            {
                bool stillLive = false;
                for (int i = 0; i < state.Projectiles.Count; i++)
                    if (state.Projectiles[i].Id == id) { stillLive = true; break; }
                if (!stillLive) _removedIds.Add(id);
            }
            for (int i = 0; i < _removedIds.Count; i++)
            {
                _trails.Remove(_removedIds[i]);
                _visibleTrails.Remove(_removedIds[i]);
            }

            for (int i = 0; i < state.Projectiles.Count; i++)
            {
                ProjectileState projectile = state.Projectiles[i];
                List<Vec2> trail;
                if (!_trails.TryGetValue(projectile.Id, out trail))
                {
                    if (_trails.Count == MaximumProjectiles) continue;
                    trail = new List<Vec2>(MaximumTrailPoints);
                    _trails.Add(projectile.Id, trail);
                    _visibleTrails.Add(projectile.Id, trail.AsReadOnly());
                }
                if (trail.Count == MaximumTrailPoints) trail.RemoveAt(0);
                trail.Add(projectile.Position);
            }

            _lastTick = state.Tick;
            _lastRound = state.RoundNumber;
        }

        public float FlashOpacity(ParryFlash flash, long currentTick, int tickRate)
        {
            if (tickRate <= 0) throw new ArgumentOutOfRangeException(nameof(tickRate));
            long age = currentTick - flash.Tick;
            if (age < 0) return 0f;
            int lifetime = Math.Max(1, SimMath.SecondsToTicks(FlashDurationSeconds, tickRate));
            return age >= lifetime ? 0f : (float)(flash.Strength * (1.0 - (double)age / lifetime));
        }
    }
}
