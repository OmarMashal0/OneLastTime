#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace OneLastTime.EditorTools
{
    public class PixelArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (assetPath.StartsWith("Assets/Level") || assetPath.StartsWith("Assets/_Core/Art"))
            {
                TextureImporter importer = (TextureImporter)assetImporter;
                ConfigurePixelArtImporter(importer);
            }
        }

        public static void ConfigurePixelArtImporter(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spritePixelsPerUnit = 16f;
            settings.filterMode = FilterMode.Point;
            settings.mipmapEnabled = false;
            settings.textureType = TextureImporterType.Sprite;
            importer.SetTextureSettings(settings);
        }

        [MenuItem("Tools/One Last Time/Configure Level 1 Pixel Art Sprites")]
        public static void ConfigureLevel1Sprites()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Level1_Museum/Art" });
            int count = 0;

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith(".png") || path.EndsWith(".jpg"))
                    {
                        if (AssetImporter.GetAtPath(path) is TextureImporter importer)
                        {
                            ConfigurePixelArtImporter(importer);
                            EditorUtility.SetDirty(importer);
                            importer.SaveAndReimport();
                            count++;
                        }
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            Debug.Log("[PixelArtImporter] Configured " + count + " textures in Level1_Museum/Art.");
        }
    }
}
#endif
