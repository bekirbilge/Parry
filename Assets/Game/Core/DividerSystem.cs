#nullable enable
using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>Shared damped membrane with explicit constraints for each fighter's half.</summary>
    public static class DividerSystem
    {
        public static void AdvanceAndConstrain(SimulationState state, CombatTuning tuning,
            SimulationConfig config, IList<SimulationEvent> events)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (tuning == null) throw new ArgumentNullException(nameof(tuning));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (events == null) throw new ArgumentNullException(nameof(events));

            DividerState divider = state.Divider;
            FighterState left = state.Fighter(SimulationState.LeftFighterId);
            FighterState right = state.Fighter(SimulationState.RightFighterId);
            divider.LeftPressure = Pressure(left, 1, state, tuning);
            divider.RightPressure = Pressure(right, -1, state, tuning);
            double target = SimMath.Clamp(divider.LeftPressure - divider.RightPressure,
                -tuning.Divider.MaximumDeflectionUnits, tuning.Divider.MaximumDeflectionUnits);

            // Small fixed substeps keep the spring bounded even with a low configured tick rate.
            int substeps = Math.Max(1, (int)Math.Ceiling(config.TickDurationSeconds * 120.0));
            double h = config.TickDurationSeconds / substeps;
            for (int i = 0; i < substeps; i++)
            {
                double acceleration = tuning.Divider.SpringStrength * (target - divider.Deflection) -
                    tuning.Divider.Damping * divider.Velocity;
                divider.Velocity += acceleration * h;
                divider.Deflection += divider.Velocity * h;
                double bounded = SimMath.Clamp(divider.Deflection,
                    -tuning.Divider.MaximumDeflectionUnits, tuning.Divider.MaximumDeflectionUnits);
                if (bounded != divider.Deflection)
                {
                    divider.Deflection = bounded;
                    if (Math.Sign(divider.Velocity) == Math.Sign(bounded)) divider.Velocity = 0;
                }
            }

            ConstrainFighter(left, state, tuning, events);
            ConstrainFighter(right, state, tuning, events);
        }

        /// <summary>Projects an endpoint to arena and membrane; safe for post-impact sweeps.</summary>
        public static void ConstrainFighter(FighterState fighter, SimulationState state, CombatTuning tuning)
        {
            ConstrainFighter(fighter, state, tuning, null);
        }

        private static void ConstrainFighter(FighterState fighter, SimulationState state,
            CombatTuning tuning, IList<SimulationEvent>? events)
        {
            double radius = tuning.Arena.FighterRadiusUnits;
            double center = tuning.Arena.WidthUnits / 2.0 + state.Divider.Deflection;
            double xMin = radius;
            double xMax = tuning.Arena.WidthUnits - radius;
            if (fighter.Id == SimulationState.LeftFighterId) xMax = Math.Min(xMax, center - radius);
            else xMin = Math.Max(xMin, center + radius);

            double oldX = fighter.Position.X;
            double x = SimMath.Clamp(oldX, xMin, xMax);
            double y = SimMath.Clamp(fighter.Position.Y, radius, tuning.Arena.HeightUnits - radius);
            fighter.Position = new Vec2(x, y);
            int inward = fighter.Id == SimulationState.LeftFighterId ? 1 : -1;
            bool touchedDivider = fighter.Id == SimulationState.LeftFighterId ? oldX > xMax : oldX < xMin;
            bool inwardDash = fighter.DashTicksRemaining > 0 &&
                fighter.DashDirection.X * inward > 1e-9;
            if (touchedDivider && inwardDash && fighter.DividerReboundDistanceRemaining <= 0)
            {
                // The membrane reverses horizontal momentum; diagonal vertical momentum survives.
                fighter.Velocity = new Vec2(-inward * tuning.Divider.DashReboundSpeedUnitsPerSecond,
                    fighter.Velocity.Y);
                fighter.DividerReboundDistanceRemaining = tuning.Divider.DashReboundDistanceUnits;
                events?.Add(new SimulationEvent(state.Tick, SimulationEventType.DividerRebound,
                    fighter.Id, -1, fighter.Position, fighter.DividerReboundDistanceRemaining));
            }
            else if (touchedDivider && fighter.Velocity.X * inward > 0)
            {
                fighter.Velocity = new Vec2(0, fighter.Velocity.Y);
            }

            if ((x <= radius && fighter.Velocity.X < 0) ||
                (x >= tuning.Arena.WidthUnits - radius && fighter.Velocity.X > 0))
                fighter.Velocity = new Vec2(0, fighter.Velocity.Y);
            if ((y <= radius && fighter.Velocity.Y < 0) ||
                (y >= tuning.Arena.HeightUnits - radius && fighter.Velocity.Y > 0))
                fighter.Velocity = new Vec2(fighter.Velocity.X, 0);
        }

        private static double Pressure(FighterState fighter, int inward,
            SimulationState state, CombatTuning tuning)
        {
            if (fighter.Eliminated || fighter.Velocity.X * inward <= 0) return 0;
            double radius = tuning.Arena.FighterRadiusUnits;
            double center = tuning.Arena.WidthUnits / 2.0 + state.Divider.Deflection;
            double toward = (fighter.Position.X - center) * inward;
            if (toward < -radius - fighter.Velocity.X * inward * 0.02) return 0;
            if (fighter.DashTicksRemaining > 0 && fighter.DashDirection.X * inward > 0)
                return tuning.Divider.DashingDeflectionUnits;
            return fighter.Grounded ? tuning.Divider.WalkingDeflectionUnits :
                tuning.Divider.JumpingDeflectionUnits;
        }
    }
}
