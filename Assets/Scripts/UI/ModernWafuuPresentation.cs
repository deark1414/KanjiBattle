using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class ModernWafuuPresentation
{
    private static readonly Dictionary<string, Sprite> sprites = new();
    private static Sprite flatSprite;
    private static bool globalStatusVisible = true;

    private static readonly ScreenChromeSpec[] screenSpecs =
    {
        new("StageSelectPanel", "行軍図"),
        new("FormationPanel", "出陣編成"),
        new("FacilityPanel", "城下"),
    };

    public static void ApplyCanvasBackdrop(Canvas canvas)
    {
        if (!IsPrimaryPresentationCanvas(canvas))
        {
            return;
        }

        Transform transform = canvas.transform.Find("AutoTheme_Backdrop");
        if (transform == null || !transform.TryGetComponent(out Image image))
        {
            return;
        }

        image.sprite = GetSprite(GetNavigationBackdropPath());
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = false;

        EnsureGlobalStatusBar(canvas);
    }

    public static void ApplyButtonTreatment(Button button)
    {
        if (button == null || button.name.StartsWith("StageButton"))
        {
            return;
        }

        Image image = button.targetGraphic as Image ?? button.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        string name = button.name.ToLowerInvariant();
        bool primary = name.Contains("nextstage") || name.Contains("startbattle");
        bool tab = name.Contains("tab");
        bool danger = name.Contains("reset");

        bool imageAction = name.StartsWith("topnav") || name.Contains("nextstageaction");
        if (!imageAction)
        {
            ApplyFlatSurface(image, primary
                ? new Color(0.55f, 0.18f, 0.09f, 1f)
                : danger
                    ? new Color(0.28f, 0.12f, 0.10f, 1f)
                    : tab
                        ? new Color(0.12f, 0.11f, 0.09f, 0.94f)
                        : new Color(0.34f, 0.29f, 0.20f, 0.92f));
        }

        var outline = button.GetComponent<Outline>();
        if (outline == null)
        {
            outline = button.gameObject.AddComponent<Outline>();
        }
        outline.effectDistance = new Vector2(1f, -1f);
        outline.effectColor = primary
            ? new Color(0.95f, 0.67f, 0.30f, 0.80f)
            : new Color(0.74f, 0.62f, 0.43f, 0.38f);

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = primary
            ? new Color(1f, 0.84f, 0.58f, 1f)
            : new Color(0.90f, 0.83f, 0.66f, 1f);
        colors.pressedColor = primary
            ? new Color(0.72f, 0.45f, 0.27f, 1f)
            : new Color(0.62f, 0.56f, 0.44f, 1f);
        colors.selectedColor = tab ? new Color(0.73f, 0.30f, 0.16f, 1f) : Color.white;
        colors.disabledColor = new Color(0.45f, 0.42f, 0.36f, 0.72f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
    }

    public static void ApplyListCardSurface(Image image, Color color, bool emphasis = false)
    {
        if (image == null)
        {
            return;
        }

        ApplyFlatSurface(image, color);
        var outline = image.GetComponent<Outline>();
        if (outline == null)
        {
            outline = image.gameObject.AddComponent<Outline>();
        }
        outline.effectDistance = new Vector2(1f, -1f);
        outline.effectColor = emphasis
            ? new Color(0.95f, 0.68f, 0.31f, 0.82f)
            : new Color(0.70f, 0.62f, 0.48f, 0.34f);
    }

    public static void ApplyFlatSurface(Image image, Color color)
    {
        if (image == null)
        {
            return;
        }

        image.sprite = GetFlatSprite();
        image.type = Image.Type.Simple;
        image.color = color;
    }

    public static void ApplyScreenChrome()
    {
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (IsPrimaryPresentationCanvas(canvas))
            {
                ApplyCanvasBackdrop(canvas);
            }
        }

        RefreshGlobalStatusBar();
        foreach (ScreenChromeSpec spec in screenSpecs)
        {
            GameObject panel = GameObject.Find(spec.panelName);
            if (panel == null)
            {
                continue;
            }

            EnsurePanelHeader(panel.transform, spec);
        }

        GameObject topPanel = GameObject.Find("TopPanel");
        if (topPanel != null)
        {
            EnsureHomeBackdrop(topPanel.transform);
            SetInactive(topPanel.transform, "TopProgressPanel");
            SetInactive(topPanel.transform, "WafuuScreenHeader");
            SetInactive(topPanel.transform, "NextStageAction");
            SetInactive(topPanel.transform, "TopNavigationTiles");
        }

        GameObject stagePanel = GameObject.Find("StageSelectPanel");
        if (stagePanel != null)
        {
            EnsureMarchBackdrop(stagePanel.transform);
        }

        GameObject formationPanel = GameObject.Find("FormationPanel");
        if (formationPanel != null)
        {
            EnsureFormationFrame(formationPanel.transform);
        }

        GameObject facilityPanel = GameObject.Find("FacilityPanel");
        if (facilityPanel != null)
        {
            EnsureFacilityFrame(facilityPanel.transform);
        }
    }

    public static void RefreshGlobalStatus()
    {
        RefreshGlobalStatusBar();
    }

    public static void SetGlobalStatusVisible(bool visible)
    {
        globalStatusVisible = visible;
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (!IsPrimaryPresentationCanvas(canvas))
            {
                continue;
            }

            Transform bar = canvas.transform.Find("GlobalStatusBar");
            if (bar != null)
            {
                bar.gameObject.SetActive(visible);
            }
        }

        if (visible)
        {
            RefreshGlobalStatusBar();
        }
    }

    public static void ApplyBattleEnvironment(Transform battlePanel, int chapterId)
    {
        if (battlePanel == null)
        {
            return;
        }

        Transform existing = battlePanel.Find("WafuuBattleEnvironment");
        Image image;
        if (existing != null && existing.TryGetComponent(out image))
        {
            image.gameObject.SetActive(true);
        }
        else
        {
            var go = new GameObject("WafuuBattleEnvironment", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(battlePanel, false);
            go.transform.SetAsFirstSibling();
            image = go.GetComponent<Image>();
            image.raycastTarget = false;
        }

        RectTransform rect = image.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        bool portrait = UnityUIRuntimeTheme.IsPortraitNarrowScreen();
        string resourcePath = portrait ? "UI/ModernWafuu/TopWashiBackdrop" : chapterId switch
        {
            <= 2 => "UI/ModernWafuu/BattleGarden",
            <= 5 => "UI/ModernWafuu/BattleStoneCastle",
            _ => "UI/ModernWafuu/BattleSnowShrine",
        };

        image.sprite = GetSprite(resourcePath);
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = portrait
            ? GetPortraitBattleTint(chapterId)
            : chapterId % 2 == 0
                ? new Color(1.02f, 1.06f, 1.08f, 1f)
                : new Color(1.08f, 1.05f, 1f, 1f);
    }

    private static void EnsureHomeBackdrop(Transform panel)
    {
        Image hero = EnsureImage(panel, "HomeHeroLandscape");
        hero.sprite = GetSprite(GetHomeBackdropPath());
        hero.type = Image.Type.Simple;
        hero.preserveAspect = false;
        hero.color = Color.white;
        hero.raycastTarget = false;
        Stretch(hero.rectTransform, Vector2.zero, Vector2.one);
        hero.transform.SetAsFirstSibling();

    }

    private static string GetNavigationBackdropPath()
    {
        bool portrait = UnityUIRuntimeTheme.IsPortraitNarrowScreen();
        if (IsPanelVisible("FacilityPanel"))
        {
            return portrait
                ? "UI/ModernWafuu/CastleTownPortrait"
                : "UI/ModernWafuu/CastleTownLandscape";
        }

        if (IsPanelVisible("TopPanel") || IsPanelVisible("FormationPanel"))
        {
            return portrait
                ? "UI/ModernWafuu/RosterCommandRoomPortrait"
                : "UI/ModernWafuu/RosterCommandRoomLandscape";
        }

        return portrait
            ? "UI/ModernWafuu/TopWashiBackdrop"
            : "UI/ModernWafuu/MarchMap";
    }

    private static string GetHomeBackdropPath()
    {
        bool portrait = UnityUIRuntimeTheme.IsPortraitNarrowScreen();
        int highestCleared = GameManager.Instance != null
            ? GameManager.Instance.GetHighestClearedStageId()
            : 0;

        string tier = highestCleared switch
        {
            <= 5 => "EarlyStorehouse",
            >= 21 => "LateTenshu",
            _ => "RosterCommandRoom",
        };

        return portrait
            ? $"UI/ModernWafuu/{tier}Portrait"
            : $"UI/ModernWafuu/{tier}Landscape";
    }

    private static bool IsPanelVisible(string name)
    {
        GameObject panel = GameObject.Find(name);
        return panel != null && panel.activeInHierarchy;
    }

    private static void EnsureMarchBackdrop(Transform panel)
    {
        bool portrait = UnityUIRuntimeTheme.IsPortraitNarrowScreen();
        Image map = EnsureImage(panel, "MarchMapBackdrop");
        map.gameObject.SetActive(!portrait);
        if (portrait)
        {
            return;
        }

        map.sprite = GetSprite("UI/ModernWafuu/MarchMap");
        map.type = Image.Type.Simple;
        map.preserveAspect = false;
        map.color = new Color(1f, 1f, 1f, 0.30f);
        map.raycastTarget = false;
        Place(map.rectTransform, new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.82f));
        map.transform.SetAsFirstSibling();

        foreach (ScrollRect scrollRect in panel.GetComponentsInChildren<ScrollRect>(true))
        {
            if (scrollRect.viewport != null && scrollRect.viewport.TryGetComponent(out Image viewport))
            {
                ApplyFlatSurface(viewport, new Color(0.95f, 0.90f, 0.78f, 0.88f));
            }
        }
    }

    private static void EnsureFormationFrame(Transform panel)
    {
        // The screen-level information region already names this view. Retire the
        // older floating header so it cannot sit on top of the stage briefing.
        SetInactive(panel, "WafuuScreenHeader");

        Image frame = EnsureImage(panel, "FormationPaperFrame");
        ApplyFlatSurface(frame, new Color(0.93f, 0.88f, 0.76f, 0.74f));
        frame.raycastTarget = false;
        // The formation information stays on the illustrated background; this paper field
        // deliberately contains only the selectable roster and formation tray.
        Place(frame.rectTransform, new Vector2(0.02f, 0.07f), new Vector2(0.98f, 0.645f));
        frame.transform.SetAsFirstSibling();
    }

    private static void EnsureFacilityFrame(Transform panel)
    {
        Image frame = EnsureImage(panel, "FacilityPaperFrame");
        ApplyFlatSurface(frame, new Color(0.93f, 0.88f, 0.76f, 0.74f));
        frame.raycastTarget = false;
        // This is a list field, not a screen-wide paper overlay. Keep it aligned
        // with the shared lower list area so the upper information region stays clear.
        Place(frame.rectTransform, new Vector2(0.04f, 0.10f), new Vector2(0.96f, 0.68f));
        frame.transform.SetAsFirstSibling();
    }

    private static Image EnsureImage(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null && existing.TryGetComponent(out Image image))
        {
            return image;
        }

        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        image = go.GetComponent<Image>();
        return image;
    }

    private static void SetInactive(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            existing.gameObject.SetActive(false);
        }
    }

    private static TextMeshProUGUI EnsureText(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null && existing.TryGetComponent(out TextMeshProUGUI text))
        {
            return text;
        }

        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        text = go.GetComponent<TextMeshProUGUI>();
        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
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

    private static void Place(RectTransform rect, Vector2 min, Vector2 max)
    {
        Stretch(rect, min, max);
    }

    private static void EnsurePanelHeader(Transform panel, ScreenChromeSpec spec)
    {
        Transform header = panel.Find("WafuuScreenHeader");
        if (header == null)
        {
            var go = new GameObject("WafuuScreenHeader", typeof(RectTransform));
            go.transform.SetParent(panel, false);
            header = go.transform;

            CreateHeaderText(header, "Title", TextAlignmentOptions.Left, 26f, FontStyles.Bold);
            CreateHeaderText(header, "Subtitle", TextAlignmentOptions.Right, 15f, FontStyles.Normal);
        }

        RectTransform headerRect = header.GetComponent<RectTransform>();
        bool portrait = UnityUIRuntimeTheme.IsPortraitNarrowScreen();
        headerRect.anchorMin = portrait ? new Vector2(0.05f, 0.83f) : new Vector2(0.06f, 0.82f);
        headerRect.anchorMax = portrait ? new Vector2(0.95f, 0.89f) : new Vector2(0.94f, 0.89f);
        headerRect.offsetMin = Vector2.zero;
        headerRect.offsetMax = Vector2.zero;

        var title = header.Find("Title")?.GetComponent<TextMeshProUGUI>();
        var subtitle = header.Find("Subtitle")?.GetComponent<TextMeshProUGUI>();
        header.gameObject.SetActive(true);
        if (title != null)
        {
            title.text = spec.title;
        }
        if (subtitle != null)
        {
            subtitle.text = BuildSubtitle();
        }
    }

    private static void RefreshGlobalStatusBar()
    {
        if (!globalStatusVisible)
        {
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                if (IsPrimaryPresentationCanvas(canvas))
                {
                    Transform bar = canvas.transform.Find("GlobalStatusBar");
                    if (bar != null) bar.gameObject.SetActive(false);
                }
            }
            return;
        }

        HideNestedStatusBars();
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (IsPrimaryPresentationCanvas(canvas))
            {
                EnsureGlobalStatusBar(canvas);
            }
        }
    }

    private static void EnsureGlobalStatusBar(Canvas canvas)
    {
        if (!IsPrimaryPresentationCanvas(canvas))
        {
            return;
        }

        if (!globalStatusVisible)
        {
            Transform hidden = canvas.transform.Find("GlobalStatusBar");
            if (hidden != null) hidden.gameObject.SetActive(false);
            return;
        }

        Transform existing = canvas.transform.Find("GlobalStatusBar");
        Image bar;
        if (existing != null && existing.TryGetComponent(out bar))
        {
            bar.gameObject.SetActive(true);
        }
        else
        {
            var go = new GameObject("GlobalStatusBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            go.transform.SetParent(canvas.transform, false);
            bar = go.GetComponent<Image>();
            bar.raycastTarget = false;
        }

        ApplyFlatSurface(bar, new Color(0.12f, 0.10f, 0.07f, 0.90f));
        Stretch(bar.rectTransform,
            UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? new Vector2(0.035f, 0.925f) : new Vector2(0.30f, 0.925f),
            new Vector2(0.965f, 0.985f));
        bar.transform.SetAsLastSibling();

        var outline = bar.GetComponent<Outline>();
        outline.effectColor = new Color(0.88f, 0.69f, 0.34f, 0.72f);
        outline.effectDistance = new Vector2(1f, -1f);

        int level = PlayerInventory.Instance != null ? PlayerInventory.Instance.PlayerLevel : 1;
        int experience = PlayerInventory.Instance != null ? PlayerInventory.Instance.PlayerExperience : 0;
        int nextExperience = PlayerInventory.Instance != null ? PlayerInventory.Instance.GetExperienceToNextPlayerLevel() : 1;
        int trainingSeconds = PlayerInventory.Instance != null ? PlayerInventory.Instance.GetSecondsUntilNextMockTraining() : 0;
        bool trainingCapped = PlayerInventory.Instance != null && PlayerInventory.Instance.IsAtEffectiveLevelCap;
        int stagePoints = GameManager.Instance != null ? GameManager.Instance.StagePoints : 0;

        bool portrait = UnityUIRuntimeTheme.IsPortraitNarrowScreen();
        CreateStatusButton(bar.transform, "PlayerLevelStatus", "軍師", $"Lv. {level}",
            portrait ? new Vector2(0.01f, 0.10f) : new Vector2(0.01f, 0.10f),
            portrait ? new Vector2(0.21f, 0.90f) : new Vector2(0.22f, 0.90f),
            anchor => UIManager.Instance?.ShowProgressInfo(ProgressInfoKind.PlayerLevel, anchor));
        CreateStatusButton(bar.transform, "PlayerExperienceStatus", "経験", $"{experience}/{nextExperience}",
            portrait ? new Vector2(0.22f, 0.10f) : new Vector2(0.23f, 0.10f),
            portrait ? new Vector2(0.43f, 0.90f) : new Vector2(0.45f, 0.90f),
            anchor => UIManager.Instance?.ShowProgressInfo(ProgressInfoKind.PlayerExperience, anchor));
        CreateStatusButton(bar.transform, "MockTrainingStatus", "稽古", trainingCapped ? "上限" : $"{trainingSeconds}秒",
            portrait ? new Vector2(0.44f, 0.10f) : new Vector2(0.46f, 0.10f),
            portrait ? new Vector2(0.61f, 0.90f) : new Vector2(0.61f, 0.90f),
            null, 16f, 20f);
        CreateStatusButton(bar.transform, "StagePointStatus", "戦果", stagePoints.ToString(),
            portrait ? new Vector2(0.62f, 0.10f) : new Vector2(0.62f, 0.10f),
            portrait ? new Vector2(0.81f, 0.90f) : new Vector2(0.81f, 0.90f),
            anchor => UIManager.Instance?.ShowProgressInfo(ProgressInfoKind.StagePoints, anchor));
        CreateStatusButton(bar.transform, "SettingsStatus", string.Empty, "設定",
            portrait ? new Vector2(0.82f, 0.10f) : new Vector2(0.82f, 0.10f),
            new Vector2(0.99f, 0.90f),
            _ => UIManager.Instance?.ShowSettings());
    }

    private static bool IsPrimaryPresentationCanvas(Canvas canvas)
    {
        // Nested override-sorting canvases are used for the result panel and common controls.
        // They must not receive a second copy of the global header.
        return canvas != null && canvas.transform.parent == null && canvas.renderMode != RenderMode.WorldSpace;
    }

    private static void HideNestedStatusBars()
    {
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (IsPrimaryPresentationCanvas(canvas))
            {
                continue;
            }

            Transform nestedBar = canvas.transform.Find("GlobalStatusBar");
            if (nestedBar != null)
            {
                nestedBar.gameObject.SetActive(false);
            }
        }
    }

    private static void CreateStatusButton(Transform parent, string name, string label, string value, Vector2 min, Vector2 max, System.Action<RectTransform> action, float fontSizeMin = 21f, float fontSizeMax = 24f)
    {
        Transform existing = parent.Find(name);
        Button button;
        if (existing != null && existing.TryGetComponent(out button))
        {
            button.gameObject.SetActive(true);
        }
        else
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
        }

        Image image = button.GetComponent<Image>();
        ApplyFlatSurface(image, new Color(0.24f, 0.19f, 0.12f, 0.96f));
        button.onClick.RemoveAllListeners();
        if (action != null)
        {
            button.onClick.AddListener(() => action(button.GetComponent<RectTransform>()));
        }
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.90f, 0.66f, 1f);
        colors.pressedColor = new Color(0.72f, 0.58f, 0.38f, 1f);
        colors.colorMultiplier = 1f;
        button.colors = colors;

        Stretch(button.GetComponent<RectTransform>(), min, max);
        button.transform.SetAsLastSibling();

        TextMeshProUGUI iconText = EnsureText(button.transform, "Icon");
        bool hasLabel = !string.IsNullOrEmpty(label);
        iconText.gameObject.SetActive(true);
        iconText.text = hasLabel ? $"{label}  {value}" : string.Empty;
        iconText.gameObject.SetActive(hasLabel);
        iconText.fontStyle = FontStyles.Bold;
        iconText.enableAutoSizing = true;
        iconText.fontSizeMin = fontSizeMin;
        iconText.fontSizeMax = fontSizeMax;
        iconText.alignment = TextAlignmentOptions.Center;
        iconText.color = new Color(1f, 0.90f, 0.63f);
        Place(iconText.rectTransform, new Vector2(0.04f, 0.14f), new Vector2(0.96f, 0.86f));

        TextMeshProUGUI valueText = EnsureText(button.transform, "Value");
        valueText.gameObject.SetActive(false);

        TextMeshProUGUI statusValueText = EnsureText(button.transform, "StatusValue");
        statusValueText.gameObject.SetActive(!hasLabel);
        statusValueText.text = value;
        statusValueText.fontStyle = FontStyles.Bold;
        statusValueText.enableAutoSizing = true;
        statusValueText.fontSizeMin = fontSizeMin;
        statusValueText.fontSizeMax = fontSizeMax;
        statusValueText.alignment = TextAlignmentOptions.Center;
        statusValueText.color = new Color(1f, 0.95f, 0.80f);
        Place(statusValueText.rectTransform,
            new Vector2(0.04f, 0.14f),
            new Vector2(0.96f, 0.86f));
    }

    private static void EnsureTopNextStageAction(Transform topPanel)
    {
        StageData stage = GetNextStage();
        Transform action = topPanel.Find("NextStageAction");
        if (stage == null)
        {
            if (action != null)
            {
                action.gameObject.SetActive(false);
            }
            return;
        }

        Button button;
        TextMeshProUGUI text;
        if (action != null && action.TryGetComponent(out button))
        {
            action.gameObject.SetActive(true);
            text = action.GetComponentInChildren<TextMeshProUGUI>(true);
        }
        else
        {
            var go = new GameObject("NextStageAction", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
            go.transform.SetParent(topPanel, false);
            action = go.transform;
            button = go.GetComponent<Button>();
            Image image = go.GetComponent<Image>();
            image.sprite = GetSprite("UI/ModernWafuu/MarchMap");
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
            go.GetComponent<Outline>().effectColor = new Color(0.96f, 0.79f, 0.38f, 0.7f);
            go.GetComponent<Outline>().effectDistance = new Vector2(2f, -2f);
            CreateImageShade(action, "Shade", new Color(0.06f, 0.05f, 0.03f, 0.34f));
            text = CreateActionText(action);
        }

        Image actionImage = button.GetComponent<Image>();
        actionImage.sprite = GetSprite("UI/ModernWafuu/MarchMap");
        actionImage.type = Image.Type.Simple;
        actionImage.preserveAspect = false;
        actionImage.color = Color.white;
        if (action.Find("Shade") == null)
        {
            CreateImageShade(action, "Shade", new Color(0.06f, 0.05f, 0.03f, 0.34f));
        }

        ApplyButtonTreatment(button);

        RectTransform rect = action.GetComponent<RectTransform>();
        bool portrait = UnityUIRuntimeTheme.IsPortraitNarrowScreen();
        rect.anchorMin = portrait ? new Vector2(0.04f, 0.64f) : new Vector2(0.64f, 0.47f);
        rect.anchorMax = portrait ? new Vector2(0.96f, 0.70f) : new Vector2(0.94f, 0.62f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        if (text != null)
        {
            text.text = $"次の局\n{stage.stageId:00}局  {stage.stageName}";
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            GameManager.Instance?.SetSelectedStage(stage);
            UIManager.Instance?.ShowFormation();
        });
    }

    private static void EnsureTopProgressPanel(Transform topPanel)
    {
        Image panel = EnsureImage(topPanel, "TopProgressPanel");
        ApplyFlatSurface(panel, new Color(0.13f, 0.11f, 0.08f, 0.82f));
        panel.raycastTarget = false;
        Place(panel.rectTransform,
            UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? new Vector2(0.04f, 0.72f) : new Vector2(0.64f, 0.70f),
            UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? new Vector2(0.96f, 0.82f) : new Vector2(0.94f, 0.81f));

        var outline = panel.GetComponent<Outline>();
        if (outline == null) outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.88f, 0.68f, 0.30f, 0.72f);
        outline.effectDistance = new Vector2(1f, -1f);

        int level = PlayerInventory.Instance != null ? PlayerInventory.Instance.PlayerLevel : 1;
        int experience = PlayerInventory.Instance != null ? PlayerInventory.Instance.PlayerExperience : 0;
        int nextExperience = PlayerInventory.Instance != null ? PlayerInventory.Instance.GetExperienceToNextPlayerLevel() : 1;
        int stagePoints = GameManager.Instance != null ? GameManager.Instance.StagePoints : 0;

        ConfigureProgressText(EnsureText(panel.transform, "LevelLabel"), "軍師", new Vector2(0.06f, 0.51f), new Vector2(0.38f, 0.91f), 13f, new Color(0.84f, 0.73f, 0.48f));
        ConfigureProgressText(EnsureText(panel.transform, "LevelValue"), $"Lv. {level}", new Vector2(0.05f, 0.16f), new Vector2(0.42f, 0.65f), 29f, new Color(1f, 0.92f, 0.68f));
        ConfigureProgressText(EnsureText(panel.transform, "ExperienceValue"), $"EXP {experience}/{nextExperience}", new Vector2(0.05f, 0.02f), new Vector2(0.48f, 0.20f), 11f, new Color(0.80f, 0.84f, 0.78f));
        ConfigureProgressText(EnsureText(panel.transform, "StagePointLabel"), "戦果", new Vector2(0.56f, 0.51f), new Vector2(0.94f, 0.91f), 13f, new Color(0.84f, 0.73f, 0.48f));
        ConfigureProgressText(EnsureText(panel.transform, "StagePointValue"), stagePoints.ToString(), new Vector2(0.54f, 0.15f), new Vector2(0.95f, 0.65f), 30f, new Color(1f, 0.92f, 0.68f));
    }

    private static void EnsureTopNavigationTiles(Transform topPanel)
    {
        Transform container = topPanel.Find("TopNavigationTiles");
        if (container == null)
        {
            var go = new GameObject("TopNavigationTiles", typeof(RectTransform));
            go.transform.SetParent(topPanel, false);
            container = go.transform;
        }

        Place(container.GetComponent<RectTransform>(),
            UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? new Vector2(0.04f, 0.08f) : new Vector2(0.64f, 0.18f),
            UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? new Vector2(0.96f, 0.44f) : new Vector2(0.94f, 0.42f));

        EnsureNavigationTile(container, "TopNavMarch", "行軍", "UI/ModernWafuu/BattleGarden", new Vector2(0f, 0f), new Vector2(0.48f, 1f), () => UIManager.Instance?.ShowStageSelect());
        EnsureNavigationTile(container, "TopNavSupply", "城下", "UI/ModernWafuu/BattleStoneCastle", new Vector2(0.52f, 0f), new Vector2(1f, 1f), () => UIManager.Instance?.ShowFacility());
    }

    private static void EnsureNavigationTile(Transform parent, string name, string label, string backgroundPath, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction onClick)
    {
        Transform existing = parent.Find(name);
        Button button;
        Image image;
        if (existing != null && existing.TryGetComponent(out button))
        {
            image = button.GetComponent<Image>();
        }
        else
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
            go.transform.SetParent(parent, false);
            button = go.GetComponent<Button>();
            image = go.GetComponent<Image>();
            CreateImageShade(go.transform, "Shade", new Color(0.05f, 0.04f, 0.02f, 0.28f));
            CreateActionText(go.transform).name = "Label";
        }

        image.sprite = GetSprite(backgroundPath);
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
        Place(button.GetComponent<RectTransform>(), min, max);
        ApplyButtonTreatment(button);

        var outline = button.GetComponent<Outline>();
        outline.effectDistance = new Vector2(2f, -2f);
        outline.effectColor = new Color(0.92f, 0.76f, 0.42f, 0.75f);
        var text = button.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
        if (text != null) text.text = label;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(onClick);
    }

    private static Image CreateImageShade(Transform parent, string name, Color color)
    {
        Image shade = EnsureImage(parent, name);
        ApplyFlatSurface(shade, color);
        shade.raycastTarget = false;
        Stretch(shade.rectTransform, Vector2.zero, Vector2.one);
        shade.transform.SetAsFirstSibling();
        return shade;
    }

    private static void ConfigureProgressText(TextMeshProUGUI text, string value, Vector2 min, Vector2 max, float size, Color color)
    {
        text.text = value;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(UnityUIRuntimeTheme.MinimumTextSize, size * 0.56f);
        text.fontSizeMax = size;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.outlineColor = new Color(0.07f, 0.05f, 0.03f, 0.94f);
        text.outlineWidth = 0.15f;
        Stretch(text.rectTransform, min, max);
    }

    private static TextMeshProUGUI CreateActionText(Transform parent)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(14f, 6f);
        rect.offsetMax = new Vector2(-14f, -6f);

        var text = go.GetComponent<TextMeshProUGUI>();
        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.enableAutoSizing = true;
        text.fontSizeMin = UnityUIRuntimeTheme.MinimumTextSize;
        text.fontSizeMax = 25f;
        text.color = new Color(1f, 0.96f, 0.82f);
        text.outlineColor = new Color(0.15f, 0.08f, 0.03f, 0.9f);
        text.outlineWidth = 0.16f;
        text.raycastTarget = false;
        return text;
    }

    public static StageData GetNextStage()
    {
        if (StageDatabase.Instance == null || GameManager.Instance == null)
        {
            return null;
        }

        int highestCleared = GameManager.Instance.GetHighestClearedStageId();
        foreach (StageData stage in StageDatabase.Instance.stages)
        {
            if (stage == null || GameManager.Instance.IsStageCleared(stage.stageId))
            {
                continue;
            }

            bool lockedByProgress = stage.stageId > highestCleared + 1;
            if (!lockedByProgress && GameManager.Instance.IsChapterUnlocked(stage.chapterId))
            {
                return stage;
            }
        }

        return null;
    }

    private static Color GetPortraitBattleTint(int chapterId)
    {
        if (chapterId >= 6)
        {
            return new Color(0.78f, 0.88f, 0.94f, 1f);
        }

        if (chapterId >= 3)
        {
            return new Color(0.88f, 0.80f, 0.66f, 1f);
        }

        return new Color(0.86f, 0.92f, 0.76f, 1f);
    }

    private static TextMeshProUGUI CreateHeaderText(Transform parent, string name, TextAlignmentOptions alignment, float fontSize, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = name == "Title" ? new Vector2(0f, 0f) : new Vector2(0.43f, 0f);
        rect.anchorMax = name == "Title" ? new Vector2(0.6f, 1f) : new Vector2(1f, 1f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var text = go.GetComponent<TextMeshProUGUI>();
        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
        text.alignment = alignment;
        text.enableAutoSizing = true;
        text.fontSizeMin = name == "Title" ? 18f : UnityUIRuntimeTheme.MinimumTextSize;
        text.fontSizeMax = fontSize;
        text.fontStyle = style;
        text.color = name == "Title"
            ? new Color(0.96f, 0.88f, 0.62f)
            : new Color(0.84f, 0.86f, 0.79f);
        text.outlineColor = new Color(0.10f, 0.09f, 0.07f, 0.9f);
        text.outlineWidth = 0.16f;
        text.raycastTarget = false;
        return text;
    }

    private static string BuildSubtitle()
    {
        if (PlayerInventory.Instance == null || GameManager.Instance == null)
        {
            return string.Empty;
        }

        return $"章の進行を確かめ、次の局へ";
    }

    private static Sprite GetSprite(string resourcePath)
    {
        if (sprites.TryGetValue(resourcePath, out Sprite sprite))
        {
            return sprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            return null;
        }

        sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        sprites[resourcePath] = sprite;
        return sprite;
    }

    private static Sprite GetFlatSprite()
    {
        if (flatSprite != null)
        {
            return flatSprite;
        }

        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        texture.Apply();
        texture.name = "ModernWafuuFlat";
        flatSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 100f);
        return flatSprite;
    }

    private readonly struct ScreenChromeSpec
    {
        public readonly string panelName;
        public readonly string title;

        public ScreenChromeSpec(string panelName, string title)
        {
            this.panelName = panelName;
            this.title = title;
        }
    }
}
