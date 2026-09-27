using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class ContextualInfoTooltip
{
    private static GameObject tooltipObject;
    private static TextMeshProUGUI titleText;
    private static TextMeshProUGUI bodyText;
    private static RectTransform activeAnchor;

    public static void Show(RectTransform anchor, string title, string body)
    {
        if (anchor == null)
        {
            return;
        }

        SkillTooltipPresenter.HideAll();
        EnsureTooltip(anchor);
        if (activeAnchor == anchor && tooltipObject.activeSelf)
        {
            Hide();
            return;
        }

        activeAnchor = anchor;
        titleText.text = title;
        bodyText.text = body;
        PositionBelow(anchor);
        tooltipObject.transform.SetAsLastSibling();
        tooltipObject.SetActive(true);
    }

    public static void Hide()
    {
        activeAnchor = null;
        if (tooltipObject != null)
        {
            tooltipObject.SetActive(false);
        }
    }

    private static void EnsureTooltip(RectTransform anchor)
    {
        if (tooltipObject != null)
        {
            return;
        }

        Canvas canvas = anchor.GetComponentInParent<Canvas>();
        Transform parent = canvas != null ? canvas.transform : anchor.root;
        tooltipObject = new GameObject("ContextualInfoTooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        tooltipObject.transform.SetParent(parent, false);

        var rect = tooltipObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = UnityUIRuntimeTheme.IsPortraitNarrowScreen()
            ? new Vector2(356f, 198f)
            : new Vector2(448f, 184f);

        var image = tooltipObject.GetComponent<Image>();
        ModernWafuuPresentation.ApplyFlatSurface(image, new Color(0.15f, 0.11f, 0.065f, 0.98f));
        image.raycastTarget = false;
        var outline = tooltipObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.96f, 0.76f, 0.38f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        titleText = CreateText("Title", tooltipObject.transform, 25f, 30f, TextAlignmentOptions.MidlineLeft, new Color(1f, 0.92f, 0.68f));
        titleText.fontStyle = FontStyles.Bold;
        Stretch(titleText.rectTransform, new Vector2(0.06f, 0.67f), new Vector2(0.94f, 0.91f));

        bodyText = CreateText("Body", tooltipObject.transform, 21f, 24f, TextAlignmentOptions.TopLeft, new Color(1f, 0.95f, 0.80f));
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        Stretch(bodyText.rectTransform, new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.62f));

        tooltipObject.SetActive(false);
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, float minSize, float maxSize, TextAlignmentOptions alignment, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
        text.enableAutoSizing = true;
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static void PositionBelow(RectTransform anchor)
    {
        var tooltipRect = tooltipObject.transform as RectTransform;
        var rootRect = tooltipRect.parent as RectTransform;
        if (tooltipRect == null || rootRect == null)
        {
            return;
        }

        Vector3[] corners = new Vector3[4];
        anchor.GetWorldCorners(corners);
        Vector3 local = rootRect.InverseTransformPoint(corners[0]);
        Vector2 halfSize = tooltipRect.rect.size * 0.5f;
        Rect bounds = rootRect.rect;
        const float margin = 16f;
        local.x = Mathf.Clamp(local.x + halfSize.x, bounds.xMin + halfSize.x + margin, bounds.xMax - halfSize.x - margin);
        local.y = Mathf.Clamp(local.y - halfSize.y - margin, bounds.yMin + halfSize.y + margin, bounds.yMax - halfSize.y - margin);
        tooltipRect.localPosition = local;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
