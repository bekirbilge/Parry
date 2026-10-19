using UnityEngine;

namespace Game.Unity
{
    /// <summary>Fits the fixed 16:9 playfield into the device safe area. Rect uses GUI top-left coordinates.</summary>
    public static class ArenaLayout
    {
        public static Rect Compute(int width, int height, Rect safeArea)
        {
            float left = Mathf.Clamp(safeArea.xMin, 0, width);
            float right = Mathf.Clamp(safeArea.xMax, left, width);
            float bottom = Mathf.Clamp(safeArea.yMin, 0, height);
            float top = Mathf.Clamp(safeArea.yMax, bottom, height);
            float availableWidth = right - left;
            float availableHeight = top - bottom;
            if (availableWidth <= 0 || availableHeight <= 0) return new Rect(0, 0, 0, 0);
            float footerHeight = Mathf.Min(Mathf.Clamp(height * .078f, 44, 72), availableHeight * .25f);
            availableHeight -= footerHeight;
            float fittedWidth = Mathf.Min(availableWidth, availableHeight * 16f / 9f);
            float fittedHeight = fittedWidth * 9f / 16f;
            return new Rect(left + (availableWidth - fittedWidth) * 0.5f,
                height - top + (availableHeight - fittedHeight) * 0.5f,
                fittedWidth, fittedHeight);
        }

        public static Rect Footer(int width, int height, Rect safeArea)
        {
            Rect arena = Compute(width, height, safeArea);
            float bottom = Mathf.Clamp(safeArea.yMin, 0, height);
            float y = arena.yMax + 2;
            return new Rect(arena.xMin, y, arena.width, Mathf.Max(0, height - bottom - y));
        }

        public static Rect TouchControl(Rect arena, int action)
        {
            float size = Mathf.Min(arena.height * .17f, arena.width * .12f);
            float x = arena.xMin + arena.width * (action == 0 ? .14f : action == 1 ? .65f : action == 2 ? .78f : .91f);
            float y = arena.yMax - arena.height * .21f;
            return new Rect(x - size * .5f, y - size * .5f, size, size);
        }

        public static Rect ToolbarButton(Rect arena, int index)
        {
            float width = arena.width * .12f;
            float gap = arena.width * .01f;
            return new Rect(arena.center.x - (4 * width + 3 * gap) * .5f + index * (width + gap),
                arena.yMin + Mathf.Max(62, arena.height * .10f), width, Mathf.Clamp(arena.height * .065f, 30, 46));
        }

        public static Rect TuningPanel(Rect arena)
        {
            float top = Mathf.Max(arena.height * .19f, ToolbarButton(arena, 0).yMax - arena.yMin + 8);
            return new Rect(arena.xMin + arena.width * .22f, arena.yMin + top,
                arena.width * .56f, Mathf.Max(0, arena.height - top - arena.height * .05f));
        }

        public static int TouchAction(Rect arena, Vector2 bottomLeftPosition, int screenHeight, bool tuningOpen)
        {
            Vector2 p = new Vector2(bottomLeftPosition.x, screenHeight - bottomLeftPosition.y);
            if (!arena.Contains(p)) return -1;
            for (int i = 0; i < 4; i++) if (ToolbarButton(arena, i).Contains(p)) return -1;
            if (tuningOpen) return -1;
            for (int i = 1; i < 4; i++) if (TouchControl(arena, i).Contains(p)) return i;
            Rect stick = TouchControl(arena, 0);
            Rect hit = new Rect(stick.center.x - stick.width * .8f, stick.center.y - stick.height * .8f,
                stick.width * 1.6f, stick.height * 1.6f);
            return hit.Contains(p) ? 0 : -1;
        }
    }
}
