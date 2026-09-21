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
    private readonly Dictionary<Vector2Int, GameObject> soilTrapVfxObjects = new();
    private readonly Dictionary<Vector2Int, GameObject> soilTrapTintObjects = new();

    private int trapDamage = 5;
    private bool isPaused = false;
    private readonly float[] battleSpeeds = { 0.5f, 1f, 2f, 4f };
    private int battleSpeedIndex = 1;
    private Button speedButton;
    private TextMeshProUGUI speedButtonText;
    private readonly List<GameObject> activeVfxObjects = new();
    private Transform battleVfxOverlay;
    private Transform weaponVfxOverlay;
    private static readonly Dictionary<string, Sprite[]> animatedVfxSprites = new();
    private static readonly Dictionary<string, Texture2D> weaponMotionTextures = new();
    private readonly Dictionary<BattleCharacter, float> pendingDeathRemovalTimes = new();
    private const float skillCinematicScale = 1.85f;
    private float skillCinematicUntil = 0f;
    private float skillImpactWindowUntil = 0f;

    // === Reinforcement fields ===
    private int reinforcementIndex = 0;
    private int reinforcementTotalSpawned = 0;
    private StageData currentStage = null;
    public StageData CurrentStage => currentStage;

    private NumberPassiveSnapshot allyNumberPassiveSnapshot;
    private NumberPassiveSnapshot enemyNumberPassiveSnapshot;

    private struct NumberPassiveSnapshot
    {
        public int lowStrength;
        public int lowAttackBonus;
        public int onePartyCount;
        public int midStrength;
        public int midHealPercent;
        public int highStrength;
        public int highAttackBonus;
        public int completedRounds;

        public bool Matches(NumberPassiveSnapshot other)
        {
            return lowStrength == other.lowStrength
                && lowAttackBonus == other.lowAttackBonus
                && onePartyCount == other.onePartyCount
                && midStrength == other.midStrength
                && midHealPercent == other.midHealPercent
                && highStrength == other.highStrength
                && highAttackBonus == other.highAttackBonus
                && completedRounds == other.completedRounds;
        }
    }

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

        StartCoroutine(StartBattleAfterSetup());
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

    private IEnumerator StartBattleAfterSetup()
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
        soilTrapVfxObjects.Clear();
        soilTrapTintObjects.Clear();
        pendingDeathRemovalTimes.Clear();
        allyNumberPassiveSnapshot = default;
        enemyNumberPassiveSnapshot = default;

        isPaused = false;
        skillCinematicUntil = 0f;
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

        // ボス召喚演出はラスボスステージの登場時だけ。通常ステージや通常の敵生成では出さない。
        if (!ally && data.isBoss && currentStage != null && currentStage.isBossStage)
        {
            Sprite[] summonFrames = LoadAnimatedVfxSprites("BossSummon");
            if (summonFrames.Length > 0)
            {
                StartCoroutine(AnimatedVfxRoutine(rect, summonFrames, "BossSummon"));
            }
        }
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

            ApplyNumberPassivesAtTurnStart("味方", allies, turn, true);

            foreach (var ally in new List<BattleCharacter>(allies))
            {
                if (ally != null) DoAction(ally, "味方");
                yield return BattleWait(0.3f);
            }

            ApplyNumberPassivesAtTurnStart("敵", enemies, turn, false);

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
        float standardDelay = seconds / CurrentBattleSpeed;
        float cinematicRemaining = Mathf.Max(0f, skillCinematicUntil - Time.time);
        return new WaitForSeconds(Mathf.Max(standardDelay, cinematicRemaining));
    }

    private void ApplyNumberPassivesAtTurnStart(string side, List<BattleCharacter> characters, int battleTurn, bool isAlly)
    {
        if (characters == null) return;

        List<BattleCharacter> livingSide = new();
        List<BattleCharacter> numberCharacters = new();
        foreach (BattleCharacter character in characters)
        {
            if (character == null || character.isDead || character.data == null) continue;
            livingSide.Add(character);
            if (character.data.skillType == SkillType.NumberPassive)
            {
                numberCharacters.Add(character);
                character.SetNumberPassiveAttackBonus(0);
            }
        }

        if (numberCharacters.Count == 0) return;

        SkillData tuning = SkillCatalog.Get(SkillType.NumberPassive);
        bool isOneOnlyParty = livingSide.Count > 0 && livingSide.TrueForAll(character => character.data.characterName == "一");
        int onePartyCount = isOneOnlyParty ? livingSide.Count : 0;
        int otherNumberTypeCount = Mathf.Max(0, CountNumberTypes(numberCharacters) - 1);
        int standardStrength = GetNumberTierStrength(otherNumberTypeCount);
        int lowStrength = isOneOnlyParty
            ? GetNumberTierStrength(Mathf.Max(0, onePartyCount - 1))
            : standardStrength;
        int midStrength = standardStrength;
        int highStrength = standardStrength;

        int lowAttackBonus = isOneOnlyParty
            ? GetOnePartyAttackBonus(tuning, onePartyCount)
            : GetNumberTierValue(lowStrength,
                tuning != null ? tuning.numberPassiveBonus1 : 15,
                tuning != null ? tuning.numberPassiveBonus2 : 30,
                tuning != null ? tuning.numberPassiveBonus3 : 50);
        int midHealPercent = GetNumberTierValue(midStrength,
            tuning != null ? tuning.numberPassiveMidHealWeak : 5,
            tuning != null ? tuning.numberPassiveMidHealMedium : 10,
            tuning != null ? tuning.numberPassiveMidHealStrong : 15);
        int highBonusPerRound = GetNumberTierValue(highStrength,
            tuning != null ? tuning.numberPassiveHighBonusPerRoundWeak : 3,
            tuning != null ? tuning.numberPassiveHighBonusPerRoundMedium : 5,
            tuning != null ? tuning.numberPassiveHighBonusPerRoundStrong : 10);
        int highBonusCap = GetNumberTierValue(highStrength,
            tuning != null ? tuning.numberPassiveHighBonusCapWeak : 100,
            tuning != null ? tuning.numberPassiveHighBonusCapMedium : 200,
            tuning != null ? tuning.numberPassiveHighBonusCapStrong : 300);
        int completedRounds = Mathf.Max(0, battleTurn - 1);
        float highMultiplier = Mathf.Pow(1f + highBonusPerRound / 100f, completedRounds);
        int highAttackBonus = Mathf.Min(highBonusCap, Mathf.RoundToInt((highMultiplier - 1f) * 100f));

        var current = new NumberPassiveSnapshot
        {
            lowStrength = lowStrength,
            lowAttackBonus = lowAttackBonus,
            onePartyCount = onePartyCount,
            midStrength = midStrength,
            midHealPercent = midHealPercent,
            highStrength = highStrength,
            highAttackBonus = highAttackBonus,
            completedRounds = completedRounds
        };

        NumberPassiveSnapshot previous = isAlly ? allyNumberPassiveSnapshot : enemyNumberPassiveSnapshot;
        if (!current.Matches(previous))
        {
            LogNumberPassiveState(side, current, isOneOnlyParty);
            if (isAlly) allyNumberPassiveSnapshot = current;
            else enemyNumberPassiveSnapshot = current;
        }

        int auraIndex = 0;
        foreach (BattleCharacter character in numberCharacters)
        {
            switch (character.data.category)
            {
                case CharacterCategory.Number1:
                    character.SetNumberPassiveAttackBonus(lowAttackBonus);
                    if (lowStrength > 0)
                    {
                        StartCoroutine(PlayNumberAuraAfterDelay(character, lowStrength, isOneOnlyParty && onePartyCount >= 4, auraIndex++ * 0.04f));
                    }
                    break;

                case CharacterCategory.Number2:
                    ApplyNumberPassiveHeal(character, midHealPercent);
                    if (midStrength > 0)
                    {
                        StartCoroutine(PlayNumberAuraAfterDelay(character, midStrength, false, auraIndex++ * 0.04f));
                    }
                    break;

                case CharacterCategory.Number3:
                    character.SetNumberPassiveAttackBonus(highAttackBonus);
                    if (highStrength > 0)
                    {
                        StartCoroutine(PlayNumberAuraAfterDelay(character, highStrength, false, auraIndex++ * 0.04f));
                    }
                    break;
            }
        }
    }

    private static int CountNumberTypes(List<BattleCharacter> characters)
    {
        HashSet<string> names = new();
        foreach (BattleCharacter character in characters)
        {
            names.Add(character.data.characterName);
        }
        return names.Count;
    }

    private static int GetNumberTierStrength(int typeCount)
    {
        return Mathf.Clamp(typeCount, 0, 3);
    }

    private static int GetNumberTierValue(int strength, int weak, int medium, int strong)
    {
        return strength switch
        {
            1 => weak,
            2 => medium,
            3 => strong,
            _ => 0
        };
    }

    private static int GetOnePartyAttackBonus(SkillData tuning, int oneCount)
    {
        int weak = tuning != null ? tuning.numberPassiveBonus1 : 15;
        int medium = tuning != null ? tuning.numberPassiveBonus2 : 30;
        int four = tuning != null ? tuning.numberPassiveOneBonus4 : 150;
        int fiveOrMore = tuning != null ? tuning.numberPassiveOneBonus5 : 300;
        return oneCount switch
        {
            2 => weak,
            3 => medium,
            4 => four,
            >= 5 => fiveOrMore,
            _ => 0
        };
    }

    private void ApplyNumberPassiveHeal(BattleCharacter character, int healPercent)
    {
        if (character == null || healPercent <= 0) return;

        int maxHp = character.data.GetMaxHP(character.level);
        int heal = Mathf.CeilToInt(maxHp * healPercent / 100f);
        int before = character.currentHP;
        character.currentHP = Mathf.Min(maxHp, character.currentHP + heal);
        int actualHeal = character.currentHP - before;
        if (actualHeal <= 0) return;

        character.UpdateHPBar();
        PlayDamageVfx(character, actualHeal, isHealing: true);
        AddLog($"{character.DisplayName} の中位数字効果: HPを {actualHeal} 回復", new Color(0.45f, 1f, 0.65f));
    }

    private void LogNumberPassiveState(string side, NumberPassiveSnapshot state, bool isOneOnlyParty)
    {
        Color color = new Color(0.78f, 0.92f, 1f);
        if (state.lowStrength > 0)
        {
            string label = isOneOnlyParty ? $"一染め {state.onePartyCount}体" : $"他数字 {state.lowStrength}種";
            AddLog($"{side} 数字結束: {label}で攻撃力+{state.lowAttackBonus}%", color);
        }
        if (state.midStrength > 0)
        {
            AddLog($"{side} 数字結束: 中位 / 他数字 {state.midStrength}種でターン開始時HP+{state.midHealPercent}%", color);
        }
        if (state.highStrength > 0)
        {
            AddLog($"{side} 数字結束: 上位 / 他数字 {state.highStrength}種 / 経過{state.completedRounds}ラウンドで攻撃力+{state.highAttackBonus}%", color);
        }
    }

    private IEnumerator PlayNumberAuraAfterDelay(BattleCharacter target, int strength, bool onePartyPeak, float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        PlayNumberAuraVfx(target, strength, onePartyPeak);
    }

    private void StartSkillCinematic(float seconds)
    {
        float scaledSeconds = seconds * skillCinematicScale / CurrentBattleSpeed;
        skillCinematicUntil = Mathf.Max(skillCinematicUntil, Time.time + scaledSeconds);
    }

    private void BeginSkillImpactWindow(float seconds)
    {
        float scaledSeconds = seconds * skillCinematicScale / CurrentBattleSpeed;
        skillImpactWindowUntil = Mathf.Max(skillImpactWindowUntil, Time.unscaledTime + scaledSeconds);
    }

    private float SkillVfxDuration(float seconds)
    {
        return seconds * skillCinematicScale / CurrentBattleSpeed;
    }

    private float CurrentBattleSpeed => Mathf.Max(0.1f, battleSpeeds[battleSpeedIndex]);

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
        AddLog($"バトル速度 x{CurrentBattleSpeed:0.#}", Color.cyan);
        UpdateSpeedButtonLabel();
    }

    private void UpdateSpeedButtonLabel()
    {
        if (speedButtonText != null)
        {
            speedButtonText.text = $"x{CurrentBattleSpeed:0.#}";
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

        BattleCharacter skillTarget = FindSkillTarget(character);

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
                    MoveCharacter(character, path, destinationIndex, side);
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
                        MoveCharacter(character, bestPath, destinationIndex, side);
                        return;
                    }
                }
                AddLog($"{side} {character.data.characterName} は動けない！", Color.gray);
            }
        }
    }

    private BattleCharacter FindSkillTarget(BattleCharacter character)
    {
        if (character == null || character.data == null) return null;

        switch (character.data.skillType)
        {
            case SkillType.Slash:
                return TargetingService.FindSwordTarget(this, character);
            case SkillType.Arrow:
                return TargetingService.FindArrowTarget(this, character);
            case SkillType.Spear:
                return TargetingService.FindSpearTarget(this, character);
            case SkillType.StunBlow:
            case SkillType.WoodPush:
            case SkillType.BirdRetreat:
            case SkillType.TigerTwinClaw:
                return TargetingService.FindAdjacentEnemy(this, character);
            case SkillType.Stone:
                return TargetingService.FindStoneTarget(this, character);
            case SkillType.Gun:
                return TargetingService.FindNearestEnemy(this, character, character.isAlly ? enemies : allies);
            case SkillType.Soil:
            case SkillType.Dragon:
                return character;
            case SkillType.Fireball:
                return FindRandomLivingTarget(character.isAlly ? enemies : allies);
            case SkillType.WaterHeal:
                return TargetingService.FindAdjacentAlly(this, character);
            case SkillType.HorseCharge:
                return TargetingService.FindHorseChargeTarget(this, character, character.isAlly ? enemies : allies);
            default:
                return null;
        }
    }

    private static BattleCharacter FindRandomLivingTarget(List<BattleCharacter> candidates)
    {
        if (candidates == null) return null;

        List<BattleCharacter> living = new();
        foreach (BattleCharacter candidate in candidates)
        {
            if (candidate != null && !candidate.isDead)
            {
                living.Add(candidate);
            }
        }

        return living.Count > 0 ? living[Random.Range(0, living.Count)] : null;
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
        MoveCharacter(character, new List<Vector2Int> { character.gridPos, newPos }, 1, side);
    }

    private void MoveCharacter(BattleCharacter character, List<Vector2Int> path, int destinationIndex, string side)
    {
        Vector2Int oldPos = character.gridPos;
        Vector2Int newPos = path[destinationIndex];
        gridMap.Remove(oldPos);
        character.gridPos = newPos;
        gridMap[newPos] = character;

        StartCoroutine(SmoothMoveAlongPath(character, path, destinationIndex));
        GameAudio.Instance.Play(GameSound.Click);
        AddLog($"{side} {character.data.characterName} が移動！", Color.white);

        // 移動方向を更新
        Vector2Int dir = newPos - oldPos;
        character.UpdateDirection(dir);

        if (trapCells.Remove(newPos))
        {
            // 到着前の座標で被弾VFXが出ないよう、移動アニメーションの完了後に解決する。
            StartCoroutine(ResolveTrapAfterMovement(character, newPos, destinationIndex, side));
        }

        if (soilTrapCells.Contains(newPos))
        {
            if (character.data.skillType != SkillType.Soil)
            {
                AddLog($"{side} {character.data.characterName} は土の罠の上に立っている！", Color.yellow);
            }
        }
    }

    private IEnumerator ResolveTrapAfterMovement(BattleCharacter character, Vector2Int landingPos, int stepCount, string side)
    {
        yield return new WaitForSeconds(0.14f * Mathf.Max(1, stepCount));
        if (character == null || character.isDead || character.gridPos != landingPos) yield break;

        AddLog($"{side} {character.data.characterName} は罠にかかった！", Color.magenta);
        character.TakeDamage(trapDamage, this, isBasicAttack: false);
    }

    public IEnumerator SmoothMoveAlongPath(BattleCharacter character, List<Vector2Int> path, int destinationIndex)
    {
        if (path == null || path.Count <= 1) yield break;

        int lastIndex = Mathf.Clamp(destinationIndex, 1, path.Count - 1);
        for (int i = 1; i <= lastIndex; i++)
        {
            Vector2Int step = path[i];
            if (gridCells == null || step.x < 0 || step.x >= cols || step.y < 0 || step.y >= rows) yield break;
            yield return SmoothMove(character, gridCells[step.x, step.y], 0.14f);
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
        bool isSkillImpact = !isHealing && Time.unscaledTime < skillImpactWindowUntil;
        target.PlayHitEffect(
            isHealing ? new Color(0.4f, 1f, 0.65f) : new Color(1f, 0.28f, 0.22f),
            emphasize: isSkillImpact);
        if (isSkillImpact) StartSkillCinematic(0.60f);
        ShowFloatingText(target, isHealing ? $"+{amount}" : $"-{amount}", isHealing ? new Color(0.45f, 1f, 0.65f) : new Color(1f, 0.78f, 0.42f));
    }

    public void PlaySkillVfx(BattleCharacter caster, BattleCharacter target, SkillType skillType)
    {
        if (caster == null) return;

        StartSkillCinematic(0.5f);
        BeginSkillImpactWindow(0.50f);
        KeepDefeatedCharacterVisibleForSkillVfx(target, skillType);

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
        // 発動予告として対象を短く明滅させる。命中時には PlayDamageVfx の
        // 強調ヒットへ引き継がれるので、火球や水滴の着弾先も追いやすくなる。
        if (target != null && target != caster)
        {
            target.PlaySkillTargetCue(color);
        }
        // 実行時のマス塗りは、着弾後も残って見えるため使わない。
        // 対象範囲は演出図鑑で確認し、盤面では素材そのものを主役にする。
        if (skillType == SkillType.Slash || skillType == SkillType.StunBlow)
        {
            PlayWeaponMotionVfx(caster, target, skillType);
        }
        else if (skillType == SkillType.Spear)
        {
            PlaySpearThrustVfx(caster, target);
        }
        else if (skillType is SkillType.Arrow or SkillType.Gun or SkillType.Stone)
        {
            if (target != null)
            {
                string folder = skillType == SkillType.Arrow ? "Arrow" : skillType == SkillType.Gun ? "Gun" : "Stone";
                StartCoroutine(SpriteProjectileVfxRoutine(
                    caster.transform as RectTransform,
                    target.transform as RectTransform,
                    folder,
                    useArc: skillType == SkillType.Stone));
            }
        }
        else if (skillType is SkillType.Fireball or SkillType.WaterHeal)
        {
            if (target != null)
            {
                string folder = skillType == SkillType.WaterHeal ? "WaterHeal" : "Fireball";
                StartCoroutine(FallingSpriteVfxRoutine(target.transform as RectTransform, folder));
            }
        }
        else if (skillType == SkillType.WoodPush)
        {
            if (target != null) StartCoroutine(WoodPushSpriteVfxRoutine(caster.transform as RectTransform, target.transform as RectTransform));
        }
        else if (skillType == SkillType.HorseCharge)
        {
            // 移動演出は実際に突進移動できた場合だけ PerformHorseCharge から開始する。
        }
        else if (skillType == SkillType.BirdRetreat)
        {
            // 消失元・出現先の羽は、実際の退避座標を確定できる PerformBirdRetreat 側で出す。
        }
        else if (skillType == SkillType.TigerTwinClaw)
        {
            if (target != null) StartCoroutine(TigerTwinClawSpriteVfxRoutine(target.transform.position));
        }
        else
        {
            PlayAnimatedSkillVfx(caster, target != null ? target : caster, skillType);
        }

        switch (skillType)
        {
            case SkillType.Arrow:
            case SkillType.Gun:
            case SkillType.Stone:
            case SkillType.Spear:
            case SkillType.Fireball:
            case SkillType.WoodPush:
            case SkillType.HorseCharge:
            case SkillType.BirdRetreat:
                break;
            case SkillType.Dragon:
                // ブレスと咆哮は実際に発動した種類ごとに専用処理する。
                break;
            case SkillType.Slash:
                if (target != null)
                {
                    Vector2Int direction = TargetingService.GetSwordAttackDirection(caster, target);
                    foreach (BattleCharacter victim in TargetingService.GetSwordWedgeTargets(this, caster, direction))
                    {
                        StartCoroutine(SlashVfxRoutine(victim.transform as RectTransform, false));
                    }
                }
                break;
            case SkillType.StunBlow:
                if (target != null) StartCoroutine(SlashVfxRoutine(target.transform as RectTransform, false));
                break;
            case SkillType.TigerTwinClaw:
                // 二段目を反対向きの爪痕として専用ルーチン内で描く。
                break;
            case SkillType.WaterHeal:
            case SkillType.Heal:
                break;
        }
        GameAudio.Instance.PlaySkillSound(skillType);
    }

    public void PlayGunLineVfx(BattleCharacter caster, Vector2Int dir)
    {
        if (caster == null) return;

        dir = new Vector2Int(Mathf.Clamp(dir.x, -1, 1), Mathf.Clamp(dir.y, -1, 1));
        if (dir == Vector2Int.zero) return;

        StartSkillCinematic(0.5f);
        BeginSkillImpactWindow(0.50f);

        Color color = new Color(1f, 0.88f, 0.42f);
        caster.PlayCastEffect(color);
        List<Vector2Int> cells = GetLineCellsFromDirection(caster.gridPos, dir, rows + cols);

        RectTransform from = caster.transform as RectTransform;
        Vector2Int endPos = cells.Count > 0 ? cells[cells.Count - 1] : caster.gridPos + dir;
        RectTransform to = null;
        if (endPos.x >= 0 && endPos.x < cols && endPos.y >= 0 && endPos.y < rows && gridCells != null)
        {
            to = gridCells[endPos.x, endPos.y] as RectTransform;
        }

        if (from != null && to != null)
        {
            StartCoroutine(SpriteProjectileVfxRoutine(from, to, "Gun", useArc: false));
        }

        GameAudio.Instance.PlaySkillSound(SkillType.Gun);
    }

    private void PlayAnimatedSkillVfx(BattleCharacter target, SkillType skillType)
    {
        PlayAnimatedSkillVfx(null, target, skillType);
    }

    private void PlayAnimatedSkillVfx(BattleCharacter caster, BattleCharacter target, SkillType skillType)
    {
        if (target == null) return;

        string folder = skillType switch
        {
            SkillType.Arrow => "Arrow",
            SkillType.Spear => "Spear",
            SkillType.Gun => "Gun",
            SkillType.StunBlow => "StunBlow",
            SkillType.Soil => "Soil",
            SkillType.Slash => "Slash",
            SkillType.TigerTwinClaw => "TigerTwinClaw",
            SkillType.WoodPush => "WoodPush",
            SkillType.WaterHeal => "WaterHeal",
            SkillType.Stone => "Stone",
            SkillType.Fireball => "Fireball",
            SkillType.BirdRetreat => "BirdRetreat",
            SkillType.HorseCharge => "HorseCharge",
            SkillType.Counter => "Shield",
            SkillType.AreaCounter => "Wall",
            SkillType.Armor => "Armor",
            SkillType.Dragon => "DragonBreath",
            _ => null
        };

        // 剣と槌は右向きの連番素材を基準にしている。発動者より左の標的へ
        // 攻撃する時だけ反転し、振りの向きが攻撃方向と一致するようにする。
        bool mirrorHorizontal = caster != null
            && target != caster
            && target.transform.position.x < caster.transform.position.x
            && (skillType == SkillType.Slash || skillType == SkillType.StunBlow);
        PlayAnimatedFolderVfx(target, folder, mirrorHorizontal);
    }

    private void PlaySpearThrustVfx(BattleCharacter caster, BattleCharacter target)
    {
        if (caster == null || target == null) return;
        Sprite sprite = LoadSpearStaticSprite();
        if (sprite == null)
        {
            Debug.LogWarning("[VFX] 槍の突き素材を読み込めませんでした。");
            return;
        }
        StartCoroutine(SpearThrustSingleSpriteVfxRoutine(
            caster.transform as RectTransform,
            target.transform as RectTransform,
            caster.gridPos,
            target.gridPos,
            sprite));
    }

    private void PlayWeaponMotionVfx(BattleCharacter caster, BattleCharacter target, SkillType skillType)
    {
        if (caster == null || target == null) return;

        string weapon = skillType == SkillType.Slash ? "SwordMotion" : "HammerMotion";
        Texture2D texture = LoadWeaponMotionTexture(weapon);
        if (texture == null)
        {
            Debug.LogWarning($"[VFX] {weapon} の素材を読み込めませんでした。");
            return;
        }

        if (skillType == SkillType.StunBlow)
        {
            StartCoroutine(HammerSwingVfxRoutine(caster, target, texture));
            return;
        }

        StartCoroutine(WeaponMotionVfxRoutine(
            caster,
            target,
            texture,
            skillType));
    }

    private static Texture2D LoadWeaponMotionTexture(string weapon)
    {
        if (weaponMotionTextures.TryGetValue(weapon, out Texture2D cached)) return cached;

        Texture2D texture = Resources.Load<Texture2D>($"VFX/Weapons/{weapon}");
        if (texture == null) return null;

        weaponMotionTextures[weapon] = texture;
        return texture;
    }

    private IEnumerator WeaponMotionVfxRoutine(
        BattleCharacter caster,
        BattleCharacter target,
        Texture2D texture,
        SkillType skillType)
    {
        if (caster == null || target == null || texture == null) yield break;

        // 武器はSpriteのタイトメッシュやWebGLのスプライト圧縮を経由させない。
        // RawImageで元PNGをそのまま描くことで、確認ページと同じ不透明度を保つ。
        // 発動者コマの子にすると、後続セルが伸びた刃を覆ってしまう。そのため
        // BattlePanel直下の最前面レイヤーで、盤面と同じCanvas座標へ直接描画する。
        var obj = new GameObject($"WeaponMotion_{skillType}", typeof(RectTransform));
        RegisterBattleVfx(obj);
        var rect = obj.GetComponent<RectTransform>();
        RectTransform casterRect = caster.transform as RectTransform;
        RectTransform targetRect = target.transform as RectTransform;
        if (casterRect == null || targetRect == null) yield break;
        rect.SetParent(EnsureWeaponVfxOverlay(), false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.position = casterRect.position;
        rect.pivot = new Vector2(0.13f, 0.16f);
        rect.SetAsLastSibling();

        RawImage[] imageLayers = CreateWeaponVfxLayers(rect, texture);

        Vector2Int gridDelta = TargetingService.GetSwordAttackDirection(caster, target);
        if (gridDelta == Vector2Int.zero) yield break;

        // 画面上の位置差はレイアウト更新中に丸められる場合があり、斜め方向が上下左右と
        // 同じ角度になることがある。演出の向きは判定そのものに使った盤面方向から決める。
        float attackAngle = GetWeaponVfxAttackAngle(gridDelta);
        int distance = Mathf.Max(1, Mathf.Max(Mathf.Abs(gridDelta.x), Mathf.Abs(gridDelta.y)));
        // 発動者中心を支点にしても対象マスまで刃先が届くよう、素材の見た目を
        // 元の約1.5倍にする。到達範囲は武器画像の
        // 大きさではなくスキル判定で決まるため、前方3マスの効果範囲は変わらない。
        float size = currentBattleCellSize * 1.59f;
        rect.sizeDelta = Vector2.one * size;

        // 素材は柄尻から鋒へ右上に伸びる。剣は対象方向を中心に、前方3マスを
        // 上側から下側へ薙ぐ。右攻撃なら右上から右下、左攻撃なら左上から左下になる。
        const float swordTipAngle = 45f;
        const float visibleSweepHalfAngle = 45f;
        const float preWindAngle = 30f;
        const float preWindEnd = 0.10f;
        const float sweepEnd = 0.35f;
        float sweepSign = gridDelta.x < 0 ? -1f : 1f;
        float fullStartAngle = attackAngle + sweepSign * visibleSweepHalfAngle - swordTipAngle;
        float fullEndAngle = attackAngle - sweepSign * visibleSweepHalfAngle - swordTipAngle;
        // 見える刀身は前方3マスを90度で横切る。手元の短い30度の振りかぶりを加え、
        // 全体では120度の勢いを保つ。
        float preWindStartAngle = fullStartAngle + sweepSign * preWindAngle;
        // 近接武器はスキル用のスローモーション中でも切れ味を失わないよう、
        // 槍より短い一振りで終える。
        float duration = SkillVfxDuration(0.18f);
        float elapsed = 0f;

        while (elapsed < duration && obj != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float preWindProgress = Mathf.InverseLerp(0f, preWindEnd, progress);
            float sweepProgress = Mathf.InverseLerp(preWindEnd, sweepEnd, progress);
            if (progress < preWindEnd)
            {
                rect.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(
                    preWindStartAngle,
                    fullStartAngle,
                    Mathf.SmoothStep(0f, 1f, preWindProgress)));
            }
            else
            {
                // 薙ぎ払いは終盤ほど速度が上がる。開始時の角速度を抑え、下側の
                // 効果マスへ一気に振り抜くことで重い剣らしい加速感を出す。
                float acceleratedSweep = sweepProgress * sweepProgress * sweepProgress;
                rect.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(
                    fullStartAngle,
                    fullEndAngle,
                    acceleratedSweep));
            }

            // 振りかぶりの30度だけは発動者マス内に留め、前方へ刃先を向けた時点で
            // 全長へ伸ばす。対象外のマスを攻撃しているようには見せない。
            float reach = Mathf.Lerp(0.28f, 1f, Mathf.SmoothStep(0f, 1f, preWindProgress));
            float scale = Mathf.Lerp(0.96f, 1.04f, Mathf.Sin(progress * Mathf.PI)) * reach;
            rect.localScale = Vector3.one * scale;

            SetWeaponVfxAlpha(imageLayers, GetWeaponVfxAlpha(progress));
            yield return null;
        }

        if (obj != null) DestroyBattleVfx(obj);
    }

    private IEnumerator HammerSwingVfxRoutine(BattleCharacter caster, BattleCharacter target, Texture2D texture)
    {
        if (caster == null || target == null || texture == null) yield break;
        RectTransform casterRect = caster.transform as RectTransform;
        RectTransform targetRect = target.transform as RectTransform;
        if (casterRect == null || targetRect == null) yield break;

        // 剣と同じく、元PNGを直接描画してWebGL側のSpriteメッシュ差をなくす。
        // 盤面セルに覆われない最前面の通常UIレイヤーへ置く。
        var obj = new GameObject("HammerSwing", typeof(RectTransform));
        RegisterBattleVfx(obj);
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(EnsureWeaponVfxOverlay(), false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        // 槌頭を支点にすると柄だけが回って見えるため、実際の柄尻を固定点にする。
        // 支点は対象側へ動かさず、発動者マスの中心に固定する。
        rect.pivot = new Vector2(0.18f, 0.19f);
        // 発動者中心の柄尻を固定したまま槌頭が対象マスまで届くよう、素材だけを
        // 元の約1.5倍にする。支点・回転量・判定範囲は変えない。
        rect.sizeDelta = Vector2.one * currentBattleCellSize * 1.62f;
        rect.SetAsLastSibling();

        RawImage[] imageLayers = CreateWeaponVfxLayers(rect, texture);

        Vector2Int gridDelta = target.gridPos - caster.gridPos;
        if (gridDelta == Vector2Int.zero) yield break;

        // 素材は柄尻から槌頭へ右上へ伸びる。発動者の柄尻を固定したまま、
        // 槌頭が対象方向の上側から約70度で振り下ろされる角度を作る。
        const float sourceHeadAngle = 40f;
        const float swingDegrees = 70f;
        float attackAngle = GetWeaponVfxAttackAngle(gridDelta);
        float swingSign = gridDelta.x < 0 ? -1f : 1f;
        float startAngle = attackAngle + swingSign * swingDegrees - sourceHeadAngle;
        float impactAngle = attackAngle - sourceHeadAngle;
        float duration = SkillVfxDuration(0.22f);
        float elapsed = 0f;
        while (elapsed < duration && obj != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            // 槌は重力に引かれるように終盤ほど速く振り下ろし、着弾後は短く保持する。
            float fallProgress = Mathf.Clamp01(progress / 0.62f);
            float gravityFall = fallProgress * fallProgress;
            rect.position = casterRect.position;
            rect.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(startAngle, impactAngle, gravityFall));
            rect.localScale = Vector3.one * Mathf.Lerp(0.88f, 0.98f, gravityFall);

            SetWeaponVfxAlpha(imageLayers, GetWeaponVfxAlpha(progress));
            yield return null;
        }

        if (obj != null) DestroyBattleVfx(obj);
    }

    private static RawImage[] CreateWeaponVfxLayers(RectTransform parent, Texture2D texture)
    {
        // 生成物の柔らかいアルファ境界がWebGLのUI合成で薄く見えないよう、
        // 同じPNGを重ねて最終的な不透明度を安定させる。
        const int layerCount = 3;
        var layers = new RawImage[layerCount];
        for (int index = 0; index < layerCount; index++)
        {
            var layer = new GameObject($"WeaponVisualLayer_{index + 1:00}", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            var layerRect = layer.GetComponent<RectTransform>();
            layerRect.SetParent(parent, false);
            layerRect.anchorMin = Vector2.zero;
            layerRect.anchorMax = Vector2.one;
            layerRect.offsetMin = Vector2.zero;
            layerRect.offsetMax = Vector2.zero;

            var image = layer.GetComponent<RawImage>();
            image.texture = texture;
            image.raycastTarget = false;
            image.color = Color.white;
            layers[index] = image;
        }

        return layers;
    }

    private static void SetWeaponVfxAlpha(RawImage[] layers, float alpha)
    {
        if (layers == null) return;
        foreach (RawImage layer in layers)
        {
            if (layer == null) continue;
            Color color = layer.color;
            color.a = alpha;
            layer.color = color;
        }
    }

    private static float GetWeaponVfxAttackAngle(Vector2Int gridDirection)
    {
        // GridLayoutGroupではYが上から下へ増える。一方UIの回転では上が正のYなので反転する。
        Vector2 visualDirection = new Vector2(gridDirection.x, -gridDirection.y).normalized;
        return Mathf.Atan2(visualDirection.y, visualDirection.x) * Mathf.Rad2Deg;
    }

    private static float GetWeaponVfxAlpha(float progress)
    {
        // 攻撃の途中から薄くなると、もっとも見せたい着弾姿勢が影のように見える。
        // 消滅はごく最後だけに留め、通常速度でも次手へ残らない短さを維持する。
        return FadeOutVfxAlpha(1f, progress, 0.94f);
    }

    private static float FadeOutVfxAlpha(float initialAlpha, float progress, float fadeStart)
    {
        // SmoothStepの第1・第2引数は進行率の範囲ではなく出力値。進行率は先に
        // InverseLerpで0～1へ正規化してから補間する。
        float fadeProgress = Mathf.InverseLerp(fadeStart, 1f, progress);
        return Mathf.Lerp(initialAlpha, 0f, Mathf.SmoothStep(0f, 1f, fadeProgress));
    }

    private static float FadeInVfxAlpha(float targetAlpha, float progress, float fadeEnd)
    {
        float fadeProgress = Mathf.InverseLerp(0f, fadeEnd, progress);
        return Mathf.Lerp(0f, targetAlpha, Mathf.SmoothStep(0f, 1f, fadeProgress));
    }

    private static Sprite LoadSpearStaticSprite()
    {
        const string cacheKey = "SpearStaticHorizontalAirWisp";
        if (animatedVfxSprites.TryGetValue(cacheKey, out Sprite[] cached))
        {
            return cached.Length > 0 ? cached[0] : null;
        }

        Texture2D texture = Resources.Load<Texture2D>("VFX/Animated/Spear/frame_03");
        if (texture == null)
        {
            Debug.LogWarning("[VFX] 槍の代表画像がResourcesから見つかりません。");
            animatedVfxSprites[cacheKey] = System.Array.Empty<Sprite>();
            return null;
        }

        // 横向き素材の実描画範囲だけを採用する。空白や旧フレーム領域を含めない。
        float height = Mathf.Min(texture.height, Mathf.Round(texture.width * 0.344f));
        float cropX = texture.width * 0.0625f;
        float width = texture.width * 0.72f;
        Rect spriteRect = new Rect(cropX, (texture.height - height) * 0.5f, width, height);
        Sprite sprite = Sprite.Create(texture, spriteRect, new Vector2(0.025f, 0.5f), 100f);
        animatedVfxSprites[cacheKey] = new[] { sprite };
        return sprite;
    }

    private static Sprite LoadHorseStaticSprite()
    {
        const string cacheKey = "HorseStaticForkedWind";
        if (animatedVfxSprites.TryGetValue(cacheKey, out Sprite[] cached))
        {
            return cached.Length > 0 ? cached[0] : null;
        }

        Texture2D texture = Resources.Load<Texture2D>("VFX/Static/HorseChargeForkedWind");
        if (texture == null)
        {
            Debug.LogWarning("[VFX] 馬の代表画像がResourcesから見つかりません。");
            animatedVfxSprites[cacheKey] = System.Array.Empty<Sprite>();
            return null;
        }

        // 馬の前方で左右に分かれる、真上視点の風圧と砂埃を使う。
        Rect spriteRect = new Rect(0f, 0f, texture.width, texture.height);
        Sprite sprite = Sprite.Create(texture, spriteRect, new Vector2(0.5f, 0.5f), 100f);
        animatedVfxSprites[cacheKey] = new[] { sprite };
        return sprite;
    }

    private static Sprite LoadBirdStaticSprite()
    {
        const string cacheKey = "BirdStaticCleanFeathers";
        if (animatedVfxSprites.TryGetValue(cacheKey, out Sprite[] cached))
        {
            return cached.Length > 0 ? cached[0] : null;
        }

        Texture2D texture = Resources.Load<Texture2D>("VFX/Static/BirdFeathersOnly");
        if (texture == null)
        {
            Debug.LogWarning("[VFX] 鳥の代表画像がResourcesから見つかりません。");
            animatedVfxSprites[cacheKey] = System.Array.Empty<Sprite>();
            return null;
        }

        // 鳥専用の羽だけを使う。地面の土埃やワープ軌跡を含む旧シートは参照しない。
        Rect spriteRect = new Rect(0f, 0f, texture.width, texture.height);
        Sprite sprite = Sprite.Create(texture, spriteRect, new Vector2(0.5f, 0.5f), 100f);
        animatedVfxSprites[cacheKey] = new[] { sprite };
        return sprite;
    }

    // 最大まで伸びた一枚絵を、発動者側の柄尻から前方だけへ伸ばす。
    private IEnumerator SpearThrustSingleSpriteVfxRoutine(
        RectTransform caster,
        RectTransform target,
        Vector2Int casterGridPos,
        Vector2Int targetGridPos,
        Sprite sprite)
    {
        if (caster == null || target == null || sprite == null) yield break;

        Vector2Int gridDelta = targetGridPos - casterGridPos;
        if (gridDelta == Vector2Int.zero) yield break;

        Image image = CreateSingleSpriteVfx("SingleVfx_SpearThrust", EnsureBattleVfxOverlay(), sprite, out RectTransform rect);
        Vector2Int direction = new Vector2Int(Mathf.Clamp(gridDelta.x, -1, 1), Mathf.Clamp(gridDelta.y, -1, 1));
        // 斜め2マスは直線距離が長いため、縦横と同じ幅では先端が届かない。
        float reachCells = 2f * new Vector2(direction.x, direction.y).magnitude;
        float width = currentBattleCellSize * (reachCells + 0.24f);
        float aspect = sprite.rect.width / sprite.rect.height;
        rect.pivot = new Vector2(0.025f, 0.5f);
        rect.position = caster.position;
        rect.rotation = Quaternion.Euler(0f, 0f, GetWeaponVfxAttackAngle(gridDelta));
        rect.sizeDelta = new Vector2(width, width / aspect);
        rect.SetAsLastSibling();

        float duration = SkillVfxDuration(0.26f);
        float elapsed = 0f;
        while (elapsed < duration && image != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float reach = Mathf.SmoothStep(0.05f, 1f, Mathf.Clamp01(progress / 0.34f));
            rect.localScale = new Vector3(reach, 1f + Mathf.Sin(progress * Mathf.PI) * 0.04f, 1f);
            Color current = image.color;
            current.a = FadeOutVfxAlpha(1f, progress, 0.70f);
            image.color = current;
            yield return null;
        }

        if (rect != null) DestroyBattleVfx(rect.gameObject);
    }

    public void PlayDefensiveVfx(BattleCharacter target, SkillType skillType)
    {
        if (target == null) return;
        PlayAnimatedSkillVfx(target, skillType);
    }

    private void PlayNumberAuraVfx(BattleCharacter target, int strength, bool onePartyPeak)
    {
        if (target == null || target.data == null) return;

        string tier = target.data.category switch
        {
            CharacterCategory.Number1 => "Low",
            CharacterCategory.Number2 => "Mid",
            CharacterCategory.Number3 => "High",
            _ => null
        };
        if (string.IsNullOrEmpty(tier)) return;

        string strengthName = strength <= 1 ? "Weak" : strength == 2 ? "Medium" : "Strong";
        string folder = $"NumberAura_{tier}_{strengthName}";
        Sprite sprite = LoadRepresentativeVfxSprite(folder);
        if (sprite != null)
        {
            StartCoroutine(NumberAuraSpriteVfxRoutine(target, sprite, folder, onePartyPeak));
        }
    }

    private void PlayAnimatedFolderVfx(BattleCharacter target, string folder, bool mirrorHorizontal = false)
    {
        if (target == null || string.IsNullOrEmpty(folder)) return;
        Sprite sprite = LoadRepresentativeVfxSprite(folder);
        if (sprite == null) return;

        if (folder.StartsWith("NumberAura_"))
        {
            StartCoroutine(NumberAuraSpriteVfxRoutine(target, sprite, folder, false));
            return;
        }

        StartCoroutine(TargetSpriteVfxRoutine(target, sprite, folder, mirrorHorizontal));
    }

    private Sprite LoadRepresentativeVfxSprite(string folder)
    {
        Sprite[] frames = LoadAnimatedVfxSprites(folder);
        if (frames.Length == 0) return null;

        if (folder == "NumberAura_High_Strong")
        {
            return CreateSafeHighStrongAuraSprite(frames[Mathf.Min(7, frames.Length - 1)]);
        }

        int keyFrame = folder switch
        {
            "Arrow" or "Gun" => 2,
            "Stone" => 0,
            "Fireball" => 2,
            "WoodPush" => 3,
            "WaterHeal" => 3,
            "Soil" => 3,
            "HorseCharge" or "BirdRetreat" => 2,
            "TigerTwinClaw" => 3,
            "Shield" or "Armor" or "Wall" => 2,
            "DragonBreath" or "DragonRoar" => 3,
            _ => 1
        };
        return frames[Mathf.Clamp(keyFrame, 0, frames.Length - 1)];
    }

    private static Sprite CreateSafeHighStrongAuraSprite(Sprite source)
    {
        if (source == null) return null;

        // 隣接コマの混入がない代表フレームを使い、端だけを安全に除外する。
        const float inset = 16f;
        Rect sourceRect = source.rect;
        float width = Mathf.Max(1f, sourceRect.width - inset * 2f);
        float height = Mathf.Max(1f, sourceRect.height - inset * 2f);
        Rect safeRect = new Rect(sourceRect.x + inset, sourceRect.y + inset, width, height);
        return Sprite.Create(source.texture, safeRect, new Vector2(0.5f, 0.5f), source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
    }

    private Image CreateSingleSpriteVfx(string objectName, Transform parent, Sprite sprite, out RectTransform rect)
    {
        var obj = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RegisterBattleVfx(obj);
        rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        var image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = Color.white;
        return image;
    }

    private IEnumerator TargetSpriteVfxRoutine(BattleCharacter target, Sprite sprite, string folder, bool mirrorHorizontal)
    {
        if (target == null || sprite == null) yield break;

        bool defensive = folder is "Shield" or "Armor" or "Wall";
        Image image = CreateSingleSpriteVfx($"SingleVfx_{folder}", defensive ? target.transform : EnsureBattleVfxOverlay(), sprite, out RectTransform rect);
        if (defensive)
        {
            rect.anchoredPosition = Vector2.zero;
        }
        else
        {
            rect.position = target.transform.position;
            rect.gameObject.AddComponent<BattleVfxFollowTarget>().Initialize(target.transform as RectTransform);
        }

        float sizeScale = folder switch
        {
            "Wall" => 1.34f,
            "DragonRoar" => 1.5f,
            "TigerTwinClaw" => 1.16f,
            _ => 1f
        };
        rect.sizeDelta = Vector2.one * currentBattleCellSize * sizeScale;
        rect.SetAsLastSibling();

        float duration = SkillVfxDuration(folder switch
        {
            // 防御演出は防いだことを読める時間だけ保持してからゆっくり消す。
            "Shield" or "Armor" or "Wall" => 0.52f,
            "DragonRoar" => 0.5f,
            _ => 0.34f
        });
        float elapsed = 0f;
        while (elapsed < duration && image != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float pulse;
            if (folder == "DragonRoar")
            {
                pulse = Mathf.Lerp(0.35f, 1.35f, Mathf.Sin(progress * Mathf.PI * 0.5f));
            }
            else if (folder == "Wall")
            {
                // 石壁はキャラの前面で一気に立ち上がり、わずかに沈んで収まる。
                float rise = Mathf.Clamp01(progress * 4.2f);
                pulse = progress < 0.28f
                    ? Mathf.Lerp(0.44f, 1.13f, rise)
                    : Mathf.Lerp(1.13f, 1f, Mathf.InverseLerp(0.28f, 0.78f, progress));
            }
            else
            {
                pulse = Mathf.Lerp(0.72f, 1.04f, Mathf.Sin(progress * Mathf.PI * 0.5f));
            }
            float horizontal = mirrorHorizontal ? -pulse : pulse;
            rect.localScale = new Vector3(horizontal, pulse, 1f);

            float rotation = folder switch
            {
                "TigerTwinClaw" => Mathf.Lerp(-10f, 14f, progress),
                "DragonRoar" => progress * 28f,
                _ => 0f
            };
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            if (folder == "Wall")
            {
                float rise = Mathf.Clamp01(progress * 4.2f);
                rect.anchoredPosition = Vector2.up * Mathf.Lerp(-currentBattleCellSize * 0.17f, 0f, rise);
            }

            Color current = image.color;
            float fadeStart = folder switch
            {
                "Shield" or "Armor" or "Wall" => 0.84f,
                "DragonRoar" => 0.68f,
                _ => 0.76f
            };
            current.a = FadeOutVfxAlpha(1f, progress, fadeStart);
            image.color = current;
            yield return null;
        }

        if (rect != null) DestroyBattleVfx(rect.gameObject);
    }

    private IEnumerator NumberAuraSpriteVfxRoutine(BattleCharacter target, Sprite sprite, string folder, bool onePartyPeak)
    {
        if (target == null || sprite == null) yield break;

        Image image = CreateSingleSpriteVfx("SingleVfx_NumberAura", target.transform, sprite, out RectTransform rect);
        rect.anchoredPosition = Vector2.zero;
        float sizeScale = folder == "NumberAura_High_Strong" ? 1.04f : 1.18f;
        if (onePartyPeak) sizeScale *= 1.14f;
        rect.sizeDelta = Vector2.one * currentBattleCellSize * sizeScale;
        rect.SetAsLastSibling();

        float duration = SkillVfxDuration(onePartyPeak ? 0.84f : 0.72f);
        float elapsed = 0f;
        while (elapsed < duration && image != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float scale = Mathf.Lerp(0.82f, 1.1f, Mathf.Sin(progress * Mathf.PI * 0.5f));
            rect.localScale = Vector3.one * scale;
            rect.localRotation = Quaternion.Euler(0f, 0f, progress * 110f);
            Color current = image.color;
            current.a = FadeOutVfxAlpha(1f, progress, 0.78f);
            image.color = current;
            yield return null;
        }

        if (rect != null) DestroyBattleVfx(rect.gameObject);
    }

    private void PlayStableVfxFrame(BattleCharacter target, Sprite[] frames, string folder, bool mirrorHorizontal = false)
    {
        int frameIndex = 0;
        var obj = new GameObject($"StableVfx_{folder}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RegisterBattleVfx(obj);
        var rect = obj.GetComponent<RectTransform>();
        bool defensiveVfx = folder is "Shield" or "Armor" or "Wall";
        // 防御系は対象コマの直接の子にして必ずキャラの前面へ出す。一方で盤面全体の
        // 攻撃オーバーレイや武器の後ろに留まるため、発動中の攻撃を覆わない。
        rect.SetParent(defensiveVfx ? target.transform : EnsureBattleVfxOverlay(), false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        if (defensiveVfx)
        {
            rect.anchoredPosition = Vector2.zero;
        }
        else
        {
            rect.position = target.transform.position;
            obj.AddComponent<BattleVfxFollowTarget>().Initialize(target.transform as RectTransform);
        }
        rect.localScale = new Vector3(mirrorHorizontal ? -1f : 1f, 1f, 1f);
        if (folder == "BossSummon")
        {
            rect.position += Vector3.up * currentBattleCellSize * 0.62f;
        }
        float scale = folder switch
        {
            "Slash" or "Shield" or "Armor" => 1.0f,
            "DragonBreath" => 1.0f,
            "Wall" => 1.25f,
            _ => 1.0f
        };
        rect.sizeDelta = Vector2.one * currentBattleCellSize * scale;
        rect.SetAsLastSibling();
        var image = obj.GetComponent<Image>();
        image.sprite = frames[frameIndex];
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = Color.white;
        var animator = obj.AddComponent<BattleVfxAnimator>();
        float baseDuration = folder switch
        {
            "Slash" => 0.25f,
            "Shield" or "Armor" or "Wall" => 0.24f,
            "DragonBreath" => 0.56f,
            _ => 0.36f
        };
        float duration = SkillVfxDuration(baseDuration);
        // 多くの素材は末尾2コマが消え際の薄い残像。そこまでを均等再生すると
        // 本体より残像の時間が長く見えるため、戦闘では見える6コマを基準にする。
        int visibleFrameCount = Mathf.Min(6, frames.Length);
        animator.Play(frames, duration, visibleFrameCount, false);
    }

    private static Sprite[] LoadAnimatedVfxSprites(string folder)
    {
        if (animatedVfxSprites.TryGetValue(folder, out Sprite[] cached)) return cached;

        var frames = new List<Sprite>();
        for (int index = 0; index < 32; index++)
        {
            string path = $"VFX/Animated/{folder}/frame_{index:00}";
            // 各PNGはUnity上で複数スプライトに分割されている場合があるため、
            // Spriteではなく元のTexture全体から中央pivotのフレームを作る。
            // これによりフレームごとの切り出し矩形差による位置ずれや背景混入を防ぐ。
            Texture2D texture = Resources.Load<Texture2D>(path);
            if (texture != null)
            {
                frames.Add(Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f));
                continue;
            }

            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite == null) break;
            frames.Add(sprite);
        }

        cached = frames.ToArray();
        animatedVfxSprites[folder] = cached;
        return cached;
    }

    private IEnumerator AnimatedVfxRoutine(RectTransform target, Sprite[] frames, string folder)
    {
        if (target == null || frames == null || frames.Length == 0) yield break;

        // バトルVFXはトップ画面と同じImage経路で描画する。
        var obj = new GameObject($"AnimatedVfx_{folder}", typeof(RectTransform));
        RegisterBattleVfx(obj);
        var rect = obj.GetComponent<RectTransform>();
        // 対象キャラクターと同じ盤面枝に置く。BattlePanel直下の別レイヤーでは
        // WebGLのUIバッチ順により、VFXが盤面の奥へ隠れることがあった。
        rect.SetParent(target, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        float sizeScale = folder switch
        {
            "DragonBreath" => 2.4f,
            "Slash" or "Shield" or "Armor" => 1.0f,
            "BossSummon" => 1.35f,
            "Wall" => 1.25f,
            "NumberAura_Low_Weak" or "NumberAura_Low_Medium" or "NumberAura_Low_Strong"
                or "NumberAura_Mid_Weak" or "NumberAura_Mid_Medium" or "NumberAura_Mid_Strong"
                or "NumberAura_High_Weak" or "NumberAura_High_Medium" or "NumberAura_High_Strong" => 1.2f,
            _ => 1.1f
        };
        rect.sizeDelta = Vector2.one * currentBattleCellSize * sizeScale;
        if (folder == "BossSummon")
        {
            // 素材の地面がセル画像の下端にあるため、亀裂・召喚地点をキャラの足元へ合わせる。
            rect.anchoredPosition += Vector2.up * currentBattleCellSize * 0.62f;
        }
        rect.SetAsLastSibling();

        // WebGLでは同一Imageのspriteを毎フレーム差し替えると、素材本体ではなく
        // 透明な残像だけが残る端末があった。各コマを独立Imageとして一度だけ設定し、
        // 表示状態だけを切り替えることで、静止検証と同じ確実な描画経路に統一する。
        var frameImages = new Image[frames.Length];
        int visibleFrame = folder == "Slash" ? Mathf.Min(3, frames.Length - 1) : 0;
        for (int index = 0; index < frames.Length; index++)
        {
            var frameObject = new GameObject($"Frame_{index:00}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RegisterBattleVfx(frameObject);
            var frameRect = frameObject.GetComponent<RectTransform>();
            // 中間の空RectTransformを挟むと、WebGLのUIバッチで画像が落ちることがある。
            // 静止検証と同じく、各コマを対象の直接の子として描画する。
            frameRect.SetParent(target, false);
            frameRect.anchorMin = new Vector2(0.5f, 0.5f);
            frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.anchoredPosition = rect.anchoredPosition;
            frameRect.sizeDelta = rect.sizeDelta;
            frameRect.SetAsLastSibling();

            var frameImage = frameObject.GetComponent<Image>();
            frameImage.sprite = frames[index];
            frameImage.raycastTarget = false;
            frameImage.preserveAspect = true;
            frameImage.color = Color.white;
            frameObject.SetActive(index == visibleFrame);
            frameImages[index] = frameImage;
        }

        float initialAlpha = 1f;
        yield return null;

        float baseDuration = folder switch
        {
            "Slash" => 0.25f,
            "Shield" or "Armor" => 0.24f,
            _ => 0.72f
        };
        float duration = SkillVfxDuration(baseDuration);
        float elapsed = 0f;
        while (elapsed < duration && obj != null)
        {
            elapsed += Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            float progress = Mathf.Clamp01(elapsed / duration);
            float animationProgress = progress;
            int frame;
            if (folder == "Slash")
            {
                // 末尾の土煙だけのフレームが長いと、半透明で消えたように見える。
                // 剣が見える0～5を長めに使い、残煙の6～7は最後だけ再生する。
                frame = animationProgress < 0.80f
                    ? Mathf.Min(5, Mathf.FloorToInt(animationProgress / 0.80f * 6f))
                    : Mathf.Min(frames.Length - 1, 6 + Mathf.FloorToInt((animationProgress - 0.80f) / 0.20f * 2f));
            }
            else
            {
                frame = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(progress * frames.Length));
            }
            if (frame != visibleFrame)
            {
                frameImages[visibleFrame].gameObject.SetActive(false);
                frameImages[frame].gameObject.SetActive(true);
                visibleFrame = frame;
            }

            Color current = frameImages[visibleFrame].color;
            float fadeStart = folder == "Slash" ? 0.96f : 0.65f;
            current.a = FadeOutVfxAlpha(initialAlpha, progress, fadeStart);
            frameImages[visibleFrame].color = current;
            yield return null;
        }

        foreach (Image frameImage in frameImages)
        {
            if (frameImage != null) DestroyBattleVfx(frameImage.gameObject);
        }
        if (obj != null) DestroyBattleVfx(obj);
    }

    private void HighlightSkillRange(BattleCharacter caster, BattleCharacter target, SkillType skillType, Color color)
    {
        List<Vector2Int> selectionCells = GetSkillSelectionCells(caster, target, skillType);
        List<Vector2Int> effectCells = GetSkillEffectCells(caster, target, skillType);
        // 戦闘中は青い選択候補を塗らない。選択ルールはスキル説明・演出図鑑で
        // 確認できるため、実行時は実際に効果が及ぶマスだけを短く示す。
        selectionCells.Clear();
        if (selectionCells.Count > 0 || effectCells.Count > 0)
        {
            float duration = skillType switch
            {
                SkillType.Slash => 0.20f,
                SkillType.Spear => 0.24f,
                _ => 0.34f
            };
            StartCoroutine(HighlightSkillRangesRoutine(selectionCells, effectCells, duration));
        }
    }

    private List<Vector2Int> GetSkillSelectionCells(BattleCharacter caster, BattleCharacter target, SkillType skillType)
    {
        var cells = new List<Vector2Int>();
        if (caster == null) return cells;

        switch (skillType)
        {
            case SkillType.Stone:
            case SkillType.Fireball:
            case SkillType.HorseCharge:
            case SkillType.Soil:
                AddBoxCells(cells, caster.gridPos, 2);
                break;
            case SkillType.Slash:
                if (target != null)
                {
                    cells.AddRange(TargetingService.GetSwordWedgeCells(
                        caster.gridPos,
                        TargetingService.GetSwordAttackDirection(caster, target)));
                }
                break;
            case SkillType.StunBlow:
            case SkillType.TigerTwinClaw:
            case SkillType.WaterHeal:
            case SkillType.BirdRetreat:
            case SkillType.WoodPush:
                AddDirectionalCells(cells, caster.gridPos, 1, diagonals: true);
                break;
            case SkillType.Arrow:
            case SkillType.Gun:
                AddLineToTargetCells(cells, caster.gridPos, target != null ? target.gridPos : caster.gridPos, maxDistance: rows + cols);
                break;
            case SkillType.Spear:
                if (target != null)
                {
                    Vector2Int delta = target.gridPos - caster.gridPos;
                    Vector2Int dir = new Vector2Int(Mathf.Clamp(delta.x, -1, 1), Mathf.Clamp(delta.y, -1, 1));
                    cells.Add(caster.gridPos + dir);
                    cells.Add(caster.gridPos + dir * 2);
                }
                break;
        }

        cells.RemoveAll(pos => pos == caster.gridPos || pos.x < 0 || pos.x >= cols || pos.y < 0 || pos.y >= rows);
        return cells;
    }

    private List<Vector2Int> GetSkillEffectCells(BattleCharacter caster, BattleCharacter target, SkillType skillType)
    {
        var cells = new List<Vector2Int>();
        if (caster == null) return cells;

        switch (skillType)
        {
            case SkillType.Slash:
                if (target != null)
                {
                    cells.AddRange(TargetingService.GetSwordWedgeCells(
                        caster.gridPos,
                        TargetingService.GetSwordAttackDirection(caster, target)));
                }
                break;
            case SkillType.StunBlow:
            case SkillType.TigerTwinClaw:
            case SkillType.Arrow:
            case SkillType.Stone:
            case SkillType.Fireball:
            case SkillType.WaterHeal:
            case SkillType.WoodPush:
            case SkillType.HorseCharge:
            case SkillType.BirdRetreat:
                if (target != null) cells.Add(target.gridPos);
                break;
            case SkillType.Spear:
                AddLineToTargetCells(cells, caster.gridPos, target != null ? target.gridPos : caster.gridPos, maxDistance: 2);
                break;
            case SkillType.Gun:
                AddLineToTargetCells(cells, caster.gridPos, target != null ? target.gridPos : caster.gridPos, maxDistance: rows + cols);
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

    private List<Vector2Int> GetLineCellsFromDirection(Vector2Int from, Vector2Int dir, int maxDistance)
    {
        var cells = new List<Vector2Int>();
        if (dir == Vector2Int.zero) return cells;

        for (int d = 1; d <= maxDistance; d++)
        {
            Vector2Int pos = from + dir * d;
            if (pos.x < 0 || pos.x >= cols || pos.y < 0 || pos.y >= rows) break;
            cells.Add(pos);
        }

        return cells;
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

        yield return new WaitForSeconds(SkillVfxDuration(duration));

        foreach (var pair in originals)
        {
            if (pair.Key != null)
            {
                pair.Key.color = pair.Value;
            }
        }
    }

    private IEnumerator HighlightSkillRangesRoutine(
        List<Vector2Int> selectionCells,
        List<Vector2Int> effectCells,
        float duration)
    {
        var originals = new Dictionary<Image, Color>();
        var selectionColor = new Color(0.22f, 0.65f, 1f, 0.58f);
        var effectColor = new Color(1f, 0.48f, 0.12f, 0.78f);

        void Apply(List<Vector2Int> cells, Color color)
        {
            foreach (var pos in cells)
            {
                if (pos.x < 0 || pos.x >= cols || pos.y < 0 || pos.y >= rows) continue;
                if (gridCells == null || gridCells[pos.x, pos.y] == null) continue;
                Transform cell = gridCells[pos.x, pos.y].parent;
                if (cell == null || !cell.TryGetComponent(out Image image)) continue;
                if (!originals.ContainsKey(image)) originals.Add(image, image.color);
                image.color = Color.Lerp(originals[image], color, 0.72f);
            }
        }

        Apply(selectionCells, selectionColor);
        Apply(effectCells, effectColor);
        yield return new WaitForSeconds(SkillVfxDuration(duration));

        foreach (var pair in originals)
        {
            if (pair.Key != null) pair.Key.color = pair.Value;
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
        rect.SetParent(EnsureBattleVfxOverlay(), false);
        rect.sizeDelta = new Vector2(18f, 18f);
        var image = obj.GetComponent<Image>();
        image.color = color;

        Vector3 start = from.position;
        Vector3 end = to.position;
        float elapsed = 0f;
        float duration = SkillVfxDuration(0.24f);
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

    private IEnumerator SpriteProjectileVfxRoutine(RectTransform from, RectTransform to, string folder, bool useArc)
    {
        if (from == null || to == null) yield break;
        Sprite sprite = LoadRepresentativeVfxSprite(folder);
        if (sprite == null) yield break;

        Image image = CreateSingleSpriteVfx($"SingleProjectile_{folder}", EnsureBattleVfxOverlay(), sprite, out RectTransform rect);
        Vector3 start = from.position;
        Vector3 end = to.position;
        Vector3 delta = end - start;
        float baseAngle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        float sizeScale = folder switch
        {
            // 矢・弾素材は正方形の透明余白が広いので、実体の太さが一マス内で読める倍率にする。
            "Gun" => 1.28f,
            "Arrow" => 1.06f,
            "Stone" => 0.68f,
            _ => 0.7f
        };
        rect.sizeDelta = Vector2.one * currentBattleCellSize * sizeScale;
        rect.position = start;
        rect.SetAsLastSibling();

        float duration = SkillVfxDuration(folder == "Stone" ? 0.38f : 0.26f);
        float elapsed = 0f;
        while (elapsed < duration && image != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float travel = Mathf.SmoothStep(0f, 1f, progress);
            Vector3 position = Vector3.Lerp(start, end, travel);
            if (useArc)
            {
                position += Vector3.up * Mathf.Sin(progress * Mathf.PI) * currentBattleCellSize * 0.68f;
            }
            rect.position = position;
            float rotation = folder == "Stone" ? progress * 410f : baseAngle;
            rect.rotation = Quaternion.Euler(0f, 0f, rotation);
            rect.localScale = Vector3.one * Mathf.Lerp(0.82f, 1.04f, Mathf.Sin(progress * Mathf.PI * 0.5f));
            Color current = image.color;
            current.a = FadeOutVfxAlpha(1f, progress, 0.9f);
            image.color = current;
            yield return null;
        }

        if (rect != null) DestroyBattleVfx(rect.gameObject);
    }

    private IEnumerator FallingSpriteVfxRoutine(RectTransform target, string folder)
    {
        if (target == null) yield break;
        Sprite sprite = LoadRepresentativeVfxSprite(folder);
        if (sprite == null) yield break;

        Image image = CreateSingleSpriteVfx($"SingleFalling_{folder}", EnsureBattleVfxOverlay(), sprite, out RectTransform rect);
        bool isWaterHeal = folder == "WaterHeal";
        Vector3 end = target.position;
        Vector3 start = end + Vector3.up * currentBattleCellSize * (isWaterHeal ? 1.45f : 1.7f);
        rect.sizeDelta = Vector2.one * currentBattleCellSize * (isWaterHeal ? 1.08f : 1.24f);
        rect.position = start;
        rect.SetAsLastSibling();

        float duration = SkillVfxDuration(0.34f);
        float elapsed = 0f;
        while (elapsed < duration && image != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            rect.position = Vector3.Lerp(start, end, progress * progress);
            // 素材自体が縦向きなので、落下中にも回転させず真上から落とす。
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * Mathf.Lerp(isWaterHeal ? 0.92f : 1.14f, isWaterHeal ? 0.82f : 0.86f, progress);
            Color current = image.color;
            current.a = FadeOutVfxAlpha(isWaterHeal ? 0.88f : 1f, progress, 0.82f);
            image.color = current;
            yield return null;
        }

        if (rect != null) DestroyBattleVfx(rect.gameObject);
    }

    private IEnumerator WoodPushSpriteVfxRoutine(RectTransform caster, RectTransform target)
    {
        if (caster == null || target == null) yield break;
        Sprite sprite = LoadRepresentativeVfxSprite("WoodPush");
        if (sprite == null) yield break;

        Image image = CreateSingleSpriteVfx("SingleVfx_WoodPush", EnsureBattleVfxOverlay(), sprite, out RectTransform rect);
        Vector3 delta = target.position - caster.position;
        rect.pivot = new Vector2(0.08f, 0.5f);
        rect.position = caster.position;
        rect.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        float width = Mathf.Max(currentBattleCellSize * 1.62f, delta.magnitude + currentBattleCellSize * 0.54f);
        rect.sizeDelta = new Vector2(width, currentBattleCellSize * 1.06f);
        rect.SetAsLastSibling();

        float duration = SkillVfxDuration(0.36f);
        float elapsed = 0f;
        while (elapsed < duration && image != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float reach = Mathf.SmoothStep(0.08f, 1f, Mathf.Clamp01(progress * 1.35f));
            rect.localScale = new Vector3(reach, 1f + Mathf.Sin(progress * Mathf.PI) * 0.08f, 1f);
            Color current = image.color;
            current.a = FadeOutVfxAlpha(1f, progress, 0.72f);
            image.color = current;
            yield return null;
        }

        if (rect != null) DestroyBattleVfx(rect.gameObject);
    }

    private IEnumerator HorseChargeSpriteVfxRoutine(RectTransform caster, RectTransform target)
    {
        if (caster == null || target == null) yield break;
        Sprite sprite = LoadHorseStaticSprite();
        if (sprite == null) yield break;

        // 馬の後方で左右に開く風圧。中央を空けて、馬の駒を覆わないようにする。
        Image image = CreateSingleSpriteVfx("SingleVfx_HorseChargeForkedWind", EnsureBattleVfxOverlay(), sprite, out RectTransform rect);
        Vector3 start = caster.position;
        Vector3 end = target.position;
        Vector3 delta = end - start;
        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        Vector3 forward = delta.sqrMagnitude > 0.001f ? delta.normalized : Vector3.right;
        rect.sizeDelta = new Vector2(currentBattleCellSize * 1.82f, currentBattleCellSize * 1.28f);
        rect.SetAsLastSibling();

        // 発生点を馬の後方に固定し、二本の砂埃を進行方向の逆へ開いて消す。
        const float duration = 0.42f;
        float elapsed = 0f;
        while (elapsed < duration && image != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float travel = Mathf.SmoothStep(0f, 1f, progress);
            Vector3 horsePosition = Vector3.Lerp(start, end, travel);

            // 素材は右向き。反転して、V字の開きが馬の後方へ流れるように配置する。
            rect.position = horsePosition - forward * currentBattleCellSize * (0.72f + 0.16f * travel);
            rect.rotation = Quaternion.Euler(0f, 0f, angle + 180f);
            rect.localScale = Vector3.one * (0.62f + 0.38f * travel);
            Color color = image.color;
            color.a = FadeOutVfxAlpha(0.88f, progress, 0.74f);
            image.color = color;
            yield return null;
        }

        if (rect != null) DestroyBattleVfx(rect.gameObject);
    }

    private IEnumerator BirdRetreatSpriteVfxRoutine(Vector3 position, bool arriving, float delay)
    {
        Sprite sprite = LoadBirdStaticSprite();
        if (sprite == null) yield break;
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);

        // 退避元では真上へ舞い上がり、出現先では真上から舞い落ちる。
        // どちらも盤面オーバーレイへ固定し、斜め移動や地面の軌跡にはしない。
        Image image = CreateSingleSpriteVfx("SingleVfx_BirdRetreat", EnsureBattleVfxOverlay(), sprite, out RectTransform rect);
        rect.position = position;
        rect.sizeDelta = Vector2.one * currentBattleCellSize * 0.88f;
        rect.SetAsLastSibling();

        const float duration = 0.34f;
        float elapsed = 0f;
        while (elapsed < duration && image != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float travel = progress * progress * (3f - 2f * progress);
            float verticalOffset = currentBattleCellSize * 0.62f;
            rect.position = position + Vector3.up * (arriving
                ? Mathf.Lerp(verticalOffset, 0f, travel)
                : Mathf.Lerp(0f, verticalOffset, travel));
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * (arriving
                ? Mathf.Lerp(1.00f, 0.80f, travel)
                : Mathf.Lerp(0.70f, 0.96f, travel));
            Color current = image.color;
            current.a = arriving
                ? FadeInVfxAlpha(0.55f, progress, 0.22f) * FadeOutVfxAlpha(1f, progress, 0.76f)
                : FadeOutVfxAlpha(0.55f, progress, 0.62f);
            image.color = current;
            yield return null;
        }

        if (rect != null) DestroyBattleVfx(rect.gameObject);
    }

    private IEnumerator TigerTwinClawSpriteVfxRoutine(Vector3 targetPosition)
    {
        Sprite sprite = LoadRepresentativeVfxSprite("TigerTwinClaw");
        if (sprite == null) yield break;

        yield return TigerClawStrokeVfxRoutine(targetPosition, sprite, false);
        yield return new WaitForSecondsRealtime(SkillVfxDuration(0.035f));
        yield return TigerClawStrokeVfxRoutine(targetPosition, sprite, true);
    }

    private IEnumerator TigerClawStrokeVfxRoutine(Vector3 targetPosition, Sprite sprite, bool mirrorHorizontal)
    {
        if (sprite == null) yield break;

        Image image = CreateSingleSpriteVfx("SingleVfx_TigerTwinClaw", EnsureBattleVfxOverlay(), sprite, out RectTransform rect);
        rect.position = targetPosition;
        rect.sizeDelta = Vector2.one * currentBattleCellSize * 1.16f;
        rect.SetAsLastSibling();

        float duration = SkillVfxDuration(0.18f);
        float elapsed = 0f;
        while (elapsed < duration && image != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float scale = Mathf.Lerp(0.66f, 1.08f, Mathf.SmoothStep(0f, 1f, progress));
            rect.localScale = new Vector3(scale, scale, 1f);
            // 左右反転だけでは対角線の差が読み取りづらいため、二撃目は明確に交差する軌道へ回す。
            rect.localRotation = Quaternion.Euler(0f, 0f, mirrorHorizontal ? 82f : -8f);
            Color current = image.color;
            current.a = FadeOutVfxAlpha(1f, progress, 0.72f);
            image.color = current;
            yield return null;
        }

        if (rect != null) DestroyBattleVfx(rect.gameObject);
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
        obj.transform.SetParent(EnsureBattleVfxOverlay(), false);
        var rect = obj.GetComponent<RectTransform>();
        rect.position = midpoint;
        rect.sizeDelta = new Vector2(length, currentBattleCellSize * 0.62f);
        rect.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        var image = obj.GetComponent<Image>();
        image.color = new Color(0.9f, 0.25f, 1f, 0.74f);

        float duration = SkillVfxDuration(0.34f);
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

    private void PlayDragonBreathVfx(BattleCharacter dragon, Vector2Int direction, int range)
    {
        if (dragon == null || range <= 0) return;

        Sprite sprite = LoadRepresentativeVfxSprite("DragonBreath");
        if (sprite == null) return;

        StartCoroutine(DragonBreathAnimatedVfxRoutine(
            dragon.transform as RectTransform,
            direction,
            range,
            sprite));
    }

    private IEnumerator DragonBreathAnimatedVfxRoutine(
        RectTransform dragon,
        Vector2Int direction,
        int range,
        Sprite sprite)
    {
        if (dragon == null || sprite == null) yield break;

        // gridPos の Y は盤面では下向き、RectTransform の Y は上向き。
        // 炎素材は左の細い端が発生点、右の広い端が到達側なので、Y 座標だけ変換する。
        Vector3 forward = new Vector3(direction.x, -direction.y, 0f).normalized;
        if (forward == Vector3.zero) yield break;

        var obj = new GameObject("AnimatedVfx_DragonBreath", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RegisterBattleVfx(obj);
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(EnsureBattleVfxOverlay(), false);

        // 素材は右向き。細い発生点を竜の中心に固定し、広い炎だけを前方3マスへ伸ばす。
        float cellSpan = currentBattleCellSize * range;
        rect.pivot = new Vector2(0f, 0.5f);
        rect.position = dragon.position + forward * currentBattleCellSize * 0.06f;
        rect.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg);
        rect.sizeDelta = new Vector2(cellSpan * 1.18f, cellSpan * 1.18f);
        rect.SetAsLastSibling();

        var image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = new Color(1f, 1f, 1f, 0.96f);

        float duration = SkillVfxDuration(0.86f);
        float elapsed = 0f;
        while (elapsed < duration && obj != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float reach = Mathf.SmoothStep(0.18f, 1f, Mathf.Clamp01(progress * 1.45f));
            rect.localScale = new Vector3(reach, Mathf.Lerp(0.62f, 1.05f, Mathf.Sin(progress * Mathf.PI)), 1f);
            Color current = image.color;
            current.a = FadeOutVfxAlpha(0.96f, progress, 0.72f);
            image.color = current;
            yield return null;
        }

        if (obj != null) DestroyBattleVfx(obj);
    }

    private IEnumerator SlashVfxRoutine(RectTransform target, bool twin)
    {
        if (target == null) yield break;

        // 斬撃が見えてから着弾を伝える。文字を出さず、対象だけを短く揺らす。
        yield return new WaitForSeconds(SkillVfxDuration(twin ? 0.08f : 0.06f));
        if (target == null) yield break;

        Vector2 origin = target.anchoredPosition;
        float duration = SkillVfxDuration(twin ? 0.18f : 0.16f);
        float amplitude = currentBattleCellSize * (twin ? 0.11f : 0.095f);
        float elapsed = 0f;
        while (elapsed < duration && target != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float envelope = 1f - progress;
            float frequency = twin ? 34f : 28f;
            target.anchoredPosition = origin + new Vector2(
                Mathf.Sin(elapsed * frequency) * amplitude * envelope,
                Mathf.Cos(elapsed * frequency * 0.82f) * amplitude * 0.42f * envelope);
            yield return null;
        }

        if (target != null) target.anchoredPosition = origin;
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
        float scaledDuration = SkillVfxDuration(duration);
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

    private Transform EnsureBattleVfxOverlay()
    {
        if (battleVfxOverlay != null)
        {
            return battleVfxOverlay;
        }

        // UI座標系はBattlePanelと共有しつつ、盤面より確実に前へ描く。
        // このCanvasはGraphicRaycasterを持たないため操作を遮らない。
        var overlay = new GameObject("BattleVfxOverlay", typeof(RectTransform));
        overlay.transform.SetParent(transform, false);

        var rect = overlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        var canvas = overlay.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingLayerID = parentCanvas != null ? parentCanvas.sortingLayerID : 0;
        // キャラクター本体の個別 Canvas（+10）より確実に前面へ出す。
        // 防御系はこのレイヤーを使わないため、攻撃演出だけがキャラを覆える。
        canvas.sortingOrder = (parentCanvas != null ? parentCanvas.sortingOrder : 0) + 20;

        overlay.transform.SetAsLastSibling();

        battleVfxOverlay = overlay.transform;
        return battleVfxOverlay;
    }

    private Transform EnsureWeaponVfxOverlay()
    {
        if (weaponVfxOverlay != null)
        {
            return weaponVfxOverlay;
        }

        // 子Canvasを追加せずBattlePanelの通常の描画順を使う。GridLayoutGroup内の
        // 各セルより後ろではなく後に置くことで、長い武器の全体を盤面の上へ描ける。
        var overlay = new GameObject("WeaponVfxOverlay", typeof(RectTransform));
        overlay.transform.SetParent(transform, false);

        var rect = overlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        overlay.transform.SetAsLastSibling();

        weaponVfxOverlay = overlay.transform;
        return weaponVfxOverlay;
    }

    private void PositionVfxAtTarget(RectTransform vfxRect, RectTransform target)
    {
        if (vfxRect == null || target == null) return;

        // VFXレイヤーはBattlePanel配下の通常UIなので、同じCanvasのワールド座標を
        // そのまま使う。画面座標をローカル座標へ二重変換すると、WebGLでは盤面から
        // ずれた位置へ描画され、透明な影だけが残って見えることがあった。
        vfxRect.position = target.position;
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

        // スキル素材の途中で倒されたコマだけが消えないよう、盤面上の論理からは
        // 先に外しつつ、見た目は短く残す。次の行動もこの時間を待つ。
        float removalTime = Time.unscaledTime + SkillVfxDuration(0.36f);
        pendingDeathRemovalTimes[target] = removalTime;
        StartSkillCinematic(0.36f);
        StartCoroutine(RemoveDefeatedCharacterAfterVfx(target));
    }

    private void KeepDefeatedCharacterVisibleForSkillVfx(BattleCharacter target, SkillType skillType)
    {
        if (target == null || !target.isDead) return;

        float baseDuration = skillType switch
        {
            SkillType.Stone => 0.44f,
            SkillType.Fireball => 0.40f,
            SkillType.WoodPush => 0.40f,
            SkillType.TigerTwinClaw => 0.46f,
            SkillType.Dragon => 0.90f,
            _ => 0.36f
        };
        float removalTime = Time.unscaledTime + SkillVfxDuration(baseDuration);
        if (!pendingDeathRemovalTimes.TryGetValue(target, out float currentRemovalTime)
            || removalTime > currentRemovalTime)
        {
            pendingDeathRemovalTimes[target] = removalTime;
        }
    }

    private IEnumerator RemoveDefeatedCharacterAfterVfx(BattleCharacter target)
    {
        while (target != null
            && pendingDeathRemovalTimes.TryGetValue(target, out float removalTime)
            && Time.unscaledTime < removalTime)
        {
            yield return null;
        }

        if (target != null)
        {
            pendingDeathRemovalTimes.Remove(target);
            target.StopAllCoroutines();
            Destroy(target.gameObject);
        }
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
            CreateSoilTrapCellTint(pos);
            CreateSoilTrapVfx(pos);
        }
    }

    private void CreateSoilTrapCellTint(Vector2Int pos)
    {
        if (gridCells == null || pos.x < 0 || pos.x >= cols || pos.y < 0 || pos.y >= rows) return;
        if (soilTrapTintObjects.ContainsKey(pos)) return;

        Transform cell = gridCells[pos.x, pos.y].parent;
        if (cell == null) return;

        var marker = new GameObject("SoilTrapCellTint", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = marker.GetComponent<RectTransform>();
        rect.SetParent(cell, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        // CellContentより後ろに置き、コマを覆わず罠マスだけを判別可能にする。
        rect.SetSiblingIndex(0);

        Image image = marker.GetComponent<Image>();
        image.color = new Color(0.44f, 0.25f, 0.08f, 0.46f);
        image.raycastTarget = false;
        soilTrapTintObjects[pos] = marker;
    }

    private void CreateSoilTrapVfx(Vector2Int pos)
    {
        if (gridCells == null || pos.x < 0 || pos.x >= cols || pos.y < 0 || pos.y >= rows) return;
        if (soilTrapVfxObjects.ContainsKey(pos)) return;

        Sprite sprite = LoadRepresentativeVfxSprite("Soil");
        if (sprite == null) return;

        var obj = new GameObject("SoilTrapMarker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RegisterBattleVfx(obj);
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(EnsureBattleVfxOverlay(), false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.position = gridCells[pos.x, pos.y].position;
        rect.sizeDelta = Vector2.one * currentBattleCellSize * 0.82f;
        rect.SetAsLastSibling();

        var image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = Color.white;
        soilTrapVfxObjects[pos] = obj;
        StartCoroutine(SoilTrapMarkerFadeRoutine(pos, image, rect));
    }

    private IEnumerator SoilTrapMarkerFadeRoutine(Vector2Int pos, Image image, RectTransform rect)
    {
        float duration = SkillVfxDuration(0.54f);
        float elapsed = 0f;
        while (elapsed < duration && image != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            Color current = image.color;
            current.a = FadeOutVfxAlpha(0.94f, progress, 0.44f);
            image.color = current;
            yield return null;
        }

        if (soilTrapVfxObjects.TryGetValue(pos, out GameObject marker) && marker == (rect != null ? rect.gameObject : null))
        {
            soilTrapVfxObjects.Remove(pos);
        }
        if (rect != null) DestroyBattleVfx(rect.gameObject);
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
        if (character.data != null && character.data.skillType == SkillType.HorseCharge)
        {
            StartCoroutine(HorseChargeSpriteVfxRoutine(
                character.transform as RectTransform,
                gridCells[newPos.x, newPos.y] as RectTransform));
        }
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
            StartSkillCinematic(0.62f);
            StartCoroutine(BirdRetreatSequence(bird, newPos));
        }
    }

    private IEnumerator BirdRetreatSequence(BattleCharacter bird, Vector2Int newPos)
    {
        if (bird == null || bird.isDead) yield break;

        Vector3 departurePosition = bird.transform.position;
        StartCoroutine(BirdRetreatSpriteVfxRoutine(departurePosition, arriving: false, delay: 0f));
        yield return new WaitForSecondsRealtime(SkillVfxDuration(0.13f));

        if (bird == null || bird.isDead || gridMap.ContainsKey(newPos)) yield break;
        gridMap.Remove(bird.gridPos);
        bird.gridPos = newPos;
        gridMap[newPos] = bird;
        yield return SmoothMove(bird, gridCells[newPos.x, newPos.y]);

        if (bird == null || bird.isDead) yield break;
        StartCoroutine(BirdRetreatSpriteVfxRoutine(bird.transform.position, arriving: true, delay: 0f));
        AddLog($"{bird.data.characterName} は攻撃後に退避した！", Color.cyan);
        yield return new WaitForSecondsRealtime(SkillVfxDuration(0.34f));
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

    // ドラゴンのブレス攻撃（選択方向の前方3x3）
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
            foreach (Vector2Int pos in GetDragonBreathCells(dragon.gridPos, dir, range))
            {
                if (gridMap.TryGetValue(pos, out var bc) && bc.isAlly != dragon.isAlly && !bc.isDead)
                {
                    validDirs.Add(dir);
                    break;
                }
            }
        }

        if (validDirs.Count == 0) return false;

        Vector2Int chosenDir = validDirs[Random.Range(0, validDirs.Count)];
        BeginSkillImpactWindow(0.90f);
        List<BattleCharacter> affectedTargets = new();

        // 攻撃処理：chosenDir 方向の前方3x3全員にダメージ
        foreach (Vector2Int pos in GetDragonBreathCells(dragon.gridPos, chosenDir, range))
        {
            if (gridMap.TryGetValue(pos, out var bc) && bc.isAlly != dragon.isAlly && !bc.isDead)
            {
                affectedTargets.Add(bc);
                int dmg = Mathf.RoundToInt(dragon.GetEffectiveAttack(this) * dragon.data.skillPower);
                AddLog($"{dragon.data.characterName} のブレスが {bc.data.characterName} に命中！ {dmg} ダメージ", Color.red);
                bc.TakeDamage(dmg, this, dragon, isBasicAttack: false);
            }
        }

        dragon.UpdateDirection(chosenDir);
        PlayDragonBreathVfx(dragon, chosenDir, range);
        StartSkillCinematic(0.90f);
        foreach (BattleCharacter affected in affectedTargets)
        {
            KeepDefeatedCharacterVisibleForSkillVfx(affected, SkillType.Dragon);
        }
        GameAudio.Instance.PlaySkillSound(SkillType.Dragon);
        return true;
    }

    private List<Vector2Int> GetDragonBreathCells(Vector2Int origin, Vector2Int dir, int range)
    {
        var cells = new List<Vector2Int>();
        const int sideRadius = 1;
        for (int d = 1; d <= range; d++)
        {
            for (int offset = -sideRadius; offset <= sideRadius; offset++)
            {
                Vector2Int pos = origin + dir * d;
                if (dir == Vector2Int.up || dir == Vector2Int.down)
                    pos += new Vector2Int(offset, 0);
                else
                    pos += new Vector2Int(0, offset);

                if (pos.x < 0 || pos.x >= cols || pos.y < 0 || pos.y >= rows) continue;
                if (!cells.Contains(pos))
                {
                    cells.Add(pos);
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
            PlayAnimatedFolderVfx(dragon, "DragonRoar");
            StartSkillCinematic(0.50f);
            GameAudio.Instance.Play(GameSound.DragonRoar);
            var targetsCopy = new List<BattleCharacter>(targets);
            foreach (var t in targetsCopy)
            {
                if (t == null || t.isDead) continue;
                // 小ダメージ
                int dmg = Mathf.RoundToInt(dragon.GetEffectiveAttack(this) * 0.5f);
                t.TakeDamage(dmg, this, dragon, isBasicAttack: false);
                KeepDefeatedCharacterVisibleForSkillVfx(t, SkillType.Dragon);

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
