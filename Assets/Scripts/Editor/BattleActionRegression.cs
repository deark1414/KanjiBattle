using System;
using System.Collections.Generic;
using UnityEngine;

public static class BattleActionRegression
{
    public static void Run()
    {
        VerifyRoleRanges();
        VerifyMovementPlanning();
        VerifyTargetSelection();
    }

    private static void VerifyRoleRanges()
    {
        Vector2Int origin = new(2, 5);
        var friends = new HashSet<Vector2Int> { new(2, 4) };
        Check(BattleRangeService.CanTargetCell(SkillType.Arrow, origin, new(2, 0), 6, 7, friends), "arrow crosses allies");
        Check(BattleRangeService.CanTargetCell(SkillType.Gun, origin, new(2, 0), 6, 7, friends), "gun crosses allies");
        Check(!BattleRangeService.CanTargetCell(SkillType.Spear, origin, new(2, 3), 6, 7, friends), "spear cannot cross allies");
        Check(BattleRangeService.CanTargetCell(SkillType.Spear, origin, new(2, 3), 6, 7), "spear can cross empty or water cells");
        Check(!BattleRangeService.CanTargetCell(SkillType.Gun, origin, new(3, 2), 6, 7), "gun requires an eight-direction ray");
        Check(BattleRangeService.CanTargetCell(SkillType.Fireball, origin, new(4, 3), 6, 7, friends), "fireball two-cell range");
        Check(!BattleRangeService.CanTargetCell(SkillType.Fireball, origin, new(2, 2), 6, 7), "fireball cannot hit outside its preview range");
        Check(!BattleRangeService.CanTargetCell(SkillType.Stone, origin, new(6, 5), 6, 7), "out-of-board target rejected");
        Check(BattleRangeService.Distance(new(0, 0), new(2, 2)) == 2, "diagonal distance counts one step");

        var data = ScriptableObject.CreateInstance<CharacterData>();
        try
        {
            data.skillChance = 0;
            foreach (SkillType skill in new[] { SkillType.Arrow, SkillType.Gun, SkillType.Stone, SkillType.Fireball, SkillType.Spear, SkillType.Slash, SkillType.WaterHeal })
                Check(SkillExecutor.GetSkillChance(data, skill) == 100, "role action must not fail a chance roll");
            Check(SkillExecutor.GetSkillChance(data, SkillType.StunBlow) == 0, "extra effect chance preserved");
            Check(!SkillExecutor.RollSkillActivation(0) && SkillExecutor.RollSkillActivation(100), "chance endpoints");
        }
        finally { UnityEngine.Object.DestroyImmediate(data); }
    }

    private static void VerifyMovementPlanning()
    {
        var start = new Vector2Int(0, 4);
        var target = new Vector2Int(0, 0);
        var targets = new List<Vector2Int> { target };
        var friends = new HashSet<Vector2Int> { new(0, 3) };
        var enemies = new HashSet<Vector2Int> { target };
        var water = new HashSet<Vector2Int>();
        Check(BattleMovementPlanner.FindNextMove(start, targets, SkillType.Arrow, CharacterCategory.Ranged,
            friends, enemies, water, 1, 5) == null, "shooter does not approach a valid target");
        List<Vector2Int> animal = BattleMovementPlanner.FindNextMove(start, targets, SkillType.None, CharacterCategory.Animal,
            friends, enemies, water, 1, 5);
        Check(animal != null && animal.Count == 3 && animal[1] == new Vector2Int(0, 3) && animal[2] == new Vector2Int(0, 2),
            "animal traverses ally without ending on ally");
        Check(BattleMovementPlanner.FindNextMove(start, targets, SkillType.None, CharacterCategory.Weapon,
            friends, enemies, water, 1, 5) == null, "one-step unit cannot stop on an ally");

        start = new Vector2Int(0, 5);
        target = new Vector2Int(3, 0);
        targets[0] = target;
        enemies = new HashSet<Vector2Int> { target };
        friends.Clear();
        for (int turn = 0; turn < 10 && !BattleRangeService.CanTargetCell(SkillType.Arrow, start, target, 6, 7); turn++)
        {
            List<Vector2Int> move = BattleMovementPlanner.FindNextMove(start, targets, SkillType.Arrow, CharacterCategory.Ranged,
                friends, enemies, water, 6, 7);
            Check(move != null && move.Count == 2, "shooter takes one legal step toward a firing cell");
            start = move[move.Count - 1];
            Check(start != target, "movement does not enter enemy cell");
        }
        Check(BattleRangeService.CanTargetCell(SkillType.Arrow, start, target, 6, 7), "shooter reaches a firing cell");

        start = new Vector2Int(0, 3);
        targets[0] = new Vector2Int(0, 0);
        enemies = new HashSet<Vector2Int>(targets);
        water.Add(new Vector2Int(0, 2));
        List<Vector2Int> detour = BattleMovementPlanner.FindNextMove(start, targets, SkillType.None, CharacterCategory.Weapon,
            friends, enemies, water, 3, 4);
        Check(detour != null && !water.Contains(detour[detour.Count - 1]), "movement routes around water");
    }

    private static void VerifyTargetSelection()
    {
        var host = new GameObject("ActionRegression", typeof(RectTransform));
        host.SetActive(false);
        var data = new List<CharacterData>();
        UnityEngine.Random.State randomState = UnityEngine.Random.state;
        try
        {
            var bm = host.AddComponent<BattleManager>();
            BattleCharacter Create(Vector2Int position, bool ally, int hp = 100, int maxHP = 100)
            {
                var definition = ScriptableObject.CreateInstance<CharacterData>();
                var serialized = new UnityEditor.SerializedObject(definition);
                serialized.FindProperty("baseHP").intValue = maxHP;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                definition.skillChance = 0;
                definition.skillType = SkillType.Arrow;
                definition.characterName = "test";
                data.Add(definition);
                var go = new GameObject("Character", typeof(RectTransform));
                go.transform.SetParent(host.transform, false);
                var character = go.AddComponent<BattleCharacter>();
                character.Init(definition, position, ally);
                character.currentHP = hp;
                bm.gridMap[position] = character;
                return character;
            }

            BattleCharacter self = Create(new(2, 4), true);
            BattleCharacter friendly = Create(new(2, 3), true);
            BattleCharacter enemy = Create(new(2, 2), false);
            Check(TargetingService.FindArrowTarget(bm, self) == enemy, "arrow targets through friendly unit");
            Check(TargetingService.FindGunTarget(bm, self) == enemy, "gun targets through friendly unit");
            Check(TargetingService.FindSpearTarget(bm, self) == null, "spear target blocked by friendly unit");
            Check(SkillExecutor.TryExecuteDetailed(self, friendly, bm).Failure == SkillExecutionFailure.NoValidTarget,
                "reliable attack rejects friendly fire");
            self.data.skillType = SkillType.Spear;
            Check(SkillExecutor.TryExecuteDetailed(self, enemy, bm).Failure == SkillExecutionFailure.NoValidTarget,
                "skill execution also respects spear blocker");

            friendly.currentHP = 60;
            BattleCharacter injured = Create(new(3, 4), true, 100, 200);
            Check(TargetingService.FindAdjacentAlly(bm, self) == injured, "healing uses HP percentage rather than absolute HP");
            self.currentHP = 40;
            Check(TargetingService.FindAdjacentAlly(bm, self) == self, "healer can heal itself");
            self.currentHP = self.maxHP;
            friendly.currentHP = friendly.maxHP;
            injured.currentHP = injured.maxHP;
            Check(TargetingService.FindAdjacentAlly(bm, self) == null, "full-health allies do not consume a healing action");
            enemy.isDead = true;
            Check(TargetingService.FindArrowTarget(bm, self) == null, "dead enemies cannot be targeted");
        }
        finally
        {
            UnityEngine.Random.state = randomState;
            UnityEngine.Object.DestroyImmediate(host);
            foreach (CharacterData definition in data) UnityEngine.Object.DestroyImmediate(definition);
        }
    }

    private static void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException("[BattleActionRegression] " + label);
    }
}
