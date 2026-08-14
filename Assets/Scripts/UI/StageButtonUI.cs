using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageButtonUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stageNameText;
    private StageData stageData;
    private TextMeshProUGUI statusText;

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
        stageNameText.margin = new Vector4(8f, 2f, 52f, 2f);
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
        EnsureStatusText();
        stageNameText.text = stageData.stageName;
        if (locked)
        {
            stageNameText.color = new Color(0.78f, 0.70f, 0.60f, 1f);
            SetStatus("未", new Color(0.78f, 0.70f, 0.60f, 1f), true);
        }
        else if (cleared)
        {
            stageNameText.color = new Color(1f, 0.95f, 0.82f, 1f);
            SetStatus("✓", new Color(0.72f, 1f, 0.72f, 1f), true);
        }
        else
        {
            stageNameText.color = new Color(1f, 0.95f, 0.82f, 1f);
            SetStatus(string.Empty, Color.white, false);
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

    private void EnsureStatusText()
    {
        if (statusText != null)
        {
            return;
        }

        var existing = transform.Find("StageStatus");
        if (existing == null)
        {
            existing = transform.Find("ClearIcon");
        }
        if (existing != null)
        {
            statusText = existing.GetComponent<TextMeshProUGUI>();
        }

        if (statusText == null)
        {
            var iconObject = new GameObject("StageStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            iconObject.transform.SetParent(transform, false);
            statusText = iconObject.GetComponent<TextMeshProUGUI>();
        }
        statusText.gameObject.name = "StageStatus";

        var rect = statusText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(44f, 0f);
        rect.anchoredPosition = new Vector2(-8f, 0f);

        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(statusText);
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.fontSize = 22f;
        statusText.enableAutoSizing = true;
        statusText.fontSizeMin = 14f;
        statusText.fontSizeMax = 24f;
        statusText.raycastTarget = false;
        statusText.gameObject.SetActive(false);
    }

    private void SetStatus(string text, Color color, bool visible)
    {
        if (statusText != null)
        {
            statusText.text = text;
            statusText.color = color;
            statusText.gameObject.SetActive(visible);
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
