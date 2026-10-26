using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Stage_", menuName = "Game/Stage Data")]
public class StageData : ScriptableObject
{
    public string stageName;
    public int stageId;

    [Header("章情報")]
    public int chapterId; // 所属する章

    [Header("敵キャラ編成")]
    public List<CharacterData> enemyPool = new();
    public List<int> enemyFusionIds = new();
    [System.NonSerialized] private List<CharacterData> fusionPreview;
    [System.NonSerialized] private int fusionPreviewLevel;

    // Enemy fusion data is level-specific and never enters the recruit database.
    public IReadOnlyList<CharacterData> GetEnemies()
    {
        if (fusionPreview == null || fusionPreviewLevel != enemyLevel
            || !fusionPreview.Select(data => data.characterId).SequenceEqual(enemyFusionIds))
        {
            ClearEnemyFusions();
            fusionPreview = new List<CharacterData>();
            fusionPreviewLevel = enemyLevel;
            var database = Resources.Load<CharacterDatabase>("CharacterDatabase");
            foreach (int id in enemyFusionIds)
            {
                var recipe = BattleFusionRules.GetRecipe(id)
                    ?? throw new System.ArgumentException("Unknown enemy fusion: " + id);
                fusionPreview.Add(CharacterData.CreateBattleFusion(recipe,
                    recipe.Materials.Select(name => database.characters.Single(data => data.characterName == name)).ToArray(), enemyLevel));
            }
        }
        return enemyPool.Concat(fusionPreview).ToArray();
    }

    private void OnDisable() => ClearEnemyFusions();

    private void ClearEnemyFusions()
    {
        if (fusionPreview != null)
            foreach (var data in fusionPreview)
                if (data != null)
                {
                    if (Application.isPlaying) Destroy(data);
                    else DestroyImmediate(data);
                }
        fusionPreview = null;
    }

    [Header("報酬")]
    public int rewardStagePoints = 1; // ←追加

    [Header("フォーメーション設定")]
    public int slotCount = 3;   // このステージで使えるスロット数
    public bool restrictFormationSlots;
    public List<Vector2Int> enemyPositions = new();

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

    public StageData prerequisite;     // このステージを解放するために必要なステージ
    [HideInInspector] public bool isCleared;   // クリア済み
}
