using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Stage_", menuName = "Game/Stage Data")]
public class StageData : ScriptableObject
{
    public string stageName;

    [Header("敵キャラ編成")]
    public List<CharacterData> enemyPool = new();

    [Header("報酬")]
    public int rewardGold = 10;

    [Header("フォーメーション設定")]
    public int slotCount = 3;   // このステージで使えるスロット数

    [Header("トラップ設定")]
    public int trapDamage = 10;  // ダメージ
    public int trapCount = 3;    // 配置する罠の数

    // 🔹 増援設定
    public List<CharacterData> reinforcementEnemy; // 増援候補
    public int reinforcementInterval = 0;          // 0なら増援なし
    public int reinforcementCount = 1;             // 1回あたりの投入数
    public int reinforcementLimit = 0;             // 0なら無限

    [SerializeField] public bool isBossStage = false;

    public int enemyLevel = 1;  // 🔹 ステージごとの敵レベル
}