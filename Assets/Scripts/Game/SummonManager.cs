using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SummonManager : MonoBehaviour
{
    [SerializeField] private Button summonButton;
    [SerializeField] private TextMeshProUGUI summonText;
    [SerializeField] private int summonCost = 10;

    private void Start()
    {
        if (summonButton != null)
            summonButton.onClick.AddListener(Summon);
        UpdateText();
    }

    private void UpdateText()
    {
        if (summonText != null)
            summonText.text = $"召喚 {summonCost}G";
    }

    public void Summon()
    {
        if (GameManager.Instance.SpendGold(summonCost))
        {
            // ランダムキャラ召喚: 解放済み・撃破済みキャラのみから選択
            var unlockedChars = PlayerInventory.Instance.GetSummonableCharacters();
            if (unlockedChars != null && unlockedChars.Count > 0)
            {
                var c = unlockedChars[Random.Range(0, unlockedChars.Count)];
                PlayerInventory.Instance.AddCharacter(c);
                Debug.Log($"[召喚] {c.characterName} を獲得！");
            }
            else
            {
                Debug.Log("召喚可能なキャラがいません");
                GameManager.Instance.AddGold(summonCost); // ゴールドを返金
            }
        }
        else
        {
            Debug.Log("ゴールド不足！");
        }
        GameManager.Instance.UpdateProduction();
    }
}