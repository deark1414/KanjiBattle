using UnityEngine;
using UnityEngine.UI;

public class FormationCharacterListUI : MonoBehaviour
{
    public static FormationCharacterListUI instance;

    [SerializeField] private Transform content;
    [SerializeField] private GameObject characterEntryPrefab;

    private void Awake()
    {
        instance = this;
    }

    private void OnEnable()
    {
        ApplyListLayout();
        DisplayCharacters();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        ApplyListLayout();
    }

    public void DisplayCharacters()
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
            var ui = entry.GetComponent<CharacterEntryForFormationUI>();
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

    public void SelectCharacter(CharacterData character)
    {
        if (!PlayerInventory.Instance.GetOwnedCharacters().TryGetValue(character, out var info) || info.count <= 0)
        {
            Debug.Log($"{character.characterName} を所持していません");
            return;
        }

        int used = 0;
        foreach (var selected in FormationUI.Instance.GetFormation())
        {
            if (selected == character)
            {
                used++;
            }
        }

        if (used >= info.count)
        {
            Debug.Log($"{character.characterName} は所持数以上に編成できません");
            return;
        }

        var formation = FormationUI.Instance.GetFormation();
        for (int i = 0; i < formation.Length; i++)
        {
            if (formation[i] == null)
            {
                FormationUI.Instance.SetCharacterToSlot(i, character);
                return;
            }
        }

        Debug.Log("空きスロットがありません");
    }

    private void ApplyListLayout()
    {
        var listRect = GetComponent<RectTransform>();
        if (listRect != null)
        {
            listRect.anchorMin = UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? new Vector2(0.03f, 0.19f) : new Vector2(0.04f, 0.10f);
            listRect.anchorMax = UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? new Vector2(0.97f, 0.72f) : new Vector2(0.96f, 0.74f);
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
