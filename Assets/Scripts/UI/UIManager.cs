using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum ProgressInfoKind
{
    PlayerLevel,
    PlayerExperience,
    StagePoints
}

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Panels")]
    [SerializeField] private GameObject StageSelectPanel;
    [SerializeField] private GameObject FormationPanel;
    [SerializeField] private GameObject BattlePanel;
    [SerializeField] private GameObject TopPanel;
    [SerializeField] private GameObject ResultPanel;
    [SerializeField] private GameObject FacilityPanel;

    private BattleManager battleManager;
    private GameObject settingsModal;
    private GameObject progressInfoModal;
    private TextMeshProUGUI settingsResetButtonText;
    private TextMeshProUGUI progressInfoTitle;
    private TextMeshProUGUI progressInfoBody;
    private float resetConfirmUntil = -1f;
    private const float ResetConfirmSeconds = 5f;

    private void Start()
    {
        ShowTop();
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;

        if (BattlePanel != null)
        {
            battleManager = BattlePanel.GetComponent<BattleManager>();
        }
    }

    private void HideAll()
    {
        SkillTooltipPresenter.HideAll();
        CloseSettings();
        CloseProgressInfo();
        if (StageSelectPanel != null) StageSelectPanel.SetActive(false);
        if (FormationPanel != null) FormationPanel.SetActive(false);
        if (BattlePanel != null) BattlePanel.SetActive(false);
        if (TopPanel != null) TopPanel.SetActive(false);
        if (ResultPanel != null) ResultPanel.SetActive(false);
        if (FacilityPanel != null) FacilityPanel.SetActive(false);
    }

    // === 画面遷移 ===
    public void ShowTop()
    {
        HideAll();
        ModernWafuuPresentation.SetGlobalStatusVisible(true);
        if (TopPanel != null) TopPanel.SetActive(true);
        ModernWafuuPresentation.ApplyScreenChrome();
        GameAudio.Instance.PlayBgm(GameBgm.Top);
        var tabs = TabManager.Instance != null ? TabManager.Instance : FindAnyObjectByType<TabManager>();
        tabs?.SetNavigationVisible(true);
        tabs?.HighlightTop();
    }

    public void ShowStageSelect()
    {
        HideAll();
        ModernWafuuPresentation.SetGlobalStatusVisible(true);
        GameAudio.Instance.PlayBgm(GameBgm.Top);
        if (StageSelectPanel != null)
        {
            StageSelectPanel.SetActive(true);
            var stageSelect = StageSelectPanel.GetComponent<StageSelectUI>();
            if (stageSelect != null) stageSelect.DisplayStages();
        }
        ModernWafuuPresentation.ApplyScreenChrome();
        var tabs = TabManager.Instance != null ? TabManager.Instance : FindAnyObjectByType<TabManager>();
        tabs?.SetNavigationVisible(true);
        tabs?.HighlightStage();
    }

    public void ShowFormation()
    {
        HideAll();
        ModernWafuuPresentation.SetGlobalStatusVisible(true);
        if (FormationPanel != null)
        {
            FormationPanel.SetActive(true);
        }
        ModernWafuuPresentation.ApplyScreenChrome();
        var tabs = TabManager.Instance != null ? TabManager.Instance : FindAnyObjectByType<TabManager>();
        tabs?.SetNavigationVisible(true);
    }

    public void ShowBattle()
    {
        HideAll();
        ModernWafuuPresentation.SetGlobalStatusVisible(false);
        if (BattlePanel != null) BattlePanel.SetActive(true);
        var tabs = TabManager.Instance != null ? TabManager.Instance : FindAnyObjectByType<TabManager>();
        tabs?.SetNavigationVisible(false);
    }

    public void ShowFacility()
    {
        HideAll();
        ModernWafuuPresentation.SetGlobalStatusVisible(true);
        GameAudio.Instance.PlayBgm(GameBgm.Top);
        if (FacilityPanel != null) FacilityPanel.SetActive(true);
        ModernWafuuPresentation.ApplyScreenChrome();
        var tabs = TabManager.Instance != null ? TabManager.Instance : FindAnyObjectByType<TabManager>();
        tabs?.SetNavigationVisible(true);
        tabs?.HighlightFacility();
    }

    public void ShowSettings()
    {
        EnsureSettingsModal();
        settingsModal.SetActive(true);
        settingsModal.transform.SetAsLastSibling();
    }

    public void ShowProgressInfo(ProgressInfoKind kind, RectTransform anchor = null)
    {
        if (anchor != null)
        {
            ContextualInfoTooltip.Show(anchor, GetProgressInfoTitle(kind), GetProgressInfoBody(kind));
            return;
        }

        EnsureProgressInfoModal();
        ConfigureProgressInfo(kind);
        progressInfoModal.SetActive(true);
        progressInfoModal.transform.SetAsLastSibling();
    }

    // === フォーメーションからバトル開始 ===
    public void StartBattleFromFormation()
    {
        FormationUI.Instance.RememberCurrentFormation();
        var allies = new System.Collections.Generic.List<CharacterData>(FormationUI.Instance.GetFormation());
        GameManager.Instance.StartStage(GameManager.Instance.GetSelectedStage(), allies);
    }

    // === ステージ開始 ===
    public void StartStage(StageData stage)
    {
        FormationUI.Instance.RememberCurrentFormation();
        var allies = new System.Collections.Generic.List<CharacterData>(FormationUI.Instance.GetFormation());

        HideAll();
        ModernWafuuPresentation.SetGlobalStatusVisible(false);
        if (BattlePanel != null) BattlePanel.SetActive(true);
        var tabs = TabManager.Instance != null ? TabManager.Instance : FindAnyObjectByType<TabManager>();
        tabs?.SetNavigationVisible(false);

        if (battleManager != null)
        {
            GameAudio.Instance.PlayBgm(stage != null && stage.isBossStage ? GameBgm.Boss : GameBgm.Battle);
            battleManager.StartBattle(allies, stage);
        }
        else
        {
            Debug.LogError("BattleManager が見つかりません。BattlePanelにアタッチされていますか？");
        }
    }

    private void EnsureSettingsModal()
    {
        if (settingsModal != null)
        {
            ConfigureModalRect(settingsModal.transform.Find("SettingsDialog") as RectTransform);
            return;
        }

        Canvas canvas = FindRootCanvas();
        if (canvas == null)
        {
            return;
        }

        settingsModal = CreateModalRoot(canvas.transform, "SettingsModal", CloseSettings);
        Image dialog = CreateDialog(settingsModal.transform, "SettingsDialog");
        Transform dialogTransform = dialog.transform;

        TextMeshProUGUI title = CreateText(dialogTransform, "Title", "設定", 28f, TextAlignmentOptions.Left, new Color(1f, 0.92f, 0.70f));
        Place(title.rectTransform, new Vector2(0.08f, 0.79f), new Vector2(0.72f, 0.93f));
        CreateCloseButton(dialogTransform, CloseSettings);

        CreateText(dialogTransform, "AudioCaption", "音量", 16f, TextAlignmentOptions.Left, new Color(0.88f, 0.79f, 0.58f));
        Place(dialogTransform.Find("AudioCaption").GetComponent<RectTransform>(), new Vector2(0.08f, 0.65f), new Vector2(0.90f, 0.74f));

        Toggle bgmToggle = CreateToggle(dialogTransform, "BgmToggle", "BGM", new Vector2(0.08f, 0.49f), new Vector2(0.92f, 0.62f));
        Toggle seToggle = CreateToggle(dialogTransform, "SeToggle", "SE", new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.47f));
        bgmToggle.onValueChanged.AddListener(GameAudio.Instance.SetBgmEnabled);
        seToggle.onValueChanged.AddListener(GameAudio.Instance.SetSfxEnabled);
        bgmToggle.SetIsOnWithoutNotify(GameAudio.Instance.BgmEnabled);
        seToggle.SetIsOnWithoutNotify(GameAudio.Instance.SfxEnabled);

        Button reset = CreateActionButton(dialogTransform, "ResetProgressButton", "進行データを削除", new Vector2(0.08f, 0.11f), new Vector2(0.92f, 0.27f), true);
        settingsResetButtonText = reset.GetComponentInChildren<TextMeshProUGUI>(true);
        reset.onClick.AddListener(HandleResetDataButtonClicked);
        SetResetDataButtonText("進行データを削除");

        settingsModal.SetActive(false);
    }

    private void HandleResetDataButtonClicked()
    {
        if (Time.unscaledTime > resetConfirmUntil)
        {
            resetConfirmUntil = Time.unscaledTime + ResetConfirmSeconds;
            SetResetDataButtonText("もう一度押すと進行データを削除");
            return;
        }

        resetConfirmUntil = -1f;
        PlayerInventory.Instance?.ResetProgress();
        FacilityManager.Instance?.ResetProgress();
        ResearchBondService.Instance.ResetProgress();
        GameManager.Instance?.ResetProgress();
        GameManager.Instance?.ResetRuntimeFacilityEffects();
        SetResetDataButtonText("削除しました");
        CloseSettings();
        ShowTop();
        Debug.Log("[UIManager] 進行データを削除しました。");
    }

    private void SetResetDataButtonText(string label)
    {
        if (settingsResetButtonText == null)
        {
            return;
        }

        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(settingsResetButtonText);
        settingsResetButtonText.text = label;
        settingsResetButtonText.enableAutoSizing = true;
        settingsResetButtonText.fontSizeMin = UnityUIRuntimeTheme.MinimumTextSize;
        settingsResetButtonText.fontSizeMax = 18f;
        settingsResetButtonText.alignment = TextAlignmentOptions.Center;
        settingsResetButtonText.color = new Color(1f, 0.95f, 0.82f);
        settingsResetButtonText.raycastTarget = false;
    }

    private void CloseSettings()
    {
        if (settingsModal != null) settingsModal.SetActive(false);
    }

    private void EnsureProgressInfoModal()
    {
        if (progressInfoModal != null)
        {
            ConfigureModalRect(progressInfoModal.transform.Find("ProgressInfoDialog") as RectTransform);
            return;
        }

        Canvas canvas = FindRootCanvas();
        if (canvas == null) return;

        progressInfoModal = CreateModalRoot(canvas.transform, "ProgressInfoModal", CloseProgressInfo);
        Image dialog = CreateDialog(progressInfoModal.transform, "ProgressInfoDialog");
        Transform dialogTransform = dialog.transform;
        progressInfoTitle = CreateText(dialogTransform, "Title", string.Empty, 28f, TextAlignmentOptions.Left, new Color(1f, 0.92f, 0.70f));
        Place(progressInfoTitle.rectTransform, new Vector2(0.09f, 0.72f), new Vector2(0.80f, 0.90f));
        progressInfoBody = CreateText(dialogTransform, "Body", string.Empty, 20f, TextAlignmentOptions.TopLeft, new Color(0.97f, 0.91f, 0.76f));
        progressInfoBody.enableAutoSizing = true;
        progressInfoBody.fontSizeMin = 14f;
        progressInfoBody.fontSizeMax = 21f;
        progressInfoBody.textWrappingMode = TextWrappingModes.Normal;
        Place(progressInfoBody.rectTransform, new Vector2(0.09f, 0.20f), new Vector2(0.91f, 0.67f));
        CreateCloseButton(dialogTransform, CloseProgressInfo);
        progressInfoModal.SetActive(false);
    }

    private void ConfigureProgressInfo(ProgressInfoKind kind)
    {
        if (progressInfoTitle == null || progressInfoBody == null) return;

        progressInfoTitle.text = GetProgressInfoTitle(kind);
        progressInfoBody.text = GetProgressInfoBody(kind);
    }

    private static string GetProgressInfoTitle(ProgressInfoKind kind)
    {
        switch (kind)
        {
            case ProgressInfoKind.PlayerLevel:
                return "軍師レベル";
            case ProgressInfoKind.PlayerExperience:
                return "軍師経験";
            default:
                return "戦果ポイント";
        }
    }

    private static string GetProgressInfoBody(ProgressInfoKind kind)
    {
        switch (kind)
        {
            case ProgressInfoKind.PlayerLevel:
                return "戦闘で得る経験値により上がる、軍師の采配力です。解放済みの駒全体で共有され、レベルが上がるほど強い局に挑めます。";
            case ProgressInfoKind.PlayerExperience:
                return "局の勝利と軍勢の稽古で獲得します。必要量に達すると軍師レベルが上がり、解放済みの駒がまとめて成長します。";
            default:
                return "局の勝利報酬です。城下で施設を解放・強化し、編成枠、経験値、縁、戦闘速度などを整えます。";
        }
    }

    private void CloseProgressInfo()
    {
        if (progressInfoModal != null) progressInfoModal.SetActive(false);
    }

    private static Canvas FindRootCanvas()
    {
        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (canvas != null
                && canvas.isRootCanvas
                && canvas.transform.parent == null
                && canvas.renderMode != RenderMode.WorldSpace)
            {
                return canvas;
            }
        }

        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (canvas != null && canvas.isRootCanvas)
            {
                return canvas;
            }
        }

        return null;
    }

    private static GameObject CreateModalRoot(Transform parent, string name, UnityEngine.Events.UnityAction dismiss)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Canvas), typeof(GraphicRaycaster));
        root.transform.SetParent(parent, false);
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = root.GetComponent<Image>();
        ModernWafuuPresentation.ApplyFlatSurface(image, new Color(0.03f, 0.03f, 0.025f, 0.58f));
        image.raycastTarget = true;
        root.GetComponent<Button>().onClick.AddListener(dismiss);
        var modalCanvas = root.GetComponent<Canvas>();
        modalCanvas.overrideSorting = true;
        modalCanvas.sortingOrder = 1000;
        return root;
    }

    private static Image CreateDialog(Transform parent, string name)
    {
        var dialog = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        dialog.transform.SetParent(parent, false);
        Image image = dialog.GetComponent<Image>();
        ModernWafuuPresentation.ApplyFlatSurface(image, new Color(0.13f, 0.10f, 0.065f, 0.98f));
        image.raycastTarget = true;
        var outline = dialog.GetComponent<Outline>();
        outline.effectColor = new Color(0.92f, 0.71f, 0.34f, 0.86f);
        outline.effectDistance = new Vector2(2f, -2f);
        ConfigureModalRect(dialog.GetComponent<RectTransform>());
        return image;
    }

    private static void ConfigureModalRect(RectTransform rect)
    {
        if (rect == null) return;
        bool portrait = UnityUIRuntimeTheme.IsPortraitNarrowScreen();
        rect.anchorMin = portrait ? new Vector2(0.07f, 0.20f) : new Vector2(0.32f, 0.24f);
        rect.anchorMax = portrait ? new Vector2(0.93f, 0.80f) : new Vector2(0.68f, 0.76f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string content, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateActionButton(Transform parent, string name, string label, Vector2 min, Vector2 max, bool danger = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        Place(rect, min, max);
        var image = go.GetComponent<Image>();
        ModernWafuuPresentation.ApplyFlatSurface(image, danger ? new Color(0.42f, 0.12f, 0.10f, 1f) : new Color(0.34f, 0.29f, 0.20f, 1f));
        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        var text = CreateText(go.transform, "Text", label, 18f, TextAlignmentOptions.Center, new Color(1f, 0.95f, 0.82f));
        Place(text.rectTransform, Vector2.zero, Vector2.one);
        return button;
    }

    private static Button CreateCloseButton(Transform parent, UnityEngine.Events.UnityAction action)
    {
        Button button = CreateActionButton(parent, "CloseButton", "閉じる", new Vector2(0.74f, 0.76f), new Vector2(0.92f, 0.91f));
        button.onClick.AddListener(action);
        return button;
    }

    private static Toggle CreateToggle(Transform parent, string name, string label, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Toggle));
        go.transform.SetParent(parent, false);
        Place(go.GetComponent<RectTransform>(), min, max);
        var background = go.GetComponent<Image>();
        ModernWafuuPresentation.ApplyFlatSurface(background, new Color(0.24f, 0.19f, 0.12f, 1f));

        var check = new GameObject("Check", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        check.transform.SetParent(go.transform, false);
        var checkImage = check.GetComponent<Image>();
        ModernWafuuPresentation.ApplyFlatSurface(checkImage, new Color(0.78f, 0.60f, 0.23f, 1f));
        Place(check.GetComponent<RectTransform>(), new Vector2(0.05f, 0.22f), new Vector2(0.16f, 0.78f));

        var labelText = CreateText(go.transform, "Label", label, 19f, TextAlignmentOptions.Left, new Color(1f, 0.94f, 0.76f));
        Place(labelText.rectTransform, new Vector2(0.21f, 0.08f), new Vector2(0.94f, 0.92f));

        var toggle = go.GetComponent<Toggle>();
        toggle.targetGraphic = background;
        toggle.graphic = checkImage;
        return toggle;
    }

    private static void Place(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

}
