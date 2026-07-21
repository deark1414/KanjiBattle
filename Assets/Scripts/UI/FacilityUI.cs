using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class FacilityUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI buttonText;

    private static readonly Color UnlockedCardColor = Color.white;
    private static readonly Color LockedCardColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);
    private static readonly Color MaxCardColor = Color.white;
    private static readonly Color ReadyOutlineColor = new Color(1f, 0.78f, 0.30f, 0.95f);
    private static readonly Color IdleOutlineColor = new Color(0.18f, 0.14f, 0.10f, 0.55f);
    private static readonly Color TitleColor = new Color(1f, 0.96f, 0.82f, 1f);
    private static readonly Color BodyColor = new Color(0.96f, 0.91f, 0.76f, 1f);
    private static readonly Color ReadyTextColor = new Color(0.68f, 1f, 0.76f, 1f);
    private static readonly Color NeedTextColor = new Color(1f, 0.72f, 0.48f, 1f);
    private static readonly Color LockedTextColor = new Color(0.64f, 0.61f, 0.56f, 1f);
    private static readonly Color MaxTextColor = new Color(0.78f, 0.90f, 1f, 1f);

    private FacilityData facility;
    private Image backgroundImage;
    private Button button;
    private Outline stateOutline;
    private float nextRefreshTime;
    private static GameObject tooltip;
    private static TextMeshProUGUI tooltipText;

    private enum FacilityActionState
    {
        UnlockReady,
        UnlockBlocked,
        UpgradeReady,
        UpgradeBlocked,
        CapReady,
        CapBlocked,
        Maxed
    }

    public void Setup(FacilityData facilityData)
    {
        facility = facilityData;
        button = GetComponent<Button>();
        backgroundImage = GetComponent<Image>();
        stateOutline = GetComponent<Outline>();
        if (stateOutline == null)
        {
            stateOutline = gameObject.AddComponent<Outline>();
        }
        stateOutline.effectDistance = new Vector2(3f, -3f);
        ApplyLayout();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClickAction);
        }

        Refresh();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.unscaledTime + 0.5f;
        Refresh();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip(GetDescription(facility));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    private void OnClickAction()
    {
        if (facility == null || FacilityManager.Instance == null)
        {
            return;
        }

        if (!FacilityManager.Instance.IsUnlocked(facility))
        {
            if (FacilityManager.Instance.Unlock(facility))
            {
                Refresh();
            }
        }
        else if (FacilityManager.Instance.IsMaxLevel(facility))
        {
            if (FacilityManager.Instance.CanUpgradeLevelCap(facility) && FacilityManager.Instance.UpgradeLevelCap(facility))
            {
                Refresh();
            }
        }
        else if (FacilityManager.Instance.Upgrade(facility))
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        if (facility == null || FacilityManager.Instance == null)
        {
            return;
        }

        FacilityActionState state = GetActionState();
        bool isUnlocked = FacilityManager.Instance.IsUnlocked(facility);
        int level = FacilityManager.Instance.GetLevel(facility);
        int maxLevel = FacilityManager.Instance.GetCurrentFacilityMaxLevel(facility);

        nameText.text = facility.facilityName;
        levelText.text = GetLevelBadge(isUnlocked, level, maxLevel);

        switch (state)
        {
            case FacilityActionState.UnlockReady:
                costText.text = $"未解放 / 解放可能  {FacilityManager.Instance.GetUnlockCost(facility)}SP";
                buttonText.text = "解放する";
                ApplyVisualState(LockedCardColor, ReadyTextColor, new Color(0.54f, 0.38f, 0.20f, 1f), true, true);
                break;
            case FacilityActionState.UnlockBlocked:
                costText.text = $"未解放 / {GetUnlockRequirementText()}";
                buttonText.text = "未解放";
                ApplyVisualState(LockedCardColor, LockedTextColor, new Color(0.26f, 0.26f, 0.26f, 1f), false, false);
                break;
            case FacilityActionState.UpgradeReady:
                costText.text = $"{GetEffectLabel()} / 強化可能  {FacilityManager.Instance.GetUpgradeCost(facility)}G";
                buttonText.text = "強化";
                ApplyVisualState(UnlockedCardColor, ReadyTextColor, new Color(0.64f, 0.42f, 0.20f, 1f), true, true);
                break;
            case FacilityActionState.UpgradeBlocked:
                costText.text = $"{GetEffectLabel()} / Gold不足  {FacilityManager.Instance.GetUpgradeCost(facility)}G";
                buttonText.text = "強化待ち";
                ApplyVisualState(UnlockedCardColor, NeedTextColor, new Color(0.42f, 0.31f, 0.22f, 1f), false, false);
                break;
            case FacilityActionState.CapReady:
                costText.text = GetCapRequirementText("上限到達 / 解放可能");
                buttonText.text = "上限解放";
                ApplyVisualState(MaxCardColor, ReadyTextColor, new Color(0.58f, 0.38f, 0.20f, 1f), true, true);
                break;
            case FacilityActionState.CapBlocked:
                costText.text = GetCapRequirementText("上限到達 / 次条件");
                buttonText.text = "上限待ち";
                ApplyVisualState(MaxCardColor, NeedTextColor, new Color(0.38f, 0.30f, 0.23f, 1f), false, false);
                break;
            default:
                costText.text = IsUnlockStyleFacility() ? "開放効果 / 適用済み" : "最大強化済み";
                buttonText.text = IsUnlockStyleFacility() ? "解放済" : "MAX";
                ApplyVisualState(MaxCardColor, MaxTextColor, new Color(0.40f, 0.30f, 0.22f, 1f), false, false);
                break;
        }
    }
    private string GetLevelBadge(bool isUnlocked, int level, int maxLevel)
{
    if (!isUnlocked)
    {
        return "未解放";
    }

    if (IsUnlockStyleFacility() && level >= maxLevel && FacilityManager.Instance.GetNextFacilityLevelCapRequirement(facility) == null)
    {
        return "解放済";
    }

    return $"{GetEffectLabel()} Lv.{level} / {maxLevel}";
}

    private string GetEffectLabel()
{
    if (facility == null)
    {
        return "施設";
    }

    switch (facility.effectType)
    {
        case FacilityEffectType.CharacterUnlock: return "キャラ解放";
        case FacilityEffectType.ChapterUnlock: return "章解放";
        case FacilityEffectType.BossUnlock: return "ボス解放";
        case FacilityEffectType.SummonRateUp: return "召喚加護";
        case FacilityEffectType.FormationSlot: return "編成拡張";
        case FacilityEffectType.LevelCap: return "上限強化";
        case FacilityEffectType.GoldProduction: return "収入強化";
        case FacilityEffectType.SummonCostDown: return "召喚補助";
        case FacilityEffectType.UpgradeCostDown: return "育成補助";
        case FacilityEffectType.StagePointBoost: return "SP強化";
        default: return "施設";
    }
}

    private bool IsUnlockStyleFacility()
{
    if (facility == null)
    {
        return false;
    }

    return facility.effectType == FacilityEffectType.CharacterUnlock
        || facility.effectType == FacilityEffectType.ChapterUnlock
        || facility.effectType == FacilityEffectType.BossUnlock;
}

    private FacilityActionState GetActionState()
    {
        if (!FacilityManager.Instance.IsUnlocked(facility))
        {
            return FacilityManager.Instance.CanUnlock(facility)
                ? FacilityActionState.UnlockReady
                : FacilityActionState.UnlockBlocked;
        }

        if (FacilityManager.Instance.IsMaxLevel(facility))
        {
            if (FacilityManager.Instance.GetNextFacilityLevelCapRequirement(facility) == null)
            {
                return FacilityActionState.Maxed;
            }

            return FacilityManager.Instance.CanUpgradeLevelCap(facility)
                ? FacilityActionState.CapReady
                : FacilityActionState.CapBlocked;
        }

        return GameManager.Instance != null && GameManager.Instance.GetGold() >= FacilityManager.Instance.GetUpgradeCost(facility)
            ? FacilityActionState.UpgradeReady
            : FacilityActionState.UpgradeBlocked;
    }

    private void ApplyVisualState(Color cardColor, Color costColor, Color buttonColor, bool interactable, bool highlightOutline)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = cardColor;
        }

        if (stateOutline != null)
        {
            stateOutline.effectColor = highlightOutline ? ReadyOutlineColor : IdleOutlineColor;
            stateOutline.enabled = true;
        }

        nameText.color = TitleColor;
        levelText.color = BodyColor;
        costText.color = costColor;
        buttonText.color = TitleColor;

        if (button != null)
        {
            button.interactable = interactable;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.94f, 0.78f);
            colors.pressedColor = new Color(0.82f, 0.74f, 0.62f);
            colors.selectedColor = new Color(1f, 0.91f, 0.62f);
            colors.disabledColor = cardColor;
            colors.colorMultiplier = 1f;
            button.colors = colors;
        }
    }

    private string GetUnlockRequirementText()
    {
        int clearedStage = GameManager.Instance != null ? GameManager.Instance.GetClearedStageId() : 0;
        int sp = GameManager.Instance != null ? GameManager.Instance.GetStagePoints() : 0;
        int cost = FacilityManager.Instance.GetUnlockCost(facility);

        if (clearedStage < facility.requiredStageId)
        {
            return $"要 {ShortStageName(GetStageName(facility.requiredStageId))}";
        }

        return $"SP不足  {sp}/{cost}";
    }

    private string GetCapRequirementText(string prefix)
    {
        var req = FacilityManager.Instance.GetNextFacilityLevelCapRequirement(facility);
        if (req == null)
        {
            return "最大強化済み";
        }

        int clearedStage = GameManager.Instance != null ? GameManager.Instance.GetClearedStageId() : 0;
        int sp = GameManager.Instance != null ? GameManager.Instance.GetStagePoints() : 0;
        if (clearedStage < req.stageId)
        {
            return $"{prefix}  要 {ShortStageName(GetStageName(req.stageId))}";
        }

        return $"{prefix}  {sp}/{req.requiredStagePoints}SP";
    }

    private static string GetStageName(int stageId)
    {
        StageData stage = StageDatabase.Instance != null ? StageDatabase.Instance.GetStageById(stageId) : null;
        return stage != null ? stage.stageName : $"Stage {stageId}";
    }

    private static string ShortStageName(string stageName)
    {
        if (string.IsNullOrEmpty(stageName)) return string.Empty;
        return stageName.Replace("Stage ", "S").Replace("ステージ", "S");
    }

    private void ApplyLayout()
    {
        var rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, 250f), 132f);
        }

        ConfigureText(nameText, 21f, 16f, TextAlignmentOptions.Left);
        ConfigureText(levelText, 16f, 12f, TextAlignmentOptions.Right);
        ConfigureText(costText, 18f, 13f, TextAlignmentOptions.Left);
        ConfigureText(buttonText, 18f, 13f, TextAlignmentOptions.Center);

        ConfigureNameRect(nameText);
        ConfigureLevelRect(levelText);
        ConfigureCostRect(costText);
        ConfigureBottomButtonRect(buttonText);
    }

    private static void ConfigureNameRect(TextMeshProUGUI text)
    {
        if (text == null) return;
        var rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -10f);
        rect.sizeDelta = new Vector2(-96f, 32f);
    }

    private static void ConfigureLevelRect(TextMeshProUGUI text)
    {
        if (text == null) return;
        var rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-12f, -12f);
        rect.sizeDelta = new Vector2(78f, 28f);
    }

    private static void ConfigureCostRect(TextMeshProUGUI text)
    {
        if (text == null) return;
        var rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, 0f);
        rect.sizeDelta = new Vector2(-24f, 34f);
    }

    private static void ConfigureBottomButtonRect(TextMeshProUGUI text)
    {
        if (text == null) return;
        var rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 10f);
        rect.sizeDelta = new Vector2(-24f, 30f);
    }

    private static void ConfigureText(TextMeshProUGUI text, float max, float min, TextAlignmentOptions alignment)
    {
        if (text == null) return;
        text.enableAutoSizing = true;
        text.fontSizeMax = max;
        text.fontSizeMin = min;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
    }
    private void ShowTooltip(string text)
{
    if (string.IsNullOrEmpty(text)) return;
    EnsureTooltip();
    tooltipText.text = text;
    tooltip.SetActive(true);

    var sourceRect = GetComponent<RectTransform>();
    var tooltipRect = tooltip.GetComponent<RectTransform>();
    tooltipRect.SetParent(transform.root, false);

    Vector3 worldPosition = sourceRect.TransformPoint(new Vector3(sourceRect.rect.width * 0.5f, sourceRect.rect.height * 0.5f + 48f, 0f));
    var rootRect = transform.root as RectTransform;
    if (rootRect == null)
    {
        tooltipRect.position = worldPosition;
        return;
    }

    Vector3 localPosition = rootRect.InverseTransformPoint(worldPosition);
    Vector2 halfSize = tooltipRect.rect.size * 0.5f;
    Rect bounds = rootRect.rect;
    const float margin = 14f;
    localPosition.x = Mathf.Clamp(localPosition.x, bounds.xMin + halfSize.x + margin, bounds.xMax - halfSize.x - margin);
    localPosition.y = Mathf.Clamp(localPosition.y, bounds.yMin + halfSize.y + margin, bounds.yMax - halfSize.y - margin);
    tooltipRect.localPosition = localPosition;
}

    private static void HideTooltip()
    {
        if (tooltip != null)
        {
            tooltip.SetActive(false);
        }
    }

    private void EnsureTooltip()
    {
        if (tooltip != null)
        {
            return;
        }

        tooltip = new GameObject("FacilityTooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        tooltip.transform.SetParent(transform.root, false);
        var image = tooltip.GetComponent<Image>();
        image.color = new Color(0.05f, 0.08f, 0.12f, 0.97f);
        image.raycastTarget = false;

        var outline = tooltip.AddComponent<Outline>();
        outline.effectColor = new Color(0.86f, 0.68f, 0.34f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        var rect = tooltip.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(320f, 78f);

        var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(tooltip.transform, false);
        tooltipText = textObject.GetComponent<TextMeshProUGUI>();
        tooltipText.color = new Color(1f, 0.93f, 0.72f);
        tooltipText.raycastTarget = false;
        tooltipText.enableAutoSizing = true;
        tooltipText.fontSizeMax = 18f;
        tooltipText.fontSizeMin = 13f;
        tooltipText.alignment = TextAlignmentOptions.Center;
        tooltipText.textWrappingMode = TextWrappingModes.Normal;

        var textRect = tooltipText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 8f);
        textRect.offsetMax = new Vector2(-12f, -8f);
    }

    private static string GetDescription(FacilityData facility)
    {
        if (facility == null) return string.Empty;
        switch (facility.effectType)
        {
            case FacilityEffectType.GoldProduction: return "Gold生産を伸ばします。長期的な強化資金を増やす施設です。";
            case FacilityEffectType.SummonCostDown: return "召喚コストを下げます。序盤の召喚回数を増やしやすくなります。";
            case FacilityEffectType.UpgradeCostDown: return "キャラクター強化コストを下げます。主力育成を進めやすくなります。";
            case FacilityEffectType.StagePointBoost: return "ステージ勝利時の獲得SPを増やします。施設解放を加速します。";
            case FacilityEffectType.FormationSlot: return "編成枠を増やします。複数キャラで戦いやすくなります。";
            case FacilityEffectType.LevelCap: return "キャラクターのLv上限を上げます。後半攻略の基盤です。";
            case FacilityEffectType.CharacterUnlock: return "新キャラクターを召喚対象に追加します。";
            case FacilityEffectType.ChapterUnlock: return "次の章を解放します。新しいステージへ進めます。";
            case FacilityEffectType.SummonRateUp: return $"{facility.summonCategory} カテゴリの召喚率を上げます。召喚ボタン下のカテゴリ選択で対象を切り替えます。";
            case FacilityEffectType.BossUnlock: return "ボス関連要素を解放します。";
            default: return facility.effectType.ToString();
        }
    }
}
