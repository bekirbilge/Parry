using System;
using System.Collections.Generic;
using Game.Core;

internal static class FuelTests
{
    private static readonly CombatTuning Tuning = new CombatTuning(
        new MovementTuning(useFuelBoost: true), CombatTuning.Default.Projectile,
        CombatTuning.Default.Defense, CombatTuning.Default.Divider,
        CombatTuning.Default.Arena, CombatTuning.Default.Match);
    private static readonly SimulationConfig Config = new SimulationConfig();

    public static void Run()
    {
        DoubledTankBurnsAndRefillsAtTheConfiguredRate();
        BurnsAndRefillsAfterDelay();
        ReleaseCoastsAndRequiresNewPress();
        SteeringAndNeutralRiseStayCapped();
        DownwardBoostWorksInAirAndWhileGrounded();
        HeldBoostCanSteerFromUpToDown();
        TapCannotChainInvulnerability();
        RopeLoadStopsOnReleaseAndExhaustion();
        RecoilTickBodyHitUsesOneIgnition();
        GuardBreakAfterRecoilKeepsReleaseLatch();
        PerfectParryRefundsOnlyEnemyShot();
        ReplayAndFuzzStayDeterministic();
    }

    private static void DoubledTankBurnsAndRefillsAtTheConfiguredRate()
    {
        var tuning = new CombatTuning(new MovementTuning(useFuelBoost: true,
            fuelCapacitySeconds: 1.8), CombatTuning.Default.Projectile,
            CombatTuning.Default.Defense, CombatTuning.Default.Divider,
            CombatTuning.Default.Arena, CombatTuning.Default.Match);
        SimulationState state = Fresh();
        FighterState fighter = state.Fighter(0);
        fighter.Position = fighter.PreviousPosition = fighter.TickStartPosition = new Vec2(25, 25);
        fighter.Grounded = false;
        fighter.FuelSeconds = tuning.Movement.FuelCapacitySeconds;
        var held = new FighterInput(Vec2.Zero, dashHeld: true);
        void TrialStep(FighterInput input)
        {
            var command = new SimulationInput(state.Tick, input, default);
            var events = new List<SimulationEvent>();
            MovementSystem.AdvanceKinematics(state, command, tuning, Config, events);
            CombatSystem.Advance(state, command, tuning, Config, events);
            state.Tick++;
        }
        for (int i = 0; i < Config.TickRateHz; i++)
            TrialStep(new FighterInput(Vec2.Zero, dashPressed: i == 0, dashHeld: true));
        Assert(fighter.FuelBoostActive && Math.Abs(fighter.FuelSeconds - .76) < 1e-8,
            "The doubled tank must stay powered beyond the old budget with normal ignition/burn cost.");
        Assert(fighter.DashInvulnerabilityTicksRemaining == 0,
            "A longer powered boost must not extend invulnerability.");
        for (int i = 0; i < Config.TickRateHz * 2 && fighter.FuelBoostActive; i++)
            TrialStep(held);
        Assert(!fighter.FuelBoostActive && fighter.FuelBoostNeedsRelease && fighter.FuelSeconds == 0,
            "The finite tank must exhaust and require release before another ignition.");
        int sequence = fighter.DashSequence;
        int delay = fighter.FuelRegenDelayTicksRemaining;
        Assert(delay > 0, "Exhaustion must retain the configured refill delay.");
        for (int i = 0; i < delay; i++) TrialStep(held);
        Assert(fighter.FuelSeconds == 0, "Refill must wait for the remaining delay.");
        for (int i = 0; i < Config.TickRateHz * 3; i++) TrialStep(held);
        Assert(Math.Abs(fighter.FuelSeconds - .9) < 1e-8 && !fighter.FuelBoostActive,
            "Three seconds at the unchanged refill rate restores half the doubled tank.");
        for (int i = 0; i < Config.TickRateHz * 3; i++) TrialStep(held);
        Assert(Math.Abs(fighter.FuelSeconds - 1.8) < 1e-8 &&
            fighter.DashSequence == sequence && fighter.FuelBoostNeedsRelease,
            "Six seconds refills the exact cap without reigniting held boost.");
        TrialStep(default);
        TrialStep(new FighterInput(Vec2.Zero, dashPressed: true, dashHeld: true));
        Assert(fighter.FuelBoostActive && fighter.DashSequence == sequence + 1,
            "Release and a fresh press must enable another finite boost.");
    }

    private static SimulationState Fresh()
    {
        var state = new SimulationState { Phase = MatchPhase.Active };
        foreach (FighterState fighter in state.Fighters)
        {
            fighter.Position = fighter.PreviousPosition = fighter.TickStartPosition =
                new Vec2(fighter.Id == 0 ? 25 : 75, Tuning.Arena.FighterRadiusUnits);
            fighter.Grounded = true;
            fighter.FuelSeconds = Tuning.Movement.FuelCapacitySeconds;
        }
        return state;
    }

    private static void Move(SimulationState state, FighterInput left, CombatTuning? tuning = null,
        SimulationConfig? config = null)
    {
        MovementSystem.AdvanceKinematics(state, new SimulationInput(state.Tick, left, default),
            tuning ?? Tuning, config ?? Config, new List<SimulationEvent>());
        state.Tick++;
    }

    private static void BurnsAndRefillsAfterDelay()
    {
        SimulationState state = Fresh();
        FighterState fighter = state.Fighter(0);
        for (int i = 0; i < 20; i++)
            Move(state, new FighterInput(new Vec2(-1, 0), dashPressed: i == 0, dashHeld: true));
        double expected = Tuning.Movement.FuelCapacitySeconds - Tuning.Movement.FuelIgnitionCostSeconds -
            20 * Config.TickDurationSeconds;
        Assert(fighter.FuelBoostActive && Math.Abs(fighter.FuelSeconds - expected) < 1e-9,
            "Held boost must burn one fixed step of fuel each tick plus one ignition cost.");
        Move(state, default);
        double afterRelease = fighter.FuelSeconds;
        int delay = SimMath.SecondsToTicks(Tuning.Movement.FuelRegenerationDelaySeconds, Config.TickRateHz);
        for (int i = 1; i < delay; i++) Move(state, default);
        Assert(Math.Abs(fighter.FuelSeconds - afterRelease) < 1e-9,
            "Fuel regeneration must wait for the delay after powered motion ends.");
        Move(state, default);
        Assert(fighter.FuelSeconds > afterRelease, "Fuel must regenerate after its delay.");
        for (int i = 0; i < 300; i++) Move(state, default);
        Assert(Math.Abs(fighter.FuelSeconds - Tuning.Movement.FuelCapacitySeconds) < 1e-9,
            "Fuel must refill to its exact cap.");
    }

    private static void ReleaseCoastsAndRequiresNewPress()
    {
        SimulationState state = Fresh();
        FighterState fighter = state.Fighter(0);
        Move(state, new FighterInput(new Vec2(-1, 0), dashPressed: true, dashHeld: true));
        int sequence = fighter.DashSequence;
        double speed = fighter.Velocity.X;
        Move(state, default);
        Assert(!fighter.FuelBoostActive && fighter.DashTicksRemaining == 0 &&
            fighter.Velocity.X < 0 && fighter.Velocity.X > speed,
            "Release must end powered thrust and preserve coasting velocity.");
        Move(state, new FighterInput(new Vec2(-1, 0), dashPressed: true, dashHeld: true));
        Assert(fighter.DashSequence == sequence + 1, "A new press must start a new attack sequence.");
        for (int i = 0; i < 90; i++) Move(state, new FighterInput(new Vec2(-1, 0), dashHeld: true));
        Assert(!fighter.FuelBoostActive && fighter.FuelBoostNeedsRelease &&
            fighter.DashSequence == sequence + 1,
            "Exhaustion must end thrust and cannot automatically reignite while held.");
        for (int i = 0; i < 100; i++) Move(state, new FighterInput(new Vec2(-1, 0), dashHeld: true));
        Assert(fighter.DashSequence == sequence + 1 && fighter.FuelSeconds > 0.1,
            "Fuel refilling under a held button must not start a new sequence.");
        Move(state, default);
        Move(state, new FighterInput(new Vec2(-1, 0), dashPressed: true, dashHeld: true));
        Assert(fighter.FuelBoostActive && fighter.DashSequence == sequence + 2,
            "Releasing then pressing must permit the next ignition.");
    }

    private static void SteeringAndNeutralRiseStayCapped()
    {
        SimulationState state = Fresh();
        FighterState fighter = state.Fighter(0);
        Move(state, new FighterInput(Vec2.Zero, dashPressed: true, dashHeld: true));
        Assert(fighter.Velocity.X == 0 && fighter.Velocity.Y == 40,
            "Neutral fuel boost must launch upward at the existing vertical cap.");
        for (int i = 0; i < 10; i++)
            Move(state, new FighterInput(new Vec2(-1, 1), dashHeld: true));
        Assert(fighter.DashSequence == 1 && fighter.Velocity.X < 0 &&
            fighter.Velocity.Y <= 40 + 1e-9 && fighter.Velocity.Length <= 80 + 1e-9,
            "Steering must be continuous within one ignition and preserve speed caps.");
    }

    private static void DownwardBoostWorksInAirAndWhileGrounded()
    {
        SimulationState state = Fresh();
        FighterState fighter = state.Fighter(0);
        fighter.Grounded = false;
        fighter.Position = fighter.PreviousPosition = fighter.TickStartPosition = new Vec2(25, 50);
        Move(state, new FighterInput(new Vec2(0, -1), dashPressed: true, dashHeld: true));
        Assert(fighter.FuelBoostActive && fighter.Velocity.Y < 0 && fighter.Position.Y < 50,
            "A downward fuel boost must ignite in midair and move the fighter down.");

        state = Fresh();
        fighter = state.Fighter(0);
        double floor = Tuning.Arena.FighterRadiusUnits;
        Move(state, new FighterInput(new Vec2(0, -1), dashPressed: true, dashHeld: true));
        Assert(fighter.Grounded && fighter.Position.Y == floor && fighter.Velocity.Y == 0 &&
            fighter.FuelBoostActive && fighter.FuelSeconds < Tuning.Movement.FuelCapacitySeconds,
            "The floor must constrain downward motion without canceling the held boost state.");
    }

    private static void HeldBoostCanSteerFromUpToDown()
    {
        SimulationState state = Fresh();
        FighterState fighter = state.Fighter(0);
        fighter.Grounded = false;
        fighter.Position = fighter.PreviousPosition = fighter.TickStartPosition = new Vec2(25, 50);
        Move(state, new FighterInput(new Vec2(0, 1), dashPressed: true, dashHeld: true));
        int sequence = fighter.DashSequence;
        for (int i = 0; i < 30; i++)
            Move(state, new FighterInput(new Vec2(0, -1), dashHeld: true));
        Assert(fighter.DashSequence == sequence && fighter.FuelBoostActive && fighter.Velocity.Y < 0,
            "A held boost must steer from upward to downward after its normal acceleration time.");
    }

    private static void TapCannotChainInvulnerability()
    {
        SimulationState state = Fresh();
        FighterState fighter = state.Fighter(0);
        Move(state, new FighterInput(new Vec2(-1, 0), dashPressed: true));
        int first = fighter.DashInvulnerabilityTicksRemaining;
        Assert(first > 0, "The first ignition must grant the short invulnerability window.");
        Move(state, default);
        fighter.DashInvulnerabilityTicksRemaining = 1;
        Move(state, new FighterInput(new Vec2(-1, 0), dashPressed: true));
        Assert(fighter.DashSequence == 2 && fighter.DashInvulnerabilityTicksRemaining == 1,
            "A quick second ignition may move but must not refresh invulnerability.");
    }

    private static void RopeLoadStopsOnReleaseAndExhaustion()
    {
        SimulationState state = Fresh();
        FighterState fighter = state.Fighter(0);
        fighter.Position = fighter.PreviousPosition = new Vec2(47.2, Tuning.Arena.FighterRadiusUnits);
        for (int i = 0; i < 8 && fighter.DividerLoadTicksRemaining == 0; i++)
            Move(state, new FighterInput(new Vec2(1, 0), dashPressed: i == 0, dashHeld: true));
        Assert(fighter.DividerLoadTicksRemaining > 0, "Inward fuel thrust must load the rope.");
        Move(state, default);
        Assert(fighter.DividerLoadTicksRemaining == 0 && !fighter.FuelBoostActive,
            "Releasing fuel thrust must stop powered rope loading.");

        state = Fresh();
        fighter = state.Fighter(0);
        fighter.Position = fighter.PreviousPosition = new Vec2(47.2, Tuning.Arena.FighterRadiusUnits);
        fighter.FuelSeconds = 0.11;
        for (int i = 0; i < 8; i++)
            Move(state, new FighterInput(new Vec2(1, 0), dashPressed: i == 0, dashHeld: true));
        Assert(!fighter.FuelBoostActive && fighter.DividerLoadTicksRemaining == 0,
            "Exhaustion must stop powered rope loading.");
    }

    private static void PerfectParryRefundsOnlyEnemyShot()
    {
        SimulationState state = Fresh();
        FighterState fighter = state.Fighter(0);
        fighter.Position = fighter.PreviousPosition = new Vec2(25, 10);
        fighter.FuelSeconds = 0.2;
        var shot = new ProjectileState { Id = 1, Position = new Vec2(27.3, 10),
            Velocity = new Vec2(-24, 0) };
        state.Projectiles.Add(shot);
        Combat(state, new FighterInput(Vec2.Zero, defensePressed: true));
        Assert(Math.Abs(fighter.FuelSeconds - 0.5) < 1e-9 && shot.LastParrierId == 0,
            "An enemy perfect parry must refund one third of the tank.");
        shot.Position = new Vec2(27.3, 10);
        shot.Velocity = new Vec2(-72, 0);
        shot.ReflectedContactMask = shot.ParryContactMask = 0;
        state.Tick++;
        Combat(state, new FighterInput(Vec2.Zero, defensePressed: true));
        Assert(Math.Abs(fighter.FuelSeconds - 0.5) < 1e-9,
            "Redirecting an owned shot must not refund fuel.");
        shot.LastParrierId = -1;
        shot.Position = new Vec2(27.3, 10);
        shot.Velocity = new Vec2(-72, 0);
        shot.ReflectedContactMask = shot.ParryContactMask = 0;
        fighter.FuelSeconds = 0.8;
        state.Tick++;
        Combat(state, new FighterInput(Vec2.Zero, defensePressed: true));
        Assert(Math.Abs(fighter.FuelSeconds - 0.9) < 1e-9,
            "Enemy parry refunds must cap at the tank maximum.");
    }

    private static void GuardBreakAfterRecoilKeepsReleaseLatch()
    {
        SimulationState state = Fresh();
        FighterState fighter = state.Fighter(0);
        fighter.FuelBoostActive = false;
        fighter.FuelBoostNeedsRelease = true;
        fighter.DashTicksRemaining = 1;
        fighter.Posture = Tuning.Defense.MaximumPosture - 1;
        DefenseSystem.ApplyPostureDamage(state, fighter, 1, Tuning, Config,
            new List<SimulationEvent>());
        Assert(fighter.GuardBreakTicksRemaining > 0 && fighter.FuelBoostNeedsRelease,
            "Guard break on a recoil tick must preserve the required release before re-ignition.");
    }

    private static void RecoilTickBodyHitUsesOneIgnition()
    {
        SimulationState state = Fresh();
        FighterState left = state.Fighter(0);
        FighterState right = state.Fighter(1);
        left.PreviousPosition = new Vec2(47, 10);
        left.Position = new Vec2(48.5, 10);
        right.PreviousPosition = right.Position = new Vec2(51.5, 10);
        left.DashSequence = 1;
        left.DashDirection = new Vec2(1, 0);
        left.DashTicksRemaining = 1;
        left.FuelBoostActive = false;
        left.FuelBoostNeedsRelease = true;
        left.DividerReboundDistanceRemaining = 18;
        left.LastDividerLoadDashSequence = 1;
        Assert(BodySystem.TryFindNextContact(state, 0, Tuning, out BodyContact contact),
            "The rope recoil tick must still expose its powered body contact.");
        BodySystem.ApplyContact(state, contact, Tuning, Config, new List<SimulationEvent>());
        Assert(right.Posture == Tuning.Defense.DashBodyPostureDamage &&
            left.LastDashBodyHitSequence == 1,
            "The recoil tick must apply one dash body hit even though powered thrust just ended.");
        state.Tick++;
        left.PreviousPosition = left.Position = new Vec2(48.5, 10);
        right.PreviousPosition = right.Position = new Vec2(51.5, 10);
        BodySystem.ApplyContact(state, new BodyContact(0, new Vec2(1, 0)),
            Tuning, Config, new List<SimulationEvent>());
        Assert(right.Posture == Tuning.Defense.DashBodyPostureDamage,
            "A recoil tick hit must consume the ignition sequence once.");
    }

    private static void Combat(SimulationState state, FighterInput left)
    {
        CombatSystem.Advance(state, new SimulationInput(state.Tick, left, default),
            Tuning, Config, new List<SimulationEvent>());
    }

    private static void ReplayAndFuzzStayDeterministic()
    {
        var first = new MatchSimulation(tuning: Tuning);
        var second = new MatchSimulation(tuning: Tuning);
        MatchSimulation? resumed = null;
        uint random = 0x365e9d1f;
        for (int tick = 0; tick < 60000; tick++)
        {
            FighterInput left = RandomInput(ref random);
            FighterInput right = RandomInput(ref random);
            var input = new SimulationInput(first.Tick, left, right);
            first.Step(input);
            second.Step(input);
            SimulationState snapshot = first.CaptureSnapshot();
            Assert(snapshot.ComputeChecksum() == second.CaptureSnapshot().ComputeChecksum(),
                "Fuel input replay must remain deterministic.");
            foreach (FighterState fighter in snapshot.Fighters)
                Assert(!double.IsNaN(fighter.FuelSeconds) &&
                    fighter.FuelSeconds >= -1e-9 && fighter.FuelSeconds <= Tuning.Movement.FuelCapacitySeconds + 1e-9,
                    "Fuel must stay finite and bounded during fuzz input.");
            if (tick == 1000)
            {
                resumed = new MatchSimulation(tuning: Tuning);
                resumed.RestoreSnapshot(snapshot);
            }
            else if (resumed != null)
            {
                resumed.Step(input);
                Assert(snapshot.ComputeChecksum() == resumed.CaptureSnapshot().ComputeChecksum(),
                    "Fuel snapshot restore must continue the same replay.");
            }
            if (first.Phase == MatchPhase.MatchComplete)
            {
                first = new MatchSimulation(tuning: Tuning);
                second = new MatchSimulation(tuning: Tuning);
                if (resumed != null) resumed = new MatchSimulation(tuning: Tuning);
            }
        }
        var bot = new MatchSimulation(tuning: Tuning);
        int rounds = 0;
        int totalTicks = 0;
        while (rounds < 240 && totalTicks < 2000000)
        {
            FighterInput left = DebugBot.Decide(bot.CaptureSnapshot(), 0, 1, useFuelBoost: true);
            FighterInput right = DebugBot.Decide(bot.CaptureSnapshot(), 1, 1, useFuelBoost: true);
            foreach (SimulationEvent item in bot.Step(new SimulationInput(bot.Tick, left, right)))
                if (item.Type == SimulationEventType.RoundWon || item.Type == SimulationEventType.RoundDraw) rounds++;
            if (bot.Phase == MatchPhase.MatchComplete) bot = new MatchSimulation(tuning: Tuning);
            totalTicks++;
        }
        Assert(rounds >= 240, "Fuel-mode bots must complete 240 deterministic rounds; got " +
            rounds + " in " + totalTicks + " ticks, phase " + bot.Phase + ".");
        Console.WriteLine("Completed " + rounds + " fuel bot rounds in " + totalTicks + " ticks.");
    }

    private static FighterInput RandomInput(ref uint random)
    {
        random ^= random << 13;
        random ^= random >> 17;
        random ^= random << 5;
        uint bits = random;
        return new FighterInput(new Vec2(((int)(bits & 255) - 127) / 127.0,
            ((int)((bits >> 8) & 255) - 127) / 127.0),
            dashPressed: (bits & (1u << 16)) != 0,
            defensePressed: (bits & (1u << 17)) != 0,
            defenseHeld: (bits & (1u << 18)) != 0,
            dashHeld: (bits & (1u << 19)) != 0);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
