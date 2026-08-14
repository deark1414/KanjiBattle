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
    [SerializeField] private Color selectedColor = new Color(1f, 0.94f, 0.74f);
    [SerializeField] private Color normalColor = Color.white;

    private Button currentTab;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        HighlightTop();
    }

    private void ResetButtonColor(Button button)
    {
        if (button == null) return;

        var colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = new Color(1f, 0.94f, 0.78f);
        colors.pressedColor = new Color(0.82f, 0.74f, 0.62f);
        colors.selectedColor = normalColor;
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);
        colors.colorMultiplier = 1f;
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
            colors.selectedColor = selectedColor;
            colors.highlightedColor = selectedColor;
            colors.colorMultiplier = 1f;
            currentTab.colors = colors;
        }
    }

    // === タブ切り替え ===
    public void ShowHome()
    {
        var uiManager = UIManager.Instance != null ? UIManager.Instance : FindAnyObjectByType<UIManager>();
        if (uiManager != null) uiManager.ShowTop();
    }

    public void ShowBattle()
    {
        var uiManager = UIManager.Instance != null ? UIManager.Instance : FindAnyObjectByType<UIManager>();
        if (uiManager != null) uiManager.ShowStageSelect();
    }

    public void ShowFacility()
    {
        var uiManager = UIManager.Instance != null ? UIManager.Instance : FindAnyObjectByType<UIManager>();
        if (uiManager != null) uiManager.ShowFacility();
    }

    // 外部から直接呼べるショートカット
    public void HighlightTop() => SetActiveTab(homeButton);
    public void HighlightStage() => SetActiveTab(battleButton);
    public void HighlightFacility() => SetActiveTab(facilityButton);
}