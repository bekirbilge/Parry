using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Unity.Tests
{
    public sealed class SwipeParryTests
    {
        private const int Width = 1920;
        private const int Height = 1080;
        private Rect _arena;
        private Vector2 _stick, _boost, _guard;
        private InputAdapter _adapter;

        [SetUp]
        public void SetUp()
        {
            _arena = ArenaLayout.Compute(Width, Height, new Rect(0, 0, Width, Height), true);
            Rect strip = ArenaLayout.ControlStrip(_arena);
            _stick = Point(strip, .25f);
            _boost = Point(strip, .70f);
            _guard = Point(strip, .95f);
            _adapter = new InputAdapter(0);
        }

        [Test]
        public void FreshBoostAndJitterNeverPressDefense()
        {
            Touch(1, TouchPhase.Began, _boost, 0);
            FighterInput first = _adapter.ConsumeTick();
            Assert.That(first.DashPressed && first.DashHeld, Is.True);
            Assert.That(first.DefensePressed || first.DefenseHeld, Is.False);
            Touch(1, TouchPhase.Moved, _boost + new Vector2(2, 2), .02);
            Touch(1, TouchPhase.Moved, _boost + new Vector2(-3, 1), .04);
            Touch(1, TouchPhase.Stationary, _boost + new Vector2(-3, 1), .20);
            AssertNoDefense(_adapter.ConsumeTick());
        }

        [Test]
        public void SlowDragCannotParryAndContinuousMotionCannotEarnAnotherPulse()
        {
            Touch(1, TouchPhase.Began, _boost, 0);
            _adapter.ConsumeTick();
            for (int i = 1; i <= 6; i++)
            {
                Touch(1, TouchPhase.Moved, _boost + new Vector2(i * 10, 0), i * .05);
                AssertNoDefense(_adapter.ConsumeTick());
            }
            Touch(1, TouchPhase.Moved, _boost + new Vector2(180, 0), .32);
            AssertNoDefense(_adapter.ConsumeTick());
        }

        [Test]
        public void FastSwipePressesOnceAndOverridesOppositeStickAimDuringBoost()
        {
            Touch(1, TouchPhase.Began, _stick, 0);
            Touch(1, TouchPhase.Moved, _stick + new Vector2(-80, 0), .01);
            Touch(2, TouchPhase.Began, _boost, .01);
            FighterInput ignition = _adapter.ConsumeTick();
            Assert.That(ignition.DashPressed && ignition.DashHeld, Is.True);
            Touch(2, TouchPhase.Moved, _boost + new Vector2(55, 0), .04);
            FighterInput swipe = _adapter.ConsumeTick();
            Assert.That(swipe.DefensePressed && swipe.DashHeld, Is.True);
            Assert.That(swipe.DefenseHeld || swipe.DashPressed, Is.False);
            Assert.That(swipe.Move.X, Is.EqualTo(1).Within(.001));
            Touch(2, TouchPhase.Moved, _boost + new Vector2(120, 0), .06);
            Touch(2, TouchPhase.Moved, _boost + new Vector2(180, 0), .08);
            AssertNoDefense(_adapter.ConsumeTick());
            FighterInput sustained = _adapter.ConsumeTick();
            AssertNoDefense(sustained);
            Assert.That(sustained.Move.X, Is.EqualTo(1).Within(.001));
        }

        [TestCase(55, 0, 1, 0)]
        [TestCase(-55, 0, -1, 0)]
        [TestCase(0, 55, 0, 1)]
        [TestCase(0, -55, 0, -1)]
        [TestCase(55, 55, .7071068, .7071068)]
        [TestCase(-55, -55, -.7071068, -.7071068)]
        public void SwipeDirectionBecomesTickAim(float dx, float dy, double x, double y)
        {
            Touch(1, TouchPhase.Began, _boost, 0);
            _adapter.ConsumeTick();
            Touch(1, TouchPhase.Moved, _boost + new Vector2(dx, dy), .03);
            FighterInput swipe = _adapter.ConsumeTick();
            Assert.That(swipe.DefensePressed, Is.True);
            Assert.That(swipe.Move.X, Is.EqualTo(x).Within(.001));
            Assert.That(swipe.Move.Y, Is.EqualTo(y).Within(.001));
        }

        [Test]
        public void StationaryStickKeepsSwipeAimUntilDeliberateStickMove()
        {
            Touch(1, TouchPhase.Began, _stick, 0);
            Touch(1, TouchPhase.Moved, _stick + new Vector2(-80, 0), .01);
            Touch(2, TouchPhase.Began, _boost, .01);
            _adapter.ConsumeTick();
            Touch(2, TouchPhase.Moved, _boost + new Vector2(0, 55), .04);
            _adapter.ConsumeTick();
            Touch(1, TouchPhase.Stationary, _stick + new Vector2(-80, 0), .05);
            Assert.That(_adapter.ConsumeTick().Move.Y, Is.EqualTo(1).Within(.001));
            Touch(1, TouchPhase.Moved, _stick + new Vector2(-81, 0), .06);
            Touch(1, TouchPhase.Moved, _stick + new Vector2(-82, 0), .07);
            Assert.That(_adapter.ConsumeTick().Move.Y, Is.EqualTo(1).Within(.001));
            Touch(1, TouchPhase.Moved, _stick + new Vector2(-84, 0), .08);
            FighterInput takenOver = _adapter.ConsumeTick();
            Assert.That(takenOver.Move.X, Is.LessThan(-.5));
            Assert.That(takenOver.Move.Y, Is.EqualTo(0).Within(.001));
        }

        [Test]
        public void DeliberateStickTakeoverBeforeTickAimsPendingParry()
        {
            Touch(1, TouchPhase.Began, _stick, 0);
            Touch(1, TouchPhase.Moved, _stick + new Vector2(-80, 0), .01);
            Touch(2, TouchPhase.Began, _boost, .01);
            _adapter.ConsumeTick();
            Touch(2, TouchPhase.Moved, _boost + new Vector2(0, 55), .04);
            Touch(1, TouchPhase.Moved, _stick + new Vector2(-81, 0), .05);
            Touch(1, TouchPhase.Moved, _stick + new Vector2(-82, 0), .06);
            Touch(1, TouchPhase.Moved, _stick + new Vector2(-84, 0), .07);
            FighterInput parry = _adapter.ConsumeTick();
            Assert.That(parry.DefensePressed, Is.True);
            Assert.That(parry.Move.X, Is.LessThan(-.5));
            Assert.That(parry.Move.Y, Is.EqualTo(0).Within(.001));
        }

        [Test]
        public void RawAimChangeTakesOverUntilAnotherSwipe()
        {
            Touch(1, TouchPhase.Began, _boost, 0);
            _adapter.ConsumeTick();
            Touch(1, TouchPhase.Moved, _boost + new Vector2(55, 0), .03);
            _adapter.ConsumeTick();
            _adapter.SampleRaw(new Vec2(0, 0), false, false, false);
            Assert.That(_adapter.ConsumeTick().Move.X, Is.EqualTo(1).Within(.001));
            _adapter.SampleRaw(new Vec2(-1, 0), false, false, false);
            Assert.That(_adapter.ConsumeTick().Move.X, Is.EqualTo(-1).Within(.001));
            Touch(1, TouchPhase.Stationary, _boost + new Vector2(55, 0), .20);
            Touch(1, TouchPhase.Moved, _boost + new Vector2(55, -55), .23);
            FighterInput rearmed = _adapter.ConsumeTick();
            Assert.That(rearmed.DefensePressed, Is.True);
            Assert.That(rearmed.Move.Y, Is.EqualTo(-1).Within(.001));
            Assert.That(_adapter.ConsumeTick().Move.Y, Is.EqualTo(-1).Within(.001));
        }

        [Test]
        public void QuietPauseRearmsSwipeAndLateFlickAfterHoldWorks()
        {
            Touch(1, TouchPhase.Began, _boost, 0);
            _adapter.ConsumeTick();
            Touch(1, TouchPhase.Stationary, _boost, .50);
            Touch(1, TouchPhase.Moved, _boost + new Vector2(55, 0), .53);
            Assert.That(_adapter.ConsumeTick().DefensePressed, Is.True);
            Touch(1, TouchPhase.Stationary, _boost + new Vector2(55, 0), .60);
            Touch(1, TouchPhase.Stationary, _boost + new Vector2(55, 0), .73);
            Touch(1, TouchPhase.Moved, _boost + new Vector2(55, -55), .76);
            FighterInput second = _adapter.ConsumeTick();
            Assert.That(second.DefensePressed && second.DashHeld, Is.True);
            Assert.That(second.DashPressed || second.DefenseHeld, Is.False);
            Assert.That(second.Move.Y, Is.EqualTo(-1).Within(.001));
        }

        [Test]
        public void GuardTapHoldAndSeparateSourcesRetainTheirOwnRelease()
        {
            Touch(1, TouchPhase.Began, _boost, 0);
            Touch(2, TouchPhase.Began, _guard, .01);
            FighterInput both = _adapter.ConsumeTick();
            Assert.That(both.DashHeld && both.DefensePressed && both.DefenseHeld, Is.True);
            Touch(2, TouchPhase.Stationary, _guard, .20);
            FighterInput guardHeld = _adapter.ConsumeTick();
            Assert.That(guardHeld.DefenseHeld && guardHeld.DashHeld, Is.True);
            Assert.That(guardHeld.DefensePressed, Is.False);
            Touch(2, TouchPhase.Ended, _guard, .21);
            FighterInput boostOnly = _adapter.ConsumeTick();
            Assert.That(boostOnly.DashHeld, Is.True);
            Assert.That(boostOnly.DefenseHeld, Is.False);
            Touch(1, TouchPhase.Ended, _boost, .22);
            Assert.That(_adapter.ConsumeTick().DashHeld, Is.False);
        }

        [Test]
        public void MultipleBoostFingersHoldUntilTheLastSourceReleases()
        {
            Touch(1, TouchPhase.Began, _boost, 0);
            Touch(2, TouchPhase.Began, _boost + new Vector2(80, 0), .01);
            Assert.That(_adapter.ConsumeTick().DashPressed, Is.True);
            Touch(1, TouchPhase.Ended, _boost, .02);
            FighterInput remaining = _adapter.ConsumeTick();
            Assert.That(remaining.DashHeld, Is.True);
            Assert.That(remaining.DashPressed || remaining.DefensePressed, Is.False);
            Touch(2, TouchPhase.Ended, _boost + new Vector2(80, 0), .03);
            Assert.That(_adapter.ConsumeTick().DashHeld, Is.False);
        }

        [Test]
        public void CancelMenuAndDuplicateBeganDoNotCreateSwipeOrStealRole()
        {
            Touch(1, TouchPhase.Began, _boost, 0);
            _adapter.ConsumeTick();
            Touch(1, TouchPhase.Began, _guard, .02);
            Touch(1, TouchPhase.Moved, _boost + new Vector2(55, 0), .04);
            FighterInput locked = _adapter.ConsumeTick();
            Assert.That(locked.DefensePressed && locked.DashHeld, Is.True);
            Assert.That(locked.DefenseHeld, Is.False);
            Touch(1, TouchPhase.Canceled, _boost + new Vector2(150, 0), .05);
            FighterInput canceled = _adapter.ConsumeTick();
            Assert.That(canceled.DashHeld, Is.False);
            Assert.That(_adapter.ConsumeTick().Move.X, Is.EqualTo(0).Within(.001));
            Touch(2, TouchPhase.Began, _boost, .1);
            _adapter.ConsumeTick();
            _adapter.SampleTouch(2, TouchPhase.Moved, _boost + new Vector2(60, 0),
                _arena, Height, false, true, true, .13);
            FighterInput cleared = _adapter.ConsumeTick();
            Assert.That(cleared.DashHeld || cleared.DefensePressed, Is.False);
        }

        [Test]
        public void FinalFastFlickOnEndedSurvivesOneFixedTick()
        {
            Touch(1, TouchPhase.Began, _boost, 0);
            _adapter.ConsumeTick();
            Touch(1, TouchPhase.Ended, _boost + new Vector2(55, 0), .03);
            FighterInput edge = _adapter.ConsumeTick();
            Assert.That(edge.DefensePressed, Is.True);
            Assert.That(edge.DashHeld || edge.DefenseHeld, Is.False);
            Assert.That(edge.Move.X, Is.EqualTo(1).Within(.001));
            FighterInput fallback = _adapter.ConsumeTick();
            AssertNoDefense(fallback);
            Assert.That(fallback.Move.X, Is.EqualTo(0).Within(.001));
        }

        [Test]
        public void SwipeParryDuringFuelBoostUsesSwipeAimAndDoesNotRestartIgnition()
        {
            var tuning = new CombatTuning(new MovementTuning(useFuelBoost: true),
                CombatTuning.Default.Projectile, CombatTuning.Default.Defense,
                CombatTuning.Default.Divider, CombatTuning.Default.Arena, CombatTuning.Default.Match);
            var config = new SimulationConfig();
            var state = new SimulationState { Phase = MatchPhase.Active };
            FighterState fighter = state.Fighter(0);
            fighter.Position = fighter.PreviousPosition = fighter.TickStartPosition = new Vec2(25, 30);
            fighter.Grounded = false;
            fighter.FuelSeconds = tuning.Movement.FuelCapacitySeconds;
            var events = new List<SimulationEvent>();
            Touch(1, TouchPhase.Began, _stick, 0);
            Touch(1, TouchPhase.Moved, _stick + new Vector2(-80, 0), .01);
            Touch(2, TouchPhase.Began, _boost, .01);
            FighterInput ignition = _adapter.ConsumeTick();
            Advance(ignition);
            int sequence = fighter.DashSequence;
            double fuel = fighter.FuelSeconds;
            Touch(2, TouchPhase.Moved, _boost + new Vector2(55, 0), .04);
            FighterInput swipe = _adapter.ConsumeTick();
            Assert.That(swipe.Move.X, Is.GreaterThan(.5));
            Assert.That(swipe.DefensePressed && swipe.DashHeld, Is.True);
            Assert.That(swipe.DefenseHeld || swipe.DashPressed, Is.False);
            Advance(swipe);
            Assert.That(DefenseSystem.CanParry(fighter), Is.True);
            Assert.That(fighter.FuelBoostActive, Is.True);
            Assert.That(fighter.DashSequence, Is.EqualTo(sequence));
            Assert.That(fighter.FuelSeconds, Is.LessThan(fuel));
            Assert.That(fighter.DashDirection.X, Is.GreaterThan(.5));

            void Advance(FighterInput input)
            {
                var step = new SimulationInput(state.Tick, input, default);
                DefenseSystem.BeginTick(state, step, tuning, config);
                MovementSystem.AdvanceKinematics(state, step, tuning, config, events);
                state.Tick++;
            }
        }

        [Test]
        public void SwipeDuringBoostParriesIncomingProjectileInFullSimulation()
        {
            var tuning = new CombatTuning(new MovementTuning(useFuelBoost: true),
                CombatTuning.Default.Projectile, CombatTuning.Default.Defense,
                CombatTuning.Default.Divider, CombatTuning.Default.Arena, CombatTuning.Default.Match);
            var simulation = new MatchSimulation(tuning: tuning);
            SimulationState setup = simulation.CaptureSnapshot();
            setup.Phase = MatchPhase.Active;
            setup.NextSpawnTick = 1000;
            FighterState fighter = setup.Fighter(0);
            fighter.Position = fighter.PreviousPosition = fighter.TickStartPosition = new Vec2(25, 30);
            fighter.Grounded = false;
            fighter.FuelSeconds = tuning.Movement.FuelCapacitySeconds;
            simulation.RestoreSnapshot(setup);
            Touch(1, TouchPhase.Began, _stick, 0);
            Touch(1, TouchPhase.Moved, _stick + new Vector2(-80, -80), .01);
            Touch(2, TouchPhase.Began, _boost, .01);
            simulation.Step(new SimulationInput(simulation.Tick, _adapter.ConsumeTick(), default));
            for (int i = 0; i < 8; i++)
                simulation.Step(new SimulationInput(simulation.Tick, _adapter.ConsumeTick(), default));
            setup = simulation.CaptureSnapshot();
            fighter = setup.Fighter(0);
            Assert.That(fighter.DashInvulnerabilityTicksRemaining, Is.Zero);
            int sequence = fighter.DashSequence;
            Vec2 incoming = new Vec2(fighter.Position.X + tuning.Arena.FighterRadiusUnits +
                tuning.Arena.ProjectileRadiusUnits + .5, fighter.Position.Y);
            setup.Projectiles.Add(new ProjectileState { Id = 999, Position = incoming,
                PreviousPosition = incoming, Velocity = new Vec2(-24, 0) });
            simulation.RestoreSnapshot(setup);
            Touch(2, TouchPhase.Stationary, _boost, .20);
            Touch(2, TouchPhase.Moved, _boost + new Vector2(55, 55), .23);
            FighterInput swipe = _adapter.ConsumeTick();
            Assert.That(swipe.Move.X, Is.GreaterThan(.5));
            Assert.That(swipe.Move.Y, Is.GreaterThan(.5));
            var events = simulation.Step(new SimulationInput(simulation.Tick, swipe, default));
            bool parried = false;
            foreach (SimulationEvent item in events)
                if (item.Type == SimulationEventType.PerfectParry) parried = true;
            Assert.That(parried, Is.True);
            var result = simulation.CaptureSnapshot();
            Assert.That(result.Fighter(0).Eliminated, Is.False);
            Assert.That(result.Fighter(0).FuelBoostActive, Is.True);
            Assert.That(result.Fighter(0).DashSequence, Is.EqualTo(sequence));
            Assert.That(result.Projectiles[0].PowerLevel, Is.EqualTo(1));
            Assert.That(result.Projectiles[0].Velocity.Y, Is.GreaterThan(0));
        }

        [Test]
        public void EmptyFuelDoesNotSuppressSwipeParryInput()
        {
            Touch(1, TouchPhase.Began, _boost, 0);
            _adapter.ConsumeTick();
            Touch(1, TouchPhase.Moved, _boost + new Vector2(55, 0), .03);
            FighterInput swipe = _adapter.ConsumeTick();
            var tuning = new CombatTuning(new MovementTuning(useFuelBoost: true),
                CombatTuning.Default.Projectile, CombatTuning.Default.Defense,
                CombatTuning.Default.Divider, CombatTuning.Default.Arena, CombatTuning.Default.Match);
            var config = new SimulationConfig();
            var state = new SimulationState { Phase = MatchPhase.Active };
            FighterState fighter = state.Fighter(0);
            fighter.FuelSeconds = 0;
            var command = new SimulationInput(0, swipe, default);
            DefenseSystem.BeginTick(state, command, tuning, config);
            MovementSystem.AdvanceKinematics(state, command, tuning, config, new List<SimulationEvent>());
            Assert.That(DefenseSystem.CanParry(fighter), Is.True);
            Assert.That(fighter.FuelBoostActive, Is.False);
        }

        private void Touch(int id, TouchPhase phase, Vector2 position, double time) =>
            _adapter.SampleTouch(id, phase, position, _arena, Height, false, true, false, time);

        private static Vector2 Point(Rect strip, float fraction) =>
            new Vector2(strip.xMin + strip.width * fraction, Height - strip.center.y);

        private static void AssertNoDefense(FighterInput input) =>
            Assert.That(input.DefensePressed || input.DefenseHeld, Is.False);
    }
}
