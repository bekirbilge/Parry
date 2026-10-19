#nullable enable
using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>Fixed-tick local authority. Tick is the next input tick to consume.</summary>
    public sealed class MatchSimulation
    {
        private SimulationState _state;
        public SimulationConfig Config { get; }
        public CombatTuning Tuning { get; }
        public long Tick => _state.Tick;
        public MatchPhase Phase => _state.Phase;

        public MatchSimulation(SimulationConfig? config = null, CombatTuning? tuning = null)
        {
            Config = config ?? new SimulationConfig();
            Tuning = tuning ?? CombatTuning.Default;
            _state = new SimulationState();
            ResetRound();
        }

        public SimulationState CaptureSnapshot() => _state.Clone();

        /// <summary>Restores a trusted local snapshot without retaining caller-owned objects.</summary>
        public void RestoreSnapshot(SimulationState snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (snapshot.Tick < 0 || snapshot.Fighters == null || snapshot.Fighters.Length != 2 ||
                snapshot.Fighters[0] == null || snapshot.Fighters[1] == null ||
                snapshot.Fighters[0].Id != 0 || snapshot.Fighters[1].Id != 1 ||
                snapshot.Projectiles == null || snapshot.Divider == null ||
                snapshot.Divider.NodeDeflections == null ||
                snapshot.Divider.NodeVelocities == null ||
                snapshot.Divider.NodeDeflections.Length != DividerSystem.RopePointCount ||
                snapshot.Divider.NodeVelocities.Length != DividerSystem.RopePointCount)
                throw new ArgumentException("Snapshot must contain canonical fighter slots and valid collections.", nameof(snapshot));
            _state = snapshot.Clone();
        }

        public IReadOnlyList<SimulationEvent> Step(SimulationInput input)
        {
            if (input.Tick != _state.Tick) throw new ArgumentException("Input must target the next simulation tick.", nameof(input));
            if (_state.Tick == long.MaxValue) throw new InvalidOperationException("Simulation tick limit reached.");
            var controls = new SimulationInput(input.Tick, ValidateInput(input.Left), ValidateInput(input.Right));
            var events = new List<SimulationEvent>();
            switch (_state.Phase)
            {
                case MatchPhase.Countdown:
                    if (_state.Tick >= _state.NextSpawnTick)
                    {
                        _state.Phase = MatchPhase.Active;
                        events.Add(new SimulationEvent(_state.Tick, SimulationEventType.RoundStarted, value: _state.RoundNumber));
                        AdvanceActive(controls, events);
                    }
                    else
                    {
                        IssueWarningIfDue(events);
                        _state.PhaseTicksRemaining = (int)Math.Max(0, _state.NextSpawnTick - _state.Tick - 1);
                    }
                    break;
                case MatchPhase.Active:
                    if (Tuning.Match.StartImmediatelyForTesting && _state.PhaseTicksRemaining != 0)
                    {
                        _state.PhaseTicksRemaining = 0;
                        events.Add(new SimulationEvent(_state.Tick, SimulationEventType.RoundStarted, value: _state.RoundNumber));
                    }
                    AdvanceActive(controls, events);
                    break;
                case MatchPhase.RoundResult:
                    if (_state.PhaseTicksRemaining > 0) _state.PhaseTicksRemaining--;
                    if (_state.PhaseTicksRemaining == 0)
                    {
                        _state.RoundNumber++;
                        ResetRound(afterResult: true);
                        if (!Tuning.Match.StartImmediatelyForTesting)
                        {
                            IssueWarningIfDue(events);
                            _state.PhaseTicksRemaining = Math.Max(0, _state.PhaseTicksRemaining - 1);
                        }
                    }
                    break;
                case MatchPhase.MatchComplete:
                    break;
                default:
                    throw new InvalidOperationException("Unknown match phase.");
            }
            _state.Tick++;
            return events.AsReadOnly();
        }

        private void AdvanceActive(SimulationInput input, List<SimulationEvent> events)
        {
            IssueWarningIfDue(events);
            if (_state.Projectiles.Count < Tuning.Projectile.MaximumActiveCount && _state.Tick >= _state.NextSpawnTick)
            {
                SpawnProjectile(events);
                _state.NextSpawnTick = _state.Tick + Ticks(Tuning.Projectile.SpawnIntervalSeconds);
                _state.SpawnWarningIssued = false;
            }
            MovementSystem.AdvanceKinematics(_state, input, Tuning, Config, events);
            CombatSystem.Advance(_state, input, Tuning, Config, events);
            ResolveRound(events);
        }

        private void IssueWarningIfDue(List<SimulationEvent> events)
        {
            if (_state.Projectiles.Count >= Tuning.Projectile.MaximumActiveCount || _state.SpawnWarningIssued ||
                _state.Tick < _state.NextSpawnTick - Ticks(Tuning.Projectile.SpawnWarningSeconds)) return;
            _state.SpawnWarningIssued = true;
            events.Add(new SimulationEvent(_state.Tick, SimulationEventType.SpawnWarning,
                _state.NextProjectileId, position: PortalPosition(), value: Tuning.Projectile.SpawnWarningSeconds));
        }

        private void SpawnProjectile(List<SimulationEvent> events)
        {
            double radians = Tuning.Projectile.SpawnAngleDegrees * Math.PI / 180.0;
            // Alternate the opening direction both between additions and between rounds.
            int sign = ((_state.SpawnSequence + _state.RoundNumber - 1) & 1) == 0 ? -1 : 1;
            Vec2 position = PortalPosition();
            var projectile = new ProjectileState
            {
                Id = _state.NextProjectileId++,
                Position = position,
                PreviousPosition = position,
                Velocity = new Vec2(sign * Math.Sin(radians), -Math.Cos(radians)) * Tuning.Projectile.BaseSpeedUnitsPerSecond,
                DecayTicksRemaining = Ticks(Tuning.Projectile.PowerDecaySecondsPerLevel)
            };
            _state.SpawnSequence++;
            _state.Projectiles.Add(projectile);
            events.Add(new SimulationEvent(_state.Tick, SimulationEventType.ProjectileSpawned,
                projectile.Id, position: position, value: sign));
        }

        private Vec2 PortalPosition() => new Vec2(Tuning.Arena.WidthUnits / 2,
            Math.Max(Tuning.Arena.ProjectileRadiusUnits,
                Tuning.Arena.HeightUnits - Tuning.Arena.ProjectileRadiusUnits - Tuning.Projectile.SpawnPortalInsetUnits));

        private void ResolveRound(List<SimulationEvent> events)
        {
            bool leftDead = _state.Fighter(0).Eliminated;
            bool rightDead = _state.Fighter(1).Eliminated;
            if (!leftDead && !rightDead) return;
            _state.RoundWinnerId = -1;
            if (leftDead && rightDead)
            {
                _state.DrawCount++;
                events.Add(new SimulationEvent(_state.Tick, SimulationEventType.RoundDraw));
            }
            else
            {
                int winner = leftDead ? 1 : 0;
                _state.RoundWinnerId = winner;
                _state.Fighter(winner).RoundWins++;
                events.Add(new SimulationEvent(_state.Tick, SimulationEventType.RoundWon, winner));
                if (_state.Fighter(winner).RoundWins == MatchTuning.RoundWinsRequired)
                {
                    _state.MatchWinnerId = winner;
                    _state.Phase = MatchPhase.MatchComplete;
                    _state.PhaseTicksRemaining = 0;
                    events.Add(new SimulationEvent(_state.Tick, SimulationEventType.MatchWon, winner));
                    return;
                }
            }
            _state.Phase = MatchPhase.RoundResult;
            _state.PhaseTicksRemaining = Ticks(Tuning.Match.RoundResultSeconds);
        }

        private void ResetRound(bool afterResult = false)
        {
            int leftWins = _state.Fighter(0).RoundWins;
            int rightWins = _state.Fighter(1).RoundWins;
            _state.Fighters = new[] { CreateFighter(0, leftWins), CreateFighter(1, rightWins) };
            _state.Projectiles.Clear();
            _state.Divider = new DividerState();
            _state.Phase = Tuning.Match.StartImmediatelyForTesting ? MatchPhase.Active : MatchPhase.Countdown;
            // Active rounds use this value only to defer RoundStarted to the first playable step.
            _state.PhaseTicksRemaining = Tuning.Match.StartImmediatelyForTesting ? 1 : Ticks(Tuning.Match.RoundCountdownSeconds);
            _state.RoundWinnerId = -1;
            _state.MatchWinnerId = -1;
            _state.NextSpawnTick = _state.Tick + (Tuning.Match.StartImmediatelyForTesting
                ? Ticks(Tuning.Projectile.SpawnWarningSeconds) + (afterResult ? 1 : 0)
                : _state.PhaseTicksRemaining);
            _state.SpawnWarningIssued = false;
            _state.SpawnSequence = 0;
            _state.NextProjectileId = 0;
            _state.LastBodyContactTick = -1;
        }

        private FighterState CreateFighter(int id, int wins)
        {
            var position = new Vec2(Tuning.Arena.WidthUnits * (id == 0 ? 0.25 : 0.75), Tuning.Arena.FighterRadiusUnits);
            return new FighterState
            {
                Id = id, Position = position, PreviousPosition = position, TickStartPosition = position,
                FacingSign = id == 0 ? 1 : -1, Grounded = true,
                DashCharges = MovementTuning.MaximumDashCharges, RoundWins = wins
            };
        }

        private int Ticks(double seconds) => SimMath.SecondsToTicks(seconds, Config.TickRateHz);

        private static FighterInput ValidateInput(FighterInput input)
        {
            if (!TuningValidation.IsFinite(input.Move.X) || !TuningValidation.IsFinite(input.Move.Y))
                throw new ArgumentException("Movement input must be finite.", nameof(input));
            Vec2 move = new Vec2(SimMath.Clamp(input.Move.X, -1, 1), SimMath.Clamp(input.Move.Y, -1, 1));
            return new FighterInput(move, input.JumpPressed, input.JumpHeld, input.DashPressed,
                input.DefensePressed, input.DefenseHeld);
        }
    }
}
