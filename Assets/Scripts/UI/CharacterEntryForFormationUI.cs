using TMPro;
using UnityEngine;

public class CharacterEntryForFormationUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI countText;
    private CharacterData characterData;

    // 呼び出し側と合わせてメソッド名を SetCharacter に変更
    public void SetCharacter(CharacterData data, int level, int count)
    {
        characterData = data;
        string skillName = data.skillType != SkillType.None ? data.skillType.ToString() : "なし";
        infoText.text = $"{data.characterName} HP:{data.GetMaxHP(level)} ATK:{data.GetAttack(level)} SKILL:{skillName}";
        levelText.text = $"Lv.{level}";
        countText.text = $"x{count}";
    }
    public void OnClick()
    {
         if (FormationCharacterListUI.instance != null)
        {
            FormationCharacterListUI.instance.SelectCharacter(characterData);
        }
    }
}