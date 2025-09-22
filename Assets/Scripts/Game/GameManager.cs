using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private int baseGoldPerSecond = 1;
    [SerializeField] private int gold = 0;
    [SerializeField] private int goldPerSecond = 1;
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
        goldPerSecond = baseGoldPerSecond + PlayerInventory.Instance.GetTotalProduction();
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
}