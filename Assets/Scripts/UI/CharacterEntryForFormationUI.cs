using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterEntryForFormationUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI countText;
    private CharacterData characterData;
    private Image iconImage;
    private TextMeshProUGUI statsText;

    public void SetCharacter(CharacterData data, int level)
    {
        characterData = data;
        ApplyLayout(data);
        EnsureSkillTooltip(data);
        string skillName = SkillDescription.GetShort(data.skillType);
        infoText.text = $"{data.characterName}　{skillName}";
        int maxHP = PlayerInventory.Instance != null
            ? PlayerInventory.Instance.GetPlayerMaxHP(data, level)
            : data.GetMaxHP(level);
        statsText.text = $"PLv.{level}　HP {maxHP}　攻 {data.GetAttack(level)}　守 {data.GetDefense(level)}";
        if (levelText != null)
        {
            levelText.gameObject.SetActive(false);
        }
        if (countText != null)
        {
            countText.gameObject.SetActive(false);
        }
    }

    private void ApplyLayout(CharacterData data)
    {
        ModernWafuuPresentation.ApplyListCardSurface(
            GetComponent<Image>(),
            new Color(0.19f, 0.17f, 0.13f, 0.97f));

        var rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, 500f), 104f);
        }

        ConfigureText(infoText, 22f, 16f, TextAlignmentOptions.Left);
        EnsureStatsText();
        ConfigureText(statsText, 20f, 16f, TextAlignmentOptions.Left);

        RectTransform infoRect = infoText != null ? infoText.GetComponent<RectTransform>() : null;
        if (infoRect != null)
        {
            infoRect.anchorMin = new Vector2(0f, 1f);
            infoRect.anchorMax = new Vector2(1f, 1f);
            infoRect.pivot = new Vector2(0.5f, 1f);
            infoRect.anchoredPosition = new Vector2(88f, -8f);
            infoRect.sizeDelta = new Vector2(-104f, 38f);
        }

        ConfigureStatTextRect(statsText);

        EnsureIcon(data);
    }

    private void EnsureStatsText()
    {
        if (statsText != null)
        {
            return;
        }

        Transform existing = transform.Find("FormationStatsText");
        if (existing != null)
        {
            statsText = existing.GetComponent<TextMeshProUGUI>();
        }

        if (statsText == null)
        {
            var go = new GameObject("FormationStatsText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(transform, false);
            statsText = go.GetComponent<TextMeshProUGUI>();
        }

        statsText.raycastTarget = false;
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

    private static void ConfigureStatTextRect(TextMeshProUGUI text)
    {
        if (text == null) return;
        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(88f, 8f);
        rect.sizeDelta = new Vector2(-104f, 30f);
    }

private static void ConfigureText(TextMeshProUGUI text, float max, float min, TextAlignmentOptions alignment)
    {
        if (text == null) return;
        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
        text.enableAutoSizing = true;
        text.fontSizeMax = max;
        text.fontSizeMin = Mathf.Max(UnityUIRuntimeTheme.MinimumTextSize, min);
        text.fontStyle = FontStyles.Normal;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
        text.raycastTarget = false;
    }

    private void EnsureSkillTooltip(CharacterData data)
    {
        var tooltip = GetComponent<SkillTooltipPresenter>();
        if (tooltip == null) tooltip = gameObject.AddComponent<SkillTooltipPresenter>();
        tooltip.SetCharacter(data);
    }

    public void OnClick()
    {
        if (FormationCharacterListUI.instance != null)
        {
            FormationCharacterListUI.instance.SelectCharacter(characterData);
        }
    }
}
