using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class CharacterInvestmentRegression
{
    public static void ValidateAndBuild()
    {
        BattleRuleRegression.RunFromCommandLine();
        GameDataValidator.ValidateFromCommandLine();
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
            var sword=db.GetById(10);var two=db.GetById(2);const int level=10;
            void Check(bool value,string label){if(!value)throw new Exception("Character investment: "+label);}
            var nodes=CastleTree.Nodes.Where(n=>n.kind==CastleTree.Kind.Upgrade).ToArray();
            Check(nodes.Length==78,"three choices for every normal character");
            Check(nodes.All(n=>CastleTreeArt.Investment(n.stat)!=null),"reuse existing building art for investment nodes");
            Check(!CastleTree.Nodes.Any(n=>n.kind==CastleTree.Kind.Facility&&n.target>=8&&n.target<=11),"no global combat facilities");
            CharacterInvestment.Record("u10_0",4);CharacterInvestment.Record("u10_1",6);CharacterInvestment.Record("u10_2",8);
            CharacterInvestment.Record("u10_0",999);
            Check(CharacterInvestment.Refund==18,"persisted actual payment, no duplicates");
            Check(CharacterInvestment.Multiplier(two,CharacterInvestment.Stat.Health)==1,"other characters unchanged");
            var ally=new BattleCharacterState();var enemy=new BattleCharacterState();
            ally.Initialize(sword,level,true);enemy.Initialize(sword,level,false);
            Check(ally.MaxHP==Mathf.RoundToInt(sword.GetMaxHP(level)*1.2f),"ally HP");
            Check(ally.Attack==Mathf.RoundToInt(sword.GetAttack(level)*1.15f),"ally attack");
            Check(enemy.MaxHP==sword.GetMaxHP(level)&&enemy.Attack==sword.GetAttack(level),"enemy unaffected");
            fusion=CharacterData.CreateBattleFusion(BattleFusionRules.GetRecipe(-1001),new[]{two,sword},level);
            float expected=(two.GetMaxHP(level)+sword.GetMaxHP(level)*1.2f)/(two.GetMaxHP(level)+(float)sword.GetMaxHP(level));
            Check(Mathf.Abs(CharacterInvestment.Multiplier(fusion,CharacterInvestment.Stat.Health)-expected)<.0001f,"fusion weighted inheritance");
            ally.Initialize(fusion,level,true);enemy.Initialize(fusion,level,false);
            Check(ally.MaxHP==Mathf.RoundToInt(fusion.GetMaxHP(level)*expected)&&enemy.MaxHP==fusion.GetMaxHP(level),"fusion applies exactly once");
            copy=CharacterData.CreateBattleCopy("砂",SkillType.None,ally.MaxHP/2,ally.Attack/2,0);
            Check(CharacterInvestment.Multiplier(copy,CharacterInvestment.Stat.Health)==1,"generated copies do not double boost");
            Check(SkillExecutor.GetSkillChance(sword,SkillType.Slash,true)==100,"reliable role skills remain reliable");
            Check(CharacterInvestment.Specialty(db.GetById(1))==CharacterInvestment.Stat.Defense,"fixed passives receive useful defense nodes");
            CharacterInvestment.Record("u1_3",8);var one=db.GetById(1);
            ally.Initialize(one,level,true);enemy.Initialize(one,level,false);
            Check(ally.Defense==one.GetDefense(level)+4&&enemy.Defense==one.GetDefense(level),"defense node ally only");
            var bell=db.GetById(27);int basicChance=SkillExecutor.GetSkillChance(bell,SkillType.BellEcho);
            CharacterInvestment.Record("u27_2",8);
            Check(SkillExecutor.GetSkillChance(bell,SkillType.BellEcho,true)==Mathf.Clamp(Mathf.RoundToInt(basicChance*1.15f),0,95),"technique chance relative boost");
            CharacterInvestment.Clear();Check(CharacterInvestment.Refund==0,"clearing consumes refund ledger");
            var stages=AssetDatabase.LoadAssetAtPath<StageDatabase>("Assets/ScriptableObjects/Stages/StageDatabase.asset");
            var facilities=AssetDatabase.LoadAssetAtPath<FacilityDatabase>("Assets/ScriptableObjects/Facilities/FacilityDatabase.asset");
            int total=CastleTree.Nodes.Where(n=>n.kind!=CastleTree.Kind.Facility).Sum(n=>n.cost);
            foreach(var node in CastleTree.Nodes.Where(n=>n.kind==CastleTree.Kind.Facility))
            {
                var f=facilities.facilities.Single(f=>f.facilityId==node.target);
                total+=f.unlockType==FacilityUnlockType.Free?0:f.unlockStagePointCost;
                for(int i=0;i<f.finalMaxLevel;i++)total+=f.GetUpgradeCost(i);
            }
            int rewards=stages.stages.Sum(s=>s.rewardStagePoints);
            Check(total<=rewards*5,"all unlocks fit five wins per stage without income boosts");
            Debug.Log($"[CharacterInvestmentRegression] Passed. All unlocks={total}, campaign reward={rewards}, equivalent runs={(float)total/rewards:F2}.");
        }
        finally
        {
            PlayerProgressStore.SetString(key,previous);PlayerProgressStore.Save();
            if(fusion!=null)UnityEngine.Object.DestroyImmediate(fusion);
            if(copy!=null)UnityEngine.Object.DestroyImmediate(copy);
        }
    }
}
