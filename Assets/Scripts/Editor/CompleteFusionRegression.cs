using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CompleteFusionRegression
{
    public static void Run()
    {
        var definitions = new List<CharacterData>();
        var host = new GameObject("CompleteFusionRegression", typeof(RectTransform)); host.SetActive(false);
        bool force = SkillExecutor.ForceSkillActivationForVfxReview;
        try
        {
            CharacterData Make(string name, int id, SkillType skill = SkillType.None)
            {
                var data = ScriptableObject.CreateInstance<CharacterData>();
                data.characterName = name; data.characterId = id; data.skillType = skill;
                definitions.Add(data); return data;
            }
            Check(BattleFusionRules.Recipes.Count == 17 && BattleFusionRules.Recipes.Select(recipe => recipe.Id).Distinct().Count() == 17,
                "seventeen unique implemented recipes");
            var db = Resources.Load<CharacterDatabase>("CharacterDatabase");
            Check(AdvancedFusionExecutor.DiceBonusPercent(1)==100
                && AdvancedFusionExecutor.DiceBonusPercent(2)==125
                && AdvancedFusionExecutor.DiceBonusPercent(9)==300
                && AdvancedFusionExecutor.DiceBonusPercent(20,2)==300, "dice grows from twice to four times without exponential stacking");
            Check(AdvancedFusionExecutor.MagnetDestination(new(2,4),new(2,2),SkillType.Slash)==new Vector2Int(2,3)
                && AdvancedFusionExecutor.MagnetDestination(new(2,4),new(4,2),SkillType.Arrow)==new Vector2Int(5,1),
                "magnet pulls melee and pushes ranged away using grid direction");
            Check(AdvancedFusionExecutor.IsMagnetRear(SkillType.Rain)
                && !AdvancedFusionExecutor.IsMagnetRear(SkillType.Fortress), "support and frontline magnet roles are distinct");
            var materials = BattleFusionRules.Recipes.SelectMany(recipe => recipe.Materials).ToHashSet();
            Check(db.characters.Where(data => !data.isBoss && data.category != CharacterCategory.Animal)
                .All(data => materials.Contains(data.characterName)), "all twenty-two normal material types covered");
            var party = new[] { Make("一",1), Make("二",2), Make("三",3) };
            var positions = new[] { new Vector2Int(0,5), new(1,5), new(1,6) };
            var dice = BattleFusionRules.ResolveAdjacent(party, positions, null);
            Check(dice.plan.groups.Count == 1 && dice.plan.groups[0].members.Length == 3, "connected three-part fusion");
            positions[2] = new(4,6);
            Check(BattleFusionRules.ResolveAdjacent(party,positions,null).plan.groups.Count == 0, "separated third piece rejected");
            foreach (var recipe in BattleFusionRules.Recipes)
            {
                var inputs = recipe.Materials.Select((name,index) => Make(name,100+index)).ToArray();
                var data = CharacterData.CreateBattleFusion(recipe,inputs,1); definitions.Add(data);
                Check(data.GetMaxHP(1) > 0 && data.skillType == recipe.Skill && data.characterId == recipe.Id, "factory " + recipe.Name);
            }
            BattleCharacter Unit(SkillType skill, bool ally)
            {
                var go = new GameObject("Unit",typeof(RectTransform)); go.transform.SetParent(host.transform,false);
                var unit = go.AddComponent<BattleCharacter>(); unit.Init(Make("試",900+definitions.Count,skill),new(2,3),ally);
                return unit;
            }
            var coal = Unit(SkillType.Coal,true); var enemy = Unit(SkillType.None,false);
            coal.TakeDamage(40,null,enemy,ignoreDefense:true);
            Check(coal.currentHP == 80 && coal.CoalAwake, "coal first hit halves and ignites");
            coal.TakeDamage(20,null,enemy,ignoreDefense:true);
            Check(coal.currentHP == 60 && coal.GetEffectiveAttack(null) == 28, "coal loses reduction and gains forty percent");
            var mountain = Unit(SkillType.Mountain,true);
            enemy.data.skillType = SkillType.Shield; mountain.currentHP = 5;
            int enemyHP = enemy.currentHP;
            SkillExecutor.ForceSkillActivationForVfxReview = true;
            mountain.TakeDamage(100,null,enemy,ignoreDefense:true);
            Check(mountain.isDead && enemy.currentHP == enemyHP - 5, "fatal actual loss returns once without mitigation");
            var protectedUnit = Unit(SkillType.None,true); protectedUnit.CocoonShield = true;
            protectedUnit.TakeDamage(20,null,enemy,ignoreDefense:true);
            Check(protectedUnit.currentHP == 100 && !protectedUnit.CocoonShield, "one cocoon absorbs one damage segment");
            protectedUnit.TakeDamage(20,null,enemy,ignoreDefense:true);
            Check(protectedUnit.currentHP == 80, "next segment is unprotected");
            protectedUnit.CocoonShield=true;
            protectedUnit.TakeDamage(20,null,enemy,ignoreDefense:true,origin:BattleDamageOrigin.Periodic);
            Check(protectedUnit.currentHP==60&&protectedUnit.CocoonShield,"periodic rain does not trigger direct-hit protection");
            var battle = host.AddComponent<BattleManager>(); battle.gridMap[protectedUnit.gridPos] = protectedUnit;
            protectedUnit.DiceDefensePercent = 20; protectedUnit.ApplyAttackBuff(BattleAttackBuff.Dice,20,1);
            FusionSupportExecutor.EndSideTurn(battle,true);
            Check(protectedUnit.DiceDefensePercent == 20 && protectedUnit.AttackBuffPercent == 0,
                "dice defense must survive friendly actions to cover enemy attacks");
            Check(FusionReactionRules.IsProjectile(SkillType.Gun) && !FusionReactionRules.IsProjectile(SkillType.ChainLightning)
                && !FusionReactionRules.IsProjectile(SkillType.Spear), "explicit projectile classification");
        }
        finally
        {
            SkillExecutor.ForceSkillActivationForVfxReview = force;
            UnityEngine.Object.DestroyImmediate(host);
            foreach (var data in definitions) UnityEngine.Object.DestroyImmediate(data);
        }
    }

    private static void Check(bool value,string label)
    { if (!value) throw new InvalidOperationException("[CompleteFusionRegression] " + label); }
}
