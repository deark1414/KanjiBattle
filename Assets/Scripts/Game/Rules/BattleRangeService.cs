using System.Collections.Generic;
using UnityEngine;

// Shared skill geometry for battle, previews, and regression checks.
public static class BattleRangeService
{
    public static int MinimumRange(SkillType skill) => skill is SkillType.Arrow or SkillType.Gun ? 2 : 1;

    public static int Distance(Vector2Int from, Vector2Int to) =>
        Mathf.Max(Mathf.Abs(to.x - from.x), Mathf.Abs(to.y - from.y));

    public static bool CanTargetCell(SkillType skill, Vector2Int from, Vector2Int to,
        int cols, int rows, ISet<Vector2Int> friendlyCells = null)
    {
        skill = BattleActionRules.GeometrySkill(skill);
        if (to.x < 0 || to.x >= cols || to.y < 0 || to.y >= rows) return false;
        int distance = Distance(from, to);
        if (skill == SkillType.WaterHeal) return distance <= 1;
        if (distance < MinimumRange(skill)) return false;
        if (skill is SkillType.Stone or SkillType.Fireball or SkillType.ThreadPull) return distance <= 2;
        if (skill is SkillType.ScatterShot or SkillType.ChainLightning) return distance <= 3;
        if (skill is not (SkillType.Arrow or SkillType.Gun or SkillType.Spear)) return distance == 1;

        Vector2Int delta = to - from;
        if (delta.x != 0 && delta.y != 0 && Mathf.Abs(delta.x) != Mathf.Abs(delta.y)) return false;
        if (skill != SkillType.Spear) return true;
        if (distance > 2) return false;
        Vector2Int direction = NormalizeDirection(delta);
        for (int step = 1; step < distance; step++)
            if (friendlyCells != null && friendlyCells.Contains(from + direction * step)) return false;
        return true;
    }

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
        skillType = BattleActionRules.GeometrySkill(skillType);
        if (skillType is SkillType.DoubleSlash or SkillType.ScatterShot)
            return GetEffectCells(skillType, caster, target, cols, rows);
        var cells = new List<Vector2Int>();
        switch (skillType)
        {
            case SkillType.ChainLightning:
                AddBox(cells, caster, 3);
                break;
            case SkillType.Stone:
            case SkillType.ThreadPull:
            case SkillType.Fireball:
            case SkillType.HorseCharge:
            case SkillType.Soil:
                AddBox(cells, caster, 2);
                break;
            case SkillType.Slash:
                if (target.HasValue) cells.AddRange(GetSwordWedgeCells(caster, target.Value - caster));
                break;
            case SkillType.StunBlow:
            case SkillType.ShadowCopy:
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

        return FilterAttackCells(cells, skillType, caster, cols, rows);
    }

    public static List<Vector2Int> GetEffectCells(
        SkillType skillType,
        Vector2Int caster,
        Vector2Int? target,
        int cols,
        int rows)
    {
        skillType = BattleActionRules.GeometrySkill(skillType);
        var cells = new List<Vector2Int>();
        switch (skillType)
        {
            case SkillType.ScatterShot:
                if (target.HasValue)
                {
                    Vector2Int direction = NormalizeDirection(target.Value - caster);
                    cells.AddRange(GetSwordWedgeCells(target.Value - direction, direction));
                }
                break;
            case SkillType.DoubleSlash:
            case SkillType.Slash:
                if (target.HasValue) cells.AddRange(GetSwordWedgeCells(caster, target.Value - caster));
                break;
            case SkillType.StunBlow:
            case SkillType.ChainLightning:
            case SkillType.ThreadPull:
            case SkillType.ShadowCopy:
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
            case SkillType.Soil:
                // The resolved trap cells are highlighted separately because their
                // positions are chosen during skill resolution.
                break;
            case SkillType.Gun:
                if (target.HasValue) cells.AddRange(GetLineToTarget(caster, target.Value, rows + cols));
                break;
        }

        return FilterAttackCells(cells, skillType, caster, cols, rows);
    }

    private static List<Vector2Int> FilterAttackCells(List<Vector2Int> cells, SkillType skill,
        Vector2Int caster, int cols, int rows)
    {
        var result = FilterBoardCells(cells, caster, cols, rows);
        result.RemoveAll(cell => Distance(caster, cell) < MinimumRange(skill));
        return result;
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
