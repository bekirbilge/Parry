using System;
using System.Collections.Generic;
using Game.Core;

internal static class CombatTests
{
    public static void Run()
    {
        PerfectParryAtNinetyNineRestoresOneDashWithoutHealing();
        PerfectParryRestorationStopsAtThreeWithoutHealing();
        ParryWorksDuringDashInvulnerabilityAndSteersThirtyDegrees();
        GuardRecoveryImpactDelayAndBreak();
        GuardBreakInterruptsActiveDashAndBlocksNextDash();
        LaterProjectileDuringSameTickBreakKills();
        SimultaneousBlocksKeepContactTimeGuardLegality();
        SameTimeResolutionDoesNotDependOnProjectileListOrder();
        BodyShoveCanCauseProjectileDeathLaterInSameTick();
        BodyDamageCanBreakPostureWithoutDirectDeath();
        SimultaneousWallParryEscapesAlongWall();
        SimultaneousWallOwnedRedirectEscapesAlongWall();
        FastOpponentProjectileHitsMovingBoostPath();
        MovingBoostGrazeUsesParryReachOnlyDuringActiveWindow();
        ParryReachBoundaryAndExpiredWindowKeepPhysicalHits();
        ExpandedParryOverlapDoesNotHideLaterPhysicalGuard();
        CloseOpponentReturnClearsPriorParryOverlap();
        OwnedRedirectCannotRepeatDuringOverlap();
        DashChaseCannotFarmOverlappingParries();
        DashChaseCannotRepeatOverlappingGuardBlocks();
        InvulnerabilityOverlapIsLethalAfterExpiry();
    }

    private static SimulationState State()
    {
        var state = new SimulationState { Tick = 1, Phase = MatchPhase.Active };
        state.Fighter(0).Position = state.Fighter(0).PreviousPosition = new Vec2(25, 10);
        state.Fighter(1).Position = state.Fighter(1).PreviousPosition = new Vec2(75, 10);
        state.Fighter(0).DashCharges = state.Fighter(1).DashCharges = MovementTuning.MaximumDashCharges;
        return state;
    }

    private static ProjectileState Inbound(int id, bool fromLeft = true, int level = 0)
    {
        return new ProjectileState
        {
            Id = id,
            Position = fromLeft ? new Vec2(22.7, 10) : new Vec2(27.3, 10),
            Velocity = fromLeft ? new Vec2(24, 0) : new Vec2(-24, 0),
            PowerLevel = level,
            DecayTicksRemaining = level == 0 ? 0 : 60
        };
    }

    private static List<SimulationEvent> Advance(SimulationState state, FighterInput left,
        CombatTuning? tuning = null, SimulationConfig? config = null)
    {
        var events = new List<SimulationEvent>();
        CombatSystem.Advance(state, new SimulationInput(state.Tick, left, default),
            tuning ?? CombatTuning.Default, config ?? new SimulationConfig(), events);
        return events;
    }

    private static void PerfectParryAtNinetyNineRestoresOneDashWithoutHealing()
    {
        SimulationState state = State();
        FighterState fighter = state.Fighter(0);
        fighter.Posture = 99;
        fighter.DashCharges = 0;
        state.Projectiles.Add(Inbound(4));
        List<SimulationEvent> events = Advance(state, new FighterInput(Vec2.Zero, defensePressed: true));
        Assert(Count(events, SimulationEventType.PerfectParry) == 1, "Parry at 99 posture must be valid.");
        Assert(fighter.Posture == 99 && fighter.PostureTouchedTick == state.Tick,
            "Perfect parry must neither damage nor recover posture.");
        Assert(fighter.DashCharges == 1, "A perfect parry restores exactly one dash charge.");
        Assert(state.Projectiles[0].PowerLevel == 1 && state.Projectiles[0].DecayTicksRemaining == 240,
            "Perfect parry must charge and reset decay.");
        Assert(state.Projectiles[0].Velocity.X < 0, "Parried projectile must reflect away from the fighter.");
    }

    private static void PerfectParryRestorationStopsAtThreeWithoutHealing()
    {
        foreach (int startingCharges in new[] { MovementTuning.MaximumDashCharges - 1, MovementTuning.MaximumDashCharges })
        {
            SimulationState state = State();
            FighterState fighter = state.Fighter(0);
            fighter.Posture = 99;
            fighter.DashCharges = startingCharges;
            state.Projectiles.Add(Inbound(40 + startingCharges));
            List<SimulationEvent> events = Advance(state, new FighterInput(Vec2.Zero, defensePressed: true));
            Assert(Count(events, SimulationEventType.PerfectParry) == 1 &&
                fighter.DashCharges == MovementTuning.MaximumDashCharges,
                "Parry must restore exactly one charge up to the three-charge cap.");
            Assert(fighter.Posture == 99 && fighter.PostureTouchedTick == state.Tick,
                "Parrying at the charge cap must not heal posture.");
        }
    }

    private static void ParryWorksDuringDashInvulnerabilityAndSteersThirtyDegrees()
    {
        SimulationState state = State();
        state.Fighter(0).DashInvulnerabilityTicksRemaining = 5;
        state.Fighter(0).DashCharges = 1;
        state.Projectiles.Add(Inbound(5));
        List<SimulationEvent> events = Advance(state,
            new FighterInput(new Vec2(0, 1), defensePressed: true));
        Vec2 direction = state.Projectiles[0].Velocity.NormalizedOr(Vec2.Zero);
        Assert(Count(events, SimulationEventType.PerfectParry) == 1 && !state.Fighter(0).Eliminated,
            "Legal parry has priority over dash invulnerability.");
        Assert(state.Fighter(0).DashCharges == 2, "A parry during dash invulnerability restores exactly one charge.");
        Assert(Math.Abs(direction.X + Math.Cos(Math.PI / 6)) < 1e-10 &&
            Math.Abs(direction.Y - 0.5) < 1e-10,
            "Up input must steer the natural left reflection by exactly 30 degrees.");

        SimulationState iframe = State();
        iframe.Fighter(0).DashInvulnerabilityTicksRemaining = 1;
        iframe.Projectiles.Add(Inbound(6));
        List<SimulationEvent> iframeEvents = Advance(iframe, default);
        Assert(!iframe.Fighter(0).Eliminated && Count(iframeEvents, SimulationEventType.FighterEliminated) == 0,
            "Dash invulnerability must ignore an otherwise lethal projectile.");
        Assert(iframe.Fighter(0).DashInvulnerabilityTicksRemaining == 0,
            "Invulnerability consumes its current contact tick before expiring.");
    }

    private static void GuardRecoveryImpactDelayAndBreak()
    {
        SimulationState guarded = State();
        guarded.Fighter(0).Posture = 50;
        guarded.Fighter(0).DefenseHeldPrevious = true;
        guarded.Fighter(0).DefenseHoldTicks = 6;
        Advance(guarded, new FighterInput(Vec2.Zero, defenseHeld: true));
        Assert(Math.Abs(guarded.Fighter(0).Posture - 49.75) < 1e-10,
            "Guard idle recovery must be 15 posture per second at 60 Hz.");

        SimulationState unguarded = State();
        unguarded.Fighter(0).Posture = 50;
        Advance(unguarded, default);
        Assert(Math.Abs(unguarded.Fighter(0).Posture - (50 - 5.0 / 60)) < 1e-10,
            "Unguarded idle recovery must be 5 posture per second at 60 Hz.");

        SimulationState impact = State();
        impact.Fighter(0).Posture = 70;
        impact.Fighter(0).DefenseHeldPrevious = true;
        impact.Fighter(0).DefenseHoldTicks = 6;
        impact.Projectiles.Add(Inbound(7));
        List<SimulationEvent> impactEvents = Advance(impact,
            new FighterInput(Vec2.Zero, defenseHeld: true));
        Assert(Count(impactEvents, SimulationEventType.GuardBlocked) == 1 &&
            Count(impactEvents, SimulationEventType.GuardBroken) == 1 &&
            impact.Fighter(0).Posture == 100,
            "A level-zero guard hit at 70 posture must break exactly at 100 without same-tick healing.");
        Assert(impact.Fighter(0).GuardBreakTicksRemaining == 36,
            "A new guard break must last the full 0.6 seconds.");
        impact.Tick++;
        impact.Projectiles[0].Position = new Vec2(22.7, 10);
        impact.Projectiles[0].Velocity = new Vec2(24, 0);
        List<SimulationEvent> brokenEvents = Advance(impact,
            new FighterInput(Vec2.Zero, defensePressed: true, defenseHeld: true));
        Assert(impact.Fighter(0).Eliminated && Count(brokenEvents, SimulationEventType.FighterEliminated) == 1,
            "A projectile during guard break must eliminate despite attempted defense.");
    }

    private static void SimultaneousBlocksKeepContactTimeGuardLegality()
    {
        SimulationState state = State();
        state.Fighter(0).Posture = 80;
        state.Fighter(0).DefenseHeldPrevious = true;
        state.Fighter(0).DefenseHoldTicks = 6;
        state.Fighter(0).DashTicksRemaining = 3;
        state.Fighter(0).DashInvulnerabilityTicksRemaining = 3;
        state.Projectiles.Add(Inbound(10));
        state.Projectiles.Add(Inbound(11, fromLeft: false));
        List<SimulationEvent> events = Advance(state, new FighterInput(Vec2.Zero, defenseHeld: true));
        Assert(Count(events, SimulationEventType.GuardBlocked) == 2 && !state.Fighter(0).Eliminated,
            "Simultaneous contacts must both use guard legality from the shared contact instant.");
        Assert(state.Fighter(0).Posture == 100 && Count(events, SimulationEventType.GuardBroken) == 1,
            "Both block damages accumulate but guard break is emitted once.");
        Assert(state.Fighter(0).DashTicksRemaining == 0 &&
            state.Fighter(0).DashInvulnerabilityTicksRemaining == 0,
            "Same-time legal blocks must stay valid even though the first break cancels dash immunity.");
    }

    private static SimulationState ActiveDashGuardFixture(bool includeLaterProjectile)
    {
        SimulationState state = State();
        FighterState fighter = state.Fighter(0);
        fighter.Position = new Vec2(25 + 50.0 / 60.0, 10);
        fighter.PreviousPosition = new Vec2(25, 10);
        fighter.Velocity = new Vec2(50, 0);
        fighter.DashDirection = new Vec2(1, 0);
        fighter.DashTicksRemaining = 3;
        fighter.DashInvulnerabilityTicksRemaining = 3;
        fighter.DashSequence = 1;
        fighter.DefenseHeldPrevious = true;
        fighter.DefenseHoldTicks = 6;
        fighter.Posture = 70;
        state.Projectiles.Add(new ProjectileState
        {
            Id = 50, Position = new Vec2(27.3, 10), Velocity = new Vec2(-24, 0)
        });
        if (includeLaterProjectile)
            state.Projectiles.Add(new ProjectileState
            {
                Id = 51, Position = new Vec2(27.55, 10), Velocity = new Vec2(-24, 0)
            });
        return state;
    }

    private static void GuardBreakInterruptsActiveDashAndBlocksNextDash()
    {
        SimulationState state = ActiveDashGuardFixture(includeLaterProjectile: false);
        FighterState fighter = state.Fighter(0);
        List<SimulationEvent> events = Advance(state, new FighterInput(Vec2.Zero, defenseHeld: true));
        Assert(Count(events, SimulationEventType.GuardBlocked) == 1 &&
            Count(events, SimulationEventType.GuardBroken) == 1 && !fighter.Eliminated,
            "An established guard must block before dash immunity and break at full posture.");
        Assert(fighter.DashTicksRemaining == 0 && fighter.DashInvulnerabilityTicksRemaining == 0 &&
            fighter.Position.X < 25.3,
            "Guard break must immediately cancel active dash, immunity, and remaining dash travel.");

        int charges = fighter.DashCharges;
        int sequence = fighter.DashSequence;
        Vec2 before = fighter.Position;
        state.Tick++;
        var nextEvents = new List<SimulationEvent>();
        MovementSystem.AdvanceKinematics(state,
            new SimulationInput(state.Tick, new FighterInput(new Vec2(1, 0), dashPressed: true), default),
            CombatTuning.Default, new SimulationConfig(), nextEvents);
        Assert(fighter.DashTicksRemaining == 0 && fighter.DashInvulnerabilityTicksRemaining == 0 &&
            fighter.DashCharges == charges && fighter.DashSequence == sequence &&
            fighter.Position.X - before.X < 0.2,
            "The following movement tick must reject a new dash throughout guard break.");
    }

    private static void LaterProjectileDuringSameTickBreakKills()
    {
        SimulationState state = ActiveDashGuardFixture(includeLaterProjectile: true);
        List<SimulationEvent> events = Advance(state, new FighterInput(Vec2.Zero, defenseHeld: true));
        Assert(Count(events, SimulationEventType.GuardBlocked) == 1 &&
            Count(events, SimulationEventType.GuardBroken) == 1 &&
            Count(events, SimulationEventType.FighterEliminated) == 1 &&
            state.Fighter(0).Eliminated,
            "A later contact in the break tick must kill after dash immunity is cancelled.");
        Assert(events.FindIndex(item => item.Type == SimulationEventType.GuardBroken) <
            events.FindIndex(item => item.Type == SimulationEventType.FighterEliminated),
            "The lethal contact must happen after the guard-break event.");
    }

    private static void SameTimeResolutionDoesNotDependOnProjectileListOrder()
    {
        SimulationState forward = State();
        forward.Fighter(0).Posture = 99;
        forward.Fighter(0).DashCharges = 0;
        forward.Projectiles.Add(Inbound(22));
        forward.Projectiles.Add(Inbound(12, fromLeft: false));
        SimulationState reversed = forward.Clone();
        reversed.Projectiles.Reverse();
        var input = new FighterInput(Vec2.Zero, defensePressed: true);
        List<SimulationEvent> a = Advance(forward, input);
        List<SimulationEvent> b = Advance(reversed, input);
        Assert(Count(a, SimulationEventType.PerfectParry) == 2 &&
            Count(b, SimulationEventType.PerfectParry) == 2 &&
            forward.Fighter(0).Posture == 99 && reversed.Fighter(0).Posture == 99 &&
            forward.Fighter(0).DashCharges == 2 && reversed.Fighter(0).DashCharges == 2,
            "Same-time parries must survive at 99 posture and restore one dash per contact up to cap.");
        Assert(a[0].EntityId == b[0].EntityId && a[1].EntityId == b[1].EntityId,
            "Same-time contact events must be sorted by stable projectile ID.");
    }

    private static SimulationState BodyFixture()
    {
        SimulationState state = State();
        FighterState left = state.Fighter(0);
        FighterState right = state.Fighter(1);
        left.PreviousPosition = new Vec2(47, 10);
        left.Position = new Vec2(49, 10);
        left.Grounded = right.Grounded = true;
        left.DashSequence = 1;
        left.DashDirection = new Vec2(1, 0);
        left.DashTicksRemaining = 3;
        right.PreviousPosition = right.Position = new Vec2(51.5, 10);
        return state;
    }

    private static void BodyShoveCanCauseProjectileDeathLaterInSameTick()
    {
        SimulationState state = BodyFixture();
        state.Projectiles.Add(new ProjectileState
        {
            Id = 30, Position = new Vec2(54.02, 10), Velocity = new Vec2(-1, 0)
        });
        List<SimulationEvent> events = Advance(state, default);
        Assert(Count(events, SimulationEventType.BodyImpact) == 1 &&
            state.Fighter(1).Eliminated && Count(events, SimulationEventType.FighterEliminated) == 1,
            "A body impulse must rewrite the remaining fighter sweep and permit a later projectile kill.");
        Assert(events.FindIndex(item => item.Type == SimulationEventType.BodyImpact) <
            events.FindIndex(item => item.Type == SimulationEventType.FighterEliminated),
            "The body impact must precede the later projectile elimination in event order.");
    }

    private static void BodyDamageCanBreakPostureWithoutDirectDeath()
    {
        SimulationState state = BodyFixture();
        state.Fighter(1).Posture = 75;
        List<SimulationEvent> events = Advance(state, default);
        Assert(state.Fighter(1).Posture == 100 &&
            Count(events, SimulationEventType.GuardBroken) == 1 &&
            Count(events, SimulationEventType.BodyImpact) == 1 &&
            !state.Fighter(1).Eliminated,
            "Dash-body damage may break posture but must never directly eliminate.");
    }

    private static void SimultaneousWallParryEscapesAlongWall()
    {
        SimulationState state = State();
        FighterState defender = state.Fighter(1);
        double reach = CombatTuning.Default.Arena.ProjectileRadiusUnits +
            CombatTuning.Default.Arena.FighterRadiusUnits +
            CombatTuning.Default.Defense.ParryExtraReachUnits;
        double wall = CombatTuning.Default.Arena.WidthUnits -
            CombatTuning.Default.Arena.ProjectileRadiusUnits;
        defender.Position = defender.PreviousPosition = new Vec2(wall - reach + 1e-6, 10);
        state.Projectiles.Add(new ProjectileState
        {
            Id = 31, Position = new Vec2(wall, 10),
            Velocity = new Vec2(24, 0)
        });
        var events = new List<SimulationEvent>();
        CombatSystem.Advance(state, new SimulationInput(state.Tick, default,
            new FighterInput(Vec2.Zero, defensePressed: true)),
            CombatTuning.Default, new SimulationConfig(), events);
        Assert(Count(events, SimulationEventType.PerfectParry) == 1 &&
            Count(events, SimulationEventType.ProjectileBounced) == 1,
            "The fighter and wall must both register their same-time contact.");
        Assert(Math.Abs(state.Projectiles[0].Velocity.X) < 1e-8 &&
            state.Projectiles[0].Velocity.Y > 0 && !defender.Eliminated,
            "The compound reflection must leave along the wall away from the fighter.");
    }

    private static void SimultaneousWallOwnedRedirectEscapesAlongWall()
    {
        SimulationState state = State();
        FighterState owner = state.Fighter(1);
        double reach = CombatTuning.Default.Arena.ProjectileRadiusUnits +
            CombatTuning.Default.Arena.FighterRadiusUnits +
            CombatTuning.Default.Defense.ParryExtraReachUnits;
        double wall = CombatTuning.Default.Arena.WidthUnits -
            CombatTuning.Default.Arena.ProjectileRadiusUnits;
        owner.Position = owner.PreviousPosition = new Vec2(wall - reach + 1e-6, 10);
        state.Projectiles.Add(new ProjectileState
        {
            Id = 32, Position = new Vec2(wall, 10),
            Velocity = new Vec2(24, 0), LastParrierId = owner.Id,
            PowerLevel = 1, DecayTicksRemaining = 50
        });
        var events = new List<SimulationEvent>();
        CombatSystem.Advance(state, new SimulationInput(state.Tick, default,
            new FighterInput(Vec2.Zero, defensePressed: true)),
            CombatTuning.Default, new SimulationConfig(), events);
        ProjectileState shot = state.Projectiles[0];
        Assert(Count(events, SimulationEventType.OwnedProjectileRedirected) == 1 &&
            Count(events, SimulationEventType.ProjectileBounced) == 1 &&
            Count(events, SimulationEventType.PerfectParry) == 0,
            "Owned redirect and wall bounce must both resolve at their shared contact time.");
        Assert(Math.Abs(shot.Velocity.X) < 1e-8 && shot.Velocity.Y > 0 &&
            shot.LastParrierId == owner.Id && shot.PowerLevel == 1 &&
            shot.DecayTicksRemaining == 49 && !owner.Eliminated,
            "Compound owned redirect must escape along wall without parry rewards.");
    }

    private static void FastOpponentProjectileHitsMovingBoostPath()
    {
        foreach (bool pressDefense in new[] { false, true })
        {
            SimulationState state = State();
            FighterState fighter = state.Fighter(0);
            fighter.PreviousPosition = new Vec2(25, 10);
            fighter.Position = fighter.PreviousPosition +
                new Vec2(CombatTuning.Default.Movement.DashSpeedUnitsPerSecond / 60, 0);
            fighter.DashTicksRemaining = 2;
            state.Projectiles.Add(new ProjectileState
            {
                Id = 33, Position = new Vec2(33, 10), Velocity = new Vec2(-300, 0),
                LastParrierId = 1
            });
            List<SimulationEvent> events = Advance(state,
                pressDefense ? new FighterInput(Vec2.Zero, defensePressed: true) : default);
            Assert(pressDefense
                    ? Count(events, SimulationEventType.PerfectParry) == 1 &&
                      Count(events, SimulationEventType.OwnedProjectileRedirected) == 0 &&
                      !fighter.Eliminated && state.Projectiles[0].LastParrierId == 0
                    : Count(events, SimulationEventType.FighterEliminated) == 1 && fighter.Eliminated,
                "Swept contacts must resolve a fast opponent shot against a moving boost path.");
        }
    }

    private static void MovingBoostGrazeUsesParryReachOnlyDuringActiveWindow()
    {
        foreach (bool invulnerable in new[] { false, true })
        {
            foreach (bool pressDefense in new[] { false, true })
            {
                SimulationState state = State();
                FighterState fighter = state.Fighter(0);
                fighter.PreviousPosition = new Vec2(25, 10);
                fighter.Position = new Vec2(25 + CombatTuning.Default.Movement.DashSpeedUnitsPerSecond / 60, 10);
                fighter.DashTicksRemaining = 2;
                fighter.DashInvulnerabilityTicksRemaining = invulnerable ? 2 : 0;
                state.Projectiles.Add(new ProjectileState
                {
                    Id = 63, Position = new Vec2(33, 12.5), Velocity = new Vec2(-300, 0),
                    LastParrierId = 1
                });
                List<SimulationEvent> events = Advance(state,
                    pressDefense ? new FighterInput(Vec2.Zero, defensePressed: true) : default);
                Assert(Count(events, SimulationEventType.PerfectParry) == (pressDefense ? 1 : 0) &&
                    Count(events, SimulationEventType.FighterEliminated) == 0 &&
                    Count(events, SimulationEventType.GuardBlocked) == 0 && !fighter.Eliminated,
                    "Swept parry reach must catch a moving boost graze during or after invulnerability; " +
                    "the same graze without parry must stay outside the physical hitbox.");
            }
        }
    }

    private static void ParryReachBoundaryAndExpiredWindowKeepPhysicalHits()
    {
        double physical = CombatTuning.Default.Arena.FighterRadiusUnits +
            CombatTuning.Default.Arena.ProjectileRadiusUnits;
        double reach = physical + CombatTuning.Default.Defense.ParryExtraReachUnits;
        foreach (bool inside in new[] { false, true })
        {
            SimulationState state = State();
            state.Projectiles.Add(new ProjectileState
            {
                Id = 64, Position = new Vec2(25 + reach + (inside ? -0.01 : 0.01), 10),
                Velocity = Vec2.Zero
            });
            List<SimulationEvent> events = Advance(state,
                new FighterInput(Vec2.Zero, defensePressed: true));
            Assert(Count(events, SimulationEventType.PerfectParry) == (inside ? 1 : 0),
                "Parry reach must have a sharp configured boundary.");
        }

        SimulationState expired = State();
        expired.Projectiles.Add(new ProjectileState
        {
            Id = 65, Position = new Vec2(25 + physical + 0.2, 10), Velocity = Vec2.Zero
        });
        Advance(expired, default);
        Assert(!expired.Fighter(0).Eliminated,
            "Expired parry must not turn the enlarged radius into a lethal body hitbox.");
        expired.Projectiles[0].Position = new Vec2(25 + physical - 0.1, 10);
        expired.Tick++;
        List<SimulationEvent> physicalHit = Advance(expired, default);
        Assert(expired.Fighter(0).Eliminated && Count(physicalHit, SimulationEventType.FighterEliminated) == 1,
            "The original physical radius must remain lethal after the parry window expires.");
    }

    private static void ExpandedParryOverlapDoesNotHideLaterPhysicalGuard()
    {
        SimulationState state = State();
        FighterState fighter = state.Fighter(0);
        ProjectileState shot = new ProjectileState
        {
            Id = 66, Position = new Vec2(25 + 2.5, 10), Velocity = Vec2.Zero,
            LastParrierId = 1
        };
        state.Projectiles.Add(shot);
        List<SimulationEvent> parry = Advance(state,
            new FighterInput(Vec2.Zero, defensePressed: true));
        Assert(Count(parry, SimulationEventType.PerfectParry) == 1 &&
            shot.ParryContactMask == 1 && shot.ReflectedContactMask == 0,
            "An expanded parry must mark only its expanded overlap when bodies did not touch.");
        fighter.ParryTicksRemaining = 0;
        fighter.DefenseHeldPrevious = true;
        fighter.DefenseHoldTicks = 6;
        shot.LastParrierId = 1;
        shot.Position = new Vec2(25 + 2.0, 10);
        shot.Velocity = Vec2.Zero;
        state.Tick++;
        List<SimulationEvent> guard = Advance(state,
            new FighterInput(Vec2.Zero, defenseHeld: true));
        Assert(Count(guard, SimulationEventType.GuardBlocked) == 1 &&
            !fighter.Eliminated && shot.ReflectedContactMask == 1,
            "After parry expires, a new physical guard contact inside the larger overlap must still resolve.");
    }

    private static void CloseOpponentReturnClearsPriorParryOverlap()
    {
        SimulationState state = State();
        FighterState left = state.Fighter(0);
        FighterState right = state.Fighter(1);
        var config = new SimulationConfig(tickRateHz: 120);
        left.Position = left.PreviousPosition = new Vec2(48.5, 10);
        right.Position = right.PreviousPosition = new Vec2(51.5, 10);
        right.DashInvulnerabilityTicksRemaining = 1;
        state.Projectiles.Add(new ProjectileState
        {
            Id = 67, Position = new Vec2(50.7, 10), Velocity = new Vec2(-24, 0)
        });
        List<SimulationEvent> first = Advance(state,
            new FighterInput(Vec2.Zero, defensePressed: true), config: config);
        ProjectileState shot = state.Projectiles[0];
        Assert(Count(first, SimulationEventType.PerfectParry) == 1 &&
            shot.ParryContactMask == 1 && !right.Eliminated,
            "The first close-range parry must leave an expanded overlap at the left fighter.");

        left.ParryTicksRemaining = 0;
        left.DashInvulnerabilityTicksRemaining = 1;
        state.Tick++;
        var second = new List<SimulationEvent>();
        CombatSystem.Advance(state, new SimulationInput(state.Tick, default,
            new FighterInput(Vec2.Zero, defensePressed: true)),
            CombatTuning.Default, config, second);
        Assert(Count(second, SimulationEventType.PerfectParry) == 1 &&
            shot.LastParrierId == 1 && (shot.ParryContactMask & 1) == 0,
            "An opponent return must clear the first fighter's stale expanded overlap.");

        state.Tick++;
        right.ParryTicksRemaining = 0;
        List<SimulationEvent> returned = Advance(state,
            new FighterInput(Vec2.Zero, defensePressed: true), config: config);
        Assert(Count(returned, SimulationEventType.PerfectParry) == 1 &&
            shot.LastParrierId == 0 && !left.Eliminated,
            "The first fighter must be able to parry a returned shot while both reach zones overlap.");
    }

    private static void OwnedRedirectCannotRepeatDuringOverlap()
    {
        SimulationState state = State();
        FighterState owner = state.Fighter(0);
        owner.DashCharges = 0;
        state.Projectiles.Add(new ProjectileState
        {
            Id = 34, Position = new Vec2(26, 10), Velocity = new Vec2(-24, 0),
            LastParrierId = owner.Id, PowerLevel = 1, DecayTicksRemaining = 50
        });
        List<SimulationEvent> first = Advance(state,
            new FighterInput(Vec2.Zero, defensePressed: true));
        Assert(Count(first, SimulationEventType.OwnedProjectileRedirected) == 1 &&
            state.Projectiles[0].ParryContactMask == 1,
            "First owned redirect must mark its continuous overlap.");
        state.Tick++;
        List<SimulationEvent> second = Advance(state,
            new FighterInput(Vec2.Zero, defensePressed: true));
        Assert(Count(second, SimulationEventType.OwnedProjectileRedirected) == 0 &&
            Count(second, SimulationEventType.PerfectParry) == 0 &&
            owner.DashCharges == 0 && state.Projectiles[0].PowerLevel == 1,
            "Repeated presses during one overlap must not redirect or reward it again.");
    }

    private static void DashChaseCannotFarmOverlappingParries()
    {
        SimulationState state = State();
        FighterState fighter = state.Fighter(0);
        state.Projectiles.Add(new ProjectileState
        {
            Id = 60, Position = new Vec2(27.3, 10), Velocity = new Vec2(-24, 0)
        });
        var events = new List<SimulationEvent>();
        var config = new SimulationConfig();
        MovementSystem.AdvanceKinematics(state,
            new SimulationInput(state.Tick,
                new FighterInput(new Vec2(1, 0), dashPressed: true, defensePressed: true, defenseHeld: true),
                default), CombatTuning.Default, config, events);
        CombatSystem.Advance(state,
            new SimulationInput(state.Tick,
                new FighterInput(new Vec2(1, 0), dashPressed: true, defensePressed: true, defenseHeld: true),
                default), CombatTuning.Default, config, events);
        Assert(Count(events, SimulationEventType.PerfectParry) == 1 &&
            state.Projectiles[0].PowerLevel == 1 && state.Projectiles[0].ParryContactMask == 1,
            "Initial dash parry must charge once and retain its overlapping contact marker.");
        int charges = fighter.DashCharges;
        for (int tick = 0; tick < 3; tick++)
        {
            state.Tick++;
            events.Clear();
            var input = new SimulationInput(state.Tick,
                new FighterInput(new Vec2(1, 0), defenseHeld: true), default);
            MovementSystem.AdvanceKinematics(state, input, CombatTuning.Default, config, events);
            CombatSystem.Advance(state, input, CombatTuning.Default, config, events);
            Assert(Count(events, SimulationEventType.PerfectParry) == 0 &&
                state.Projectiles[0].PowerLevel == 1 && fighter.DashCharges == charges,
                "One continuous reflected overlap must not grant another parry or dash charge.");
        }

        fighter.Position = fighter.PreviousPosition = new Vec2(25, 10);
        fighter.MotionStartFraction = 0;
        fighter.DashTicksRemaining = 0;
        fighter.DashInvulnerabilityTicksRemaining = 0;
        state.Projectiles[0].Position = new Vec2(30, 10);
        state.Projectiles[0].Velocity = new Vec2(27.6, 0);
        state.Tick++;
        Advance(state, default);
        Assert(state.Projectiles[0].ReflectedContactMask == 0 &&
            state.Projectiles[0].ParryContactMask == 0,
            "Reflected contact must clear only after actual geometric separation.");
        state.Projectiles[0].Position = new Vec2(22.7, 10);
        state.Projectiles[0].Velocity = new Vec2(24, 0);
        state.Projectiles[0].LastParrierId = 1; // Opponent returned it; our own shot is immune.
        state.Tick++;
        List<SimulationEvent> freshEvents = Advance(state,
            new FighterInput(Vec2.Zero, defensePressed: true));
        Assert(Count(freshEvents, SimulationEventType.PerfectParry) == 1 &&
            state.Projectiles[0].PowerLevel == 2,
            "A genuinely fresh contact after separation must parry and charge normally.");
    }

    private static void DashChaseCannotRepeatOverlappingGuardBlocks()
    {
        SimulationState state = State();
        FighterState fighter = state.Fighter(0);
        fighter.DashTicksRemaining = 3;
        fighter.DashDirection = new Vec2(1, 0);
        fighter.DashInvulnerabilityTicksRemaining = 3;
        fighter.DefenseHeldPrevious = true;
        fighter.DefenseHoldTicks = 6;
        state.Projectiles.Add(new ProjectileState
        {
            Id = 61, Position = new Vec2(27.3, 10), Velocity = new Vec2(-24, 0)
        });
        var config = new SimulationConfig();
        var input = new SimulationInput(state.Tick,
            new FighterInput(new Vec2(1, 0), defenseHeld: true), default);
        var events = new List<SimulationEvent>();
        MovementSystem.AdvanceKinematics(state, input, CombatTuning.Default, config, events);
        CombatSystem.Advance(state, input, CombatTuning.Default, config, events);
        Assert(Count(events, SimulationEventType.GuardBlocked) == 1 &&
            fighter.Posture == 30 && state.Projectiles[0].ReflectedContactMask == 1,
            "Initial guard block must mark the reflected overlap.");
        state.Tick++;
        events.Clear();
        input = new SimulationInput(state.Tick,
            new FighterInput(new Vec2(1, 0), defenseHeld: true), default);
        MovementSystem.AdvanceKinematics(state, input, CombatTuning.Default, config, events);
        CombatSystem.Advance(state, input, CombatTuning.Default, config, events);
        Assert(Count(events, SimulationEventType.GuardBlocked) == 0 &&
            fighter.Posture <= 30 && !fighter.Eliminated,
            "A continuous guard overlap must not add posture damage on a later tick.");
    }

    private static void InvulnerabilityOverlapIsLethalAfterExpiry()
    {
        SimulationState state = State();
        FighterState fighter = state.Fighter(0);
        fighter.DashInvulnerabilityTicksRemaining = 1;
        state.Projectiles.Add(new ProjectileState
        {
            Id = 62, Position = new Vec2(26, 10), Velocity = Vec2.Zero
        });
        List<SimulationEvent> first = Advance(state, default);
        Assert(!fighter.Eliminated && Count(first, SimulationEventType.FighterEliminated) == 0 &&
            state.Projectiles[0].ReflectedContactMask == 0,
            "Invulnerability must ignore the current contact without persistently suppressing it.");
        state.Tick++;
        List<SimulationEvent> next = Advance(state, default);
        Assert(fighter.Eliminated && Count(next, SimulationEventType.FighterEliminated) == 1,
            "An overlapping projectile must become lethal when invulnerability expires.");

        SimulationState parrying = State();
        parrying.Fighter(0).DashInvulnerabilityTicksRemaining = 1;
        parrying.Projectiles.Add(new ProjectileState
        {
            Id = 68, Position = new Vec2(26, 10), Velocity = Vec2.Zero
        });
        Advance(parrying, default);
        parrying.Tick++;
        List<SimulationEvent> saved = Advance(parrying,
            new FighterInput(Vec2.Zero, defensePressed: true));
        Assert(Count(saved, SimulationEventType.PerfectParry) == 1 &&
            !parrying.Fighter(0).Eliminated,
            "An invulnerable physical overlap must remain parryable on the next tick.");
    }

    private static int Count(List<SimulationEvent> events, SimulationEventType type)
    {
        int count = 0;
        foreach (SimulationEvent item in events) if (item.Type == type) count++;
        return count;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
