using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;

namespace Game.Unity.Tests
{
    public sealed class FighterAnimationTests
    {
        private const int TickRate = 60;

        [Test]
        public void PosePriorityAndSheetCoordinatesFollowFighterState()
        {
            var selector = new FighterAnimationPresentation(0);
            var fighter = new FighterState { Id = 0, Grounded = true };
            AssertFrame(selector.Sample(fighter, 0, TickRate, true, .15), FighterAnimationPose.Idle, 0);

            fighter.Velocity = new Vec2(5, 0);
            AssertFrame(selector.Sample(fighter, 5, TickRate, true, .15), FighterAnimationPose.Run, 1);
            fighter.DefenseHeldPrevious = true;
            fighter.DefenseHoldTicks = 8;
            AssertFrame(selector.Sample(fighter, 5, TickRate, true, .15), FighterAnimationPose.Run, 1);
            fighter.DefenseHoldTicks = 9;
            AssertFrame(selector.Sample(fighter, 5, TickRate, true, .15), FighterAnimationPose.Guard, 1);
            fighter.FuelBoostActive = true;
            AssertFrame(selector.Sample(fighter, 5, TickRate, true, .15), FighterAnimationPose.Boost, 1);
            fighter.ParryTicksRemaining = 2;
            AssertFrame(selector.Sample(fighter, 5, TickRate, true, .15), FighterAnimationPose.Parry, 1);
            fighter.GuardBreakTicksRemaining = 3;
            AssertFrame(selector.Sample(fighter, 5, TickRate, true, .15), FighterAnimationPose.Recoil, 1);
            fighter.Eliminated = true;
            AssertFrame(selector.Sample(fighter, 5, TickRate, true, .15), FighterAnimationPose.Recoil, 3);
        }

        [Test]
        public void ImpactAndSuccessfulParryArePerFighterAndExpireOnTick()
        {
            var selector = new FighterAnimationPresentation(0);
            var fighter = new FighterState { Id = 0, Grounded = true };
            selector.Observe(new List<SimulationEvent>
            {
                new SimulationEvent(100, SimulationEventType.BodyImpact, 0, 1),
                new SimulationEvent(100, SimulationEventType.PerfectParry, 7, 1)
            }, TickRate);
            AssertFrame(selector.Sample(fighter, 100, TickRate, true, .15), FighterAnimationPose.Idle, 0);

            selector.Observe(new List<SimulationEvent>
            {
                new SimulationEvent(100, SimulationEventType.PerfectParry, 7, 0)
            }, TickRate);
            AssertFrame(selector.Sample(fighter, 100, TickRate, true, .15), FighterAnimationPose.Parry, 0);
            AssertFrame(selector.Sample(fighter, 109, TickRate, true, .15), FighterAnimationPose.Parry, 1);
            AssertFrame(selector.Sample(fighter, 110, TickRate, true, .15), FighterAnimationPose.Idle, 2);

            selector.Observe(new List<SimulationEvent>
            {
                new SimulationEvent(120, SimulationEventType.BodyImpact, 1, 0)
            }, TickRate);
            AssertFrame(selector.Sample(fighter, 120, TickRate, true, .15), FighterAnimationPose.Recoil, 0);
            AssertFrame(selector.Sample(fighter, 134, TickRate, true, .15), FighterAnimationPose.Recoil, 2);
            AssertFrame(selector.Sample(fighter, 135, TickRate, true, .15), FighterAnimationPose.Idle, 3);
        }

        [Test]
        public void RoundAndManualResetClearTransientPoses()
        {
            var selector = new FighterAnimationPresentation(0);
            var fighter = new FighterState { Id = 0, Grounded = true };
            selector.Observe(new[] { new SimulationEvent(10, SimulationEventType.DividerRebound, 0) }, TickRate);
            Assert.That(selector.Sample(fighter, 10, TickRate, false, .15).Pose, Is.EqualTo(FighterAnimationPose.Recoil));
            selector.Observe(new[] { new SimulationEvent(11, SimulationEventType.RoundStarted) }, TickRate);
            Assert.That(selector.Sample(fighter, 11, TickRate, false, .15).Pose, Is.EqualTo(FighterAnimationPose.Idle));
            selector.Observe(new[] { new SimulationEvent(12, SimulationEventType.GuardBroken, 0) }, TickRate);
            selector.Reset();
            Assert.That(selector.Sample(fighter, 12, TickRate, false, .15).Pose, Is.EqualTo(FighterAnimationPose.Idle));
        }

        [Test]
        public void AirFallbackAndFrameWrapAreStableAcrossRepeatedSamples()
        {
            var selector = new FighterAnimationPresentation(0);
            var fighter = new FighterState { Id = 0, Grounded = false, Velocity = new Vec2(0, 4) };
            AssertFrame(selector.Sample(fighter, 10, TickRate, false, .15), FighterAnimationPose.Boost, 0);
            fighter.Velocity = new Vec2(0, -4);
            AssertFrame(selector.Sample(fighter, 10, TickRate, false, .15), FighterAnimationPose.Boost, 2);
            fighter.Grounded = true;
            AssertFrame(selector.Sample(fighter, 20, TickRate, false, .15), FighterAnimationPose.Idle, 0);
            AssertFrame(selector.Sample(fighter, 39, TickRate, false, .15), FighterAnimationPose.Idle, 3);
            AssertFrame(selector.Sample(fighter, 40, TickRate, false, .15), FighterAnimationPose.Idle, 0);
            AssertFrame(selector.Sample(fighter, 40, TickRate, false, .15), FighterAnimationPose.Idle, 0);
        }

        [Test]
        public void LegacyDividerLoadBoostYieldsToReboundRecoil()
        {
            var selector = new FighterAnimationPresentation(0);
            var fighter = new FighterState { Id = 0, Grounded = true, DividerLoadTicksRemaining = 4 };
            AssertFrame(selector.Sample(fighter, 5, TickRate, false, .15), FighterAnimationPose.Boost, 1);
            fighter.DividerReboundDistanceRemaining = 2;
            AssertFrame(selector.Sample(fighter, 5, TickRate, false, .15), FighterAnimationPose.Recoil, 1);
        }

        [Test]
        public void ObservationAndSamplingDoNotChangeAuthoritativeChecksum()
        {
            var state = new SimulationState();
            FighterState fighter = state.Fighter(0);
            fighter.Grounded = true;
            fighter.FuelBoostActive = true;
            fighter.ParryTicksRemaining = 3;
            ulong before = state.ComputeChecksum();
            var selector = new FighterAnimationPresentation(0);
            selector.Observe(new[] { new SimulationEvent(8, SimulationEventType.PerfectParry, 12, 0) }, TickRate);
            selector.Sample(fighter, 8, TickRate, true, .15);
            selector.Sample(fighter, 9, TickRate, true, .15);
            Assert.That(state.ComputeChecksum(), Is.EqualTo(before));
        }

        private static void AssertFrame(FighterAnimationFrame frame, FighterAnimationPose pose, int column)
        {
            Assert.That(frame.Pose, Is.EqualTo(pose));
            Assert.That(frame.Row, Is.EqualTo((int)pose));
            Assert.That(frame.Column, Is.EqualTo(column));
        }
    }
}
