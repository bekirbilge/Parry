using System;
using Game.Core;
using UnityEngine;

namespace Game.Unity
{
    /// <summary>Measured sprite layout; animation never defines authoritative collision.</summary>
    public sealed class NinjaAnimationAtlas
    {
        public const string TextureResource = "Art/char_ninja_animation_sample_v01";
        public const string LayoutResource = "Art/ninja_animation_sample_v01";

        [Serializable] public sealed class FrameBounds
        {
            public int left, top, right, bottom;
        }

        [Serializable] public sealed class Metadata
        {
            public int columns, rows, cellWidth, cellHeight;
            public float referenceHeight;
            public FrameBounds[] frames;
        }

        public Texture2D Texture { get; }
        private readonly Metadata _layout;
        public Metadata Layout => Copy(_layout);

        public NinjaAnimationAtlas(Texture2D texture, Metadata layout)
        {
            if (texture == null || !Valid(layout, texture.width, texture.height))
                throw new ArgumentException("Animation atlas requires valid measured frame bounds.");
            Texture = texture;
            _layout = Copy(layout);
        }

        public static NinjaAnimationAtlas Load()
        {
            var texture = Resources.Load<Texture2D>(TextureResource);
            var metadata = Resources.Load<TextAsset>(LayoutResource);
            if (texture == null || metadata == null) return null;
            try { return new NinjaAnimationAtlas(texture, JsonUtility.FromJson<Metadata>(metadata.text)); }
            catch (ArgumentException) { Debug.LogWarning("Animation sample layout invalid; using static reference."); }
            return null;
        }

        public static bool Valid(Metadata layout, int width, int height)
        {
            if (layout == null || layout.columns != 4 || layout.rows != 6 ||
                layout.cellWidth <= 0 || layout.cellHeight <= 0 ||
                width != layout.cellWidth * layout.columns || height != layout.cellHeight * layout.rows ||
                float.IsNaN(layout.referenceHeight) || float.IsInfinity(layout.referenceHeight) ||
                layout.referenceHeight <= 0 || layout.referenceHeight > layout.cellHeight ||
                layout.frames == null || layout.frames.Length != 24) return false;
            foreach (var frame in layout.frames)
                if (frame == null || frame.left < 0 || frame.top < 0 ||
                    frame.right <= frame.left || frame.bottom <= frame.top ||
                    frame.right > width || frame.bottom > height) return false;
            return true;
        }

        public Rect Uv(int row, int column, bool facingRight)
        {
            var b = Bounds(row, column);
            // Half-texel inset keeps transparent adjacent cells out of point-sampled frames.
            float x = (b.left + .5f) / Texture.width;
            float y = 1f - (b.bottom - .5f) / Texture.height;
            float width = (b.right - b.left - 1f) / Texture.width;
            float height = (b.bottom - b.top - 1f) / Texture.height;
            return facingRight ? new Rect(x, y, width, height) : new Rect(x + width, y, -width, height);
        }

        public Rect FighterRect(Rect arena, Vec2 center, float arenaWidth, float arenaHeight,
            float radius, int row, int column, bool facingRight)
        {
            var b = Bounds(row, column);
            float unitsPerPixel = 2f * radius / _layout.referenceHeight;
            float width = (b.right - b.left) * unitsPerPixel * arena.width / arenaWidth;
            float height = (b.bottom - b.top) * unitsPerPixel * arena.height / arenaHeight;
            float offset = (b.left - (column + .5f) * _layout.cellWidth) * unitsPerPixel * arena.width / arenaWidth;
            float centerX = arena.xMin + (float)center.X * arena.width / arenaWidth;
            float soleY = arena.yMax - ((float)center.Y - radius) * arena.height / arenaHeight;
            float x = facingRight ? centerX + offset : centerX - offset - width;
            return new Rect(x, soleY - height, width, height);
        }

        private static Metadata Copy(Metadata source)
        {
            var frames = new FrameBounds[source.frames.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                var b = source.frames[i];
                frames[i] = new FrameBounds { left = b.left, top = b.top, right = b.right, bottom = b.bottom };
            }
            return new Metadata { columns = source.columns, rows = source.rows,
                cellWidth = source.cellWidth, cellHeight = source.cellHeight,
                referenceHeight = source.referenceHeight, frames = frames };
        }

        private FrameBounds Bounds(int row, int column)
        {
            if (row < 0 || row >= _layout.rows || column < 0 || column >= _layout.columns)
                throw new ArgumentOutOfRangeException();
            return _layout.frames[row * _layout.columns + column];
        }
    }
}
