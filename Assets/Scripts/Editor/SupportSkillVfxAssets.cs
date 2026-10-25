#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SupportSkillVfxAssets
{
    private const string Destination = "Assets/Resources/VFX/SupportSkills/";
    private const string Drafts = "Assets/Art/Drafts/VFX/SupportSkills/";

    [Serializable] private sealed class Manifest { public Generated[] generated; }
    [Serializable] private sealed class Generated
    {
        public string key, sprite;
        public int logicalWidth, logicalHeight;
    }

    [MenuItem("KanjiBattle/Art/Promote Support Skill VFX")]
    public static void PromoteAndValidate()
    {
        string project = Path.GetDirectoryName(Application.dataPath);
        string layoutText = File.ReadAllText(Path.Combine(project, Drafts + "layout.json"));
        var layout = JsonUtility.FromJson<SupportVfxLayout>(layoutText);
        var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(
            Path.Combine(project, "Docs/Art/SupportSkillVfx.json")));
        if (layout?.entries == null || manifest?.generated == null)
            throw new InvalidDataException("Support VFX manifest/layout is empty.");
        var entries = new Dictionary<string, SupportVfxEntry>(StringComparer.Ordinal);
        foreach (var entry in layout.entries) entries.Add(entry.key, entry);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        // Validate the entire batch before writing anything. Never touch originals outside SupportSkills.
        foreach (var item in manifest.generated)
        {
            if (string.IsNullOrEmpty(item.key) || item.key.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || item.key.Contains("/") || item.key.Contains("\\") || item.key.Contains("..")
                || !seen.Add(item.key) || !entries.TryGetValue(item.key, out var entry))
                throw new InvalidDataException("Invalid or duplicate Support VFX key: " + item.key);
            string expected = Drafts + item.key + ".png";
            if (item.sprite != expected || entry.sprite != expected || !File.Exists(Path.Combine(project, expected)))
                throw new InvalidDataException("Missing or unsafe sprite: " + item.key);
            if (item.logicalWidth <= 0 || item.logicalHeight <= 0 || entry.width <= 0 || entry.height <= 0
                || entry.width % item.logicalWidth != 0 || entry.height % item.logicalHeight != 0
                || entry.bounds == null || entry.bounds.w <= 0 || entry.bounds.h <= 0
                || entry.bounds.x < 0 || entry.bounds.y < 0
                || entry.bounds.x + entry.bounds.w > entry.width || entry.bounds.y + entry.bounds.h > entry.height)
                throw new InvalidDataException("Invalid pixel grid or bounds: " + item.key);
            var probe = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!ImageConversion.LoadImage(probe, File.ReadAllBytes(Path.Combine(project, expected)))
                    || probe.width != entry.width || probe.height != entry.height)
                    throw new InvalidDataException("PNG dimensions do not match layout: " + item.key);
            }
            finally { UnityEngine.Object.DestroyImmediate(probe); }
        }
        if (seen.Count != entries.Count) throw new InvalidDataException("Manifest/layout keys differ.");

        Directory.CreateDirectory(Path.Combine(project, Destination));
        foreach (var item in manifest.generated)
        {
            string asset = Destination + item.key + ".png";
            File.Copy(Path.Combine(project, item.sprite), Path.Combine(project, asset), true);
            AssetDatabase.ImportAsset(asset, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            var importer = AssetImporter.GetAtPath(asset) as TextureImporter;
            if (importer == null) throw new InvalidDataException("Not a texture: " + asset);
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            importer.maxTextureSize = 8192;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            var defaults = importer.GetDefaultPlatformTextureSettings();
            defaults.format = TextureImporterFormat.RGBA32;
            defaults.maxTextureSize = 8192;
            defaults.textureCompression = TextureImporterCompression.Uncompressed;
            defaults.crunchedCompression = false;
            importer.SetPlatformTextureSettings(defaults);
            foreach (string platform in new[] { "Standalone", "WebGL", "Android", "iPhone" })
                importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
                {
                    name = platform, overridden = true, maxTextureSize = 8192,
                    format = TextureImporterFormat.RGBA32,
                    textureCompression = TextureImporterCompression.Uncompressed,
                    crunchedCompression = false
                });
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(asset);
            if (texture == null || texture.filterMode != FilterMode.Point || texture.wrapMode != TextureWrapMode.Clamp
                || texture.format != TextureFormat.RGBA32 || texture.width != entries[item.key].width
                || texture.height != entries[item.key].height)
                throw new InvalidDataException("Imported texture validation failed: " + asset);
        }
        // Keep the same bounds/key schema; runtime addresses textures by key, not draft paths.
        foreach (var entry in layout.entries) entry.sprite = Destination + entry.key + ".png";
        File.WriteAllText(Path.Combine(project, Destination + "layout.json"), JsonUtility.ToJson(layout, true));
        AssetDatabase.ImportAsset(Destination + "layout.json", ImportAssetOptions.ForceSynchronousImport);
        Debug.Log($"[SupportSkillVfxAssets] Passed. {seen.Count} textures promoted to {Destination}");
    }
}
#endif
