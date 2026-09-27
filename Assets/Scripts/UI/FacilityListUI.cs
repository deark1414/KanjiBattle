using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(200)]
public class FacilityListUI : MonoBehaviour
{
    private const float HorizontalPadding = 10f;
    private const float CardSpacing = 16f;
    private const float CardHeight = 224f;
    private const float MinCardWidth = 220f;

    [SerializeField] private Transform contentParent;
    [SerializeField] private FacilityUI facilityPrefab;

    private readonly List<FacilityUI> spawnedFacilities = new();
    private ScrollRect scrollRect;
    private GameObject facilityDetailOverlay;
    private TextMeshProUGUI facilityDetailTitle;
    private TextMeshProUGUI facilityDetailBody;
    private Button facilityDetailAction;
    private TextMeshProUGUI facilityDetailActionLabel;
    private FacilityUI inspectedFacility;
    private ScreenInfoRegion facilityInformationRegion;
    private bool isFacilityChangeSubscribed;

    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
    }

    private void Start()
    {
        SubscribeToFacilityChanges();
        ApplyListLayout();
        EnsureFacilityInformationRegion();
        RefreshList();
    }

    private void OnEnable()
    {
        SubscribeToFacilityChanges();
        ApplyListLayout();
        RefreshList();
    }

    private void OnDisable()
    {
        if (isFacilityChangeSubscribed && FacilityManager.Instance != null)
        {
            FacilityManager.Instance.OnFacilitiesChanged -= RefreshList;
        }
        isFacilityChangeSubscribed = false;
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        ApplyListLayout();
    }

    private void SubscribeToFacilityChanges()
    {
        if (isFacilityChangeSubscribed || FacilityManager.Instance == null)
        {
            return;
        }

        FacilityManager.Instance.OnFacilitiesChanged += RefreshList;
        isFacilityChangeSubscribed = true;
    }

    public void RefreshList()
    {
        if (FacilityManager.Instance == null || contentParent == null || facilityPrefab == null)
        {
            return;
        }

        ApplyListLayout();

        foreach (var ui in spawnedFacilities)
        {
            if (ui != null)
            {
                Destroy(ui.gameObject);
            }
        }
        spawnedFacilities.Clear();

        foreach (var facility in FacilityManager.Instance.GetFacilities())
        {
            var ui = Instantiate(facilityPrefab, contentParent);
            ui.Setup(facility);
            ui.SetSelectionHandler(ShowFacilityDetails);
            spawnedFacilities.Add(ui);
        }

        ApplyListLayout();
        FinalizeContentLayout();
        ShowFacilityOverview();
    }

    private void FinalizeContentLayout()
    {
        var contentRect = contentParent as RectTransform;
        if (contentRect == null) return;

        int columns = GetColumnCount(GetContentWidth(contentRect));
        int rows = Mathf.CeilToInt(spawnedFacilities.Count / (float)columns);
        float height = rows > 0 ? rows * CardHeight + Mathf.Max(0, rows - 1) * CardSpacing + 10f : 0f;
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, height);
        contentRect.anchoredPosition = Vector2.zero;
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    public bool OwnsContent(Transform target)
    {
        if (target == null || contentParent == null)
        {
            return false;
        }

        return target == contentParent || target.IsChildOf(contentParent) || contentParent.IsChildOf(target);
    }

    private void ApplyListLayout()
    {
        if (scrollRect == null)
        {
            scrollRect = GetComponent<ScrollRect>();
        }

        var listRect = GetComponent<RectTransform>();
        if (listRect != null)
        {
            listRect.anchorMin = new Vector2(0.04f, 0.10f);
            listRect.anchorMax = new Vector2(0.96f, 0.68f);
            listRect.anchoredPosition = Vector2.zero;
            listRect.sizeDelta = Vector2.zero;
        }

        Canvas.ForceUpdateCanvases();
        ForceFacilityContent(contentParent);
    }

    private void ForceFacilityContent(Transform target)
    {
        if (target == null) return;

        foreach (var vertical in target.GetComponents<VerticalLayoutGroup>())
        {
            // A GridLayoutGroup is used below. Unity refuses to add it while any
            // other layout group remains on the same generated content object.
            DestroyImmediate(vertical);
        }

        foreach (var horizontal in target.GetComponents<HorizontalLayoutGroup>())
        {
            DestroyImmediate(horizontal);
        }

        foreach (var fitter in target.GetComponents<ResponsiveGridFitter>())
        {
            fitter.enabled = false;
        }

        var rect = target as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(HorizontalPadding, rect.offsetMin.y);
            rect.offsetMax = new Vector2(-HorizontalPadding, rect.offsetMax.y);
        }

        var contentFitter = target.GetComponent<ContentSizeFitter>();
        if (contentFitter != null)
        {
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        var grid = target.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = target.gameObject.AddComponent<GridLayoutGroup>();
        }

        Canvas.ForceUpdateCanvases();
        ApplyFacilityGrid(grid, GetContentWidth(rect));
    }

    private float GetContentWidth(RectTransform contentRect)
    {
        RectTransform viewport = scrollRect != null ? scrollRect.viewport : null;
        if (viewport != null && viewport.rect.width > 0f)
        {
            return Mathf.Max(0f, viewport.rect.width - HorizontalPadding * 2f);
        }

        var parentRect = contentRect != null ? contentRect.parent as RectTransform : null;
        if (parentRect != null && parentRect.rect.width > 0f)
        {
            return Mathf.Max(0f, parentRect.rect.width - HorizontalPadding * 2f);
        }

        if (contentRect != null && contentRect.rect.width > 0f)
        {
            return Mathf.Max(0f, contentRect.rect.width - HorizontalPadding * 2f);
        }

        var ownRect = GetComponent<RectTransform>();
        return ownRect != null ? Mathf.Max(0f, ownRect.rect.width - HorizontalPadding * 2f) : MinCardWidth * 2f + CardSpacing;
    }

    private static void ApplyFacilityGrid(GridLayoutGroup grid, float contentWidth)
    {
        if (grid == null) return;

        int columns = GetColumnCount(contentWidth);
        float cardWidth = columns == 1
            ? Mathf.Max(MinCardWidth, contentWidth)
            : Mathf.Max(MinCardWidth, (contentWidth - CardSpacing) * 0.5f);
        grid.padding = new RectOffset(0, 0, 5, 0);
        grid.cellSize = new Vector2(cardWidth, CardHeight);
        grid.spacing = new Vector2(CardSpacing, CardSpacing);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.childAlignment = TextAnchor.UpperCenter;
    }

    private static int GetColumnCount(float contentWidth)
    {
        return 2;
    }

    private void ShowFacilityDetails(FacilityUI facility)
    {
        if (facility == null || facility.Facility == null)
        {
            return;
        }

        inspectedFacility = facility;
        EnsureFacilityInformationRegion();
        if (facilityDetailOverlay != null)
        {
            facilityDetailOverlay.SetActive(false);
        }
        FacilityData data = facility.Facility;
        facilityInformationRegion.Configure(
            data.facilityName,
            $"{GetBriefDescription(facility.DetailDescription)}\n{facility.DetailStatus}",
            facility.DetailActionLabel,
            facility.CanExecuteAction,
            facility.CanExecuteAction ? () =>
            {
                facility.ExecuteActionFromDetail();
                ShowFacilityDetails(facility);
            } : null);
    }

    private void EnsureFacilityInformationRegion()
    {
        Transform host = transform.parent != null ? transform.parent : transform;
        facilityInformationRegion = ScreenInfoRegion.Ensure(
            host,
            "FacilityInformationRegion",
            new Vector2(0.04f, 0.72f),
            new Vector2(0.96f, 0.90f));
    }

    private void ShowFacilityOverview()
    {
        if (facilityInformationRegion == null)
        {
            return;
        }

        facilityInformationRegion.Configure(
            "城下の政",
            "施設を選ぶと、効果と次にできることを確認できます。");
    }

    private static string GetBriefDescription(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        int end = text.IndexOf('。');
        return end >= 0 ? text[..(end + 1)] : text;
    }

    private static string GetStageName(int stageId)
    {
        if (stageId < 0)
        {
            return "条件なし";
        }

        StageData stage = StageDatabase.Instance != null ? StageDatabase.Instance.GetStageById(stageId) : null;
        return stage != null ? $"{stage.stageId:00}局 {stage.stageName}" : $"{stageId:00}局到達";
    }

    private void EnsureFacilityDetailOverlay()
    {
        if (facilityDetailOverlay != null)
        {
            return;
        }

        Transform host = transform.parent != null ? transform.parent : transform;
        var scrim = new GameObject("FacilityDetailOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        scrim.transform.SetParent(host, false);
        facilityDetailOverlay = scrim;
        var scrimImage = scrim.GetComponent<Image>();
        scrimImage.color = new Color(0.10f, 0.08f, 0.05f, 0.20f);
        scrimImage.raycastTarget = true;
        RectTransform scrimRect = scrim.GetComponent<RectTransform>();
        scrimRect.anchorMin = Vector2.zero;
        scrimRect.anchorMax = Vector2.one;
        scrimRect.offsetMin = Vector2.zero;
        scrimRect.offsetMax = Vector2.zero;

        var panelObject = new GameObject("FacilityDetailPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        panelObject.transform.SetParent(scrim.transform, false);
        ModernWafuuPresentation.ApplyFlatSurface(panelObject.GetComponent<Image>(), new Color(0.96f, 0.92f, 0.81f, 0.99f));
        var outline = panelObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.20f, 0.13f, 0.08f, 0.8f);
        outline.effectDistance = new Vector2(2f, -2f);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        bool portrait = UnityUIRuntimeTheme.IsPortraitNarrowScreen();
        panelRect.anchorMin = portrait ? new Vector2(0.05f, 0.23f) : new Vector2(0.24f, 0.22f);
        panelRect.anchorMax = portrait ? new Vector2(0.95f, 0.75f) : new Vector2(0.76f, 0.78f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        facilityDetailTitle = CreateDetailText("Title", panelObject.transform, new Vector2(0.08f, 0.79f), new Vector2(0.82f, 0.93f), 28f, 16f, TextAlignmentOptions.Left, new Color(0.20f, 0.12f, 0.07f));
        facilityDetailTitle.fontStyle = FontStyles.Bold;
        facilityDetailBody = CreateDetailText("Body", panelObject.transform, new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.75f), 19f, 12f, TextAlignmentOptions.TopLeft, new Color(0.19f, 0.16f, 0.11f));
        facilityDetailBody.textWrappingMode = TextWrappingModes.Normal;

        var actionObject = new GameObject("Action", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        actionObject.transform.SetParent(panelObject.transform, false);
        facilityDetailAction = actionObject.GetComponent<Button>();
        ModernWafuuPresentation.ApplyButtonTreatment(facilityDetailAction);
        RectTransform actionRect = actionObject.GetComponent<RectTransform>();
        actionRect.anchorMin = new Vector2(0.38f, 0.05f);
        actionRect.anchorMax = new Vector2(0.92f, 0.17f);
        actionRect.offsetMin = Vector2.zero;
        actionRect.offsetMax = Vector2.zero;
        facilityDetailActionLabel = CreateDetailText("Label", actionObject.transform, Vector2.zero, Vector2.one, 20f, 12f, TextAlignmentOptions.Center, new Color(1f, 0.95f, 0.82f));
        facilityDetailActionLabel.fontStyle = FontStyles.Bold;
        facilityDetailAction.onClick.AddListener(() =>
        {
            if (inspectedFacility == null || !inspectedFacility.CanExecuteAction)
            {
                return;
            }

            inspectedFacility.ExecuteActionFromDetail();
            ShowFacilityDetails(inspectedFacility);
        });

        var closeObject = new GameObject("Close", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        closeObject.transform.SetParent(panelObject.transform, false);
        ModernWafuuPresentation.ApplyFlatSurface(closeObject.GetComponent<Image>(), new Color(0.27f, 0.17f, 0.10f, 0.95f));
        RectTransform closeRect = closeObject.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0.85f, 0.80f);
        closeRect.anchorMax = new Vector2(0.94f, 0.93f);
        closeRect.offsetMin = Vector2.zero;
        closeRect.offsetMax = Vector2.zero;
        var closeLabel = CreateDetailText("Label", closeObject.transform, Vector2.zero, Vector2.one, 19f, 12f, TextAlignmentOptions.Center, new Color(1f, 0.95f, 0.82f));
        closeLabel.text = "×";
        closeObject.GetComponent<Button>().onClick.AddListener(() => facilityDetailOverlay.SetActive(false));
    }

    private static TextMeshProUGUI CreateDetailText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, float maxSize, float minSize, TextAlignmentOptions alignment, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        RectTransform rect = text.rectTransform;
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
