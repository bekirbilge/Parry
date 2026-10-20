using System;
using Game.Core;
using UnityEngine;

namespace Game.Unity
{
    /// <summary>Cosmetic layout only; gameplay positions and radii come from Game.Core.</summary>
    public static class ArtPresentation
    {
        public const string CharacterResource = "Art/char_ninja_idle_v01";
        public const string ArenaResource = "Art/arena_rookie_dojo_background_v01";
        public const string CharacterLayoutResource = "Art/reference_art_layout_v01";

        [Serializable]
        private sealed class CharacterLayoutMetadata
        {
            public float visibleHeightFraction;
            public float soleInsetFraction;
            public float arenaBottomCropFraction;
        }

        public readonly struct CharacterLayout
        {
            public readonly float VisibleHeightFraction;
            public readonly float SoleInsetFraction;
            public readonly float ArenaBottomCropFraction;
            public Rect ArenaUvRect => new Rect(0f, ArenaBottomCropFraction,
                1f, 1f - ArenaBottomCropFraction);

            public CharacterLayout(float visibleHeightFraction, float soleInsetFraction,
                float arenaBottomCropFraction = 0f)
            {
                VisibleHeightFraction = visibleHeightFraction;
                SoleInsetFraction = soleInsetFraction;
                ArenaBottomCropFraction = arenaBottomCropFraction;
            }
        }

        // 4-unit fallback cell with a 3-unit visible body and a 0.5-unit sole inset.
        public static CharacterLayout FallbackCharacterLayout => new CharacterLayout(.75f, .125f);

        public static CharacterLayout LoadCharacterLayout(out bool metadataLoaded)
        {
            metadataLoaded = false;
            TextAsset asset = Resources.Load<TextAsset>(CharacterLayoutResource);
            if (asset == null) return FallbackCharacterLayout;
            try
            {
                CharacterLayoutMetadata data = JsonUtility.FromJson<CharacterLayoutMetadata>(asset.text);
                if (data != null && ValidFraction(data.visibleHeightFraction) &&
                    ValidFraction(data.soleInsetFraction) && data.visibleHeightFraction > 0 &&
                    data.visibleHeightFraction + data.soleInsetFraction <= 1f &&
                    ValidFraction(data.arenaBottomCropFraction) && data.arenaBottomCropFraction <= .25f)
                {
                    metadataLoaded = true;
                    return new CharacterLayout(data.visibleHeightFraction, data.soleInsetFraction,
                        data.arenaBottomCropFraction);
                }
            }
            catch (ArgumentException) { }
            Debug.LogWarning("Reference character layout is invalid; using fallback placement.");
            return FallbackCharacterLayout;
        }

        private static bool ValidFraction(float value) =>
            !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value <= 1f;

        public static Rect FighterRect(Rect arena, Vec2 center, float arenaWidthUnits,
            float arenaHeightUnits, float fighterRadiusUnits, CharacterLayout layout)
        {
            float unitX = arena.width / arenaWidthUnits;
            float unitY = arena.height / arenaHeightUnits;
            float x = arena.xMin + (float)center.X * unitX;
            float y = arena.yMax - (float)center.Y * unitY;
            float sizeUnits = 2f * fighterRadiusUnits / layout.VisibleHeightFraction;
            float bottom = y + (fighterRadiusUnits + layout.SoleInsetFraction * sizeUnits) * unitY;
            return new Rect(x - sizeUnits * unitX * .5f, bottom - sizeUnits * unitY,
                sizeUnits * unitX, sizeUnits * unitY);
        }
    }
}
