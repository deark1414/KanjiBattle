using System;
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

    private Image iconImage;
    private Image bondTrack;
    private Image bondFill;
    private Image recruitHalo;
    private Outline iconOutline;
    private bool recruitReady;

    public void SetCharacter(CharacterData data, int level, int experience = 0, int experienceToNextLevel = 0, int levelCap = 99, Action<CharacterData> onSelect = null)
    {
        ApplyLayout(data);
        ApplyMembershipAppearance(true);
        DisableSkillTooltip();

        HideLegacyText();
        HideBondMeter();
        SetRecruitState(false);

        selfButton.onClick.RemoveAllListeners();
        if (onSelect != null)
        {
            selfButton.onClick.AddListener(() => onSelect(data));
        }
        selfButton.interactable = onSelect != null;
    }

    public void SetBondCandidate(CharacterData data, int bond, int threshold, Action onRecruit, Action<CharacterData, int, int> onSelect = null)
    {
        ApplyLayout(data);
        bool canRecruit = threshold > 0 && bond >= threshold;
        ApplyMembershipAppearance(false, canRecruit);
        DisableSkillTooltip();

        HideLegacyText();
        EnsureBondMeter(threshold > 0 ? Mathf.Clamp01((float)bond / threshold) : 0f, canRecruit);
        SetRecruitState(canRecruit);
        ApplyBondRowLayout();

        selfButton.onClick.RemoveAllListeners();
        if (canRecruit && onRecruit != null)
        {
            selfButton.onClick.AddListener(() => onRecruit.Invoke());
        }
        else if (onSelect != null)
        {
            selfButton.onClick.AddListener(() => onSelect(data, bond, threshold));
        }
        selfButton.interactable = (canRecruit && onRecruit != null) || (!canRecruit && onSelect != null);
    }

    private void ApplyLayout(CharacterData data)
    {
        var background = GetComponent<Image>();
        // The piece itself carries state; there must be no rectangular card behind it.
        ModernWafuuPresentation.ApplyFlatSurface(background, new Color(1f, 1f, 1f, 0f));
        var outline = background != null ? background.GetComponent<Outline>() : null;
        if (outline != null)
        {
            outline.enabled = false;
        }

        var rect = GetComponent<RectTransform>();
        bool compact = IsCompactCard();
        if (rect != null)
        {
            // The roster supplies a variable cell height. Preserve it so a large
            // piece never spills upward into the preceding group label.
            rect.sizeDelta = new Vector2(
                Mathf.Max(rect.sizeDelta.x, compact ? 126f : 280f),
                Mathf.Max(rect.sizeDelta.y, compact ? 106f : 112f));
        }

        EnsureIcon(data);

        ConfigureText(infoText, compact ? 18f : 22f, compact ? 11f : 15f, TextAlignmentOptions.Left);
        ConfigureText(levelText, compact ? 16f : 20f, compact ? 10f : 14f, TextAlignmentOptions.Center);
        ConfigureText(countText, compact ? 15f : 19f, compact ? 9f : 13f, TextAlignmentOptions.Center);
        ConfigureText(costText, compact ? 13f : 18f, compact ? 9f : 12f, TextAlignmentOptions.Center);

        RectTransform infoRect = infoText != null ? infoText.GetComponent<RectTransform>() : null;
        if (infoRect != null)
        {
            infoRect.anchorMin = new Vector2(0f, 1f);
            infoRect.anchorMax = new Vector2(1f, 1f);
            infoRect.pivot = new Vector2(0.5f, 1f);
            infoRect.anchoredPosition = new Vector2(compact ? 48f : 64f, -8f);
            infoRect.sizeDelta = new Vector2(compact ? -56f : -82f, compact ? 58f : 62f);
        }

        ConfigureBottomTextRect(levelText, 0f, compact ? 76f : 104f);
        ConfigureBottomTextRect(costText, 0.5f, compact ? 0f : 170f);
        ConfigureBottomTextRect(countText, 1f, compact ? 72f : 112f);
        if (costText != null)
        {
            costText.gameObject.SetActive(false);
        }

        BringTextToFront(infoText);
        BringTextToFront(levelText);
        BringTextToFront(countText);
        BringTextToFront(costText);
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
        iconImage.transform.SetAsFirstSibling();
        iconImage.sprite = data.icon;
        iconImage.preserveAspect = true;
        var iconRect = iconImage.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        bool compact = IsCompactCard();
        iconRect.anchoredPosition = Vector2.zero;
        var cardRect = GetComponent<RectTransform>();
        float cardWidth = cardRect != null ? Mathf.Max(cardRect.rect.width, cardRect.sizeDelta.x) : 108f;
        // The roster groups are intentionally variable-width. Scale the piece with
        // its group cell so a single member does not look stranded in empty space.
        bool landscape = !UnityUIRuntimeTheme.IsPortraitNarrowScreen();
        // On wide screens the roster cells expand with the viewport. Cap the
        // physical piece instead of scaling it into an oversized centerpiece.
        float iconSize = Mathf.Clamp(
            cardWidth * (landscape ? 0.64f : 0.82f),
            66f,
            landscape ? 116f : (compact ? 142f : 156f));
        iconRect.sizeDelta = Vector2.one * iconSize;
    }

    private void HideLegacyText()
    {
        if (infoText != null) infoText.gameObject.SetActive(false);
        if (levelText != null) levelText.gameObject.SetActive(false);
        if (countText != null) countText.gameObject.SetActive(false);
        if (costText != null) costText.gameObject.SetActive(false);
    }

    private void ApplyMembershipAppearance(bool owned, bool readyToRecruit = false)
    {
        if (iconImage != null)
        {
            iconImage.color = owned
                ? Color.white
                : readyToRecruit
                    ? new Color(0.78f, 1f, 0.82f, 1f)
                    : new Color(0.46f, 0.46f, 0.46f, 0.82f);
        }

        var background = GetComponent<Image>();
        if (background != null)
        {
            ModernWafuuPresentation.ApplyFlatSurface(background, new Color(1f, 1f, 1f, 0f));
        }
    }

    private void SetRecruitState(bool value)
    {
        recruitReady = value;
        EnsureRecruitHalo();

        if (recruitHalo != null)
        {
            recruitHalo.transform.SetAsFirstSibling();
            recruitHalo.gameObject.SetActive(value);
        }

        if (iconOutline != null)
        {
            iconOutline.enabled = value;
        }

        // 緑の縁印だけで迎え入れ可能を伝える。駒絵の漢字名と競合する文言は出さない。
        if (costText != null)
        {
            costText.gameObject.SetActive(false);
        }
    }

    private void EnsureRecruitHalo()
    {
        if (iconImage == null)
        {
            return;
        }

        if (recruitHalo == null)
        {
            var haloObject = new GameObject("RecruitReadyHalo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            haloObject.transform.SetParent(transform, false);
            recruitHalo = haloObject.GetComponent<Image>();
            recruitHalo.raycastTarget = false;
            ModernWafuuPresentation.ApplyFlatSurface(recruitHalo, new Color(0.22f, 0.82f, 0.36f, 0.22f));

            RectTransform haloRect = recruitHalo.rectTransform;
            haloRect.anchorMin = new Vector2(0.14f, 0.5f);
            haloRect.anchorMax = new Vector2(0.14f, 0.5f);
            haloRect.pivot = new Vector2(0.5f, 0.5f);
            haloRect.anchoredPosition = Vector2.zero;
            haloRect.sizeDelta = new Vector2(90f, 90f);
            recruitHalo.transform.SetAsFirstSibling();
        }

        if (!iconImage.TryGetComponent(out iconOutline))
        {
            iconOutline = iconImage.gameObject.AddComponent<Outline>();
            iconOutline.effectDistance = new Vector2(2f, -2f);
        }
    }

    private void Update()
    {
        if (!recruitReady || recruitHalo == null || !recruitHalo.gameObject.activeSelf)
        {
            return;
        }

        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5.2f);
        recruitHalo.color = new Color(0.18f, 0.72f + pulse * 0.20f, 0.32f, 0.10f + pulse * 0.16f);
        if (iconOutline != null)
        {
            iconOutline.effectColor = new Color(0.36f, 0.95f, 0.50f, 0.50f + pulse * 0.45f);
        }
    }

    private void EnsureBondMeter(float progress, bool readyToRecruit)
    {
        if (bondTrack == null)
        {
            var trackObject = new GameObject("BondProgressTrack", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            trackObject.transform.SetParent(transform, false);
            bondTrack = trackObject.GetComponent<Image>();
            ModernWafuuPresentation.ApplyFlatSurface(bondTrack, new Color(0.15f, 0.13f, 0.10f, 0.44f));

            var fillObject = new GameObject("BondProgressFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.transform.SetParent(trackObject.transform, false);
            bondFill = fillObject.GetComponent<Image>();
            bondFill.raycastTarget = false;
            bondFill.sprite = bondTrack.sprite;
        }

        bondTrack.gameObject.SetActive(true);
        ModernWafuuPresentation.ApplyFlatSurface(
            bondTrack,
            readyToRecruit
                ? new Color(0.08f, 0.28f, 0.13f, 0.68f)
                : new Color(0.15f, 0.13f, 0.10f, 0.44f));

        RectTransform trackRect = bondTrack.rectTransform;
        trackRect.anchorMin = new Vector2(0.16f, 0.37f);
        trackRect.anchorMax = new Vector2(0.94f, 0.63f);
        trackRect.offsetMin = Vector2.zero;
        trackRect.offsetMax = Vector2.zero;

        bondFill.color = readyToRecruit
            ? new Color(0.24f, 0.88f, 0.40f, 1f)
            : new Color(0.86f, 0.58f, 0.20f, 0.96f);
        RectTransform fillRect = bondFill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(progress, 1f);
        fillRect.offsetMin = new Vector2(1f, 1f);
        fillRect.offsetMax = new Vector2(-1f, -1f);
        bondTrack.transform.SetAsLastSibling();
    }

    private void ApplyBondRowLayout()
    {
        if (iconImage == null)
        {
            return;
        }

        RectTransform iconRect = iconImage.rectTransform;
        iconRect.anchorMin = new Vector2(0.12f, 0.5f);
        iconRect.anchorMax = new Vector2(0.12f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = new Vector2(76f, 76f);

        if (recruitHalo != null)
        {
            RectTransform haloRect = recruitHalo.rectTransform;
            haloRect.anchorMin = new Vector2(0.12f, 0.5f);
            haloRect.anchorMax = new Vector2(0.12f, 0.5f);
            haloRect.pivot = new Vector2(0.5f, 0.5f);
            haloRect.anchoredPosition = Vector2.zero;
            haloRect.sizeDelta = new Vector2(90f, 90f);
        }
    }

    private void HideBondMeter()
    {
        if (bondTrack != null)
        {
            bondTrack.gameObject.SetActive(false);
        }
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
        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
        text.enableAutoSizing = true;
        text.fontSizeMax = max;
        text.fontSizeMin = Mathf.Max(UnityUIRuntimeTheme.MinimumTextSize, min);
        text.fontStyle = FontStyles.Normal;
        text.alignment = alignment;
        text.color = new Color(1f, 0.94f, 0.76f, 1f);
        text.faceColor = Color.white;
        text.outlineColor = new Color(0.10f, 0.055f, 0.02f, 1f);
        text.outlineWidth = 0.18f;
        text.maskable = true;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Truncate;
        text.raycastTarget = false;
        text.canvasRenderer.SetAlpha(1f);
        text.ForceMeshUpdate(true, true);
    }

    private bool IsCompactCard()
    {
        var rect = GetComponent<RectTransform>();
        return rect != null && rect.sizeDelta.x <= 260f;
    }

    private static void BringTextToFront(TextMeshProUGUI text)
    {
        if (text == null) return;
        text.transform.SetAsLastSibling();
        text.SetVerticesDirty();
        text.SetMaterialDirty();
    }

    private void DisableSkillTooltip()
    {
        var tooltip = GetComponent<SkillTooltipPresenter>();
        if (tooltip != null)
        {
            tooltip.enabled = false;
        }
        SkillTooltipPresenter.HideAll();
    }

}
