namespace Game.Core
{
    public enum SimulationEventType
    {
        RoundStarted,
        SpawnWarning,
        ProjectileSpawned,
        ProjectileBounced,
        PerfectParry,
        GuardBlocked,
        GuardBroken,
        FighterEliminated,
        BodyImpact,
        DividerRebound,
        RoundDraw,
        RoundWon,
        MatchWon
    }

    /// <summary>Transient tick output; events are ordered by the simulation step.</summary>
    public readonly struct SimulationEvent
    {
        public readonly long Tick;
        public readonly SimulationEventType Type;
        public readonly int EntityId;
        public readonly int TargetId;
        public readonly Vec2 Position;
        public readonly double Value;

        public SimulationEvent(long tick, SimulationEventType type, int entityId = -1,
            int targetId = -1, Vec2 position = default, double value = 0)
        {
            Tick = tick;
            Type = type;
            EntityId = entityId;
            TargetId = targetId;
            Position = position;
            Value = value;
        }
    }

    /// <summary>Body contact time and left-to-right normal in the current tick.</summary>
    public readonly struct BodyContact
    {
        public readonly double Fraction;
        public readonly Vec2 Normal;

        public BodyContact(double fraction, Vec2 normal)
        {
            Fraction = fraction;
            Normal = normal;
        }
    }
}
