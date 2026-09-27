using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A persistent, screen-level information surface. It keeps explanatory text out
/// of scrollable lists, where a floating tooltip would hide or intercept a target.
/// </summary>
public sealed class ScreenInfoRegion : MonoBehaviour
{
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI bodyText;
    private Button actionButton;
    private TextMeshProUGUI actionText;
    private RectTransform pieceRows;
    private bool reserveLowerContent;

    public static ScreenInfoRegion Ensure(Transform host, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (host == null)
        {
            return null;
        }

        Transform existing = host.Find(name);
        ScreenInfoRegion region = existing != null ? existing.GetComponent<ScreenInfoRegion>() : null;
        if (region == null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(ScreenInfoRegion));
            go.transform.SetParent(host, false);
            region = go.GetComponent<ScreenInfoRegion>();
        }

        region.ApplyLayout(anchorMin, anchorMax);
        return region;
    }

    public void Configure(string title, string body, string actionLabel = null, bool actionEnabled = false, Action action = null)
    {
        EnsureChildren();

        titleText.text = title ?? string.Empty;
        bodyText.text = body ?? string.Empty;

        bool showAction = !string.IsNullOrEmpty(actionLabel);
        actionButton.gameObject.SetActive(showAction);
        ApplyTextLayout(false, showAction);
        ClearPieceRows();
        if (!showAction)
        {
            return;
        }

        actionText.text = actionLabel;
        actionButton.interactable = actionEnabled && action != null;
        actionButton.onClick.RemoveAllListeners();
        if (action != null)
        {
            actionButton.onClick.AddListener(() => action());
        }
    }

    public void ReserveLowerContent(bool value)
    {
        EnsureChildren();
        reserveLowerContent = value;
        bool showAction = actionButton != null && actionButton.gameObject.activeSelf;
        ApplyTextLayout(false, showAction);
    }

    public void ConfigurePieceRows(IReadOnlyList<CharacterData> allies, IReadOnlyList<CharacterData> enemies)
    {
        EnsureChildren();
        EnsurePieceRows();
        ClearPieceRows();

        bool hasAllies = HasPieces(allies);
        bool hasEnemies = HasPieces(enemies);
        bool hasRows = hasAllies || hasEnemies;
        pieceRows.gameObject.SetActive(hasRows);
        if (!hasRows)
        {
            return;
        }

        bool showAction = actionButton != null && actionButton.gameObject.activeSelf;
        ApplyTextLayout(true, showAction);
        if (hasAllies)
        {
            CreatePieceRow("自軍", allies, new Color(0.42f, 0.77f, 1f, 1f));
        }

        if (hasEnemies)
        {
            CreatePieceRow("敵軍", enemies, new Color(1f, 0.53f, 0.48f, 1f));
        }
    }

    private void ApplyLayout(Vector2 anchorMin, Vector2 anchorMax)
    {
        var image = GetComponent<Image>();
        ModernWafuuPresentation.ApplyFlatSurface(image, new Color(0.13f, 0.09f, 0.05f, 0.94f));
        image.raycastTarget = false;

        var outline = GetComponent<Outline>();
        outline.effectColor = new Color(0.83f, 0.63f, 0.30f, 0.68f);
        outline.effectDistance = new Vector2(1f, -1f);

        var rect = GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        transform.SetAsLastSibling();

        EnsureChildren();
    }

    private void EnsureChildren()
    {
        if (titleText == null)
        {
            titleText = CreateText("Title", 25f, 28f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(1f, 0.91f, 0.66f));
            Stretch(titleText.rectTransform, new Vector2(0.05f, 0.55f), new Vector2(0.70f, 0.92f));
        }

        if (bodyText == null)
        {
            bodyText = CreateText("Body", 21f, 23f, FontStyles.Normal, TextAlignmentOptions.TopLeft, new Color(1f, 0.96f, 0.84f));
            bodyText.textWrappingMode = TextWrappingModes.Normal;
            bodyText.overflowMode = TextOverflowModes.Ellipsis;
            Stretch(bodyText.rectTransform, new Vector2(0.05f, 0.10f), new Vector2(0.70f, 0.55f));
        }

        if (actionButton != null)
        {
            return;
        }

        var actionObject = new GameObject("Action", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        actionObject.transform.SetParent(transform, false);
        actionButton = actionObject.GetComponent<Button>();
        ApplyPrimaryActionTreatment(actionButton);
        Stretch(actionButton.GetComponent<RectTransform>(), new Vector2(0.74f, 0.20f), new Vector2(0.95f, 0.80f));

        actionText = CreateText("Label", 21f, 24f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(1f, 0.95f, 0.80f), actionButton.transform);
        Stretch(actionText.rectTransform, Vector2.zero, Vector2.one);
    }

    private static void ApplyPrimaryActionTreatment(Button button)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        ModernWafuuPresentation.ApplyFlatSurface(image, new Color(0.67f, 0.10f, 0.06f, 1f));
        Outline outline = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.74f, 0.30f, 0.88f);
        outline.effectDistance = new Vector2(2f, -2f);

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.89f, 0.73f, 1f);
        colors.pressedColor = new Color(0.86f, 0.68f, 0.50f, 1f);
        colors.disabledColor = new Color(0.40f, 0.29f, 0.25f, 0.92f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
    }

    private void EnsurePieceRows()
    {
        if (pieceRows != null)
        {
            return;
        }

        Transform existing = transform.Find("PieceRows");
        if (existing != null)
        {
            pieceRows = existing as RectTransform;
        }

        if (pieceRows == null)
        {
            var go = new GameObject("PieceRows", typeof(RectTransform), typeof(VerticalLayoutGroup));
            go.transform.SetParent(transform, false);
            pieceRows = go.GetComponent<RectTransform>();
        }

        var layout = pieceRows.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            layout.spacing = 4f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            // Keep the piece row at its declared height. Expanding it makes the
            // icon spill into the information text on taller stage headers.
            layout.childForceExpandHeight = false;
        }

        Stretch(pieceRows, new Vector2(0.05f, 0.28f), new Vector2(0.70f, 0.69f));
        pieceRows.SetAsLastSibling();
    }

    private void ClearPieceRows()
    {
        if (pieceRows == null)
        {
            return;
        }

        for (int i = pieceRows.childCount - 1; i >= 0; i--)
        {
            Destroy(pieceRows.GetChild(i).gameObject);
        }

        pieceRows.gameObject.SetActive(false);
    }

    private void CreatePieceRow(string label, IReadOnlyList<CharacterData> characters, Color labelColor)
    {
        var row = new GameObject("PieceRow_" + label, typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(pieceRows, false);
        row.GetComponent<LayoutElement>().preferredHeight = 76f;
        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 7f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI rowLabel = CreateText("Label", 21f, 23f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, labelColor, row.transform);
        rowLabel.text = label;
        var labelLayout = rowLabel.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 54f;
        labelLayout.preferredHeight = 72f;

        foreach (CharacterData character in characters)
        {
            if (character == null)
            {
                continue;
            }

            var iconObject = new GameObject("Piece_" + character.characterName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(AspectRatioFitter), typeof(LayoutElement));
            iconObject.transform.SetParent(row.transform, false);
            var icon = iconObject.GetComponent<Image>();
            // The campaign is a reconnaissance view: enemy pieces face upward here.
            // Only the battle board uses the reverse-facing enemy art.
            icon.sprite = character.icon;
            icon.preserveAspect = true;
            icon.color = icon.sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            icon.raycastTarget = false;
            var fitter = iconObject.GetComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;
            var iconLayout = iconObject.GetComponent<LayoutElement>();
            iconLayout.preferredWidth = 72f;
            iconLayout.preferredHeight = 72f;
        }
    }

    private void ApplyTextLayout(bool hasPieceRows, bool showAction)
    {
        float textMax = showAction ? 0.70f : 0.95f;
        if (hasPieceRows)
        {
            Stretch(titleText.rectTransform, new Vector2(0.05f, 0.72f), new Vector2(textMax, 0.94f));
            Stretch(bodyText.rectTransform, new Vector2(0.05f, 0.06f), new Vector2(textMax, 0.24f));
            if (pieceRows != null)
            {
                Stretch(pieceRows, new Vector2(0.05f, 0.27f), new Vector2(textMax, 0.69f));
            }
            return;
        }

        if (reserveLowerContent)
        {
            Stretch(titleText.rectTransform, new Vector2(0.05f, 0.76f), new Vector2(textMax, 0.94f));
            Stretch(bodyText.rectTransform, new Vector2(0.05f, 0.56f), new Vector2(textMax, 0.75f));
            return;
        }

        Stretch(titleText.rectTransform, new Vector2(0.05f, 0.55f), new Vector2(textMax, 0.92f));
        Stretch(bodyText.rectTransform, new Vector2(0.05f, 0.10f), new Vector2(textMax, 0.55f));
    }

    private static bool HasPieces(IReadOnlyList<CharacterData> characters)
    {
        if (characters == null)
        {
            return false;
        }

        foreach (CharacterData character in characters)
        {
            if (character != null)
            {
                return true;
            }
        }

        return false;
    }

    private TextMeshProUGUI CreateText(string name, float minSize, float maxSize, FontStyles style, TextAlignmentOptions alignment, Color color, Transform parent = null)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent ?? transform, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
        text.enableAutoSizing = true;
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.outlineColor = new Color(0.06f, 0.04f, 0.02f, 0.96f);
        text.outlineWidth = 0.16f;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
