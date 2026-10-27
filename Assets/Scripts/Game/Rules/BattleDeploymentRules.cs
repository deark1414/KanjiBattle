using System;
using System.Collections.Generic;
using UnityEngine;

public static class BattleDeploymentRules
{
    public const int Columns = 6;
    public const int Rows = 7;
    public const int DeploymentRows = 2;
    public const int Capacity = Columns * DeploymentRows;
    private const string SaveKey = "KanjiBattle.Deployment.Positions.v1";
    private static readonly int[] ColumnOrder = { 2, 3, 1, 4, 0, 5 };

    [Serializable]
    public sealed class SavedLayout
    {
        public int version = 1;
        public List<SavedPosition> positions = new();
    }

    [Serializable]
    public sealed class SavedPosition
    {
        public int characterId;
        public int occurrence;
        public int x;
        public int y;
    }

    public static bool IsAllyCell(Vector2Int cell) =>
        cell.x >= 0 && cell.x < Columns && cell.y >= Rows - DeploymentRows && cell.y < Rows;

    public static Vector2Int DefaultPosition(int index, bool ally)
    {
        if (index < 0 || index >= Capacity) throw new ArgumentOutOfRangeException(nameof(index));
        int row = index / Columns;
        return new Vector2Int(ColumnOrder[index % Columns], ally ? Rows - DeploymentRows + row : DeploymentRows - 1 - row);
    }

    public static List<Vector2Int> Resolve(IReadOnlyList<int> characterIds, SavedLayout saved)
    {
        if (characterIds == null) throw new ArgumentNullException(nameof(characterIds));
        if (characterIds.Count > Capacity) throw new ArgumentOutOfRangeException(nameof(characterIds));
        var result = new List<Vector2Int>();
        var reserved = new HashSet<Vector2Int>();
        var occurrences = new Dictionary<int, int>();
        for (int index = 0; index < characterIds.Count; index++)
        {
            int id = characterIds[index];
            occurrences.TryGetValue(id, out int occurrence);
            occurrences[id] = occurrence + 1;
            Vector2Int position = new(-1, -1);
            if (saved != null && saved.version == 1 && saved.positions != null)
            {
                foreach (SavedPosition entry in saved.positions)
                {
                    if (entry == null || entry.characterId != id || entry.occurrence != occurrence) continue;
                    var candidate = new Vector2Int(entry.x, entry.y);
                    if (IsAllyCell(candidate) && !reserved.Contains(candidate)) position = candidate;
                    break;
                }
            }
            result.Add(position);
            if (IsAllyCell(position)) reserved.Add(position);
        }

        // Reserve every valid saved cell before filling new or invalid entries.
        for (int index = 0; index < result.Count; index++)
        {
            if (IsAllyCell(result[index])) continue;
            for (int candidate = 0; candidate < Capacity; candidate++)
            {
                Vector2Int cell = DefaultPosition(candidate, true);
                if (!reserved.Add(cell)) continue;
                result[index] = cell;
                break;
            }
        }
        return result;
    }

    public static SavedLayout Capture(IReadOnlyList<int> characterIds, IReadOnlyList<Vector2Int> positions)
    {
        if (characterIds == null || positions == null || characterIds.Count != positions.Count || positions.Count > Capacity)
            throw new ArgumentException("Deployment entries must match the party.");
        var saved = new SavedLayout();
        var occupied = new HashSet<Vector2Int>();
        var occurrences = new Dictionary<int, int>();
        for (int index = 0; index < characterIds.Count; index++)
        {
            Vector2Int cell = positions[index];
            if (!IsAllyCell(cell) || !occupied.Add(cell)) throw new ArgumentException("Invalid deployment position.");
            int id = characterIds[index];
            occurrences.TryGetValue(id, out int occurrence);
            occurrences[id] = occurrence + 1;
            saved.positions.Add(new SavedPosition { characterId = id, occurrence = occurrence, x = cell.x, y = cell.y });
        }
        return saved;
    }

    public static SavedLayout Load()
    {
        string json = PlayerProgressStore.GetString(SaveKey);
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonUtility.FromJson<SavedLayout>(json); }
        catch (ArgumentException) { return null; }
    }

    public static void Save(IReadOnlyList<int> characterIds, IReadOnlyList<Vector2Int> positions)
    {
        PlayerProgressStore.SetString(SaveKey, JsonUtility.ToJson(Capture(characterIds, positions)));
        PlayerProgressStore.Save();
    }
}
