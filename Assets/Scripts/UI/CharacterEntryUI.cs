using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Button selfButton;
    [SerializeField] private TextMeshProUGUI costText;

    private CharacterData characterData;
    private Image iconImage;

    public void SetCharacter(CharacterData data, int level, int count)
    {
        characterData = data;
        ApplyLayout(data);

        string skillName = GetSkillLabel(data.skillType);
        infoText.text = $"{data.characterName}  {skillName}\nHP {data.GetMaxHP(level)} / ATK {data.GetAttack(level)} / DEF {data.GetDefense(level)}";
        levelText.text = $"Lv.{level}";
        countText.text = $"所持 x{count}";

        int baseCost = data.GetUpgradeCost(level);
        int effectiveCost = GameManager.Instance.GetEffectiveUpgradeCost(baseCost);
        costText.text = $"強化 {effectiveCost}G";

        selfButton.onClick.RemoveAllListeners();
        selfButton.onClick.AddListener(OnClickUpgrade);
    }

    private void ApplyLayout(CharacterData data)
    {
        var rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, 500f), 104f);
        }

        ConfigureText(infoText, 22f, 16f, TextAlignmentOptions.Left);
        ConfigureText(levelText, 21f, 16f, TextAlignmentOptions.Center);
        ConfigureText(countText, 21f, 16f, TextAlignmentOptions.Center);
        ConfigureText(costText, 21f, 16f, TextAlignmentOptions.Center);

        RectTransform infoRect = infoText != null ? infoText.GetComponent<RectTransform>() : null;
        if (infoRect != null)
        {
            infoRect.anchorMin = new Vector2(0f, 1f);
            infoRect.anchorMax = new Vector2(1f, 1f);
            infoRect.pivot = new Vector2(0.5f, 1f);
            infoRect.anchoredPosition = new Vector2(56f, -8f);
            infoRect.sizeDelta = new Vector2(-152f, 56f);
        }

        ConfigureBottomTextRect(levelText, 0f, 104f);
        ConfigureBottomTextRect(costText, 0.5f, 170f);
        ConfigureBottomTextRect(countText, 1f, 112f);

        EnsureIcon(data);
    }

    private void EnsureIcon(CharacterData data)
    {
        if (data == null || data.icon == null)
        {
            if (iconImage != null) iconImage.gameObject.SetActive(false);
            return;
        }

        if (iconImage == null)
        {
            var go = new GameObject("CharacterIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            iconImage = go.GetComponent<Image>();
            iconImage.raycastTarget = false;
        }

        iconImage.gameObject.SetActive(true);
        iconImage.sprite = data.icon;
        iconImage.preserveAspect = true;
        var iconRect = iconImage.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(10f, 4f);
        iconRect.sizeDelta = new Vector2(72f, 72f);
    }

    private static void ConfigureBottomTextRect(TextMeshProUGUI text, float anchorX, float width)
{
    if (text == null) return;
    RectTransform rect = text.GetComponent<RectTransform>();
    rect.anchorMin = new Vector2(anchorX, 0f);
    rect.anchorMax = new Vector2(anchorX, 0f);
    rect.pivot = new Vector2(anchorX, 0f);
    rect.anchoredPosition = anchorX switch
    {
        0f => new Vector2(92f, 10f),
        1f => new Vector2(-12f, 10f),
        _ => new Vector2(0f, 10f)
    };
    rect.sizeDelta = new Vector2(width, 34f);
}

private static void ConfigureText(TextMeshProUGUI text, float max, float min, TextAlignmentOptions alignment)
    {
        if (text == null) return;
        UnityUIRuntimeTheme.ApplyJapaneseFont(text);
        text.enableAutoSizing = true;
        text.fontSizeMax = max;
        text.fontSizeMin = min;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
        text.raycastTarget = false;
    }

    private static string GetSkillLabel(SkillType skillType)
    {
        return skillType == SkillType.None ? "スキルなし" : skillType.ToString();
    }

    private void OnClickUpgrade()
    {
        PlayerInventory.Instance.UpgradeCharacter(characterData);
    }
}
