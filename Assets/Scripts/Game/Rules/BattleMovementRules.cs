using System.Collections.Generic;
using UnityEngine;

public static class BattleMovementRules
{
    private static readonly Vector2Int[] MovementDirections =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
        new Vector2Int(1, 1), new Vector2Int(-1, 1),
        new Vector2Int(1, -1), new Vector2Int(-1, -1)
    };

    public static int GetMaxSteps(CharacterCategory category)
    {
        return category == CharacterCategory.Animal ? 2 : 1;
    }

    public static int GetDestinationIndex(CharacterCategory category, List<Vector2Int> path, bool targetCellOccupied)
    {
        if (path == null || path.Count < 2) return 0;

        int lastWalkableIndex = targetCellOccupied ? path.Count - 2 : path.Count - 1;
        return Mathf.Clamp(GetMaxSteps(category), 1, Mathf.Max(1, lastWalkableIndex));
    }

    public static int GetStageObstacleCount(StageData stage)
    {
        if (stage == null || stage.chapterId <= 2) return 0;

        int count = stage.chapterId switch
        {
            3 or 4 => 1,
            5 or 6 => 2,
            _ => 3
        };

        return stage.isBossStage ? Mathf.Max(1, count - 1) : count;
    }

    public static HashSet<Vector2Int> GenerateImpassableCells(
        int cols,
        int rows,
        int desiredCount,
        ISet<Vector2Int> reservedCells,
        int seed)
    {
        var result = new HashSet<Vector2Int>();
        if (cols <= 0 || rows <= 0 || desiredCount <= 0) return result;

        var candidates = new List<Vector2Int>();
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                var cell = new Vector2Int(x, y);
                if (reservedCells == null || !reservedCells.Contains(cell)) candidates.Add(cell);
            }
        }

        var random = new System.Random(seed);
        Shuffle(candidates, random);

        // Water is one terrain feature. Grow each later cell from the existing
        // shoreline so multi-cell water always shares an edge.
        foreach (Vector2Int start in candidates)
        {
            result.Clear();
            result.Add(start);

            while (result.Count < desiredCount)
            {
                var neighbors = CollectOpenNeighbors(cols, rows, result, reservedCells);
                Shuffle(neighbors, random);

                bool added = false;
                foreach (Vector2Int candidate in neighbors)
                {
                    result.Add(candidate);
                    if (AreAllWalkableCellsConnected(cols, rows, result))
                    {
                        added = true;
                        break;
                    }

                    result.Remove(candidate);
                }

                if (!added) break;
            }

            if (result.Count == desiredCount) return result;
        }

        // Preserve the reachability guarantee in the rare case where reservations
        // prevent a contiguous group of the requested size.
        result.Clear();
        foreach (Vector2Int candidate in candidates)
        {
            if (result.Count >= desiredCount) break;
            result.Add(candidate);
            if (!AreAllWalkableCellsConnected(cols, rows, result)) result.Remove(candidate);
        }

        return result;
    }

    private static List<Vector2Int> CollectOpenNeighbors(
        int cols,
        int rows,
        ISet<Vector2Int> selected,
        ISet<Vector2Int> reserved)
    {
        var result = new List<Vector2Int>();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int cell in selected)
        {
            foreach (Vector2Int direction in directions)
            {
                Vector2Int next = cell + direction;
                if (next.x < 0 || next.x >= cols || next.y < 0 || next.y >= rows) continue;
                if (selected.Contains(next) || (reserved != null && reserved.Contains(next))) continue;
                if (!result.Contains(next)) result.Add(next);
            }
        }

        return result;
    }

    private static void Shuffle<T>(List<T> values, System.Random random)
    {
        for (int index = values.Count - 1; index > 0; index--)
        {
            int swapIndex = random.Next(index + 1);
            (values[index], values[swapIndex]) = (values[swapIndex], values[index]);
        }
    }

    public static bool AreAllWalkableCellsConnected(int cols, int rows, ISet<Vector2Int> impassableCells)
    {
        Vector2Int? start = null;
        int walkableCount = 0;
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                var cell = new Vector2Int(x, y);
                if (impassableCells != null && impassableCells.Contains(cell)) continue;
                start ??= cell;
                walkableCount++;
            }
        }

        if (!start.HasValue) return false;

        var visited = new HashSet<Vector2Int> { start.Value };
        var open = new Queue<Vector2Int>();
        open.Enqueue(start.Value);

        while (open.Count > 0)
        {
            Vector2Int current = open.Dequeue();
            foreach (Vector2Int direction in MovementDirections)
            {
                Vector2Int next = current + direction;
                if (next.x < 0 || next.x >= cols || next.y < 0 || next.y >= rows) continue;
                if (impassableCells != null && impassableCells.Contains(next)) continue;
                if (visited.Add(next)) open.Enqueue(next);
            }
        }

        return visited.Count == walkableCount;
    }
}
