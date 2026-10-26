using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CharacterInvestmentRegression
{
    public static void ValidateAndBuild()
    {
        BattleRuleRegression.RunFromCommandLine();GameDataValidator.ValidateFromCommandLine();
        KanjiBattle.Editor.WebGLBuild.BuildLocalE2E();
    }
    public static void Run()
    {
        const string key="KanjiBattle.CharacterInvestment.Paid.v1";
        string previous=PlayerProgressStore.GetString(key);
        var db=AssetDatabase.LoadAssetAtPath<CharacterDatabase>("Assets/Resources/CharacterDatabase.asset");
        CharacterData fusion=null,copy=null;
        try
        {
            CharacterInvestment.Clear();
            void Check(bool value,string label){if(!value)throw new Exception("Skill investment: "+label);}
            var characters=CastleTree.Nodes.Where(n=>n.kind==CastleTree.Kind.Character).ToArray();
            Check(characters.Length==26&&!CastleTree.Nodes.Any(n=>n.kind==CastleTree.Kind.Upgrade),"one repeated character node, no extra stat nodes");
            Check(CastleTree.Nodes.Count(n=>n.kind==CastleTree.Kind.CategoryUpgrade)==10,"five category branches, two tiers");
            foreach(var node in characters)
            {
                var upgrades=CharacterInvestment.Upgrades(CastleTree.Character(node));
                Check((CastleTree.Character(node).skillType==SkillType.Wall?upgrades.Length==0:upgrades.Length>=2)
                    &&upgrades.All(s=>(int)s>=4),"skill upgrades except fixed wall immunity");
                Check(upgrades.All(s=>CastleTreeArt.Investment(s)!=null),"existing icons");
            }
            Check(!CharacterInvestment.Upgrades(db.GetById(10)).Contains(CharacterInvestment.Stat.Chance),"no chance node for guaranteed roles");
            Check(CharacterInvestment.CurrentSummary(db.GetById(15)).Contains("100%"),"wall immunity is fixed");
            foreach(string id in new[]{"u15_4","u15_5","u15_6","u15_7"})
                Check(CharacterInvestment.IsRetiredUpgrade(id),"refund retired wall effect and chance upgrades");
            Check(!CharacterInvestment.IsRetiredUpgrade("u13_4")&&!CharacterInvestment.IsRetiredUpgrade("k1_4"),
                "other defense investments remain valid");
            var water=db.GetById(20);
            float Recovery()=>CharacterInvestment.WaterRecoveryRatio(water,CharacterInvestment.Effect(water));
            Check(Mathf.Abs(Recovery()-.3f)<.0001f,"water base recovery 30 percent");
            CharacterInvestment.Record("u20_4",4);Check(Mathf.Abs(Recovery()-.4f)<.0001f,"water first tier adds ten points");
            CharacterInvestment.Record("u20_6",8);Check(Mathf.Abs(Recovery()-.5f)<.0001f,"water second tier adds ten points");
            CharacterInvestment.Record("k3_4",10);Check(Mathf.Abs(Recovery()-.55f)<.0001f,"water category adds five points");
            CharacterInvestment.Record("k3_6",18);Check(Mathf.Abs(Recovery()-.6f)<.0001f,"water maximum recovery 60 percent");
            Check(CharacterInvestment.WaterRecoveryRatio(water,2f)==.6f,"water recovery ceiling");
            Check(CharacterInvestment.CurrentSummary(water).Contains("60%"),"water summary matches actual recovery");
            CharacterInvestment.Clear();
            var ox=db.GetById(30);
            Check(ox.skillType==SkillType.ProjectileGuard && CharacterInvestment.CurrentSummary(ox).Contains("50%"),"ox base protection replaces experience");
            CharacterInvestment.Record("u30_4",4);CharacterInvestment.Record("u30_6",8);
            CharacterInvestment.Record("k4_4",10);CharacterInvestment.Record("k4_6",18);
            Check(BattleDamageRules.GuardShare(100,100,ox.skillPower*CharacterInvestment.Effect(ox),true)==65,
                "ox individual and animal upgrades increase sharing to sixty-five percent");
            Check(BattleDamageRules.GuardLoss(50,true)==35 && BattleRewardRules.ArmyExperienceMultiplier(new[]{ox})==1,
                "ox upgrades do not change fixed mitigation or grant experience");
            CharacterInvestment.Clear();
            CharacterInvestment.Record("u10_4",4);CharacterInvestment.Record("u10_6",8);CharacterInvestment.Record("k0_4",10);
            CharacterInvestment.Record("u10_4",999);
            Check(CharacterInvestment.Refund==22,"actual payment once");
            var sword=db.GetById(10);var two=db.GetById(2);
            Check(Mathf.Abs(CharacterInvestment.Effect(sword)-1.25f)<.0001f,"individual and category effect add, not multiply");
            var ally=new BattleCharacterState();var enemy=new BattleCharacterState();ally.Initialize(sword,10,true);enemy.Initialize(sword,10,false);
            Check(ally.MaxHP==enemy.MaxHP&&ally.Attack==enemy.Attack&&ally.Defense==enemy.Defense,"no base stat bonuses");
            Check(SkillExecutor.GetSkillChance(sword,SkillType.Slash,true)==100,"reliable roles remain guaranteed");
            fusion=CharacterData.CreateBattleFusion(BattleFusionRules.GetRecipe(-1001),new[]{two,sword},10);
            Check(Mathf.Abs(CharacterInvestment.Effect(fusion)-1.125f)<.0001f,"fusion inherits mean skill effect once");
            ally.Initialize(fusion,10,true);enemy.Initialize(fusion,10,false);
            Check(ally.MaxHP==enemy.MaxHP&&ally.Attack==enemy.Attack,"fusion base stats unchanged");
            copy=CharacterData.CreateBattleCopy("砂",SkillType.None,100,20,0);
            Check(CharacterInvestment.Effect(copy)==1,"generated copy does not inherit twice");
            var bell=db.GetById(27);int chance=SkillExecutor.GetSkillChance(bell,SkillType.BellEcho);
            CharacterInvestment.Record("u27_5",4);CharacterInvestment.Record("u27_7",8);
            Check(SkillExecutor.GetSkillChance(bell,SkillType.BellEcho,true)==chance+10,"bell chance grows in points");
            Check(SkillExecutor.GetSkillChance(bell,SkillType.BellEcho,false)==chance,"enemy chance unchanged");
            CharacterInvestment.Record("u32_8",4);CharacterInvestment.Record("u32_9",8);
            Check(CharacterInvestment.WindDuration(db.GetById(32))==3,"wind grows duration, not move distance");
            var state=new BattleCharacterState();state.Initialize(db.GetById(32),10,true);
            state.WindBoostTurns=3;Check(state.WindBoost,"wind duration keeps compatibility flag");
            CharacterInvestment.Clear();
            var stages=AssetDatabase.LoadAssetAtPath<StageDatabase>("Assets/ScriptableObjects/Stages/StageDatabase.asset");
            var facilities=AssetDatabase.LoadAssetAtPath<FacilityDatabase>("Assets/ScriptableObjects/Facilities/FacilityDatabase.asset");
            int total=CastleTree.Nodes.Where(n=>n.kind!=CastleTree.Kind.Facility).Sum(n=>n.cost);
            foreach(var character in characters)
                foreach(var stat in CharacterInvestment.Upgrades(CastleTree.Character(character)))
                    total+=CharacterInvestment.IsSecond(stat)?8:stat==CharacterInvestment.Stat.Chance&&CastleTree.Character(character).skillType!=SkillType.BellEcho?6:4;
            foreach(var node in CastleTree.Nodes.Where(n=>n.kind==CastleTree.Kind.Facility))
            {
                var f=facilities.facilities.Single(f=>f.facilityId==node.target);
                total+=f.unlockType==FacilityUnlockType.Free?0:f.unlockStagePointCost;
                for(int i=0;i<f.finalMaxLevel;i++)total+=f.GetUpgradeCost(i);
            }
            int rewards=stages.stages.Sum(s=>s.rewardStagePoints);
            Check(total<=rewards*5,"all unlocks fit five wins per stage");
            Debug.Log($"[CharacterInvestmentRegression] Passed. All unlocks={total}, campaign reward={rewards}, equivalent runs={(float)total/rewards:F2}.");
        }
        finally
        {
            PlayerProgressStore.SetString(key,previous);PlayerProgressStore.Save();
            if(fusion!=null)UnityEngine.Object.DestroyImmediate(fusion);if(copy!=null)UnityEngine.Object.DestroyImmediate(copy);
        }
    }
}
