using System;
using System.Collections.Generic;
using Game.Core;

internal static class ProjectileTests
{
    public static void Run()
    {
        RicochetAtWallAndCornerKeepsProjectile();
        FastProjectileSweepsThroughFighterPath();
        PowerCapsAndDecaysEveryFourSeconds();
        DecayEasesSpeedWithoutChangingDirectionOrLevelTiming();
        DecayRampSurvivesSnapshotReplay();
        ProjectilesCrossEachOtherAndDividerWithoutInteraction();
        ContactLimitThrowsInsteadOfDroppingBounces();
    }

    private static SimulationState State(Vec2 left, Vec2 right)
    {
        var state = new SimulationState { Tick = 1, Phase = MatchPhase.Active };
        state.Fighter(0).Position = state.Fighter(0).PreviousPosition = left;
        state.Fighter(1).Position = state.Fighter(1).PreviousPosition = right;
        state.Fighter(0).DashCharges = state.Fighter(1).DashCharges = MovementTuning.MaximumDashCharges;
        return state;
    }

    private static List<SimulationEvent> Advance(SimulationState state, FighterInput left = default)
    {
        var events = new List<SimulationEvent>();
        CombatSystem.Advance(state, new SimulationInput(state.Tick, left, default),
            CombatTuning.Default, new SimulationConfig(), events);
        return events;
    }

    private static void RicochetAtWallAndCornerKeepsProjectile()
    {
        SimulationState wall = State(new Vec2(25, 10), new Vec2(75, 10));
        wall.Projectiles.Add(new ProjectileState
        {
            Id = 1, Position = new Vec2(99.1, 30), Velocity = new Vec2(24, 0)
        });
        List<SimulationEvent> events = Advance(wall);
        Assert(wall.Projectiles.Count == 1 && events.Count == 1 &&
            events[0].Type == SimulationEventType.ProjectileBounced &&
            Math.Abs(wall.Projectiles[0].Position.X - 98.7) < 1e-10 &&
            wall.Projectiles[0].Velocity.X == -24,
            "A wall bounce must reflect at the true contact time without despawning.");

        SimulationState corner = State(new Vec2(25, 10), new Vec2(75, 10));
        corner.Projectiles.Add(new ProjectileState
        {
            Id = 2, Position = new Vec2(99.1, 55.35), Velocity = new Vec2(24, 24)
        });
        List<SimulationEvent> cornerEvents = Advance(corner);
        Assert(corner.Projectiles[0].Velocity.X < 0 && corner.Projectiles[0].Velocity.Y < 0 &&
            Count(cornerEvents, SimulationEventType.ProjectileBounced) == 2,
            "A simultaneous corner contact must reflect both axes deterministically.");
    }

    private static void FastProjectileSweepsThroughFighterPath()
    {
        SimulationState state = State(new Vec2(25, 30), new Vec2(75, 10));
        state.Projectiles.Add(new ProjectileState
        {
            Id = 3, Position = new Vec2(10, 30), Velocity = new Vec2(1000, 0)
        });
        List<SimulationEvent> events = Advance(state);
        Assert(state.Fighter(0).Eliminated && Count(events, SimulationEventType.FighterEliminated) == 1,
            "A projectile crossing a fighter between endpoints must still eliminate.");
        Assert(state.Projectiles.Count == 1, "Lethal contact must not despawn a permanent projectile.");
    }

    private static void PowerCapsAndDecaysEveryFourSeconds()
    {
        SimulationState state = State(new Vec2(25, 10), new Vec2(75, 10));
        var projectile = new ProjectileState
        {
            Id = 4, Position = new Vec2(22.7, 10), Velocity = new Vec2(24, 0)
        };
        state.Projectiles.Add(projectile);
        for (int parry = 0; parry < 5; parry++)
        {
            projectile.Position = new Vec2(22.7, 10);
            projectile.Velocity = new Vec2(24, 0);
            projectile.LastParrierId = 1; // Each shot represents a new opponent return.
            projectile.ParryContactMask = 0; // An opponent reflection clears our prior parry overlap.
            projectile.ReflectedContactMask = 0;
            List<SimulationEvent> events = Advance(state,
                new FighterInput(Vec2.Zero, defensePressed: true));
            Assert(Count(events, SimulationEventType.PerfectParry) == 1,
                "Each deliberate contact must resolve as a perfect parry.");
            state.Tick++;
        }
        Assert(projectile.PowerLevel == 3 && projectile.DecayTicksRemaining == 240 &&
            Math.Abs(projectile.Velocity.Length - 24 * 4.0) < 1e-10,
            "Power must cap at three and apply the configured critical speed multiplier.");
        projectile.Position = new Vec2(50, 30);
        projectile.Velocity = new Vec2(24 * 4.0, 0);
        for (int tick = 0; tick < 239; tick++)
        {
            Advance(state);
            state.Tick++;
        }
        Assert(projectile.PowerLevel == 3, "Power must not decay before four full seconds.");
        Advance(state);
        state.Tick++;
        Assert(projectile.PowerLevel == 2 && projectile.DecayTicksRemaining == 240 &&
            projectile.SpeedDecaying && Math.Abs(projectile.Velocity.Length - 95.8) < 1e-9,
            "Power drops on the four-second boundary while speed begins a bounded ramp.");
        for (int tick = 0; tick < 480; tick++)
        {
            Advance(state);
            state.Tick++;
        }
        Assert(projectile.PowerLevel == 0 && state.Projectiles.Count == 1,
            "Power must decay to zero while the projectile remains permanent through ricochets.");
    }

    private static void DecayEasesSpeedWithoutChangingDirectionOrLevelTiming()
    {
        SimulationState state = State(new Vec2(25, 10), new Vec2(75, 10));
        var shot = new ProjectileState
        {
            Id = 42, Position = new Vec2(50, 30), Velocity = new Vec2(72, 0),
            PowerLevel = 1, DecayTicksRemaining = 1
        };
        state.Projectiles.Add(shot);
        Advance(state);
        state.Tick++;
        Assert(shot.PowerLevel == 0 && shot.DecayTicksRemaining == 0 && shot.SpeedDecaying &&
            Math.Abs(shot.Velocity.Length - 71.8) < 1e-9,
            "Level one reaches zero on schedule without snapping from 72 to 24.");
        double previous = shot.Velocity.Length;
        for (int tick = 1; tick < 240; tick++)
        {
            Advance(state);
            state.Tick++;
            double speed = shot.Velocity.Length;
            Assert(speed <= previous + 1e-9 && previous - speed <= 0.2 + 1e-9 &&
                speed >= 24 - 1e-9 && shot.PowerLevel == 0 && shot.Velocity.Y == 0,
                "Each fixed tick must preserve heading and decelerate monotonically without overshoot.");
            previous = speed;
        }
        Assert(Math.Abs(shot.Velocity.Length - 24) < 1e-8 && !shot.SpeedDecaying,
            "The 72-to-24 ramp must finish in four seconds at 12 units per second squared.");

        // A synthetic fast level-zero projectile has no real decay transition.
        SimulationState synthetic = State(new Vec2(25, 10), new Vec2(75, 10));
        synthetic.Projectiles.Add(new ProjectileState
        {
            Id = 43, Position = new Vec2(50, 30), Velocity = new Vec2(96, 0)
        });
        Advance(synthetic);
        Assert(Math.Abs(synthetic.Projectiles[0].Velocity.Length - 96) < 1e-9,
            "An unflagged synthetic level-zero shot must retain its requested speed.");
    }

    private static void DecayRampSurvivesSnapshotReplay()
    {
        SimulationState state = State(new Vec2(25, 10), new Vec2(75, 10));
        state.Projectiles.Add(new ProjectileState
        {
            Id = 44, Position = new Vec2(50, 30), Velocity = new Vec2(72, 0),
            PowerLevel = 1, DecayTicksRemaining = 1
        });
        Advance(state);
        state.Tick++;
        SimulationState snapshot = state.Clone();
        Assert(snapshot.ComputeChecksum() == state.ComputeChecksum() &&
            snapshot.Projectiles[0].SpeedDecaying,
            "An active speed ramp must survive a deep snapshot copy.");
        snapshot.Projectiles[0].SpeedDecaying = false;
        Assert(snapshot.ComputeChecksum() != state.ComputeChecksum(),
            "The ramp flag must contribute to the snapshot checksum.");
        snapshot = state.Clone();
        for (int tick = 0; tick < 80; tick++)
        {
            Advance(state);
            Advance(snapshot);
            state.Tick++;
            snapshot.Tick++;
            Assert(state.ComputeChecksum() == snapshot.ComputeChecksum(),
                "Resumed speed decay must replay exactly from a captured mid-ramp state.");
        }
    }

    private static void ContactLimitThrowsInsteadOfDroppingBounces()
    {
        SimulationState state = State(new Vec2(25, 10), new Vec2(75, 10));
        state.Projectiles.Add(new ProjectileState
        {
            Id = 5, Position = new Vec2(50, 30), Velocity = new Vec2(1000000000, 0)
        });
        bool threw = false;
        try { Advance(state); }
        catch (InvalidOperationException error)
        {
            threw = error.Message.Contains("Contact budget exceeded");
        }
        Assert(threw, "An extreme contact count must fail visibly rather than drop collisions.");
    }

    private static void ProjectilesCrossEachOtherAndDividerWithoutInteraction()
    {
        SimulationState state = State(new Vec2(25, 10), new Vec2(75, 10));
        state.Projectiles.Add(new ProjectileState
        {
            Id = 40, Position = new Vec2(49.9, 30), Velocity = new Vec2(24, 0)
        });
        state.Projectiles.Add(new ProjectileState
        {
            Id = 41, Position = new Vec2(50.1, 30), Velocity = new Vec2(-24, 0)
        });
        List<SimulationEvent> events = Advance(state);
        Assert(state.Projectiles.Count == 2 && state.Projectiles[0].Position.X > 50 &&
            state.Projectiles[1].Position.X < 50 &&
            state.Projectiles[0].Velocity.X == 24 && state.Projectiles[1].Velocity.X == -24 &&
            Count(events, SimulationEventType.ProjectileBounced) == 0,
            "Crossing projectiles must pass through each other and the center divider unchanged.");
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
