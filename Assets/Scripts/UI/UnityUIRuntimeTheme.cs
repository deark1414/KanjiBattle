using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DefaultExecutionOrder(-500)]
public sealed class UnityUIRuntimeTheme : MonoBehaviour
{
    private static UnityUIRuntimeTheme instance;
    private static TMP_FontAsset preferredJapaneseFont;

    private readonly Dictionary<string, Sprite> spriteCache = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;

        var host = new GameObject(nameof(UnityUIRuntimeTheme));
        DontDestroyOnLoad(host);
        instance = host.AddComponent<UnityUIRuntimeTheme>();
        SceneManager.sceneLoaded += (_, __) => instance.StartCoroutine(instance.ApplyNextFrame());
        instance.ApplyTheme();
        instance.StartCoroutine(instance.ApplyNextFrame());
        instance.StartCoroutine(instance.ApplyPeriodically());
    }

    private void Awake()
    {
        if (Application.isPlaying)
        {
            instance = this;
        }

        ApplyTheme();
    }

    private void OnEnable()
    {
        ApplyTheme();
    }

    private IEnumerator ApplyNextFrame()
    {
        yield return null;
        ApplyTheme();
    }

    private IEnumerator ApplyPeriodically()
    {
        var wait = new WaitForSeconds(0.5f);
        while (true)
        {
            yield return wait;
            ApplyTheme();
        }
    }

    public void ApplyTheme()
    {
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            ConfigureCanvas(canvas);
            EnsureBackdrop(canvas);
        }

        foreach (var image in FindObjectsByType<Image>(FindObjectsInactive.Include))
        {
            StyleImage(image);
        }

        foreach (var scrollRect in FindObjectsByType<ScrollRect>(FindObjectsInactive.Include))
        {
            StyleScrollRect(scrollRect);
        }

        foreach (var button in FindObjectsByType<Button>(FindObjectsInactive.Include))
        {
            StyleButton(button);
        }

        foreach (var text in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include))
        {
            StyleText(text);
        }

        foreach (var grid in FindObjectsByType<GridLayoutGroup>(FindObjectsInactive.Include))
        {
            string path = GetPath(grid.transform).ToLowerInvariant();
            if (path.Contains("battlefield") || path.Contains("facility") || IsFacilityOwnedGrid(grid)) continue;

            var fitter = grid.GetComponent<ResponsiveGridFitter>();
            if (fitter == null) fitter = grid.gameObject.AddComponent<ResponsiveGridFitter>();
            fitter.ConfigureFromName();
            fitter.Refresh();
        }

        foreach (var layout in FindObjectsByType<VerticalLayoutGroup>(FindObjectsInactive.Include))
        {
            layout.spacing = Mathf.Max(layout.spacing, 8f);
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
        }
    }

    private static bool IsFacilityOwnedGrid(GridLayoutGroup grid)
    {
        if (grid == null) return false;

        foreach (var list in FindObjectsByType<FacilityListUI>(FindObjectsInactive.Include))
        {
            if (list != null && list.OwnsContent(grid.transform))
            {
                return true;
            }
        }

        foreach (Transform child in grid.transform)
        {
            if (child.GetComponent<FacilityUI>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private static void ConfigureCanvas(Canvas canvas)
    {
        if (!canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace) return;

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null) return;

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = IsPortraitNarrowScreen() ? 0f : 0.5f;
    }

    private void EnsureBackdrop(Canvas canvas)
    {
        if (!canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace) return;
        if (canvas.transform.Find("AutoTheme_Backdrop") != null) return;

        var backdrop = new GameObject("AutoTheme_Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backdrop.transform.SetParent(canvas.transform, false);
        backdrop.transform.SetAsFirstSibling();

        var rect = backdrop.GetComponent<RectTransform>();
        Stretch(rect, Vector2.zero, Vector2.one);

        var image = backdrop.GetComponent<Image>();
        image.sprite = CreateSolidSprite("Backdrop", new Color(0.11f, 0.13f, 0.18f));
        image.color = Color.white;
        image.raycastTarget = false;
    }

    private void StyleImage(Image image)
    {
        if (image == null || image.name == "AutoTheme_Backdrop") return;

        string objectName = image.gameObject.name.ToLowerInvariant();
        bool isButton = image.GetComponent<Button>() != null;
        bool isPanel = objectName.Contains("panel") || image.GetComponent<ScrollRect>() != null || image.GetComponent<Mask>() != null;

        if (isButton) return;

        if (isPanel)
        {
            image.sprite = GetSprite(objectName.Contains("inset") || objectName.Contains("viewport") ? "panelInset_blue" : "panel_blue", new Vector4(12f, 12f, 12f, 12f));
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }
    }

    private void StyleScrollRect(ScrollRect scrollRect)
    {
        if (scrollRect == null) return;

        string path = GetPath(scrollRect.transform).ToLowerInvariant();
        var rect = scrollRect.GetComponent<RectTransform>();
        if (rect == null) return;

        if (path.Contains("facility"))
        {
            Stretch(rect,
                IsPortraitNarrowScreen() ? new Vector2(0.04f, 0.12f) : new Vector2(0.54f, 0.14f),
                IsPortraitNarrowScreen() ? new Vector2(0.96f, 0.88f) : new Vector2(0.98f, 0.94f));
        }
        else if (path.Contains("stage"))
        {
            Stretch(rect,
                IsPortraitNarrowScreen() ? new Vector2(0.04f, 0.13f) : new Vector2(0.06f, 0.16f),
                IsPortraitNarrowScreen() ? new Vector2(0.96f, 0.88f) : new Vector2(0.94f, 0.90f));
        }
        else if (path.Contains("formation") || path.Contains("character"))
        {
            Stretch(rect,
                IsPortraitNarrowScreen() ? new Vector2(0.04f, 0.08f) : new Vector2(0.06f, 0.12f),
                IsPortraitNarrowScreen() ? new Vector2(0.96f, 0.60f) : new Vector2(0.94f, 0.62f));
        }

        if (scrollRect.viewport != null && !path.Contains("battle"))
        {
            Stretch(scrollRect.viewport, Vector2.zero, Vector2.one);
        }

        if (scrollRect.content != null && !path.Contains("battle"))
        {
            scrollRect.content.anchorMin = new Vector2(0f, 1f);
            scrollRect.content.anchorMax = new Vector2(1f, 1f);
            scrollRect.content.pivot = new Vector2(0.5f, 1f);
            scrollRect.content.offsetMin = new Vector2(10f, scrollRect.content.offsetMin.y);
            scrollRect.content.offsetMax = new Vector2(-10f, scrollRect.content.offsetMax.y);
        }
    }

    private void StyleButton(Button button)
    {
        if (button == null) return;
        var image = button.targetGraphic as Image ?? button.GetComponent<Image>();
        if (image != null)
        {
            bool compact = ((RectTransform)button.transform).rect.width < 90f || button.name.ToLowerInvariant().Contains("tab");
            image.sprite = GetSprite(compact ? "buttonSquare_brown" : "buttonLong_brown", new Vector4(10f, 10f, 10f, 10f));
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.94f, 0.78f);
        colors.pressedColor = new Color(0.82f, 0.74f, 0.62f);
        colors.selectedColor = new Color(1f, 0.91f, 0.62f);
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        var rectTransform = button.transform as RectTransform;
        if (rectTransform != null && rectTransform.rect.height > 0f && rectTransform.rect.height < 42f)
        {
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 42f);
        }

        if (!GetPath(button.transform).ToLowerInvariant().Contains("battle"))
        {
            var layout = button.GetComponent<LayoutElement>();
            if (layout == null) layout = button.gameObject.AddComponent<LayoutElement>();
            layout.minHeight = Mathf.Max(layout.minHeight, button.name.ToLowerInvariant().Contains("tab") ? 46f : 54f);
            layout.flexibleWidth = 1f;
        }
    }

    private static void StyleText(TextMeshProUGUI text)
    {
        if (text == null) return;

        EnsureJapaneseCapableFont(text);

        if (text.GetComponentInParent<FacilityUI>() != null) return;

        string path = GetPath(text.transform).ToLowerInvariant();
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;

        if (text.transform.parent != null && text.transform.parent.GetComponent<Button>() != null)
        {
            var parentRect = text.transform.parent as RectTransform;
            bool compact = parentRect != null && (parentRect.rect.height < 45f || parentRect.rect.width < 140f);
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle |= FontStyles.Bold;
            text.color = new Color(1f, 0.95f, 0.82f);
            text.raycastTarget = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = compact ? 8f : 10f;
            text.fontSizeMax = compact ? 16f : 21f;
            text.margin = compact ? new Vector4(4f, 1f, 4f, 1f) : new Vector4(8f, 2f, 8f, 2f);
        }
        else
        {
            text.color = new Color(0.98f, 0.93f, 0.82f);
        }

        if (path.Contains("entry") || path.Contains("facility") || path.Contains("stagebutton"))
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = 9f;
            text.fontSizeMax = Mathf.Min(text.fontSizeMax <= 0f ? 20f : text.fontSizeMax, 20f);
            text.margin = new Vector4(6f, 2f, 6f, 2f);
        }

        if (path.Contains("slot"))
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = 22f;
            text.margin = new Vector4(4f, 2f, 4f, 2f);
            text.alignment = TextAlignmentOptions.Center;
            text.transform.SetAsLastSibling();
        }
        else if (!path.Contains("entry") && !path.Contains("facility") && !path.Contains("stagebutton") && text.fontSize < 18f)
        {
            text.fontSize = 18f;
        }
    }

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

        foreach (var text in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include))
        {
            TMP_FontAsset font = text.font;
            if (font == null) continue;
            string fontName = font.name;
            if (fontName.Contains("NotoSansJP") && fontName.Contains("Runtime SDF"))
            {
                preferredJapaneseFont = font;
                return preferredJapaneseFont;
            }
        }

        preferredJapaneseFont = CreateRuntimeJapaneseFont();
        if (preferredJapaneseFont != null) return preferredJapaneseFont;

        preferredJapaneseFont = TMP_Settings.defaultFontAsset;
        return preferredJapaneseFont;
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

    private Sprite GetSprite(string name, Vector4 border)
    {
        string key = name + border;
        if (spriteCache.TryGetValue(key, out var cached)) return cached;

        var sprite = Resources.Load<Sprite>($"Kenney/UIRPG/PNG/{name}");
        if (sprite != null)
        {
            spriteCache[key] = sprite;
            return sprite;
        }

        var texture = Resources.Load<Texture2D>($"Kenney/UIRPG/PNG/{name}");
        if (texture == null)
        {
            return spriteCache[key] = CreateSolidSprite(name, Color.white);
        }

        var rect = new Rect(0f, 0f, texture.width, texture.height);
        sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        spriteCache[key] = sprite;
        return sprite;
    }

    private Sprite CreateSolidSprite(string name, Color color)
    {
        var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        texture.name = name;
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static bool IsPortraitNarrowScreen()
    {
        return Screen.height > Screen.width * 1.15f;
    }

    private static string GetPath(Transform transform)
    {
        if (transform == null) return string.Empty;
        return transform.parent == null ? transform.name : GetPath(transform.parent) + "/" + transform.name;
    }
}
