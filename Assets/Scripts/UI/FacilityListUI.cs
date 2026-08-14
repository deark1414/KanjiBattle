using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(200)]
public class FacilityListUI : MonoBehaviour
{
    private const float HorizontalPadding = 10f;
    private const float CardSpacing = 16f;
    private const float CardHeight = 132f;
    private const float MinCardWidth = 250f;

    [SerializeField] private Transform contentParent;
    [SerializeField] private FacilityUI facilityPrefab;

    private readonly List<FacilityUI> spawnedFacilities = new();
    private ScrollRect scrollRect;

    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
    }

    private void Start()
    {
        ApplyListLayout();
        RefreshList();
    }

    private void OnEnable()
    {
        ApplyListLayout();
        RefreshList();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        ApplyListLayout();
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
            spawnedFacilities.Add(ui);
        }

        ApplyListLayout();
        FinalizeContentLayout();
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
            listRect.anchorMin = UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? new Vector2(0.03f, 0.10f) : new Vector2(0.04f, 0.12f);
            listRect.anchorMax = UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? new Vector2(0.97f, 0.86f) : new Vector2(0.96f, 0.88f);
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
            vertical.enabled = false;
        }

        foreach (var horizontal in target.GetComponents<HorizontalLayoutGroup>())
        {
            horizontal.enabled = false;
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
        return UnityUIRuntimeTheme.IsPortraitNarrowScreen() || contentWidth < MinCardWidth * 2f + CardSpacing
            ? 1
            : 2;
    }
}
