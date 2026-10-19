using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Unity.Tests
{
    public sealed class InputAdapterTests
    {
        [Test]
        public void SimultaneousShortPressesSurviveUntilNextFixedTick()
        {
            var adapter = new InputAdapter(0);
            adapter.SampleRaw(new Vec2(1, 0), true, true, true);
            adapter.SampleRaw(new Vec2(0, 0), false, false, false);
            FighterInput tick = adapter.ConsumeTick();
            Assert.That(tick.JumpPressed && tick.DashPressed && tick.DefensePressed, Is.True);
            Assert.That(tick.JumpHeld || tick.DefenseHeld, Is.False);
            FighterInput next = adapter.ConsumeTick();
            Assert.That(next.JumpPressed || next.DashPressed || next.DefensePressed, Is.False);
        }

        [Test]
        public void HeldActionProducesOnePressEdgeUntilReleasedAndPressedAgain()
        {
            var adapter = new InputAdapter(0);
            adapter.SampleRaw(default(Vec2), true, false, false);
            Assert.That(adapter.ConsumeTick().JumpPressed, Is.True);
            adapter.SampleRaw(default(Vec2), true, false, false);
            FighterInput held = adapter.ConsumeTick();
            Assert.That(held.JumpHeld, Is.True);
            Assert.That(held.JumpPressed, Is.False);
            adapter.SampleRaw(default(Vec2), false, false, false);
            adapter.SampleRaw(default(Vec2), true, false, false);
            Assert.That(adapter.ConsumeTick().JumpPressed, Is.True);
        }

        [Test]
        public void SeparateFingersKeepStickBoostAndDefenseIndependent()
        {
            const int width = 1920, height = 1080;
            Rect arena = ArenaLayout.Compute(width, height, new Rect(0, 0, width, height));
            var adapter = new InputAdapter(0);
            Vector2 stick = BottomLeft(ArenaLayout.TouchControl(arena, 0).center, height);
            Vector2 boost = BottomLeft(ArenaLayout.TouchControl(arena, 1).center, height);
            Vector2 defense = BottomLeft(ArenaLayout.TouchControl(arena, 2).center, height);

            adapter.SampleTouch(11, TouchPhase.Began, stick, arena, height);
            adapter.SampleTouch(12, TouchPhase.Began, boost, arena, height);
            adapter.SampleTouch(13, TouchPhase.Began, defense, arena, height);
            adapter.SampleTouch(11, TouchPhase.Moved, stick + new Vector2(90, 0), arena, height);
            FighterInput first = adapter.ConsumeTick();
            Assert.That(first.Move.X, Is.GreaterThan(0.5));
            Assert.That(first.DashPressed, Is.True);
            Assert.That(first.JumpPressed || first.JumpHeld, Is.False);
            Assert.That(first.DefensePressed && first.DefenseHeld, Is.True);

            adapter.SampleTouch(12, TouchPhase.Ended, boost, arena, height);
            FighterInput second = adapter.ConsumeTick();
            Assert.That(second.DashPressed, Is.False);
            Assert.That(second.DefenseHeld, Is.True);
            Assert.That(second.Move.X, Is.GreaterThan(0.5));
            Assert.That(second.DefensePressed, Is.False);

            adapter.SampleTouch(11, TouchPhase.Ended, stick, arena, height);
            adapter.SampleTouch(13, TouchPhase.Ended, defense, arena, height);
            FighterInput released = adapter.ConsumeTick();
            Assert.That(released.DefenseHeld, Is.False);
            Assert.That(released.Move.X, Is.EqualTo(0));
        }

        [Test]
        public void TouchLayoutHasOnlyMoveBoostAndDefenseRegions()
        {
            const int width = 1920, height = 1080;
            Rect arena = ArenaLayout.Compute(width, height, new Rect(0, 0, width, height));
            for (int action = 0; action <= 2; action++)
                Assert.That(ArenaLayout.TouchAction(arena,
                    BottomLeft(ArenaLayout.TouchControl(arena, action).center, height), height, false),
                    Is.EqualTo(action));
            Assert.That(ArenaLayout.TouchControl(arena, 3), Is.EqualTo(Rect.zero));
        }

        private static Vector2 BottomLeft(Vector2 gui, int height) => new Vector2(gui.x, height - gui.y);
    }
}
