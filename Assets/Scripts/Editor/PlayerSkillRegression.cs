using System;
using UnityEngine;

public static class PlayerSkillRegression
{
    public static void Run()
    {
        void Check(bool value,string name){if(!value)throw new Exception("Player skill: "+name);}
        const string key=PlayerSkillRules.SaveKey;
        string previous=PlayerProgressStore.GetString(key);
        try
        {
            PlayerSkillRules.ResetProgress();
            Check(PlayerSkillRules.Load().skillId==0,"unset is no skill");
            Check(PlayerSkillRules.MaxTurns==20,"twenty-turn battle limit");
            Check(PlayerSkillRules.IsValid(new(){skillId=1,turn=20},0),"initial attack and final turn");
            Check(!PlayerSkillRules.IsValid(new(){skillId=2,turn=1},4),"guard gated");
            Check(PlayerSkillRules.IsValid(new(){skillId=2,turn=1},5),"guard unlock");
            Check(!PlayerSkillRules.IsValid(new(){skillId=3,turn=1},9),"heal gated");
            foreach(int turn in new[]{0,21,50,51,-1}) Check(!PlayerSkillRules.IsValid(new(){skillId=1,turn=turn},40),"invalid turn");
            PlayerProgressStore.SetString(key,"{\"skillId\":1,\"turn\":50}");
            Check(PlayerSkillRules.Load().turn==20,"legacy late activation moves to final turn");
            PlayerProgressStore.SetString(key,"{\"skillId\":1,\"turn\":999}");
            Check(!PlayerSkillRules.IsValid(PlayerSkillRules.Load(),40),"corrupt late turn stays invalid");
            Check(!PlayerSkillRules.IsValid(new(){skillId=99,turn=1},40),"unknown skill");
            PlayerSkillRules.Save(new(){skillId=1,turn=3},0);
            Check(PlayerSkillRules.Load().turn==3,"save restore");
            var state=new PlayerSkillRules.BattleState();
            state.Reset(PlayerSkillRules.Load());
            Check(!state.Begin(2,true)&&!state.Begin(3,false),"only scheduled live turn");
            Check(state.Begin(3,true)&&state.AttackPercent==30&&!state.Begin(3,true),"exactly once");
            state.EndTurn();Check(state.Active&&state.Remaining==1,"second turn survives first enemy phase");
            state.EndTurn();Check(!state.Active&&state.AttackPercent==0&&!state.Begin(5,true),"expires without reactivation");
            state.Reset(new(){skillId=2,turn=1});state.Begin(1,true);
            Check(state.ReduceDamage(100)==70&&state.ReduceDamage(1)==1,"guard and minimum damage");
            state.EndTurn();Check(state.ReduceDamage(100)==100,"guard expires after enemy turn");
            state.Reset(new(){skillId=3,turn=1});Check(state.Begin(1,true)&&state.Fired&&!state.Active,"instant has no lingering effect");
            state.Reset(new(){skillId=1,turn=20});state.Finish();Check(!state.Begin(20,true),"no activation after result");
            state.Reset(new(){skillId=1,turn=1});Check(!state.Fired&&state.Begin(1,true),"retry resets once flag");
            PlayerProgressStore.SetString(key,"{broken");Check(!PlayerSkillRules.IsValid(PlayerSkillRules.Load(),40),"corrupt save requires confirmation");
            PlayerSkillRules.ResetProgress();Check(PlayerSkillRules.Load().skillId==0,"reset clears plan");
            Debug.Log("[PlayerSkillRegression] Passed.");
        }
        finally
        {
            if(string.IsNullOrEmpty(previous))PlayerProgressStore.Delete(key);else PlayerProgressStore.SetString(key,previous);
            PlayerProgressStore.Save();
        }
    }
    public static void BuildLocalReview()
    {
        BattleRuleRegression.RunFromCommandLine();GameDataValidator.ValidateFromCommandLine();
        KanjiBattle.Editor.WebGLBuild.BuildLocalE2E();
    }
}
