using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using TMPro;

public class StageSelectUI : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private GameObject stageButtonPrefab;
    [SerializeField] private StageDatabase stageDatabase;

    private Button nextStageButton;
    private TextMeshProUGUI nextStageLabel;
    private GameObject stageDetailOverlay;
    private TextMeshProUGUI stageDetailTitle;
    private TextMeshProUGUI stageDetailBody;
    private Button stageDetailAction;
    private TextMeshProUGUI stageDetailActionLabel;
    private ScreenInfoRegion stageInformationRegion;
    private Button stageDeployButton;
    private TextMeshProUGUI stageDeployLabel;
    private StageData inspectedStage;
    private bool inspectedStageLocked;
    private readonly Dictionary<StageData, RectTransform> stageRectsByData = new();
    private MarchEnemyVfx marchEnemyVfx;
    private const int StagesPerRegion = 5;
    private static Texture2D campaignMapTexture;

    private static readonly Vector2[] landmarkOffsets =
    {
        new(-0.070f, 0.012f), new(0.088f, 0.010f), new(-0.090f, 0.011f),
        new(0.082f, 0.014f), new(-0.058f, 0.013f)
    };

    // Hand-authored waypoints for MarchMapCampaignForestSoft. The art is painterly,
    // so colour-based road detection also picks up rock and field highlights. These
    // points mark the road itself; landmark offsets place the interactive objects
    // next to it, including a paired side-by-side stop in every five-stage stretch.
    private static readonly Vector2[] campaignRoadAnchors =
    {
        new(0.504f, 0.005f), new(0.465f, 0.058f), new(0.471f, 0.102f), new(0.487f, 0.125f), new(0.579f, 0.142f),
        new(0.573f, 0.175f), new(0.480f, 0.190f), new(0.552f, 0.216f), new(0.595f, 0.264f), new(0.508f, 0.285f),
        new(0.502f, 0.331f), new(0.594f, 0.348f), new(0.612f, 0.405f), new(0.523f, 0.434f), new(0.447f, 0.467f),
        new(0.537f, 0.490f), new(0.629f, 0.508f), new(0.595f, 0.549f), new(0.505f, 0.573f), new(0.428f, 0.614f),
        new(0.507f, 0.647f), new(0.600f, 0.657f), new(0.685f, 0.684f), new(0.609f, 0.720f), new(0.518f, 0.740f),
        new(0.427f, 0.755f), new(0.445f, 0.800f), new(0.538f, 0.816f), new(0.573f, 0.840f), new(0.480f, 0.851f),
        new(0.388f, 0.866f), new(0.472f, 0.883f), new(0.565f, 0.893f), new(0.658f, 0.900f), new(0.618f, 0.918f),
        new(0.525f, 0.927f), new(0.532f, 0.946f), new(0.625f, 0.947f), new(0.566f, 0.963f), new(0.606f, 0.975f)
    };

    private void OnEnable()
    {
        if (stageDatabase != null)
        {
            stageDatabase.AssignStageIds();
        }
        DisplayStages();
    }

    public void DisplayStages()
    {
        if (content == null || stageDatabase == null || stageButtonPrefab == null)
        {
            return;
        }

        ClearStageButtons();
        stageRectsByData.Clear();

        var gameManager = GameManager.Instance != null ? GameManager.Instance : FindAnyObjectByType<GameManager>();
        if (gameManager == null)
        {
            return;
        }

        int focusIndex = 0;
        bool foundFocus = false;
        StageData nextStage = null;
        int index = 0;
        var stageRects = new List<RectTransform>();
        foreach (var stage in stageDatabase.stages.Where(stage => stage != null).OrderBy(stage => stage.stageId))
        {
            var btn = Instantiate(stageButtonPrefab, content);
            ConfigureStageButtonLayout(btn);
            RectTransform stageRect = btn.GetComponent<RectTransform>();
            stageRects.Add(stageRect);
            stageRectsByData[stage] = stageRect;
            var ui = btn.GetComponent<StageButtonUI>();
            ui.SetStage(stage);

            bool stageProgressLocked = stage.stageId > gameManager.GetHighestClearedStageId() + 1;
            bool chapterLocked = !gameManager.IsChapterUnlocked(stage.chapterId);
            bool locked = stageProgressLocked || chapterLocked;
            // A locked node is still useful: it should open its dossier so players can
            // inspect future opponents and rewards before committing resources.
            btn.GetComponent<UnityEngine.UI.Button>().interactable = true;
            ui.SetState(locked, gameManager.IsStageCleared(stage.stageId));
            ui.SetSelectionHandler(selected => ShowStageDetails(selected, locked));

            if (!locked && !gameManager.IsStageCleared(stage.stageId) && !foundFocus)
            {
                focusIndex = index;
                foundFocus = true;
                nextStage = stage;
            }
            index++;
        }

        FinalizeContentLayout(stageRects, focusIndex);
        if (nextStageButton != null)
        {
            nextStageButton.gameObject.SetActive(false);
        }

        if (nextStage != null && stageDetailOverlay == null)
        {
            ShowStageDetails(nextStage, false);
        }
    }

    private void ClearStageButtons()
    {
        marchEnemyVfx?.Clear();
        if (stageDeployButton != null)
        {
            stageDeployButton.gameObject.SetActive(false);
        }
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            var child = content.GetChild(i);
            if (child.GetComponent<StageButtonUI>() != null || child.name.StartsWith("StageRoute"))
            {
                child.gameObject.SetActive(false);
                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }
    }

    private static void ConfigureStageButtonLayout(GameObject buttonObject)
    {
        if (buttonObject == null) return;

        var rect = buttonObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? 156f : 218f, 136f);
        }

        var layout = buttonObject.GetComponent<LayoutElement>();
        if (layout == null) layout = buttonObject.AddComponent<LayoutElement>();
        layout.minHeight = 0f;
        layout.preferredHeight = 0f;
        layout.flexibleHeight = 0f;
        layout.ignoreLayout = true;
    }

    private void FinalizeContentLayout(IReadOnlyList<RectTransform> stageRects, int focusIndex)
    {
        var contentRect = content as RectTransform;
        if (contentRect == null || stageRects == null || stageRects.Count == 0) return;

        bool portrait = UnityUIRuntimeTheme.IsPortraitNarrowScreen();
        // A landscape viewport is short and wide, so the portrait map scale reads
        // as an unnecessary close-up. Pull it back while reducing node footprints
        // by the same proportion, preserving the authored road spacing.
        float mapHeight = portrait ? 5200f : 4300f;
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, mapHeight);
        contentRect.anchoredPosition = Vector2.zero;

        var layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            layout.enabled = false;
        }

        var fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter != null) fitter.enabled = false;

        var scrollRect = GetComponentInChildren<ScrollRect>();
        if (scrollRect != null)
        {
            RectTransform scrollRectTransform = scrollRect.GetComponent<RectTransform>();
            if (scrollRectTransform != null)
            {
                scrollRectTransform.anchorMin = new Vector2(0.04f, 0.07f);
                scrollRectTransform.anchorMax = new Vector2(0.96f, 0.65f);
                scrollRectTransform.offsetMin = Vector2.zero;
                scrollRectTransform.offsetMax = Vector2.zero;
            }
            scrollRect.verticalScrollbar = null;
            scrollRect.horizontalScrollbar = null;
            foreach (Scrollbar scrollbar in GetComponentsInChildren<Scrollbar>(true))
            {
                scrollbar.gameObject.SetActive(false);
            }
        }

        EnsureMapSurface(contentRect, mapHeight);
        Canvas.ForceUpdateCanvases();
        float mapWidth = GetMapWidth(mapHeight);
        float horizontalMapScale = mapWidth / Mathf.Max(1f, contentRect.rect.width);
        PositionStageNodes(stageRects, mapHeight, portrait, horizontalMapScale);

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        ScrollToFocus(contentRect, focusIndex, mapHeight, stageRects.Count);
    }

    private void PositionStageNodes(IReadOnlyList<RectTransform> stageRects, float mapHeight, bool portrait, float horizontalMapScale)
    {
        float nodeWidth = portrait ? 140f : 158f;
        float nodeHeight = portrait ? 112f : 106f;
        float contentWidth = GetMapWidth(mapHeight) / Mathf.Max(0.01f, horizontalMapScale);
        var placedNodes = new List<Vector2>();

        for (int i = 0; i < stageRects.Count; i++)
        {
            RectTransform rect = stageRects[i];
            Vector2 roadPosition = GetCampaignStop(i, stageRects.Count, mapHeight);
            Vector2 routePosition = GetNodePosition(i, stageRects.Count, roadPosition, mapHeight, horizontalMapScale);
            routePosition = ResolveNodeOverlap(
                routePosition,
                placedNodes,
                horizontalMapScale,
                nodeWidth,
                nodeHeight,
                contentWidth);

            rect.anchorMin = new Vector2(routePosition.x, 1f);
            rect.anchorMax = new Vector2(routePosition.x, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            float routeYFromTop = mapHeight - routePosition.y;
            rect.anchoredPosition = new Vector2(0f, -routeYFromTop);
            rect.sizeDelta = new Vector2(nodeWidth, nodeHeight);
            placedNodes.Add(routePosition);
        }
    }

    private static Vector2 GetNodePosition(int stageIndex, int stageCount, Vector2 roadPosition, float mapHeight, float horizontalMapScale)
    {
        // Convert from the full map to the centered crop, then set each landmark
        // just beside or above the road so the route itself remains visible.
        Vector2 offset = landmarkOffsets[stageIndex % StagesPerRegion];
        float mapX = 0.5f + (roadPosition.x - 0.5f) * horizontalMapScale;
        // The first landmark begins at the bottom edge of the campaign. Reserve
        // room for its caption rather than letting it spill out of the map.
        float firstStageLift = stageIndex == 0 ? 48f : 0f;
        return new Vector2(
            Mathf.Clamp01(mapX + offset.x * horizontalMapScale),
            Mathf.Clamp(roadPosition.y + offset.y * mapHeight + firstStageLift, 54f, mapHeight - 54f));
    }

    private static Vector2 ResolveNodeOverlap(
        Vector2 desired,
        IReadOnlyList<Vector2> placedNodes,
        float horizontalMapScale,
        float nodeWidth,
        float nodeHeight,
        float contentWidth)
    {
        if (placedNodes.Count == 0)
        {
            return desired;
        }

        float lateralStep = 0.10f * horizontalMapScale;
        Vector2[] candidates =
        {
            Vector2.zero,
            new Vector2(-lateralStep * 1.3f, 0f),
            new Vector2(lateralStep * 1.3f, 0f),
            new Vector2(-lateralStep * 2.2f, 0f),
            new Vector2(lateralStep * 2.2f, 0f),
            new Vector2(-lateralStep * 3f, 0f),
            new Vector2(lateralStep * 3f, 0f)
        };

        float minimumHorizontalGap = (nodeWidth + 18f) / Mathf.Max(1f, contentWidth);
        float minimumVerticalGap = nodeHeight + 18f;
        foreach (Vector2 candidateOffset in candidates)
        {
            Vector2 candidate = new(
                Mathf.Clamp01(desired.x + candidateOffset.x),
                desired.y);
            bool overlaps = placedNodes.Any(placed =>
                Mathf.Abs(candidate.x - placed.x) < minimumHorizontalGap &&
                Mathf.Abs(candidate.y - placed.y) < minimumVerticalGap);
            if (!overlaps)
            {
                return candidate;
            }
        }

        return desired;
    }

    private static Vector2 GetCampaignStop(int stageIndex, int stageCount, float mapHeight)
    {
        if (stageCount <= 1)
        {
            Vector2 first = campaignRoadAnchors[0];
            return new Vector2(first.x, first.y * mapHeight);
        }

        float stageProgress = GetStageRouteProgress(stageIndex, stageCount);
        Vector2 point = GetRoadPointAtDistance(stageProgress);
        point.y *= mapHeight;
        return point;
    }

    private static float GetStageRouteProgress(int stageIndex, int stageCount)
    {
        float progress = Mathf.Clamp01(stageIndex / (float)Mathf.Max(1, stageCount - 1));
        // The opening lane already has natural room. Shift a small amount of that
        // room toward the final stretch so the final landmarks do not bunch up.
        const float pivot = 0.60f;
        const float earlyScale = 0.93f;
        float pivotProgress = pivot * earlyScale;
        return progress <= pivot
            ? progress * earlyScale
            : pivotProgress + (progress - pivot) * ((1f - pivotProgress) / (1f - pivot));
    }

    private static Vector2 GetRoadPointAtDistance(float progress)
    {
        if (campaignRoadAnchors.Length == 0) return new Vector2(0.5f, 0.5f);
        if (campaignRoadAnchors.Length == 1) return campaignRoadAnchors[0];

        float aspect = campaignMapTexture != null && campaignMapTexture.height > 0
            ? campaignMapTexture.width / (float)campaignMapTexture.height
            : 2f / 3f;
        float totalLength = 0f;
        for (int index = 1; index < campaignRoadAnchors.Length; index++)
        {
            totalLength += GetRoadSegmentLength(campaignRoadAnchors[index - 1], campaignRoadAnchors[index], aspect);
        }

        float remaining = totalLength * Mathf.Clamp01(progress);
        for (int index = 1; index < campaignRoadAnchors.Length; index++)
        {
            Vector2 from = campaignRoadAnchors[index - 1];
            Vector2 to = campaignRoadAnchors[index];
            float segmentLength = GetRoadSegmentLength(from, to, aspect);
            if (remaining <= segmentLength || index == campaignRoadAnchors.Length - 1)
            {
                float t = segmentLength <= 0.0001f ? 0f : Mathf.Clamp01(remaining / segmentLength);
                return Vector2.Lerp(from, to, t);
            }
            remaining -= segmentLength;
        }

        return campaignRoadAnchors[campaignRoadAnchors.Length - 1];
    }

    private static float GetRoadSegmentLength(Vector2 from, Vector2 to, float aspect)
    {
        return Vector2.Distance(new Vector2(from.x * aspect, from.y), new Vector2(to.x * aspect, to.y));
    }

    private void EnsureMapSurface(RectTransform contentRect, float mapHeight)
    {
        Transform fieldExisting = contentRect.Find("StageRouteField");
        Image field;
        if (fieldExisting != null && fieldExisting.TryGetComponent(out field))
        {
            field.gameObject.SetActive(true);
        }
        else
        {
            var go = new GameObject("StageRouteField", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(contentRect, false);
            field = go.GetComponent<Image>();
            field.raycastTarget = false;
        }

        ModernWafuuPresentation.ApplyFlatSurface(field, new Color(0.12f, 0.08f, 0.04f, 0.88f));
        RectTransform fieldRect = field.rectTransform;
        fieldRect.anchorMin = Vector2.zero;
        fieldRect.anchorMax = Vector2.one;
        fieldRect.offsetMin = Vector2.zero;
        fieldRect.offsetMax = Vector2.zero;
        field.transform.SetAsFirstSibling();

        if (campaignMapTexture == null)
        {
            campaignMapTexture = Resources.Load<Texture2D>("UI/ModernWafuu/Campaign/MarchMapCampaignForestSoft")
                ?? Resources.Load<Texture2D>("UI/ModernWafuu/Campaign/MarchMapMasterSpacious")
                ?? Resources.Load<Texture2D>("UI/ModernWafuu/MarchMapCampaign");
        }

        Transform existing = contentRect.Find("StageRouteMasterMap");
        RawImage masterMap;
        if (existing != null && existing.TryGetComponent(out masterMap))
        {
            masterMap.gameObject.SetActive(true);
        }
        else
        {
            if (existing != null) Destroy(existing.gameObject);
            var go = new GameObject("StageRouteMasterMap", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(contentRect, false);
            masterMap = go.GetComponent<RawImage>();
            masterMap.raycastTarget = false;
        }

        masterMap.texture = campaignMapTexture;
        if (campaignMapTexture != null) campaignMapTexture.filterMode = FilterMode.Bilinear;
        masterMap.uvRect = new Rect(0f, 0f, 1f, 1f);
        masterMap.color = Color.white;
        RectTransform mapRect = masterMap.rectTransform;
        // Keep the source map at a uniform scale. The scroll viewport crops its
        // wider sides, instead of stretching the landscape into a tall strip.
        float aspect = campaignMapTexture != null && campaignMapTexture.height > 0
            ? campaignMapTexture.width / (float)campaignMapTexture.height
            : 2f / 3f;
        mapRect.anchorMin = new Vector2(0.5f, 0f);
        mapRect.anchorMax = new Vector2(0.5f, 1f);
        mapRect.pivot = new Vector2(0.5f, 0.5f);
        mapRect.anchoredPosition = Vector2.zero;
        mapRect.sizeDelta = new Vector2(mapHeight * aspect, 0f);
        mapRect.localScale = Vector3.one;
        masterMap.transform.SetSiblingIndex(1);
    }

    private static float GetMapWidth(float mapHeight)
    {
        float aspect = campaignMapTexture != null && campaignMapTexture.height > 0
            ? campaignMapTexture.width / (float)campaignMapTexture.height
            : 2f / 3f;
        return mapHeight * aspect;
    }

    private void ScrollToFocus(RectTransform contentRect, int focusIndex, float mapHeight, int stageCount)
    {
        var scrollRect = GetComponentInChildren<ScrollRect>();
        RectTransform viewport = scrollRect != null && scrollRect.viewport != null
            ? scrollRect.viewport
            : contentRect.parent as RectTransform;

        if (viewport == null)
        {
            return;
        }

        float maxY = Mathf.Max(0f, contentRect.rect.height - viewport.rect.height);
        Vector2 focus = GetCampaignStop(focusIndex, stageCount, mapHeight);
        float focusYFromTop = mapHeight - focus.y;
        float targetY = Mathf.Clamp(focusYFromTop - viewport.rect.height * 0.48f, 0f, maxY);
        contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, targetY);
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = maxY <= 0f ? 1f : 1f - targetY / maxY;
        }
    }

    private void EnsureNextStageButton(StageData stage)
    {
        if (nextStageButton == null)
        {
            Transform existing = transform.Find("NextStageButton");
            if (existing != null)
            {
                nextStageButton = existing.GetComponent<Button>();
                nextStageLabel = existing.GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        if (nextStageButton == null)
        {
            var go = new GameObject("NextStageButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(Outline));
            go.transform.SetParent(transform, false);
            nextStageButton = go.GetComponent<Button>();

            var label = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            label.transform.SetParent(go.transform, false);
            nextStageLabel = label.GetComponent<TextMeshProUGUI>();
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 2f);
            labelRect.offsetMax = new Vector2(-12f, -2f);
            UnityUIRuntimeTheme.EnsureJapaneseCapableFont(nextStageLabel);
            nextStageLabel.alignment = TextAlignmentOptions.Center;
            nextStageLabel.fontStyle = FontStyles.Bold;
            nextStageLabel.enableAutoSizing = true;
            nextStageLabel.fontSizeMin = UnityUIRuntimeTheme.MinimumTextSize;
            nextStageLabel.fontSizeMax = 23f;
            nextStageLabel.color = new Color(1f, 0.96f, 0.82f);
            nextStageLabel.outlineColor = new Color(0.07f, 0.04f, 0.02f, 0.98f);
            nextStageLabel.outlineWidth = 0.20f;
            nextStageLabel.raycastTarget = false;
        }

        if (stage == null)
        {
            nextStageButton.gameObject.SetActive(false);
            return;
        }

        nextStageButton.gameObject.SetActive(true);
        ModernWafuuPresentation.ApplyButtonTreatment(nextStageButton);
        var rect = nextStageButton.GetComponent<RectTransform>();
        bool portrait = UnityUIRuntimeTheme.IsPortraitNarrowScreen();
        rect.anchorMin = portrait ? new Vector2(0.04f, 0.77f) : new Vector2(0.06f, 0.74f);
        rect.anchorMax = portrait ? new Vector2(0.96f, 0.85f) : new Vector2(0.94f, 0.83f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        nextStageLabel.text = $"次の局  {stage.stageId:00}局  {stage.stageName}";
        nextStageButton.onClick.RemoveAllListeners();
        nextStageButton.onClick.AddListener(() =>
        {
            ShowStageDetails(stage, false);
        });
    }

    private void ShowStageDetails(StageData stage, bool locked)
    {
        if (stage == null)
        {
            return;
        }

        inspectedStage = stage;
        inspectedStageLocked = locked;
        EnsureStageInformationRegion();
        if (stageDetailOverlay != null)
        {
            stageDetailOverlay.SetActive(false);
        }

        int stagePoints = GameManager.Instance != null
            ? GameManager.Instance.GetEffectiveStagePointReward(stage.rewardStagePoints)
            : stage.rewardStagePoints;
        int experience = PlayerInventory.Instance != null
            ? PlayerInventory.Instance.GetEffectiveBattleExperienceReward(stage)
            : PlayerInventory.BaseBattleExperience;
        bool cleared = GameManager.Instance != null && GameManager.Instance.IsStageCleared(stage.stageId);
        string boss = stage.isBossStage ? "  首領戦" : string.Empty;
        IReadOnlyList<CharacterData> enemies = cleared ? GetStageEnemyPreview(stage) : null;
        string body = cleared
            ? $"報酬　戦果 +{stagePoints} / 経験 +{experience}"
            : $"敵軍　未詳\n報酬　戦果 +{stagePoints} / 経験 +{experience}";

        stageInformationRegion.Configure(
            $"第{stage.stageId:00}局　{stage.stageName}{boss}",
            body);
        stageInformationRegion.ConfigurePieceRows(null, enemies);
        ConfigureStageDeployButton(stage, locked);

        if (stageRectsByData.TryGetValue(stage, out RectTransform stageRect))
        {
            if (marchEnemyVfx == null)
            {
                marchEnemyVfx = GetComponent<MarchEnemyVfx>();
                if (marchEnemyVfx == null)
                {
                    marchEnemyVfx = gameObject.AddComponent<MarchEnemyVfx>();
                }
            }
            marchEnemyVfx.Configure(content, stageRect, cleared ? GetStageEnemyPreview(stage) : null);
        }
    }

    private void ConfigureStageDeployButton(StageData stage, bool locked)
    {
        if (stageDeployButton != null)
        {
            stageDeployButton.gameObject.SetActive(false);
        }

        if (stage == null || locked)
        {
            return;
        }

        if (stageDeployButton == null)
        {
            var actionObject = new GameObject("StageDeployButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            actionObject.transform.SetParent(transform, false);
            stageDeployButton = actionObject.GetComponent<Button>();

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(actionObject.transform, false);
            stageDeployLabel = labelObject.GetComponent<TextMeshProUGUI>();
            UnityUIRuntimeTheme.EnsureJapaneseCapableFont(stageDeployLabel);
            stageDeployLabel.text = "出陣";
            stageDeployLabel.fontStyle = FontStyles.Bold;
            stageDeployLabel.alignment = TextAlignmentOptions.Center;
            stageDeployLabel.enableAutoSizing = true;
            stageDeployLabel.fontSizeMin = 21f;
            stageDeployLabel.fontSizeMax = 30f;
            stageDeployLabel.color = new Color(1f, 0.95f, 0.80f);
            stageDeployLabel.outlineColor = new Color(0.08f, 0.04f, 0.02f, 0.98f);
            stageDeployLabel.outlineWidth = 0.20f;
            stageDeployLabel.raycastTarget = false;
            RectTransform labelRect = stageDeployLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(5f, 2f);
            labelRect.offsetMax = new Vector2(-5f, -2f);
        }

        stageDeployButton.gameObject.SetActive(true);
        stageDeployButton.transform.SetAsLastSibling();
        ApplyDeployButtonTreatment(stageDeployButton);

        RectTransform actionRect = stageDeployButton.GetComponent<RectTransform>();
        actionRect.anchorMin = new Vector2(0.08f, 0.67f);
        actionRect.anchorMax = new Vector2(0.92f, 0.73f);
        actionRect.offsetMin = Vector2.zero;
        actionRect.offsetMax = Vector2.zero;

        stageDeployButton.onClick.RemoveAllListeners();
        stageDeployButton.onClick.AddListener(() =>
        {
            GameManager.Instance.SetSelectedStage(stage);
            UIManager.Instance.ShowFormation();
        });
    }

    private static void ApplyDeployButtonTreatment(Button button)
    {
        if (button == null)
        {
            return;
        }

        var image = button.GetComponent<Image>();
        ModernWafuuPresentation.ApplyFlatSurface(image, new Color(0.67f, 0.10f, 0.06f, 1f));
        var outline = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.74f, 0.30f, 0.88f);
        outline.effectDistance = new Vector2(2f, -2f);

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.89f, 0.73f, 1f);
        colors.pressedColor = new Color(0.86f, 0.68f, 0.50f, 1f);
        colors.disabledColor = new Color(0.45f, 0.31f, 0.27f, 0.95f);
        button.colors = colors;
    }

    private void EnsureStageInformationRegion()
    {
        stageInformationRegion = ScreenInfoRegion.Ensure(
            transform,
            "StageInformationRegion",
            new Vector2(0.04f, 0.75f),
            new Vector2(0.96f, 0.90f));
    }

    private string GetStageUnlockText(StageData stage)
    {
        if (stage == null || GameManager.Instance == null)
        {
            return "未解放";
        }

        if (!GameManager.Instance.IsChapterUnlocked(stage.chapterId))
        {
            return $"未解放  第{stage.chapterId}章の到達が必要";
        }

        StageData previous = stageDatabase != null
            ? stageDatabase.stages.Where(item => item != null && item.stageId == stage.stageId - 1).FirstOrDefault()
            : null;
        return previous != null
            ? $"未解放  前局「{previous.stageName}」の勝利が必要"
            : "未解放";
    }

    private static IReadOnlyList<CharacterData> GetStageEnemyPreview(StageData stage)
    {
        if (stage == null)
        {
            return null;
        }

        return stage.enemyPool
            .Concat(stage.reinforcementEnemy ?? Enumerable.Empty<CharacterData>())
            .Where(character => character != null)
            .ToArray();
    }

    private void EnsureStageDetailOverlay()
    {
        if (stageDetailOverlay != null)
        {
            return;
        }

        var scrim = new GameObject("StageDetailOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        scrim.transform.SetParent(transform, false);
        stageDetailOverlay = scrim;
        var scrimImage = scrim.GetComponent<Image>();
        scrimImage.color = new Color(0.10f, 0.08f, 0.05f, 0.22f);
        scrimImage.raycastTarget = true;
        RectTransform scrimRect = scrim.GetComponent<RectTransform>();
        scrimRect.anchorMin = Vector2.zero;
        scrimRect.anchorMax = Vector2.one;
        scrimRect.offsetMin = Vector2.zero;
        scrimRect.offsetMax = Vector2.zero;

        var panelObject = new GameObject("StageDetailPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        panelObject.transform.SetParent(scrim.transform, false);
        var panelImage = panelObject.GetComponent<Image>();
        ModernWafuuPresentation.ApplyFlatSurface(panelImage, new Color(0.96f, 0.92f, 0.81f, 0.99f));
        var panelOutline = panelObject.GetComponent<Outline>();
        panelOutline.effectColor = new Color(0.20f, 0.13f, 0.08f, 0.8f);
        panelOutline.effectDistance = new Vector2(2f, -2f);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        bool portrait = UnityUIRuntimeTheme.IsPortraitNarrowScreen();
        panelRect.anchorMin = portrait ? new Vector2(0.05f, 0.26f) : new Vector2(0.23f, 0.25f);
        panelRect.anchorMax = portrait ? new Vector2(0.95f, 0.72f) : new Vector2(0.77f, 0.75f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        stageDetailTitle = CreateDetailText("Title", panelObject.transform, new Vector2(0.08f, 0.76f), new Vector2(0.82f, 0.93f), 28f, 16f, TextAlignmentOptions.Left, new Color(0.20f, 0.12f, 0.07f));
        stageDetailTitle.fontStyle = FontStyles.Bold;
        stageDetailBody = CreateDetailText("Body", panelObject.transform, new Vector2(0.08f, 0.21f), new Vector2(0.92f, 0.72f), 20f, 13f, TextAlignmentOptions.TopLeft, new Color(0.19f, 0.16f, 0.11f));
        stageDetailBody.textWrappingMode = TextWrappingModes.Normal;

        var actionObject = new GameObject("Action", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        actionObject.transform.SetParent(panelObject.transform, false);
        stageDetailAction = actionObject.GetComponent<Button>();
        ModernWafuuPresentation.ApplyButtonTreatment(stageDetailAction);
        RectTransform actionRect = actionObject.GetComponent<RectTransform>();
        actionRect.anchorMin = new Vector2(0.38f, 0.05f);
        actionRect.anchorMax = new Vector2(0.92f, 0.17f);
        actionRect.offsetMin = Vector2.zero;
        actionRect.offsetMax = Vector2.zero;
        stageDetailActionLabel = CreateDetailText("Label", actionObject.transform, Vector2.zero, Vector2.one, 20f, 12f, TextAlignmentOptions.Center, new Color(1f, 0.95f, 0.82f));
        stageDetailActionLabel.fontStyle = FontStyles.Bold;
        stageDetailAction.onClick.AddListener(() =>
        {
            if (inspectedStage == null || inspectedStageLocked)
            {
                return;
            }

            GameManager.Instance.SetSelectedStage(inspectedStage);
            UIManager.Instance.ShowFormation();
        });

        var closeObject = new GameObject("Close", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeObject.transform.SetParent(panelObject.transform, false);
        var closeButton = closeObject.GetComponent<Button>();
        ModernWafuuPresentation.ApplyFlatSurface(closeObject.GetComponent<Image>(), new Color(0.27f, 0.17f, 0.10f, 0.95f));
        RectTransform closeRect = closeObject.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.85f, 0.78f);
        closeRect.anchorMax = new Vector2(0.94f, 0.92f);
        closeRect.offsetMin = Vector2.zero;
        closeRect.offsetMax = Vector2.zero;
        var closeLabel = CreateDetailText("Label", closeObject.transform, Vector2.zero, Vector2.one, 19f, 12f, TextAlignmentOptions.Center, new Color(1f, 0.95f, 0.82f));
        closeLabel.text = "×";
        closeButton.onClick.AddListener(() => stageDetailOverlay.SetActive(false));
    }

    private static TextMeshProUGUI CreateDetailText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, float maxSize, float minSize, TextAlignmentOptions alignment, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        var rect = text.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
        text.enableAutoSizing = true;
        text.fontSizeMax = maxSize;
        text.fontSizeMin = Mathf.Max(UnityUIRuntimeTheme.MinimumTextSize, minSize);
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }
}
