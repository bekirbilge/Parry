using System;

namespace Game.Core
{
    /// <summary>Small double-precision vector used only by the authoritative core.</summary>
    public readonly struct Vec2
    {
        public readonly double X;
        public readonly double Y;

        public Vec2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static Vec2 Zero => new Vec2(0, 0);
        public double LengthSquared => X * X + Y * Y;
        public double Length => Math.Sqrt(LengthSquared);

        public Vec2 NormalizedOr(Vec2 fallback)
        {
            double length = Length;
            return length > 1e-12 ? this / length : fallback;
        }

        public static double Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Y * b.Y;
        public static Vec2 Reflect(Vec2 velocity, Vec2 unitNormal) => velocity - 2 * Dot(velocity, unitNormal) * unitNormal;
        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator -(Vec2 a) => new Vec2(-a.X, -a.Y);
        public static Vec2 operator *(Vec2 a, double scale) => new Vec2(a.X * scale, a.Y * scale);
        public static Vec2 operator *(double scale, Vec2 a) => a * scale;
        public static Vec2 operator /(Vec2 a, double scale) => new Vec2(a.X / scale, a.Y / scale);
    }

    public static class SimMath
    {
        public static double Clamp(double value, double minimum, double maximum) => Math.Max(minimum, Math.Min(maximum, value));

        /// <summary>Samples the remaining fighter path after the most recent contact.</summary>
        public static Vec2 FighterPositionAt(FighterState fighter, double fraction)
        {
            if (fighter == null) throw new ArgumentNullException(nameof(fighter));
            double start = Clamp(fighter.MotionStartFraction, 0, 1);
            double t = start >= 1 ? 1 : Clamp((fraction - start) / (1 - start), 0, 1);
            return fighter.PreviousPosition + (fighter.Position - fighter.PreviousPosition) * t;
        }

        public static int SecondsToTicks(double seconds, int tickRateHz)
        {
            if (!TuningValidation.IsFinite(seconds) || seconds < 0) throw new ArgumentOutOfRangeException(nameof(seconds));
            if (tickRateHz <= 0) throw new ArgumentOutOfRangeException(nameof(tickRateHz));
            double ticks = Math.Ceiling(seconds * tickRateHz - 1e-9);
            if (ticks > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(seconds));
            return (int)ticks;
        }

        /// <summary>First contact of a moving point and stationary circle during one step.</summary>
        public static bool SweepPointCircle(Vec2 start, Vec2 delta, Vec2 center, double radius,
            out double fraction, out Vec2 normal)
        {
            if (!TuningValidation.IsFinite(radius) || radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));
            Vec2 offset = start - center;
            double c = offset.LengthSquared - radius * radius;
            if (c <= 0)
            {
                fraction = 0;
                normal = offset.NormalizedOr((-delta).NormalizedOr(new Vec2(1, 0)));
                return true;
            }

            double a = delta.LengthSquared;
            double b = 2 * Vec2.Dot(offset, delta);
            double discriminant = b * b - 4 * a * c;
            if (a <= 1e-24 || b >= 0 || discriminant < 0)
            {
                fraction = 0;
                normal = Vec2.Zero;
                return false;
            }

            double candidate = (-b - Math.Sqrt(discriminant)) / (2 * a);
            if (candidate < 0 || candidate > 1)
            {
                fraction = 0;
                normal = Vec2.Zero;
                return false;
            }

            fraction = candidate;
            normal = (offset + delta * candidate).NormalizedOr(new Vec2(1, 0));
            return true;
        }

        /// <summary>Relative sweep includes movement of both circles over the same tick.</summary>
        public static bool SweepCircles(Vec2 aStart, Vec2 aDelta, double aRadius,
            Vec2 bStart, Vec2 bDelta, double bRadius, out double fraction, out Vec2 normal)
        {
            if (!TuningValidation.IsFinite(aRadius) || aRadius < 0) throw new ArgumentOutOfRangeException(nameof(aRadius));
            if (!TuningValidation.IsFinite(bRadius) || bRadius < 0) throw new ArgumentOutOfRangeException(nameof(bRadius));
            return SweepPointCircle(aStart - bStart, aDelta - bDelta, Vec2.Zero,
                aRadius + bRadius, out fraction, out normal);
        }
    }
}
