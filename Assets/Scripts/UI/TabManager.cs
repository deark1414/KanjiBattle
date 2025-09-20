using UnityEngine;
using UnityEngine.UI;

public class TabManager : MonoBehaviour
{
    public static TabManager Instance;

    [Header("Tab Buttons")]
    [SerializeField] private Button homeButton;
    [SerializeField] private Button battleButton;
    [SerializeField] private Button facilityButton;

    [Header("Tab Colors")]
    [SerializeField] private Color selectedColor = Color.white;
    [SerializeField] private Color normalColor = Color.gray;

    private Button currentTab;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        HighlightTop();
    }

    private void ResetButtonColor(Button button)
    {
        var colors = button.colors;
        colors.normalColor = normalColor;
        button.colors = colors;
    }

    private void ResetTabColors()
    {
        ResetButtonColor(homeButton);
        ResetButtonColor(battleButton);
        ResetButtonColor(facilityButton);
    }

    public void SetActiveTab(Button targetTab)
    {
        ResetTabColors();

        currentTab = targetTab;
        if (currentTab != null)
        {
            // 新しいタブをハイライト
            var colors = currentTab.colors;
            colors.normalColor = selectedColor;
            currentTab.colors = colors;
        }
    }

    // === タブ切り替え ===
    public void ShowHome()
    {
        UIManager.Instance.ShowTop();
    }

    public void ShowBattle()
    {
        UIManager.Instance.ShowStageSelect();
    }

    public void ShowFacility()
    {
        UIManager.Instance.ShowFacility();
    }

    // 外部から直接呼べるショートカット
    public void HighlightTop() => SetActiveTab(homeButton);
    public void HighlightStage() => SetActiveTab(battleButton);
    public void HighlightFacility() => SetActiveTab(facilityButton);
}