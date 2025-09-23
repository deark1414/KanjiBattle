using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Button selfButton;
    [SerializeField] private TextMeshProUGUI costText; 

    private CharacterData characterData;

    // 呼び出し側と合わせてメソッド名を SetCharacter に変更
    public void SetCharacter(CharacterData data, int level, int count)
    {
        characterData = data;

        string skillName = data.skillType != SkillType.None ? data.skillType.ToString() : "なし";
        infoText.text = $"{data.characterName} HP:{data.GetMaxHP(level)} ATK:{data.GetAttack(level)} SKILL:{skillName}";
        levelText.text = $"Lv.{level}";
        countText.text = $"x{count}";

        int baseCost = 100;
        float growthRate = 1.2f;
        int nextLevelCost = data.GetUpgradeCost(level);
        costText.text = $"Cost: {nextLevelCost}";

        selfButton.onClick.RemoveAllListeners();
        selfButton.onClick.AddListener(OnClickUpgrade);
    }

    private void OnClickUpgrade()
    {
        PlayerInventory.Instance.UpgradeCharacter(characterData);
    }
}