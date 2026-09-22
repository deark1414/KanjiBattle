using System.Collections.Generic;
using UnityEngine;

// Shared skill geometry for battle, previews, and regression checks.
public static class BattleRangeService
{
    private static readonly Vector2Int[] AdjacentDirections =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
        new Vector2Int(1, 1), new Vector2Int(-1, 1),
        new Vector2Int(1, -1), new Vector2Int(-1, -1)
    };

    public static List<Vector2Int> GetSelectionCells(
        SkillType skillType,
        Vector2Int caster,
        Vector2Int? target,
        int cols,
        int rows)
    {
        var cells = new List<Vector2Int>();
        switch (skillType)
        {
            case SkillType.Stone:
            case SkillType.Fireball:
            case SkillType.HorseCharge:
            case SkillType.Soil:
                AddBox(cells, caster, 2);
                break;
            case SkillType.Slash:
                if (target.HasValue) cells.AddRange(GetSwordWedgeCells(caster, target.Value - caster));
                break;
            case SkillType.StunBlow:
            case SkillType.TigerTwinClaw:
            case SkillType.WaterHeal:
            case SkillType.BirdRetreat:
            case SkillType.WoodPush:
                AddDirectional(cells, caster, 1, includeDiagonals: true);
                break;
            case SkillType.Arrow:
            case SkillType.Gun:
                if (target.HasValue) cells.AddRange(GetLineToTarget(caster, target.Value, rows + cols));
                break;
            case SkillType.Spear:
                if (target.HasValue) cells.AddRange(GetLineToTarget(caster, target.Value, 2));
                break;
        }

        return FilterBoardCells(cells, caster, cols, rows);
    }

    public static List<Vector2Int> GetEffectCells(
        SkillType skillType,
        Vector2Int caster,
        Vector2Int? target,
        int cols,
        int rows)
    {
        var cells = new List<Vector2Int>();
        switch (skillType)
        {
            case SkillType.Slash:
                if (target.HasValue) cells.AddRange(GetSwordWedgeCells(caster, target.Value - caster));
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
                if (target.HasValue) cells.Add(target.Value);
                break;
            case SkillType.Spear:
                if (target.HasValue) cells.AddRange(GetLineToTarget(caster, target.Value, 2));
                break;
            case SkillType.Gun:
                if (target.HasValue) cells.AddRange(GetLineToTarget(caster, target.Value, rows + cols));
                break;
        }

        return FilterBoardCells(cells, caster, cols, rows);
    }

    public static List<Vector2Int> GetSwordWedgeCells(Vector2Int caster, Vector2Int forward)
    {
        forward = NormalizeDirection(forward);
        if (forward == Vector2Int.zero) return new List<Vector2Int>();

        var cells = new List<Vector2Int> { caster + forward };
        if (forward.x == 0)
        {
            cells.Add(caster + new Vector2Int(1, forward.y));
            cells.Add(caster + new Vector2Int(-1, forward.y));
        }
        else if (forward.y == 0)
        {
            cells.Add(caster + new Vector2Int(forward.x, 1));
            cells.Add(caster + new Vector2Int(forward.x, -1));
        }
        else
        {
            cells.Add(caster + new Vector2Int(forward.x, 0));
            cells.Add(caster + new Vector2Int(0, forward.y));
        }
        return cells;
    }

    public static List<Vector2Int> GetLineToTarget(Vector2Int from, Vector2Int to, int maxDistance)
    {
        return GetLine(from, NormalizeDirection(to - from), maxDistance, stopAt: to);
    }

    public static List<Vector2Int> GetLine(Vector2Int from, Vector2Int direction, int maxDistance)
    {
        return GetLine(from, NormalizeDirection(direction), maxDistance, stopAt: null);
    }

    public static List<Vector2Int> GetLineWithinBoard(
        Vector2Int from,
        Vector2Int direction,
        int maxDistance,
        int cols,
        int rows)
    {
        var cells = new List<Vector2Int>();
        direction = NormalizeDirection(direction);
        if (direction == Vector2Int.zero) return cells;

        for (int distance = 1; distance <= maxDistance; distance++)
        {
            Vector2Int position = from + direction * distance;
            if (position.x < 0 || position.x >= cols || position.y < 0 || position.y >= rows) break;
            cells.Add(position);
        }
        return cells;
    }

    public static Vector2Int NormalizeDirection(Vector2Int direction)
    {
        return new Vector2Int(Mathf.Clamp(direction.x, -1, 1), Mathf.Clamp(direction.y, -1, 1));
    }

    private static List<Vector2Int> GetLine(Vector2Int from, Vector2Int direction, int maxDistance, Vector2Int? stopAt)
    {
        var cells = new List<Vector2Int>();
        if (direction == Vector2Int.zero) return cells;

        for (int distance = 1; distance <= maxDistance; distance++)
        {
            Vector2Int position = from + direction * distance;
            cells.Add(position);
            if (stopAt.HasValue && position == stopAt.Value) break;
        }
        return cells;
    }

    private static void AddBox(List<Vector2Int> cells, Vector2Int center, int radius)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (x != 0 || y != 0) cells.Add(center + new Vector2Int(x, y));
            }
        }
    }

    private static void AddDirectional(List<Vector2Int> cells, Vector2Int center, int distance, bool includeDiagonals)
    {
        int count = includeDiagonals ? AdjacentDirections.Length : 4;
        for (int index = 0; index < count; index++)
        {
            for (int step = 1; step <= distance; step++) cells.Add(center + AdjacentDirections[index] * step);
        }
    }

    private static List<Vector2Int> FilterBoardCells(List<Vector2Int> cells, Vector2Int caster, int cols, int rows)
    {
        var result = new List<Vector2Int>();
        var seen = new HashSet<Vector2Int>();
        foreach (Vector2Int cell in cells)
        {
            if (cell == caster || cell.x < 0 || cell.x >= cols || cell.y < 0 || cell.y >= rows) continue;
            if (seen.Add(cell)) result.Add(cell);
        }
        return result;
    }
}
