using System;
using Game.Core;
using Game.Unity;
using NUnit.Framework;

namespace Game.Unity.Tests
{
    public sealed class CombatEffectsTests
    {
        [Test]
        public void PerfectParryUsesImpactAndFighterAndExpiresOnSimulationTicks()
        {
            var state = new SimulationState { Tick = 10 };
            var effects = new CombatEffectsPresentation();
            ulong checksum = state.ComputeChecksum();
            var events = new[]
            {
                new SimulationEvent(10, SimulationEventType.PerfectParry, 77, 1, new Vec2(3, 4)),
                new SimulationEvent(10, SimulationEventType.OwnedProjectileRedirected, 78, 0, new Vec2(9, 9))
            };
            effects.Observe(state, events, 60);
            Assert.That(state.ComputeChecksum(), Is.EqualTo(checksum));
            Assert.That(effects.Flashes.Count, Is.EqualTo(1));
            var flash = effects.Flashes[0];
            Assert.That(flash.FighterId, Is.EqualTo(1));
            Assert.That(flash.Position.X, Is.EqualTo(3));
            Assert.That(flash.Position.Y, Is.EqualTo(4));
            Assert.That(flash.Strength, Is.EqualTo(1));
            Assert.That(effects.FlashOpacity(flash, 10, 60), Is.EqualTo(1f));

            // Repainting or pausing on the same simulation tick cannot replay events.
            effects.Observe(state, events, 60);
            Assert.That(effects.Flashes.Count, Is.EqualTo(1));
            Assert.That(effects.FlashOpacity(flash, 10, 60), Is.EqualTo(1f));
            state.Tick = 20;
            effects.Observe(state, Array.Empty<SimulationEvent>(), 60);
            Assert.That(effects.FlashOpacity(flash, 20, 60), Is.GreaterThan(0f));
            state.Tick = 21;
            effects.Observe(state, Array.Empty<SimulationEvent>(), 60);
            Assert.That(effects.Flashes, Is.Empty);
            Assert.That(effects.FlashOpacity(flash, 21, 60), Is.Zero);
        }

        [Test]
        public void TrailsCopyDiscretePositionsAndPruneDeletedProjectiles()
        {
            var state = new SimulationState();
            var projectile = new ProjectileState { Id = 7, Position = new Vec2(2, 3) };
            state.Projectiles.Add(projectile);
            var effects = new CombatEffectsPresentation();
            state.Tick = 1;
            ulong checksum = state.ComputeChecksum();
            effects.Observe(state, Array.Empty<SimulationEvent>(), 60);
            Assert.That(state.ComputeChecksum(), Is.EqualTo(checksum));
            projectile.Position = new Vec2(50, 70);
            Assert.That(effects.ProjectileTrail(7)[0].X, Is.EqualTo(2));
            effects.Observe(state, Array.Empty<SimulationEvent>(), 60);
            Assert.That(effects.ProjectileTrail(7).Count, Is.EqualTo(1));

            for (int tick = 2; tick <= 8; tick++)
            {
                state.Tick = tick;
                projectile.Position = new Vec2(tick % 2 == 0 ? 90 : 10, tick);
                effects.Observe(state, Array.Empty<SimulationEvent>(), 60);
            }
            Assert.That(effects.ProjectileTrail(7).Count, Is.EqualTo(6));
            Assert.That(effects.ProjectileTrail(7)[0].Y, Is.EqualTo(3));
            Assert.That(effects.ProjectileTrail(7)[5].X, Is.EqualTo(90));

            state.Projectiles.Clear();
            state.Tick = 9;
            effects.Observe(state, Array.Empty<SimulationEvent>(), 60);
            Assert.That(effects.ProjectileTrail(7), Is.Empty);
        }

        [Test]
        public void RoundStartRewindAndResetClearPreviousVisuals()
        {
            var state = new SimulationState { Tick = 40 };
            state.Projectiles.Add(new ProjectileState { Id = 5, Position = new Vec2(1, 1) });
            var effects = new CombatEffectsPresentation();
            var parry = new[] { new SimulationEvent(40, SimulationEventType.PerfectParry, 5, 0, new Vec2(2, 2)) };
            effects.Observe(state, parry, 60);
            state.Tick = 41;
            state.Projectiles[0].Position = new Vec2(3, 3);
            effects.Observe(state, new[] { new SimulationEvent(41, SimulationEventType.RoundStarted) }, 60);
            Assert.That(effects.Flashes, Is.Empty);
            Assert.That(effects.ProjectileTrail(5).Count, Is.EqualTo(1));
            Assert.That(effects.ProjectileTrail(5)[0].X, Is.EqualTo(3));
            effects.Observe(state, new[] { new SimulationEvent(41, SimulationEventType.RoundStarted) }, 60);
            Assert.That(effects.ProjectileTrail(5).Count, Is.EqualTo(1));

            state.Tick = 1;
            effects.Observe(state, Array.Empty<SimulationEvent>(), 60);
            Assert.That(effects.ProjectileTrail(5).Count, Is.EqualTo(1));
            effects.Reset();
            Assert.That(effects.ProjectileTrail(5), Is.Empty);
            Assert.That(effects.Flashes, Is.Empty);
        }

        [Test]
        public void MultishotMemoryIsBounded()
        {
            var state = new SimulationState();
            var effects = new CombatEffectsPresentation();
            for (int id = 0; id < 70; id++)
                state.Projectiles.Add(new ProjectileState { Id = id, Position = new Vec2(id, 0) });
            state.Tick = 1;
            effects.Observe(state, Array.Empty<SimulationEvent>(), 60);
            int tracked = 0;
            for (int id = 0; id < 70; id++)
                if (effects.ProjectileTrail(id).Count > 0) tracked++;
            Assert.That(tracked, Is.EqualTo(CombatEffectsPresentation.MaximumProjectiles));

            for (int tick = 2; tick <= 12; tick++)
            {
                state.Tick = tick;
                effects.Observe(state, new[]
                {
                    new SimulationEvent(tick, SimulationEventType.PerfectParry, 0, tick % 2, new Vec2(tick, 1))
                }, 60);
            }
            Assert.That(effects.Flashes.Count, Is.LessThanOrEqualTo(CombatEffectsPresentation.MaximumFlashes));
            for (int id = 0; id < 64; id++)
                Assert.That(effects.ProjectileTrail(id).Count, Is.EqualTo(CombatEffectsPresentation.MaximumTrailPoints));
        }
    }
}
