#nullable enable
using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>A pinned polyline whose interior nodes are bounded damped springs.</summary>
    public static class DividerSystem
    {
        // The first and last mobile nodes lie on the arena boundaries. Their
        // horizontal segments are visible hinges, allowing a grounded circle
        // to push the rope while the true endpoints remain pinned at X=50.
        public const int RopePointCount = 21;

        /// <summary>Authoritative collision/render point in arena units, bottom to top.</summary>
        public static Vec2 GetRopePoint(SimulationState state, CombatTuning tuning, int index)
        {
            if (index < 0 || index >= RopePointCount) throw new ArgumentOutOfRangeException(nameof(index));
            double y = index <= 1 ? 0 : index >= RopePointCount - 2 ? tuning.Arena.HeightUnits :
                tuning.Arena.FighterRadiusUnits + (index - 2) *
                (tuning.Arena.HeightUnits - 2 * tuning.Arena.FighterRadiusUnits) / (RopePointCount - 5);
            double x = tuning.Arena.WidthUnits * 0.5 +
                (index == 0 || index == RopePointCount - 1 ? 0 : state.Divider.NodeDeflections[index]);
            return new Vec2(x, y);
        }

        /// <summary>Interpolated X of the exact authoritative collision polyline.</summary>
        public static double CenterXAtHeight(SimulationState state, CombatTuning tuning, double y)
        {
            y = SimMath.Clamp(y, 0, tuning.Arena.HeightUnits);
            if (y == 0) return GetRopePoint(state, tuning, 1).X;
            if (y == tuning.Arena.HeightUnits)
                return GetRopePoint(state, tuning, RopePointCount - 2).X;
            for (int i = 1; i < RopePointCount; i++)
            {
                Vec2 upper = GetRopePoint(state, tuning, i);
                if (y > upper.Y) continue;
                Vec2 lower = GetRopePoint(state, tuning, i - 1);
                return lower.X + (upper.X - lower.X) * (y - lower.Y) / (upper.Y - lower.Y);
            }
            return tuning.Arena.WidthUnits * 0.5;
        }

        /// <summary>Exact X limit for a circular fighter against every rope segment.</summary>
        public static double FighterCenterLimitX(SimulationState state, CombatTuning tuning,
            int fighterId, double centerY)
        {
            if (fighterId != 0 && fighterId != 1) throw new ArgumentOutOfRangeException(nameof(fighterId));
            double radius = tuning.Arena.FighterRadiusUnits;
            double limit = fighterId == 0 ? double.PositiveInfinity : double.NegativeInfinity;
            for (int i = 1; i < RopePointCount; i++)
            {
                Vec2 a = GetRopePoint(state, tuning, i - 1);
                Vec2 b = GetRopePoint(state, tuning, i);
                // Boundary hinges are tangent to every legal fighter circle.
                if (b.Y == a.Y) continue;
                double low = Math.Max(a.Y, centerY - radius);
                double high = Math.Min(b.Y, centerY + radius);
                if (low > high) continue;
                double slope = (b.X - a.X) / (b.Y - a.Y);
                // Extremum of line X plus/minus the circle's horizontal extent.
                double stationary = centerY + (fighterId == 0 ? -1 : 1) *
                    slope * radius / Math.Sqrt(1 + slope * slope);
                double first = Evaluate(a, slope, centerY, radius, low, fighterId);
                double last = Evaluate(a, slope, centerY, radius, high, fighterId);
                double middle = Evaluate(a, slope, centerY, radius,
                    SimMath.Clamp(stationary, low, high), fighterId);
                if (fighterId == 0) limit = Math.Min(limit, Math.Min(first, Math.Min(last, middle)));
                else limit = Math.Max(limit, Math.Max(first, Math.Max(last, middle)));
            }
            return limit;
        }

        private static double Evaluate(Vec2 a, double slope, double centerY, double radius,
            double y, int fighterId)
        {
            double dy = y - centerY;
            double extent = Math.Sqrt(Math.Max(0, radius * radius - dy * dy));
            return a.X + slope * (y - a.Y) + (fighterId == 0 ? -extent : extent);
        }

        public static void AdvanceAndConstrain(SimulationState state, CombatTuning tuning,
            SimulationConfig config, IList<SimulationEvent> events)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (tuning == null) throw new ArgumentNullException(nameof(tuning));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (events == null) throw new ArgumentNullException(nameof(events));

            DividerState divider = state.Divider;
            divider.NodeDeflections[0] = divider.NodeDeflections[RopePointCount - 1] = 0;
            divider.NodeVelocities[0] = divider.NodeVelocities[RopePointCount - 1] = 0;
            FighterState left = state.Fighter(0);
            FighterState right = state.Fighter(1);
            divider.LeftPressure = Pressure(left, 0, state, tuning);
            divider.RightPressure = Pressure(right, 1, state, tuning);
            int substeps = Math.Max(1, (int)Math.Ceiling(config.TickDurationSeconds * 120.0));
            double h = config.TickDurationSeconds / substeps;
            for (int node = 1; node < RopePointCount - 1; node++)
            {
                double y = GetRopePoint(state, tuning, node).Y;
                double effectiveY = SimMath.Clamp(y, tuning.Arena.FighterRadiusUnits,
                    tuning.Arena.HeightUnits - tuning.Arena.FighterRadiusUnits);
                double target = divider.LeftPressure * Weight(effectiveY, left.Position.Y, tuning.Divider.DeformationReachUnits) -
                    divider.RightPressure * Weight(effectiveY, right.Position.Y, tuning.Divider.DeformationReachUnits);
                target = SimMath.Clamp(target, -tuning.Divider.MaximumDeflectionUnits,
                    tuning.Divider.MaximumDeflectionUnits);
                bool boostLoading = (left.DividerLoadTicksRemaining > 0 &&
                    Weight(effectiveY, left.Position.Y, tuning.Divider.DeformationReachUnits) > 0) ||
                    (right.DividerLoadTicksRemaining > 0 &&
                    Weight(effectiveY, right.Position.Y, tuning.Divider.DeformationReachUnits) > 0);
                double strength = boostLoading ? tuning.Divider.BoostLoadSpringStrength : tuning.Divider.SpringStrength;
                double damping = boostLoading ? tuning.Divider.BoostLoadDamping : tuning.Divider.Damping;
                for (int step = 0; step < substeps; step++)
                {
                    double acceleration = strength *
                        (target - divider.NodeDeflections[node]) - damping * divider.NodeVelocities[node];
                    divider.NodeVelocities[node] += acceleration * h;
                    divider.NodeDeflections[node] += divider.NodeVelocities[node] * h;
                    double bounded = SimMath.Clamp(divider.NodeDeflections[node],
                        -tuning.Divider.MaximumDeflectionUnits, tuning.Divider.MaximumDeflectionUnits);
                    if (bounded != divider.NodeDeflections[node])
                    {
                        divider.NodeDeflections[node] = bounded;
                        if (Math.Sign(divider.NodeVelocities[node]) == Math.Sign(bounded))
                            divider.NodeVelocities[node] = 0;
                    }
                }
            }
            ConstrainFighter(left, state, tuning, events, config);
            ConstrainFighter(right, state, tuning, events, config);
            AdvanceLoading(left, state, tuning, config, events);
            AdvanceLoading(right, state, tuning, config, events);
        }

        private static double Weight(double nodeY, double contactY, double reach)
        {
            return Math.Max(0, 1 - Math.Abs(nodeY - contactY) / reach);
        }

        /// <summary>Projects an endpoint to arena and rope; safe for post-impact sweeps.</summary>
        public static void ConstrainFighter(FighterState fighter, SimulationState state,
            CombatTuning tuning, SimulationConfig? config = null)
        {
            ConstrainFighter(fighter, state, tuning, null, config ?? new SimulationConfig());
        }

        private static void ConstrainFighter(FighterState fighter, SimulationState state,
            CombatTuning tuning, IList<SimulationEvent>? events, SimulationConfig config)
        {
            double radius = tuning.Arena.FighterRadiusUnits;
            double y = SimMath.Clamp(fighter.Position.Y, radius, tuning.Arena.HeightUnits - radius);
            double ropeLimit = FighterCenterLimitX(state, tuning, fighter.Id, y);
            double xMin = radius;
            double xMax = tuning.Arena.WidthUnits - radius;
            if (fighter.Id == 0) xMax = Math.Min(xMax, ropeLimit);
            else xMin = Math.Max(xMin, ropeLimit);
            double oldX = fighter.Position.X;
            double x = SimMath.Clamp(oldX, xMin, xMax);
            // The spring may move into a fighter between ticks. Depenetrate the
            // sweep start locally before testing the fighter's straight motion.
            Vec2 start = fighter.PreviousPosition;
            if (start.Y >= radius && start.Y <= tuning.Arena.HeightUnits - radius &&
                start.X >= radius && start.X <= tuning.Arena.WidthUnits - radius)
            {
                double startLimit = FighterCenterLimitX(state, tuning, fighter.Id, start.Y);
                double startX = fighter.Id == 0 ? Math.Min(start.X, startLimit) : Math.Max(start.X, startLimit);
                start = new Vec2(startX, start.Y);
                fighter.PreviousPosition = start;
                Vec2 sweptEnd = SweptCenterPosition(state, tuning, fighter.Id, start, new Vec2(x, y));
                x = sweptEnd.X;
                y = sweptEnd.Y;
            }
            fighter.Position = new Vec2(x, y);
            int inward = fighter.Id == 0 ? 1 : -1;
            bool touchedDivider = fighter.Id == 0 ? oldX > x + 1e-9 : oldX < x - 1e-9;
            bool inwardDash = fighter.DashTicksRemaining > 0 && fighter.DashDirection.X * inward > 1e-9;
            if (touchedDivider && inwardDash && fighter.GuardBreakTicksRemaining <= 0 &&
                fighter.DividerReboundDistanceRemaining <= 0 &&
                fighter.DashSequence > fighter.LastDividerLoadDashSequence)
            {
                fighter.LastDividerLoadDashSequence = fighter.DashSequence;
                // Include the contact tick; AdvanceLoading consumes one tick below.
                fighter.DividerLoadTicksRemaining = Math.Max(1,
                    SimMath.SecondsToTicks(tuning.Divider.BoostLoadSeconds, config.TickRateHz) + 1);
            }
            else if (touchedDivider && fighter.DividerLoadTicksRemaining <= 0 &&
                fighter.Velocity.X * inward > 0)
                fighter.Velocity = new Vec2(0, fighter.Velocity.Y);

            if ((x <= radius && fighter.Velocity.X < 0) ||
                (x >= tuning.Arena.WidthUnits - radius && fighter.Velocity.X > 0))
                fighter.Velocity = new Vec2(0, fighter.Velocity.Y);
            if ((y <= radius && fighter.Velocity.Y < 0) ||
                (y >= tuning.Arena.HeightUnits - radius && fighter.Velocity.Y > 0))
                fighter.Velocity = new Vec2(fighter.Velocity.X, 0);
        }

        private static void AdvanceLoading(FighterState fighter, SimulationState state,
            CombatTuning tuning, SimulationConfig config, IList<SimulationEvent> events)
        {
            if (fighter.DividerLoadTicksRemaining <= 0) return;
            int inward = fighter.Id == 0 ? 1 : -1;
            double gap = inward * (FighterCenterLimitX(state, tuning, fighter.Id,
                fighter.Position.Y) - fighter.Position.X);
            if (fighter.Eliminated || fighter.GuardBreakTicksRemaining > 0 ||
                gap > tuning.Arena.FighterRadiusUnits +
                    tuning.Movement.DashSpeedUnitsPerSecond * config.TickDurationSeconds)
            {
                fighter.DividerLoadTicksRemaining = 0;
                return;
            }
            fighter.DividerLoadTicksRemaining--;
            int nearest = 1;
            double distance = double.PositiveInfinity;
            for (int i = 1; i < RopePointCount - 1; i++)
            {
                double delta = Math.Abs(GetRopePoint(state, tuning, i).Y - fighter.Position.Y);
                if (delta >= distance) continue;
                nearest = i;
                distance = delta;
            }
            bool fullyStretched = inward * state.Divider.NodeDeflections[nearest] >=
                tuning.Divider.MaximumDeflectionUnits - 1e-6;
            if (fighter.DividerLoadTicksRemaining > 0 && !fullyStretched) return;

            fighter.DividerLoadTicksRemaining = 0;
            // An elastic impulse requires contact at release, even if a prior
            // body impact separated the fighter shortly before the timer ended.
            if (gap > 1e-6) return;
            // Collision classification still sees this tick's powered motion.
            // MovementSystem clears the dash on the following recoil tick.
            fighter.DashTicksRemaining = Math.Max(1, fighter.DashTicksRemaining);
            fighter.Velocity = new Vec2(-inward * tuning.Divider.DashReboundSpeedUnitsPerSecond,
                fighter.Velocity.Y);
            fighter.DividerReboundDistanceRemaining = tuning.Divider.DashReboundDistanceUnits;
            events.Add(new SimulationEvent(state.Tick, SimulationEventType.DividerRebound,
                fighter.Id, -1, fighter.Position, fighter.DividerReboundDistanceRemaining));
        }

        private static Vec2 SweptCenterPosition(SimulationState state, CombatTuning tuning, int id,
            Vec2 start, Vec2 desired)
        {
            double radius = tuning.Arena.FighterRadiusUnits;
            if (SweepClear(state, tuning, start, desired, radius)) return desired;
            double maxSlope = 0;
            for (int i = 1; i < RopePointCount; i++)
            {
                Vec2 a = GetRopePoint(state, tuning, i - 1);
                Vec2 b = GetRopePoint(state, tuning, i);
                if (b.Y == a.Y) continue;
                maxSlope = Math.Max(maxSlope, Math.Abs((b.X - a.X) / (b.Y - a.Y)));
            }
            // A rope with maximum slope S changes its safe center limit by at
            // most S * |deltaY|. This yields a nearby safe binary-search bound.
            double travel = maxSlope * Math.Abs(desired.Y - start.Y) + 1e-6;
            double safe = id == 0 ? Math.Max(radius, Math.Min(desired.X, start.X - travel)) :
                Math.Min(tuning.Arena.WidthUnits - radius, Math.Max(desired.X, start.X + travel));
            if (!SweepClear(state, tuning, start, new Vec2(safe, desired.Y), radius))
                return start; // Extreme tuned slopes can leave no clear straight path this tick.
            double blocked = desired.X;
            for (int iteration = 0; iteration < 36; iteration++)
            {
                double middle = (safe + blocked) * 0.5;
                if (SweepClear(state, tuning, start, new Vec2(middle, desired.Y), radius)) safe = middle;
                else blocked = middle;
            }
            return new Vec2(safe, desired.Y);
        }

        private static bool SweepClear(SimulationState state, CombatTuning tuning,
            Vec2 start, Vec2 end, double radius)
        {
            double radiusSquared = radius * radius - 1e-9;
            for (int i = 1; i < RopePointCount; i++)
                if (SegmentDistanceSquared(start, end, GetRopePoint(state, tuning, i - 1),
                    GetRopePoint(state, tuning, i)) < radiusSquared) return false;
            return true;
        }

        private static double SegmentDistanceSquared(Vec2 p, Vec2 q, Vec2 a, Vec2 b)
        {
            Vec2 travel = q - p;
            Vec2 rope = b - a;
            double denominator = travel.X * rope.Y - travel.Y * rope.X;
            if (Math.Abs(denominator) > 1e-12)
            {
                Vec2 offset = a - p;
                double t = (offset.X * rope.Y - offset.Y * rope.X) / denominator;
                double u = (offset.X * travel.Y - offset.Y * travel.X) / denominator;
                if (t >= 0 && t <= 1 && u >= 0 && u <= 1) return 0;
            }
            return Math.Min(Math.Min(PointSegmentDistanceSquared(p, a, b),
                    PointSegmentDistanceSquared(q, a, b)),
                Math.Min(PointSegmentDistanceSquared(a, p, q),
                    PointSegmentDistanceSquared(b, p, q)));
        }

        private static double PointSegmentDistanceSquared(Vec2 point, Vec2 a, Vec2 b)
        {
            Vec2 segment = b - a;
            double lengthSquared = segment.LengthSquared;
            double t = lengthSquared <= 1e-18 ? 0 :
                SimMath.Clamp(Vec2.Dot(point - a, segment) / lengthSquared, 0, 1);
            return (point - (a + segment * t)).LengthSquared;
        }

        private static double Pressure(FighterState fighter, int id,
            SimulationState state, CombatTuning tuning)
        {
            int inward = id == 0 ? 1 : -1;
            if (fighter.Eliminated || fighter.Velocity.X * inward <= 0) return 0;
            double limit = FighterCenterLimitX(state, tuning, id,
                SimMath.Clamp(fighter.Position.Y, tuning.Arena.FighterRadiusUnits,
                    tuning.Arena.HeightUnits - tuning.Arena.FighterRadiusUnits));
            double gap = (limit - fighter.Position.X) * inward;
            if (gap > fighter.Velocity.X * inward * 0.02) return 0;
            if ((fighter.DashTicksRemaining > 0 || fighter.DividerLoadTicksRemaining > 0) &&
                fighter.DashDirection.X * inward > 0)
                return tuning.Divider.DashingDeflectionUnits;
            return fighter.Grounded ? tuning.Divider.WalkingDeflectionUnits :
                tuning.Divider.JumpingDeflectionUnits;
        }
    }
}
