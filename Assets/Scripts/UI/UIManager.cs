using UnityEngine;

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

    private void Start()
    {
        // 起動時にタブハイライトをTopに強制
        TabManager.Instance?.HighlightTop();
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
}
