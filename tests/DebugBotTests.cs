using System;
using Game.Core;

internal static class DebugBotTests
{
    public static void Run()
    {
        UpwardMovementRequiresBoostCharge();
        RepeatsWithoutChangingState();
        KeepsAxesFiniteAndWithinInputRange();
        ClampsDifficultyAndRejectsBadFighterIds();
        ReturnsNeutralInputsOutsideActivePlay();
    }

    private static void RepeatsWithoutChangingState()
    {
        SimulationState state = ActiveState();
        state.Projectiles.Add(new ProjectileState
        {
            Id = 2,
            Position = new Vec2(35, 4),
            Velocity = new Vec2(-20, 0)
        });
        ulong before = state.ComputeChecksum();
        FighterInput first = DebugBot.Decide(state, 0, 1);
        FighterInput second = DebugBot.Decide(state, 0, 1);
        Assert(Same(first, second), "Identical snapshots must produce identical bot inputs.");
        Assert(state.ComputeChecksum() == before, "Deciding must leave every simulation field unchanged.");
    }

    private static void KeepsAxesFiniteAndWithinInputRange()
    {
        SimulationState state = ActiveState();
        // Verify ordinary edge positions and an approaching projectile return legal axes.
        state.Fighter(0).Position = new Vec2(0, 1.5);
        state.Fighter(1).Position = new Vec2(100, 1.5);
        state.Projectiles.Add(new ProjectileState
        {
            Id = 0,
            Position = new Vec2(4, 1.5),
            Velocity = new Vec2(-24, 0)
        });

        for (int difficulty = 0; difficulty <= 2; difficulty++)
        {
            FighterInput input = DebugBot.Decide(state, 0, difficulty);
            Assert(!double.IsNaN(input.Move.X) && !double.IsInfinity(input.Move.X), "Horizontal axis must be finite.");
            Assert(!double.IsNaN(input.Move.Y) && !double.IsInfinity(input.Move.Y), "Vertical axis must be finite.");
            Assert(Math.Abs(input.Move.X) <= 1 && Math.Abs(input.Move.Y) <= 1, "Axes must fit the core input range.");
        }
    }

    private static void ClampsDifficultyAndRejectsBadFighterIds()
    {
        SimulationState state = ActiveState();
        Assert(Same(DebugBot.Decide(state, 0, 99), DebugBot.Decide(state, 0, 2)),
            "Difficulty above range must clamp to the hardest setting.");
        Assert(Same(DebugBot.Decide(state, 0, -99), DebugBot.Decide(state, 0, 0)),
            "Difficulty below range must clamp to easy.");
        ExpectThrows<ArgumentOutOfRangeException>(() => DebugBot.Decide(state, -1));
        ExpectThrows<ArgumentOutOfRangeException>(() => DebugBot.Decide(state, 2));
        ExpectThrows<ArgumentNullException>(() => DebugBot.Decide(null!, 0));
    }

    private static void ReturnsNeutralInputsOutsideActivePlay()
    {
        SimulationState state = ActiveState();
        state.Phase = MatchPhase.Countdown;
        FighterInput input = DebugBot.Decide(state, 0);
        Assert(input.Move.X == 0 && input.Move.Y == 0 && !input.JumpPressed && !input.DashPressed &&
            !input.DefensePressed && !input.DefenseHeld, "Non-active phases must not issue gameplay actions.");
    }

    private static void UpwardMovementRequiresBoostCharge()
    {
        SimulationState state = ActiveState();
        state.Tick = 176;
        FighterInput charged = DebugBot.Decide(state, 0);
        Assert(charged.DashPressed && charged.Move.X == 0 && charged.Move.Y == 1 &&
            !charged.JumpPressed && !charged.JumpHeld, "Bot vertical movement must use the same upward boost action.");
        state.Fighter(0).DashCharges = 0;
        FighterInput empty = DebugBot.Decide(state, 0);
        Assert(!empty.DashPressed && !empty.JumpPressed && !empty.JumpHeld,
            "A bot with empty charges must not gain a hidden free jump.");
    }

    private static SimulationState ActiveState()
    {
        var state = new SimulationState { Tick = 600, Phase = MatchPhase.Active };
        state.Fighters = new[]
        {
            new FighterState { Id = 0, Position = new Vec2(25, 1.5), Grounded = true, DashCharges = MovementTuning.MaximumDashCharges },
            new FighterState { Id = 1, Position = new Vec2(75, 1.5), Grounded = true, DashCharges = MovementTuning.MaximumDashCharges }
        };
        return state;
    }

    private static bool Same(FighterInput a, FighterInput b) =>
        a.Move.X.Equals(b.Move.X) && a.Move.Y.Equals(b.Move.Y) &&
        a.JumpPressed == b.JumpPressed && a.JumpHeld == b.JumpHeld &&
        a.DashPressed == b.DashPressed && a.DefensePressed == b.DefensePressed &&
        a.DefenseHeld == b.DefenseHeld;

    private static void ExpectThrows<TException>(Action action) where TException : Exception
    {
        try { action(); }
        catch (TException) { return; }
        throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
