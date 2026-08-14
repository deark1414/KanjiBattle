using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(GridLayoutGroup))]
public sealed class ResponsiveGridFitter : MonoBehaviour
{
    [SerializeField] private float minimumCellWidth = 220f;
    [SerializeField] private float cellHeight = 72f;
    [SerializeField] private int maximumColumns = 2;
    [SerializeField] private float spacing = 12f;

    private GridLayoutGroup grid;
    private RectTransform rectTransform;

    private void Awake()
    {
        CacheComponents();
    }

    private void OnEnable()
    {
        ConfigureFromName();
        Refresh();
    }

    private void OnRectTransformDimensionsChange()
    {
        Refresh();
    }

    public void ConfigureFromName()
    {
        string lowerName = GetHierarchyName(transform);

        if (lowerName.Contains("facility") || HasFacilityListOwner(transform) || HasFacilityChildren(transform))
        {
            minimumCellWidth = 250f;
            cellHeight = 132f;
            maximumColumns = 2;
        }
        else if (lowerName.Contains("stagelist") || lowerName.Contains("stageselect"))
        {
            minimumCellWidth = 180f;
            cellHeight = 64f;
            maximumColumns = 5;
        }
        else if (lowerName.Contains("slot"))
        {
            minimumCellWidth = 120f;
            cellHeight = 58f;
            maximumColumns = 6;
        }
        else if (lowerName.Contains("character"))
        {
            minimumCellWidth = 340f;
            cellHeight = 104f;
            maximumColumns = 2;
        }
    }

    public void Refresh()
    {
        CacheComponents();
        if (grid == null || rectTransform == null) return;

        if (IsFacilityGrid())
        {
            minimumCellWidth = 250f;
            cellHeight = 132f;
            maximumColumns = 2;
            spacing = 16f;
        }

        float availableWidth = rectTransform.rect.width - grid.padding.left - grid.padding.right;
        if (availableWidth <= 0f) return;

        int effectiveMaximumColumns = GetEffectiveMaximumColumns();
        int columnsByWidth = Mathf.Max(1, Mathf.FloorToInt((availableWidth + spacing) / (minimumCellWidth + spacing)));
        int columns = Mathf.Max(1, Mathf.Min(effectiveMaximumColumns, columnsByWidth));
        float cellWidth = (availableWidth - spacing * (columns - 1)) / columns;

        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.spacing = new Vector2(spacing, spacing);
        grid.cellSize = new Vector2(Mathf.Max(1f, cellWidth), cellHeight);
        grid.childAlignment = TextAnchor.UpperCenter;
    }

    private int GetEffectiveMaximumColumns()
    {
        if (IsFacilityGrid())
        {
            return 2;
        }

        return Mathf.Max(1, maximumColumns);
    }

    private bool IsFacilityGrid()
    {
        string lowerName = GetHierarchyName(transform);
        return lowerName.Contains("facility") || HasFacilityListOwner(transform) || HasFacilityChildren(transform);
    }

    private void CacheComponents()
    {
        if (grid == null) grid = GetComponent<GridLayoutGroup>();
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
    }

    private static bool HasFacilityListOwner(Transform target)
    {
        foreach (var list in FindObjectsByType<FacilityListUI>(FindObjectsInactive.Include))
        {
            if (list != null && list.OwnsContent(target))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasFacilityChildren(Transform target)
    {
        if (target == null) return false;

        foreach (Transform child in target)
        {
            if (child.GetComponent<FacilityUI>() != null)
            {
                return true;
            }
        }

        return false;
    }

    private static string GetHierarchyName(Transform target)
    {
        if (target == null) return string.Empty;

        string path = target.name;
        Transform current = target.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path.ToLowerInvariant();
    }
}
