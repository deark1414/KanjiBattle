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
    private bool show = true;
    private Vector2 scroll;
    private bool overrideEnemies = false;
    private bool dragonBossOverride = true;

    private readonly List<Entry> allyEntries = new();
    private readonly List<Entry> enemyEntries = new();
    private readonly List<CharacterData> sortedCharacters = new();
    private readonly List<StageData> sortedStages = new();
    private string[] characterOptions = new string[0];
    private bool bossStageOverride = false;

    private class Entry
    {
        public int characterIndex;
        public int level = 1;
        public bool showPicker = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        var existing = FindObjectOfType<DebugBattleOverlay>();
        if (existing != null)
        {
            return;
        }

        var go = new GameObject("DebugBattleOverlay");
        DontDestroyOnLoad(go);
        go.AddComponent<DebugBattleOverlay>();
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

        GUILayout.BeginArea(new Rect(10, 10, 420, 360), "Debug Battle", GUI.skin.window);
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

        GUILayout.Label("Allies");
        DrawEntries(allyEntries);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Ally"))
        {
            allyEntries.Add(new Entry());
        }
        if (GUILayout.Button("Remove Ally") && allyEntries.Count > 0)
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
            if (GUILayout.Button("Add Enemy"))
            {
                enemyEntries.Add(new Entry());
            }
            if (GUILayout.Button("Remove Enemy") && enemyEntries.Count > 0)
            {
                enemyEntries.RemoveAt(enemyEntries.Count - 1);
            }
            GUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Start Battle"))
        {
            TryStartBattle();
        }

        if (GUILayout.Button("Hide"))
        {
            show = false;
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
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

        StageData stage = sortedStages[0];
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

    private static BattleManager FindBattleManager()
    {
        var manager = FindObjectOfType<BattleManager>();
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
