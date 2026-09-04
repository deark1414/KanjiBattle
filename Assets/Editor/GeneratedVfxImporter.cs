using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace KanjiBattle.Editor
{
    public static class GeneratedVfxImporter
    {
        private const string CatalogPath = "Assets/Resources/VFX/kanjibattle_vfx_catalog_02.png";
        private const string AuraPath = "Assets/Resources/VFX/kanjibattle_vfx_number_auras_01.png";

        public static void Configure()
        {
            ConfigureGrid(CatalogPath, 7, 6, 192);
            ConfigureGrid(AuraPath, 3, 3, 192);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Configured generated VFX atlases as fixed grid sprites.");
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
    }
}
