using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KanjiBattle.Editor
{
    public static class CharacterArtImporter
    {
        [MenuItem("KanjiBattle/Art/Import Complete Character Art")]
        public static void ImportProduction()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var entry in CharacterArtCatalog.Entries)
            {
                Configure(entry.resource, false);
                Configure(entry.piece, true);
                Configure(entry.enemyPiece, true);
            }
            var database = Resources.Load<CharacterDatabase>("CharacterDatabase");
            foreach (var character in database.characters)
            {
                var entry = CharacterArtCatalog.Find(character);
                if (entry == null) throw new Exception("Missing character art: " + character.characterName);
                character.icon = AssetDatabase.LoadAssetAtPath<Sprite>(AssetPath(entry.piece));
                character.enemyIcon = AssetDatabase.LoadAssetAtPath<Sprite>(AssetPath(entry.enemyPiece));
                EditorUtility.SetDirty(character);
            }
            AssetDatabase.SaveAssets();
            ValidateProduction();
        }

        private static string AssetPath(string resource) => "Assets/Resources/" + resource + ".png";

        private static void Configure(string resource, bool sprite)
        {
            string path = AssetPath(resource);
            if (!File.Exists(path)) throw new Exception("Missing art: " + path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            if (sprite)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
            }
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.filterMode = sprite ? FilterMode.Bilinear : FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            var web = importer.GetPlatformTextureSettings("WebGL");
            web.name = "WebGL"; web.overridden = true; web.maxTextureSize = 512;
            web.format = TextureImporterFormat.RGBA32;
            web.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(web);
            importer.SaveAndReimport();
        }

        public static void ValidateProduction()
        {
            var entries = CharacterArtCatalog.Entries;
            if (entries.Count != 45 || entries.Select(entry => entry.id).Distinct().Count() != 45)
                throw new Exception("Expected 45 unique character art sets.");
            foreach (var entry in entries)
            {
                if (Resources.Load<Texture2D>(entry.resource) == null
                    || Resources.Load<Sprite>(entry.piece) == null || Resources.Load<Sprite>(entry.enemyPiece) == null)
                    throw new Exception("Incomplete character art: " + entry.name);
            }
            foreach (var character in Resources.Load<CharacterDatabase>("CharacterDatabase").characters)
                if (character.icon == null || character.enemyIcon == null || CharacterArtCatalog.Find(character) == null)
                    throw new Exception("Unwired character art: " + character.characterName);
            foreach (var recipe in BattleFusionRules.Recipes)
                if (!entries.Any(entry => entry.id == recipe.Id && entry.name == recipe.Name))
                    throw new Exception("Missing fusion art: " + recipe.Name);
            Debug.Log("[CharacterArtImporter] 45 motifs, 90 pieces, all base and fusion characters wired. Passed.");
        }

        [Serializable]
        private sealed class ReferenceAudit
        {
            public ReferenceAuditEntry[] entries;
        }

        [Serializable]
        private sealed class ReferenceAuditEntry
        {
            public int id;
            public string name, dataAsset, allyPiece, enemyPiece, motif, source;
            public int motifWidth, motifHeight;
            public bool matchesCatalog;
        }

        [MenuItem("KanjiBattle/Art/Export Runtime Art References")]
        public static void ExportReferenceAudit()
        {
            var database = Resources.Load<CharacterDatabase>("CharacterDatabase");
            var rows = CharacterArtCatalog.Entries.Select(entry =>
            {
                var character = database.characters.FirstOrDefault(value => value.characterId == entry.id);
                bool temporary = character == null;
                if (temporary)
                {
                    var recipe = BattleFusionRules.GetRecipe(entry.id);
                    character = recipe == null
                        ? CharacterData.CreateBattleCopy(entry.name, SkillType.None, 1, 1, 0)
                        : CharacterData.CreateBattleFusion(recipe, recipe.Materials.Select(name =>
                            database.characters.First(value => value.characterName == name)).ToArray(), 1);
                }
                var texture = Resources.Load<Texture2D>(entry.resource);
                var row = new ReferenceAuditEntry
                {
                    id = entry.id, name = entry.name,
                    dataAsset = temporary ? "runtime: CharacterData factory" : AssetDatabase.GetAssetPath(character),
                    allyPiece = AssetDatabase.GetAssetPath(character.icon),
                    enemyPiece = AssetDatabase.GetAssetPath(character.enemyIcon),
                    motif = AssetDatabase.GetAssetPath(texture), source = entry.source,
                    motifWidth = texture == null ? 0 : texture.width,
                    motifHeight = texture == null ? 0 : texture.height,
                    matchesCatalog = character.icon != null && character.enemyIcon != null && texture != null
                        && AssetDatabase.GetAssetPath(character.icon) == AssetPath(entry.piece)
                        && AssetDatabase.GetAssetPath(character.enemyIcon) == AssetPath(entry.enemyPiece)
                        && AssetDatabase.GetAssetPath(texture) == AssetPath(entry.resource)
                };
                if (temporary) UnityEngine.Object.DestroyImmediate(character);
                return row;
            }).ToArray();
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../tmp/character-art/reference-audit.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(new ReferenceAudit { entries = rows }, true));
            if (rows.Any(row => !row.matchesCatalog)) throw new Exception("Art reference mismatch. See " + output);
            Debug.Log($"[CharacterArtAudit] {rows.Length} loaded art references match the catalog. Passed. {output}");
        }
    }
}
