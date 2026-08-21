using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private Transform battleField;
    [SerializeField] private int rows = 5;
    [SerializeField] private int cols = 8;

    public int Rows => rows;
    public int Cols => cols;

    [SerializeField] private GameObject characterPrefab;
    [SerializeField] private List<CharacterData> characterPool;

    private Transform[,] gridCells;
    public Dictionary<Vector2Int, BattleCharacter> gridMap = new();

    private List<BattleCharacter> allies = new();
    private List<BattleCharacter> enemies = new();

    [SerializeField] private ScrollRect logScroll;
    [SerializeField] private Transform logContent;
    [SerializeField] private GameObject logEntryPrefab;
    private const int maxLogs = 50;

    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultText;

    private HashSet<Vector2Int> occupied = new();
    private int currentReward = 0;

    private HashSet<Vector2Int> trapCells = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> soilTrapCells = new HashSet<Vector2Int>();

    private int trapDamage = 5;
    private bool isPaused = false;

    // === Reinforcement fields ===
    private int reinforcementIndex = 0;
    private int reinforcementTotalSpawned = 0;
    private StageData currentStage = null;
    public StageData CurrentStage => currentStage;

    private void Start()
    {
        // GenerateField(); // debug用
    }

    private void OnEnable()
    {
        ConfigureResponsiveLayout();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        ConfigureResponsiveLayout();
    }

    public void StartBattle(List<CharacterData> allies, StageData stage)
    {
        ResetBattle();
        ConfigureResponsiveLayout();
        GenerateField(stage);

        currentReward = stage.rewardStagePoints;

        // Initialize reinforcement fields
        reinforcementIndex = 0;
        reinforcementTotalSpawned = 0;
        currentStage = stage;

        foreach (var ally in allies)
        {
            if (ally != null)
            {
                var pos = GetRandomFreeCell();
                SpawnCharacter(ally, pos, true);
            }
        }

        foreach (var enemy in stage.enemyPool)
        {
            var pos = GetRandomFreeCell();
            SpawnCharacter(enemy, pos, false);
        }

        StartCoroutine(StartBattleAfterSetup(stage));
    }

    public void PauseBattle()
    {
        isPaused = true;
        AddLog("=== バトル一時停止 ===", Color.gray);
    }

    public void ResumeBattle()
    {
        isPaused = false;
        AddLog("=== バトル再開 ===", Color.green);
    }

    private IEnumerator StartBattleAfterSetup(StageData stage)
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return null;
        StartCoroutine(BattleLoop());
    }

    private void ResetBattle()
    {
        foreach (Transform child in battleField)
            Destroy(child.gameObject);

        foreach (Transform child in logContent)
            Destroy(child.gameObject);

        gridMap.Clear();
        allies.Clear();
        enemies.Clear();
        occupied.Clear();
        trapCells.Clear();
        soilTrapCells.Clear();

        isPaused = false;
        StopAllCoroutines();
    }

    private void GenerateField(StageData stage)
    {
        ConfigureResponsiveLayout();
        gridCells = new Transform[cols, rows];
        trapCells.Clear();
        soilTrapCells.Clear();

        var grid = battleField.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = cols;
            ApplyBattleGridCellSize(grid);
        }

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                GameObject cell = Instantiate(cellPrefab, battleField);
                cell.name = $"Cell_{x}_{y}";
                StyleBattleCell(cell, x, y);

                // CellContent を必ず探す or なければ作る
                Transform content = cell.transform.Find("CellContent");
                if (content == null)
                {
                    GameObject contentObj = new GameObject("CellContent", typeof(RectTransform));
                    contentObj.transform.SetParent(cell.transform, false);

                    // RectTransform の初期化
                    RectTransform rt = contentObj.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;

                    content = contentObj.transform;
                }

                gridCells[x, y] = content;
            }
        }

        // 罠ダメージをステージから取得
        trapDamage = (stage != null) ? stage.trapDamage : 5;
        int trapCount = (stage != null) ? stage.trapCount : 3;

        // 罠をランダム配置
        int placed = 0;
        while (placed < trapCount)
        {
            Vector2Int pos = new Vector2Int(Random.Range(0, cols), Random.Range(0, rows));
            if (trapCells.Contains(pos)) continue;
            trapCells.Add(pos);

            Transform cell = gridCells[pos.x, pos.y].parent; // ← 親Cellを参照
            var img = cell.GetComponent<Image>();
            if (img != null)
                img.color = new Color(0.6f, 0.6f, 0.6f);

            Debug.Log($"[Trap] 配置 {pos}");
            placed++;
        }
    }

    private void ConfigureResponsiveLayout()
    {
        bool portrait = UnityUIRuntimeTheme.IsPortraitNarrowScreen();

        var fieldRect = battleField as RectTransform;
        if (fieldRect != null)
        {
            if (portrait)
            {
                fieldRect.anchorMin = new Vector2(0.03f, 0.405f);
                fieldRect.anchorMax = new Vector2(0.97f, 0.79f);
            }
            else
            {
                fieldRect.anchorMin = new Vector2(0.22f, 0.36f);
                fieldRect.anchorMax = new Vector2(0.78f, 0.78f);
            }

            fieldRect.offsetMin = Vector2.zero;
            fieldRect.offsetMax = Vector2.zero;
        }

        var logRect = logScroll != null ? logScroll.GetComponent<RectTransform>() : null;
        if (logRect != null)
        {
            if (portrait)
            {
                logRect.anchorMin = new Vector2(0.06f, 0.185f);
                logRect.anchorMax = new Vector2(0.94f, 0.345f);
            }
            else
            {
                logRect.anchorMin = new Vector2(0.18f, 0.12f);
                logRect.anchorMax = new Vector2(0.82f, 0.30f);
            }

            logRect.offsetMin = Vector2.zero;
            logRect.offsetMax = Vector2.zero;
        }

        if (battleField != null && battleField.TryGetComponent(out GridLayoutGroup grid))
        {
            ApplyBattleGridCellSize(grid);
        }
    }

    private void ApplyBattleGridCellSize(GridLayoutGroup grid)
    {
        if (grid == null)
        {
            return;
        }

        var fieldRect = battleField as RectTransform;
        float fieldWidth = fieldRect != null && fieldRect.rect.width > 0f ? fieldRect.rect.width : (UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? 1120f : 720f);
        float fieldHeight = fieldRect != null && fieldRect.rect.height > 0f ? fieldRect.rect.height : (UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? 620f : 420f);
        float spacing = UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? 8f : 6f;
        float cellSize = Mathf.Floor(Mathf.Min(
            (fieldWidth - spacing * (cols - 1)) / cols,
            (fieldHeight - spacing * (rows - 1)) / rows));

        grid.spacing = new Vector2(spacing, spacing);
        grid.cellSize = new Vector2(Mathf.Max(48f, cellSize), Mathf.Max(48f, cellSize));
        grid.childAlignment = TextAnchor.MiddleCenter;
    }

    private static void StyleBattleCell(GameObject cell, int x, int y)
    {
        var image = cell.GetComponent<Image>();
        if (image != null)
        {
            bool alternate = (x + y) % 2 == 0;
            image.color = alternate ? new Color(0.80f, 0.84f, 0.88f, 1f) : new Color(0.69f, 0.75f, 0.82f, 1f);
        }

        var outline = cell.GetComponent<Outline>();
        if (outline == null) outline = cell.AddComponent<Outline>();
        outline.effectColor = new Color(0.18f, 0.22f, 0.28f, 0.75f);
        outline.effectDistance = new Vector2(1f, -1f);
        outline.useGraphicAlpha = false;
    }

    private Vector2Int GetRandomFreeCell()
    {
        Vector2Int pos;
        do
        {
            int x = Random.Range(0, cols);
            int y = Random.Range(0, rows);
            pos = new Vector2Int(x, y);
        } while (occupied.Contains(pos));

        occupied.Add(pos);
        return pos;
    }

    private void SpawnCharacter(CharacterData data, Vector2Int pos, bool ally)
    {
        // CellContent を親にしてキャラを生成
        Transform content = gridCells[pos.x, pos.y];
        GameObject obj = Instantiate(characterPrefab, content);

        var bc = obj.GetComponent<BattleCharacter>();

        int level = 1;
        if (ally && PlayerInventory.Instance != null && PlayerInventory.Instance.GetOwnedCharacters().TryGetValue(data, out var info))
        {
            level = info.level;
        }
        else if (!ally && currentStage != null)
        {
            level = currentStage.enemyLevel;  // 🔹 敵はステージ設定
        }

        bc.Init(data, pos, ally, level);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;
        rect.SetAsLastSibling(); // 念のため一番前に

        gridMap[pos] = bc;
        if (ally) allies.Add(bc); else enemies.Add(bc);
    }

    private IEnumerator BattleLoop()
    {
        int turn = 1;
        const int maxTurns = 50;

        while (true)
        {
            // 🔹 ポーズ中は待機する
            while (isPaused)
            {
                yield return null; // 1フレーム待機
            }
            
            AddLog($"--- ターン {turn} ---");

            foreach (var ally in new List<BattleCharacter>(allies))
            {
                if (ally != null) DoAction(ally, "味方");
                yield return new WaitForSeconds(0.3f);
            }

            foreach (var enemy in new List<BattleCharacter>(enemies))
            {
                if (enemy != null) DoAction(enemy, "敵");
                yield return new WaitForSeconds(0.3f);
            }

            // 土の罠による継続ダメージ
            foreach (var bc in new List<BattleCharacter>(allies))
            {
                if (bc != null && soilTrapCells.Contains(bc.gridPos) && bc.data.skillType != SkillType.Soil)
                {
                    AddLog($"{bc.data.characterName} は土の罠でダメージを受けた！", Color.yellow);
                    bc.TakeDamage(trapDamage, this, isBasicAttack: false);
                }
            }
            foreach (var bc in new List<BattleCharacter>(enemies))
            {
                if (bc != null && soilTrapCells.Contains(bc.gridPos) && bc.data.skillType != SkillType.Soil)
                {
                    AddLog($"{bc.data.characterName} は土の罠でダメージを受けた！", Color.yellow);
                    bc.TakeDamage(trapDamage, this, isBasicAttack: false);
                }
            }


            // 敵ドラゴン専用：確率で回復
            foreach (var enemy in new List<BattleCharacter>(enemies))
            {
                // 敵ドラゴン専用：ボスステージ時のみ確率で回復
                if (currentStage != null && currentStage.isBossStage
                    && enemy != null && !enemy.isDead 
                    && enemy.data.skillType == SkillType.Dragon && !enemy.isAlly)
                {
                    if (Random.value < 0.3f) // 30% の確率
                    {
                        int heal = Mathf.RoundToInt(enemy.data.GetMaxHP(enemy.level) * 0.1f); // 最大HPの10%
                        int beforeHP = enemy.currentHP;
                        enemy.currentHP = Mathf.Min(enemy.currentHP + heal, enemy.data.GetMaxHP(enemy.level));
                        enemy.UpdateHPBar();
                        AddLog($"[ボス効果] {enemy.data.characterName} は体力を {enemy.currentHP - beforeHP} 回復した！", Color.green);
                    }
                }
            }

            // === Reinforcement check for boss stage ===
            if (currentStage != null
                && currentStage.reinforcementInterval > 0
                && currentStage.reinforcementEnemy != null
                && currentStage.reinforcementEnemy.Count > 0)
            {
                if (turn % currentStage.reinforcementInterval == 0)
                {
                    SpawnReinforcements(currentStage);
                }
            }

            if (enemies.Count == 0)
            {
                ShowResult("勝利！", Color.green, true);
                yield break;
            }
            if (allies.Count == 0)
            {
                ShowResult("敗北…", Color.red, false);
                yield break;
            }
            if (turn >= maxTurns)
            {
                ShowResult("引き分け", Color.gray, false);
                yield break;
            }

            turn++;
            yield return new WaitForSeconds(0.5f);
        }
    }

    // === Reinforcement spawning ===
    private void SpawnReinforcements(StageData stage)
    {
        // Check if limit is reached
        if (stage.reinforcementLimit > 0 && reinforcementTotalSpawned >= stage.reinforcementLimit)
            return;

        int spawnCount = stage.reinforcementCount > 0 ? stage.reinforcementCount : 1;
        int spawned = 0;
        for (int i = 0; i < spawnCount; i++)
        {
            // Check limit again for each spawn
            if (stage.reinforcementLimit > 0 && reinforcementTotalSpawned >= stage.reinforcementLimit)
                break;
            // Get next enemy in order
            int idx = reinforcementIndex % stage.reinforcementEnemy.Count;
            CharacterData reinf = stage.reinforcementEnemy[idx];
            reinforcementIndex++;
            if (reinf != null)
            {
                // 必ず空きマスを見つける（罠/土罠を避ける）。最大100回まで再抽選
                bool found = false;
                Vector2Int pos = new Vector2Int();
                int tryCount = 0;
                while (tryCount < 100)
                {
                    int x = Random.Range(0, cols);
                    int y = Random.Range(0, rows);
                    pos = new Vector2Int(x, y);
                    if (!gridMap.ContainsKey(pos) && !trapCells.Contains(pos) && !soilTrapCells.Contains(pos))
                    {
                        found = true;
                        break;
                    }
                    tryCount++;
                }
                if (!found)
                {
                    Debug.LogWarning($"[増援] 空きマスが見つからずスキップ: {reinf.characterName}");
                    continue;
                }
                SpawnCharacter(reinf, pos, false);
                AddLog($"[増援] {reinf.characterName} が登場！", Color.red);
                spawned++;
                reinforcementTotalSpawned++;
            }
        }
    }


    private void DoAction(BattleCharacter character, string side)
    {
        if (character == null || character.currentHP <= 0) return;

        if (character.IsStunned())
        {
            AddLog($"{character.data.characterName} はスタンして動けない！", Color.gray);
            character.TickStun();
            return;
        }

        // スキル対象を探す
        BattleCharacter skillTarget = null;
        switch (character.data.skillType)
        {
            case SkillType.Slash:
                skillTarget = TargetingService.FindSwordTarget(this, character);
                break;
            case SkillType.Arrow:
                skillTarget = TargetingService.FindArrowTarget(this, character);
                break;
            case SkillType.Spear:
                skillTarget = TargetingService.FindSpearTarget(this, character);
                break;
            case SkillType.StunBlow:
                skillTarget = TargetingService.FindAdjacentEnemy(this, character);
                break;
            case SkillType.Stone:
                skillTarget = TargetingService.FindStoneTarget(this, character);
                break;
            case SkillType.Gun:
                skillTarget = TargetingService.FindNearestEnemy(this, character, character.isAlly ? enemies : allies);
                break;
            case SkillType.Soil:
                skillTarget = character; // 自分自身を対象にスキル発動扱い
                break;
            case SkillType.Fireball:
                {
                    var candidates = character.isAlly ? enemies : allies;
                    if (candidates.Count > 0)
                    {
                        skillTarget = candidates[Random.Range(0, candidates.Count)];
                    }
                }
                break;
            case SkillType.WoodPush:
                skillTarget = TargetingService.FindAdjacentEnemy(this, character);
                break;
            case SkillType.WaterHeal:
                skillTarget = TargetingService.FindAdjacentAlly(this, character);
                break;
            case SkillType.HorseCharge:
                // 2マス以内に敵がいる場合のみターゲット選択
                skillTarget = TargetingService.FindHorseChargeTarget(this, character, character.isAlly ? enemies : allies);
                break;
            case SkillType.BirdRetreat:
                skillTarget = TargetingService.FindAdjacentEnemy(this, character);
                break;
            case SkillType.TigerTwinClaw:
                skillTarget = TargetingService.FindAdjacentEnemy(this, character);
                break;
            case SkillType.Dragon:
                skillTarget = character; // 自分をダミー対象にする
                break;
        }

        // ドラゴンは特殊処理
        if (character.data.skillType == SkillType.Dragon)
        {
            bool used = character.TryUseSkill(null, this);
            if (used)
                return; // スキル成功したら行動終了
            // スキル失敗 or 対象なしなら通常攻撃/移動に進む
        }
        else
        {
            // 通常キャラのスキル試行
            if (skillTarget != null && character.TryUseSkill(skillTarget, this))
            {
                return; // スキル成功
            }
        }

        // 通常攻撃
        BattleCharacter target = TargetingService.FindAdjacentEnemy(this, character);
        if (target != null)
        {
            character.PerformAttack(target, this);
            return;
        }

        // 移動処理
        BattleCharacter nearest = TargetingService.FindNearestEnemy(this, character, character.isAlly ? enemies : allies);
        if (nearest != null)
        {
            var blocked = new HashSet<Vector2Int>(gridMap.Keys);
            blocked.Remove(character.gridPos);
            blocked.Remove(nearest.gridPos);

            List<Vector2Int> path = Pathfinding.FindPath(character.gridPos, nearest.gridPos, rows, cols, blocked);

            if (path != null && path.Count > 1)
            {
                Vector2Int nextStep = path[1];
                if (IsCellFree(nextStep))
                {
                    MoveCharacter(character, nextStep, side);
                    return;
                }
            }
            else
            {
                // ターゲットの周囲8マスを候補として最も近づけるセルを探す
                List<Vector2Int> candidates = new List<Vector2Int>();
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        Vector2Int pos = nearest.gridPos + new Vector2Int(dx, dy);
                        if (pos.x < 0 || pos.x >= cols || pos.y < 0 || pos.y >= rows) continue;
                        if (!gridMap.ContainsKey(pos))
                        {
                            candidates.Add(pos);
                        }
                    }
                }
                // 最も近づけるセルへのpathを探す
                float minDist = float.MaxValue;
                List<Vector2Int> bestPath = null;
                foreach (var candidate in candidates)
                {
                    var blocked2 = new HashSet<Vector2Int>(gridMap.Keys);
                    blocked2.Remove(character.gridPos);
                    // candidateは空きマスなのでblocked2に含まれていない
                    List<Vector2Int> candidatePath = Pathfinding.FindPath(character.gridPos, candidate, rows, cols, blocked2);
                    if (candidatePath != null && candidatePath.Count > 1)
                    {
                        // 距離はターゲットまでの距離で比較
                        float dist = Vector2Int.Distance(candidate, nearest.gridPos);
                        if (bestPath == null || dist < minDist)
                        {
                            minDist = dist;
                            bestPath = candidatePath;
                        }
                    }
                }
                if (bestPath != null && bestPath.Count > 1)
                {
                    Vector2Int nextStep = bestPath[1];
                    if (IsCellFree(nextStep))
                    {
                        MoveCharacter(character, nextStep, side);
                        return;
                    }
                }
                AddLog($"{side} {character.data.characterName} は動けない！", Color.gray);
            }
        }
    }

    private bool IsCellFree(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < cols && pos.y >= 0 && pos.y < rows && !gridMap.ContainsKey(pos);
    }

    private void MoveCharacter(BattleCharacter character, Vector2Int newPos, string side)
    {
        Vector2Int oldPos = character.gridPos;
        gridMap.Remove(oldPos);
        character.gridPos = newPos;
        gridMap[newPos] = character;

        StartCoroutine(SmoothMove(character, gridCells[newPos.x, newPos.y]));
        AddLog($"{side} {character.data.characterName} が移動！", Color.white);

        // 移動方向を更新
        Vector2Int dir = newPos - oldPos;
        character.UpdateDirection(dir);

        if (trapCells.Contains(newPos))
        {
            AddLog($"{side} {character.data.characterName} は罠にかかった！", Color.magenta);
            character.TakeDamage(trapDamage, this, isBasicAttack: false);
            trapCells.Remove(newPos);
        }

        if (soilTrapCells.Contains(newPos))
        {
            if (character.data.skillType != SkillType.Soil)
            {
                AddLog($"{side} {character.data.characterName} は土の罠の上に立っている！", Color.yellow);
            }
        }
    }

    public IEnumerator SmoothMove(BattleCharacter character, Transform targetCell, float duration = 0.2f)
    {
        RectTransform charRect = character.GetComponent<RectTransform>();
        if (charRect == null) yield break;

        // ワールド座標で開始と終了を保持
        Vector3 startWorld = charRect.position;
        Vector3 endWorld = targetCell.position;

        float time = 0f;
        while (time < duration)
        {
            if (charRect == null) yield break;
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            // 世界座標で補間
            charRect.position = Vector3.Lerp(startWorld, endWorld, t); 
            charRect.SetAsLastSibling();

            Canvas canvas = charRect.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 10;
            }

            yield return null;
        }

        if (charRect != null)
        {
            // 最後に親を切り替えてローカル座標をゼロに
            charRect.SetParent(targetCell, false);
            charRect.localPosition = Vector3.zero;
            charRect.SetAsLastSibling();

            Canvas canvas = charRect.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 10;
            }
        }
    }

    public void AddLog(string message, Color? color = null)
    {
        GameObject entry = Instantiate(logEntryPrefab, logContent);
        var text = entry.GetComponent<TextMeshProUGUI>();
        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
        text.text = message;

        if (color.HasValue)
            text.color = color.Value;

        if (logContent.childCount > maxLogs)
            Destroy(logContent.GetChild(0).gameObject);

        Canvas.ForceUpdateCanvases();
        logScroll.verticalNormalizedPosition = 0f;
    }

    private void ShowResult(string message, Color color, bool isWin = false)
    {
        int effectiveReward = 0;
        if (isWin && GameManager.Instance != null && currentStage != null)
        {
            GameManager.Instance.RegisterClearedStage(currentStage.stageId);
            effectiveReward = GameManager.Instance.GetEffectiveStagePointReward(currentReward);
            GameManager.Instance.AddStagePoints(effectiveReward);
            AddLog($"報酬 {effectiveReward} ステージポイント を獲得！", Color.yellow);
        }

        if (resultPanel == null || resultText == null)
        {
            AddLog(message, color);
            return;
        }

        resultPanel.SetActive(true);
        resultPanel.transform.SetAsLastSibling();

        resultText.gameObject.SetActive(true);
        resultText.transform.SetAsLastSibling();
        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(resultText);
        resultText.text = isWin && effectiveReward > 0
            ? $"{message}\n報酬 +{effectiveReward} StagePts"
            : message;
        resultText.color = color;
        resultText.alignment = TextAlignmentOptions.Center;
        resultText.enableAutoSizing = true;
        resultText.fontSizeMin = 22f;
        resultText.fontSizeMax = 48f;

        Canvas.ForceUpdateCanvases();
    }

    public void PlayAttackVfx(BattleCharacter attacker, BattleCharacter target, bool skill = false)
    {
        if (attacker != null)
        {
            attacker.PlayCastEffect(skill ? new Color(0.45f, 0.85f, 1f) : new Color(1f, 0.9f, 0.35f));
        }

        if (target != null)
        {
            target.PlayHitEffect(skill ? new Color(0.85f, 0.55f, 1f) : new Color(1f, 0.35f, 0.25f));
        }
    }

    public void PlayDamageVfx(BattleCharacter target, int amount, bool isHealing = false)
    {
        if (target == null) return;

        target.PlayHitEffect(isHealing ? new Color(0.4f, 1f, 0.65f) : new Color(1f, 0.28f, 0.22f));
        ShowFloatingText(target, isHealing ? $"+{amount}" : $"-{amount}", isHealing ? new Color(0.45f, 1f, 0.65f) : new Color(1f, 0.78f, 0.42f));
    }

    public void ShowFloatingText(BattleCharacter target, string message, Color color)
    {
        if (target == null || string.IsNullOrEmpty(message)) return;
        StartCoroutine(FloatingTextRoutine(target.transform as RectTransform, message, color));
    }

    private IEnumerator FloatingTextRoutine(RectTransform parent, string message, Color color)
    {
        if (parent == null) yield break;

        var obj = new GameObject("BattleFloatingText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.55f);
        rect.anchorMax = new Vector2(0.5f, 0.55f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(100f, 36f);
        rect.anchoredPosition = Vector2.zero;
        rect.SetAsLastSibling();

        var text = obj.GetComponent<TextMeshProUGUI>();
        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
        text.text = message;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 12f;
        text.fontSizeMax = 24f;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.raycastTarget = false;

        float duration = 0.55f;
        float elapsed = 0f;
        Vector2 start = rect.anchoredPosition;
        while (elapsed < duration)
        {
            if (rect == null || text == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rect.anchoredPosition = start + new Vector2(0f, Mathf.Lerp(0f, 34f, t));
            var c = color;
            c.a = 1f - t;
            text.color = c;
            yield return null;
        }

        if (obj != null) Destroy(obj);
    }

    public void HandleDeath(BattleCharacter target)
    {
        if (target == null || target.isDead) return;
        target.isDead = true;

        AddLog($"{target.data.characterName} は倒れた！", Color.gray);

        gridMap.Remove(target.gridPos);
        if (target.isAlly) allies.Remove(target);
        else enemies.Remove(target);

        target.StopAllCoroutines();
        Destroy(target.gameObject);
    }



    // 銃スキル攻撃処理
    public void PerformGunAttack(BattleCharacter self, BattleCharacter firstTarget)
    {
        if (firstTarget == null) return;
        Vector2 dirVec = (firstTarget.gridPos - self.gridPos);
        if (dirVec == Vector2.zero) return;
        // ノーマライズ: 方向ベクトルを整数で正規化
        int dx = (int)Mathf.Sign(dirVec.x);
        int dy = (int)Mathf.Sign(dirVec.y);
        if (dx != 0) dx = (firstTarget.gridPos.x - self.gridPos.x) / Mathf.Abs(firstTarget.gridPos.x - self.gridPos.x);
        if (dy != 0) dy = (firstTarget.gridPos.y - self.gridPos.y) / Mathf.Abs(firstTarget.gridPos.y - self.gridPos.y);
        Vector2Int dir = new Vector2Int(dx, dy);
        Vector2Int pos = self.gridPos + dir;
        List<BattleCharacter> affected = new List<BattleCharacter>();
        while (pos.x >= 0 && pos.x < cols && pos.y >= 0 && pos.y < rows)
        {
            if (gridMap.ContainsKey(pos))
            {
                var bc = gridMap[pos];
                if (bc.currentHP > 0)
                {
                    affected.Add(bc);
                }
            }
            pos += dir;
        }
        foreach (var bc in affected)
        {
            int dmg = Mathf.RoundToInt(self.GetEffectiveAttack(this) * self.data.skillPower);
            string type = (bc.isAlly == self.isAlly) ? "味方" : "敵";
            AddLog($"{self.data.characterName} の銃が{type} {bc.data.characterName} を撃った！({dmg}ダメージ)", bc.isAlly == self.isAlly ? Color.cyan : Color.red);
            bc.TakeDamage(dmg, this, isBasicAttack: false);
        }
        // 最後に方向更新
        if (affected.Count > 0)
        {
            self.UpdateDirection(dir);
        }
    }
    public void GenerateSoilTraps(Vector2Int center)
    {
        // 候補マスを収集
        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                Vector2Int pos = new Vector2Int(center.x + dx, center.y + dy);
                if (pos.x < 0 || pos.x >= cols || pos.y < 0 || pos.y >= rows) continue;
                // 既に土罠 or 通常罠がある場合はスキップ
                if (soilTrapCells.Contains(pos) || trapCells.Contains(pos)) continue;
                candidates.Add(pos);
            }
        }

        // シャッフル
        for (int i = 0; i < candidates.Count; i++)
        {
            int j = Random.Range(i, candidates.Count);
            (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
        }

        // 最大2個
        int trapsToPlace = Mathf.Min(2, candidates.Count);
        for (int i = 0; i < trapsToPlace; i++)
        {
            Vector2Int pos = candidates[i];
            soilTrapCells.Add(pos);
            Transform cell = gridCells[pos.x, pos.y].parent;
            var img = cell.GetComponent<Image>();
            if (img != null)
                img.color = new Color(0.5f, 0.3f, 0.1f);
        }
    }

    public void PushBackCharacter(BattleCharacter attacker, BattleCharacter target)
    {
        if (attacker == null || target == null) return;

        Vector2Int dir = target.gridPos - attacker.gridPos;
        if (dir == Vector2Int.zero) return;

        // 方向を正規化（-1, 0, 1 に限定）
        dir = new Vector2Int(Mathf.Clamp(dir.x, -1, 1), Mathf.Clamp(dir.y, -1, 1));
        Vector2Int newPos = target.gridPos + dir;

        // 範囲外チェック
        if (newPos.x < 0 || newPos.x >= cols || newPos.y < 0 || newPos.y >= rows)
        {
            AddLog($"{target.data.characterName} は押し出されたが壁にぶつかった！", Color.gray);
            return;
        }

        // 占有マスチェック
        if (gridMap.ContainsKey(newPos))
        {
            AddLog($"{target.data.characterName} は押し出されたが進めず足止めされた！", Color.gray);
            target.ApplyStun(1);
            return;
        }

        // 移動実行
        gridMap.Remove(target.gridPos);
        target.gridPos = newPos;
        gridMap[newPos] = target;

        StartCoroutine(SmoothMove(target, gridCells[newPos.x, newPos.y]));
        AddLog($"{target.data.characterName} は後方に押し出された！", Color.yellow);
    }
    public void PerformHorseCharge(BattleCharacter self, BattleCharacter target)
    {
        if (target == null) return;

        Vector2Int dir = target.gridPos - self.gridPos;
        dir = new Vector2Int(Mathf.Clamp(dir.x, -1, 1), Mathf.Clamp(dir.y, -1, 1));

        Vector2Int pos1 = self.gridPos + dir;
        Vector2Int pos2 = self.gridPos + dir * 2;

        bool pos1HasEnemy = gridMap.ContainsKey(pos1) && gridMap[pos1].isAlly != self.isAlly;
        bool pos2HasEnemy = gridMap.ContainsKey(pos2) && gridMap[pos2].isAlly != self.isAlly;
        bool pos1Free = !gridMap.ContainsKey(pos1);
        bool pos2Free = !gridMap.ContainsKey(pos2);

        if (pos1HasEnemy)
        {
            self.PerformAttack(gridMap[pos1], this, self.data.skillPower, $"{self.data.characterName} の突進攻撃！ {{0}} ダメージ");
            if (!pos2HasEnemy && pos2Free) MoveCharacterNoTrap(self, pos2);
            return;
        }
        if (pos2HasEnemy && pos1Free)
        {
            MoveCharacterNoTrap(self, pos1);
            self.PerformAttack(gridMap[pos2], this, self.data.skillPower, $"{self.data.characterName} の突進攻撃！ {{0}} ダメージ");
            return;
        }

        // 移動できない場合でも、突進方向に敵がいれば攻撃だけ行う
        if (pos1HasEnemy)
        {
            self.PerformAttack(gridMap[pos1], this, self.data.skillPower, $"{self.data.characterName} の突進攻撃！ {{0}} ダメージ");
            return;
        }
        if (pos2HasEnemy)
        {
            self.PerformAttack(gridMap[pos2], this, self.data.skillPower, $"{self.data.characterName} の突進攻撃！ {{0}} ダメージ");
            return;
        }

        AddLog($"{self.data.characterName} の突進は進めなかった！", Color.gray);
    }

    // 罠を踏まない移動（トラップチェックなし）
    private void MoveCharacterNoTrap(BattleCharacter character, Vector2Int newPos)
    {
        // 盤面外チェック
        if (newPos.x < 0 || newPos.x >= cols || newPos.y < 0 || newPos.y >= rows)
        {
            AddLog($"{character.data.characterName} の突進は壁に阻まれた！", Color.gray);
            return;
        }

        Vector2Int oldPos = character.gridPos;
        gridMap.Remove(oldPos);
        character.gridPos = newPos;
        gridMap[newPos] = character;
        StartCoroutine(SmoothMove(character, gridCells[newPos.x, newPos.y]));
        // 移動方向を更新
        Vector2Int dir = newPos - oldPos;
        character.UpdateDirection(dir);
        AddLog($"{character.data.characterName} が突進で移動！", Color.white);
    }

    public void PerformBirdRetreat(BattleCharacter bird)
    {
        if (bird == null || bird.isDead) return;

        List<Vector2Int> candidates = new();
        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                Vector2Int pos = bird.gridPos + new Vector2Int(dx, dy);
                if (pos.x < 0 || pos.x >= cols || pos.y < 0 || pos.y >= rows) continue;
                if (!gridMap.ContainsKey(pos)) candidates.Add(pos);
            }
        }

        if (candidates.Count > 0)
        {
            Vector2Int newPos = candidates[Random.Range(0, candidates.Count)];
            gridMap.Remove(bird.gridPos);
            bird.gridPos = newPos;
            gridMap[newPos] = bird;

            StartCoroutine(SmoothMove(bird, gridCells[newPos.x, newPos.y]));
            AddLog($"{bird.data.characterName} は攻撃後に退避した！", Color.cyan);
        }
    }

    public void PerformTigerTwinClaw(BattleCharacter tiger, BattleCharacter firstTarget)
    {
        if (tiger == null || tiger.isDead || firstTarget == null) return;

        // 1回目
        tiger.PerformAttack(firstTarget, this, tiger.data.skillPower, $"{tiger.data.characterName} のツインクロー1撃目！ {{0}} ダメージ");

        // 2回目
        var secondTarget = TargetingService.FindAdjacentEnemy(this, tiger);
        if (secondTarget != null && secondTarget != firstTarget)
        {
            tiger.PerformAttack(secondTarget, this, tiger.data.skillPower, $"{tiger.data.characterName} のツインクロー2撃目！ {{0}} ダメージ");
        }
        else if (firstTarget != null && !firstTarget.isDead)
        {
            tiger.PerformAttack(firstTarget, this, tiger.data.skillPower, $"{tiger.data.characterName} のツインクロー2撃目！ {{0}} ダメージ");
        }
    }

    // 🔥 ドラゴンのブレス攻撃（方向ごとに N×N 範囲）
    public bool PerformDragonBreath(BattleCharacter dragon, int range)
    {
        if (dragon == null || dragon.isDead) return false;

        List<Vector2Int> dirs = new List<Vector2Int>
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        List<Vector2Int> validDirs = new();

        foreach (var dir in dirs)
        {
            // この方向に range×range のブロックを配置
            for (int d = 1; d <= range; d++)
            {
                bool found = false;
                for (int dx = -range + 1; dx <= range - 1; dx++)
                {
                    for (int dy = -range + 1; dy <= range - 1; dy++)
                    {
                        Vector2Int offset = new Vector2Int(dx, dy);
                        Vector2Int pos = dragon.gridPos + dir * d;

                        // dir が上下なら「上にずらして正方形」
                        if (dir == Vector2Int.up || dir == Vector2Int.down)
                            pos += new Vector2Int(dx, dy >= 0 ? dy : 0);
                        else // 左右なら横にずらして正方形
                            pos += new Vector2Int(dx >= 0 ? dx : 0, dy);

                        if (gridMap.TryGetValue(pos, out var bc) && bc.isAlly != dragon.isAlly && !bc.isDead)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found) break;
                }
                if (found)
                {
                    validDirs.Add(dir);
                    break;
                }
            }
        }

        if (validDirs.Count == 0) return false;

        Vector2Int chosenDir = validDirs[Random.Range(0, validDirs.Count)];

        // 攻撃処理：chosenDir 方向の range×range 全員にダメージ
        for (int d = 1; d <= range; d++)
        {
            for (int dx = -range + 1; dx <= range - 1; dx++)
            {
                for (int dy = -range + 1; dy <= range - 1; dy++)
                {
                    Vector2Int pos = dragon.gridPos + chosenDir * d;
                    if (chosenDir == Vector2Int.up || chosenDir == Vector2Int.down)
                        pos += new Vector2Int(dx, dy >= 0 ? dy : 0);
                    else
                        pos += new Vector2Int(dx >= 0 ? dx : 0, dy);

                    if (gridMap.TryGetValue(pos, out var bc) && bc.isAlly != dragon.isAlly && !bc.isDead)
                    {
                        int dmg = Mathf.RoundToInt(dragon.GetEffectiveAttack(this) * dragon.data.skillPower);
                        AddLog($"{dragon.data.characterName} のブレスが {bc.data.characterName} に命中！ {dmg} ダメージ", Color.red);
                        bc.TakeDamage(dmg, this, dragon, isBasicAttack: false);
                    }
                }
            }
        }

        dragon.UpdateDirection(chosenDir);
        return true;
    }

    // 🐉 ドラゴンの咆哮（周囲8方向、スタン＋小ダメージ）
    public bool PerformDragonRoar(BattleCharacter dragon)
    {
        if (dragon == null || dragon.isDead) return false;

        List<BattleCharacter> targets = new();
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                Vector2Int pos = dragon.gridPos + new Vector2Int(dx, dy);
                if (gridMap.TryGetValue(pos, out var bc) && bc.isAlly != dragon.isAlly && !bc.isDead)
                {
                    targets.Add(bc);
                }
            }
        }

        if (targets.Count > 0)
        {
            AddLog($"{dragon.data.characterName} が咆哮した！", Color.magenta);
            var targetsCopy = new List<BattleCharacter>(targets);
            foreach (var t in targetsCopy)
            {
                if (t == null || t.isDead) continue;
                // 小ダメージ
                int dmg = Mathf.RoundToInt(dragon.GetEffectiveAttack(this) * 0.5f);
                t.TakeDamage(dmg, this, dragon, isBasicAttack: false);

                AddLog($"{t.data.characterName} は咆哮で {dmg} ダメージを受けた！", Color.red);

                // 🔹 HP が 0 以下なら即死亡処理
                if (t.currentHP <= 0)
                {
                    HandleDeath(t);
                    continue; // 死亡したのでスタン処理はスキップ
                }

                // 30% の確率でスタン
                if (Random.value < 0.3f)
                {
                    t.ApplyStun(1);
                    AddLog($"{t.data.characterName} は咆哮でスタンした！", Color.gray);
                }
            }
            return true;
        }
        return false;
    }

}
