using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace KanjiBattle.Editor
{
    public static class FontAssetMaintenance
    {
        private const string SourceFontPath = "Assets/Fonts/NotoSansJP-Medium.ttf";
        private const string TargetFontAssetPath = "Assets/Fonts/NotoSansJP-Medium SDF.asset";

        public static void RebuildJapaneseTmpFontAsset()
        {
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (sourceFont == null)
            {
                throw new FileNotFoundException($"Source font not found: {SourceFontPath}");
            }

            if (File.Exists(TargetFontAssetPath))
            {
                File.Delete(TargetFontAssetPath);
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic);

            fontAsset.name = "NotoSansJP-Medium SDF";
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            AssetDatabase.CreateAsset(fontAsset, TargetFontAssetPath);

            if (fontAsset.material != null)
            {
                fontAsset.material.name = "NotoSansJP-Medium SDF Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            Texture2D[] atlasTextures = fontAsset.atlasTextures;
            if (atlasTextures != null)
            {
                foreach (var atlasTexture in atlasTextures)
                {
                    if (atlasTexture == null) continue;
                    atlasTexture.name = "NotoSansJP-Medium SDF Atlas";
                    AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);
                }
            }

            TMP_Settings.defaultFontAsset = fontAsset;
            TMP_Settings.fallbackFontAssets.Clear();
            TMP_Settings.fallbackFontAssets.Add(fontAsset);
            EditorUtility.SetDirty(TMP_Settings.instance);
            EditorUtility.SetDirty(fontAsset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
