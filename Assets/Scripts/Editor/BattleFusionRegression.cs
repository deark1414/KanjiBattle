using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BattleFusionRegression
{
    public static void Run()
    {
        var definitions = new List<CharacterData>();
        try
        {
            CharacterData Make(string name, int id)
            {
                var data = ScriptableObject.CreateInstance<CharacterData>();
                data.characterName = name; data.characterId = id;
                definitions.Add(data); return data;
            }
            var two = Make("二", 2);
            var sword = Make("剣", 10);
            var three = Make("三", 3);
            var gun = Make("銃", 12);
            var evolved = Make("刀", 110);
            CharacterData[] party = { two, sword, two, sword, three, gun };
            var candidates = BattleFusionRules.Candidates(party);
            Check(candidates.Count == 5, "duplicate instances have separate candidates");
            Check(BattleFusionRules.Candidates(new[] { two, evolved }).Count == 0, "evolution cannot substitute for basic material");
            VerifyAdjacent(party);
            var plan = BattleFusionRules.Empty(party);
            plan.groups.Add(new BattleFusionRules.Group { recipeId = -1001, members = new[] { 0, 3 }, origin = 3 });
            plan.groups.Add(new BattleFusionRules.Group { recipeId = -1001, members = new[] { 2, 1 }, origin = 2 });
            plan.groups.Add(new BattleFusionRules.Group { recipeId = -1002, members = new[] { 4, 5 }, origin = 4 });
            Check(BattleFusionRules.IsValid(party, plan), "independent groups have valid material membership before layout validation");
            Check(party.Length - plan.groups.Sum(group => group.members.Length - 1) == 3, "six paid formation slots become three battlefield units");
            var restored = JsonUtility.FromJson<BattleFusionRules.Plan>(JsonUtility.ToJson(plan));
            Check(BattleFusionRules.IsValid(party, restored) && restored.groups[0].origin == 3, "plan JSON preserves exact members and origin");
            Check(!BattleFusionRules.IsValid(new[] { sword, two, two, sword, three, gun }, restored), "changed party does not silently remap materials");
            plan.groups[1].members[0] = 0;
            Check(!BattleFusionRules.IsValid(party, plan), "one material cannot be consumed twice");
            plan.groups[1].members[0] = 2;
            plan.groups[0].origin = 5;
            Check(!BattleFusionRules.IsValid(party, plan), "origin must be one of the materials");
            plan.groups[0].origin = 3;
            plan.groups[0].members[0] = -1;
            Check(!BattleFusionRules.IsValid(party, plan), "corrupt material index rejected");
            plan.groups[0].members[0] = 0;
            plan.groups[0].recipeId = -9999;
            Check(!BattleFusionRules.IsValid(party, plan), "unknown recipes rejected");

            var serialized = new SerializedObject(sword);
            serialized.FindProperty("baseHP").intValue = 200;
            serialized.FindProperty("baseAttack").intValue = 40;
            serialized.FindProperty("baseDefense").intValue = 20;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var fusion = CharacterData.CreateBattleFusion(BattleFusionRules.GetRecipe(-1001), new[] { two, sword }, 11);
            definitions.Add(fusion);
            Check(fusion.GetMaxHP(11) == 875 && fusion.GetAttack(11) == 75 && fusion.GetDefense(11) == 24,
                "same-level mean stats apply fusion multipliers before facilities");
            Check(two.GetMaxHP(11) == 200 && sword.GetMaxHP(11) == 300, "shared material assets are unchanged");
            Check(fusion.characterId < 0 && fusion != two && fusion != sword
                && fusion.hideFlags == HideFlags.HideAndDontSave && fusion.skillType == SkillType.DoubleSlash,
                "temporary result is separate from owned characters");
            Check(fusion.icon != null && fusion.enemyIcon != null, "fusion uses production piece art for both sides");
            var state = new BattleCharacterState(); state.Initialize(fusion, 11, false);
            Check(state.MaxHP == 875 && state.CurrentHP == 875 && state.Level == 11, "fusion initializes at full HP and shared level");

            var from = new Vector2Int(2, 5); var target = new Vector2Int(2, 2);
            var friends = new HashSet<Vector2Int> { new(2, 4) };
            Check(BattleRangeService.CanTargetCell(SkillType.ScatterShot, from, target, 6, 7, friends), "scatter crosses friends at range three");
            Check(!BattleRangeService.CanTargetCell(SkillType.ScatterShot, from, new(2, 1), 6, 7), "scatter range is limited");
            Check(BattleRangeService.GetEffectCells(SkillType.ScatterShot, from, target, 6, 7)
                .SequenceEqual(new[] { new Vector2Int(2, 2), new Vector2Int(3, 2), new Vector2Int(1, 2) }), "scatter has three target-side cells");
            Check(!FusionSkillExecutor.UsesFocusedShot(0) && FusionSkillExecutor.UsesFocusedShot(1)
                && FusionSkillExecutor.UsesFocusedShot(2) && !FusionSkillExecutor.UsesFocusedShot(3),
                "scatter automatically concentrates below three living enemies");
            Check(BattleRangeService.GetEffectCells(SkillType.DoubleSlash, from, new(2, 4), 6, 7)
                .SequenceEqual(BattleRangeService.GetEffectCells(SkillType.Slash, from, new(2, 4), 6, 7)), "double slash shares sword geometry");
            Check(SkillExecutor.GetSkillChance(two, SkillType.DoubleSlash) == 100
                && SkillExecutor.GetSkillChance(two, SkillType.ScatterShot) == 100, "fusion role attacks cannot chance-fail");
            Debug.Log("[BattleFusionRegression] Passed.");
        }
        finally { foreach (CharacterData data in definitions) UnityEngine.Object.DestroyImmediate(data); }
    }
    private static void Check(bool condition, string label)
    { if (!condition) throw new InvalidOperationException("[BattleFusionRegression] " + label); }

    private static void VerifyAdjacent(CharacterData[] party)
    {
        Vector2Int[] positions = { new(0,5), new(2,5), new(4,5), new(1,6), new(3,6), new(5,6) };
        var choices = BattleFusionRules.Empty(party);
        var result = BattleFusionRules.ResolveAdjacent(party, positions, choices);
        Check(result.candidates.Count == 0 && result.plan.groups.Count == 0, "separation and diagonal contact do not fuse");
        positions[1] = new(1,5);
        result = BattleFusionRules.ResolveAdjacent(party, positions, choices);
        Check(result.plan.groups.Count == 1 && result.conflicts.Count == 0, "one orthogonal pair fuses automatically");
        Check(result.plan.groups[0].origin == 0, "automatic origin is the front-left material");
        positions[5] = new(4,6);
        result = BattleFusionRules.ResolveAdjacent(party, positions, choices);
        Check(result.plan.groups.Count == 2 && result.alternatives.Count == 0, "independent adjacent pairs need no selection");
        positions[2] = new(2,5); positions[3] = new(3,5);
        result = BattleFusionRules.ResolveAdjacent(party, positions, choices);
        Check(result.conflicts.Count == 3 && result.plan.groups.Count == 1, "shared material ambiguity is never silently resolved");
        choices.groups.Add(new BattleFusionRules.Group { recipeId = -1001, members = new[] { 0,1 }, origin = 1 });
        result = BattleFusionRules.ResolveAdjacent(party, positions, choices);
        Check(result.conflicts.Count == 0 && result.plan.groups.Count == 3, "explicit choice also resolves remaining independent pairs");
        Check(result.plan.groups.Any(group => group.recipeId == -1001 && group.origin == 1), "explicit source cell survives conflict resolution");
        positions[1] = new(5,5);
        result = BattleFusionRules.ResolveAdjacent(party, positions, choices);
        Check(!result.plan.groups.Any(group => group.members.Contains(0)), "saved choice cannot fuse separated materials");
        var corrupt = BattleFusionRules.Empty(party);
        corrupt.groups.Add(new BattleFusionRules.Group { recipeId = -1001, members = new[] { 0,0 }, origin = 0 });
        result = BattleFusionRules.ResolveAdjacent(party, positions, corrupt);
        Check(BattleFusionRules.IsValid(party, result.plan), "corrupt preferences do not create overlapping results");
        bool rejected = false;
        try { BattleFusionRules.ResolveAdjacent(party, new[] { Vector2Int.zero }, null); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected, "mismatched layout rejected");
    }
}
