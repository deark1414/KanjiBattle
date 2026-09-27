using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TabManager : MonoBehaviour
{
    public static TabManager Instance;

    [Header("Tab Buttons")]
    [SerializeField] private Button homeButton;
    [SerializeField] private Button battleButton;
    [SerializeField] private Button facilityButton;

    [Header("Tab Colors")]
    [SerializeField] private Color selectedColor = new Color(0.73f, 0.30f, 0.16f);
    [SerializeField] private Color normalColor = Color.white;

    private Button currentTab;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        EnsureNavigationLayer();
        EnsureButtonListeners();
        ApplyLabels();
        HighlightTop();
    }

    private void EnsureNavigationLayer()
    {
        Transform navigation = homeButton != null ? homeButton.transform.parent : null;
        if (navigation == null) return;

        var canvas = navigation.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = navigation.gameObject.AddComponent<Canvas>();
        }
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        if (navigation.GetComponent<GraphicRaycaster>() == null)
        {
            navigation.gameObject.AddComponent<GraphicRaycaster>();
        }

        navigation.SetAsLastSibling();
    }

    private void EnsureButtonListeners()
    {
        if (homeButton != null) homeButton.onClick.AddListener(ShowHome);
        if (battleButton != null) battleButton.onClick.AddListener(ShowBattle);
        if (facilityButton != null) facilityButton.onClick.AddListener(ShowFacility);
    }

    private void ApplyLabels()
    {
        SetButtonLabel(homeButton, "軍勢");
        SetButtonLabel(battleButton, "行軍");
        SetButtonLabel(facilityButton, "城下");
    }

    private static void SetButtonLabel(Button button, string label)
    {
        if (button == null)
        {
            return;
        }

        TextMeshProUGUI text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text == null)
        {
            return;
        }

        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
        text.text = label;
        text.enableAutoSizing = true;
        text.fontSizeMin = UnityUIRuntimeTheme.MinimumTextSize;
        text.fontSizeMax = 18f;
        text.color = new Color(1f, 0.94f, 0.78f, 1f);
        text.outlineColor = new Color(0.08f, 0.05f, 0.02f, 1f);
        text.outlineWidth = 0.18f;
    }

    private void ResetButtonColor(Button button)
    {
        if (button == null) return;

        ModernWafuuPresentation.ApplyButtonTreatment(button);

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
            var colors = currentTab.colors;
            colors.normalColor = selectedColor;
            colors.selectedColor = selectedColor;
            colors.highlightedColor = selectedColor;
            colors.colorMultiplier = 1f;
            currentTab.colors = colors;

            Image image = currentTab.targetGraphic as Image ?? currentTab.GetComponent<Image>();
            ModernWafuuPresentation.ApplyFlatSurface(image, new Color(0.48f, 0.20f, 0.12f, 0.98f));
            var outline = currentTab.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = new Color(0.96f, 0.72f, 0.34f, 0.88f);
            }
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

    public void SetNavigationVisible(bool visible)
    {
        Transform navigation = homeButton != null ? homeButton.transform.parent : null;
        if (navigation != null)
        {
            navigation.gameObject.SetActive(visible);
            if (visible)
            {
                EnsureNavigationLayer();
            }
        }
    }

    // 外部から直接呼べるショートカット
    public void HighlightTop() => SetActiveTab(homeButton);
    public void HighlightStage() => SetActiveTab(battleButton);
    public void HighlightFacility() => SetActiveTab(facilityButton);
}
