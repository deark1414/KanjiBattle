using System.Collections.Generic;
using UnityEngine;

public static class BattleTargetFinder
{
    // Returns the first adjacent enemy in 8 directions, or null if none found.
    public static BattleCharacter FindAdjacentEnemy(BattleManager bm, BattleCharacter self)
    {
        List<Vector2Int> dirs = new()
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1,1), new Vector2Int(-1,1), new Vector2Int(1,-1), new Vector2Int(-1,-1)
        };
        foreach (var dir in dirs)
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

    // Returns the first adjacent ally in 8 directions, or null if none found.
    public static BattleCharacter FindAdjacentAlly(BattleManager bm, BattleCharacter self)
    {
        List<Vector2Int> dirs = new()
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1,1), new Vector2Int(-1,1), new Vector2Int(1,-1), new Vector2Int(-1,-1)
        };
        foreach (var dir in dirs)
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

    // Returns the nearest enemy from the candidates list, or null if none found.
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

    // Returns the nearest ally from the candidates list, or null if none found.
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
    // 剣スキル用
    public static BattleCharacter FindSwordTarget(BattleManager bm, BattleCharacter self)
    {
        List<Vector2Int> dirs = new()
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1,1), new Vector2Int(-1,1), new Vector2Int(1,-1), new Vector2Int(-1,-1)
        };

        List<BattleCharacter> candidates = new();

        foreach (var dir in dirs)
        {
            Vector2Int pos1 = self.gridPos + dir;
            Vector2Int pos2 = self.gridPos + dir * 2;
            if (bm.gridMap.ContainsKey(pos1) && bm.gridMap[pos1].isAlly != self.isAlly && bm.gridMap[pos1].currentHP > 0)
            {
                candidates.Add(bm.gridMap[pos1]);
            }
            else if (bm.gridMap.ContainsKey(pos2) && bm.gridMap[pos2].isAlly != self.isAlly && bm.gridMap[pos2].currentHP > 0)
            {
                candidates.Add(bm.gridMap[pos2]);
            }
        }
        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    // 矢スキル用
    public static BattleCharacter FindArrowTarget(BattleManager bm, BattleCharacter self)
    {
        List<Vector2Int> dirs = new()
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1,1), new Vector2Int(-1,1), new Vector2Int(1,-1), new Vector2Int(-1,-1)
        };

        List<BattleCharacter> candidates = new();
        foreach (var dir in dirs)
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
                    else break;
                }
            }
        }
        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    // 槍スキル用
    public static BattleCharacter FindSpearTarget(BattleManager bm, BattleCharacter self)
    {
        List<Vector2Int> dirs = new()
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1,1), new Vector2Int(-1,1), new Vector2Int(1,-1), new Vector2Int(-1,-1)
        };

        List<BattleCharacter> candidates = new();

        foreach (var dir in dirs)
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

    // 石スキル用
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

    // 銃スキル用
    public static BattleCharacter FindGunTarget(BattleManager bm, BattleCharacter self)
    {
        List<Vector2Int> dirs = new()
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
            new Vector2Int(1,1), new Vector2Int(-1,1), new Vector2Int(1,-1), new Vector2Int(-1,-1)
        };

        List<BattleCharacter> candidates = new();
        foreach (var dir in dirs)
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
                    else break;
                }
            }
        }
        return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
    }

    // 馬突進スキル用
    public static BattleCharacter FindHorseChargeTarget(
        BattleManager bm,
        BattleCharacter self,
        List<BattleCharacter> candidates)
    {
        List<BattleCharacter> validTargets = new();

        foreach (var c in candidates)
        {
            if (c == null || c.isDead) continue;

            int dx = Mathf.Abs(c.gridPos.x - self.gridPos.x);
            int dy = Mathf.Abs(c.gridPos.y - self.gridPos.y);

            // 2マス以内なら候補に追加
            if (dx <= 2 && dy <= 2 && (dx + dy) > 0)
            {
                validTargets.Add(c);
            }
        }

        if (validTargets.Count > 0)
        {
            return validTargets[Random.Range(0, validTargets.Count)];
        }

        return null;
    }
}