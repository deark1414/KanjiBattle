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
            listRect.anchorMin = new Vector2(0.04f, 0.10f);
            listRect.anchorMax = new Vector2(0.96f, 0.64f);
            listRect.anchoredPosition = Vector2.zero;
            listRect.sizeDelta = Vector2.zero;
        }

        var grid = content != null ? content.GetComponent<GridLayoutGroup>() : null;
        if (grid != null)
        {
            grid.cellSize = new Vector2(520f, 108f);
            grid.spacing = new Vector2(12f, 12f);
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
}
