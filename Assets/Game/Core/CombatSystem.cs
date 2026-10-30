using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>Chronological, continuous contacts after MovementSystem.AdvanceKinematics.</summary>
    public static class CombatSystem
    {
        private const double FractionEpsilon = 1e-10;
        private const double SeparationEpsilon = 1e-8;
        private const int MaximumContactsPerTick = 2048;

        private enum ContactKind { SafetyExit, Fighter, WallX, WallY, Body }
        private enum DefenseKind { Parry, OwnedRedirect, Guard, Invulnerable, Lethal }

        private readonly struct Contact
        {
            public readonly double Fraction;
            public readonly ContactKind Kind;
            public readonly ProjectileState Projectile;
            public readonly int FighterId;
            public readonly Vec2 Normal;
            public readonly BodyContact Body;

            public Contact(double fraction, ContactKind kind, ProjectileState projectile,
                int fighterId, Vec2 normal, BodyContact body = default)
            {
                Fraction = fraction;
                Kind = kind;
                Projectile = projectile;
                FighterId = fighterId;
                Normal = normal;
                Body = body;
            }
        }

        public static void Advance(SimulationState state, SimulationInput input, CombatTuning tuning,
            SimulationConfig config, IList<SimulationEvent> events)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (tuning == null) throw new ArgumentNullException(nameof(tuning));
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (events == null) throw new ArgumentNullException(nameof(events));

            int[] breakTicksAtStart =
            {
                state.Fighter(0).GuardBreakTicksRemaining,
                state.Fighter(1).GuardBreakTicksRemaining
            };
            DefenseSystem.BeginTick(state, input, tuning, config);
            FloorHazardSystem.Apply(state, input, tuning, config, events);
            foreach (ProjectileState projectile in state.Projectiles)
            {
                projectile.PreviousPosition = projectile.Position;
                DecayPower(projectile, tuning, config);
                EaseDecayingSpeed(projectile, tuning, config);
            }

            double now = 0;
            int contactsResolved = 0;
            var suppressedPairs = new HashSet<long>();
            var lastContactFractions = new Dictionary<long, double>();
            var candidates = new List<Contact>(state.Projectiles.Count * 4 + 1);
            while (now < 1 - FractionEpsilon)
            {
                candidates.Clear();
                if (BodySystem.TryFindNextContact(state, now, tuning, out BodyContact body))
                    candidates.Add(new Contact(body.Fraction, ContactKind.Body, null!, -1, body.Normal, body));

                foreach (ProjectileState projectile in state.Projectiles)
                {
                    AddWallContacts(candidates, projectile, now, tuning, config);
                    AddSafetyExit(candidates, projectile, state, now, tuning, config);
                    for (int fighterId = 0; fighterId < 2; fighterId++)
                        AddFighterContact(candidates, projectile, state.Fighter(fighterId), now,
                            state, tuning, config, suppressedPairs);
                }

                if (candidates.Count == 0)
                {
                    AdvanceProjectiles(state.Projectiles, 1 - now, config);
                    now = 1;
                    break;
                }

                double next = 1;
                foreach (Contact candidate in candidates)
                    if (candidate.Fraction < next) next = candidate.Fraction;
                if (next < now - FractionEpsilon || next > 1 + FractionEpsilon)
                    throw new InvalidOperationException("Contact time escaped the current tick.");
                next = SimMath.Clamp(next, now, 1);
                AdvanceProjectiles(state.Projectiles, next - now, config);
                now = next;

                var batch = new List<Contact>();
                foreach (Contact candidate in candidates)
                    if (Math.Abs(candidate.Fraction - next) <= FractionEpsilon) batch.Add(candidate);
                batch.Sort(CompareContacts);
                contactsResolved += batch.Count;
                if (contactsResolved > MaximumContactsPerTick)
                    throw new InvalidOperationException("Contact budget exceeded; simulation refuses to lose contacts silently.");

                // Clearance must take effect before simultaneous fighter contacts are classified.
                foreach (Contact contact in batch)
                    if (contact.Kind == ContactKind.SafetyExit)
                        ClearContactSafety(contact.Projectile, contact.FighterId, suppressedPairs);

                // Classify the entire contact-time group before any block can cause guard break.
                var defenses = new DefenseKind[batch.Count];
                for (int index = 0; index < batch.Count; index++)
                {
                    if (batch[index].Kind != ContactKind.Fighter) continue;
                    FighterState fighter = state.Fighter(batch[index].FighterId);
                    FighterInput controls = input.ForFighter(fighter.Id);
                    defenses[index] = batch[index].Projectile.LastParrierId == fighter.Id
                        ? DefenseKind.OwnedRedirect
                        : DefenseSystem.CanParry(fighter) ? DefenseKind.Parry :
                        DefenseSystem.CanGuard(fighter, controls, tuning, config) ? DefenseKind.Guard :
                        fighter.DashInvulnerabilityTicksRemaining > 0 ? DefenseKind.Invulnerable : DefenseKind.Lethal;
                }

                for (int index = 0; index < batch.Count; index++)
                {
                    Contact contact = batch[index];
                    if (contact.Kind == ContactKind.Fighter)
                    {
                        ResolveFighterContact(state, input, tuning, config, events,
                            contact, defenses[index], suppressedPairs, lastContactFractions);
                    }
                    else if (contact.Kind == ContactKind.WallX || contact.Kind == ContactKind.WallY)
                    {
                        ResolveWallContact(state, tuning, events, contact);
                    }
                    else if (contact.Kind == ContactKind.Body)
                    {
                        BodySystem.ApplyContact(state, contact.Body, tuning, config, events);
                    }
                }
                ResolveCompoundBoundaryContacts(batch, defenses);
            }

            if (now < 1) AdvanceProjectiles(state.Projectiles, 1 - now, config);
            ValidateProjectileBounds(state.Projectiles, tuning);
            RefreshContactMasks(state, tuning);
            DefenseSystem.EndTick(state, input, tuning, config, breakTicksAtStart);
            for (int id = 0; id < 2; id++)
            {
                FighterState fighter = state.Fighter(id);
                if (fighter.DashInvulnerabilityTicksRemaining > 0)
                    fighter.DashInvulnerabilityTicksRemaining--;
            }
        }

        private static int CompareContacts(Contact left, Contact right)
        {
            int leftId = left.Kind == ContactKind.Body ? int.MaxValue : left.Projectile.Id;
            int rightId = right.Kind == ContactKind.Body ? int.MaxValue : right.Projectile.Id;
            int byId = leftId.CompareTo(rightId);
            if (byId != 0) return byId;
            int byKind = left.Kind.CompareTo(right.Kind);
            return byKind != 0 ? byKind : left.FighterId.CompareTo(right.FighterId);
        }

        private static void DecayPower(ProjectileState projectile, CombatTuning tuning, SimulationConfig config)
        {
            if (projectile.PowerLevel <= 0)
            {
                projectile.PowerLevel = 0;
                projectile.DecayTicksRemaining = 0;
                return;
            }
            if (--projectile.DecayTicksRemaining > 0) return;
            projectile.PowerLevel--;
            projectile.DecayTicksRemaining = projectile.PowerLevel == 0 ? 0 :
                Math.Max(1, SimMath.SecondsToTicks(tuning.Projectile.PowerDecaySecondsPerLevel, config.TickRateHz));
            projectile.SpeedDecaying = true;
        }

        private static void EaseDecayingSpeed(ProjectileState projectile, CombatTuning tuning, SimulationConfig config)
        {
            if (!projectile.SpeedDecaying) return;
            double targetSpeed = tuning.Projectile.BaseSpeedUnitsPerSecond *
                tuning.Projectile.SpeedMultiplierAtLevel(projectile.PowerLevel);
            double currentSpeed = projectile.Velocity.Length;
            double nextSpeed = Math.Max(targetSpeed, currentSpeed -
                tuning.Projectile.SpeedDecayUnitsPerSecondSquared * config.TickDurationSeconds);
            if (currentSpeed <= targetSpeed || nextSpeed <= targetSpeed)
            {
                nextSpeed = targetSpeed;
                projectile.SpeedDecaying = false;
            }
            projectile.Velocity = projectile.Velocity.NormalizedOr(new Vec2(1, 0)) * nextSpeed;
        }

        private static void SetProjectileSpeed(ProjectileState projectile, CombatTuning tuning)
        {
            double speed = tuning.Projectile.BaseSpeedUnitsPerSecond *
                tuning.Projectile.SpeedMultiplierAtLevel(projectile.PowerLevel);
            projectile.Velocity = projectile.Velocity.NormalizedOr(new Vec2(1, 0)) * speed;
        }

        private static void AdvanceProjectiles(List<ProjectileState> projectiles, double fraction, SimulationConfig config)
        {
            if (fraction <= 0) return;
            double seconds = fraction * config.TickDurationSeconds;
            foreach (ProjectileState projectile in projectiles)
                projectile.Position += projectile.Velocity * seconds;
        }

        private static void AddFighterContact(List<Contact> contacts, ProjectileState projectile,
            FighterState fighter, double now, SimulationState state, CombatTuning tuning,
            SimulationConfig config, HashSet<long> suppressedPairs)
        {
            if (fighter.Eliminated) return;
            long pair = ((long)projectile.Id << 32) | (uint)fighter.Id;
            int fighterBit = 1 << fighter.Id;
            bool parryActive = DefenseSystem.CanParry(fighter);
            double physicalRadius = tuning.Arena.ProjectileRadiusUnits + tuning.Arena.FighterRadiusUnits;
            double parryRadius = physicalRadius + tuning.Defense.ParryExtraReachUnits;
            double radius = parryActive ? parryRadius : physicalRadius;
            Vec2 fighterStart = SimMath.FighterPositionAt(fighter, now);
            double separationSquared = (projectile.Position - fighterStart).LengthSquared;
            double physicalClearRadius = physicalRadius + SeparationEpsilon;
            double parryClearRadius = parryRadius + SeparationEpsilon;
            if (projectile.LastParrierId == fighter.Id &&
                separationSquared > parryClearRadius * parryClearRadius)
                ClearContactSafety(projectile, fighter.Id, suppressedPairs);
            if ((projectile.ReflectedContactMask & fighterBit) != 0)
            {
                if (separationSquared <= physicalClearRadius * physicalClearRadius) return;
                projectile.ReflectedContactMask &= ~fighterBit;
            }
            if ((projectile.ParryContactMask & fighterBit) != 0)
            {
                if (separationSquared > parryClearRadius * parryClearRadius)
                    projectile.ParryContactMask &= ~fighterBit;
                else if (parryActive) return;
            }
            // Safety lasts only through the parry overlap. A timed press may
            // redirect during that overlap; other contacts pass through.
            if (projectile.LastParrierId == fighter.Id && !parryActive) return;
            if (suppressedPairs.Contains(pair))
            {
                double clearRadius = radius + SeparationEpsilon;
                if (separationSquared <= clearRadius * clearRadius) return;
                suppressedPairs.Remove(pair);
            }
            double remaining = 1 - now;
            if (remaining <= FractionEpsilon) return;
            Vec2 projectileDelta = projectile.Velocity * (remaining * config.TickDurationSeconds);
            Vec2 fighterDelta = SimMath.FighterPositionAt(fighter, 1) - fighterStart;
            if (!SimMath.SweepCircles(projectile.Position, projectileDelta, tuning.Arena.ProjectileRadiusUnits,
                fighterStart, fighterDelta, radius - tuning.Arena.ProjectileRadiusUnits,
                out double local, out Vec2 normal)) return;
            contacts.Add(new Contact(now + local * remaining, ContactKind.Fighter, projectile, fighter.Id, normal));
        }

        private static void AddSafetyExit(List<Contact> contacts, ProjectileState projectile,
            SimulationState state, double now, CombatTuning tuning, SimulationConfig config)
        {
            int fighterId = projectile.LastParrierId;
            if (fighterId < 0) return;
            FighterState fighter = state.Fighter(fighterId);
            Vec2 start = projectile.Position - SimMath.FighterPositionAt(fighter, now);
            double radius = tuning.Arena.ProjectileRadiusUnits + tuning.Arena.FighterRadiusUnits +
                tuning.Defense.ParryExtraReachUnits + SeparationEpsilon;
            if (start.LengthSquared > radius * radius)
            {
                contacts.Add(new Contact(now, ContactKind.SafetyExit, projectile, fighterId, Vec2.Zero));
                return;
            }
            double remaining = 1 - now;
            if (remaining <= FractionEpsilon) return;
            Vec2 delta = projectile.Velocity * (remaining * config.TickDurationSeconds) -
                (SimMath.FighterPositionAt(fighter, 1) - SimMath.FighterPositionAt(fighter, now));
            double a = delta.LengthSquared;
            if (a <= 1e-24) return;
            double b = 2 * Vec2.Dot(start, delta);
            double c = start.LengthSquared - radius * radius;
            double discriminant = b * b - 4 * a * c;
            if (discriminant < 0) return;
            double exit = (-b + Math.Sqrt(discriminant)) / (2 * a);
            if (exit >= 0 && exit < 1 - FractionEpsilon)
                contacts.Add(new Contact(now + exit * remaining, ContactKind.SafetyExit,
                    projectile, fighterId, Vec2.Zero));
        }

        private static void ClearContactSafety(ProjectileState projectile, int fighterId,
            HashSet<long> suppressedPairs)
        {
            if (projectile.LastParrierId != fighterId) return;
            projectile.LastParrierId = -1;
            int bit = 1 << fighterId;
            projectile.ReflectedContactMask &= ~bit;
            projectile.ParryContactMask &= ~bit;
            suppressedPairs.Remove(((long)projectile.Id << 32) | (uint)fighterId);
        }

        private static void AddWallContacts(List<Contact> contacts, ProjectileState projectile,
            double now, CombatTuning tuning, SimulationConfig config)
        {
            double remaining = 1 - now;
            if (remaining <= FractionEpsilon) return;
            double radius = tuning.Arena.ProjectileRadiusUnits;
            double maxX = tuning.Arena.WidthUnits - radius;
            double maxY = tuning.Arena.HeightUnits - radius;
            AddAxisWall(contacts, projectile, now, remaining, config.TickDurationSeconds,
                projectile.Position.X, projectile.Velocity.X, radius, maxX, ContactKind.WallX);
            AddAxisWall(contacts, projectile, now, remaining, config.TickDurationSeconds,
                projectile.Position.Y, projectile.Velocity.Y, radius, maxY, ContactKind.WallY);
        }

        private static void AddAxisWall(List<Contact> contacts, ProjectileState projectile,
            double now, double remaining, double tickSeconds, double position, double velocity,
            double minimum, double maximum, ContactKind kind)
        {
            if (Math.Abs(velocity) < 1e-12) return;
            double target = velocity < 0 ? minimum : maximum;
            double seconds = (target - position) / velocity;
            if (seconds < -FractionEpsilon || seconds > remaining * tickSeconds + FractionEpsilon) return;
            double fraction = now + Math.Max(0, seconds / tickSeconds);
            Vec2 normal = kind == ContactKind.WallX
                ? new Vec2(velocity < 0 ? 1 : -1, 0)
                : new Vec2(0, velocity < 0 ? 1 : -1);
            contacts.Add(new Contact(fraction, kind, projectile, -1, normal));
        }

        private static void ResolveFighterContact(SimulationState state, SimulationInput input,
            CombatTuning tuning, SimulationConfig config, IList<SimulationEvent> events,
            Contact contact, DefenseKind defense, HashSet<long> suppressedPairs,
            Dictionary<long, double> lastContactFractions)
        {
            ProjectileState projectile = contact.Projectile;
            FighterState fighter = state.Fighter(contact.FighterId);
            long pair = ((long)projectile.Id << 32) | (uint)fighter.Id;
            suppressedPairs.Add(pair);
            lastContactFractions[pair] = contact.Fraction;
            projectile.LastContactTick = state.Tick;
            projectile.LastContactFighterId = fighter.Id;
            Vec2 impact = projectile.Position;
            if (defense == DefenseKind.OwnedRedirect)
            {
                ClearOtherFighterContact(projectile, fighter.Id, contact.Fraction,
                    suppressedPairs, lastContactFractions);
                MarkParryContact(projectile, fighter, contact.Fraction, tuning);
                ReflectFromFighter(projectile, contact.Normal, input.ForFighter(fighter.Id).Move,
                    tuning.Projectile.MaximumParrySteeringDegrees);
                events.Add(new SimulationEvent(state.Tick, SimulationEventType.OwnedProjectileRedirected,
                    projectile.Id, fighter.Id, impact, projectile.PowerLevel));
            }
            else if (defense == DefenseKind.Parry)
            {
                ClearOtherFighterContact(projectile, fighter.Id, contact.Fraction,
                    suppressedPairs, lastContactFractions);
                MarkParryContact(projectile, fighter, contact.Fraction, tuning);
                projectile.LastParrierId = fighter.Id;
                fighter.PostureTouchedTick = state.Tick;
                projectile.PowerLevel = Math.Min(ProjectileTuning.MaximumPowerLevel, projectile.PowerLevel + 1);
                projectile.DecayTicksRemaining = Math.Max(1,
                    SimMath.SecondsToTicks(tuning.Projectile.PowerDecaySecondsPerLevel, config.TickRateHz));
                projectile.SpeedDecaying = false;
                ReflectFromFighter(projectile, contact.Normal, input.ForFighter(fighter.Id).Move,
                    tuning.Projectile.MaximumParrySteeringDegrees);
                SetProjectileSpeed(projectile, tuning);
                if (tuning.Movement.UseFuelBoost)
                    fighter.FuelSeconds = Math.Min(tuning.Movement.FuelCapacitySeconds,
                        fighter.FuelSeconds + tuning.Movement.FuelCapacitySeconds / 3.0);
                else
                    fighter.DashCharges = Math.Min(MovementTuning.MaximumDashCharges, fighter.DashCharges + 1);
                events.Add(new SimulationEvent(state.Tick, SimulationEventType.PerfectParry,
                    projectile.Id, fighter.Id, impact, projectile.PowerLevel));
            }
            else if (defense == DefenseKind.Guard)
            {
                ClearOtherFighterContact(projectile, fighter.Id, contact.Fraction,
                    suppressedPairs, lastContactFractions);
                projectile.ReflectedContactMask |= 1 << fighter.Id;
                bool activeDash = fighter.DashTicksRemaining > 0 || fighter.DividerLoadTicksRemaining > 0;
                DefenseSystem.ApplyPostureDamage(state, fighter,
                    tuning.GetBlockPostureDamage(projectile.PowerLevel), tuning, config, events);
                if (activeDash && fighter.GuardBreakTicksRemaining > 0)
                    CancelDashPath(fighter, contact.Fraction, config);
                ReflectFromFighter(projectile, contact.Normal, Vec2.Zero, 0);
                events.Add(new SimulationEvent(state.Tick, SimulationEventType.GuardBlocked,
                    projectile.Id, fighter.Id, impact, fighter.Posture));
            }
            else if (defense == DefenseKind.Lethal)
            {
                if (!fighter.Eliminated)
                {
                    fighter.Eliminated = true;
                    events.Add(new SimulationEvent(state.Tick, SimulationEventType.FighterEliminated,
                        fighter.Id, projectile.Id, impact));
                }
            }
        }

        private static void ClearOtherFighterContact(ProjectileState projectile, int fighterId,
            double fraction, HashSet<long> suppressedPairs,
            Dictionary<long, double> lastContactFractions)
        {
            // A return from the other fighter is a fresh approach even when the
            // two reach zones overlap near the divider.
            int ownBit = 1 << fighterId;
            projectile.ReflectedContactMask &= ownBit;
            projectile.ParryContactMask &= ownBit;
            long otherPair = ((long)projectile.Id << 32) | (uint)(1 - fighterId);
            if (lastContactFractions.TryGetValue(otherPair, out double previousFraction) &&
                fraction > previousFraction + FractionEpsilon)
                suppressedPairs.Remove(otherPair);
        }

        private static void MarkParryContact(ProjectileState projectile, FighterState fighter,
            double fraction, CombatTuning tuning)
        {
            int bit = 1 << fighter.Id;
            projectile.ParryContactMask |= bit;
            double physicalRadius = tuning.Arena.ProjectileRadiusUnits +
                tuning.Arena.FighterRadiusUnits + SeparationEpsilon;
            if ((projectile.Position - SimMath.FighterPositionAt(fighter, fraction)).LengthSquared <=
                physicalRadius * physicalRadius)
                projectile.ReflectedContactMask |= bit;
        }

        private static void ReflectFromFighter(ProjectileState projectile, Vec2 normal,
            Vec2 steering, double maximumDegrees)
        {
            Vec2 unitNormal = normal.NormalizedOr(new Vec2(1, 0));
            Vec2 natural = Vec2.Dot(projectile.Velocity, unitNormal) < 0
                ? Vec2.Reflect(projectile.Velocity, unitNormal)
                : unitNormal * projectile.Velocity.Length;
            natural = natural.NormalizedOr(unitNormal);
            if (steering.LengthSquared > 1e-12 && maximumDegrees > 0)
            {
                Vec2 desired = steering.NormalizedOr(natural);
                double cross = natural.X * desired.Y - natural.Y * desired.X;
                double angle = Math.Atan2(cross, Vec2.Dot(natural, desired));
                double cap = maximumDegrees * Math.PI / 180.0;
                angle = SimMath.Clamp(angle, -cap, cap);
                double cosine = Math.Cos(angle);
                double sine = Math.Sin(angle);
                Vec2 steered = new Vec2(natural.X * cosine - natural.Y * sine,
                    natural.X * sine + natural.Y * cosine);
                if (Vec2.Dot(steered, unitNormal) > 1e-10) natural = steered;
            }
            projectile.Velocity = natural * projectile.Velocity.Length;
        }

        private static void CancelDashPath(FighterState fighter, double fraction, SimulationConfig config)
        {
            Vec2 atContact = SimMath.FighterPositionAt(fighter, fraction);
            fighter.PreviousPosition = atContact;
            fighter.MotionStartFraction = fraction;
            fighter.Velocity = new Vec2(0, fighter.Velocity.Y);
            fighter.Position = atContact + fighter.Velocity * ((1 - fraction) * config.TickDurationSeconds);
        }

        private static void ResolveWallContact(SimulationState state, CombatTuning tuning,
            IList<SimulationEvent> events, Contact contact)
        {
            ProjectileState projectile = contact.Projectile;
            if (contact.Kind == ContactKind.WallX)
            {
                double edge = contact.Normal.X > 0 ? tuning.Arena.ProjectileRadiusUnits :
                    tuning.Arena.WidthUnits - tuning.Arena.ProjectileRadiusUnits;
                projectile.Position = new Vec2(edge, projectile.Position.Y);
                projectile.Velocity = new Vec2(-projectile.Velocity.X, projectile.Velocity.Y);
            }
            else
            {
                double edge = contact.Normal.Y > 0 ? tuning.Arena.ProjectileRadiusUnits :
                    tuning.Arena.HeightUnits - tuning.Arena.ProjectileRadiusUnits;
                projectile.Position = new Vec2(projectile.Position.X, edge);
                projectile.Velocity = new Vec2(projectile.Velocity.X, -projectile.Velocity.Y);
            }
            events.Add(new SimulationEvent(state.Tick, SimulationEventType.ProjectileBounced,
                projectile.Id, position: projectile.Position));
        }

        private static void ResolveCompoundBoundaryContacts(List<Contact> batch, DefenseKind[] defenses)
        {
            for (int fighterIndex = 0; fighterIndex < batch.Count; fighterIndex++)
            {
                Contact fighterContact = batch[fighterIndex];
                if (fighterContact.Kind != ContactKind.Fighter ||
                    (defenses[fighterIndex] != DefenseKind.Parry &&
                     defenses[fighterIndex] != DefenseKind.OwnedRedirect &&
                     defenses[fighterIndex] != DefenseKind.Guard))
                    continue;

                ProjectileState projectile = fighterContact.Projectile;
                if (Vec2.Dot(projectile.Velocity, fighterContact.Normal) > 1e-10) continue;
                for (int wallIndex = 0; wallIndex < batch.Count; wallIndex++)
                {
                    Contact wall = batch[wallIndex];
                    if (wall.Projectile != projectile) continue;
                    double speed = projectile.Velocity.Length;
                    if (wall.Kind == ContactKind.WallX)
                    {
                        double sign = fighterContact.Normal.Y >= 0 ? 1 : -1;
                        projectile.Velocity = new Vec2(0, sign * speed);
                        break;
                    }
                    if (wall.Kind == ContactKind.WallY)
                    {
                        double sign = fighterContact.Normal.X >= 0 ? 1 : -1;
                        projectile.Velocity = new Vec2(sign * speed, 0);
                        break;
                    }
                }
            }
        }

        private static void ValidateProjectileBounds(List<ProjectileState> projectiles, CombatTuning tuning)
        {
            double radius = tuning.Arena.ProjectileRadiusUnits;
            foreach (ProjectileState projectile in projectiles)
            {
                Vec2 point = projectile.Position;
                if (!TuningValidation.IsFinite(point.X) || !TuningValidation.IsFinite(point.Y) ||
                    point.X < radius - 1e-7 || point.X > tuning.Arena.WidthUnits - radius + 1e-7 ||
                    point.Y < radius - 1e-7 || point.Y > tuning.Arena.HeightUnits - radius + 1e-7)
                    throw new InvalidOperationException("Projectile left the arena after continuous collision resolution.");
                projectile.Position = new Vec2(
                    SimMath.Clamp(point.X, radius, tuning.Arena.WidthUnits - radius),
                    SimMath.Clamp(point.Y, radius, tuning.Arena.HeightUnits - radius));
            }
        }

        private static void RefreshContactMasks(SimulationState state, CombatTuning tuning)
        {
            double physicalClearRadius = tuning.Arena.ProjectileRadiusUnits +
                tuning.Arena.FighterRadiusUnits + SeparationEpsilon;
            double physicalClearRadiusSquared = physicalClearRadius * physicalClearRadius;
            double parryClearRadius = physicalClearRadius + tuning.Defense.ParryExtraReachUnits;
            double parryClearRadiusSquared = parryClearRadius * parryClearRadius;
            foreach (ProjectileState projectile in state.Projectiles)
            {
                for (int fighterId = 0; fighterId < 2; fighterId++)
                {
                    int bit = 1 << fighterId;
                    double separationSquared =
                        (projectile.Position - state.Fighter(fighterId).Position).LengthSquared;
                    if (separationSquared > physicalClearRadiusSquared)
                        projectile.ReflectedContactMask &= ~bit;
                    if (separationSquared > parryClearRadiusSquared)
                    {
                        projectile.ParryContactMask &= ~bit;
                        if (projectile.LastParrierId == fighterId)
                            projectile.LastParrierId = -1;
                    }
                }
            }
        }
    }
}
