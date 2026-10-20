using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Unity.Tests
{
    public sealed class PhonePresentationTests
    {
        [TestCase(2868, 1320, 100, 35, 2668, 1250)]
        [TestCase(640, 360, 20, 0, 600, 350)]
        [TestCase(1280, 720, 0, 0, 1280, 720)]
        public void PhoneArenaAndControlStripPartitionSafeArea(int width, int height,
            int safeX, int safeY, int safeWidth, int safeHeight)
        {
            Rect safe = new Rect(safeX, safeY, safeWidth, safeHeight);
            Rect arena = ArenaLayout.Compute(width, height, safe, true);
            Rect footer = ArenaLayout.Footer(width, height, safe, true);
            Rect strip = ArenaLayout.ControlStrip(arena);
            Assert.That(arena, Is.EqualTo(new Rect(safeX, height - safeY - safeHeight,
                safeWidth, safeHeight * .78f)));
            Assert.That(strip.xMin, Is.EqualTo(safeX));
            Assert.That(strip.yMin, Is.EqualTo(arena.yMax));
            Assert.That(strip.width, Is.EqualTo(safeWidth));
            Assert.That(strip.height, Is.EqualTo(safeHeight * .22f).Within(.01f));
            Assert.That(strip.yMax, Is.EqualTo(height - safeY).Within(.01f));
            Assert.That(footer, Is.EqualTo(Rect.zero));

            Rect pause = ArenaLayout.PauseButton(arena);
            Rect panel = ArenaLayout.PauseMenuPanel(arena);
            Assert.That(arena.Contains(pause.min) && arena.Contains(pause.max - Vector2.one), Is.True);
            Assert.That(arena.Contains(panel.min) && arena.Contains(panel.max - Vector2.one), Is.True);
            Assert.That(pause.height, Is.EqualTo(arena.height * .10f).Within(.01f));
            Assert.That(pause.width, Is.EqualTo(arena.height * .18f).Within(.01f));
            Assert.That(pause.xMax, Is.GreaterThan(arena.center.x));
            Vector2 pauseTouch = new Vector2(pause.center.x, height - pause.center.y);
            Assert.That(ArenaLayout.TouchAction(arena, pauseTouch, height, false, true), Is.EqualTo(-1));

            Rect previous = Rect.zero;
            for (int i = 0; i < 4; i++)
            {
                Rect button = ArenaLayout.PauseMenuButton(arena, i);
                Assert.That(panel.Contains(button.min) && panel.Contains(button.max - Vector2.one), Is.True);
                Assert.That(button.width, Is.GreaterThan(button.height));
                Assert.That(button.Overlaps(pause), Is.False);
                Assert.That(ArenaLayout.ToolbarButton(arena, i, true), Is.EqualTo(button));
                if (i > 0) Assert.That(button.yMin, Is.GreaterThan(previous.yMax));
                previous = button;
            }
            Assert.That(ArenaLayout.PauseMenuButton(arena, 4), Is.EqualTo(Rect.zero));

            for (int action = 0; action < 3; action++)
            {
                Rect target = ArenaLayout.TouchControl(arena, action, true);
                Assert.That(strip.Contains(target.min) && strip.Contains(target.max - Vector2.one), Is.True);
                Assert.That(target.height, Is.LessThanOrEqualTo(strip.height * .65f + .01f));
                Assert.That(target.Overlaps(arena), Is.False);
                Vector2 bottomLeft = new Vector2(target.center.x, height - target.center.y);
                Assert.That(ArenaLayout.TouchAction(arena, bottomLeft, height, false, true), Is.EqualTo(action));
                Assert.That(ArenaLayout.TouchAction(arena, bottomLeft, height, false, true, true), Is.EqualTo(-1));
                Assert.That(target.Overlaps(pause), Is.False);
                for (int i = 0; i < 4; i++)
                    Assert.That(target.Overlaps(ArenaLayout.PauseMenuButton(arena, i)), Is.False);
            }
            Assert.That(ArenaLayout.TouchAction(arena,
                new Vector2(strip.xMin + strip.width * .43f, height - strip.center.y),
                height, false, true), Is.EqualTo(0));
            Assert.That(ArenaLayout.TouchAction(arena,
                new Vector2(strip.xMin + strip.width * .60f, height - strip.center.y),
                height, false, true), Is.EqualTo(-1));
            Assert.That(ArenaLayout.TouchAction(arena,
                new Vector2(arena.xMin + arena.width * .15f, height - arena.center.y),
                height, false, true), Is.EqualTo(-1));
            Assert.That(ArenaLayout.TouchAction(arena,
                new Vector2(strip.xMin - 1, height - strip.center.y),
                height, false, true), Is.EqualTo(-1));
            Assert.That(ArenaLayout.TuningPanel(arena, true).Overlaps(pause), Is.False);
        }

        [Test]
        public void DesktopDefaultsKeepTheirOriginalLayout()
        {
            Rect safe = new Rect(0, 0, 1920, 1080);
            Rect arena = ArenaLayout.Compute(1920, 1080, safe);
            Assert.That(arena, Is.EqualTo(ArenaLayout.Compute(1920, 1080, safe, false)));
            Assert.That(ArenaLayout.ToolbarButton(arena, 0),
                Is.EqualTo(ArenaLayout.ToolbarButton(arena, 0, false)));
            Assert.That(ArenaLayout.Footer(1920, 1080, safe),
                Is.EqualTo(ArenaLayout.Footer(1920, 1080, safe, false)));
        }

        [Test]
        public void MobileTouchInputKeepsSimultaneousActionsAndClearsThemForMenu()
        {
            const int width = 2868, height = 1320;
            Rect arena = ArenaLayout.Compute(width, height, new Rect(100, 35, 2668, 1250), true);
            var adapter = new InputAdapter(0);
            Rect pause = ArenaLayout.PauseButton(arena);
            Vector2 pausePoint = new Vector2(pause.center.x, height - pause.center.y);
            adapter.SampleTouch(5, TouchPhase.Began, pausePoint, arena, height, false, true);
            Assert.That(adapter.ConsumeTick().DashPressed, Is.False);

            Rect stick = ArenaLayout.TouchControl(arena, 0, true);
            Vector2 stickPoint = new Vector2(stick.center.x, height - stick.center.y);
            adapter.SampleTouch(10, TouchPhase.Began, stickPoint, arena, height, false, true);
            adapter.SampleTouch(10, TouchPhase.Moved, stickPoint + new Vector2(60, 0), arena, height, false, true);
            for (int action = 1; action <= 2; action++)
            {
                Vector2 center = ArenaLayout.TouchControl(arena, action, true).center;
                adapter.SampleTouch(action + 10, TouchPhase.Began,
                    new Vector2(center.x, height - center.y), arena, height, false, true);
            }
            FighterInput actionInput = adapter.ConsumeTick();
            Assert.That(actionInput.DashPressed && actionInput.DefensePressed, Is.True);
            Assert.That(actionInput.Move.X, Is.GreaterThan(0));

            adapter.SampleTouch(12, TouchPhase.Stationary, pausePoint, arena, height,
                false, true, true);
            FighterInput menuInput = adapter.ConsumeTick();
            Assert.That(menuInput.Move.X, Is.EqualTo(0));
            Assert.That(menuInput.DashPressed || menuInput.DefensePressed ||
                menuInput.DashHeld || menuInput.DefenseHeld, Is.False);

            Vector2 dash = ArenaLayout.TouchControl(arena, 1, true).center;
            adapter.SampleTouch(13, TouchPhase.Began, new Vector2(dash.x, height - dash.y),
                arena, height, false, true, true);
            Assert.That(adapter.ConsumeTick().DashPressed, Is.False);

            adapter.SampleTouch(14, TouchPhase.Began, new Vector2(dash.x, height - dash.y),
                arena, height, false, true);
            Assert.That(adapter.ConsumeTick().DashPressed, Is.True);
        }

        [Test]
        public void FloatingJoystickKeepsItsOriginAndFingerUntilReleased()
        {
            const int height = 1320;
            Rect arena = ArenaLayout.Compute(2868, height, new Rect(100, 35, 2668, 1250), true);
            Rect strip = ArenaLayout.ControlStrip(arena);
            var adapter = new InputAdapter(0);
            Vector2 first = new Vector2(strip.xMin + strip.width * .40f, height - strip.center.y);
            Vector2 second = new Vector2(strip.xMin + strip.width * .12f, height - strip.center.y);
            float travel = ArenaLayout.JoystickTravel(arena, true);

            adapter.SampleTouch(1, TouchPhase.Began, first, arena, height, false, true);
            Assert.That(adapter.JoystickActive, Is.True);
            Assert.That(adapter.JoystickOrigin, Is.EqualTo(first));
            adapter.SampleTouch(2, TouchPhase.Began, second, arena, height, false, true);
            adapter.SampleTouch(2, TouchPhase.Moved, second + new Vector2(0, travel), arena,
                height, false, true);
            Assert.That(adapter.JoystickOrigin, Is.EqualTo(first));
            Assert.That(adapter.JoystickPoint, Is.EqualTo(first));

            Vector2 dragged = first + new Vector2(travel * .6f, travel * .8f);
            adapter.SampleTouch(1, TouchPhase.Moved, dragged, arena, height, false, true);
            FighterInput diagonal = adapter.ConsumeTick();
            Assert.That(adapter.JoystickOrigin, Is.EqualTo(first));
            Assert.That(adapter.JoystickPoint, Is.EqualTo(dragged));
            Assert.That(diagonal.Move.X, Is.EqualTo(.6f).Within(.001f));
            Assert.That(diagonal.Move.Y, Is.EqualTo(.8f).Within(.001f));

            adapter.SampleTouch(1, TouchPhase.Moved, first + new Vector2(travel * 2, 0),
                arena, height, false, true);
            Assert.That(adapter.ConsumeTick().Move.X, Is.EqualTo(1).Within(.001f));
            adapter.SampleTouch(1, TouchPhase.Ended, first, arena, height, false, true);
            Assert.That(adapter.JoystickActive, Is.False);
            Assert.That(adapter.JoystickOrigin, Is.EqualTo(Vector2.zero));
            Assert.That(adapter.ConsumeTick().Move.X, Is.EqualTo(0));
            adapter.SampleTouch(2, TouchPhase.Moved, second + new Vector2(0, travel), arena,
                height, false, true);
            Assert.That(adapter.JoystickActive, Is.False);

            adapter.SampleTouch(3, TouchPhase.Began, second, arena, height, false, true);
            Assert.That(adapter.JoystickOrigin, Is.EqualTo(second));
            adapter.SampleTouch(3, TouchPhase.Canceled, second, arena, height, false, true);
            Assert.That(adapter.JoystickActive, Is.False);
            Assert.That(adapter.ConsumeTick().Move.Y, Is.EqualTo(0));
            adapter.SampleTouch(4, TouchPhase.Began, first, arena, height, false, true);
            adapter.SampleTouch(4, TouchPhase.Moved, first + Vector2.right * travel, arena,
                height, false, true);
            adapter.SampleTouch(4, TouchPhase.Stationary, first, arena, height, false, true, true);
            Assert.That(adapter.JoystickActive, Is.False);
            Assert.That(adapter.ConsumeTick().Move.X, Is.EqualTo(0));
        }
    }
}
