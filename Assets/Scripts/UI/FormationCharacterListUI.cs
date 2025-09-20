using UnityEngine;

public class FormationCharacterListUI : MonoBehaviour
{
    public static FormationCharacterListUI instance;
    [SerializeField] private Transform content;
    [SerializeField] private GameObject characterEntryPrefab;
    [SerializeField] private FormationUI formationUI;

    private void Awake() => instance = this;

    public void DisplayCharacters()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        foreach (var kv in PlayerInventory.Instance.GetOwnedCharacters())
        {
            var entry = Instantiate(characterEntryPrefab, content);
            var ui = entry.GetComponent<CharacterEntryForFormationUI>();
            ui.SetCharacter(kv.Key, kv.Value.level, kv.Value.count);
        }
    }

    public void SelectCharacter(CharacterData character)
    {
        // 所持数チェック
        var owned = PlayerInventory.Instance.GetOwnedCharacters();
        if (!owned.ContainsKey(character) || owned[character].count <= 0)
        {
            Debug.Log($"{character.characterName} を所持していません");
            return;
        }

        // すでに編成している数を数える
        int alreadyInFormation = 0;
        foreach (var c in formationUI.GetFormation())
        {
            if (c == character) alreadyInFormation++;
        }

        if (alreadyInFormation >= owned[character].count)
        {
            Debug.Log($"{character.characterName} は所持数以上に編成できません");
            return;
        }

        // 空きスロットを探して配置
        var slots = formationUI.GetFormation();
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                formationUI.SetCharacterToSlot(i, character);
                return;
            }
        }

        Debug.Log("空きスロットがありません");
    }
}