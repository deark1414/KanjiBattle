using System;
using System.Collections.Generic;
using UnityEngine;

public static class FusionSupportRegression
{
    public static void Run()
    {
        var definitions = new List<CharacterData>();
        var host = new GameObject("FusionSupportRegression", typeof(RectTransform));
        host.SetActive(false);
        try
        {
            CharacterData Make(string name, int id)
            {
                var data = ScriptableObject.CreateInstance<CharacterData>();
                data.characterName = name; data.characterId = id;
                definitions.Add(data); return data;
            }
            var sword = Make("剣", 10); var shield = Make("盾", 13);
            var spear = Make("槍", 11); var fire = Make("火", 19); var hammer = Make("槌", 12);
            var sharedShield = BattleFusionRules.ResolveAdjacent(new[] { sword, shield, spear },
                new[] { new Vector2Int(0, 5), new(1, 5), new(2, 5) }, null);
            Check(sharedShield.conflicts.Count == 2 && sharedShield.plan.groups.Count == 0,
                "shared shield requires explicit warrior or sentinel choice");
            foreach (var item in new[] {
                (id:-1003, materials:new[] { sword, shield }, hp:400, attack:24),
                (id:-1004, materials:new[] { spear, shield }, hp:400, attack:24),
                (id:-1005, materials:new[] { fire, hammer }, hp:300, attack:24) })
            {
                var recipe = BattleFusionRules.GetRecipe(item.id);
                var fusion = CharacterData.CreateBattleFusion(recipe, item.materials, 1);
                definitions.Add(fusion);
                Check(fusion.GetMaxHP(1) == item.hp && fusion.GetAttack(1) == item.attack,
                    "role-specific stat profile " + item.id);
                Check(BattleFusionRules.Candidates(item.materials).Count == 1, "recipe materials " + item.id);
            }
            var state = new BattleCharacterState(); state.Initialize(sword, 1, false);
            state.Attack = 100;
            state.ApplyAttackBuff(BattleAttackBuff.Warrior, 20, 2);
            state.ApplyAttackBuff(BattleAttackBuff.Warrior, 20, 2);
            Check(state.GetEffectiveAttack() == 120, "same buff does not stack");
            state.ApplyAttackBuff(BattleAttackBuff.Forge, 25, 3);
            Check(state.GetEffectiveAttack() == 145, "different named sources add");
            state.SetNumberPassiveAttackBonus(10);
            Check(state.GetEffectiveAttack() == 155, "number bonus adds without multiplying support twice");
            state.SetNumberPassiveAttackBonus(0);
            state.TickAttackBuffs(); state.TickAttackBuffs();
            Check(state.GetEffectiveAttack() == 125, "warrior expires after two actions");
            state.TickAttackBuffs(); Check(state.GetEffectiveAttack() == 100, "forge expires after three");
            state.ApplyAttackBuff(BattleAttackBuff.Forge, 25, 3); state.TickAttackBuffs();
            state.ApplyAttackBuff(BattleAttackBuff.Forge, 25, 3);
            state.TickAttackBuffs(); state.TickAttackBuffs();
            Check(state.GetEffectiveAttack() == 125, "refresh restores duration without stacking");
            state.Initialize(sword, 1, false);
            Check(state.AttackBuffPercent == 0, "retry initialization clears buffs");
            Check(FusionSupportExecutor.IsSupportRound(1) && FusionSupportExecutor.IsSupportRound(4)
                && !FusionSupportExecutor.IsSupportRound(2), "three-turn cadence");
            Check(FusionSupportExecutor.IsForgeTarget(sword) && FusionSupportExecutor.IsForgeTarget(shield)
                && !FusionSupportExecutor.IsForgeTarget(fire), "explicit base weapon whitelist");
            Check(FusionSupportExecutor.IsForgeTarget(definitions[5]) && !FusionSupportExecutor.IsForgeTarget(definitions[7]),
                "weapon fusion allowed but forge itself excluded");
            var unit = host.AddComponent<BattleCharacter>();
            unit.Init(definitions[5], new(2, 3), true);
            int hp = unit.currentHP;
            unit.TakeDamage(100, null, ignoreDefense: true);
            Check(hp - unit.currentHP == 80, "warrior shield mitigation");
            unit.currentHP = hp;
            unit.TakeDamage(100, null, origin: BattleDamageOrigin.Transferred);
            Check(hp - unit.currentHP == 100, "transferred hit bypasses shield");
            Check(BattleRangeService.CanTargetCell(SkillType.Sentinel, new(2, 3), new(2, 1), 6, 7)
                && !BattleRangeService.CanTargetCell(SkillType.Sentinel, new(2, 3), new(2, 1), 6, 7,
                    new HashSet<Vector2Int> { new(2, 2) }), "sentinel spear geometry and friendly block");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
            foreach (CharacterData data in definitions) UnityEngine.Object.DestroyImmediate(data);
        }
    }

    private static void Check(bool value, string label)
    {
        if (!value) throw new InvalidOperationException("[FusionSupportRegression] " + label);
    }
}
