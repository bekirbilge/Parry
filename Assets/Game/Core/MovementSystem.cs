using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>Advances fighter controls and predicts one authoritative motion segment.</summary>
    public static class MovementSystem
    {
        public static void AdvanceKinematics(SimulationState state, SimulationInput input,
            CombatTuning tuning, SimulationConfig config, IList<SimulationEvent> events)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (tuning == null) throw new ArgumentNullException(nameof(tuning));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (events == null) throw new ArgumentNullException(nameof(events));

            for (int id = 0; id < state.Fighters.Length; id++)
            {
                FighterState fighter = state.Fighters[id];
                fighter.TickStartPosition = fighter.Position;
                fighter.PreviousPosition = fighter.Position;
                fighter.MotionStartFraction = 0;
                if (fighter.Eliminated) continue;
                AdvanceFighter(fighter, input.ForFighter(id), tuning, config);
            }

            DividerSystem.AdvanceAndConstrain(state, tuning, config, events);
        }

        private static void AdvanceFighter(FighterState fighter, FighterInput input,
            CombatTuning tuning, SimulationConfig config)
        {
            MovementTuning movement = tuning.Movement;
            double dt = config.TickDurationSeconds;
            Vec2 stick = input.Move;
            if (!TuningValidation.IsFinite(stick.X) || !TuningValidation.IsFinite(stick.Y)) stick = Vec2.Zero;
            if (stick.LengthSquared > 1) stick = stick.NormalizedOr(Vec2.Zero);
            if (stick.X > 0) fighter.FacingSign = 1;
            else if (stick.X < 0) fighter.FacingSign = -1;

            // A membrane impact ends the dash on the following tick, after the
            // collision system has had the impact tick to classify body contact.
            if (movement.UseFuelBoost)
                AdvanceFuel(fighter, input, stick, movement, config);
            else
            {
                if (fighter.DividerReboundDistanceRemaining > 0) fighter.DashTicksRemaining = 0;
                else if (fighter.DashTicksRemaining > 0) fighter.DashTicksRemaining--;
                Recharge(fighter, movement, config);
            }
            if (input.JumpPressed) fighter.JumpBufferTicksRemaining =
                Math.Max(1, SimMath.SecondsToTicks(movement.JumpBufferSeconds, config.TickRateHz));

            if (fighter.Grounded)
                fighter.CoyoteTicksRemaining = SimMath.SecondsToTicks(movement.CoyoteTimeSeconds, config.TickRateHz);
            else if (fighter.CoyoteTicksRemaining > 0)
                fighter.CoyoteTicksRemaining--;

            bool canJump = fighter.Grounded || fighter.CoyoteTicksRemaining > 0;
            if (fighter.JumpBufferTicksRemaining > 0 && canJump)
            {
                fighter.Velocity = new Vec2(fighter.Velocity.X, movement.JumpSpeedUnitsPerSecond);
                fighter.Grounded = false;
                fighter.CoyoteTicksRemaining = 0;
                fighter.JumpBufferTicksRemaining = 0;
                fighter.AirSequence++;
            }

            if (!movement.UseFuelBoost)
            {
                int dashDuration = SimMath.SecondsToTicks(movement.DashDurationSeconds, config.TickRateHz);
                int cancelTicks = SimMath.SecondsToTicks(movement.DashCancelAfterSeconds, config.TickRateHz);
                Vec2 requestedDashDirection = stick.NormalizedOr(new Vec2(0, 1));
                int inward = fighter.Id == SimulationState.LeftFighterId ? 1 : -1;
                // A fresh inward press cannot restart the rope loading clock.
                // Outward or vertical dashes still let the fighter escape it.
                bool canCancelLoad = fighter.DividerLoadTicksRemaining <= 0 ||
                    requestedDashDirection.X * inward <= 1e-9;
                bool canCancelDash = canCancelLoad && (fighter.DashTicksRemaining <= 0 ||
                    dashDuration - fighter.DashTicksRemaining >= cancelTicks);
                if (input.DashPressed && fighter.DashCharges > 0 &&
                    fighter.GuardBreakTicksRemaining <= 0 && canCancelDash)
                {
                    fighter.DashCharges--;
                    fighter.DashTicksRemaining = dashDuration;
                    fighter.DashInvulnerabilityTicksRemaining =
                        SimMath.SecondsToTicks(movement.DashInvulnerabilitySeconds, config.TickRateHz);
                    fighter.DashDirection = requestedDashDirection;
                    fighter.DashSequence++;
                    fighter.DividerLoadTicksRemaining = 0;
                    fighter.DividerReboundDistanceRemaining = 0;
                    if (fighter.DashRechargeTicksRemaining <= 0)
                        fighter.DashRechargeTicksRemaining = SimMath.SecondsToTicks(movement.DashRechargeSeconds, config.TickRateHz);
                }
            }

            if ((movement.UseFuelBoost ? fighter.FuelBoostActive :
                    fighter.DashTicksRemaining > 0 || fighter.DividerLoadTicksRemaining > 0) &&
                fighter.DividerReboundDistanceRemaining <= 0)
            {
                Vec2 target = fighter.DashDirection * movement.BoostSpeedForDirection(fighter.DashDirection);
                fighter.Velocity = movement.UseFuelBoost
                    ? MoveToward(fighter.Velocity, target,
                        movement.FuelSteeringAccelerationUnitsPerSecondSquared * dt)
                    : target;
            }
            else
            {
                double target = stick.X * movement.RunSpeedUnitsPerSecond;
                double acceleration = fighter.Grounded ? movement.GroundAccelerationUnitsPerSecondSquared :
                    movement.AirAccelerationUnitsPerSecondSquared;
                if (Math.Abs(stick.X) < 1e-9 && fighter.Grounded)
                    acceleration = movement.GroundFrictionUnitsPerSecondSquared;
                double vx = MoveToward(fighter.Velocity.X, target, acceleration * dt);

                if (fighter.DividerReboundDistanceRemaining > 0)
                {
                    int outward = fighter.Id == SimulationState.LeftFighterId ? -1 : 1;
                    double rebound = tuning.Divider.DashReboundSpeedUnitsPerSecond;
                    // Steering changes the rebound without removing the initial spring impulse.
                    vx = outward * Math.Max(0, rebound - Math.Max(0, -outward * target));
                }

                double gravity = movement.GravityUnitsPerSecondSquared;
                if (!input.JumpHeld && fighter.Velocity.Y > 0)
                    gravity *= movement.JumpReleaseGravityMultiplier;
                double vy = fighter.Velocity.Y - gravity * dt;
                fighter.Velocity = new Vec2(vx, vy);
            }

            Vec2 desired = fighter.Position + fighter.Velocity * dt;
            double floor = tuning.Arena.FighterRadiusUnits;
            double ceiling = tuning.Arena.HeightUnits - floor;
            if (desired.Y <= floor)
            {
                desired = new Vec2(desired.X, floor);
                fighter.Velocity = new Vec2(fighter.Velocity.X, Math.Max(0, fighter.Velocity.Y));
                fighter.Grounded = true;
            }
            else
            {
                fighter.Grounded = false;
                if (desired.Y >= ceiling)
                {
                    desired = new Vec2(desired.X, ceiling);
                    fighter.Velocity = new Vec2(fighter.Velocity.X, Math.Min(0, fighter.Velocity.Y));
                }
            }

            if (fighter.DividerReboundDistanceRemaining > 0)
            {
                int outward = fighter.Id == SimulationState.LeftFighterId ? -1 : 1;
                double traveled = Math.Max(0, outward * (desired.X - fighter.Position.X));
                double bounded = Math.Min(traveled, fighter.DividerReboundDistanceRemaining);
                desired = new Vec2(fighter.Position.X + outward * bounded, desired.Y);
                fighter.DividerReboundDistanceRemaining -= bounded;
            }

            fighter.Position = desired;
            if (fighter.JumpBufferTicksRemaining > 0) fighter.JumpBufferTicksRemaining--;
            fighter.JumpHeldPrevious = input.JumpHeld;
            // Timers describe the current tick until all collision classification finishes.
            // Dash invulnerability is decremented by CombatSystem after collisions.
        }

        private static void AdvanceFuel(FighterState fighter, FighterInput input, Vec2 stick,
            MovementTuning movement, SimulationConfig config)
        {
            double dt = config.TickDurationSeconds;
            bool requested = input.DashHeld || input.DashPressed;
            bool freshPress = input.DashPressed || (input.DashHeld && !fighter.FuelDashHeldPrevious);
            if (fighter.FuelInvulnerabilityRearmTicksRemaining > 0)
                fighter.FuelInvulnerabilityRearmTicksRemaining--;

            if (!requested) fighter.FuelBoostNeedsRelease = false;
            if (fighter.FuelBoostActive && (!requested || fighter.GuardBreakTicksRemaining > 0 ||
                fighter.DividerReboundDistanceRemaining > 0 || fighter.FuelSeconds <= dt + 1e-9))
            {
                bool needsRelease = requested;
                EndFuelBoost(fighter, needsRelease);
                if (fighter.FuelSeconds <= dt + 1e-9) fighter.FuelSeconds = 0;
            }

            if (!fighter.FuelBoostActive && requested && freshPress &&
                !fighter.FuelBoostNeedsRelease && fighter.GuardBreakTicksRemaining <= 0 &&
                fighter.DividerReboundDistanceRemaining <= 0 &&
                fighter.FuelSeconds > movement.FuelIgnitionCostSeconds + dt + 1e-9)
            {
                fighter.FuelBoostActive = true;
                fighter.FuelSeconds -= movement.FuelIgnitionCostSeconds;
                fighter.DashDirection = stick.NormalizedOr(new Vec2(0, 1));
                fighter.Velocity = fighter.DashDirection * movement.BoostSpeedForDirection(fighter.DashDirection);
                fighter.DashSequence++;
                fighter.DashTicksRemaining = 1;
                if (fighter.FuelInvulnerabilityRearmTicksRemaining <= 0)
                {
                    fighter.DashInvulnerabilityTicksRemaining =
                        SimMath.SecondsToTicks(movement.DashInvulnerabilitySeconds, config.TickRateHz);
                    fighter.FuelInvulnerabilityRearmTicksRemaining =
                        SimMath.SecondsToTicks(movement.FuelInvulnerabilityRearmSeconds, config.TickRateHz);
                }
            }

            if (fighter.FuelBoostActive)
            {
                fighter.DashDirection = stick.NormalizedOr(new Vec2(0, 1));
                fighter.DashTicksRemaining = 1;
                fighter.FuelSeconds = Math.Max(0, fighter.FuelSeconds - dt);
                fighter.FuelRegenDelayTicksRemaining =
                    SimMath.SecondsToTicks(movement.FuelRegenerationDelaySeconds, config.TickRateHz);
            }
            else
            {
                fighter.DashTicksRemaining = 0;
                if (fighter.FuelRegenDelayTicksRemaining > 0)
                    fighter.FuelRegenDelayTicksRemaining--;
                else
                    fighter.FuelSeconds = Math.Min(movement.FuelCapacitySeconds,
                        fighter.FuelSeconds + movement.FuelRegenerationPerSecond * dt);
            }
            fighter.FuelDashHeldPrevious = input.DashHeld;
        }

        private static void EndFuelBoost(FighterState fighter, bool needsRelease)
        {
            fighter.FuelBoostActive = false;
            fighter.FuelBoostNeedsRelease = needsRelease;
            fighter.DashTicksRemaining = 0;
            fighter.DividerLoadTicksRemaining = 0;
        }

        private static void Recharge(FighterState fighter, MovementTuning movement, SimulationConfig config)
        {
            if (fighter.DashCharges >= MovementTuning.MaximumDashCharges)
            {
                fighter.DashCharges = MovementTuning.MaximumDashCharges;
                fighter.DashRechargeTicksRemaining = 0;
                return;
            }

            if (fighter.DashRechargeTicksRemaining <= 0)
                fighter.DashRechargeTicksRemaining = SimMath.SecondsToTicks(movement.DashRechargeSeconds, config.TickRateHz);
            fighter.DashRechargeTicksRemaining--;
            if (fighter.DashRechargeTicksRemaining <= 0)
            {
                fighter.DashCharges++;
                if (fighter.DashCharges < MovementTuning.MaximumDashCharges)
                    fighter.DashRechargeTicksRemaining = SimMath.SecondsToTicks(movement.DashRechargeSeconds, config.TickRateHz);
            }
        }

        private static double MoveToward(double current, double target, double delta)
        {
            if (current < target) return Math.Min(current + delta, target);
            return Math.Max(current - delta, target);
        }

        private static Vec2 MoveToward(Vec2 current, Vec2 target, double delta)
        {
            Vec2 difference = target - current;
            double distance = difference.Length;
            return distance <= delta ? target : current + difference * (delta / distance);
        }
    }
}
