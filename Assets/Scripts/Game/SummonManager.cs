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
                // Calculate probabilities using UI value as category bonus approach
                var activeCategory = GameManager.Instance.GetActiveSummonCategory();
                int totalChars = unlockedChars.Count;
                float baseProbability = 1.0f / totalChars;

                int activeCategoryCount = 0;
                if (activeCategory != CharacterCategory.None)
                {
                    foreach (var ch in unlockedChars)
                    {
                        if (ch.category == activeCategory)
                            activeCategoryCount++;
                    }
                }

                float[] probabilities = new float[totalChars];

                if (activeCategory != CharacterCategory.None && activeCategoryCount > 0)
                {
                    float categoryTotal = baseProbability * activeCategoryCount;
                    categoryTotal += (GameManager.Instance.GetEffectiveSummonRate(activeCategory) - 1.0f);

                    float categoryProbPerChar = categoryTotal / activeCategoryCount;
                    float nonCategoryProbPerChar = (1.0f - categoryTotal) / (totalChars - activeCategoryCount);

                    for (int i = 0; i < totalChars; i++)
                    {
                        if (unlockedChars[i].category == activeCategory)
                        {
                            probabilities[i] = categoryProbPerChar;
                        }
                        else
                        {
                            probabilities[i] = nonCategoryProbPerChar;
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < totalChars; i++)
                    {
                        probabilities[i] = baseProbability;
                    }
                }

                // Normalize probabilities to sum to 1.0 (just in case)
                float sumProb = 0f;
                for (int i = 0; i < totalChars; i++)
                {
                    sumProb += probabilities[i];
                }
                for (int i = 0; i < totalChars; i++)
                {
                    probabilities[i] /= sumProb;
                }

                // Build cumulative weights
                float[] cumulativeWeights = new float[totalChars];
                float cumulative = 0f;
                for (int i = 0; i < totalChars; i++)
                {
                    cumulative += probabilities[i];
                    cumulativeWeights[i] = cumulative;
                }

                // Debug logging for category probabilities and per-character probabilities
                Debug.Log($"Active Summon Category: {activeCategory}");
                var categoryProbabilities = new System.Collections.Generic.Dictionary<CharacterCategory, float>();
                var categoryCharProbabilities = new System.Collections.Generic.Dictionary<CharacterCategory, System.Collections.Generic.List<(string name, float prob)>>();
                for (int i = 0; i < totalChars; i++)
                {
                    var cat = unlockedChars[i].category;
                    float prob = probabilities[i];
                    if (!categoryProbabilities.ContainsKey(cat))
                        categoryProbabilities[cat] = 0f;
                    categoryProbabilities[cat] += prob;

                    if (!categoryCharProbabilities.ContainsKey(cat))
                        categoryCharProbabilities[cat] = new System.Collections.Generic.List<(string, float)>();
                    categoryCharProbabilities[cat].Add((unlockedChars[i].characterName, prob));
                }
                foreach (var kvp in categoryProbabilities)
                {
                    var cat = kvp.Key;
                    var catProb = kvp.Value;
                    float percent = catProb * 100f;
                    string probsStr = "";
                    foreach (var cp in categoryCharProbabilities[cat])
                    {
                        probsStr += $"{cp.name}: {cp.prob:F4}, ";
                    }
                    if (probsStr.EndsWith(", "))
                        probsStr = probsStr.Substring(0, probsStr.Length - 2);
                    Debug.Log($"Category {cat}: {percent:F2}% ({probsStr})");
                }

                float randomWeight = Random.Range(0f, 1f);
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