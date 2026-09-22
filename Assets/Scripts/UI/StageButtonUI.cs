using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageButtonUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stageNameText;
    private StageData stageData;
    private TextMeshProUGUI statusText;
    private TextMeshProUGUI detailText;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    public void SetStage(StageData data)
    {
        stageData = data;
        EnsureStageNameText();
        if (stageNameText == null)
        {
            return;
        }

        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(stageNameText);
        stageNameText.gameObject.SetActive(true);
        stageNameText.enabled = true;
        stageNameText.enableAutoSizing = false;
        stageNameText.fontSize = 21f;
        stageNameText.fontSizeMin = 15f;
        stageNameText.fontSizeMax = 24f;
        stageNameText.alignment = TextAlignmentOptions.Left;
        stageNameText.textWrappingMode = TextWrappingModes.Normal;
        stageNameText.overflowMode = TextOverflowModes.Truncate;

        RectTransform titleRect = stageNameText.GetComponent<RectTransform>();
        if (titleRect != null)
        {
            titleRect.anchorMin = new Vector2(0f, 0.62f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(12f, -3f);
            titleRect.offsetMax = new Vector2(-54f, -4f);
        }

        EnsureDetailText();
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
        RefreshDetails(cleared);
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

        Transform existing = transform.Find("StageStatus") ?? transform.Find("ClearIcon");
        if (existing != null)
        {
            statusText = existing.GetComponent<TextMeshProUGUI>();
        }

        if (statusText == null)
        {
            GameObject iconObject = new GameObject("StageStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            iconObject.transform.SetParent(transform, false);
            statusText = iconObject.GetComponent<TextMeshProUGUI>();
        }

        statusText.gameObject.name = "StageStatus";
        RectTransform rect = statusText.GetComponent<RectTransform>();
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

    private void EnsureDetailText()
    {
        if (detailText != null)
        {
            return;
        }

        Transform existing = transform.Find("StageDetails");
        if (existing != null)
        {
            detailText = existing.GetComponent<TextMeshProUGUI>();
        }

        if (detailText == null)
        {
            GameObject detailsObject = new GameObject("StageDetails", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            detailsObject.transform.SetParent(transform, false);
            detailText = detailsObject.GetComponent<TextMeshProUGUI>();
        }

        RectTransform rect = detailText.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0.64f);
        rect.offsetMin = new Vector2(12f, 5f);
        rect.offsetMax = new Vector2(-54f, -2f);

        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(detailText);
        detailText.fontSize = 16f;
        detailText.enableAutoSizing = true;
        detailText.fontSizeMin = 13f;
        detailText.fontSizeMax = 17f;
        detailText.alignment = TextAlignmentOptions.TopLeft;
        detailText.textWrappingMode = TextWrappingModes.Normal;
        detailText.overflowMode = TextOverflowModes.Ellipsis;
        detailText.raycastTarget = false;
    }

    private void RefreshDetails(bool cleared)
    {
        EnsureDetailText();
        if (stageData == null || detailText == null)
        {
            return;
        }

        int stagePoints = GameManager.Instance != null
            ? GameManager.Instance.GetEffectiveStagePointReward(stageData.rewardStagePoints)
            : stageData.rewardStagePoints;
        int experience = PlayerInventory.Instance != null
            ? PlayerInventory.Instance.GetEffectiveBattleExperienceReward()
            : PlayerInventory.BaseBattleExperience;
        string rewardLine = $"敵 Lv.{stageData.enemyLevel}  |  SP +{stagePoints}  |  Player EXP +{experience}";

        if (!cleared)
        {
            detailText.text = rewardLine;
            detailText.color = new Color(0.82f, 0.87f, 0.90f, 1f);
            return;
        }

        string enemies = FormatCharacters(stageData.enemyPool);
        string reinforcements = FormatCharacters(stageData.reinforcementEnemy);
        detailText.text = string.IsNullOrEmpty(reinforcements)
            ? $"{rewardLine}\n敵編成: {enemies}"
            : $"{rewardLine}\n敵編成: {enemies} / 増援: {reinforcements}";
        detailText.color = new Color(0.80f, 0.92f, 0.86f, 1f);
    }

    private static string FormatCharacters(IEnumerable<CharacterData> characters)
    {
        if (characters == null)
        {
            return string.Empty;
        }

        return string.Join("・", characters
            .Where(character => character != null)
            .Select(character => character.characterName));
    }

    private void SetStatus(string text, Color color, bool visible)
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = text;
        statusText.color = color;
        statusText.gameObject.SetActive(visible);
    }

    private void OnClick()
    {
        if (stageData == null)
        {
            return;
        }

        GameManager.Instance.SetSelectedStage(stageData);
        UIManager.Instance.ShowFormation();
    }
}
