#if KANJI_BATTLE_E2E
using System;
using System.Linq;
using UnityEngine;

public partial class BattleManager
{
    internal void SeedPlayerSkillLethalTraps()
    {
        if(!IsDeploying)throw new InvalidOperationException("Deployment required.");
        foreach(var unit in allies){unit.currentHP=1;trapCells.Add(unit.gridPos);}
        trapDamage=int.MaxValue;
    }
    internal void ProbePlayerSkill(string mode)
    {
        if(!IsDeploying || !PlayerSkillReady)throw new InvalidOperationException("Valid player skill deployment required.");
        void Check(bool value,string label){if(!value)throw new Exception("Player skill probe: "+label);}
        int turn=playerSkillPlan.turn;
        var target=allies[0];var enemy=enemies[0];
        int baseline=target.GetEffectiveAttack(this), enemyAttack=enemy.GetEffectiveAttack(this);
        Check(!playerSkill.Fired,"not fired during setup");
        if(mode=="heal")target.currentHP=target.maxHP-3;
        BeginPlayerSkillTurn(turn-1);Check(!playerSkill.Fired,"not before scheduled turn");
        BeginPlayerSkillTurn(turn);Check(playerSkill.Fired,"fires at scheduled turn");
        if(mode=="attack")
        {
            Check(target.GetEffectiveAttack(this)==Mathf.RoundToInt(baseline*1.3f),"ally attack boosted");
            Check(enemy.GetEffectiveAttack(this)==enemyAttack,"enemy unchanged");
            EndPlayerSkillTurn();Check(playerSkill.Active&&playerSkill.Remaining==1,"second turn remains");
            EndPlayerSkillTurn();Check(!playerSkill.Active&&target.GetEffectiveAttack(this)==baseline,"expires after second enemy phase");
        }
        else if(mode=="guard")
        {
            target.defense=0;int before=target.currentHP;
            target.TakeDamage(100,this,enemy,ignoreDefense:true);
            Check(before-target.currentHP==70,"guard reduces actual damage");
            var command=battleRecords.Entries.Single(row=>row.token==PlayerSkillRecordToken);
            Check(command.result.prevented==180,"commander owns prevented damage");
            Check(battleRecords.Entries.Single(row=>row.token==target.RecordToken).result.prevented==0,"not duplicated as character prevention");
            before=target.currentHP;target.TakeDamage(40,this,enemy,ignoreDefense:true,origin:BattleDamageOrigin.Transferred);
            Check(before-target.currentHP==40,"no second mitigation of transferred damage");
            EndPlayerSkillTurn();before=target.currentHP;target.TakeDamage(100,this,enemy,ignoreDefense:true);
            Check(before-target.currentHP==100,"guard expires");
        }
        else if(mode=="heal")
        {
            Check(target.currentHP==target.maxHP&&!playerSkill.Active,"instant heal completes");
            Check(battleRecords.Entries.Single(row=>row.token==PlayerSkillRecordToken).result.healed==18,"actual healing only");
            Check(battleRecords.MaterialTotals().All(row=>row.healed==0),"not assigned to character trials");
            target.currentHP-=2;BeginPlayerSkillTurn(turn);
            Check(target.currentHP==target.maxHP-2,"cannot heal twice");
        }
        else throw new ArgumentException("Unknown player skill probe.");
        FinishPlayerSkill();Check(!playerSkill.Active,"result clears duration");
        BeginPlayerSkillTurn(turn);Check(!playerSkill.Active,"no activation after result");
        AddLog("プレイヤースキル能力チェック完了");
    }
}
#endif
