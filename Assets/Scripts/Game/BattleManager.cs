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
    private float currentBattleCellSize = 60f;

    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultText;

    private HashSet<Vector2Int> occupied = new();
    private int currentReward = 0;

    private HashSet<Vector2Int> trapCells = new HashSet<Vector2Int>();
    private HashSet<Vector2Int> soilTrapCells = new HashSet<Vector2Int>();

    private int trapDamage = 5;
    private bool isPaused = false;
    private readonly float[] battleSpeeds = { 1f, 2f, 4f };
    private int battleSpeedIndex = 0;
    private Button speedButton;
    private TextMeshProUGUI speedButtonText;
    private readonly List<GameObject> activeVfxObjects = new();

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
        EnsureSpeedButton();
        ConfigureResponsiveLayout();
    }

    private void OnDisable()
    {
        CleanupBattleVfx();
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
        GameAudio.Instance.EnsureBgm();
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
        StopAllCoroutines();
        CleanupBattleVfx();

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
            currentBattleCellSize = BattleUILayout.Apply(transform, battleField, logScroll, cols, rows);
        }

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                GameObject cell = Instantiate(cellPrefab, battleField);
                cell.name = $"Cell_{x}_{y}";
                BattleUILayout.StyleBattleCell(cell, x, y);

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
        currentBattleCellSize = BattleUILayout.Apply(transform, battleField, logScroll, cols, rows);
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
        BattleUILayout.ApplyCharacterVisualSize(rect, currentBattleCellSize);
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
                yield return BattleWait(0.3f);
            }

            foreach (var enemy in new List<BattleCharacter>(enemies))
            {
                if (enemy != null) DoAction(enemy, "敵");
                yield return BattleWait(0.3f);
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
            yield return BattleWait(0.5f);
        }
    }

    private WaitForSeconds BattleWait(float seconds)
    {
        return new WaitForSeconds(seconds / Mathf.Max(1f, battleSpeeds[battleSpeedIndex]));
    }

    private void EnsureSpeedButton()
    {
        if (speedButton != null) return;

        var buttonObj = new GameObject("SpeedButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObj.transform.SetParent(transform, false);
        speedButton = buttonObj.GetComponent<Button>();
        var image = buttonObj.GetComponent<Image>();
        image.color = new Color(0.62f, 0.42f, 0.23f, 1f);
        var outline = buttonObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.95f, 0.72f, 0.42f, 0.8f);
        outline.effectDistance = new Vector2(1f, -1f);

        var textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(buttonObj.transform, false);
        speedButtonText = textObj.GetComponent<TextMeshProUGUI>();
        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(speedButtonText);
        speedButtonText.alignment = TextAlignmentOptions.Center;
        speedButtonText.enableAutoSizing = true;
        speedButtonText.fontSizeMin = 14f;
        speedButtonText.fontSizeMax = 24f;
        speedButtonText.color = Color.white;
        speedButtonText.raycastTarget = false;

        var textRect = speedButtonText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        speedButton.onClick.AddListener(ToggleBattleSpeed);
        UpdateSpeedButtonLabel();
    }

    private void ToggleBattleSpeed()
    {
        GameAudio.Instance.EnsureBgm();
        battleSpeedIndex = (battleSpeedIndex + 1) % battleSpeeds.Length;
        GameAudio.Instance.Play(GameSound.Click);
        AddLog($"バトル速度 x{battleSpeeds[battleSpeedIndex]:0}", Color.cyan);
        UpdateSpeedButtonLabel();
    }

    private void UpdateSpeedButtonLabel()
    {
        if (speedButtonText != null)
        {
            speedButtonText.text = $"x{battleSpeeds[battleSpeedIndex]:0}";
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
                int destinationIndex = GetMovementDestinationIndex(character, path, targetCellOccupied: true);
                Vector2Int nextStep = path[destinationIndex];
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
                    int destinationIndex = GetMovementDestinationIndex(character, bestPath, targetCellOccupied: false);
                    Vector2Int nextStep = bestPath[destinationIndex];
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

    private static int GetMovementDestinationIndex(BattleCharacter character, List<Vector2Int> path, bool targetCellOccupied)
    {
        int maxSteps = character != null && character.data != null && character.data.category == CharacterCategory.Animal ? 2 : 1;
        int lastWalkableIndex = targetCellOccupied ? path.Count - 2 : path.Count - 1;
        return Mathf.Clamp(maxSteps, 1, Mathf.Max(1, lastWalkableIndex));
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
        GameAudio.Instance.Play(GameSound.Click);
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
        GameAudio.Instance.Play(isWin ? GameSound.Win : GameSound.Lose);
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
        GameAudio.Instance.Play(skill ? GameSound.Skill : GameSound.Attack);
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

        GameAudio.Instance.Play(isHealing ? GameSound.Heal : GameSound.Hit);
        target.PlayHitEffect(isHealing ? new Color(0.4f, 1f, 0.65f) : new Color(1f, 0.28f, 0.22f));
        ShowFloatingText(target, isHealing ? $"+{amount}" : $"-{amount}", isHealing ? new Color(0.45f, 1f, 0.65f) : new Color(1f, 0.78f, 0.42f));
    }

    public void PlaySkillVfx(BattleCharacter caster, BattleCharacter target, SkillType skillType)
    {
        if (caster == null) return;

        Color color = skillType switch
        {
            SkillType.Fireball => new Color(1f, 0.30f, 0.08f),
            SkillType.Arrow or SkillType.Spear or SkillType.Gun => new Color(1f, 0.88f, 0.42f),
            SkillType.WaterHeal or SkillType.Heal => new Color(0.36f, 0.95f, 1f),
            SkillType.Soil or SkillType.WoodPush => new Color(0.50f, 0.95f, 0.35f),
            SkillType.StunBlow => new Color(0.85f, 0.55f, 1f),
            SkillType.Dragon => new Color(0.95f, 0.35f, 1f),
            _ => new Color(0.62f, 0.92f, 1f)
        };

        caster.PlayCastEffect(color);
        ShowFloatingText(caster, SkillDescription.GetShort(skillType), color);
        HighlightSkillRange(caster, target, skillType, color);

        switch (skillType)
        {
            case SkillType.Arrow:
            case SkillType.Spear:
            case SkillType.Gun:
            case SkillType.Stone:
                if (target != null) StartCoroutine(ProjectileVfxRoutine(caster.transform as RectTransform, target.transform as RectTransform, GetProjectileSymbol(skillType), color));
                break;
            case SkillType.Fireball:
                if (target != null) StartCoroutine(FallingFireVfxRoutine(target.transform as RectTransform));
                break;
            case SkillType.Dragon:
                StartCoroutine(DragonBreathVfxRoutine(caster.transform as RectTransform, target != null ? target.transform as RectTransform : null));
                break;
            case SkillType.Slash:
            case SkillType.TigerTwinClaw:
            case SkillType.StunBlow:
                if (target != null) StartCoroutine(SlashVfxRoutine(target.transform as RectTransform, skillType == SkillType.TigerTwinClaw));
                break;
            case SkillType.WaterHeal:
            case SkillType.Heal:
                if (target != null) StartCoroutine(HealBurstVfxRoutine(target.transform as RectTransform));
                break;
            default:
                if (target != null) StartCoroutine(SkillTrailRoutine(caster.transform as RectTransform, target.transform as RectTransform, color));
                break;
        }
        GameAudio.Instance.Play(skillType == SkillType.WaterHeal || skillType == SkillType.Heal ? GameSound.Heal : GameSound.Skill);
    }

    private void HighlightSkillRange(BattleCharacter caster, BattleCharacter target, SkillType skillType, Color color)
    {
        List<Vector2Int> cells = GetSkillHighlightCells(caster, target, skillType);
        if (cells.Count == 0) return;

        Color highlightColor = color;
        highlightColor.a = 0.82f;
        StartCoroutine(HighlightCellsRoutine(cells, highlightColor, 0.34f));
    }

    private List<Vector2Int> GetSkillHighlightCells(BattleCharacter caster, BattleCharacter target, SkillType skillType)
    {
        var cells = new List<Vector2Int>();
        if (caster == null) return cells;

        switch (skillType)
        {
            case SkillType.Stone:
            case SkillType.Soil:
            case SkillType.HorseCharge:
                AddBoxCells(cells, caster.gridPos, 2);
                break;
            case SkillType.WaterHeal:
            case SkillType.Heal:
            case SkillType.StunBlow:
            case SkillType.TigerTwinClaw:
                AddBoxCells(cells, caster.gridPos, 1);
                break;
            case SkillType.Slash:
                AddDirectionalCells(cells, caster.gridPos, 2, diagonals: true);
                break;
            case SkillType.Arrow:
            case SkillType.Gun:
                AddLineToTargetCells(cells, caster.gridPos, target != null ? target.gridPos : caster.gridPos, maxDistance: rows + cols);
                break;
            case SkillType.Spear:
                AddLineToTargetCells(cells, caster.gridPos, target != null ? target.gridPos : caster.gridPos, maxDistance: 2);
                break;
            case SkillType.Fireball:
                if (target != null) cells.Add(target.gridPos);
                break;
        }

        cells.RemoveAll(pos => pos == caster.gridPos || pos.x < 0 || pos.x >= cols || pos.y < 0 || pos.y >= rows);
        return cells;
    }

    private void AddBoxCells(List<Vector2Int> cells, Vector2Int center, int radius)
    {
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                cells.Add(center + new Vector2Int(dx, dy));
            }
        }
    }

    private void AddDirectionalCells(List<Vector2Int> cells, Vector2Int center, int distance, bool diagonals)
    {
        Vector2Int[] dirs = diagonals
            ? new[]
            {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1)
            }
            : new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in dirs)
        {
            for (int d = 1; d <= distance; d++)
            {
                cells.Add(center + dir * d);
            }
        }
    }

    private void AddLineToTargetCells(List<Vector2Int> cells, Vector2Int from, Vector2Int to, int maxDistance)
    {
        Vector2Int delta = to - from;
        if (delta == Vector2Int.zero) return;
        Vector2Int dir = new Vector2Int(Mathf.Clamp(delta.x, -1, 1), Mathf.Clamp(delta.y, -1, 1));

        for (int d = 1; d <= maxDistance; d++)
        {
            Vector2Int pos = from + dir * d;
            if (pos.x < 0 || pos.x >= cols || pos.y < 0 || pos.y >= rows) break;
            cells.Add(pos);
            if (pos == to) break;
        }
    }

    private IEnumerator HighlightCellsRoutine(List<Vector2Int> cells, Color highlightColor, float duration)
    {
        var originals = new Dictionary<Image, Color>();
        foreach (var pos in cells)
        {
            if (pos.x < 0 || pos.x >= cols || pos.y < 0 || pos.y >= rows) continue;
            if (gridCells == null || gridCells[pos.x, pos.y] == null) continue;
            Transform cell = gridCells[pos.x, pos.y].parent;
            if (cell == null || !cell.TryGetComponent(out Image image)) continue;
            if (!originals.ContainsKey(image))
            {
                originals.Add(image, image.color);
                image.color = Color.Lerp(image.color, highlightColor, 0.72f);
            }
        }

        yield return new WaitForSeconds(duration / Mathf.Max(1f, battleSpeeds[battleSpeedIndex]));

        foreach (var pair in originals)
        {
            if (pair.Key != null)
            {
                pair.Key.color = pair.Value;
            }
        }
    }

    private static string GetProjectileSymbol(SkillType skillType)
    {
        return skillType switch
        {
            SkillType.Arrow => "➤",
            SkillType.Spear => "—",
            SkillType.Gun => "•",
            SkillType.Stone => "●",
            _ => "•"
        };
    }

    private IEnumerator SkillTrailRoutine(RectTransform from, RectTransform to, Color color)
    {
        if (from == null || to == null || transform == null) yield break;

        var obj = new GameObject("SkillTrail", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RegisterBattleVfx(obj);
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.sizeDelta = new Vector2(18f, 18f);
        var image = obj.GetComponent<Image>();
        image.color = color;

        Vector3 start = from.position;
        Vector3 end = to.position;
        float elapsed = 0f;
        float duration = 0.24f / Mathf.Max(1f, battleSpeeds[battleSpeedIndex]);
        while (elapsed < duration)
        {
            if (rect == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rect.position = Vector3.Lerp(start, end, t);
            rect.localScale = Vector3.one * Mathf.Lerp(1.2f, 0.45f, t);
            var c = color;
            c.a = 1f - t * 0.35f;
            image.color = c;
            yield return null;
        }

        DestroyBattleVfx(obj);
    }

    private IEnumerator ProjectileVfxRoutine(RectTransform from, RectTransform to, string symbol, Color color)
    {
        if (from == null || to == null) yield break;

        var obj = CreateVfxText("SkillProjectile", symbol, 38f, color);
        var rect = obj.GetComponent<RectTransform>();
        Vector3 start = from.position;
        Vector3 end = to.position;
        Vector3 delta = end - start;
        rect.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        float duration = 0.34f / Mathf.Max(1f, battleSpeeds[battleSpeedIndex]);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (rect == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rect.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t));
            rect.localScale = Vector3.one * Mathf.Lerp(1.15f, 0.9f, t);
            yield return null;
        }

        DestroyBattleVfx(obj);
    }

    private IEnumerator FallingFireVfxRoutine(RectTransform target)
    {
        if (target == null) yield break;

        Vector3 targetPos = target.position;
        Vector3 start = targetPos + new Vector3(0f, currentBattleCellSize * 1.7f, 0f);
        var fire = CreateVfxText("FallingFire", "火", 42f, new Color(1f, 0.26f, 0.04f));
        var rect = fire.GetComponent<RectTransform>();

        float duration = 0.38f / Mathf.Max(1f, battleSpeeds[battleSpeedIndex]);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (rect == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rect.position = Vector3.Lerp(start, targetPos, t);
            rect.localScale = Vector3.one * Mathf.Lerp(1.2f, 0.82f, t);
            yield return null;
        }

        DestroyBattleVfx(fire);
        yield return BurstTextRoutine(target, "炎", new Color(1f, 0.58f, 0.08f), 52f, 0.22f);
    }

    private IEnumerator DragonBreathVfxRoutine(RectTransform caster, RectTransform target)
    {
        if (caster == null) yield break;

        Vector3 start = caster.position;
        Vector3 end = target != null ? target.position : start + new Vector3(currentBattleCellSize * 2.2f, 0f, 0f);
        Vector3 midpoint = (start + end) * 0.5f;
        Vector3 delta = end - start;
        float length = Mathf.Max(currentBattleCellSize * 1.6f, delta.magnitude);

        var obj = new GameObject("DragonBreath", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RegisterBattleVfx(obj);
        obj.transform.SetParent(transform, false);
        var rect = obj.GetComponent<RectTransform>();
        rect.position = midpoint;
        rect.sizeDelta = new Vector2(length, currentBattleCellSize * 0.62f);
        rect.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        var image = obj.GetComponent<Image>();
        image.color = new Color(0.9f, 0.25f, 1f, 0.74f);

        float duration = 0.34f / Mathf.Max(1f, battleSpeeds[battleSpeedIndex]);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (rect == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rect.localScale = new Vector3(Mathf.Lerp(0.1f, 1f, t), Mathf.Lerp(0.35f, 1.15f, Mathf.Sin(t * Mathf.PI)), 1f);
            var c = image.color;
            c.a = Mathf.Lerp(0.82f, 0f, t);
            image.color = c;
            yield return null;
        }

        DestroyBattleVfx(obj);
    }

    private IEnumerator SlashVfxRoutine(RectTransform target, bool twin)
    {
        if (target == null) yield break;

        yield return BurstTextRoutine(target, "斬", new Color(1f, 0.92f, 0.55f), 52f, 0.18f, -28f);
        if (twin)
        {
            yield return BurstTextRoutine(target, "斬", new Color(1f, 0.72f, 0.40f), 48f, 0.16f, 28f);
        }
    }

    private IEnumerator HealBurstVfxRoutine(RectTransform target)
    {
        if (target == null) yield break;
        yield return BurstTextRoutine(target, "癒", new Color(0.45f, 1f, 0.72f), 48f, 0.30f);
    }

    private IEnumerator BurstTextRoutine(RectTransform parent, string symbol, Color color, float fontSize, float duration, float angle = 0f)
    {
        if (parent == null) yield break;

        var obj = CreateVfxText("SkillBurst", symbol, fontSize, color);
        var rect = obj.GetComponent<RectTransform>();
        rect.position = parent.position;
        rect.rotation = Quaternion.Euler(0f, 0f, angle);

        float elapsed = 0f;
        float scaledDuration = duration / Mathf.Max(1f, battleSpeeds[battleSpeedIndex]);
        while (elapsed < scaledDuration)
        {
            if (rect == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / scaledDuration);
            rect.localScale = Vector3.one * Mathf.Lerp(0.45f, 1.45f, t);
            var text = obj.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                var c = color;
                c.a = 1f - t;
                text.color = c;
            }
            yield return null;
        }

        DestroyBattleVfx(obj);
    }

    private GameObject CreateVfxText(string objectName, string textValue, float fontSize, Color color)
    {
        var obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RegisterBattleVfx(obj);
        obj.transform.SetParent(transform, false);
        obj.transform.SetAsLastSibling();
        var rect = obj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(96f, 64f);

        var text = obj.GetComponent<TextMeshProUGUI>();
        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
        text.text = textValue;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.fontSize = fontSize;
        text.enableAutoSizing = false;
        text.color = color;
        text.raycastTarget = false;

        var shadow = obj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        shadow.effectDistance = new Vector2(2f, -2f);
        return obj;
    }

    private void RegisterBattleVfx(GameObject obj)
    {
        if (obj != null && !activeVfxObjects.Contains(obj))
        {
            activeVfxObjects.Add(obj);
        }
    }

    private void DestroyBattleVfx(GameObject obj)
    {
        if (obj == null) return;
        activeVfxObjects.Remove(obj);
        Destroy(obj);
    }

    private void CleanupBattleVfx()
    {
        for (int i = activeVfxObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = activeVfxObjects[i];
            if (obj != null)
            {
                Destroy(obj);
            }
        }

        activeVfxObjects.Clear();
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
        StartCoroutine(HighlightCellsRoutine(
            GetDragonBreathCells(dragon.gridPos, chosenDir, range),
            new Color(0.9f, 0.25f, 1f, 0.82f),
            0.45f));

        // 攻撃処理：chosenDir 方向の range×range 全員にダメージ
        foreach (Vector2Int pos in GetDragonBreathCells(dragon.gridPos, chosenDir, range))
        {
            if (gridMap.TryGetValue(pos, out var bc) && bc.isAlly != dragon.isAlly && !bc.isDead)
            {
                int dmg = Mathf.RoundToInt(dragon.GetEffectiveAttack(this) * dragon.data.skillPower);
                AddLog($"{dragon.data.characterName} のブレスが {bc.data.characterName} に命中！ {dmg} ダメージ", Color.red);
                bc.TakeDamage(dmg, this, dragon, isBasicAttack: false);
            }
        }

        dragon.UpdateDirection(chosenDir);
        return true;
    }

    private List<Vector2Int> GetDragonBreathCells(Vector2Int origin, Vector2Int dir, int range)
    {
        var cells = new List<Vector2Int>();
        for (int d = 1; d <= range; d++)
        {
            for (int dx = -range + 1; dx <= range - 1; dx++)
            {
                for (int dy = -range + 1; dy <= range - 1; dy++)
                {
                    Vector2Int pos = origin + dir * d;
                    if (dir == Vector2Int.up || dir == Vector2Int.down)
                        pos += new Vector2Int(dx, dy >= 0 ? dy : 0);
                    else
                        pos += new Vector2Int(dx >= 0 ? dx : 0, dy);

                    if (pos.x < 0 || pos.x >= cols || pos.y < 0 || pos.y >= rows) continue;
                    if (!cells.Contains(pos))
                    {
                        cells.Add(pos);
                    }
                }
            }
        }
        return cells;
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
