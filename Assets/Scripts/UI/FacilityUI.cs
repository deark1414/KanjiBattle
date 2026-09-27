using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class FacilityUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI buttonText;
    private TextMeshProUGUI descriptionText;
    private Image descriptionPlate;

    private static readonly Color UnlockedCardColor = Color.white;
    private static readonly Color LockedCardColor = new Color(0.45f, 0.45f, 0.45f, 0.7f);
    private static readonly Color MaxCardColor = Color.white;
    private static readonly Color IdleOutlineColor = new Color(0.18f, 0.14f, 0.10f, 0.55f);
    private static readonly Color TitleColor = new Color(1f, 0.96f, 0.82f, 1f);
    private static readonly Color BodyColor = new Color(0.96f, 0.91f, 0.76f, 1f);
    private static readonly Color ReadyTextColor = new Color(0.68f, 1f, 0.76f, 1f);
    private static readonly Color NeedTextColor = new Color(1f, 0.72f, 0.48f, 1f);
    private static readonly Color LockedTextColor = new Color(0.64f, 0.61f, 0.56f, 1f);
    private static readonly Color MaxTextColor = new Color(0.78f, 0.90f, 1f, 1f);

    private FacilityData facility;
    private Image backgroundImage;
    private Image landscapeImage;
    private Image landscapeShade;
    private Image actionSeal;
    private TextMeshProUGUI actionSealText;
    private Outline buildingOutline;
    private Button button;
    private Outline stateOutline;
    private float nextRefreshTime;
    private static GameObject tooltip;
    private static TextMeshProUGUI tooltipText;
    private static readonly Dictionary<string, Sprite> landscapeSprites = new();
    private System.Action<FacilityUI> selectionHandler;
    private bool actionAvailable;

    private enum FacilityActionState
    {
        UnlockReady,
        UnlockBlocked,
        UpgradeReady,
        UpgradeBlocked,
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

    public FacilityData Facility => facility;
    public string DetailDescription => GetDescription(facility);
    public string DetailStatus => GetCompactDetailStatus();
    public string DetailActionLabel => actionAvailable && buttonText != null ? buttonText.text : string.Empty;
    public bool CanExecuteAction => actionAvailable;

    public void SetSelectionHandler(System.Action<FacilityUI> handler)
    {
        selectionHandler = handler;
        Refresh();
    }

    public void ExecuteActionFromDetail()
    {
        ExecuteAction();
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
        // Details live in the fixed information region so they cannot cover
        // neighboring facilities or consume a tap.
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // See OnPointerEnter.
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    private void OnClickAction()
    {
        if (selectionHandler != null)
        {
            selectionHandler(this);
            return;
        }

        ExecuteAction();
    }

    private void ExecuteAction()
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
            // Facility level caps advance automatically when the required stage is cleared.
            // There is intentionally no player action at this state.
            Refresh();
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

        EnsureDescription();
        descriptionText.text = GetInlineDescription();
        HideLegacyText();

        switch (state)
        {
            case FacilityActionState.UnlockReady:
                costText.text = $"未解放 / 解放可能  戦果 {FacilityManager.Instance.GetUnlockCost(facility)}";
                buttonText.text = "解放する";
                ApplyVisualState(LockedCardColor, ReadyTextColor, new Color(0.54f, 0.38f, 0.20f, 1f), true, true);
                break;
            case FacilityActionState.UnlockBlocked:
                costText.text = $"未解放 / {GetUnlockRequirementText()}";
                buttonText.text = "未解放";
                ApplyVisualState(LockedCardColor, LockedTextColor, new Color(0.26f, 0.26f, 0.26f, 1f), false, false);
                break;
            case FacilityActionState.UpgradeReady:
                costText.text = $"{GetEffectLabel()} / 強化可能  戦果 {FacilityManager.Instance.GetUpgradeCost(facility)}";
                buttonText.text = "強化";
                ApplyVisualState(UnlockedCardColor, ReadyTextColor, new Color(0.64f, 0.42f, 0.20f, 1f), true, true);
                break;
            case FacilityActionState.UpgradeBlocked:
                costText.text = $"{GetEffectLabel()} / 戦果不足  {FacilityManager.Instance.GetUpgradeCost(facility)}";
                buttonText.text = "強化待ち";
                ApplyVisualState(UnlockedCardColor, NeedTextColor, new Color(0.42f, 0.31f, 0.22f, 1f), false, false);
                break;
            case FacilityActionState.CapBlocked:
                costText.text = GetCapRequirementText("上限到達 / 次の上限");
                buttonText.text = string.Empty;
                ApplyVisualState(MaxCardColor, NeedTextColor, new Color(0.38f, 0.30f, 0.23f, 1f), false, false);
                break;
            default:
                bool isUnlockOnly = facility.effectType == FacilityEffectType.StageRetry;
                costText.text = isUnlockOnly ? "戦闘後に同じステージへ再挑戦できます" : "最大強化済み";
                buttonText.text = isUnlockOnly ? "解放済み" : "MAX";
                ApplyVisualState(MaxCardColor, MaxTextColor, new Color(0.40f, 0.30f, 0.22f, 1f), false, false);
                break;
        }

        UpdateActionSeal(state);
    }
    private string GetLevelBadge(bool isUnlocked, int level, int maxLevel)
{
    if (!isUnlocked)
    {
        return "未解放";
    }

    if (facility != null && facility.effectType == FacilityEffectType.StageRetry)
    {
        return "再戦機能 解放済み";
    }

    string capPrefix = level >= maxLevel ? "上限中 " : string.Empty;
    return $"{capPrefix}{GetEffectLabel()} Lv.{level} / {maxLevel}";
}

    private string GetEffectLabel()
{
    if (facility == null)
    {
        return "施設";
    }

    switch (facility.effectType)
    {
        case FacilityEffectType.FormationSlot: return "編成枠";
        case FacilityEffectType.StagePointBoost: return "戦果増加";
        case FacilityEffectType.Recruitment: return "縁の進行";
        case FacilityEffectType.Training: return "経験値強化";
        case FacilityEffectType.BattleSpeed: return "戦闘速度";
        case FacilityEffectType.StageRetry: return "再戦解放";
        case FacilityEffectType.TrainingFrequency: return "稽古頻度";
        case FacilityEffectType.AttackBoost: return "攻勢強化";
        case FacilityEffectType.SkillPowerBoost: return "技能威力";
        case FacilityEffectType.SkillChanceBoost: return "技能発動";
        case FacilityEffectType.HealthBoost: return "体力強化";
        default: return "施設";
    }
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

            return FacilityActionState.CapBlocked;
        }

        return GameManager.Instance != null && GameManager.Instance.GetStagePoints() >= FacilityManager.Instance.GetUpgradeCost(facility)
            ? FacilityActionState.UpgradeReady
            : FacilityActionState.UpgradeBlocked;
    }

    private void ApplyVisualState(Color cardColor, Color costColor, Color buttonColor, bool interactable, bool highlightOutline)
    {
        actionAvailable = interactable;
        if (backgroundImage != null)
        {
            ModernWafuuPresentation.ApplyFlatSurface(backgroundImage, new Color(1f, 1f, 1f, 0f));
            backgroundImage.raycastTarget = false;
            if (backgroundImage.TryGetComponent(out Outline backgroundOutline))
            {
                backgroundOutline.enabled = false;
            }
        }

        Transform legacyActionBand = transform.Find("FacilityActionBand");
        if (legacyActionBand != null)
        {
            legacyActionBand.gameObject.SetActive(false);
        }

        EnsureLandscape(interactable);

        if (stateOutline != null)
        {
            stateOutline.enabled = false;
        }

        nameText.color = TitleColor;
        levelText.color = BodyColor;
        costText.color = costColor;
        if (descriptionText != null) descriptionText.color = BodyColor;
        buttonText.color = interactable ? new Color(1f, 0.86f, 0.43f, 1f) : TitleColor;

        if (button != null)
        {
            button.targetGraphic = landscapeImage;
            button.interactable = selectionHandler != null || interactable;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.94f, 0.78f);
            colors.pressedColor = new Color(0.82f, 0.74f, 0.62f);
            colors.selectedColor = new Color(1f, 0.91f, 0.62f);
            colors.disabledColor = new Color(0.46f, 0.46f, 0.46f, 0.62f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
        }
    }

    private void EnsureLandscape(bool interactable)
    {
        if (facility == null)
        {
            return;
        }

        if (landscapeImage == null)
        {
            var go = new GameObject("FacilityBuilding", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            landscapeImage = go.GetComponent<Image>();
        }

        landscapeImage.sprite = GetLandscapeSprite();
        landscapeImage.type = Image.Type.Simple;
        landscapeImage.preserveAspect = true;
        landscapeImage.raycastTarget = true;
        bool locked = !FacilityManager.Instance.IsUnlocked(facility);
        landscapeImage.color = locked
            ? new Color(0.34f, 0.34f, 0.34f, 0.58f)
            : Color.white;
        Stretch(landscapeImage.rectTransform, new Vector2(0.03f, 0.18f), new Vector2(0.42f, 0.95f));
        landscapeImage.rectTransform.localScale = ShouldMirrorLandscape()
            ? new Vector3(-1f, 1f, 1f)
            : Vector3.one;
        landscapeImage.transform.SetAsFirstSibling();

        if (landscapeImage.TryGetComponent(out buildingOutline))
        {
            buildingOutline.effectColor = IdleOutlineColor;
            buildingOutline.effectDistance = new Vector2(1.5f, -1.5f);
            buildingOutline.enabled = !interactable;
        }
        else
        {
            buildingOutline = landscapeImage.gameObject.AddComponent<Outline>();
            buildingOutline.effectColor = IdleOutlineColor;
            buildingOutline.effectDistance = new Vector2(1.5f, -1.5f);
            buildingOutline.enabled = !interactable;
        }

        if (landscapeShade != null)
        {
            landscapeShade.gameObject.SetActive(false);
        }
    }

    private void UpdateActionSeal(FacilityActionState state)
    {
        bool visible = state == FacilityActionState.UnlockReady
            || state == FacilityActionState.UpgradeReady;
        if (actionSeal == null)
        {
            var sealObject = new GameObject("FacilityActionSeal", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            sealObject.transform.SetParent(transform, false);
            actionSeal = sealObject.GetComponent<Image>();
            ModernWafuuPresentation.ApplyFlatSurface(actionSeal, new Color(0.74f, 0.08f, 0.05f, 0.96f));
            actionSeal.raycastTarget = false;
            var outline = sealObject.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.90f, 0.72f, 0.92f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            var sealRect = actionSeal.rectTransform;
            sealRect.anchorMin = new Vector2(0.72f, 0.70f);
            sealRect.anchorMax = new Vector2(0.92f, 0.91f);
            sealRect.offsetMin = Vector2.zero;
            sealRect.offsetMax = Vector2.zero;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(sealObject.transform, false);
            actionSealText = labelObject.GetComponent<TextMeshProUGUI>();
            UnityUIRuntimeTheme.EnsureJapaneseCapableFont(actionSealText);
            actionSealText.enableAutoSizing = true;
            actionSealText.fontSizeMax = 16f;
            actionSealText.fontSizeMin = UnityUIRuntimeTheme.MinimumTextSize;
            actionSealText.alignment = TextAlignmentOptions.Center;
            actionSealText.color = new Color(1f, 0.96f, 0.80f, 1f);
            actionSealText.raycastTarget = false;
            var labelRect = actionSealText.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(2f, 2f);
            labelRect.offsetMax = new Vector2(-2f, -2f);
        }

        actionSeal.gameObject.SetActive(visible);
        if (!visible)
        {
            return;
        }

        // The seal is the sole ready-state signal. Keeping it still avoids competing
        // color tweens and lets the building art remain visually calm.
        actionSeal.rectTransform.localScale = Vector3.one;

        actionSealText.text = state switch
        {
            FacilityActionState.UnlockReady => "解放",
            FacilityActionState.UpgradeReady => "強化",
            _ => string.Empty
        };
        actionSeal.transform.SetAsLastSibling();
    }

    private Sprite GetLandscapeSprite()
    {
        string path = facility.effectType switch
        {
            FacilityEffectType.FormationSlot => "UI/ModernWafuu/Buildings/FacilityBarracks",
            FacilityEffectType.StagePointBoost => "UI/ModernWafuu/Buildings/FacilityStorehouse",
            FacilityEffectType.Recruitment => "UI/ModernWafuu/Buildings/FacilityBondShrine",
            FacilityEffectType.Training => "UI/ModernWafuu/Buildings/FacilityDojo",
            FacilityEffectType.BattleSpeed => "UI/ModernWafuu/Buildings/FacilityBattleArchive",
            FacilityEffectType.StageRetry => "UI/ModernWafuu/Buildings/FacilityCouncilPavilion",
            FacilityEffectType.TrainingFrequency => "UI/ModernWafuu/Buildings/FacilityDrillTower",
            FacilityEffectType.AttackBoost => "UI/ModernWafuu/Buildings/FacilityMartialHall",
            FacilityEffectType.SkillPowerBoost => "UI/ModernWafuu/Buildings/FacilityStrategyHall",
            FacilityEffectType.SkillChanceBoost => "UI/ModernWafuu/Buildings/FacilityShrine",
            FacilityEffectType.HealthBoost => "UI/ModernWafuu/Buildings/FacilityInfirmary",
            _ => "UI/ModernWafuu/Buildings/FacilityBarracks",
        };

        if (landscapeSprites.TryGetValue(path, out Sprite sprite))
        {
            return sprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(path);
        if (texture == null)
        {
            return null;
        }

        sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        landscapeSprites[path] = sprite;
        return sprite;
    }

    private bool ShouldMirrorLandscape()
    {
        return facility.effectType is FacilityEffectType.StagePointBoost
            or FacilityEffectType.Recruitment
            or FacilityEffectType.StageRetry;
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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

        return $"戦果不足  {sp}/{cost}";
    }

    private string GetCapRequirementText(string prefix)
    {
        var req = FacilityManager.Instance.GetNextFacilityLevelCapRequirement(facility);
        if (req == null)
        {
            return "最大強化済み";
        }

        int clearedStage = GameManager.Instance != null ? GameManager.Instance.GetClearedStageId() : 0;
        if (clearedStage < req.stageId)
        {
            return $"{prefix}  要 {ShortStageName(GetStageName(req.stageId))}";
        }

        return $"{prefix}  {ShortStageName(GetStageName(req.stageId))} 到達済";
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
            rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, 250f), 216f);
        }

        ConfigureText(nameText, 28f, 24f, TextAlignmentOptions.Left);
        ConfigureText(levelText, 24f, 21f, TextAlignmentOptions.Right);
        ConfigureText(costText, 22f, 21f, TextAlignmentOptions.Left);
        ConfigureText(buttonText, 24f, 21f, TextAlignmentOptions.Center);

        ConfigureNameRect(nameText);
        ConfigureLevelRect(levelText);
        ConfigureCostRect(costText);
        ConfigureBottomButtonRect(buttonText);
    }

    private static void ConfigureNameRect(TextMeshProUGUI text)
    {
        if (text == null) return;
        var rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.46f, 0.73f);
        rect.anchorMax = new Vector2(0.92f, 0.91f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private static void ConfigureLevelRect(TextMeshProUGUI text)
    {
        if (text == null) return;
        var rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.46f, 0.60f);
        rect.anchorMax = new Vector2(0.92f, 0.73f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private static void ConfigureCostRect(TextMeshProUGUI text)
    {
        if (text == null) return;
        var rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.46f, 0.16f);
        rect.anchorMax = new Vector2(0.92f, 0.30f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private static void ConfigureBottomButtonRect(TextMeshProUGUI text)
    {
        if (text == null) return;
        var rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.46f, 0.04f);
        rect.anchorMax = new Vector2(0.92f, 0.16f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private static void ConfigureText(TextMeshProUGUI text, float max, float min, TextAlignmentOptions alignment)
    {
        if (text == null) return;
        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
        text.enableAutoSizing = true;
        text.fontSizeMax = max;
        text.fontSizeMin = Mathf.Max(UnityUIRuntimeTheme.MinimumTextSize, min);
        text.alignment = alignment;
        text.fontStyle = FontStyles.Bold;
        text.faceColor = Color.white;
        text.outlineColor = new Color(0.07f, 0.04f, 0.02f, 0.98f);
        text.outlineWidth = 0.20f;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
    }

    private void EnsureDescription()
    {
        if (descriptionText != null)
        {
            return;
        }

        var plateObject = new GameObject("FacilityInfoPlate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        plateObject.transform.SetParent(transform, false);
        descriptionPlate = plateObject.GetComponent<Image>();
        ModernWafuuPresentation.ApplyFlatSurface(descriptionPlate, new Color(0.10f, 0.08f, 0.05f, 0.78f));
        // The building silhouette and its written explanation describe one facility.
        // Let either surface reach the parent Button so the generous card target does
        // not force players to tap the narrow artwork alone.
        descriptionPlate.raycastTarget = true;
        RectTransform plateRect = descriptionPlate.rectTransform;
        plateRect.anchorMin = new Vector2(0.44f, 0.10f);
        plateRect.anchorMax = new Vector2(0.96f, 0.94f);
        plateRect.offsetMin = Vector2.zero;
        plateRect.offsetMax = Vector2.zero;

        var descriptionObject = new GameObject("Description", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        descriptionObject.transform.SetParent(plateObject.transform, false);
        descriptionText = descriptionObject.GetComponent<TextMeshProUGUI>();
        ConfigureText(descriptionText, 23f, 18f, TextAlignmentOptions.TopLeft);
        descriptionText.fontStyle = FontStyles.Bold;
        descriptionText.textWrappingMode = TextWrappingModes.Normal;
        descriptionText.overflowMode = TextOverflowModes.Ellipsis;
        RectTransform rect = descriptionText.rectTransform;
        rect.anchorMin = new Vector2(0.07f, 0.10f);
        rect.anchorMax = new Vector2(0.93f, 0.90f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        plateObject.transform.SetAsLastSibling();
    }

    private void HideLegacyText()
    {
        if (nameText != null) nameText.gameObject.SetActive(false);
        if (levelText != null) levelText.gameObject.SetActive(false);
        if (costText != null) costText.gameObject.SetActive(false);
        if (buttonText != null) buttonText.gameObject.SetActive(false);
    }

    private string GetInlineDescription()
    {
        int level = FacilityManager.Instance != null
            ? FacilityManager.Instance.GetLevel(facility)
            : 0;
        string effect = facility.effectType switch
        {
            FacilityEffectType.FormationSlot => $"編成枠 +{level}",
            FacilityEffectType.Recruitment => $"縁の獲得 {FacilityManager.Instance.GetRecruitmentBondGain()}倍",
            // Cards are scanned at a glance. Keep the two experience gains short
            // enough to remain legible in either column on landscape screens.
            FacilityEffectType.Training => $"戦闘経験 +{FacilityManager.Instance.GetBattleExperienceRate() * 100f:0}%\n稽古経験 +{FacilityManager.Instance.GetMockTrainingExperienceRate() * 100f:0.##}%",
            FacilityEffectType.TrainingFrequency => $"稽古の間隔 {FacilityManager.Instance.GetMockTrainingCooldownSeconds()}秒",
            FacilityEffectType.AttackBoost => $"味方ATK x{FacilityManager.Instance.GetAttackMultiplier():0.##}",
            FacilityEffectType.SkillPowerBoost => $"技能威力 x{FacilityManager.Instance.GetSkillPowerMultiplier():0.##}",
            FacilityEffectType.SkillChanceBoost => $"技能発動 x{FacilityManager.Instance.GetSkillChanceMultiplier():0.##}",
            FacilityEffectType.HealthBoost => $"味方HP x{FacilityManager.Instance.GetHealthMultiplier():0.##}",
            FacilityEffectType.BattleSpeed => $"最高速度 x{GetSpeedForLevel(level):0.##}",
            FacilityEffectType.StagePointBoost => $"戦果獲得 +{facility.GetEffectValue(level) * 100f:0}%",
            FacilityEffectType.StageRetry => "同じ局へ即再挑戦",
            _ => string.Empty
        };
        // The building card itself already conveys the name and lock state. Keep
        // the caption to the effect only so the card never turns into four lines
        // on a narrow screen; the information region holds level/cost details.
        return effect;
    }

    private string GetCompactDetailStatus()
    {
        if (facility == null || FacilityManager.Instance == null)
        {
            return string.Empty;
        }

        if (!FacilityManager.Instance.IsUnlocked(facility))
        {
            return $"解放: 第{facility.requiredStageId:00}局 / 戦果 {FacilityManager.Instance.GetUnlockCost(facility)}";
        }

        if (facility.effectType == FacilityEffectType.StageRetry)
        {
            return "同じ局へ再挑戦可能";
        }

        int level = FacilityManager.Instance.GetLevel(facility);
        int cap = FacilityManager.Instance.GetCurrentFacilityMaxLevel(facility);
        if (FacilityManager.Instance.IsMaxLevel(facility))
        {
            FacilityLevelCapRequirement next = FacilityManager.Instance.GetNextFacilityLevelCapRequirement(facility);
            return next != null ? $"Lv.{level}/{cap}　次の上限: 第{next.stageId:00}局" : $"Lv.{level}/{cap}　強化済み";
        }

        return $"Lv.{level}/{cap}　強化: 戦果 {FacilityManager.Instance.GetUpgradeCost(facility)}";
    }

    private static float GetSpeedForLevel(int level)
    {
        float[] speeds = { 1f, 1.1f, 1.25f, 1.45f, 1.7f, 2f, 2.35f, 2.7f, 3f };
        return speeds[Mathf.Clamp(level, 0, speeds.Length - 1)];
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
        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(tooltipText);
        tooltipText.color = new Color(1f, 0.93f, 0.72f);
        tooltipText.raycastTarget = false;
        tooltipText.enableAutoSizing = true;
        tooltipText.fontSizeMax = 18f;
        tooltipText.fontSizeMin = UnityUIRuntimeTheme.MinimumTextSize;
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
            case FacilityEffectType.StagePointBoost: return "局の勝利時に得る戦果を増やし、施設解放を早めます。";
            case FacilityEffectType.FormationSlot: return "編成できる仲間の数を1枠ずつ増やします。";
            case FacilityEffectType.Recruitment: return "勝利した局で出会った敵との縁を早め、低確率で縁を一気に満たします。満ちた仲間は軍勢から迎え入れます。";
            case FacilityEffectType.Training: return "現在の必要経験値に対する戦闘・稽古の獲得割合を増やします。";
            case FacilityEffectType.BattleSpeed: return "戦闘速度を小刻みに上げ、最高3倍まで解放します。";
            case FacilityEffectType.StageRetry: return "戦闘結果から、同じステージにすぐ再挑戦できるようにします。";
            case FacilityEffectType.TrainingFrequency: return "稽古の間隔を短くし、経験値を得る頻度を高めます。";
            case FacilityEffectType.AttackBoost: return "味方の攻撃力を高めます。";
            case FacilityEffectType.SkillPowerBoost: return "味方の技能の威力を高めます。";
            case FacilityEffectType.SkillChanceBoost: return "味方の技能発動率を倍率で高めます。";
            case FacilityEffectType.HealthBoost: return "味方の最大HPを高め、長く戦える軍勢にします。";
            default: return "施設の効果を高めます。";
        }
    }
}
