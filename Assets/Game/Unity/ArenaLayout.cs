using UnityEngine;

namespace Game.Unity
{
    /// <summary>Places the playfield in GUI top-left coordinates within the device safe area.</summary>
    public static class ArenaLayout
    {
        public static Rect Compute(int width, int height, Rect safeArea, bool mobileUi = false)
        {
            float left = Mathf.Clamp(safeArea.xMin, 0, width);
            float right = Mathf.Clamp(safeArea.xMax, left, width);
            float bottom = Mathf.Clamp(safeArea.yMin, 0, height);
            float top = Mathf.Clamp(safeArea.yMax, bottom, height);
            float availableWidth = right - left;
            float availableHeight = top - bottom;
            if (availableWidth <= 0 || availableHeight <= 0) return new Rect(0, 0, 0, 0);
            if (mobileUi) return new Rect(left, height - top, availableWidth, availableHeight);
            float footerHeight = Mathf.Min(Mathf.Clamp(height * .078f, 44, 72), availableHeight * .25f);
            availableHeight -= footerHeight;
            float fittedWidth = Mathf.Min(availableWidth, availableHeight * 16f / 9f);
            float fittedHeight = fittedWidth * 9f / 16f;
            return new Rect(left + (availableWidth - fittedWidth) * 0.5f,
                height - top + (availableHeight - fittedHeight) * 0.5f,
                fittedWidth, fittedHeight);
        }

        public static Rect Footer(int width, int height, Rect safeArea, bool mobileUi = false)
        {
            if (mobileUi) return Rect.zero;
            Rect arena = Compute(width, height, safeArea, mobileUi);
            float bottom = Mathf.Clamp(safeArea.yMin, 0, height);
            float y = arena.yMax + 2;
            return new Rect(arena.xMin, y, arena.width, Mathf.Max(0, height - bottom - y));
        }

        public static Rect TouchControl(Rect arena, int action)
        {
            if (action < 0 || action > 2) return Rect.zero;
            float size = Mathf.Min(arena.height * .125f, arena.width * .12f);
            float x = arena.xMin + arena.width * (action == 0 ? .14f : action == 1 ? .76f : .90f);
            float y = arena.yMax - arena.height * .21f;
            return new Rect(x - size * .5f, y - size * .5f, size, size);
        }

        public static Rect ToolbarButton(Rect arena, int index, bool mobileUi = false)
        {
            if (mobileUi) return PauseMenuButton(arena, index);
            float width = arena.width * .12f;
            float gap = arena.width * .01f;
            return new Rect(arena.center.x - (4 * width + 3 * gap) * .5f + index * (width + gap),
                arena.yMin + Mathf.Max(62, arena.height * .10f),
                width, Mathf.Clamp(arena.height * .065f, 30, 46));
        }

        public static Rect PauseButton(Rect arena)
        {
            float height = arena.height * .10f;
            float inset = arena.height * .025f;
            float width = arena.height * .18f;
            return new Rect(arena.xMax - inset - width, arena.yMin + inset, width, height);
        }

        public static Rect PauseMenuPanel(Rect arena)
        {
            float width = Mathf.Min(arena.width * .48f, arena.height * .9f);
            float height = arena.height * .55f;
            return new Rect(arena.center.x - width * .5f, arena.center.y - height * .5f, width, height);
        }

        public static Rect PauseMenuButton(Rect arena, int index)
        {
            if (index < 0 || index > 3) return Rect.zero;
            Rect panel = PauseMenuPanel(arena);
            float height = arena.height * .10f;
            float gap = arena.height * .025f;
            return new Rect(panel.xMin + panel.width * .10f,
                panel.center.y - (4 * height + 3 * gap) * .5f + index * (height + gap),
                panel.width * .80f, height);
        }

        public static Rect TuningPanel(Rect arena, bool mobileUi = false)
        {
            float top = Mathf.Max(arena.height * .19f, ToolbarButton(arena, 0, mobileUi).yMax - arena.yMin + 8);
            return new Rect(arena.xMin + arena.width * .22f, arena.yMin + top,
                arena.width * .56f, Mathf.Max(0, arena.height - top - arena.height * .05f));
        }

        public static int TouchAction(Rect arena, Vector2 bottomLeftPosition, int screenHeight, bool tuningOpen,
            bool mobileUi = false, bool menuOpen = false)
        {
            Vector2 p = new Vector2(bottomLeftPosition.x, screenHeight - bottomLeftPosition.y);
            if (!arena.Contains(p)) return -1;
            if (tuningOpen || menuOpen) return -1;
            if (mobileUi)
            {
                if (PauseButton(arena).Contains(p)) return -1;
            }
            else
            {
                for (int i = 0; i < 4; i++) if (ToolbarButton(arena, i).Contains(p)) return -1;
            }
            for (int i = 1; i <= 2; i++) if (TouchControl(arena, i).Contains(p)) return i;
            Rect stick = TouchControl(arena, 0);
            Rect hit = new Rect(stick.center.x - stick.width * .8f, stick.center.y - stick.height * .8f,
                stick.width * 1.6f, stick.height * 1.6f);
            return hit.Contains(p) ? 0 : -1;
        }
    }
}
