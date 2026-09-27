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
    private TextMeshProUGUI stageIndexText;
    private Image cardImage;
    private Image indexRail;
    private Image landscapeImage;
    private Image landscapeShade;
    private Image landmarkImage;
    private Image landmarkHalo;
    private Image landmarkLabelPlate;
    private static readonly Dictionary<string, Sprite> landscapeSprites = new();
    private System.Action<StageData> selectionHandler;

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
        bool mapNode = IsMapNode();
        stageNameText.enableAutoSizing = mapNode;
        stageNameText.fontSize = mapNode ? 24f : 26f;
        stageNameText.fontSizeMin = 21f;
        stageNameText.fontSizeMax = mapNode ? 24f : 30f;
        stageNameText.alignment = mapNode ? TextAlignmentOptions.Center : TextAlignmentOptions.Left;
        stageNameText.textWrappingMode = TextWrappingModes.Normal;
        stageNameText.overflowMode = TextOverflowModes.Truncate;
        stageNameText.outlineColor = new Color(0.08f, 0.05f, 0.02f, 0.96f);
        stageNameText.outlineWidth = 0.18f;

        RectTransform titleRect = stageNameText.GetComponent<RectTransform>();
        if (titleRect != null)
        {
            // Keep the two-line caption inside its node. The first landmark sits at
            // the bottom edge of the scroll view, so an external caption gets clipped.
            titleRect.anchorMin = mapNode ? new Vector2(-0.04f, 0.01f) : new Vector2(0f, 0.53f);
            titleRect.anchorMax = mapNode ? new Vector2(1.04f, 0.36f) : new Vector2(1f, 1f);
            titleRect.offsetMin = mapNode ? Vector2.zero : new Vector2(64f, -3f);
            titleRect.offsetMax = mapNode ? Vector2.zero : new Vector2(-50f, -2f);
        }

        EnsureStageIndexText();
        EnsureDetailText();
        SetLocked(false);
    }

    public void SetSelectionHandler(System.Action<StageData> handler)
    {
        selectionHandler = handler;
    }

    public void SetState(bool locked, bool cleared)
    {
        EnsureStageNameText();
        if (stageData == null || stageNameText == null)
        {
            return;
        }

        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(stageNameText);
        EnsureStageIndexText();
        RefreshDetails(cleared);
        bool mapNode = IsMapNode();
        stageNameText.text = mapNode
            ? $"第{stageData.stageId:00}局\n{stageData.stageName}"
            : stageData.stageName;
        stageIndexText.gameObject.SetActive(!mapNode);
        if (!mapNode)
        {
            stageIndexText.text = $"{stageData.stageId:00}\n局";
        }
        if (locked)
        {
            stageNameText.color = new Color(0.66f, 0.67f, 0.67f, 1f);
            stageIndexText.color = new Color(0.55f, 0.49f, 0.40f, 1f);
        }
        else if (cleared)
        {
            stageNameText.color = new Color(0.73f, 1f, 0.79f, 1f);
            stageIndexText.color = new Color(0.74f, 0.93f, 0.73f, 1f);
        }
        else
        {
            stageNameText.color = new Color(1f, 0.78f, 0.43f, 1f);
            stageIndexText.color = new Color(1f, 0.78f, 0.38f, 1f);
        }

        HideStatusText();

        ApplyCardState(locked, cleared);
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
        bool mapNode = IsMapNode();
        rect.anchorMin = mapNode ? new Vector2(1f, 0.68f) : new Vector2(1f, 0f);
        rect.anchorMax = mapNode ? new Vector2(1f, 1f) : new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(mapNode ? 30f : 44f, 0f);
        rect.anchoredPosition = new Vector2(-8f, 0f);

        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(statusText);
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.fontSize = IsMapNode() ? 21f : 24f;
        statusText.enableAutoSizing = true;
        statusText.fontSizeMin = 18f;
        statusText.fontSizeMax = 26f;
        statusText.outlineColor = new Color(0.06f, 0.04f, 0.02f, 0.98f);
        statusText.outlineWidth = 0.18f;
        statusText.raycastTarget = false;
        statusText.gameObject.SetActive(false);
    }

    private void EnsureStageIndexText()
    {
        if (stageIndexText != null)
        {
            return;
        }

        Transform existing = transform.Find("StageIndex");
        if (existing != null)
        {
            stageIndexText = existing.GetComponent<TextMeshProUGUI>();
        }

        if (stageIndexText == null)
        {
            GameObject indexObject = new GameObject("StageIndex", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            indexObject.transform.SetParent(transform, false);
            stageIndexText = indexObject.GetComponent<TextMeshProUGUI>();
        }

        RectTransform rect = stageIndexText.GetComponent<RectTransform>();
        bool mapNode = IsMapNode();
        rect.anchorMin = mapNode ? new Vector2(0.08f, -0.25f) : new Vector2(0f, 0f);
        rect.anchorMax = mapNode ? new Vector2(0.30f, -0.02f) : new Vector2(0f, 1f);
        rect.pivot = mapNode ? new Vector2(0.5f, 0.5f) : new Vector2(0f, 0.5f);
        rect.sizeDelta = mapNode ? Vector2.zero : new Vector2(56f, 0f);
        rect.anchoredPosition = mapNode ? Vector2.zero : new Vector2(8f, 0f);

        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(stageIndexText);
        stageIndexText.alignment = TextAlignmentOptions.Center;
        stageIndexText.fontStyle = FontStyles.Bold;
        stageIndexText.enableAutoSizing = true;
        stageIndexText.fontSizeMin = UnityUIRuntimeTheme.MinimumTextSize;
        stageIndexText.fontSizeMax = mapNode ? 24f : 24f;
        stageIndexText.outlineColor = new Color(0.08f, 0.06f, 0.04f, 0.9f);
        stageIndexText.outlineWidth = 0.15f;
        stageIndexText.raycastTarget = false;
        EnsureIndexRail();
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
        bool mapNode = IsMapNode();
        rect.anchorMin = mapNode ? new Vector2(0.06f, 0.22f) : new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, mapNode ? 0.37f : 0.55f);
        rect.offsetMin = mapNode ? Vector2.zero : new Vector2(64f, 4f);
        rect.offsetMax = mapNode ? Vector2.zero : new Vector2(-50f, -2f);

        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(detailText);
        detailText.fontSize = mapNode ? 18f : 21f;
        detailText.enableAutoSizing = true;
        detailText.fontSizeMin = 18f;
        detailText.fontSizeMax = mapNode ? 18f : 21f;
        detailText.alignment = mapNode ? TextAlignmentOptions.Center : TextAlignmentOptions.TopLeft;
        detailText.textWrappingMode = TextWrappingModes.Normal;
        detailText.overflowMode = TextOverflowModes.Ellipsis;
        detailText.outlineColor = new Color(0.06f, 0.04f, 0.02f, 0.96f);
        detailText.outlineWidth = 0.18f;
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
        string rewardLine = $"敵 Lv.{stageData.enemyLevel}  |  戦果 +{stagePoints}  |  軍師経験 +{experience}";
        bool mapNode = IsMapNode();

        if (mapNode)
        {
            // The node stays readable at a glance; the full opponent and reward data lives
            // in the selection panel opened from this node.
            detailText.text = string.Empty;
            return;
        }

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

    private void HideStatusText()
    {
        if (statusText != null)
        {
            statusText.gameObject.SetActive(false);
        }

        Transform legacyStatus = transform.Find("StageStatus") ?? transform.Find("ClearIcon");
        if (legacyStatus != null)
        {
            legacyStatus.gameObject.SetActive(false);
        }
    }

    private void ApplyCardState(bool locked, bool cleared)
    {
        cardImage ??= GetComponent<Image>();
        if (cardImage == null)
        {
            return;
        }

        if (IsMapNode())
        {
            ApplyMapNodeState(locked, cleared);
            return;
        }

        Color surface;
        Color rail;
        bool emphasis = false;
        if (locked)
        {
            surface = new Color(0.11f, 0.10f, 0.08f, 0.92f);
            rail = new Color(0.18f, 0.16f, 0.12f, 1f);
        }
        else if (cleared)
        {
            surface = new Color(0.16f, 0.22f, 0.17f, 0.97f);
            rail = new Color(0.29f, 0.39f, 0.29f, 1f);
        }
        else
        {
            surface = new Color(0.30f, 0.22f, 0.14f, 0.98f);
            rail = new Color(0.64f, 0.27f, 0.13f, 1f);
            emphasis = true;
        }

        ModernWafuuPresentation.ApplyListCardSurface(cardImage, surface, emphasis);
        EnsureLandscape(locked, cleared);
        EnsureIndexRail();
        if (indexRail != null)
        {
            ModernWafuuPresentation.ApplyFlatSurface(indexRail, rail);
        }
    }

    private void ApplyMapNodeState(bool locked, bool cleared)
    {
        ModernWafuuPresentation.ApplyFlatSurface(cardImage, new Color(1f, 1f, 1f, 0f));
        cardImage.raycastTarget = false;
        if (cardImage.TryGetComponent(out Outline cardOutline))
        {
            cardOutline.enabled = false;
        }

        EnsureLandmark();
        EnsureLandmarkLabelPlate(locked, cleared);
        Color tint = locked
            ? new Color(0.44f, 0.44f, 0.44f, 0.66f)
            : cleared
                ? new Color(0.78f, 0.92f, 0.80f, 0.94f)
                : Color.white;
        landmarkImage.color = tint;

        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.targetGraphic = landmarkImage;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.88f, 0.62f, 1f);
            colors.pressedColor = new Color(0.78f, 0.64f, 0.48f, 1f);
            colors.selectedColor = new Color(1f, 0.82f, 0.48f, 1f);
            colors.disabledColor = new Color(0.46f, 0.46f, 0.46f, 0.62f);
            colors.colorMultiplier = 1f;
            button.colors = colors;
        }

        // Landmark art already has a silhouette. A rectangular halo reads as an
        // accidental gray card against the map, so keep the state on its outline.
        landmarkHalo.gameObject.SetActive(false);
        landmarkLabelPlate.gameObject.SetActive(true);
        if (!landmarkImage.TryGetComponent(out Outline landmarkOutline))
        {
            landmarkOutline = landmarkImage.gameObject.AddComponent<Outline>();
        }
        landmarkOutline.enabled = true;
        landmarkOutline.effectDistance = new Vector2(1.5f, -1.5f);
        landmarkOutline.effectColor = locked
            ? new Color(0.16f, 0.14f, 0.12f, 0.88f)
            : cleared
                ? new Color(0.32f, 0.72f, 0.38f, 0.92f)
                : new Color(0.85f, 0.32f, 0.10f, 0.94f);

        EnsureIndexRail();
        if (indexRail != null)
        {
            indexRail.gameObject.SetActive(false);
        }

        if (landscapeImage != null) landscapeImage.gameObject.SetActive(false);
        if (landscapeShade != null) landscapeShade.gameObject.SetActive(false);
    }

    private void EnsureLandmarkLabelPlate(bool locked, bool cleared)
    {
        if (landmarkLabelPlate == null)
        {
            Transform existing = transform.Find("StageLandmarkLabelPlate");
            landmarkLabelPlate = existing != null ? existing.GetComponent<Image>() : null;
        }

        if (landmarkLabelPlate == null)
        {
            var go = new GameObject("StageLandmarkLabelPlate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            landmarkLabelPlate = go.GetComponent<Image>();
            landmarkLabelPlate.raycastTarget = false;
        }

        Color plateColor = locked
            ? new Color(0.11f, 0.11f, 0.11f, 0.94f)
            : cleared
                ? new Color(0.06f, 0.22f, 0.12f, 0.95f)
                : new Color(0.30f, 0.10f, 0.04f, 0.95f);
        ModernWafuuPresentation.ApplyFlatSurface(landmarkLabelPlate, plateColor);
        Stretch(landmarkLabelPlate.rectTransform, new Vector2(-0.06f, 0f), new Vector2(1.06f, 0.38f));
        landmarkLabelPlate.transform.SetSiblingIndex(2);
    }

    private void EnsureLandmark()
    {
        if (landmarkHalo == null)
        {
            var go = new GameObject("StageLandmarkHalo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            landmarkHalo = go.GetComponent<Image>();
            landmarkHalo.raycastTarget = false;
        }

        if (landmarkImage == null)
        {
            var go = new GameObject("StageLandmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            landmarkImage = go.GetComponent<Image>();
        }

        landmarkImage.sprite = GetLandmarkSprite();
        landmarkImage.type = Image.Type.Simple;
        landmarkImage.preserveAspect = true;
        landmarkImage.raycastTarget = true;
        landmarkImage.gameObject.SetActive(landmarkImage.sprite != null);
        GetLandmarkBounds(out Vector2 landmarkMin, out Vector2 landmarkMax);
        Stretch(landmarkImage.rectTransform, landmarkMin, landmarkMax);
        landmarkImage.transform.SetAsFirstSibling();

        Stretch(landmarkHalo.rectTransform, new Vector2(0.18f, 0.25f), new Vector2(0.82f, 0.68f));
        landmarkHalo.transform.SetSiblingIndex(1);
    }

    private void EnsureLandscape(bool locked, bool cleared)
    {
        if (stageData == null)
        {
            return;
        }

        if (landscapeImage == null)
        {
            var go = new GameObject("StageLandscape", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            landscapeImage = go.GetComponent<Image>();
            landscapeImage.raycastTarget = false;
        }

        if (landscapeShade == null)
        {
            var go = new GameObject("StageLandscapeShade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            landscapeShade = go.GetComponent<Image>();
            landscapeShade.raycastTarget = false;
        }

        landscapeImage.sprite = GetLandscapeSprite(stageData.chapterId);
        landscapeImage.type = Image.Type.Simple;
        landscapeImage.preserveAspect = false;
        landscapeImage.color = locked
            ? new Color(0.42f, 0.42f, 0.42f, 0.44f)
            : cleared
                ? new Color(0.78f, 0.90f, 0.80f, 0.78f)
                : Color.white;
        Stretch(landscapeImage.rectTransform, Vector2.zero, Vector2.one);
        landscapeImage.transform.SetAsFirstSibling();

        ModernWafuuPresentation.ApplyFlatSurface(
            landscapeShade,
            locked ? new Color(0.08f, 0.07f, 0.05f, 0.68f) : new Color(0.06f, 0.05f, 0.03f, 0.36f));
        Stretch(landscapeShade.rectTransform, Vector2.zero, Vector2.one);
        landscapeShade.transform.SetSiblingIndex(1);
    }

    private static Sprite GetLandscapeSprite(int chapterId)
    {
        string path = chapterId switch
        {
            <= 2 => "UI/ModernWafuu/BattleGarden",
            <= 5 => "UI/ModernWafuu/BattleStoneCastle",
            _ => "UI/ModernWafuu/BattleSnowShrine",
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

    private Sprite GetLandmarkSprite()
    {
        string path = GetLandmarkPath();

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

    private string GetLandmarkPath()
    {
        if (stageData == null)
        {
            return "UI/ModernWafuu/Stages/StageBanner";
        }

        // The castle is reserved for the boss. Each preceding chapter climb reads as
        // banner -> lookout -> checkpoint -> battlefield before the next stretch.
        if (stageData.isBossStage)
        {
            return "UI/ModernWafuu/Stages/StageBossCastle";
        }

        return ((stageData.stageId - 1) % 5) switch
        {
            4 => "UI/ModernWafuu/Stages/StageBattlefield",
            3 => "UI/ModernWafuu/Stages/StageCheckpoint",
            2 => "UI/ModernWafuu/Stages/StageLookout",
            _ => "UI/ModernWafuu/Stages/StageBanner",
        };
    }

    private void GetLandmarkBounds(out Vector2 min, out Vector2 max)
    {
        string path = GetLandmarkPath();
        if (path.EndsWith("StageBattlefield"))
        {
            min = new Vector2(-0.16f, 0.35f);
            max = new Vector2(1.16f, 1.03f);
            return;
        }

        if (path.EndsWith("StageBossCastle"))
        {
            min = new Vector2(-0.13f, 0.35f);
            max = new Vector2(1.13f, 1.10f);
            return;
        }

        min = new Vector2(-0.08f, 0.35f);
        max = new Vector2(1.08f, 1.12f);
    }

    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void EnsureIndexRail()
    {
        if (indexRail == null)
        {
            Transform existing = transform.Find("StageIndexRail");
            if (existing != null)
            {
                indexRail = existing.GetComponent<Image>();
            }
        }

        if (indexRail == null)
        {
            var railObject = new GameObject("StageIndexRail", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            railObject.transform.SetParent(transform, false);
            indexRail = railObject.GetComponent<Image>();
            indexRail.raycastTarget = false;
        }

        var rect = indexRail.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(IsMapNode() ? 4f : 48f, 0f);
        rect.anchoredPosition = Vector2.zero;
        indexRail.transform.SetAsFirstSibling();
    }

    private bool IsMapNode()
    {
        var rect = GetComponent<RectTransform>();
        return rect != null && rect.sizeDelta.x <= 220f;
    }

    private void OnClick()
    {
        if (stageData == null)
        {
            return;
        }

        if (selectionHandler != null)
        {
            selectionHandler(stageData);
            return;
        }

        GameManager.Instance.SetSelectedStage(stageData);
        UIManager.Instance.ShowFormation();
    }
}
