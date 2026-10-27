using System.Collections.Generic;
using UnityEngine;

public static class TargetingService
{
    public static BattleCharacter FindAdjacentEnemy(BattleManager bm, BattleCharacter self) =>
        FindEnemyTarget(bm, self, SkillType.None);

    public static BattleCharacter FindAdjacentAlly(BattleManager bm, BattleCharacter self)
    {
        BattleCharacter best = null;
        int equalCount = 0;
        if (bm == null || self == null) return null;
        foreach (BattleCharacter candidate in bm.gridMap.Values)
        {
            if (!IsLiving(candidate) || candidate.isAlly != self.isAlly || candidate.currentHP >= candidate.maxHP
                || !BattleRangeService.CanTargetCell(SkillType.WaterHeal, self.gridPos, candidate.gridPos, bm.Cols, bm.Rows)) continue;
            long ratio = best == null ? -1 : (long)candidate.currentHP * best.maxHP - (long)best.currentHP * candidate.maxHP;
            if (best == null || ratio < 0)
            {
                best = candidate;
                equalCount = 1;
            }
            else if (ratio == 0 && Random.Range(0, ++equalCount) == 0) best = candidate;
        }
        return best;
    }

    public static BattleCharacter FindNearestEnemy(BattleManager bm, BattleCharacter self, List<BattleCharacter> candidates) =>
        FindNearest(self, candidates, false);

    public static BattleCharacter FindNearestAlly(BattleManager bm, BattleCharacter self, List<BattleCharacter> candidates) =>
        FindNearest(self, candidates, true);

    private static BattleCharacter FindNearest(BattleCharacter self, IEnumerable<BattleCharacter> candidates, bool ally)
    {
        if (self == null || candidates == null) return null;
        BattleCharacter best = null;
        int distance = int.MaxValue, equalCount = 0;
        foreach (BattleCharacter candidate in candidates)
        {
            if (!IsLiving(candidate) || candidate == self || (candidate.isAlly == self.isAlly) != ally) continue;
            int nextDistance = BattleRangeService.Distance(self.gridPos, candidate.gridPos);
            if (nextDistance < distance)
            {
                distance = nextDistance;
                best = candidate;
                equalCount = 1;
            }
            else if (nextDistance == distance && Random.Range(0, ++equalCount) == 0) best = candidate;
        }
        return best;
    }

    public static BattleCharacter FindEnemyTarget(BattleManager bm, BattleCharacter self, SkillType skill)
    {
        if (bm == null || self == null) return null;
        var friends = new HashSet<Vector2Int>();
        foreach (BattleCharacter character in bm.gridMap.Values)
            if (IsLiving(character) && character.isAlly == self.isAlly) friends.Add(character.gridPos);
        var candidates = new List<BattleCharacter>();
        foreach (BattleCharacter candidate in bm.gridMap.Values)
        {
            if (!IsLiving(candidate) || candidate.isAlly == self.isAlly) continue;
            if (BattleRangeService.CanTargetCell(skill, self.gridPos, candidate.gridPos, bm.Cols, bm.Rows, friends)) candidates.Add(candidate);
        }
        if (FusionReactionRules.IsProjectile(skill))
        {
            var lamps = candidates.FindAll(unit => unit.data.skillType == SkillType.Lantern);
            if (lamps.Count > 0) candidates = lamps;
        }
        return skill == SkillType.ScatterShot ? FusionSkillExecutor.FindScatterTarget(bm,self,candidates)
            : FindNearest(self, candidates, false);
    }

    public static BattleCharacter FindArrowTarget(BattleManager bm, BattleCharacter self) => FindEnemyTarget(bm, self, SkillType.Arrow);
    public static BattleCharacter FindGunTarget(BattleManager bm, BattleCharacter self) => FindEnemyTarget(bm, self, SkillType.Gun);
    public static BattleCharacter FindSpearTarget(BattleManager bm, BattleCharacter self) => FindEnemyTarget(bm, self, SkillType.Spear);
    public static BattleCharacter FindStoneTarget(BattleManager bm, BattleCharacter self) => FindEnemyTarget(bm, self, SkillType.Stone);

    public static BattleCharacter FindSwordTarget(BattleManager bm, BattleCharacter self)
    {
        if (bm == null || self == null) return null;
        BattleCharacter best = null;
        int bestCount = 0, equalCount = 0;
        foreach (BattleCharacter target in bm.gridMap.Values)
        {
            if (!IsLiving(target) || target.isAlly == self.isAlly
                || !BattleRangeService.CanTargetCell(SkillType.Slash, self.gridPos, target.gridPos, bm.Cols, bm.Rows)) continue;
            int count = GetSwordWedgeTargets(bm, self, target.gridPos - self.gridPos).Count;
            if (count > bestCount)
            {
                bestCount = count;
                best = target;
                equalCount = 1;
            }
            else if (count == bestCount && Random.Range(0, ++equalCount) == 0) best = target;
        }
        return best;
    }

    public static List<Vector2Int> GetSwordWedgeCells(Vector2Int center, Vector2Int forward) =>
        BattleRangeService.GetSwordWedgeCells(center, forward);

    public static Vector2Int GetSwordAttackDirection(BattleCharacter self, BattleCharacter target) =>
        self == null || target == null ? Vector2Int.zero : BattleRangeService.NormalizeDirection(target.gridPos - self.gridPos);

    public static List<BattleCharacter> GetSwordWedgeTargets(BattleManager bm, BattleCharacter self, Vector2Int forward)
    {
        var result = new List<BattleCharacter>();
        if (bm == null || self == null) return result;
        foreach (Vector2Int cell in GetSwordWedgeCells(self.gridPos, forward))
            if (bm.gridMap.TryGetValue(cell, out BattleCharacter target) && IsLiving(target) && target.isAlly != self.isAlly) result.Add(target);
        return result;
    }

    public static BattleCharacter FindHorseChargeTarget(BattleManager bm, BattleCharacter self, List<BattleCharacter> candidates)
    {
        if (bm == null || self == null || candidates == null) return null;
        var valid = new List<BattleCharacter>();
        foreach (BattleCharacter candidate in candidates)
        {
            if (!IsLiving(candidate) || candidate.isAlly == self.isAlly) continue;
            if (!BattleRangeService.CanTargetCell(SkillType.Spear, self.gridPos, candidate.gridPos, bm.Cols, bm.Rows)) continue;
            Vector2Int first = self.gridPos + BattleRangeService.NormalizeDirection(candidate.gridPos - self.gridPos);
            if (first == candidate.gridPos || (!bm.gridMap.ContainsKey(first) && !bm.IsTerrainBlocked(first))) valid.Add(candidate);
        }
        return FindNearest(self, valid, false);
    }

    private static bool IsLiving(BattleCharacter character) =>
        character != null && !character.isDead && character.currentHP > 0;
}
