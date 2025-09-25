using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SummonManager : MonoBehaviour
{
    [SerializeField] private Button summonButton;
    [SerializeField] private TextMeshProUGUI summonText;
    [SerializeField] private int baseSummonCost = 100;
    private int CurrentSummonCost
    {
        get
        {
            int count = PlayerInventory.Instance != null ? PlayerInventory.Instance.GetSummonableCharacters().Count : 0;
            int baseCostForCount = Mathf.FloorToInt(baseSummonCost * Mathf.Pow(2, Mathf.Max(0, count - 1)));
            return GameManager.Instance.GetEffectiveSummonCost(baseCostForCount);
        }
    }

    private void Start()
    {
        if (summonButton != null)
            summonButton.onClick.AddListener(Summon);
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnSummonableChanged += UpdateText;
        if (GameManager.Instance != null)
            GameManager.Instance.OnCostModifiersChanged += UpdateText;
        UpdateText();
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.OnSummonableChanged -= UpdateText;
        if (GameManager.Instance != null)
            GameManager.Instance.OnCostModifiersChanged -= UpdateText;
    }

    private void UpdateText()
    {
        if (summonText != null)
            summonText.text = $"召喚 {CurrentSummonCost}G";
    }

    public void Summon()
    {
        if (GameManager.Instance.SpendGold(CurrentSummonCost))
        {
            // ランダムキャラ召喚: 解放済み・撃破済みキャラのみから選択
            var unlockedChars = PlayerInventory.Instance.GetSummonableCharacters();
            if (unlockedChars != null && unlockedChars.Count > 0)
            {
                // Calculate weights based on summon rate multipliers
                float totalWeight = 0f;
                float[] cumulativeWeights = new float[unlockedChars.Count];
                for (int i = 0; i < unlockedChars.Count; i++)
                {
                    float weight = GameManager.Instance.GetEffectiveSummonRate(unlockedChars[i].category);
                    totalWeight += weight;
                    cumulativeWeights[i] = totalWeight;
                }

                float randomWeight = Random.Range(0f, totalWeight);
                int selectedIndex = 0;
                for (int i = 0; i < cumulativeWeights.Length; i++)
                {
                    if (randomWeight <= cumulativeWeights[i])
                    {
                        selectedIndex = i;
                        break;
                    }
                }

                var c = unlockedChars[selectedIndex];
                PlayerInventory.Instance.AddCharacter(c);
                Debug.Log($"[召喚] {c.characterName} を獲得！");
            }
            else
            {
                Debug.Log("召喚可能なキャラがいません");
            }
        }
        else
        {
            Debug.Log("ゴールド不足！");
        }
        GameManager.Instance.UpdateProduction();
    }
}