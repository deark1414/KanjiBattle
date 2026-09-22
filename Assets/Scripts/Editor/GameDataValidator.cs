using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class GameDataValidator
{
    private const string CharacterDatabasePath = "Assets/ScriptableObjects/Characters/CharacterDatabase.asset";
    private const string ResourcesCharacterDatabasePath = "Assets/Resources/CharacterDatabase.asset";
    private const string FacilityDatabasePath = "Assets/ScriptableObjects/Facilities/FacilityDatabase.asset";
    private const string StageDatabasePath = "Assets/ScriptableObjects/Stages/StageDatabase.asset";

    [MenuItem("Tools/Validation/Validate Game Data")]
    public static void ValidateFromEditor()
    {
        ValidateOrThrow();
        Debug.Log("[GameDataValidator] Passed.");
    }

    public static void ValidateFromCommandLine()
    {
        ValidateOrThrow();
        Debug.Log("[GameDataValidator] Passed.");
    }

    private static void ValidateOrThrow()
    {
        var errors = new List<string>();
        CharacterDatabase characterDatabase = LoadRequired<CharacterDatabase>(CharacterDatabasePath, errors);
        CharacterDatabase resourcesCharacterDatabase = LoadRequired<CharacterDatabase>(ResourcesCharacterDatabasePath, errors);
        FacilityDatabase facilityDatabase = LoadRequired<FacilityDatabase>(FacilityDatabasePath, errors);
        StageDatabase stageDatabase = LoadRequired<StageDatabase>(StageDatabasePath, errors);

        HashSet<int> characterIds = ValidateCharacters(characterDatabase, "CharacterDatabase", errors);
        HashSet<int> resourceCharacterIds = ValidateCharacters(resourcesCharacterDatabase, "Resources CharacterDatabase", errors);
        ValidateSameIds(characterIds, resourceCharacterIds, "CharacterDatabase", "Resources CharacterDatabase", errors);
        ValidateJsonIds<CharacterJson>("Assets/Data/characters.json", entry => entry.id, characterIds, "characters", errors);

        HashSet<int> facilityIds = ValidateFacilities(facilityDatabase, errors);
        ValidateJsonIds<FacilityJson>("Assets/Data/facilities.json", entry => entry.id, facilityIds, "facilities", errors);

        HashSet<int> stageIds = ValidateStages(stageDatabase, characterIds, errors);
        ValidateJsonIds<StageJson>("Assets/Data/stages.json", entry => entry.stageId, stageIds, "stages", errors);

        if (errors.Count > 0)
        {
            throw new InvalidOperationException("[GameDataValidator]\n - " + string.Join("\n - ", errors));
        }
    }

    private static T LoadRequired<T>(string path, List<string> errors) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) errors.Add($"Required asset is missing: {path}");
        return asset;
    }

    private static HashSet<int> ValidateCharacters(CharacterDatabase database, string label, List<string> errors)
    {
        var ids = new HashSet<int>();
        if (database == null) return ids;

        foreach (CharacterData character in database.characters)
        {
            if (character == null)
            {
                errors.Add($"{label} contains a missing character reference.");
                continue;
            }
            AddPositiveUniqueId(ids, character.characterId, $"{label} character '{character.name}'", errors);
        }
        return ids;
    }

    private static HashSet<int> ValidateFacilities(FacilityDatabase database, List<string> errors)
    {
        var ids = new HashSet<int>();
        if (database == null) return ids;

        foreach (FacilityData facility in database.facilities)
        {
            if (facility == null)
            {
                errors.Add("FacilityDatabase contains a missing facility reference.");
                continue;
            }
            AddPositiveUniqueId(ids, facility.facilityId, $"facility '{facility.name}'", errors);
        }
        return ids;
    }

    private static HashSet<int> ValidateStages(StageDatabase database, HashSet<int> characterIds, List<string> errors)
    {
        var ids = new HashSet<int>();
        if (database == null) return ids;

        foreach (StageData stage in database.stages)
        {
            if (stage == null)
            {
                errors.Add("StageDatabase contains a missing stage reference.");
                continue;
            }

            AddPositiveUniqueId(ids, stage.stageId, $"stage '{stage.name}'", errors);
            ValidateCharacterReferences(stage.enemyPool, stage.name + " enemyPool", characterIds, errors);
            ValidateCharacterReferences(stage.reinforcementEnemy, stage.name + " reinforcementEnemy", characterIds, errors);
            if (stage.prerequisite != null && stage.prerequisite.stageId >= stage.stageId)
            {
                errors.Add($"Stage '{stage.name}' prerequisite must have a lower stage ID.");
            }
        }
        return ids;
    }

    private static void ValidateCharacterReferences(List<CharacterData> characters, string label, HashSet<int> characterIds, List<string> errors)
    {
        if (characters == null) return;
        foreach (CharacterData character in characters)
        {
            if (character == null || !characterIds.Contains(character.characterId))
            {
                errors.Add($"{label} contains an unresolved character reference.");
            }
        }
    }

    private static void AddPositiveUniqueId(HashSet<int> ids, int id, string label, List<string> errors)
    {
        if (id <= 0) errors.Add($"{label} has a non-positive ID ({id}).");
        else if (!ids.Add(id)) errors.Add($"Duplicate ID {id} found for {label}.");
    }

    private static void ValidateSameIds(HashSet<int> left, HashSet<int> right, string leftLabel, string rightLabel, List<string> errors)
    {
        if (left.SetEquals(right)) return;
        errors.Add($"{leftLabel} and {rightLabel} do not contain the same IDs.");
    }

    private static void ValidateJsonIds<T>(string path, Func<T, int> getId, HashSet<int> assetIds, string label, List<string> errors)
    {
        if (!File.Exists(path))
        {
            errors.Add($"Required JSON file is missing: {path}");
            return;
        }

        string wrapped = "{\"items\":" + File.ReadAllText(path) + "}";
        JsonArray<T> parsed = JsonUtility.FromJson<JsonArray<T>>(wrapped);
        var jsonIds = new HashSet<int>();
        if (parsed?.items != null)
        {
            foreach (T item in parsed.items) AddPositiveUniqueId(jsonIds, getId(item), label + " JSON", errors);
        }

        if (!jsonIds.SetEquals(assetIds)) errors.Add($"{label} JSON and database assets are out of sync.");
    }

    [Serializable] private class JsonArray<T> { public List<T> items; }
    [Serializable] private class CharacterJson { public int id; }
    [Serializable] private class FacilityJson { public int id; }
    [Serializable] private class StageJson { public int stageId; }
}
