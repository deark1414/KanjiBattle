using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    private Button resetDataButton;
    private TextMeshProUGUI resetDataButtonText;
    private float resetConfirmUntil = -1f;
    private const float ResetConfirmSeconds = 5f;

    private void Start()
    {
        EnsureResetDataButton();
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
        EnsureResetDataButton();
        HideAll();
        if (TopPanel != null) TopPanel.SetActive(true);
        var tabs = TabManager.Instance != null ? TabManager.Instance : FindAnyObjectByType<TabManager>();
        tabs?.HighlightTop();
    }

    public void ShowStageSelect()
    {
        HideAll();
        if (StageSelectPanel != null)
        {
            StageSelectPanel.SetActive(true);
            var stageSelect = StageSelectPanel.GetComponent<StageSelectUI>();
            if (stageSelect != null) stageSelect.DisplayStages();
        }
        var tabs = TabManager.Instance != null ? TabManager.Instance : FindAnyObjectByType<TabManager>();
        tabs?.HighlightStage();
    }

    public void ShowFormation()
    {
        HideAll();
        if (FormationPanel != null)
        {
            FormationPanel.SetActive(true);
        }
    }

    public void ShowBattle()
    {
        HideAll();
        if (BattlePanel != null) BattlePanel.SetActive(true);
    }

    public void ShowFacility()
    {
        HideAll();
        if (FacilityPanel != null) FacilityPanel.SetActive(true);
        var tabs = TabManager.Instance != null ? TabManager.Instance : FindAnyObjectByType<TabManager>();
        tabs?.HighlightFacility();
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
        if (BattlePanel != null) BattlePanel.SetActive(true);

        if (battleManager != null)
        {
            battleManager.StartBattle(allies, stage);
        }
        else
        {
            Debug.LogError("BattleManager が見つかりません。BattlePanelにアタッチされていますか？");
        }
    }

    private void EnsureResetDataButton()
    {
        if (TopPanel == null || resetDataButton != null)
        {
            return;
        }

        var existing = TopPanel.transform.Find("ResetDataButton");
        if (existing != null && existing.TryGetComponent(out resetDataButton))
        {
            resetDataButtonText = resetDataButton.GetComponentInChildren<TextMeshProUGUI>();
            resetDataButton.onClick.RemoveAllListeners();
            resetDataButton.onClick.AddListener(HandleResetDataButtonClicked);
            SetResetDataButtonText("データ削除");
            return;
        }

        var go = new GameObject("ResetDataButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(TopPanel.transform, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.72f, 0.04f);
        rect.anchorMax = new Vector2(0.96f, 0.12f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = go.GetComponent<Image>();
        image.sprite = Resources.Load<Sprite>("Kenney/UIRPG/PNG/buttonLong_brown");
        image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = image.sprite != null ? Color.white : new Color(0.58f, 0.41f, 0.24f, 1f);

        resetDataButton = go.GetComponent<Button>();
        resetDataButton.onClick.AddListener(HandleResetDataButtonClicked);

        var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(go.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 2f);
        textRect.offsetMax = new Vector2(-8f, -2f);

        resetDataButtonText = textObject.GetComponent<TextMeshProUGUI>();
        SetResetDataButtonText("データ削除");
    }

    private void HandleResetDataButtonClicked()
    {
        if (Time.unscaledTime > resetConfirmUntil)
        {
            resetConfirmUntil = Time.unscaledTime + ResetConfirmSeconds;
            SetResetDataButtonText("もう一度で削除");
            return;
        }

        resetConfirmUntil = -1f;
        PlayerInventory.Instance?.ResetProgress();
        FacilityManager.Instance?.ResetProgress();
        GameManager.Instance?.ResetProgress();
        GameManager.Instance?.ResetRuntimeFacilityEffects();
        GameManager.Instance?.UpdateProduction();
        SetResetDataButtonText("削除しました");
        ShowTop();
        Debug.Log("[UIManager] 進行データを削除しました。");
    }

    private void SetResetDataButtonText(string label)
    {
        if (resetDataButtonText == null)
        {
            return;
        }

        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(resetDataButtonText);
        resetDataButtonText.text = label;
        resetDataButtonText.enableAutoSizing = true;
        resetDataButtonText.fontSizeMin = 10f;
        resetDataButtonText.fontSizeMax = 18f;
        resetDataButtonText.alignment = TextAlignmentOptions.Center;
        resetDataButtonText.color = new Color(1f, 0.95f, 0.82f);
        resetDataButtonText.raycastTarget = false;
    }
}
