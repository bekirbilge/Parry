using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>Defense timing and posture rules for one authoritative tick.</summary>
    public static class DefenseSystem
    {
        public static void BeginTick(SimulationState state, SimulationInput input, CombatTuning tuning, SimulationConfig config)
        {
            int parryTicks = Math.Max(1, SimMath.SecondsToTicks(tuning.Movement.PerfectParryWindowSeconds, config.TickRateHz));
            for (int id = 0; id < state.Fighters.Length; id++)
            {
                FighterState fighter = state.Fighter(id);
                FighterInput controls = input.ForFighter(id);
                bool pressed = controls.DefensePressed || (controls.DefenseHeld && !fighter.DefenseHeldPrevious);
                fighter.DefenseHoldTicks = controls.DefenseHeld ? fighter.DefenseHoldTicks + 1 : 0;
                if (pressed && fighter.GuardBreakTicksRemaining == 0)
                    fighter.ParryTicksRemaining = parryTicks;
                fighter.DefenseHeldPrevious = controls.DefenseHeld;
            }
        }

        public static bool CanParry(FighterState fighter) =>
            fighter.GuardBreakTicksRemaining == 0 && fighter.ParryTicksRemaining > 0;

        public static bool CanGuard(FighterState fighter, FighterInput input, CombatTuning tuning, SimulationConfig config) =>
            fighter.GuardBreakTicksRemaining == 0 && input.DefenseHeld &&
            fighter.DefenseHoldTicks >= SimMath.SecondsToTicks(tuning.Defense.GuardHoldThresholdSeconds, config.TickRateHz);

        public static void ApplyPostureDamage(SimulationState state, FighterState fighter, double amount,
            CombatTuning tuning, SimulationConfig config, IList<SimulationEvent> events)
        {
            if (amount < 0 || double.IsNaN(amount) || double.IsInfinity(amount))
                throw new ArgumentOutOfRangeException(nameof(amount));
            fighter.PostureTouchedTick = state.Tick;
            fighter.PostureRecoveryDelayTicksRemaining = SimMath.SecondsToTicks(tuning.Defense.HitRecoveryDelaySeconds, config.TickRateHz);
            if (amount == 0) return;
            fighter.Posture = Math.Min(tuning.Defense.MaximumPosture, fighter.Posture + amount);
            if (fighter.Posture >= tuning.Defense.MaximumPosture && fighter.GuardBreakTicksRemaining == 0)
            {
                fighter.GuardBreakTicksRemaining = Math.Max(1,
                    SimMath.SecondsToTicks(tuning.Defense.GuardBreakDurationSeconds, config.TickRateHz));
                fighter.ParryTicksRemaining = 0;
                fighter.DashTicksRemaining = 0;
                fighter.DividerLoadTicksRemaining = 0;
                fighter.DashInvulnerabilityTicksRemaining = 0;
                events.Add(new SimulationEvent(state.Tick, SimulationEventType.GuardBroken,
                    fighter.Id, position: fighter.Position));
            }
        }

        public static void EndTick(SimulationState state, SimulationInput input, CombatTuning tuning,
            SimulationConfig config, int[] breakTicksAtStart)
        {
            for (int id = 0; id < state.Fighters.Length; id++)
            {
                FighterState fighter = state.Fighter(id);
                if (fighter.ParryTicksRemaining > 0) fighter.ParryTicksRemaining--;
                if (breakTicksAtStart[id] > 0 && fighter.GuardBreakTicksRemaining > 0)
                    fighter.GuardBreakTicksRemaining--;
                if (fighter.PostureTouchedTick == state.Tick) continue;
                if (fighter.PostureRecoveryDelayTicksRemaining > 0)
                {
                    fighter.PostureRecoveryDelayTicksRemaining--;
                    continue;
                }
                double rate = CanGuard(fighter, input.ForFighter(id), tuning, config)
                    ? tuning.Defense.GuardIdleRecoveryPerSecond
                    : tuning.Defense.UnguardedIdleRecoveryPerSecond;
                fighter.Posture = Math.Max(0, fighter.Posture - rate * config.TickDurationSeconds);
            }
        }
    }
}
