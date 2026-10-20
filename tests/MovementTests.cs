using System;
using System.Collections.Generic;
using Game.Core;

internal static class MovementTests
{
    private static readonly CombatTuning Tuning = CombatTuning.Default;
    private static readonly SimulationConfig Config = new SimulationConfig();

    public static void Run()
    {
        RunAccelerationAndAirSteering();
        VariableJumpAndNoDoubleJump();
        CoyoteAndLandingBuffer();
        DirectionalDashAndSequentialRecharge();
        BoostFollowsContinuousAimAtFullSpeed();
        NeutralBoostLaunchesUpward();
        GuardBreakLeavesRunAndJumpAvailable();
    }

    private static void NeutralBoostLaunchesUpward()
    {
        SimulationState state = Fresh();
        FighterState fighter = state.Fighter(0);
        Tick(state, new FighterInput(Vec2.Zero, dashPressed: true));
        Assert(Math.Abs(fighter.DashDirection.X) < 1e-12 && fighter.DashDirection.Y == 1 &&
            Math.Abs(fighter.Position.X - 25) < 1e-9 &&
            fighter.Position.Y > Tuning.Arena.FighterRadiusUnits,
            "A neutral boost must launch straight upward without horizontal drift.");
    }

    private static void RunAccelerationAndAirSteering()
    {
        SimulationState state = Fresh();
        FighterState left = state.Fighter(0);
        for (int i = 0; i < 12; i++) Tick(state, new FighterInput(new Vec2(1, 0)));
        Assert(left.Position.X > 25 && left.Velocity.X > 0, "Ground run must accelerate continuously.");
        Tick(state, new FighterInput(new Vec2(0, 0), jumpPressed: true, jumpHeld: true));
        double initialVelocityX = left.Velocity.X;
        SimulationState unsteered = state.Clone();
        for (int i = 0; i < 14; i++) Tick(state, new FighterInput(new Vec2(-1, 0), jumpHeld: true));
        for (int i = 0; i < 14; i++) Tick(unsteered, new FighterInput(Vec2.Zero, jumpHeld: true));
        Assert(left.Velocity.X < 0 && left.Velocity.X < initialVelocityX &&
            left.Position.X < unsteered.Fighter(0).Position.X,
            "Opposite air steering must reverse motion and travel less than an unsteered jump.");
    }

    private static void VariableJumpAndNoDoubleJump()
    {
        SimulationState held = Fresh();
        SimulationState released = Fresh();
        Tick(held, new FighterInput(Vec2.Zero, jumpPressed: true, jumpHeld: true));
        Tick(released, new FighterInput(Vec2.Zero, jumpPressed: true, jumpHeld: false));
        for (int i = 0; i < 20; i++)
        {
            Tick(held, new FighterInput(Vec2.Zero, jumpHeld: true));
            Tick(released, new FighterInput(Vec2.Zero));
        }
        Assert(held.Fighter(0).Position.Y > released.Fighter(0).Position.Y + 1,
            "Holding jump must create a taller arc than releasing early.");
        FighterState fighter = held.Fighter(0);
        int sequence = fighter.AirSequence;
        Tick(held, new FighterInput(Vec2.Zero, jumpPressed: true, jumpHeld: true));
        Assert(fighter.AirSequence == sequence, "Airborne jump press must not double jump.");
    }

    private static void CoyoteAndLandingBuffer()
    {
        SimulationState coyote = Fresh();
        FighterState fighter = coyote.Fighter(0);
        fighter.Grounded = false;
        fighter.Position = new Vec2(25, 5);
        fighter.CoyoteTicksRemaining = 3;
        Tick(coyote, new FighterInput(Vec2.Zero, jumpPressed: true, jumpHeld: true));
        Assert(fighter.AirSequence == 1 && fighter.Velocity.Y > 0, "Coyote jump must work after leaving ground.");

        SimulationState buffered = Fresh();
        fighter = buffered.Fighter(0);
        fighter.Grounded = false;
        fighter.Position = new Vec2(25, 1.6);
        fighter.Velocity = new Vec2(0, -8);
        Tick(buffered, new FighterInput(Vec2.Zero, jumpPressed: true, jumpHeld: true));
        Assert(fighter.Grounded && fighter.JumpBufferTicksRemaining > 0,
            "A pre-landing jump press must remain buffered.");
        Tick(buffered, new FighterInput(Vec2.Zero, jumpHeld: true));
        Assert(fighter.AirSequence == 1 && !fighter.Grounded,
            "Buffered jump must fire after landing.");
    }

    private static void DirectionalDashAndSequentialRecharge()
    {
        SimulationState state = Fresh();
        FighterState fighter = state.Fighter(0);
        Tick(state, new FighterInput(new Vec2(1, 0.8), dashPressed: true));
        Assert(fighter.DashCharges == MovementTuning.MaximumDashCharges - 1 && fighter.DashSequence == 1,
            "First dash must consume one charge.");
        Vec2 firstDirection = new Vec2(1, 0.8).NormalizedOr(Vec2.Zero);
        Assert((fighter.DashDirection - firstDirection).Length < 1e-9,
            "Diagonal boost must retain the exact aimed direction.");
        Assert(fighter.DashInvulnerabilityTicksRemaining > 0, "Dash must start its iframe window.");
        for (int i = 0; i < 9; i++) Tick(state, new FighterInput(Vec2.Zero));
        Tick(state, new FighterInput(new Vec2(-1, 0), dashPressed: true));
        Assert(fighter.DashCharges == MovementTuning.MaximumDashCharges - 2 && fighter.DashSequence == 2,
            "Second dash must consume one charge.");
        for (int i = 0; i < 9; i++) Tick(state, new FighterInput(Vec2.Zero));
        Tick(state, new FighterInput(new Vec2(1, 0), dashPressed: true));
        Assert(fighter.DashCharges == 0 && fighter.DashSequence == 3,
            "Third dash must consume the last charge.");
        for (int i = 0; i < 9; i++) Tick(state, new FighterInput(Vec2.Zero));
        Tick(state, new FighterInput(new Vec2(1, 0), dashPressed: true));
        Assert(fighter.DashCharges == 0 && fighter.DashSequence == 3,
            "A fourth dash must be denied before recharge.");
        int recharge = SimMath.SecondsToTicks(Tuning.Movement.DashRechargeSeconds, Config.TickRateHz);
        for (int i = 0; i < recharge; i++) Tick(state, new FighterInput(Vec2.Zero));
        Assert(fighter.DashCharges == 1, "Only one dash charge must refill in the first recharge interval.");
        for (int i = 0; i < recharge; i++) Tick(state, new FighterInput(Vec2.Zero));
        Assert(fighter.DashCharges == 2, "Second charge must refill sequentially.");
        for (int i = 0; i < recharge; i++) Tick(state, new FighterInput(Vec2.Zero));
        Assert(fighter.DashCharges == MovementTuning.MaximumDashCharges && fighter.DashRechargeTicksRemaining == 0,
            "Third charge must refill sequentially and stop recharge at the cap.");
    }

    private static void BoostFollowsContinuousAimAtFullSpeed()
    {
        double[] angles = { 1, 10, 20, 37, 89, -1, -10, -20, -37, -89 };
        foreach (double angle in angles)
        {
            AssertBoostAim(angle, 1);
            AssertBoostAim(180 - angle, 0.25);
        }
    }

    private static void AssertBoostAim(double degrees, double stickMagnitude)
    {
        SimulationState state = Fresh();
        FighterState fighter = state.Fighter(0);
        fighter.Position = new Vec2(25, Tuning.Arena.HeightUnits / 2);
        fighter.Grounded = false;
        double radians = degrees * Math.PI / 180;
        Vec2 expected = new Vec2(Math.Cos(radians), Math.Sin(radians));
        Tick(state, new FighterInput(expected * stickMagnitude, dashPressed: true));
        Assert((fighter.DashDirection - expected).Length < 1e-9,
            "Boost aim must retain non-cardinal and mirrored angles.");
        Assert(Math.Abs(fighter.Velocity.Length - Tuning.Movement.DashSpeedUnitsPerSecond) < 1e-9,
            "Boost speed must be independent of aim angle and stick magnitude.");

        Tick(state, new FighterInput(-expected));
        Assert((fighter.DashDirection - expected).Length < 1e-9 &&
            (fighter.Velocity - expected * Tuning.Movement.DashSpeedUnitsPerSecond).Length < 1e-9,
            "A boost must keep its initial direction when the stick changes mid-boost.");
    }

    private static void GuardBreakLeavesRunAndJumpAvailable()
    {
        SimulationState state = Fresh();
        FighterState fighter = state.Fighter(0);
        fighter.GuardBreakTicksRemaining = 10;
        Tick(state, new FighterInput(new Vec2(1, 0), jumpPressed: true, jumpHeld: true, dashPressed: true));
        Assert(fighter.DashCharges == MovementTuning.MaximumDashCharges && fighter.DashSequence == 0,
            "Guard break must disable dash.");
        Assert(fighter.AirSequence == 1 && fighter.Velocity.X > 0,
            "Guard break must retain movement and jump control.");
    }

    private static SimulationState Fresh()
    {
        var state = new SimulationState { Tick = 1, Phase = MatchPhase.Active };
        state.Fighter(0).Position = new Vec2(25, Tuning.Arena.FighterRadiusUnits);
        state.Fighter(1).Position = new Vec2(75, Tuning.Arena.FighterRadiusUnits);
        foreach (FighterState fighter in state.Fighters)
        {
            fighter.Grounded = true;
            fighter.DashCharges = MovementTuning.MaximumDashCharges;
        }
        return state;
    }

    private static void Tick(SimulationState state, FighterInput left)
    {
        MovementSystem.AdvanceKinematics(state, new SimulationInput(state.Tick, left, default),
            Tuning, Config, new List<SimulationEvent>());
        state.Tick++;
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
