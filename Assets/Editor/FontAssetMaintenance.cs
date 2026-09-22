using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace KanjiBattle.Editor
{
    public static class FontAssetMaintenance
    {
        private const string SourceFontPath = "Assets/Fonts/NotoSansJP-Medium.ttf";
        private const string TargetFontAssetPath = "Assets/Fonts/NotoSansJP-Medium SDF.asset";
        private const string GlyphListPath = "tmp/font/glyphs.txt";
        private const int AtlasSize = 4096;

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
                AtlasSize,
                AtlasSize,
                AtlasPopulationMode.Dynamic);

            fontAsset.name = "NotoSansJP-Medium SDF";
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            fontAsset.isMultiAtlasTexturesEnabled = false;

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Directory.GetCurrentDirectory();
            string glyphListFullPath = Path.Combine(projectRoot, GlyphListPath);
            if (File.Exists(glyphListFullPath))
            {
                string characters = File.ReadAllText(glyphListFullPath);
                if (!fontAsset.TryAddCharacters(characters, out string missingCharacters))
                {
                    Debug.LogWarning($"Missing glyphs while rebuilding TMP font asset: {missingCharacters}");
                }
            }
            else
            {
                Debug.LogWarning($"Glyph list not found: {glyphListFullPath}");
            }

            AssetDatabase.CreateAsset(fontAsset, TargetFontAssetPath);
            DisableClearDynamicDataOnBuild(fontAsset);

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

        public static void RepairSceneJapaneseFontReferences()
        {
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TargetFontAssetPath);
            if (fontAsset == null)
            {
                throw new FileNotFoundException($"Project TMP font not found: {TargetFontAssetPath}");
            }

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            TextMeshProUGUI[] textComponents = Object.FindObjectsByType<TextMeshProUGUI>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (TextMeshProUGUI text in textComponents)
            {
                text.font = fontAsset;
                text.fontSharedMaterial = fontAsset.material;
                EditorUtility.SetDirty(text);
            }

            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static void DisableClearDynamicDataOnBuild(TMP_FontAsset fontAsset)
        {
            var serializedObject = new SerializedObject(fontAsset);
            SerializedProperty property = serializedObject.FindProperty("m_ClearDynamicDataOnBuild");
            if (property == null) return;

            property.boolValue = false;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
