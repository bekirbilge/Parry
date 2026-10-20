using System;

namespace Game.Core
{
    /// <summary>
    /// Deterministic local debug opponent. This is deliberately simple and is not production AI.
    /// Decisions depend only on the supplied snapshot and fixed tick number.
    /// </summary>
    public static class DebugBot
    {
        private const int DefaultTickRate = 60;

        public static FighterInput Decide(SimulationState state, int fighterId, int difficulty = 0,
            bool useFuelBoost = false)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (fighterId != SimulationState.LeftFighterId && fighterId != SimulationState.RightFighterId)
                throw new ArgumentOutOfRangeException(nameof(fighterId));
            difficulty = Math.Max(0, Math.Min(2, difficulty));

            FighterState self = state.Fighter(fighterId);
            FighterState opponent = state.Fighter(1 - fighterId);
            if (state.Phase != MatchPhase.Active || self.Eliminated || state.Tick < 0)
                return new FighterInput(Vec2.Zero);

            long tick = state.Tick;
            long phase = tick % 240;
            bool threat = IsImmediateThreat(state, self, difficulty);

            // Keep to the fighter's half, drifting toward its center when pushed too far outward
            // or toward the membrane. Approach the opponent only while safely in the outer half.
            double centerX = CombatTuning.Default.Arena.WidthUnits * (fighterId == 0 ? 0.25 : 0.75);
            double dividerX = DividerSystem.CenterXAtHeight(state, CombatTuning.Default, self.Position.Y);
            double safeEdgeX = fighterId == 0 ? dividerX - 5.0 : dividerX + 5.0;
            double targetX = centerX;
            double gap = Math.Abs(opponent.Position.X - self.Position.X);
            if (!threat && gap > 18.0 && Math.Abs(self.Position.X - safeEdgeX) > 9.0)
                targetX = fighterId == 0 ? safeEdgeX - 10.0 : safeEdgeX + 10.0;

            double moveX = Math.Abs(targetX - self.Position.X) > 1.25
                ? Math.Sign(targetX - self.Position.X) : 0;
            double moveY = 0;
            bool fuelAvailable = self.FuelSeconds > 0.08;
            bool hop = (useFuelBoost ? fuelAvailable : self.DashCharges > 0) && self.Grounded && !threat &&
                (useFuelBoost ? phase >= 176 && phase < 188 : phase == 176) &&
                (tick / 240 + fighterId) % 2 == 0;
            bool threatBoost = threat && phase % (difficulty == 0 ? 19 : difficulty == 1 ? 15 : 12) == 0;
            bool closeBoost = !threat && gap < 13.0 && phase % 73 == (fighterId == 0 ? 21 : 53);
            bool dash = !self.Eliminated && (useFuelBoost ? fuelAvailable : self.DashCharges > 0) &&
                (hop || threatBoost || closeBoost);
            bool fuelHold = useFuelBoost && (hop || threatBoost && fuelAvailable ||
                closeBoost && fuelAvailable || self.FuelBoostActive &&
                (threat || phase % 73 < (fighterId == 0 ? 33 : 65)));
            if (useFuelBoost)
                dash = fuelHold && !self.FuelDashHeldPrevious;

            if (hop) { moveX = 0; moveY = 1; }
            else if (dash || fuelHold)
            {
                // Aim toward open space: a threat-side dash is a dodge, while a close-range dash
                // advances only when it remains within the bot's own half.
                moveX = threat ? -Math.Sign(opponent.Position.X - self.Position.X) :
                    Math.Sign(opponent.Position.X - self.Position.X);
                if (moveX == 0) moveX = fighterId == 0 ? 1 : -1;
                moveY = phase % 2 == 0 ? 1 : 0;
            }

            int parryPeriod = difficulty == 0 ? 41 : difficulty == 1 ? 31 : 23;
            bool parryPulse = threat && tick % parryPeriod == (fighterId == 0 ? 0 : parryPeriod / 2);
            bool defenseHeld = threat && !parryPulse && tick % 17 < (difficulty == 0 ? 4 : 7);
            bool defensePressed = parryPulse && !self.DefenseHeldPrevious;
            // If defense was held on the preceding tick, release before another parry edge.
            if (self.DefenseHeldPrevious && parryPulse) defensePressed = false;

            return new FighterInput(new Vec2(moveX, moveY), false, false, dash,
                defensePressed, defenseHeld, fuelHold);
        }

        private static bool IsImmediateThreat(SimulationState state, FighterState self, int difficulty)
        {
            double reach = difficulty == 0 ? 11.0 : difficulty == 1 ? 15.0 : 19.0;
            foreach (ProjectileState projectile in state.Projectiles)
            {
                if (projectile.LastParrierId == self.Id) continue;
                Vec2 offset = self.Position - projectile.Position;
                double distanceSquared = offset.LengthSquared;
                if (distanceSquared > reach * reach) continue;
                if (distanceSquared < 1e-12 || Vec2.Dot(projectile.Velocity, offset) > 0)
                    return true;
            }
            return false;
        }
    }
}
