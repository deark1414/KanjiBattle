using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageButtonUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stageNameText;
    private StageData stageData;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    public void SetStage(StageData data)
    {
        stageData = data;
        UnityUIRuntimeTheme.ApplyJapaneseFont(stageNameText);
        stageNameText.enableAutoSizing = true;
        stageNameText.fontSizeMin = 12f;
        stageNameText.fontSizeMax = 22f;
        stageNameText.textWrappingMode = TMPro.TextWrappingModes.Normal;
        stageNameText.overflowMode = TMPro.TextOverflowModes.Truncate;
        stageNameText.margin = new Vector4(8f, 2f, 8f, 2f);
        SetLocked(false);
    }

    public void SetLocked(bool locked)
    {
        if (stageData == null || stageNameText == null)
        {
            return;
        }

        stageNameText.text = locked
            ? $"第{stageData.chapterId}章 未解放\n{stageData.stageName}"
            : stageData.stageName;
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
