using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DebugBattleOverlay : MonoBehaviour
{
#if UNITY_EDITOR
    private StageDatabase stageDatabase;
    private CharacterDatabase characterDatabase;
    private bool show = false;
    private Vector2 scroll;
    private bool overrideEnemies = false;
    private bool dragonBossOverride = true;

    private readonly List<Entry> allyEntries = new();
    private readonly List<Entry> enemyEntries = new();
    private readonly List<CharacterData> sortedCharacters = new();
    private readonly List<StageData> sortedStages = new();
    private string[] characterOptions = new string[0];
    private bool bossStageOverride = false;
    private int selectedStageIndex = 0;
    private bool showManualBattle = true;

    private class Entry
    {
        public int characterIndex;
        public int level = 1;
        public bool showPicker = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (!IsDebugOverlayEnabled())
        {
            return;
        }

        var existing = FindAnyObjectByType<DebugBattleOverlay>();
        if (existing != null)
        {
            return;
        }

        var go = new GameObject("DebugBattleOverlay");
        DontDestroyOnLoad(go);
        go.AddComponent<DebugBattleOverlay>();
    }

    private static bool IsDebugOverlayEnabled()
    {
        if (EditorPrefs.GetBool("KanjiBattle.DebugBattleOverlay.Enabled", false))
        {
            return true;
        }

        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-kanjiBattleDebugOverlay")
            {
                return true;
            }
        }
        return false;
    }

    private void Awake()
    {
        LoadDatabases();
    }

    private void OnGUI()
    {
        if (!show)
        {
            if (GUI.Button(new Rect(10, 10, 120, 30), "Debug Battle"))
            {
                show = true;
            }
            return;
        }

GUILayout.BeginArea(new Rect(10, 10, 520, 620), "Debug Battle", GUI.skin.window);
GUILayout.BeginHorizontal();
if (GUILayout.Button("Hide", GUILayout.Height(30), GUILayout.Width(90)))
{
    show = false;
    GUILayout.EndHorizontal();
    GUILayout.EndArea();
    return;
}
GUILayout.Label("Debug Battle");
GUILayout.EndHorizontal();

scroll = GUILayout.BeginScrollView(scroll);

        if (stageDatabase == null || characterDatabase == null)
        {
            GUILayout.Label("DBが見つかりません。");
            if (GUILayout.Button("Reload DB"))
            {
                LoadDatabases();
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            return;
        }

DrawGamePlaytestControls();
GUILayout.Space(8);
DrawScenarioControls();
GUILayout.Space(8);
DrawProgressControls();
GUILayout.Space(8);
DrawStageSelector();
GUILayout.Space(8);

showManualBattle = GUILayout.Toggle(showManualBattle, "Manual Battle Setup");
if (showManualBattle)
{
    GUILayout.Label("Allies");
    DrawEntries(allyEntries);
    GUILayout.BeginHorizontal();
    if (GUILayout.Button("Add Ally", GUILayout.Height(28)))
    {
        allyEntries.Add(new Entry());
    }
    if (GUILayout.Button("Remove Ally", GUILayout.Height(28)) && allyEntries.Count > 0)
    {
        allyEntries.RemoveAt(allyEntries.Count - 1);
    }
    GUILayout.EndHorizontal();

    GUILayout.Space(8);
    if (enemyEntries.Count > 0)
    {
        overrideEnemies = true;
    }
    overrideEnemies = GUILayout.Toggle(overrideEnemies, "Override Enemies");
    bossStageOverride = GUILayout.Toggle(bossStageOverride, "Boss Stage");
    dragonBossOverride = GUILayout.Toggle(dragonBossOverride, "Dragon Is Boss");
    if (overrideEnemies)
    {
        GUILayout.Label("Enemies");
        DrawEntries(enemyEntries);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Enemy", GUILayout.Height(28)))
        {
            enemyEntries.Add(new Entry());
        }
        if (GUILayout.Button("Remove Enemy", GUILayout.Height(28)) && enemyEntries.Count > 0)
        {
            enemyEntries.RemoveAt(enemyEntries.Count - 1);
        }
        GUILayout.EndHorizontal();
    }
}

if (GUILayout.Button("Start Battle", GUILayout.Height(34)))
{
    TryStartBattle();
}

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

private void DrawGamePlaytestControls()
{
    int gold = GameManager.Instance != null ? GameManager.Instance.GetGold() : 0;
    int sp = GameManager.Instance != null ? GameManager.Instance.GetStagePoints() : 0;
    int cleared = GameManager.Instance != null ? GameManager.Instance.GetClearedStageId() : 0;
    GUILayout.Label($"Game Playtest   Gold:{gold}  SP:{sp}  Cleared:{cleared}");

    GUILayout.BeginHorizontal();
    if (GUILayout.Button("Top", GUILayout.Height(28))) UIManager.Instance?.ShowTop();
    if (GUILayout.Button("Stages", GUILayout.Height(28))) UIManager.Instance?.ShowStageSelect();
    if (GUILayout.Button("Formation", GUILayout.Height(28))) ShowFormationForSelectedStage();
    if (GUILayout.Button("Facilities", GUILayout.Height(28))) UIManager.Instance?.ShowFacility();
    GUILayout.EndHorizontal();

    GUILayout.BeginHorizontal();
    if (GUILayout.Button("+1000G", GUILayout.Height(28))) GameManager.Instance?.AddGold(1000);
    if (GUILayout.Button("+1SP", GUILayout.Height(28))) GameManager.Instance?.AddStagePoints(1);
    if (GUILayout.Button("Summon x1", GUILayout.Height(28))) SummonTimes(1);
    if (GUILayout.Button("Summon x5", GUILayout.Height(28))) SummonTimes(5);
    GUILayout.EndHorizontal();

    GUILayout.BeginHorizontal();
    if (GUILayout.Button("Unlock Affordable", GUILayout.Height(28))) UnlockAffordableFacilities();
    if (GUILayout.Button("Upgrade Facilities", GUILayout.Height(28))) UpgradeAffordableFacilitiesOnce();
    if (GUILayout.Button("Upgrade Owned", GUILayout.Height(28))) UpgradeOwnedCharactersOnce();
    GUILayout.EndHorizontal();

    GUILayout.BeginHorizontal();
    if (GUILayout.Button("Auto Formation", GUILayout.Height(28))) AutoFillFormation();
    if (GUILayout.Button("Play Selected Stage", GUILayout.Height(28))) PlaySelectedStageFromFormation();
    if (GUILayout.Button("Sim Clear Selected", GUILayout.Height(28))) SimulateSelectedStageClear();
    GUILayout.EndHorizontal();
}

private StageData GetSelectedDebugStage()
{
    if (sortedStages.Count == 0)
    {
        LoadDatabases();
    }

    if (sortedStages.Count == 0)
    {
        return null;
    }

    selectedStageIndex = Mathf.Clamp(selectedStageIndex, 0, sortedStages.Count - 1);
    return sortedStages[selectedStageIndex];
}

private void ShowFormationForSelectedStage()
{
    StageData stage = GetSelectedDebugStage();
    if (stage != null && GameManager.Instance != null)
    {
        GameManager.Instance.SetSelectedStage(stage);
    }

    UIManager.Instance?.ShowFormation();
}

private void SummonTimes(int times)
{
    var summonManager = FindDebugObject<SummonManager>();
    if (summonManager == null)
    {
        Debug.LogWarning("[DebugBattleOverlay] SummonManager が見つかりません。");
        return;
    }

    for (int i = 0; i < times; i++)
    {
        summonManager.Summon();
    }
}

private void UnlockAffordableFacilities()
{
    if (FacilityManager.Instance == null) return;
    int count = 0;
    var facilities = new List<FacilityData>(FacilityManager.Instance.GetFacilities());
    facilities.Sort((a, b) => GetDebugUnlockPriority(a).CompareTo(GetDebugUnlockPriority(b)));
    foreach (var facility in facilities)
    {
        if (facility != null && FacilityManager.Instance.Unlock(facility))
        {
            count++;
        }
    }
    Debug.Log($"[DebugBattleOverlay] Affordable facilities unlocked: {count}");
}

private int GetDebugUnlockPriority(FacilityData facility)
{
    if (facility == null) return 999;
    switch (facility.effectType)
    {
        case FacilityEffectType.FormationSlot:
            return 0;
        case FacilityEffectType.CharacterUnlock:
            return 1;
        case FacilityEffectType.LevelCap:
            return 2;
        case FacilityEffectType.SummonCostDown:
        case FacilityEffectType.UpgradeCostDown:
        case FacilityEffectType.GoldProduction:
            return 3;
        default:
            return 10;
    }
}

private void UpgradeAffordableFacilitiesOnce
()
{
    if (FacilityManager.Instance == null) return;
    int count = 0;
    foreach (var facility in FacilityManager.Instance.GetFacilities())
    {
        if (facility == null || !FacilityManager.Instance.IsUnlocked(facility)) continue;
        if (FacilityManager.Instance.IsMaxLevel(facility))
        {
            if (FacilityManager.Instance.UpgradeLevelCap(facility)) count++;
        }
        else if (FacilityManager.Instance.Upgrade(facility))
        {
            count++;
        }
    }
    Debug.Log($"[DebugBattleOverlay] Affordable facility upgrades: {count}");
}

private void UpgradeOwnedCharactersOnce()
{
    if (PlayerInventory.Instance == null) return;
    var owned = new List<CharacterData>(PlayerInventory.Instance.GetOwnedCharacters().Keys);
    int beforeGold = GameManager.Instance != null ? GameManager.Instance.GetGold() : 0;
    foreach (var character in owned)
    {
        PlayerInventory.Instance.UpgradeCharacter(character);
    }
    int afterGold = GameManager.Instance != null ? GameManager.Instance.GetGold() : beforeGold;
    Debug.Log($"[DebugBattleOverlay] Upgrade owned attempted: {owned.Count}, gold {beforeGold}->{afterGold}");
}

private void AutoFillFormation()
{
    StageData stage = GetSelectedDebugStage();
    if (stage != null && GameManager.Instance != null)
    {
        GameManager.Instance.SetSelectedStage(stage);
    }

    UIManager.Instance?.ShowFormation();
    var formation = FormationUI.Instance;
    if (formation == null || PlayerInventory.Instance == null)
    {
        Debug.LogWarning("[DebugBattleOverlay] FormationUI または PlayerInventory が見つかりません。");
        return;
    }

    int slots = stage != null ? stage.slotCount : 1;
    if (GameManager.Instance != null)
    {
        slots = Mathf.Min(slots, GameManager.Instance.GetFacilityFormationSlots());
    }

formation.SetupSlots(Mathf.Max(1, slots));
var owned = PlayerInventory.Instance.GetOwnedCharacters();
var candidates = new List<CharacterData>();
foreach (var kvp in owned)
{
    if (kvp.Key != null && kvp.Value != null && kvp.Value.count > 0)
    {
        candidates.Add(kvp.Key);
    }
}
candidates.Sort((a, b) => GetDebugCombatScore(b).CompareTo(GetDebugCombatScore(a)));

int slotIndex = 0;
foreach (var character in candidates)
{
    formation.SetCharacterToSlot(slotIndex, character);
    slotIndex++;
    if (slotIndex >= slots) break;
}
if (slotIndex < slots)
{
    foreach (var character in candidates)
    {
        if (!owned.TryGetValue(character, out var info)) continue;
        int remainingCopies = Mathf.Max(0, info.count - 1);
        for (int i = 0; i < remainingCopies && slotIndex < slots; i++)
        {
            formation.SetCharacterToSlot(slotIndex, character);
            slotIndex++;
        }
        if (slotIndex >= slots) break;
    }
}
var names = new List<string>();
foreach (var character in formation.GetFormation())
{
    if (character != null) names.Add($"{character.characterName}(score:{GetDebugCombatScore(character)})");
}
Debug.Log($"[DebugBattleOverlay] Auto formation filled {slotIndex}/{slots}: {string.Join(", ", names)}");
}

private int GetDebugCombatScore(CharacterData character)
{
    if (character == null) return 0;
    int level = 1;
    if (PlayerInventory.Instance != null &&
        PlayerInventory.Instance.GetOwnedCharacters().TryGetValue(character, out var info) &&
        info != null)
    {
        level = info.level;
    }
    return character.GetMaxHP(level) + character.GetAttack(level) * 4 + character.GetDefense(level) * 3;
}

private void PlaySelectedStageFromFormation()
{
    StageData stage = GetSelectedDebugStage();
    if (stage == null)
    {
        Debug.LogWarning("[DebugBattleOverlay] Stage が見つかりません。");
        return;
    }

    if (GameManager.Instance != null)
    {
        GameManager.Instance.SetSelectedStage(stage);
    }

    var formation = FormationUI.Instance;
    if (formation == null || formation.GetFormation() == null)
    {
        AutoFillFormation();
        formation = FormationUI.Instance;
    }

    var allies = new List<CharacterData>();
    if (formation != null && formation.GetFormation() != null)
    {
        foreach (var character in formation.GetFormation())
        {
            if (character != null) allies.Add(character);
        }
    }

    if (allies.Count == 0)
    {
        AutoFillFormation();
        formation = FormationUI.Instance;
        if (formation != null && formation.GetFormation() != null)
        {
            foreach (var character in formation.GetFormation())
            {
                if (character != null) allies.Add(character);
            }
        }
    }

    if (allies.Count == 0)
    {
        Debug.LogWarning("[DebugBattleOverlay] 編成が空です。");
        return;
    }

    if (UIManager.Instance != null)
    {
        UIManager.Instance.ShowBattle();
    }

    var battleManager = FindBattleManager();
    if (battleManager == null)
    {
        Debug.LogWarning("[DebugBattleOverlay] BattleManager が見つかりません。");
        return;
    }

    ActivateHierarchy(battleManager.transform);
    Debug.Log($"[DebugBattleOverlay] Play stage {stage.stageId} with formation: {string.Join(", ", allies.ConvertAll(c => c.characterName))}");
    battleManager.StartBattle(allies, stage);
}

private void SimulateSelectedStageClear()
{
    StageData stage = GetSelectedDebugStage();
    if (stage == null || GameManager.Instance == null) return;
    GameManager.Instance.RegisterClearedStage(stage.stageId);
    int reward = GameManager.Instance.GetEffectiveStagePointReward(stage.rewardStagePoints);
    GameManager.Instance.AddStagePoints(reward);
    Debug.Log($"[DebugBattleOverlay] Simulated clear: Stage {stage.stageId}, +{reward}SP");
}

private static T FindDebugObject<T>() where T : Object
{
    var active = FindAnyObjectByType<T>();
    if (active != null)
    {
        return active;
    }

    var all = Resources.FindObjectsOfTypeAll<T>();
    foreach (var item in all)
    {
        if (item != null && item.hideFlags == HideFlags.None)
        {
            return item;
        }
    }
    return null;
}

private void DrawScenarioControls()
{
    GUILayout.Label("Scenario");
    GUILayout.BeginHorizontal();
    if (GUILayout.Button("Quick Battle", GUILayout.Height(32)))
    {
        ConfigureQuickBattle();
        TryStartBattle();
    }
    if (GUILayout.Button("Visual Test", GUILayout.Height(32)))
    {
        ConfigureVisualTest();
        TryStartBattle();
    }
    GUILayout.EndHorizontal();

    GUILayout.BeginHorizontal();
    if (GUILayout.Button("Skill Test", GUILayout.Height(32)))
    {
        ConfigureSkillTest();
        TryStartBattle();
    }
    if (GUILayout.Button("Boss Test", GUILayout.Height(32)))
    {
        ConfigureBossTest();
        TryStartBattle();
    }
    GUILayout.EndHorizontal();

    GUILayout.BeginHorizontal();
    if (GUILayout.Button("Stage2 Base", GUILayout.Height(32)))
    {
        ConfigureStage2Baseline(1);
        TryStartBattle();
    }
    if (GUILayout.Button("Stage2 Lv2", GUILayout.Height(32)))
    {
        ConfigureStage2Baseline(2);
        TryStartBattle();
    }
    GUILayout.EndHorizontal();
}

private void DrawProgressControls()
{
    GUILayout.Label("Progress / Economy");
    GUILayout.BeginHorizontal();
    if (GUILayout.Button("Early", GUILayout.Height(28))) ApplyProgressPreset(1, 500, 5);
    if (GUILayout.Button("Mid", GUILayout.Height(28))) ApplyProgressPreset(5, 5000, 50);
    if (GUILayout.Button("Late", GUILayout.Height(28))) ApplyProgressPreset(20, 50000, 500);
    GUILayout.EndHorizontal();

    GUILayout.Label("Balance Presets");
    GUILayout.BeginHorizontal();
    if (GUILayout.Button("Ch1 End", GUILayout.Height(28))) ApplyBalancePreset(5, 5, 2000, 12, 6);
    if (GUILayout.Button("Ch2 Mid", GUILayout.Height(28))) ApplyBalancePreset(7, 5, 6000, 24, 8);
    if (GUILayout.Button("Ch4 Mid", GUILayout.Height(28))) ApplyBalancePreset(18, 8, 30000, 120, 19);
    GUILayout.EndHorizontal();

    GUILayout.BeginHorizontal();
    if (GUILayout.Button("Ch6 Mid", GUILayout.Height(28))) ApplyBalancePreset(28, 11, 80000, 300, 29);
    if (GUILayout.Button("Roster Lv5", GUILayout.Height(28))) GrantRoster(5);
    if (GUILayout.Button("Roster Lv20", GUILayout.Height(28))) GrantRoster(20);
    GUILayout.EndHorizontal();

    GUILayout.BeginHorizontal();
    if (GUILayout.Button("Unlock Facilities", GUILayout.Height(28))) UnlockAvailableFacilities();
    if (GUILayout.Button("Upgrade Facilities", GUILayout.Height(28))) UpgradeFacilitiesOnce();
    GUILayout.EndHorizontal();
}

private void DrawStageSelector()
{
    if (sortedStages.Count == 0)
    {
        LoadDatabases();
    }

    if (sortedStages.Count == 0)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Stage: none");
        if (GUILayout.Button("Reload DB", GUILayout.Width(100), GUILayout.Height(28)))
        {
            LoadDatabases();
        }
        GUILayout.EndHorizontal();
        return;
    }

    selectedStageIndex = Mathf.Clamp(selectedStageIndex, 0, sortedStages.Count - 1);
    StageData stage = sortedStages[selectedStageIndex];
    GUILayout.BeginHorizontal();
    if (GUILayout.Button("<", GUILayout.Width(42), GUILayout.Height(28)))
    {
        selectedStageIndex = Mathf.Max(0, selectedStageIndex - 1);
    }
    GUILayout.Label($"Stage {stage.stageId}: {stage.stageName}  EnemyLv:{stage.enemyLevel}", GUILayout.Height(28));
    if (GUILayout.Button(">", GUILayout.Width(42), GUILayout.Height(28)))
    {
        selectedStageIndex = Mathf.Min(sortedStages.Count - 1, selectedStageIndex + 1);
    }
    GUILayout.EndHorizontal();
}

private void ConfigureQuickBattle()
{
    SetStageByOffset(0);
    SetEntries(allyEntries, 18, 3, 16, 3, 10, 3);
    SetEntries(enemyEntries, 1, 2, 2, 2, 3, 2);
    overrideEnemies = true;
    bossStageOverride = false;
    dragonBossOverride = true;
    showManualBattle = false;
}

private void ConfigureVisualTest()
{
    SetStageByOffset(0);
    SetEntries(allyEntries, 1, 5, 10, 5, 19, 5);
    SetEntries(enemyEntries, 2, 5, 13, 5, 25, 5);
    overrideEnemies = true;
    bossStageOverride = false;
    dragonBossOverride = true;
    showManualBattle = false;
}

private void ConfigureSkillTest()
{
    SetStageByOffset(Mathf.Min(2, sortedStages.Count - 1));
    SetEntries(allyEntries, 10, 10, 11, 10, 20, 10);
    SetEntries(enemyEntries, 13, 10, 19, 10, 25, 10);
    overrideEnemies = true;
    bossStageOverride = false;
    dragonBossOverride = true;
    showManualBattle = false;
}

private void ConfigureBossTest()
{
    SetStageByOffset(sortedStages.Count - 1);
    SetEntries(allyEntries, 10, 20, 18, 20, 20, 20);
    SetEntries(enemyEntries, 26, 18);
    overrideEnemies = true;
    bossStageOverride = true;
    dragonBossOverride = true;
    showManualBattle = false;
}

private void ConfigureStage2Baseline(int allyLevel)
{
    SetStageById(2);
    SetEntries(allyEntries, 1, allyLevel, 2, allyLevel);
    enemyEntries.Clear();
    overrideEnemies = false;
    bossStageOverride = false;
    dragonBossOverride = true;
    showManualBattle = false;
}

private void ApplyProgressPreset(int clearedStageId, int gold, int stagePoints)
{
    if (GameManager.Instance != null)
    {
        GameManager.Instance.SetGold(gold);
        int delta = stagePoints - GameManager.Instance.GetStagePoints();
        GameManager.Instance.AddStagePoints(delta);
        GameManager.Instance.RegisterClearedStage(clearedStageId);
        Debug.Log($"[DebugBattleOverlay] Progress preset: cleared={clearedStageId}, gold={gold}, sp={stagePoints}");
    }
}

private void ApplyBalancePreset(int clearedStageId, int characterLevel, int gold, int stagePoints, int selectedStageId)
{
    ApplyProgressPreset(clearedStageId, gold, stagePoints);
    UnlockCharactersAvailableByStage(clearedStageId, characterLevel);
    PrepareFacilitiesForBalance(clearedStageId, characterLevel);
    SetStageById(selectedStageId);

    StageData stage = GetSelectedDebugStage();
    if (stage != null && GameManager.Instance != null)
    {
        GameManager.Instance.SetSelectedStage(stage);
    }

    AutoFillFormation();
    Debug.Log($"[DebugBattleOverlay] Balance preset: cleared={clearedStageId}, charLv={characterLevel}, selectedStage={selectedStageId}");
}

private void UnlockCharactersAvailableByStage(int clearedStageId, int level)
{
    if (PlayerInventory.Instance == null) return;

    int[] unlockIds =
    {
        1, 2, 3,
        10, 11, 12,
        13, 14, 15,
        4, 5, 6,
        16, 17, 18,
        19, 20, 21, 22,
        23, 24, 25,
        7, 8, 9
    };

    int[] requiredStages =
    {
        0, 2, 3,
        6, 7, 8,
        11, 12, 13,
        16, 17, 18,
        21, 22, 23,
        26, 27, 28, 29,
        31, 32, 33,
        36, 37, 38
    };

    var owned = PlayerInventory.Instance.GetOwnedCharacters();
    for (int i = 0; i < unlockIds.Length; i++)
    {
        if (clearedStageId < requiredStages[i]) continue;

        CharacterData character = FindCharacterById(unlockIds[i]);
        if (character == null || character.isBoss) continue;

        PlayerInventory.Instance.UnlockCharacterForSummon(character);
        if (!owned.ContainsKey(character))
        {
            PlayerInventory.Instance.AddCharacter(character);
        }

        owned[character].level = Mathf.Max(1, level);
        owned[character].count = Mathf.Max(owned[character].count, 3);
    }
}

private void PrepareFacilitiesForBalance(int clearedStageId, int targetLevelCap)
{
    if (FacilityManager.Instance == null || GameManager.Instance == null) return;

    int savedGold = GameManager.Instance.GetGold();
    int savedStagePoints = GameManager.Instance.GetStagePoints();
    GameManager.Instance.SetGold(1000000);
    GameManager.Instance.AddStagePoints(1000000 - savedStagePoints);

    foreach (var facility in FacilityManager.Instance.GetFacilities())
    {
        if (facility == null || clearedStageId < facility.requiredStageId) continue;
        FacilityManager.Instance.Unlock(facility);
    }

    int guard = 0;
    while (GameManager.Instance.GetFacilityFormationSlots() < GetExpectedFormationSlots(clearedStageId) && guard++ < 10)
    {
        FacilityData training = FindFacility(FacilityEffectType.FormationSlot);
        if (training == null || !FacilityManager.Instance.Upgrade(training)) break;
    }

    guard = 0;
    while (PlayerInventory.Instance != null && PlayerInventory.Instance.GetEffectiveLevelCap() < targetLevelCap && guard++ < 20)
    {
        FacilityData library = FindFacility(FacilityEffectType.LevelCap);
        if (library == null) break;

        if (FacilityManager.Instance.IsMaxLevel(library))
        {
            if (!FacilityManager.Instance.UpgradeLevelCap(library)) break;
        }
        else if (!FacilityManager.Instance.Upgrade(library))
        {
            break;
        }
    }

    GameManager.Instance.SetGold(savedGold);
    int restoreDelta = savedStagePoints - GameManager.Instance.GetStagePoints();
    GameManager.Instance.AddStagePoints(restoreDelta);
}

private int GetExpectedFormationSlots(int clearedStageId)
{
    if (clearedStageId >= 35) return 6;
    if (clearedStageId >= 25) return 5;
    if (clearedStageId >= 12) return 4;
    if (clearedStageId >= 5) return 3;
    if (clearedStageId >= 3) return 2;
    return 1;
}

private CharacterData FindCharacterById(int characterId)
{
    return sortedCharacters.Find(character => character != null && character.characterId == characterId);
}

private FacilityData FindFacility(FacilityEffectType effectType)
{
    if (FacilityManager.Instance == null) return null;
    foreach (var facility in FacilityManager.Instance.GetFacilities())
    {
        if (facility != null && facility.effectType == effectType)
        {
            return facility;
        }
    }
    return null;
}

private void UnlockAvailableFacilities()
{
    ApplyProgressPreset(999, 100000, 10000);
    if (FacilityManager.Instance == null) return;
    int count = 0;
    foreach (var facility in FacilityManager.Instance.GetFacilities())
    {
        if (facility != null && FacilityManager.Instance.Unlock(facility))
        {
            count++;
        }
    }
    Debug.Log($"[DebugBattleOverlay] Unlocked facilities: {count}");
}

private void UpgradeFacilitiesOnce()
{
    ApplyProgressPreset(999, 100000, 10000);
    if (FacilityManager.Instance == null) return;
    int count = 0;
    foreach (var facility in FacilityManager.Instance.GetFacilities())
    {
        if (facility != null && FacilityManager.Instance.Upgrade(facility))
        {
            count++;
        }
    }
    Debug.Log($"[DebugBattleOverlay] Upgraded facilities once: {count}");
}

private void GrantRoster(int level)
{
    ApplyProgressPreset(999, 100000, 10000);
    if (PlayerInventory.Instance == null) return;
    foreach (var character in sortedCharacters)
    {
        if (character == null || character.isBoss) continue;
        PlayerInventory.Instance.UnlockCharacterForSummon(character);
        var owned = PlayerInventory.Instance.GetOwnedCharacters();
        if (!owned.ContainsKey(character))
        {
            PlayerInventory.Instance.AddCharacter(character);
        }
        owned[character].level = Mathf.Max(1, level);
        owned[character].count = Mathf.Max(owned[character].count, 1);
    }
    Debug.Log($"[DebugBattleOverlay] Granted roster at Lv{level}");
}

private void SetStageByOffset(int index)
{
    if (sortedStages.Count == 0) return;
    selectedStageIndex = Mathf.Clamp(index, 0, sortedStages.Count - 1);
}

private void SetStageById(int stageId)
{
    if (sortedStages.Count == 0) return;
    int index = sortedStages.FindIndex(stage => stage != null && stage.stageId == stageId);
    selectedStageIndex = index >= 0 ? index : Mathf.Clamp(stageId - 1, 0, sortedStages.Count - 1);
}

private void SetEntries(List<Entry> entries, params int[] idLevelPairs)
{
    entries.Clear();
    for (int i = 0; i + 1 < idLevelPairs.Length; i += 2)
    {
        int index = sortedCharacters.FindIndex(c => c.characterId == idLevelPairs[i]);
        if (index >= 0)
        {
            entries.Add(new Entry { characterIndex = index, level = Mathf.Max(1, idLevelPairs[i + 1]) });
        }
    }
}

    private void LoadDatabases()
    {
        stageDatabase = LoadAsset<StageDatabase>("Assets/ScriptableObjects/Stages/StageDatabase.asset");
        characterDatabase = LoadAsset<CharacterDatabase>("Assets/ScriptableObjects/Characters/CharacterDatabase.asset");
        BuildOptions();
    }

    private static T LoadAsset<T>(string path) where T : Object
    {
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    private void TryStartBattle()
    {
        if (sortedStages.Count == 0)
        {
            Debug.LogWarning("[DebugBattleOverlay] Stage が見つかりません。");
            return;
        }

        selectedStageIndex = Mathf.Clamp(selectedStageIndex, 0, sortedStages.Count - 1);
        StageData stage = sortedStages[selectedStageIndex];
        var allies = ResolveEntries(allyEntries);
        if (allies.Count == 0)
        {
            Debug.LogWarning("[DebugBattleOverlay] Ally Ids が空です。");
            return;
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowBattle();
        }

        var battleManager = FindBattleManager();
        if (battleManager == null)
        {
            Debug.LogWarning("[DebugBattleOverlay] BattleManager が見つかりません。");
            return;
        }

        ActivateHierarchy(battleManager.transform);

        if (overrideEnemies)
        {
            var enemies = ResolveEntries(enemyEntries);
            if (enemies.Count == 0)
            {
                Debug.LogWarning("[DebugBattleOverlay] Enemy Ids が空です。");
                return;
            }

            Debug.Log($"[DebugBattleOverlay] Allies: {FormatEntryNames(allyEntries)}");
            Debug.Log($"[DebugBattleOverlay] Enemies: {FormatEntryNames(enemyEntries)}");

            var originalPool = stage.enemyPool;
            bool originalBoss = stage.isBossStage;
            stage.enemyPool = enemies;
            stage.isBossStage = bossStageOverride;
            battleManager.StartBattle(allies, stage);
            stage.enemyPool = originalPool;
            stage.isBossStage = originalBoss;

            ApplyDragonBossOverride(enemies);
        }
        else
        {
            Debug.Log($"[DebugBattleOverlay] Allies: {FormatEntryNames(allyEntries)}");
            battleManager.StartBattle(allies, stage);
        }

        ApplyLevels(battleManager, allyEntries, isAlly: true);
        if (overrideEnemies)
        {
            ApplyLevels(battleManager, enemyEntries, isAlly: false);
        }
    }

private static void ActivateHierarchy(Transform transform)
{
    if (transform == null)
    {
        return;
    }

    var stack = new List<GameObject>();
    Transform current = transform;
    while (current != null)
    {
        stack.Add(current.gameObject);
        current = current.parent;
    }

    for (int i = stack.Count - 1; i >= 0; i--)
    {
        if (!stack[i].activeSelf)
        {
            stack[i].SetActive(true);
        }
    }
}

    private static BattleManager FindBattleManager()
    {
        var manager = FindAnyObjectByType<BattleManager>();
        if (manager != null)
        {
            return manager;
        }

        var all = Resources.FindObjectsOfTypeAll<BattleManager>();
        if (all != null && all.Length > 0)
        {
            return all[0];
        }

        return null;
    }

    private List<CharacterData> ResolveEntries(List<Entry> entries)
    {
        var result = new List<CharacterData>();
        foreach (var entry in entries)
        {
            if (entry.characterIndex < 0 || entry.characterIndex >= sortedCharacters.Count)
            {
                continue;
            }
            result.Add(sortedCharacters[entry.characterIndex]);
        }
        return result;
    }

    private void ApplyLevels(BattleManager battleManager, List<Entry> entries, bool isAlly)
    {
        if (entries.Count == 0)
        {
            return;
        }

        var spawned = new List<BattleCharacter>();
        foreach (var bc in battleManager.gridMap.Values)
        {
            if (bc != null && bc.isAlly == isAlly)
            {
                spawned.Add(bc);
            }
        }

        spawned.Sort((a, b) => a.InstanceId.CompareTo(b.InstanceId));
        for (int i = 0; i < spawned.Count && i < entries.Count; i++)
        {
            int level = Mathf.Max(1, entries[i].level);
            spawned[i].SetLevelForDebug(level);
        }
    }

    private void BuildOptions()
    {
        sortedCharacters.Clear();
        sortedStages.Clear();

        if (characterDatabase != null && characterDatabase.characters != null)
        {
            sortedCharacters.AddRange(characterDatabase.characters);
            sortedCharacters.RemoveAll(c => c == null);
            sortedCharacters.Sort((a, b) => a.characterId.CompareTo(b.characterId));
        }

        if (stageDatabase != null && stageDatabase.stages != null)
        {
            sortedStages.AddRange(stageDatabase.stages);
            sortedStages.RemoveAll(s => s == null);
            sortedStages.Sort((a, b) => a.stageId.CompareTo(b.stageId));
        }

        characterOptions = new string[sortedCharacters.Count];
        for (int i = 0; i < sortedCharacters.Count; i++)
        {
            var c = sortedCharacters[i];
            characterOptions[i] = $"{c.characterId}: {c.characterName} ({c.category})";
        }

        if (allyEntries.Count == 0 && sortedCharacters.Count > 0)
        {
            AddDefaultEntryById(allyEntries, 18); // Gun
            AddDefaultEntryById(allyEntries, 16); // Stone
        }
    }

    private void DrawEntries(List<Entry> entries)
    {
        if (characterOptions.Length == 0)
        {
            GUILayout.Label("キャラデータが空です。");
            return;
        }

        string[] filteredOptions = characterOptions;
        int[] filteredIndices = BuildAllIndices();

        for (int i = 0; i < entries.Count; i++)
        {
            int selectedIndex = 0;
            for (int idx = 0; idx < filteredIndices.Length; idx++)
            {
                if (filteredIndices[idx] == entries[i].characterIndex)
                {
                    selectedIndex = idx;
                    break;
                }
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label(filteredOptions[selectedIndex], GUILayout.Width(260));
            if (GUILayout.Button(entries[i].showPicker ? "Close" : "Pick", GUILayout.Width(50)))
            {
                entries[i].showPicker = !entries[i].showPicker;
            }
            GUILayout.Label("Lv", GUILayout.Width(20));
            string levelText = GUILayout.TextField(entries[i].level.ToString(), GUILayout.Width(40));
            if (int.TryParse(levelText, out int lv))
            {
                entries[i].level = Mathf.Max(1, lv);
            }
            GUILayout.EndHorizontal();

            if (entries[i].showPicker)
            {
                for (int opt = 0; opt < filteredOptions.Length; opt++)
                {
                    if (GUILayout.Button(filteredOptions[opt], GUILayout.Width(320)))
                    {
                        entries[i].characterIndex = filteredIndices[opt];
                        entries[i].showPicker = false;
                        break;
                    }
                }
            }
        }
    }

    private int[] BuildAllIndices()
    {
        var indices = new int[sortedCharacters.Count];
        for (int i = 0; i < sortedCharacters.Count; i++)
        {
            indices[i] = i;
        }
        return indices;
    }

    private void ApplyDragonBossOverride(List<CharacterData> enemies)
    {
        if (enemies == null)
        {
            return;
        }

        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.skillType == SkillType.Dragon)
            {
                enemy.isBoss = dragonBossOverride;
            }
        }
    }

    private string FormatEntryNames(List<Entry> entries)
    {
        if (entries.Count == 0)
        {
            return "(none)";
        }

        var parts = new List<string>();
        foreach (var entry in entries)
        {
            if (entry.characterIndex < 0 || entry.characterIndex >= sortedCharacters.Count)
            {
                continue;
            }
            var c = sortedCharacters[entry.characterIndex];
            parts.Add($"{c.characterName}(Lv{entry.level})");
        }
        return string.Join(", ", parts);
    }

    private void AddDefaultEntryById(List<Entry> entries, int characterId)
    {
        int index = sortedCharacters.FindIndex(c => c.characterId == characterId);
        if (index >= 0)
        {
            entries.Add(new Entry { characterIndex = index, level = 1 });
        }
    }
#endif
}
