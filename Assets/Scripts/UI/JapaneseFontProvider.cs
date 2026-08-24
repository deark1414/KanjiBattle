using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class JapaneseFontProvider
{
    private static TMP_FontAsset preferredJapaneseFont;

    public static void EnsureJapaneseCapableFont(TextMeshProUGUI text)
    {
        if (text == null) return;

        TMP_FontAsset font = GetPreferredJapaneseFont();
        if (font != null && text.font != font)
        {
            text.font = font;
        }
        if (text.font != null && text.font.material != null && text.fontSharedMaterial != text.font.material)
        {
            text.fontSharedMaterial = text.font.material;
        }
        text.SetAllDirty();
    }

    private static TMP_FontAsset GetPreferredJapaneseFont()
    {
        if (preferredJapaneseFont != null) return preferredJapaneseFont;

        var defaultFont = TMP_Settings.defaultFontAsset;
        if (IsJapaneseProjectFont(defaultFont))
        {
            preferredJapaneseFont = defaultFont;
            return preferredJapaneseFont;
        }

        preferredJapaneseFont = CreateRuntimeJapaneseFont();
        if (preferredJapaneseFont != null) return preferredJapaneseFont;

        preferredJapaneseFont = defaultFont;
        return preferredJapaneseFont;
    }

    private static bool IsJapaneseProjectFont(TMP_FontAsset font)
    {
        return font != null && font.name.Contains("NotoSansJP");
    }

    private static TMP_FontAsset CreateRuntimeJapaneseFont()
    {
#if UNITY_EDITOR
        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSansJP-Medium.ttf");
        if (sourceFont == null) return null;

        var fontAsset = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            90,
            9,
            GlyphRenderMode.SDFAA,
            1024,
            1024,
            AtlasPopulationMode.Dynamic);
        fontAsset.name = "NotoSansJP-Medium Runtime SDF";
        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        return fontAsset;
#else
        return null;
#endif
    }
}
