using UnityEngine;
using UnityEngine.UI;

public static class BattleUILayout
{
    public static float Apply(
        Transform panelRoot,
        Transform battleField,
        ScrollRect logScroll,
        int cols,
        int rows)
    {
        bool portrait = UnityUIRuntimeTheme.IsPortraitNarrowScreen();
        ConfigureBattleField(battleField, portrait);
        ConfigureBattleLog(logScroll, portrait);
        ConfigureControlButtons(panelRoot, portrait);
        HideLegacyBackButton(panelRoot);

        if (battleField != null && battleField.TryGetComponent(out GridLayoutGroup grid))
        {
            return ApplyGridSize(panelRoot, battleField, grid, cols, rows, portrait);
        }

        return 60f;
    }

    public static void ApplyCharacterVisualSize(RectTransform rect, float cellSize)
    {
        if (rect == null) return;

        float pieceSize = Mathf.Clamp(cellSize * 0.84f, 52f, 96f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(pieceSize, pieceSize);
    }

    public static void StyleBattleCell(GameObject cell, int x, int y)
    {
        var image = cell.GetComponent<Image>();
        if (image != null)
        {
            bool alternate = (x + y) % 2 == 0;
            image.color = alternate ? new Color(0.80f, 0.84f, 0.88f, 1f) : new Color(0.69f, 0.75f, 0.82f, 1f);
        }

        var outline = cell.GetComponent<Outline>();
        if (outline == null) outline = cell.AddComponent<Outline>();
        outline.effectColor = new Color(0.18f, 0.22f, 0.28f, 0.75f);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = false;
    }

    private static void ConfigureBattleField(Transform battleField, bool portrait)
    {
        var fieldRect = battleField as RectTransform;
        if (fieldRect == null) return;

        ConfigureBattleFieldContainer(fieldRect, portrait);
        fieldRect.anchorMin = new Vector2(0.5f, 0.5f);
        fieldRect.anchorMax = new Vector2(0.5f, 0.5f);
        fieldRect.pivot = new Vector2(0.5f, 0.5f);
        fieldRect.anchoredPosition = Vector2.zero;

        var fitter = fieldRect.GetComponent<AspectRatioFitter>();
        if (fitter != null)
        {
            Object.Destroy(fitter);
        }
    }

    private static void ConfigureBattleFieldContainer(RectTransform fieldRect, bool portrait)
    {
        var container = fieldRect.parent as RectTransform;
        if (container == null) return;

        container.anchorMin = portrait ? new Vector2(0.5f, 0.64f) : new Vector2(0.5f, 0.66f);
        container.anchorMax = container.anchorMin;
        container.pivot = new Vector2(0.5f, 0.5f);
        container.anchoredPosition = Vector2.zero;
    }

    private static void ConfigureBattleLog(ScrollRect logScroll, bool portrait)
    {
        var logRect = logScroll != null ? logScroll.GetComponent<RectTransform>() : null;
        if (logRect == null) return;

        logScroll.horizontal = false;
        logScroll.horizontalScrollbar = null;
        logScroll.verticalScrollbar = null;
        HideScrollbars(logScroll);

        logRect.anchorMin = portrait ? new Vector2(0.04f, 0.08f) : new Vector2(0.16f, 0.08f);
        logRect.anchorMax = portrait ? new Vector2(0.96f, 0.25f) : new Vector2(0.84f, 0.23f);
        logRect.offsetMin = Vector2.zero;
        logRect.offsetMax = Vector2.zero;
    }

    private static void HideScrollbars(ScrollRect scrollRect)
    {
        if (scrollRect == null) return;

        foreach (var scrollbar in scrollRect.GetComponentsInChildren<Scrollbar>(true))
        {
            scrollbar.gameObject.SetActive(false);
        }
    }

    private static void ConfigureControlButtons(Transform panelRoot, bool portrait)
    {
        PlaceControlButton(FindChildRect(panelRoot, "PauseButton"), portrait ? new Vector2(0.34f, 0.285f) : new Vector2(0.42f, 0.285f), portrait, panelRoot);
        PlaceControlButton(FindChildRect(panelRoot, "ResumeButton"), portrait ? new Vector2(0.54f, 0.285f) : new Vector2(0.58f, 0.285f), portrait, panelRoot);
    }

    private static void PlaceControlButton(RectTransform rect, Vector2 centerAnchor, bool portrait, Transform panelRoot)
    {
        if (rect == null) return;

        if (rect.parent != panelRoot)
        {
            rect.SetParent(panelRoot, false);
        }

        rect.anchorMin = centerAnchor;
        rect.anchorMax = centerAnchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = portrait ? new Vector2(120f, 44f) : new Vector2(120f, 48f);
    }

    private static void HideLegacyBackButton(Transform panelRoot)
    {
        var backButton = FindChildRect(panelRoot, "BackButton");
        if (backButton != null)
        {
            backButton.gameObject.SetActive(false);
        }
    }

    private static RectTransform FindChildRect(Transform root, string childName)
    {
        if (root == null) return null;

        foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect.name == childName)
            {
                return rect;
            }
        }

        return null;
    }

    private static float ApplyGridSize(Transform panelRoot, Transform battleField, GridLayoutGroup grid, int cols, int rows, bool portrait)
    {
        var fieldRect = battleField as RectTransform;
        var containerRect = fieldRect != null ? fieldRect.parent as RectTransform : null;
        var rootRect = panelRoot as RectTransform;
        float rootWidth = rootRect != null && rootRect.rect.width > 0f ? rootRect.rect.width : 1280f;
        float rootHeight = rootRect != null && rootRect.rect.height > 0f ? rootRect.rect.height : 720f;
        float fieldWidth = portrait ? rootWidth * 0.96f : rootWidth * 0.64f;
        float fieldHeight = portrait ? rootHeight * 0.42f : rootHeight * 0.44f;
        float spacing = portrait ? 8f : 6f;
        int boardPadding = portrait ? 12 : 10;
        float cellSize = Mathf.Floor(Mathf.Min(
            (fieldWidth - boardPadding * 2f - spacing * (cols - 1)) / cols,
            (fieldHeight - boardPadding * 2f - spacing * (rows - 1)) / rows));

        float currentCellSize = Mathf.Max(48f, cellSize);
        grid.padding = new RectOffset(boardPadding, boardPadding, boardPadding, boardPadding);
        grid.spacing = new Vector2(spacing, spacing);
        grid.cellSize = new Vector2(currentCellSize, currentCellSize);
        grid.childAlignment = TextAnchor.MiddleCenter;

        if (fieldRect != null)
        {
            var boardSize = new Vector2(
                currentCellSize * cols + spacing * (cols - 1) + boardPadding * 2f,
                currentCellSize * rows + spacing * (rows - 1) + boardPadding * 2f);

            fieldRect.sizeDelta = boardSize;
            if (containerRect != null)
            {
                containerRect.sizeDelta = boardSize;
            }
        }

        return currentCellSize;
    }
}
