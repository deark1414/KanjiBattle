using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[ExecuteAlways]
[DefaultExecutionOrder(-500)]
public sealed class UnityUIRuntimeTheme : MonoBehaviour
{
    // Descriptive text must remain comfortable to read over illustrated backgrounds.
    public const float MinimumTextSize = 21f;
    private static UnityUIRuntimeTheme instance;

    private readonly Dictionary<string, Sprite> spriteCache = new();
    private int lastScreenWidth = -1;
    private int lastScreenHeight = -1;
    private int lastScreenOrientation = -1;

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
            // Dynamic entries apply their own styles; the periodic pass is only a resize fallback.
            if (Screen.width != lastScreenWidth ||
                Screen.height != lastScreenHeight ||
                (int)Screen.orientation != lastScreenOrientation)
            {
                ApplyTheme();
            }

            // Dynamic list entries and modal text are often created after the first theme pass.
            // Keep their accessibility floor consistent without waiting for a screen resize.
            EnforceTextSizeFloor();
        }
    }

    public void ApplyTheme()
    {
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            ConfigureCanvas(canvas);
            EnsureBackdrop(canvas);
            ModernWafuuPresentation.ApplyCanvasBackdrop(canvas);
        }

        foreach (var image in FindObjectsByType<Image>(FindObjectsInactive.Include))
        {
            StyleImage(image);
        }

        foreach (var scrollRect in FindObjectsByType<ScrollRect>(FindObjectsInactive.Include))
        {
            StyleScrollRect(scrollRect);
        }

        foreach (var rect in FindObjectsByType<RectTransform>(FindObjectsInactive.Include))
        {
            ApplyResponsiveContainerPlacement(rect);
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
            if (path.Contains("battlefield") || path.Contains("facility") || IsFacilityOwnedGrid(grid) || IsRosterOwnedGrid(grid)) continue;

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

        ModernWafuuPresentation.ApplyScreenChrome();

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        lastScreenOrientation = (int)Screen.orientation;
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

    private static bool IsRosterOwnedGrid(GridLayoutGroup grid)
    {
        if (grid == null) return false;

        foreach (var list in FindObjectsByType<CharacterListUI>(FindObjectsInactive.Include))
        {
            if (list != null && list.OwnsContent(grid.transform))
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
        if (image == null || image.name == "AutoTheme_Backdrop" || image.name == "WafuuBattleEnvironment") return;

        string objectName = image.gameObject.name.ToLowerInvariant();
        bool isButton = image.GetComponent<Button>() != null;
        bool isPanel = objectName.Contains("panel") || objectName.Contains("viewport") ||
                       image.GetComponent<ScrollRect>() != null || image.GetComponent<Mask>() != null;

        if (isButton) return;

        if (isPanel)
        {
            image.sprite = CreateSolidSprite("WashiPanel", Color.white);
            image.type = Image.Type.Simple;
            image.color = objectName.Contains("inset") || objectName.Contains("viewport")
                ? new Color(1f, 0.97f, 0.88f, 0.025f)
                : new Color(1f, 0.97f, 0.88f, 0.04f);
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
                new Vector2(0.04f, 0.10f),
                new Vector2(0.96f, 0.68f));
        }
        else if (path.Contains("stage"))
        {
            Stretch(rect,
                new Vector2(0.04f, 0.10f),
                new Vector2(0.96f, 0.68f));
        }
        else if (path.Contains("formation") || path.Contains("character"))
        {
            if (path.Contains("toppanel") && path.Contains("character"))
            {
                Stretch(rect,
                    IsPortraitNarrowScreen() ? new Vector2(0.04f, 0.25f) : new Vector2(0.04f, 0.16f),
                    IsPortraitNarrowScreen() ? new Vector2(0.96f, 0.59f) : new Vector2(0.60f, 0.80f));
            }
            else if (path.Contains("formation"))
            {
                Stretch(rect,
                    new Vector2(0.04f, 0.10f),
                    new Vector2(0.96f, 0.38f));
            }
            else
            {
                Stretch(rect,
                    new Vector2(0.04f, 0.10f),
                    new Vector2(0.96f, 0.68f));
            }
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

    private static void ApplyResponsiveContainerPlacement(RectTransform rect)
    {
        if (rect == null) return;

        string path = GetPath(rect.transform).ToLowerInvariant();
        if (!path.EndsWith("tabbuttons")) return;

        if (IsPortraitNarrowScreen())
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 56f);
        }
        else
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 60f);
        }
    }

    private void StyleButton(Button button)
    {
        if (button == null) return;
        ApplyResponsiveButtonPlacement(button);

        ModernWafuuPresentation.ApplyButtonTreatment(button);

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

    private static void ApplyResponsiveButtonPlacement(Button button)
    {
        if (button == null) return;

        string path = GetPath(button.transform).ToLowerInvariant();
        var rect = button.transform as RectTransform;
        if (rect == null) return;

        if (path.Contains("formation") && path.Contains("startbattlebutton"))
        {
            Stretch(rect,
                IsPortraitNarrowScreen() ? new Vector2(0.38f, 0.115f) : new Vector2(0.40f, 0.03f),
                IsPortraitNarrowScreen() ? new Vector2(0.62f, 0.175f) : new Vector2(0.60f, 0.10f));
        }
        else if (path.Contains("formation") && path.Contains("backbutton"))
        {
            Stretch(rect,
                IsPortraitNarrowScreen() ? new Vector2(0.04f, 0.115f) : new Vector2(0.04f, 0.03f),
                IsPortraitNarrowScreen() ? new Vector2(0.22f, 0.175f) : new Vector2(0.18f, 0.10f));
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
            text.fontSizeMin = MinimumTextSize;
            text.fontSizeMax = compact ? 21f : 24f;
            text.margin = compact ? new Vector4(4f, 1f, 4f, 1f) : new Vector4(8f, 2f, 8f, 2f);
        }
        else
        {
            text.color = new Color(0.98f, 0.93f, 0.82f);
        }

        if (path.Contains("entry") || path.Contains("facility") || path.Contains("stagebutton"))
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = MinimumTextSize;
            text.fontSizeMax = Mathf.Max(MinimumTextSize, text.fontSizeMax <= 0f ? 24f : text.fontSizeMax);
            text.margin = new Vector4(6f, 2f, 6f, 2f);
        }

        if (path.Contains("slot"))
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = MinimumTextSize;
            text.fontSizeMax = 22f;
            text.margin = new Vector4(4f, 2f, 4f, 2f);
            text.alignment = TextAlignmentOptions.Center;
            text.transform.SetAsLastSibling();
        }
        else if (!path.Contains("entry") && !path.Contains("facility") && !path.Contains("stagebutton") && text.fontSize < 18f)
        {
            text.fontSize = 18f;
        }

        EnforceTextSizeFloor(text);
    }

    public static void EnsureJapaneseCapableFont(TextMeshProUGUI text)
    {
        JapaneseFontProvider.EnsureJapaneseCapableFont(text);
        EnforceTextSizeFloor(text);
    }

    public static void EnforceTextSizeFloor(TextMeshProUGUI text)
    {
        if (text == null)
        {
            return;
        }

        text.fontSizeMin = Mathf.Max(MinimumTextSize, text.fontSizeMin);
        text.fontSizeMax = Mathf.Max(MinimumTextSize, text.fontSizeMax);
        if (!text.enableAutoSizing)
        {
            text.fontSize = Mathf.Max(MinimumTextSize, text.fontSize);
        }
    }

    private static void EnforceTextSizeFloor()
    {
        foreach (var text in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include))
        {
            EnforceTextSizeFloor(text);
        }
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
