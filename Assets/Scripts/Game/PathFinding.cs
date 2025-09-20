using System.Collections.Generic;
using UnityEngine;

public static class Pathfinding
{
    public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, int rows, int cols, HashSet<Vector2Int> blocked)
    {
        Queue<Vector2Int> open = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        open.Enqueue(start);
        cameFrom[start] = start;

        Vector2Int[] dirs = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1,1), new Vector2Int(-1,1), new Vector2Int(1,-1), new Vector2Int(-1,-1)
        };

        while (open.Count > 0)
        {
            var cur = open.Dequeue();
            if (cur == goal) break;

            foreach (var d in dirs)
            {
                Vector2Int next = cur + d;
                if (next.x < 0 || next.x >= cols || next.y < 0 || next.y >= rows) continue;
                if (blocked.Contains(next)) continue;
                if (cameFrom.ContainsKey(next)) continue;

                open.Enqueue(next);
                cameFrom[next] = cur;
            }
        }

        if (!cameFrom.ContainsKey(goal)) return null;

        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int step = goal;
        while (step != start)
        {
            path.Add(step);
            step = cameFrom[step];
        }
        path.Add(start);
        path.Reverse();

        return path;
    }
}