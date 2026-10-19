using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>Continuous fighter contact and one-hit-per-attack body impulses.</summary>
    public static class BodySystem
    {
        public static bool TryFindNextContact(SimulationState state, double fromFraction,
            CombatTuning tuning, out BodyContact contact)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (tuning == null) throw new ArgumentNullException(nameof(tuning));
            if (!TuningValidation.IsFinite(fromFraction) || fromFraction < 0 || fromFraction > 1)
                throw new ArgumentOutOfRangeException(nameof(fromFraction));

            FighterState left = state.Fighter(SimulationState.LeftFighterId);
            FighterState right = state.Fighter(SimulationState.RightFighterId);
            contact = default;
            if (left.Eliminated || right.Eliminated || state.LastBodyContactTick == state.Tick ||
                fromFraction >= 1) return false;

            Vec2 leftStart = SimMath.FighterPositionAt(left, fromFraction);
            Vec2 rightStart = SimMath.FighterPositionAt(right, fromFraction);
            Vec2 leftDelta = left.Position - leftStart;
            Vec2 rightDelta = right.Position - rightStart;
            if (!SimMath.SweepCircles(leftStart, leftDelta, tuning.Arena.FighterRadiusUnits,
                rightStart, rightDelta, tuning.Arena.FighterRadiusUnits,
                out double localFraction, out Vec2 fromRightToLeft)) return false;

            Vec2 normal = (-fromRightToLeft).NormalizedOr(new Vec2(1, 0));
            if (normal.X < 0) normal = -normal;
            Vec2 relativeTravel = leftDelta - rightDelta;
            bool approaching = Vec2.Dot(relativeTravel, normal) > 1e-9;
            bool dashPress = IsDashAttack(left, 1, tuning) || IsDashAttack(right, -1, tuning);
            bool jumpPress = IsJumpAttack(left) || IsJumpAttack(right);
            bool walkingPress = state.Divider.LeftPressure > 0 || state.Divider.RightPressure > 0;
            if (!approaching && !dashPress && !jumpPress && !walkingPress) return false;

            contact = new BodyContact(fromFraction + (1 - fromFraction) * localFraction, normal);
            return true;
        }

        public static void ApplyContact(SimulationState state, BodyContact contact,
            CombatTuning tuning, SimulationConfig config, IList<SimulationEvent> events)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (tuning == null) throw new ArgumentNullException(nameof(tuning));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (events == null) throw new ArgumentNullException(nameof(events));
            if (!TuningValidation.IsFinite(contact.Fraction) || contact.Fraction < 0 || contact.Fraction > 1)
                throw new ArgumentOutOfRangeException(nameof(contact));

            FighterState left = state.Fighter(SimulationState.LeftFighterId);
            FighterState right = state.Fighter(SimulationState.RightFighterId);
            if (state.LastBodyContactTick == state.Tick) return;
            Vec2 leftContact = SimMath.FighterPositionAt(left, contact.Fraction);
            Vec2 rightContact = SimMath.FighterPositionAt(right, contact.Fraction);
            state.LastBodyContactTick = state.Tick;

            bool leftDash = IsDashAttack(left, 1, tuning);
            bool rightDash = IsDashAttack(right, -1, tuning);
            bool leftJump = !leftDash && IsJumpAttack(left);
            bool rightJump = !rightDash && IsJumpAttack(right);
            int leftDamage = leftDash ? tuning.Defense.DashBodyPostureDamage :
                leftJump ? tuning.Defense.JumpBodyPostureDamage : 0;
            int rightDamage = rightDash ? tuning.Defense.DashBodyPostureDamage :
                rightJump ? tuning.Defense.JumpBodyPostureDamage : 0;

            if (leftDash) left.LastDashBodyHitSequence = left.DashSequence;
            if (rightDash) right.LastDashBodyHitSequence = right.DashSequence;
            if (leftJump) left.LastJumpBodyHitAirSequence = left.AirSequence;
            if (rightJump) right.LastJumpBodyHitAirSequence = right.AirSequence;

            double speed = leftDash || rightDash ? tuning.Defense.DashBodyKnockbackUnitsPerSecond :
                leftJump || rightJump ? tuning.Defense.JumpBodyKnockbackUnitsPerSecond :
                tuning.Defense.WalkingBodyPushUnitsPerSecond;
            // Dash invulnerability suppresses posture only. The collision still transfers momentum.
            if (leftDamage > 0 && right.DashInvulnerabilityTicksRemaining <= 0)
                DefenseSystem.ApplyPostureDamage(state, right, leftDamage, tuning, config, events);
            if (rightDamage > 0 && left.DashInvulnerabilityTicksRemaining <= 0)
                DefenseSystem.ApplyPostureDamage(state, left, rightDamage, tuning, config, events);

            left.Velocity = new Vec2(-speed, left.Velocity.Y);
            right.Velocity = new Vec2(speed, right.Velocity.Y);
            double remaining = (1 - contact.Fraction) * config.TickDurationSeconds;
            left.PreviousPosition = leftContact;
            right.PreviousPosition = rightContact;
            left.MotionStartFraction = contact.Fraction;
            right.MotionStartFraction = contact.Fraction;
            left.Position = leftContact + left.Velocity * remaining;
            right.Position = rightContact + right.Velocity * remaining;
            DividerSystem.ConstrainFighter(left, state, tuning, config);
            DividerSystem.ConstrainFighter(right, state, tuning, config);

            Vec2 center = (leftContact + rightContact) * 0.5;
            if (leftDamage > 0)
                events.Add(new SimulationEvent(state.Tick, SimulationEventType.BodyImpact,
                    left.Id, right.Id, center, leftDamage));
            if (rightDamage > 0)
                events.Add(new SimulationEvent(state.Tick, SimulationEventType.BodyImpact,
                    right.Id, left.Id, center, rightDamage));
            if (leftDamage == 0 && rightDamage == 0)
                events.Add(new SimulationEvent(state.Tick, SimulationEventType.BodyImpact,
                    -1, -1, center));
        }

        private static bool IsDashAttack(FighterState fighter, int towardOpponent, CombatTuning tuning)
        {
            if (fighter.DashSequence <= fighter.LastDashBodyHitSequence ||
                fighter.DashDirection.X * towardOpponent <= 1e-9) return false;
            return fighter.DashTicksRemaining > 0 || fighter.DividerLoadTicksRemaining > 0 ||
                Math.Abs(fighter.Velocity.X) >= tuning.Movement.DashSpeedUnitsPerSecond * 0.9;
        }

        private static bool IsJumpAttack(FighterState fighter)
        {
            return !fighter.Grounded && fighter.AirSequence > fighter.LastJumpBodyHitAirSequence;
        }
    }
}
