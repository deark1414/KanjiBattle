using UnityEngine;
using UnityEngine.UI;

public class CharacterListUI : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private GameObject characterEntryPrefab;

    private void Start()
    {
        ApplyListLayout();
        RefreshList();
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.onInventoryChanged += RefreshList;
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCostModifiersChanged += RefreshList;
        }
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

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.onInventoryChanged -= RefreshList;
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCostModifiersChanged -= RefreshList;
        }
    }

    public void RefreshList()
    {
        if (PlayerInventory.Instance == null || content == null)
        {
            return;
        }

        ApplyListLayout();

        ClearEntries();

        foreach (var kv in PlayerInventory.Instance.GetOwnedCharacters())
        {
            var entry = Instantiate(characterEntryPrefab, content);
            var ui = entry.GetComponent<CharacterEntryUI>();
            ui.SetCharacter(kv.Key, kv.Value.level, kv.Value.count);
        }
    }

    private void ClearEntries()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            var child = content.GetChild(i);
            child.gameObject.SetActive(false);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }

    private void ApplyListLayout()
    {
        var listRect = GetComponent<RectTransform>();
        if (listRect != null)
        {
            listRect.anchorMin = UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? new Vector2(0.03f, 0.15f) : new Vector2(0.04f, 0.10f);
            listRect.anchorMax = UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? new Vector2(0.97f, 0.60f) : new Vector2(0.96f, 0.66f);
            listRect.anchoredPosition = Vector2.zero;
            listRect.sizeDelta = Vector2.zero;
        }

        var grid = content != null ? content.GetComponent<GridLayoutGroup>() : null;
        if (grid != null)
        {
            grid.cellSize = new Vector2(GetListCellWidth(), UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? 92f : 104f);
            grid.spacing = UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? new Vector2(10f, 8f) : new Vector2(12f, 10f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 1;
            grid.childAlignment = TextAnchor.UpperCenter;
        }

        var fitter = content != null ? content.GetComponent<ResponsiveGridFitter>() : null;
        if (fitter != null)
        {
            fitter.enabled = false;
        }
    }

    private float GetListCellWidth()
    {
        var listRect = GetComponent<RectTransform>();
        if (listRect != null && listRect.rect.width > 0f)
        {
            return Mathf.Clamp(listRect.rect.width - 24f, 280f, 520f);
        }

        return UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? 320f : 520f;
    }
}
