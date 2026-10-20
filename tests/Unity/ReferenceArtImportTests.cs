using System.IO;
using Game.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Unity.Tests
{
    public sealed class ReferenceArtImportTests
    {
        [TestCase(ReferenceArtPipeline.CharacterPath, ReferenceArtPipeline.CharacterResource)]
        [TestCase(ReferenceArtPipeline.ArenaPath, ReferenceArtPipeline.ArenaResource)]
        public void ReferencePngsLoadThroughResourcesWithPixelSafeImport(
            string assetPath, string resourcePath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            Assert.That(importer, Is.Not.Null, assetPath);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Default));
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
            Assert.That(importer.sRGBTexture, Is.True);
            Assert.That(importer.alphaSource, Is.EqualTo(TextureImporterAlphaSource.FromInput));
            Assert.That(importer.alphaIsTransparency, Is.True);
            Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None));
            Assert.That(importer.maxTextureSize, Is.EqualTo(2048));

            var texture = Resources.Load<Texture2D>(resourcePath);
            Assert.That(texture, Is.Not.Null, resourcePath);
            Assert.That(texture.width, Is.GreaterThan(0));
            Assert.That(texture.height, Is.GreaterThan(0));
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(ImageConversion.LoadImage(source, File.ReadAllBytes(assetPath), false), Is.True);
                Assert.That(texture.width, Is.EqualTo(source.width));
                Assert.That(texture.height, Is.EqualTo(source.height));
            }
            finally
            {
                Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void CharacterSourceContainsVisibleAndTransparentPixels()
        {
            byte[] png = File.ReadAllBytes(ReferenceArtPipeline.CharacterPath);
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(ImageConversion.LoadImage(source, png, false), Is.True);
                bool visible = false;
                bool transparent = false;
                foreach (var pixel in source.GetPixels32())
                {
                    visible |= pixel.a == 255;
                    transparent |= pixel.a == 0;
                }
                Assert.That(visible, Is.True);
                Assert.That(transparent, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(source);
            }
        }
    }
}
