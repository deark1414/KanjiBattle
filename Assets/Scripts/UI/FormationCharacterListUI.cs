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
            ui.SetCharacter(kv.Key, PlayerInventory.Instance.PlayerLevel);
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
        if (!PlayerInventory.Instance.GetOwnedCharacters().ContainsKey(character))
        {
            Debug.Log($"{character.characterName} を所持していません");
            return;
        }

        FormationUI.Instance.ShowCharacterDetails(character);

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
            // The utility action now lives beside the active piece, so the owned
            // list can begin directly beneath the formation tray on every shape.
            listRect.anchorMin = new Vector2(0.04f, 0.085f);
            // The formation's visible artwork ends well above the structural tray
            // boundary, so start the owned-piece region from that visual edge
            // instead of leaving an empty card-sized band between the two.
            listRect.anchorMax = new Vector2(0.96f, 0.520f);
            listRect.anchoredPosition = Vector2.zero;
            listRect.sizeDelta = Vector2.zero;
        }

        var grid = content != null ? content.GetComponent<GridLayoutGroup>() : null;
        if (grid != null)
        {
            grid.cellSize = new Vector2(GetListCellWidth(), UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? 98f : 96f);
            grid.spacing = new Vector2(0f, 8f);
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 1;
            grid.childAlignment = TextAnchor.UpperCenter;
        }

        if (content is RectTransform contentRect)
        {
            contentRect.offsetMin = new Vector2(0f, contentRect.offsetMin.y);
            contentRect.offsetMax = new Vector2(0f, 0f);
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
            return Mathf.Clamp(listRect.rect.width, 280f, 620f);
        }

        return UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? 320f : 520f;
    }
}
