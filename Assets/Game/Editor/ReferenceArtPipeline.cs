using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>Pixel import settings for the static references and single-character animation proof.</summary>
    public sealed class ReferenceArtPipeline : AssetPostprocessor
    {
        public const string CharacterPath = "Assets/Game/Resources/Art/char_ninja_idle_v01.png";
        public const string ArenaPath = "Assets/Game/Resources/Art/arena_rookie_dojo_background_v01.png";
        public const string CharacterResource = "Art/char_ninja_idle_v01";
        public const string ArenaResource = "Art/arena_rookie_dojo_background_v01";

        private void OnPreprocessTexture()
        {
            if (assetPath != CharacterPath && assetPath != ArenaPath &&
                assetPath != "Assets/Game/Resources/Art/char_ninja_animation_sample_v01.png")
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
        }
    }
}
