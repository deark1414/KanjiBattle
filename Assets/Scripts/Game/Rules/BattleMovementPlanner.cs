using System.Collections.Generic;
using UnityEngine;

public static class BattleMovementPlanner
{
    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
        new(1, 1), new(-1, 1), new(1, -1), new(-1, -1)
    };

    public static List<Vector2Int> FindNextMove(Vector2Int start, IReadOnlyList<Vector2Int> targets,
        SkillType attackSkill, CharacterCategory category, ISet<Vector2Int> friendlyCells,
        ISet<Vector2Int> enemyCells, ISet<Vector2Int> terrain, int cols, int rows)
    {
        if (targets == null || targets.Count == 0) return null;
        var friends = friendlyCells != null ? new HashSet<Vector2Int>(friendlyCells) : new HashSet<Vector2Int>();
        friends.Remove(start);
        var blocked = terrain != null ? new HashSet<Vector2Int>(terrain) : new HashSet<Vector2Int>();
        if (enemyCells != null) blocked.UnionWith(enemyCells);

        var queue = new Queue<Vector2Int>();
        var firstMoves = new Dictionary<Vector2Int, List<Vector2Int>> { [start] = null };
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            Vector2Int position = queue.Dequeue();
            foreach (Vector2Int target in targets)
            {
                if (BattleRangeService.CanTargetCell(attackSkill, position, target, cols, rows, friends))
                    return firstMoves[position];
            }

            // Search legal turns rather than individual cells: animals can cross
            // allies within a two-step turn, but no unit can end on an occupied cell.
            foreach (List<Vector2Int> move in CollectMoves(position, BattleMovementRules.GetMaxSteps(category), friends, blocked, cols, rows))
            {
                Vector2Int destination = move[move.Count - 1];
                if (firstMoves.ContainsKey(destination)) continue;
                firstMoves[destination] = firstMoves[position] ?? move;
                queue.Enqueue(destination);
            }
        }
        return null;
    }

    private static List<List<Vector2Int>> CollectMoves(Vector2Int start, int maxSteps,
        ISet<Vector2Int> friends, ISet<Vector2Int> blocked, int cols, int rows)
    {
        var moves = new List<List<Vector2Int>>();
        var queue = new Queue<List<Vector2Int>>();
        var visited = new HashSet<Vector2Int> { start };
        queue.Enqueue(new List<Vector2Int> { start });
        while (queue.Count > 0)
        {
            List<Vector2Int> path = queue.Dequeue();
            if (path.Count - 1 >= maxSteps) continue;
            Vector2Int position = path[path.Count - 1];
            foreach (Vector2Int direction in Directions)
            {
                Vector2Int next = position + direction;
                if (next.x < 0 || next.x >= cols || next.y < 0 || next.y >= rows || blocked.Contains(next) || !visited.Add(next)) continue;
                var candidate = new List<Vector2Int>(path) { next };
                if (!friends.Contains(next)) moves.Add(candidate);
                queue.Enqueue(candidate);
            }
        }
        return moves;
    }
}
