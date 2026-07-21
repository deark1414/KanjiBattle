using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
public class ComputerUseActionOverlay : MonoBehaviour
{
    private StageDatabase stageDatabase;
    private bool show = true;
    private Vector2 scroll;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (FindAnyObjectByType<ComputerUseActionOverlay>() != null)
        {
            return;
        }

        var go = new GameObject("ComputerUseActionOverlay");
        DontDestroyOnLoad(go);
        go.AddComponent<ComputerUseActionOverlay>();
    }

    private void Awake()
    {
        stageDatabase = LoadAsset<StageDatabase>("Assets/ScriptableObjects/Stages/StageDatabase.asset");
    }

    private void OnGUI()
    {
        const float width = 260f;
        float x = Mathf.Max(8f, Screen.width - width - 12f);
        float y = 12f;

        if (!show)
        {
            if (GUI.Button(new Rect(x, y, width, 34f), "CU Actions"))
            {
                show = true;
            }
            return;
        }

        GUILayout.BeginArea(new Rect(x, y, width, 330f), "CU Actions", GUI.skin.window);
        scroll = GUILayout.BeginScrollView(scroll);

        DrawStatus();
        GUILayout.Space(4);

        string screen = GetCurrentScreenName();
        GUILayout.Label($"Screen: {screen}");

        switch (screen)
        {
            case "Top":
                DrawTopActions();
                break;
            case "Stages":
                DrawStageActions();
                break;
            case "Formation":
                DrawFormationActions();
                break;
            case "Battle":
                DrawBattleActions();
                break;
            case "Facilities":
                DrawFacilityActions();
                break;
            case "Result":
                DrawResultActions();
                break;
            default:
                DrawNavigationActions();
                break;
        }

        GUILayout.Space(6);
        if (GUILayout.Button("Hide", GUILayout.Height(30)))
        {
            show = false;
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawStatus()
    {
        int gold = GameManager.Instance != null ? GameManager.Instance.GetGold() : 0;
        int sp = GameManager.Instance != null ? GameManager.Instance.GetStagePoints() : 0;
        int cleared = GameManager.Instance != null ? GameManager.Instance.GetClearedStageId() : 0;
        GUILayout.Label($"Gold {gold}   SP {sp}   Clear {cleared}");
    }

    private void DrawTopActions()
    {
        if (GUILayout.Button("Summon Once", GUILayout.Height(36))) SummonOnce();
        if (GUILayout.Button("Upgrade Owned Once", GUILayout.Height(36))) UpgradeOwnedOnce();
        DrawNavigationActions();
    }

    private void DrawStageActions()
    {
        if (GUILayout.Button("Select Next Stage", GUILayout.Height(38))) SelectNextPlayableStage();
        if (GUILayout.Button("Open Formation", GUILayout.Height(34))) UIManager.Instance?.ShowFormation();
        DrawNavigationActions();
    }

    private void DrawFormationActions()
    {
        if (GUILayout.Button("Auto Fill Formation", GUILayout.Height(38))) AutoFillFormation();
        if (GUILayout.Button("Start Battle", GUILayout.Height(38))) StartBattleFromFormation();
        DrawNavigationActions();
    }

    private void DrawBattleActions()
    {
        if (GUILayout.Button("Back To Top", GUILayout.Height(36))) UIManager.Instance?.ShowTop();
        if (GUILayout.Button("Open Stages", GUILayout.Height(36))) UIManager.Instance?.ShowStageSelect();
    }

    private void DrawResultActions()
    {
        if (GUILayout.Button("Next Stage Setup", GUILayout.Height(38)))
        {
            SelectNextPlayableStage();
        }
        if (GUILayout.Button("Back To Top", GUILayout.Height(36))) UIManager.Instance?.ShowTop();
    }

    private void DrawFacilityActions()
    {
        if (GUILayout.Button("Run First Facility Action", GUILayout.Height(40))) RunFirstFacilityAction();
        if (GUILayout.Button("Refresh Facility List", GUILayout.Height(34))) FindDebugObject<FacilityListUI>()?.RefreshList();
        DrawNavigationActions();
    }

    private void DrawNavigationActions()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Top", GUILayout.Height(32))) UIManager.Instance?.ShowTop();
        if (GUILayout.Button("Stages", GUILayout.Height(32))) UIManager.Instance?.ShowStageSelect();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Formation", GUILayout.Height(32))) UIManager.Instance?.ShowFormation();
        if (GUILayout.Button("Facilities", GUILayout.Height(32))) UIManager.Instance?.ShowFacility();
        GUILayout.EndHorizontal();
    }

    private void SummonOnce()
    {
        var summonManager = FindDebugObject<SummonManager>();
        if (summonManager == null)
        {
            Debug.LogWarning("[ComputerUseActionOverlay] SummonManager not found.");
            return;
        }

        summonManager.Summon();
    }

    private void UpgradeOwnedOnce()
    {
        if (PlayerInventory.Instance == null)
        {
            return;
        }

        foreach (var character in PlayerInventory.Instance.GetOwnedCharacters().Keys)
        {
            PlayerInventory.Instance.UpgradeCharacter(character);
        }
    }

    private void SelectNextPlayableStage()
    {
        StageData stage = GetNextPlayableStage();
        if (stage == null)
        {
            Debug.LogWarning("[ComputerUseActionOverlay] Next playable stage not found.");
            return;
        }

        GameManager.Instance?.SetSelectedStage(stage);
        UIManager.Instance?.ShowFormation();
        Debug.Log($"[ComputerUseActionOverlay] Selected stage {stage.stageId}: {stage.stageName}");
    }

    private StageData GetNextPlayableStage()
    {
        if (stageDatabase == null)
        {
            stageDatabase = LoadAsset<StageDatabase>("Assets/ScriptableObjects/Stages/StageDatabase.asset");
        }

        if (stageDatabase == null || stageDatabase.stages == null || GameManager.Instance == null)
        {
            return null;
        }

        int targetStageId = GameManager.Instance.GetHighestClearedStageId() + 1;
        StageData fallback = null;
        foreach (var stage in stageDatabase.stages)
        {
            if (stage == null)
            {
                continue;
            }

            if (stage.stageId == targetStageId && GameManager.Instance.IsChapterUnlocked(stage.chapterId))
            {
                return stage;
            }

            if (fallback == null && stage.stageId > GameManager.Instance.GetHighestClearedStageId() && GameManager.Instance.IsChapterUnlocked(stage.chapterId))
            {
                fallback = stage;
            }
        }

        return fallback;
    }

    private void AutoFillFormation()
    {
        var formation = FormationUI.Instance;
        if (formation == null || PlayerInventory.Instance == null)
        {
            Debug.LogWarning("[ComputerUseActionOverlay] FormationUI or PlayerInventory not found.");
            return;
        }

        StageData stage = GameManager.Instance != null ? GameManager.Instance.GetSelectedStage() : null;
        if (stage == null)
        {
            stage = GetNextPlayableStage();
            GameManager.Instance?.SetSelectedStage(stage);
        }

        int slots = stage != null ? stage.slotCount : 1;
        if (GameManager.Instance != null)
        {
            slots = Mathf.Min(slots, GameManager.Instance.GetFacilityFormationSlots());
        }
        slots = Mathf.Max(1, slots);

        formation.SetupSlots(slots);

        var owned = PlayerInventory.Instance.GetOwnedCharacters();
        var candidates = new List<CharacterData>();
        foreach (var pair in owned)
        {
            if (pair.Key != null && pair.Value != null && pair.Value.count > 0)
            {
                candidates.Add(pair.Key);
            }
        }

        candidates.Sort((a, b) => GetCombatScore(b).CompareTo(GetCombatScore(a)));

        int slotIndex = 0;
        foreach (var character in candidates)
        {
            formation.SetCharacterToSlot(slotIndex, character);
            slotIndex++;
            if (slotIndex >= slots)
            {
                break;
            }
        }

        Debug.Log($"[ComputerUseActionOverlay] Auto filled formation {slotIndex}/{slots}.");
    }

    private int GetCombatScore(CharacterData character)
    {
        if (character == null)
        {
            return 0;
        }

        int level = 1;
        if (PlayerInventory.Instance != null &&
            PlayerInventory.Instance.GetOwnedCharacters().TryGetValue(character, out var info) &&
            info != null)
        {
            level = info.level;
        }

        return character.GetMaxHP(level) + character.GetAttack(level) * 4 + character.GetDefense(level) * 3;
    }

    private void StartBattleFromFormation()
    {
        if (GameManager.Instance != null && GameManager.Instance.GetSelectedStage() == null)
        {
            GameManager.Instance.SetSelectedStage(GetNextPlayableStage());
        }

        if (FormationUI.Instance == null || FormationUI.Instance.GetFormation() == null || FormationUI.Instance.GetFormation().Length == 0)
        {
            AutoFillFormation();
        }

        UIManager.Instance?.StartBattleFromFormation();
    }

    private void RunFirstFacilityAction()
    {
        if (FacilityManager.Instance == null)
        {
            return;
        }

        foreach (var facility in FacilityManager.Instance.GetFacilities())
        {
            if (facility == null)
            {
                continue;
            }

            if (!FacilityManager.Instance.IsUnlocked(facility))
            {
                if (FacilityManager.Instance.Unlock(facility))
                {
                    RefreshFacilities();
                    return;
                }
            }
            else if (FacilityManager.Instance.IsMaxLevel(facility))
            {
                if (FacilityManager.Instance.UpgradeLevelCap(facility))
                {
                    RefreshFacilities();
                    return;
                }
            }
            else if (FacilityManager.Instance.Upgrade(facility))
            {
                RefreshFacilities();
                return;
            }
        }
    }

    private void RefreshFacilities()
    {
        FindDebugObject<FacilityListUI>()?.RefreshList();
    }

    private string GetCurrentScreenName()
    {
        if (IsActive("TopPanel")) return "Top";
        if (IsActive("StageSelectPanel")) return "Stages";
        if (IsActive("FormationPanel")) return "Formation";
        if (IsActive("BattlePanel")) return "Battle";
        if (IsActive("FacilityPanel")) return "Facilities";
        if (IsActive("ResultPanel")) return "Result";
        return "Unknown";
    }

    private static bool IsActive(string objectName)
    {
        var go = GameObject.Find(objectName);
        return go != null && go.activeInHierarchy;
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

    private static T LoadAsset<T>(string path) where T : Object
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
#else
        return null;
#endif
    }
}
#endif
