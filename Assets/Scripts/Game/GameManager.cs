using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private int baseGoldPerSecond = 100;
    [SerializeField] private int gold = 0;
    [SerializeField] private int goldPerSecond = 100;
    public int Gold => gold; // 外部から参照可能

    // Stage Points
    private int stagePoints = 0;
    public int StagePoints => stagePoints; // 外部から参照可能

    private StageData selectedStage;

    // 最もクリアしたステージIDを保持
    private int highestClearedStageId = 0;

    // ゴールド変更イベント
    public event Action<int> OnGoldChanged;
    // ステージポイント変更イベント
    public event Action<int> OnStagePointsChanged;
    public event Action OnCostModifiersChanged;

    [SerializeField] private int unlockedChapter = 1; // 初期状態で第1章は解放済み
    public int UnlockedChapter => unlockedChapter;

    // 施設効果用の乗数
    private float productionMultiplier = 1f;
    private float characterUpgradeCostMultiplier = 1f;
    private float summonCostMultiplier = 1f;

    private System.Collections.Generic.Dictionary<CharacterCategory, float> summonRateMultipliers = new();

    private CharacterCategory activeSummonCategory = CharacterCategory.None;

    /// <summary>
    /// アクティブな召喚カテゴリを取得または設定します。
    /// CharacterCategory.Noneの場合は全カテゴリが等しく有効（倍率1.0）となります。
    /// </summary>
    public CharacterCategory ActiveSummonCategory
    {
        get => activeSummonCategory;
        set
        {
            if (activeSummonCategory != value)
            {
                activeSummonCategory = value;
                if (activeSummonCategory == CharacterCategory.None)
                {
                    Debug.Log("[GameManager] Active summon category cleared (all categories enabled)");
                }
                else
                {
                    Debug.Log($"[GameManager] Active summon category set to: {activeSummonCategory}");
                }
            }
        }
    }

    /// <summary>
    /// ステージポイントを追加
    /// </summary>
    public void AddStagePoints(int amount)
    {
        stagePoints += amount;
        OnStagePointsChanged?.Invoke(stagePoints);
    }

    /// <summary>
    /// ステージポイントを消費（足りない場合は false）
    /// </summary>
    public bool SpendStagePoints(int amount)
    {
        if (stagePoints < amount) return false;
        stagePoints -= amount;
        OnStagePointsChanged?.Invoke(stagePoints);
        return true;
    }

    /// <summary>
    /// 現在のステージポイントを返す
    /// </summary>
    public int GetStagePoints()
    {
        return stagePoints;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            foreach (CharacterCategory category in Enum.GetValues(typeof(CharacterCategory))) {
                summonRateMultipliers[category] = 1f;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// ゴールドを追加
    /// </summary>
    public void AddGold(int amount)
    {
        gold += amount;
        OnGoldChanged?.Invoke(gold);
    }

    /// <summary>
    /// ゴールドを消費（足りない場合は false）
    /// </summary>
    public bool SpendGold(int amount)
    {
        if (gold < amount) return false;

        gold -= amount;
        OnGoldChanged?.Invoke(gold);
        return true;
    }

    /// <summary>
    /// 現在のゴールドを返す
    /// </summary>
    public int GetGold()
    {
        return gold;
    }

    /// <summary>
    /// ゴールドを直接設定（デバッグやリセット用）
    /// </summary>
    public void SetGold(int amount)
    {
        gold = amount;
        OnGoldChanged?.Invoke(gold);
    }

    /// <summary>
    /// 指定した金額を支払えるかどうかを判定するヘルパー
    /// </summary>
    public static bool CanAfford(int amount)
    {
        return Instance != null && Instance.gold >= amount;
    }

    private void Start()
    {
        UpdateProduction();
        StartCoroutine(PassiveGoldCoroutine());
    }

    private System.Collections.IEnumerator PassiveGoldCoroutine()
    {
        while (true)
        {
            gold += goldPerSecond;
            OnGoldChanged?.Invoke(gold);
            yield return new WaitForSeconds(1f);
        }
    }

    public void UpdateProduction()
    {
        goldPerSecond = Mathf.FloorToInt((baseGoldPerSecond + PlayerInventory.Instance.GetTotalProduction()) * productionMultiplier);
    }

    public void SetSelectedStage(StageData stage)
    {
        selectedStage = stage;
    }

    public StageData GetSelectedStage()
    {
        return selectedStage;
    }

    public void StartStage(StageData stage, System.Collections.Generic.List<CharacterData> allies)
    {
        UIManager.Instance.ShowBattle();
        BattleManager battleManager = FindObjectOfType<BattleManager>();
        if (battleManager != null)
        {
            battleManager.StartBattle(allies, stage);
        }
        else
        {
            Debug.LogError("BattleManager not found in the scene.");
        }
    }

    /// <summary>
    /// 最もクリアしたステージIDを返す
    /// </summary>
    public int GetClearedStageId()
    {
        return highestClearedStageId;
    }

    /// <summary>
    /// ステージクリア時に呼び出して最も高いクリア済みIDを更新
    /// </summary>
    public void RegisterClearedStage(int stageId)
    {
        if (stageId > highestClearedStageId)
        {
            highestClearedStageId = stageId;
        }
    }

    /// <summary>
    /// 指定ステージがクリア済みか判定
    /// </summary>
    public bool IsStageCleared(int stageId)
    {
        return stageId <= highestClearedStageId;
    }

    /// <summary>
    /// 指定ステージが解放されているか（前のステージがクリア済みか）判定
    /// </summary>
    public bool IsStageUnlocked(int stageId)
    {
        // ステージIDが1の場合は常に解放
        if (stageId <= 1) return true;
        // 1つ前のステージがクリア済みか
        return IsStageCleared(stageId - 1);
    }

    /// <summary>
    /// ステージクリア登録（StageData版）
    /// </summary>
    public void ClearStage(StageData stage)
    {
        if (stage == null) return;
        RegisterClearedStage(stage.stageId);
    }

    public int GetHighestClearedStageId()
    {
        return highestClearedStageId;
    }

    public void UnlockChapter(int chapterId)
    {
        if (chapterId > unlockedChapter)
        {
            unlockedChapter = chapterId;
            Debug.Log($"Chapter {chapterId} が解放されました！");
        }
    }

    public bool IsChapterUnlocked(int chapterId)
    {
        return chapterId <= unlockedChapter;
    }

    // ========================
    // Facility Effect Helpers
    // ========================

    // Gold 生産力増加
    public void ApplyGoldProductionBoost(float effectValue)
    {
        productionMultiplier = 1f + effectValue; // effectValue directly added
        UpdateProduction();
    }

    // キャラ強化コスト減算
    public void ApplyUpgradeCostReduction(float effectValue)
    {
        characterUpgradeCostMultiplier = Mathf.Clamp01(1f - effectValue);
        OnCostModifiersChanged?.Invoke();
    }

    // 召喚コスト減算
    public void ApplySummonCostReduction(float effectValue)
    {
        // 指数的に効く召喚コスト軽減（キャラ数増加の指数を圧縮）
        summonCostMultiplier = Mathf.Clamp01(1f - effectValue);
        OnCostModifiersChanged?.Invoke();
    }

    // ステージポイント獲得倍率
    private float stagePointMultiplier = 1f;
    public void ApplyStagePointBoost(float effectValue)
    {
        stagePointMultiplier = 1f + effectValue;
    }
    public int GetEffectiveStagePointReward(int baseReward)
    {
        return Mathf.FloorToInt(baseReward * stagePointMultiplier);
    }

    // 編成枠追加
    private int facilityFormationSlots = 1;
    public void ApplyFormationSlotIncrease(int slots)
    {
        facilityFormationSlots += slots;
    }
    public int GetFacilityFormationSlots()
    {
        return facilityFormationSlots;
    }

    // 召喚率 (カテゴリ別)
    public void AddSummonRateMultiplier(CharacterCategory category, float value)
    {
        if (summonRateMultipliers.ContainsKey(category))
        {
            summonRateMultipliers[category] += value;
        }
    }

    /// <summary>
    /// 指定カテゴリの召喚確率に施設効果を適用した値を取得
    /// アクティブカテゴリがCharacterCategory.Noneの場合は全カテゴリが等分（=1.0）になる。
    /// アクティブカテゴリが設定されている場合は、そのカテゴリのみ倍率を返し、それ以外は1.0を返す。
    /// </summary>
    public float GetEffectiveSummonRate(CharacterCategory category)
    {
        if (activeSummonCategory == CharacterCategory.None)
        {
            // 全カテゴリが等しく有効（倍率1.0）
            return 1f;
        }
        else if (activeSummonCategory == category)
        {
            if (summonRateMultipliers.TryGetValue(category, out float multiplier))
                return multiplier;
            return 1f;
        }
        else
        {
            return 1f;
        }
    }

    /// <summary>
    /// 現在の生産力乗数を取得
    /// </summary>
    public float GetProductionMultiplier()
    {
        return productionMultiplier;
    }

    // 新規追加: キャラクターアンロック施設効果
    public void AddCharacterUnlock(int characterId)
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.UnlockCharacterForSummon(characterId);
        }
    }

    public void UnlockBoss(string bossName)
    {
        Debug.Log($"[GameManager] Boss '{bossName}' unlocked (召喚不可扱い)");
        // 必要ならフラグ管理などをここに
    }

    /// <summary>
    /// 指定した基本強化コストに施設効果を適用した実際の強化コストを取得
    /// </summary>
    public int GetEffectiveUpgradeCost(int baseCost)
    {
        return Mathf.CeilToInt(baseCost * characterUpgradeCostMultiplier);
    }

    /// <summary>
    /// 指定した基本召喚コストに施設効果を適用した実際の召喚コストを取得
    /// </summary>
    public int GetEffectiveSummonCost(int baseCost)
    {
        return Mathf.CeilToInt(baseCost * summonCostMultiplier);
    }

    /// <summary>
    /// 現在アクティブな召喚カテゴリを取得
    /// </summary>
    public CharacterCategory GetActiveSummonCategory()
    {
        return activeSummonCategory;
    }
}
