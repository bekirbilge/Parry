namespace Game.Core
{
    /// <summary>One fighter's sampled controls. Pressed flags are edges on this tick.</summary>
    public readonly struct FighterInput
    {
        public readonly Vec2 Move;
        public readonly bool JumpPressed;
        public readonly bool JumpHeld;
        public readonly bool DashPressed;
        public readonly bool DefensePressed;
        public readonly bool DefenseHeld;

        public FighterInput(Vec2 move, bool jumpPressed = false, bool jumpHeld = false,
            bool dashPressed = false, bool defensePressed = false, bool defenseHeld = false)
        {
            Move = move;
            JumpPressed = jumpPressed;
            JumpHeld = jumpHeld;
            DashPressed = dashPressed;
            DefensePressed = defensePressed;
            DefenseHeld = defenseHeld;
        }
    }

    /// <summary>Authoritative inputs for one numbered simulation tick.</summary>
    public readonly struct SimulationInput
    {
        public readonly long Tick;
        public readonly FighterInput Left;
        public readonly FighterInput Right;

        public SimulationInput(long tick, FighterInput left, FighterInput right)
        {
            Tick = tick;
            Left = left;
            Right = right;
        }

        public FighterInput ForFighter(int fighterId) => fighterId == 0 ? Left :
            fighterId == 1 ? Right : throw new System.ArgumentOutOfRangeException(nameof(fighterId));
    }
}
