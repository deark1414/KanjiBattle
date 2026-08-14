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
            listRect.anchorMin = new Vector2(0.04f, 0.10f);
            listRect.anchorMax = new Vector2(0.96f, 0.72f);
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
