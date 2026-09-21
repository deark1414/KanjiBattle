using System.Collections.Generic;
using UnityEngine;

public static class TargetingService
{
    private static readonly Vector2Int[] AdjacentDirections =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
        new Vector2Int(1, 1), new Vector2Int(-1, 1),
        new Vector2Int(1, -1), new Vector2Int(-1, -1)
    };

    public static BattleCharacter FindAdjacentEnemy(BattleManager bm, BattleCharacter self)
    {
        foreach (var dir in GetAdjacentDirs())
        {
            Vector2Int pos = self.gridPos + dir;
            if (bm.gridMap.ContainsKey(pos))
            {
                var bc = bm.gridMap[pos];
                if (bc.isAlly != self.isAlly && bc.currentHP > 0)
                {
                    return bc;
                }
            }
        }
        return null;
    }

    public static BattleCharacter FindAdjacentAlly(BattleManager bm, BattleCharacter self)
    {
        foreach (var dir in GetAdjacentDirs())
        {
            Vector2Int pos = self.gridPos + dir;
            if (bm.gridMap.ContainsKey(pos))
            {
                var bc = bm.gridMap[pos];
                if (bc.isAlly == self.isAlly && bc != self && bc.currentHP > 0)
                {
                    return bc;
                }
            }
        }
        return null;
    }

    public static BattleCharacter FindNearestEnemy(BattleManager bm, BattleCharacter self, List<BattleCharacter> candidates)
    {
        BattleCharacter nearest = null;
        int minDist = int.MaxValue;
        foreach (var bc in candidates)
        {
            if (bc.isAlly == self.isAlly || bc.currentHP <= 0) continue;
            int dist = Mathf.Abs(bc.gridPos.x - self.gridPos.x) + Mathf.Abs(bc.gridPos.y - self.gridPos.y);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = bc;
            }
        }
        return nearest;
    }

    public static BattleCharacter FindNearestAlly(BattleManager bm, BattleCharacter self, List<BattleCharacter> candidates)
    {
        BattleCharacter nearest = null;
        int minDist = int.MaxValue;
        foreach (var bc in candidates)
        {
            if ((bc.isAlly != self.isAlly) || bc == self || bc.currentHP <= 0) continue;
            int dist = Mathf.Abs(bc.gridPos.x - self.gridPos.x) + Mathf.Abs(bc.gridPos.y - self.gridPos.y);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = bc;
            }
        }
        return nearest;
    }

    public static BattleCharacter FindSwordTarget(BattleManager bm, BattleCharacter self)
    {
        List<BattleCharacter> candidates = new();
        int bestHitCount = 0;
        foreach (var dir in GetAdjacentDirs())
        {
            Vector2Int targetPos = self.gridPos + dir;
            if (!bm.gridMap.TryGetValue(targetPos, out BattleCharacter target)
                || target == null
                || target.isDead
                || target.isAlly == self.isAlly)
            {
                continue;
            }

            int hitCount = GetSwordWedgeTargets(bm, self, dir).Count;
            if (hitCount > bestHitCount)
            {
                candidates.Clear();
                bestHitCount = hitCount;
            }
            if (hitCount == bestHitCount)
            {
                candidates.Add(target);
            }
        }
        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    // 剣は正面を中心に、左右前を含めた3マスを同時に薙ぐ。
    // 斜めを正面にした場合も、隣接する横・縦の2マスで自然な扇形になる。
    public static List<Vector2Int> GetSwordWedgeCells(Vector2Int center, Vector2Int forward)
    {
        forward = NormalizeDirection(forward);
        if (forward == Vector2Int.zero)
        {
            return new List<Vector2Int>();
        }

        var cells = new List<Vector2Int> { center + forward };
        if (forward.x == 0)
        {
            cells.Add(center + new Vector2Int(1, forward.y));
            cells.Add(center + new Vector2Int(-1, forward.y));
        }
        else if (forward.y == 0)
        {
            cells.Add(center + new Vector2Int(forward.x, 1));
            cells.Add(center + new Vector2Int(forward.x, -1));
        }
        else
        {
            cells.Add(center + new Vector2Int(forward.x, 0));
            cells.Add(center + new Vector2Int(0, forward.y));
        }

        return cells;
    }

    public static Vector2Int GetSwordAttackDirection(BattleCharacter self, BattleCharacter target)
    {
        if (self == null || target == null)
        {
            return Vector2Int.zero;
        }
        return NormalizeDirection(target.gridPos - self.gridPos);
    }

    public static List<BattleCharacter> GetSwordWedgeTargets(
        BattleManager bm,
        BattleCharacter self,
        Vector2Int forward)
    {
        var targets = new List<BattleCharacter>();
        if (bm == null || self == null)
        {
            return targets;
        }

        foreach (Vector2Int position in GetSwordWedgeCells(self.gridPos, forward))
        {
            if (bm.gridMap.TryGetValue(position, out BattleCharacter target)
                && target != null
                && !target.isDead
                && target.isAlly != self.isAlly)
            {
                targets.Add(target);
            }
        }
        return targets;
    }

    private static Vector2Int NormalizeDirection(Vector2Int direction)
    {
        return new Vector2Int(Mathf.Clamp(direction.x, -1, 1), Mathf.Clamp(direction.y, -1, 1));
    }

    public static BattleCharacter FindArrowTarget(BattleManager bm, BattleCharacter self)
    {
        List<BattleCharacter> candidates = new();
        foreach (var dir in GetAdjacentDirs())
        {
            for (int d = 1; d <= bm.Rows + bm.Cols; d++)
            {
                Vector2Int pos = self.gridPos + dir * d;
                if (bm.gridMap.ContainsKey(pos))
                {
                    var bc = bm.gridMap[pos];
                    if (bc.isAlly != self.isAlly && bc.currentHP > 0)
                    {
                        candidates.Add(bc);
                        break;
                    }
                    break;
                }
            }
        }
        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    public static BattleCharacter FindSpearTarget(BattleManager bm, BattleCharacter self)
    {
        List<BattleCharacter> candidates = new();
        foreach (var dir in GetAdjacentDirs())
        {
            Vector2Int pos1 = self.gridPos + dir;
            Vector2Int pos2 = self.gridPos + dir * 2;

            if (bm.gridMap.ContainsKey(pos1))
            {
                var bc1 = bm.gridMap[pos1];
                if (bc1.isAlly != self.isAlly && bc1.currentHP > 0)
                {
                    candidates.Add(bc1);
                    continue;
                }
            }
            if (bm.gridMap.ContainsKey(pos2))
            {
                var bc2 = bm.gridMap[pos2];
                if (bc2.isAlly != self.isAlly && bc2.currentHP > 0)
                {
                    candidates.Add(bc2);
                }
            }
        }
        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    public static BattleCharacter FindStoneTarget(BattleManager bm, BattleCharacter self)
    {
        List<BattleCharacter> candidates = new();
        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int tx = self.gridPos.x + dx;
                int ty = self.gridPos.y + dy;
                if (tx < 0 || tx >= bm.Cols || ty < 0 || ty >= bm.Rows) continue;
                Vector2Int pos = new Vector2Int(tx, ty);
                if (bm.gridMap.ContainsKey(pos))
                {
                    var bc = bm.gridMap[pos];
                    if (bc.isAlly != self.isAlly && bc.currentHP > 0)
                        candidates.Add(bc);
                }
            }
        }
        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    public static BattleCharacter FindGunTarget(BattleManager bm, BattleCharacter self)
    {
        List<BattleCharacter> candidates = new();
        foreach (var dir in GetAdjacentDirs())
        {
            for (int d = 1; d <= bm.Rows + bm.Cols; d++)
            {
                Vector2Int pos = self.gridPos + dir * d;
                if (pos.x < 0 || pos.x >= bm.Cols || pos.y < 0 || pos.y >= bm.Rows) break;

                if (bm.gridMap.ContainsKey(pos))
                {
                    var bc = bm.gridMap[pos];
                    if (bc.isAlly != self.isAlly && bc.currentHP > 0)
                    {
                        candidates.Add(bc);
                        break;
                    }
                    break;
                }
            }
        }
        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    public static BattleCharacter FindHorseChargeTarget(
        BattleManager bm,
        BattleCharacter self,
        List<BattleCharacter> candidates)
    {
        List<BattleCharacter> validTargets = new();
        if (bm == null || self == null || candidates == null)
        {
            return null;
        }

        // 馬の突進は桂馬跳びではなく、八方向へ一直線に最大2マス進む攻撃。
        // 途中のコマを飛び越えず、候補に存在する敵だけを採用する。
        foreach (Vector2Int dir in GetAdjacentDirs())
        {
            Vector2Int pos1 = self.gridPos + dir;
            Vector2Int pos2 = self.gridPos + dir * 2;

            if (bm.gridMap.TryGetValue(pos1, out BattleCharacter first)
                && first != null
                && !first.isDead
                && first.isAlly != self.isAlly
                && candidates.Contains(first))
            {
                validTargets.Add(first);
                continue;
            }

            // 1マス目が空いている時だけ、2マス目へ突進できる。
            if (!bm.gridMap.ContainsKey(pos1)
                && bm.gridMap.TryGetValue(pos2, out BattleCharacter second)
                && second != null
                && !second.isDead
                && second.isAlly != self.isAlly
                && candidates.Contains(second))
            {
                validTargets.Add(second);
            }
        }

        if (validTargets.Count > 0)
        {
            return validTargets[Random.Range(0, validTargets.Count)];
        }

        return null;
    }

    private static Vector2Int[] GetAdjacentDirs()
    {
        return AdjacentDirections;
    }
}
