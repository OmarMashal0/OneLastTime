using System;
using UnityEditor;
using UnityEngine;

namespace NoticeboardRunes.Editor
{
    internal sealed class NoticeboardRuneImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (assetPath.IndexOf("/NoticeboardRunes/Sprites/", StringComparison.Ordinal) < 0)
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 256;
        }
    }
}
