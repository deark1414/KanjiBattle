using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace KanjiBattle.Editor
{
    public static class GeneratedVfxImporter
    {
        private const string AuraPath = "Assets/Resources/VFX/kanjibattle_vfx_number_auras_01.png";
        private static readonly string[] WeaponPaths =
        {
            "Assets/Resources/VFX/Weapons/SwordMotion.png",
            "Assets/Resources/VFX/Weapons/HammerMotion.png"
        };

        public static void Configure()
        {
            ConfigureGrid(AuraPath, 3, 3, 192);
            ConfigureWeaponMotionTextures();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Configured generated VFX atlases as fixed grid sprites.");
        }

        /// <summary>
        /// 武器PNGはRawImageで直接描画する。WebGLで半透明の残像だけになるのを防ぐため、
        /// Spriteのタイトメッシュと自動圧縮を使わない設定をビルド前に毎回正規化する。
        /// </summary>
        public static void ConfigureWeaponMotionTextures()
        {
            foreach (string path in WeaponPaths)
            {
                ConfigureWeaponMotionTexture(path);
            }
        }

        private static void ConfigureGrid(string path, int columns, int rows, int cellSize)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.isReadable = false;

            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
            dataProvider.InitSpriteEditorDataProvider();
            var sprites = new SpriteRect[columns * rows];
            int index = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    sprites[index] = new SpriteRect
                    {
                        name = $"{System.IO.Path.GetFileNameWithoutExtension(path)}_{index:00}",
                        // Atlas labels are read top-to-bottom, while Unity rects use bottom-left origin.
                        rect = new Rect(column * cellSize, (rows - row - 1) * cellSize, cellSize, cellSize),
                        pivot = new Vector2(0.5f, 0.5f)
                    };
                    index++;
                }
            }

            dataProvider.SetSpriteRects(sprites);
            dataProvider.Apply();
            importer.SaveAndReimport();
        }

        private static void ConfigureWeaponMotionTexture(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Default;
            importer.spriteImportMode = SpriteImportMode.None;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;

            var webGl = importer.GetPlatformTextureSettings("WebGL");
            webGl.name = "WebGL";
            webGl.overridden = true;
            webGl.format = TextureImporterFormat.RGBA32;
            webGl.maxTextureSize = 2048;
            webGl.compressionQuality = 100;
            importer.SetPlatformTextureSettings(webGl);
            importer.SaveAndReimport();

            var verified = AssetImporter.GetAtPath(path) as TextureImporter;
            var verifiedWebGl = verified?.GetPlatformTextureSettings("WebGL");
            if (verified == null
                || verified.textureType != TextureImporterType.Default
                || verified.textureCompression != TextureImporterCompression.Uncompressed
                || !verifiedWebGl.overridden
                || verifiedWebGl.format != TextureImporterFormat.RGBA32)
            {
                throw new System.InvalidOperationException(
                    $"武器VFXのWebGL描画設定を検証できませんでした: {path}");
            }
        }
    }
}
