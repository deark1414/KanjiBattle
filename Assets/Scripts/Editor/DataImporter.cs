using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public class DataImporter : EditorWindow
{
    [MenuItem("Tools/Import JSON Data")]
    public static void ImportAllData()
    {
        ImportCharacters("Assets/Data/characters.json");
        ImportFacilities("Assets/Data/facilities.json");
        ImportStages("Assets/Data/stages.json");
        AssetDatabase.SaveAssets();
        RebuildCharacterDatabase();
        RebuildFacilityDatabase();
        RebuildStageDatabase();
        Debug.Log("✅ すべてのJSONデータをScriptableObjectに反映しました。");
    }

    [System.Serializable]
    class JsonArrayWrapper<T>
    {
        public List<T> items;
    }

    static List<T> ReadJsonList<T>(string json)
    {
        string wrappedJson = $"{{\"items\":{json}}}";
        JsonArrayWrapper<T> wrapper = JsonUtility.FromJson<JsonArrayWrapper<T>>(wrappedJson);
        return wrapper?.items ?? new List<T>();
    }

    [System.Serializable]
    class CharacterJson
    {
        public int id;
        public string fileName;
        public string characterName;
        public string category;
        public int baseHP;
        public int baseAttack;
        public int baseDefense;
        public int attackGrowth;
        public int hpGrowth;
        public int defenseGrowth;
        public string skillType;
        public float skillPower;
        public int skillChance;
        public bool isBoss;
    }

    [System.Serializable]
    class FacilityLevelCapRequirementJson
    {
        public int stageId;
        public int requiredStagePoints;
    }

    [System.Serializable]
    class FacilityJson
    {
        public int id;
        public string fileName;
        public string facilityName;
        public string effectType;
        public string unlockType;
        public int requiredStageId;
        public int unlockStagePointCost;
        public int initialMaxLevel;
        public int finalMaxLevel;
        public int baseCost;
        public float growthFactor;
        public float effectPerLevel;
        public int levelCapIncreasePerUnlock;
        public string summonCategory;
        public float summonRatePerLevel;
        public List<FacilityLevelCapRequirementJson> facilityLevelCapUnlocks;
    }

    [System.Serializable]
    class StageJson
    {
        public int stageId;
        public string fileName;
        public string stageName;
        public int chapterId;
        public List<int> enemyIds;
        public int rewardStagePoints;
        public int slotCount;
        public int trapDamage;
        public int trapCount;
        public List<int> reinforcementEnemyIds;
        public int reinforcementInterval;
        public int reinforcementCount;
        public int reinforcementLimit;
        public bool isBossStage;
        public int enemyLevel;
        public int prerequisiteStageId;
    }

    static void ImportCharacters(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"❌ Character JSON not found: {path}");
            return;
        }

        string jsonText = File.ReadAllText(path);
        List<CharacterJson> characters = ReadJsonList<CharacterJson>(jsonText);

        foreach (var c in characters)
        {
            string assetPath = $"Assets/ScriptableObjects/Characters/{c.fileName}.asset";
            CharacterData data = AssetDatabase.LoadAssetAtPath<CharacterData>(assetPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<CharacterData>();
                AssetDatabase.CreateAsset(data, assetPath);
            }

            data.characterId = c.id;
            data.characterName = c.characterName;
            data.category = (CharacterCategory)System.Enum.Parse(typeof(CharacterCategory), c.category);
            data.GetType().GetField("baseHP", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(data, c.baseHP);
            data.GetType().GetField("baseAttack", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(data, c.baseAttack);
            data.GetType().GetField("baseDefense", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(data, c.baseDefense);
            data.attackGrowth = c.attackGrowth;
            data.hpGrowth = c.hpGrowth;
            data.defenseGrowth = c.defenseGrowth;
            data.skillType = (SkillType)System.Enum.Parse(typeof(SkillType), c.skillType);
            data.skillPower = c.skillPower;
            data.skillChance = c.skillChance;
            data.isBoss = c.isBoss;

            EditorUtility.SetDirty(data);
        }
        Debug.Log("✅ Characters imported.");
    }

    static void ImportFacilities(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"❌ Facility JSON not found: {path}");
            return;
        }

        string jsonText = File.ReadAllText(path);
        List<FacilityJson> facilities = ReadJsonList<FacilityJson>(jsonText);

        foreach (var f in facilities)
        {
            string assetPath = $"Assets/ScriptableObjects/Facilities/{f.fileName}.asset";
            FacilityData data = AssetDatabase.LoadAssetAtPath<FacilityData>(assetPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<FacilityData>();
                AssetDatabase.CreateAsset(data, assetPath);
            }

            data.facilityId = f.id;
            data.facilityName = f.facilityName;
            data.effectType = (FacilityEffectType)System.Enum.Parse(typeof(FacilityEffectType), f.effectType);
            data.unlockType = (FacilityUnlockType)System.Enum.Parse(typeof(FacilityUnlockType), f.unlockType);
            data.requiredStageId = f.requiredStageId;
            data.unlockStagePointCost = f.unlockStagePointCost;
            data.initialMaxLevel = f.initialMaxLevel;
            data.finalMaxLevel = f.finalMaxLevel;
            data.baseCost = f.baseCost;
            data.growthFactor = f.growthFactor;
            data.effectPerLevel = f.effectPerLevel;
            data.levelCapIncreasePerUnlock = f.levelCapIncreasePerUnlock;
            data.summonCategory = (CharacterCategory)System.Enum.Parse(typeof(CharacterCategory), f.summonCategory);
            data.summonRatePerLevel = f.summonRatePerLevel;

            data.facilityLevelCapUnlocks.Clear();
            if (f.facilityLevelCapUnlocks != null)
            {
                foreach (var unlock in f.facilityLevelCapUnlocks)
                {
                    data.facilityLevelCapUnlocks.Add(new FacilityLevelCapRequirement
                    {
                        stageId = unlock.stageId,
                        requiredStagePoints = unlock.requiredStagePoints
                    });
                }
            }

            EditorUtility.SetDirty(data);
        }
        Debug.Log("✅ Facilities imported.");
    }

    static void ImportStages(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"❌ Stage JSON not found: {path}");
            return;
        }

        string jsonText = File.ReadAllText(path);
        List<StageJson> stages = ReadJsonList<StageJson>(jsonText);
        Dictionary<int, CharacterData> characterById = BuildCharacterIdMap();
        Dictionary<int, string> stageFileNameById = stages.ToDictionary(s => s.stageId, s => s.fileName);

        foreach (var s in stages)
        {
            string assetPath = $"Assets/ScriptableObjects/Stages/{s.fileName}.asset";
            StageData data = AssetDatabase.LoadAssetAtPath<StageData>(assetPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<StageData>();
                AssetDatabase.CreateAsset(data, assetPath);
            }

            data.stageId = s.stageId;
            data.stageName = s.stageName;
            data.chapterId = s.chapterId;

            data.enemyPool = new List<CharacterData>();
            if (s.enemyIds != null)
            {
                foreach (var id in s.enemyIds)
                {
                    if (characterById.TryGetValue(id, out CharacterData ch) && ch != null)
                    {
                        data.enemyPool.Add(ch);
                    }
                }
            }

            data.rewardStagePoints = s.rewardStagePoints;
            data.slotCount = s.slotCount;
            data.trapDamage = s.trapDamage;
            data.trapCount = s.trapCount;

            data.reinforcementEnemy = new List<CharacterData>();
            if (s.reinforcementEnemyIds != null)
            {
                foreach (var id in s.reinforcementEnemyIds)
                {
                    if (characterById.TryGetValue(id, out CharacterData ch) && ch != null)
                    {
                        data.reinforcementEnemy.Add(ch);
                    }
                }
            }

            data.reinforcementInterval = s.reinforcementInterval;
            data.reinforcementCount = s.reinforcementCount;
            data.reinforcementLimit = s.reinforcementLimit;
            data.isBossStage = s.isBossStage;
            data.enemyLevel = s.enemyLevel;

            if (s.prerequisiteStageId > 0 && stageFileNameById.TryGetValue(s.prerequisiteStageId, out string prereqFileName))
            {
                string prereqPath = $"Assets/ScriptableObjects/Stages/{prereqFileName}.asset";
                data.prerequisite = AssetDatabase.LoadAssetAtPath<StageData>(prereqPath);
            }
            else
            {
                data.prerequisite = null;
            }

            EditorUtility.SetDirty(data);
        }
        Debug.Log("✅ Stages imported.");
    }

    static void RebuildCharacterDatabase()
    {
        string dbPath = "Assets/ScriptableObjects/Characters/CharacterDatabase.asset";
        CharacterDatabase db = AssetDatabase.LoadAssetAtPath<CharacterDatabase>(dbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<CharacterDatabase>();
            AssetDatabase.CreateAsset(db, dbPath);
        }

        string[] guids = AssetDatabase.FindAssets("t:CharacterData", new[] { "Assets/ScriptableObjects/Characters" });
        db.characters.Clear();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CharacterData data = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            if (data != null)
                db.characters.Add(data);
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log("✅ CharacterDatabase rebuilt.");
    }

    static Dictionary<int, CharacterData> BuildCharacterIdMap()
    {
        var map = new Dictionary<int, CharacterData>();
        string[] guids = AssetDatabase.FindAssets("t:CharacterData", new[] { "Assets/ScriptableObjects/Characters" });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CharacterData data = AssetDatabase.LoadAssetAtPath<CharacterData>(path);
            if (data == null)
            {
                continue;
            }

            if (!map.ContainsKey(data.characterId))
            {
                map.Add(data.characterId, data);
            }
        }

        return map;
    }

    static void RebuildFacilityDatabase()
    {
        string dbPath = "Assets/ScriptableObjects/Facilities/FacilityDatabase.asset";
        FacilityDatabase db = AssetDatabase.LoadAssetAtPath<FacilityDatabase>(dbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<FacilityDatabase>();
            AssetDatabase.CreateAsset(db, dbPath);
        }

        string[] guids = AssetDatabase.FindAssets("t:FacilityData", new[] { "Assets/ScriptableObjects/Facilities" });
        db.facilities.Clear();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            FacilityData data = AssetDatabase.LoadAssetAtPath<FacilityData>(path);
            if (data != null)
            {
                db.facilities.Add(data);
            }
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log("✅ FacilityDatabase rebuilt.");
    }

    static void RebuildStageDatabase()
    {
        string dbPath = "Assets/ScriptableObjects/Stages/StageDatabase.asset";
        StageDatabase db = AssetDatabase.LoadAssetAtPath<StageDatabase>(dbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<StageDatabase>();
            AssetDatabase.CreateAsset(db, dbPath);
        }

        string[] guids = AssetDatabase.FindAssets("t:StageData", new[] { "Assets/ScriptableObjects/Stages" });
        db.stages.Clear();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            StageData data = AssetDatabase.LoadAssetAtPath<StageData>(path);
            if (data != null)
                db.stages.Add(data);
        }

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log("✅ StageDatabase rebuilt.");
    }
}
