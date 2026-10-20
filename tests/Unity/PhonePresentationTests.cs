using Game.Core;
using NUnit.Framework;
using UnityEngine;

namespace Game.Unity.Tests
{
    public sealed class PhonePresentationTests
    {
        [TestCase(2868, 1320, 100, 35, 2668, 1250)]
        [TestCase(640, 360, 20, 0, 600, 350)]
        public void PhoneControlsFitSafeAreaAndRemainSeparate(int width, int height,
            int safeX, int safeY, int safeWidth, int safeHeight)
        {
            Rect safe = new Rect(safeX, safeY, safeWidth, safeHeight);
            Rect arena = ArenaLayout.Compute(width, height, safe, true);
            Rect footer = ArenaLayout.Footer(width, height, safe, true);
            Assert.That(arena.width / arena.height, Is.EqualTo(16f / 9f).Within(.0001f));
            Assert.That(arena.xMin, Is.GreaterThanOrEqualTo(safe.xMin));
            Assert.That(arena.xMax, Is.LessThanOrEqualTo(safe.xMax));
            Assert.That(arena.yMin, Is.GreaterThanOrEqualTo(height - safe.yMax));
            Assert.That(footer.yMax, Is.LessThanOrEqualTo(height - safe.yMin + .01f));
            Assert.That(footer.yMin, Is.GreaterThan(arena.yMax));

            Rect previous = Rect.zero;
            for (int i = 0; i < 4; i++)
            {
                Rect button = ArenaLayout.ToolbarButton(arena, i, true);
                Assert.That(arena.Contains(button.min) && arena.Contains(button.max - Vector2.one), Is.True);
                Assert.That(button.yMin, Is.GreaterThan(arena.yMin + arena.height * 76f / 360f),
                    "Toolbar should clear the phone HUD's fuel label.");
                Assert.That(button.height, Is.GreaterThanOrEqualTo(arena.height * .119f));
                Vector2 buttonTouch = new Vector2(button.center.x, height - button.center.y);
                Assert.That(ArenaLayout.TouchAction(arena, buttonTouch, height, false, true), Is.EqualTo(-1));
                if (i > 0) Assert.That(button.xMin, Is.GreaterThan(previous.xMax));
                previous = button;
            }

            for (int action = 0; action < 3; action++)
            {
                Rect target = ArenaLayout.TouchControl(arena, action);
                Assert.That(arena.Contains(target.center), Is.True);
                Vector2 bottomLeft = new Vector2(target.center.x, height - target.center.y);
                Assert.That(ArenaLayout.TouchAction(arena, bottomLeft, height, false, true), Is.EqualTo(action));
                for (int i = 0; i < 4; i++)
                    Assert.That(target.Overlaps(ArenaLayout.ToolbarButton(arena, i, true)), Is.False);
            }
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
        public void MobileTouchInputUsesPhoneLayoutForToolbarAndActions()
        {
            const int width = 2868, height = 1320;
            Rect arena = ArenaLayout.Compute(width, height, new Rect(100, 35, 2668, 1250), true);
            var adapter = new InputAdapter(0);
            for (int i = 0; i < 4; i++)
            {
                Vector2 center = ArenaLayout.ToolbarButton(arena, i, true).center;
                Vector2 point = new Vector2(center.x, height - center.y);
                adapter.SampleTouch(i, TouchPhase.Began, point, arena, height, false, true);
                adapter.SampleTouch(i, TouchPhase.Ended, point, arena, height, false, true);
            }
            FighterInput toolbarInput = adapter.ConsumeTick();
            Assert.That(toolbarInput.DashPressed || toolbarInput.DefensePressed, Is.False);
            Assert.That(toolbarInput.Move.X, Is.EqualTo(0));

            for (int action = 1; action <= 2; action++)
            {
                Vector2 center = ArenaLayout.TouchControl(arena, action).center;
                adapter.SampleTouch(action + 10, TouchPhase.Began,
                    new Vector2(center.x, height - center.y), arena, height, false, true);
            }
            FighterInput actionInput = adapter.ConsumeTick();
            Assert.That(actionInput.DashPressed && actionInput.DefensePressed, Is.True);
        }
    }
}
