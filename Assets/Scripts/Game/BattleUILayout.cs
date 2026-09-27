using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class BattleUILayout
{
    private enum GroundTheme
    {
        EarlySoil,
        Field,
        Fortified,
    }

    private static int chapterTheme = 1;
    private static string stageTitle = "戦場";
    private static string stageSubtitle = "敵軍をすべて退けよ";
    private static GroundTheme groundTheme;

    public static bool UsesFortifiedTerrain => groundTheme == GroundTheme.Fortified;

    // Water uses one shared board-space texture in every environment. The stone
    // backdrop and label distinguish a castle waterway without breaking continuity.
    public static string ImpassableTerrainTexturePath => "UI/ModernWafuu/Terrain/TerrainWater";

    public static string TrapTerrainTexturePath => UsesFortifiedTerrain
        ? "UI/ModernWafuu/Terrain/TerrainPavingBroken"
        : "UI/ModernWafuu/Terrain/TerrainSoil";

    public static void SetChapterTheme(int chapterId)
    {
        chapterTheme = Mathf.Max(1, chapterId);
    }

    public static void SetStageContext(StageData stage)
    {
        SetChapterTheme(stage != null ? stage.chapterId : 1);
        groundTheme = ResolveGroundTheme(stage);
        stageTitle = stage != null
            ? $"第{stage.stageId:00}局  {stage.stageName}"
            : "戦場";
        stageSubtitle = stage != null && stage.isBossStage
            ? "大将を討ち、局を制せ"
            : "敵軍をすべて退けよ";
    }

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
        EnsureBattleHeader(panelRoot, portrait);
        EnsureCommandBand(panelRoot, portrait);
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
            // The terrain is a single continuous board image beneath the grid. Cells
            // stay almost transparent so their input and outlines remain available.
            image.color = new Color(0.20f, 0.14f, 0.08f, 0.025f);
        }

        var outline = cell.GetComponent<Outline>();
        if (outline == null) outline = cell.AddComponent<Outline>();
        outline.effectColor = new Color(0.31f, 0.20f, 0.10f, 0.70f);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = false;

        var shadow = cell.GetComponent<Shadow>();
        if (shadow == null) shadow = cell.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.09f, 0.06f, 0.03f, 0.20f);
        shadow.effectDistance = new Vector2(0f, -2f);
        shadow.useGraphicAlpha = false;

    }

    private static bool IsFortifiedStage(StageData stage)
    {
        if (stage == null) return false;

        // The campaign landmark sequence reserves each fourth stop for a checkpoint
        // and the final boss for the castle. Their boards share built terrain.
        return stage.isBossStage || (stage.stageId - 1) % 5 == 3;
    }

    private static GroundTheme ResolveGroundTheme(StageData stage)
    {
        if (IsFortifiedStage(stage)) return GroundTheme.Fortified;

        // The opening lessons occur on a sparse earthen training ground. Green
        // terrain is reserved for the animal chapter and other outdoor encounters.
        if (stage != null && stage.chapterId == 1) return GroundTheme.EarlySoil;

        return GroundTheme.Field;
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

    private static void EnsureBattleHeader(Transform panelRoot, bool portrait)
    {
        if (panelRoot == null) return;

        var header = EnsureImage(panelRoot, "BattleInformationHeader");
        ModernWafuuPresentation.ApplyFlatSurface(header, new Color(0.10f, 0.075f, 0.045f, 0.92f));
        header.raycastTarget = false;
        Place(header.rectTransform,
            portrait ? new Vector2(0.04f, 0.89f) : new Vector2(0.08f, 0.90f),
            portrait ? new Vector2(0.96f, 0.97f) : new Vector2(0.92f, 0.97f));
        header.transform.SetAsLastSibling();

        var outline = header.GetComponent<Outline>() ?? header.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.84f, 0.62f, 0.28f, 0.72f);
        outline.effectDistance = new Vector2(1f, -1f);

        TextMeshProUGUI title = EnsureText(header.transform, "Title", 23f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, new Color(1f, 0.90f, 0.63f));
        title.text = stageTitle;
        Place(title.rectTransform, new Vector2(0.04f, 0.38f), new Vector2(0.68f, 0.92f));

        TextMeshProUGUI objective = EnsureText(header.transform, "Objective", 21f, FontStyles.Normal, TextAlignmentOptions.MidlineRight, new Color(0.94f, 0.91f, 0.79f));
        objective.text = stageSubtitle;
        Place(objective.rectTransform, new Vector2(0.45f, 0.08f), new Vector2(0.96f, 0.55f));
    }

    private static void EnsureCommandBand(Transform panelRoot, bool portrait)
    {
        if (panelRoot == null) return;

        var band = EnsureImage(panelRoot, "BattleCommandBand");
        ModernWafuuPresentation.ApplyFlatSurface(band, new Color(0.10f, 0.075f, 0.045f, 0.86f));
        band.raycastTarget = false;
        Place(band.rectTransform,
            portrait ? new Vector2(0.04f, 0.05f) : new Vector2(0.14f, 0.05f),
            portrait ? new Vector2(0.96f, 0.32f) : new Vector2(0.86f, 0.31f));

        var outline = band.GetComponent<Outline>() ?? band.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.78f, 0.59f, 0.30f, 0.50f);
        outline.effectDistance = new Vector2(1f, -1f);

        // Keep the command band behind the scroll log and buttons while remaining above
        // the battle environment. It never participates in input.
        band.transform.SetSiblingIndex(Mathf.Min(1, panelRoot.childCount - 1));
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
        PlaceControlButton(FindChildRect(panelRoot, "SpeedButton"), portrait ? new Vector2(0.74f, 0.285f) : new Vector2(0.70f, 0.285f), portrait, panelRoot);
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

            EnsureBoardTerrainBackdrop(fieldRect, boardSize);
            EnsureBoardDecoration(fieldRect, boardSize, cols, rows);
        }

        return currentCellSize;
    }

    private static void EnsureBoardTerrainBackdrop(RectTransform fieldRect, Vector2 boardSize)
    {
        var container = fieldRect != null ? fieldRect.parent as RectTransform : null;
        if (container == null) return;

        Transform existing = container.Find("BattleTerrainBackdrop");
        RawImage backdrop = existing != null ? existing.GetComponent<RawImage>() : null;
        if (backdrop == null)
        {
            var go = new GameObject("BattleTerrainBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(container, false);
            backdrop = go.GetComponent<RawImage>();
        }

        string path = UsesFortifiedTerrain
            ? "UI/ModernWafuu/Terrain/BattleFortifiedGround"
            : groundTheme == GroundTheme.EarlySoil
                ? "UI/ModernWafuu/Terrain/BattleEarlySoilGround"
                : "UI/ModernWafuu/Terrain/BattleFieldGround";
        backdrop.texture = Resources.Load<Texture2D>(path);
        backdrop.color = new Color(1.16f, 1.11f, 1.03f, 1f);
        backdrop.uvRect = new Rect(0f, 0f, 1f, 1f);
        backdrop.raycastTarget = false;

        RectTransform rect = backdrop.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = boardSize;
        backdrop.transform.SetSiblingIndex(Mathf.Min(1, container.childCount - 1));
    }

    private static void EnsureBoardDecoration(RectTransform fieldRect, Vector2 boardSize, int cols, int rows)
    {
        var container = fieldRect != null ? fieldRect.parent : null;
        if (container == null) return;

        var frame = EnsureImage(container, "BattleBoardFrame");
        ModernWafuuPresentation.ApplyFlatSurface(frame, new Color(0.34f, 0.22f, 0.12f, 0.94f));
        frame.raycastTarget = false;
        RectTransform frameRect = frame.rectTransform;
        frameRect.anchorMin = new Vector2(0.5f, 0.5f);
        frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.anchoredPosition = Vector2.zero;
        frameRect.sizeDelta = boardSize + new Vector2(52f, 52f);
        frame.transform.SetAsFirstSibling();

        var outline = frame.GetComponent<Outline>() ?? frame.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.88f, 0.65f, 0.29f, 0.70f);
        outline.effectDistance = new Vector2(2f, -2f);

        EnsureBoardCoordinates(frame.transform, cols, rows);
    }

    private static void EnsureBoardCoordinates(Transform frame, int cols, int rows)
    {
        for (int x = 0; x < cols; x++)
        {
            TextMeshProUGUI label = EnsureText(frame, $"Column_{x + 1}", 21f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.98f, 0.84f, 0.53f));
            label.text = (x + 1).ToString();
            float center = (x + 0.5f) / cols;
            Place(label.rectTransform, new Vector2(center - 0.04f, 0.93f), new Vector2(center + 0.04f, 0.995f));
        }

        for (int y = 0; y < rows; y++)
        {
            TextMeshProUGUI label = EnsureText(frame, $"Row_{y + 1}", 21f, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.98f, 0.84f, 0.53f));
            label.text = ((char)('A' + y)).ToString();
            float center = 1f - (y + 0.5f) / rows;
            Place(label.rectTransform, new Vector2(0.005f, center - 0.055f), new Vector2(0.075f, center + 0.055f));
        }
    }

    private static Image EnsureImage(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null && existing.TryGetComponent(out Image image)) return image;

        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        return go.GetComponent<Image>();
    }

    private static TextMeshProUGUI EnsureText(Transform parent, string name, float size, FontStyles style, TextAlignmentOptions alignment, Color color)
    {
        Transform existing = parent.Find(name);
        TextMeshProUGUI text = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;
        if (text == null)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            text = go.GetComponent<TextMeshProUGUI>();
        }

        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
        text.enableAutoSizing = true;
        text.fontSizeMin = UnityUIRuntimeTheme.MinimumTextSize;
        text.fontSizeMax = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.outlineColor = new Color(0.04f, 0.025f, 0.01f, 0.92f);
        text.outlineWidth = 0.16f;
        text.raycastTarget = false;
        return text;
    }

    private static void Place(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
