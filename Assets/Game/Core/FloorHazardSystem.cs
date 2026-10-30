using System;
using System.Collections.Generic;

namespace Game.Core
{
    public enum FloorHazardPhase { Dormant, Warning, Active }

    public sealed class FloorHazardTuning
    {
        public bool Enabled { get; }
        public double WarningSeconds { get; }
        public double ActiveSeconds { get; }
        public double IntervalSeconds { get; }
        public double InitialDelaySeconds { get; }
        public double PatchWidthFraction { get; }
        public double HeightUnits { get; }

        public FloorHazardTuning(bool enabled = false, double warningSeconds = 2,
            double activeSeconds = 1.25, double intervalSeconds = 10,
            double initialDelaySeconds = 6, double patchWidthFraction = 0.24,
            double heightUnits = 1.5)
        {
            Enabled = enabled;
            WarningSeconds = TuningValidation.InRange(warningSeconds, nameof(warningSeconds), 0.001, 60);
            ActiveSeconds = TuningValidation.InRange(activeSeconds, nameof(activeSeconds), 0.001, 60);
            IntervalSeconds = TuningValidation.InRange(intervalSeconds, nameof(intervalSeconds), 0.001, 120);
            InitialDelaySeconds = TuningValidation.InRange(initialDelaySeconds, nameof(initialDelaySeconds), 0, 120);
            PatchWidthFraction = TuningValidation.InRange(patchWidthFraction, nameof(patchWidthFraction), 0.001, 1);
            HeightUnits = TuningValidation.InRange(heightUnits, nameof(heightUnits), 0.001, 100);
            if (warningSeconds + activeSeconds > intervalSeconds)
                throw new ArgumentOutOfRangeException(nameof(intervalSeconds), "Warning and active time must fit between warning starts.");
        }
    }

    public sealed class FloorHazardState
    {
        public long ElapsedActiveTicks;
        public long[] LastHitCycle = { -1, -1 };

        public FloorHazardState Clone() => new FloorHazardState
        {
            ElapsedActiveTicks = ElapsedActiveTicks,
            LastHitCycle = (long[])LastHitCycle.Clone()
        };
    }

    public readonly struct FloorHazardFrame
    {
        public FloorHazardPhase Phase { get; }
        public long CycleIndex { get; }
        public double LeftCenterX { get; }
        public double RightCenterX { get; }
        public double HalfWidth { get; }
        public double HeightUnits { get; }
        public int TicksRemaining { get; }

        public FloorHazardFrame(FloorHazardPhase phase, long cycleIndex,
            double leftCenterX, double rightCenterX, double halfWidth,
            double heightUnits, int ticksRemaining)
        {
            Phase = phase;
            CycleIndex = cycleIndex;
            LeftCenterX = leftCenterX;
            RightCenterX = rightCenterX;
            HalfWidth = halfWidth;
            HeightUnits = heightUnits;
            TicksRemaining = ticksRemaining;
        }
    }

    public static class FloorHazardSystem
    {
        public static FloorHazardFrame Sample(SimulationState state, CombatTuning tuning, SimulationConfig config)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (tuning == null) throw new ArgumentNullException(nameof(tuning));
            if (config == null) throw new ArgumentNullException(nameof(config));
            FloorHazardTuning settings = tuning.FloorHazard;
            double halfArena = tuning.Arena.WidthUnits / 2;
            if (!settings.Enabled || state.Phase != MatchPhase.Active)
                return new FloorHazardFrame(FloorHazardPhase.Dormant, -1,
                    halfArena * 0.5, halfArena * 1.5,
                    halfArena * settings.PatchWidthFraction / 2, settings.HeightUnits, 0);

            int delay = SimMath.SecondsToTicks(settings.InitialDelaySeconds, config.TickRateHz);
            int warning = SimMath.SecondsToTicks(settings.WarningSeconds, config.TickRateHz);
            int active = SimMath.SecondsToTicks(settings.ActiveSeconds, config.TickRateHz);
            int interval = Math.Max(SimMath.SecondsToTicks(settings.IntervalSeconds, config.TickRateHz),
                warning + active);
            long elapsed = state.FloorHazard.ElapsedActiveTicks;
            if (elapsed < delay)
                return new FloorHazardFrame(FloorHazardPhase.Dormant, -1,
                    halfArena * 0.5, halfArena * 1.5,
                    halfArena * settings.PatchWidthFraction / 2, settings.HeightUnits, (int)(delay - elapsed));

            long sinceFirst = elapsed - delay;
            long cycle = sinceFirst / interval;
            int inCycle = (int)(sinceFirst % interval);
            double fraction = (cycle % 5) switch
            {
                0 => 0.5,
                1 => 0.12,
                2 => 0.88,
                3 => 0.32,
                _ => 0.68
            };
            FloorHazardPhase phase = inCycle < warning ? FloorHazardPhase.Warning :
                inCycle < warning + active ? FloorHazardPhase.Active : FloorHazardPhase.Dormant;
            int remaining = phase == FloorHazardPhase.Warning ? warning - inCycle :
                phase == FloorHazardPhase.Active ? warning + active - inCycle : interval - inCycle;
            return new FloorHazardFrame(phase, cycle, halfArena * fraction,
                halfArena * (2 - fraction), halfArena * settings.PatchWidthFraction / 2,
                settings.HeightUnits, remaining);
        }

        public static void Apply(SimulationState state, SimulationInput input, CombatTuning tuning,
            SimulationConfig config, IList<SimulationEvent> events)
        {
            FloorHazardFrame frame = Sample(state, tuning, config);
            if (frame.Phase != FloorHazardPhase.Active) return;
            for (int id = 0; id < 2; id++)
            {
                FighterState fighter = state.Fighter(id);
                double radius = tuning.Arena.FighterRadiusUnits;
                bool overlapsPatch = IntersectsPatch(fighter.Position, radius, frame.LeftCenterX, frame) ||
                    IntersectsPatch(fighter.Position, radius, frame.RightCenterX, frame);
                if (fighter.Eliminated || state.FloorHazard.LastHitCycle[id] == frame.CycleIndex ||
                    !overlapsPatch) continue;

                state.FloorHazard.LastHitCycle[id] = frame.CycleIndex;
                fighter.Eliminated = true;
                events.Add(new SimulationEvent(state.Tick, SimulationEventType.FloorHazardHit,
                    id, position: fighter.Position));
                events.Add(new SimulationEvent(state.Tick, SimulationEventType.FighterEliminated,
                    id, -1, fighter.Position));
            }
        }

        private static bool IntersectsPatch(Vec2 position, double radius, double centerX,
            FloorHazardFrame frame)
        {
            double nearestX = SimMath.Clamp(position.X, centerX - frame.HalfWidth,
                centerX + frame.HalfWidth);
            double nearestY = SimMath.Clamp(position.Y, 0, frame.HeightUnits);
            double dx = position.X - nearestX;
            double dy = position.Y - nearestY;
            return dx * dx + dy * dy <= radius * radius + 1e-9;
        }
    }
}
