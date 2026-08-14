using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageButtonUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stageNameText;
    private StageData stageData;
    private TextMeshProUGUI clearIconText;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    public void SetStage(StageData data)
    {
        stageData = data;
        EnsureStageNameText();
        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(stageNameText);
        stageNameText.gameObject.SetActive(true);
        stageNameText.enabled = true;
        stageNameText.enableAutoSizing = false;
        stageNameText.fontSize = 20f;
        stageNameText.fontSizeMin = 12f;
        stageNameText.fontSizeMax = 22f;
        stageNameText.alignment = TMPro.TextAlignmentOptions.Center;
        stageNameText.textWrappingMode = TMPro.TextWrappingModes.Normal;
        stageNameText.overflowMode = TMPro.TextOverflowModes.Truncate;
        stageNameText.margin = new Vector4(8f, 2f, 36f, 2f);
        var rect = stageNameText.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        SetLocked(false);
    }

    public void SetState(bool locked, bool cleared)
    {
        EnsureStageNameText();
        if (stageData == null || stageNameText == null)
        {
            return;
        }

        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(stageNameText);
        EnsureClearIconText();
        if (locked)
        {
            stageNameText.text = $"第{stageData.chapterId}章 未解放\n{stageData.stageName}";
            stageNameText.color = new Color(0.78f, 0.70f, 0.60f, 1f);
            SetClearIconVisible(false);
        }
        else if (cleared)
        {
            stageNameText.text = stageData.stageName;
            stageNameText.color = new Color(1f, 0.95f, 0.82f, 1f);
            SetClearIconVisible(true);
        }
        else
        {
            stageNameText.text = stageData.stageName;
            stageNameText.color = new Color(1f, 0.95f, 0.82f, 1f);
            SetClearIconVisible(false);
        }
        stageNameText.ForceMeshUpdate();
    }

    public void SetLocked(bool locked)
    {
        SetState(locked, false);
    }

    private void EnsureStageNameText()
    {
        if (stageNameText == null)
        {
            stageNameText = GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    private void EnsureClearIconText()
    {
        if (clearIconText != null)
        {
            return;
        }

        var existing = transform.Find("ClearIcon");
        if (existing != null)
        {
            clearIconText = existing.GetComponent<TextMeshProUGUI>();
        }

        if (clearIconText == null)
        {
            var iconObject = new GameObject("ClearIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            iconObject.transform.SetParent(transform, false);
            clearIconText = iconObject.GetComponent<TextMeshProUGUI>();
        }

        var rect = clearIconText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(32f, 0f);
        rect.anchoredPosition = new Vector2(-8f, 0f);

        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(clearIconText);
        clearIconText.text = "✓";
        clearIconText.alignment = TextAlignmentOptions.Center;
        clearIconText.fontSize = 24f;
        clearIconText.enableAutoSizing = true;
        clearIconText.fontSizeMin = 16f;
        clearIconText.fontSizeMax = 26f;
        clearIconText.color = new Color(0.72f, 1f, 0.72f, 1f);
        clearIconText.raycastTarget = false;
        clearIconText.gameObject.SetActive(false);
    }

    private void SetClearIconVisible(bool visible)
    {
        if (clearIconText != null)
        {
            clearIconText.gameObject.SetActive(visible);
        }
    }

    private void OnClick()
    {
        if (stageData != null)
        {
            GameManager.Instance.SetSelectedStage(stageData);
            UIManager.Instance.ShowFormation();
        }
    }
}
