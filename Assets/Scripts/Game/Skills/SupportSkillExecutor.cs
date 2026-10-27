using System;
using System.Linq;
using UnityEngine;

public static class SupportSkillExecutor
{
    public static bool CanCopy(BattleCharacter caster, BattleCharacter target) =>
        caster != null && target != null && caster != target && !caster.HasTransformed
        && !caster.isDead && !target.isDead && caster.currentHP > 0 && target.currentHP > 0
        && caster.data != null && target.data != null && caster.isAlly != target.isAlly
        && BattleRangeService.Distance(caster.gridPos, target.gridPos) == 1
        && !target.data.isBoss && !target.data.isEvolved && target.data.characterId >= 0;

    public static BattleCharacter FindCopyTarget(BattleCharacter caster, BattleManager battle) =>
        battle.gridMap.Values.Where(target => CanCopy(caster, target)).OrderByDescending(target => target.attack).FirstOrDefault();

    public static bool CanEcho(BattleCharacter bell, BattleCharacter caster) =>
        bell != null && caster != null && bell != caster && !bell.isDead && !caster.isDead
        && bell.currentHP > 0 && caster.currentHP > 0 && bell.data != null && caster.data != null
        && bell.isAlly == caster.isAlly
        && (bell.data.skillType == SkillType.WindChime || (bell.data.skillType == SkillType.BellEcho
            && BattleRangeService.Distance(bell.gridPos, caster.gridPos) == 1));

    public static void TryEcho(BattleCharacter caster, BattleCharacter target, BattleManager battle, Func<BattleCharacter, bool> repeat)
    {
        if (battle == null || caster == null || caster.isDead || target == null || target.isDead || target.currentHP <= 0) return;
        SkillType skill = caster.data.skillType;
        if (skill is not (SkillType.Soil or SkillType.Dragon or SkillType.Rain or SkillType.Cocoon))
        {
            bool healing = skill == SkillType.WaterHeal;
            if (healing ? target.isAlly != caster.isAlly || target.currentHP >= target.maxHP : target.isAlly == caster.isAlly) return;
            var friends = new System.Collections.Generic.HashSet<Vector2Int>(battle.gridMap.Values
                .Where(unit => unit != null && !unit.isDead && unit.isAlly == caster.isAlly).Select(unit => unit.gridPos));
            if (!BattleRangeService.CanTargetCell(ElementSkillExecutor.AttackSkill(caster), caster.gridPos, target.gridPos, battle.Cols, battle.Rows, friends)) return;
        }
        // At most one repeat per action; repeats bypass this entry point to avoid chains.
        BattleCharacter bell = battle.gridMap.Values.Where(unit => CanEcho(unit, caster))
            .OrderByDescending(unit => SkillExecutor.GetSkillChance(unit.data, unit.data.skillType, unit.isAlly, unit.OriginalData)).FirstOrDefault();
        if (bell == null || !SkillExecutor.RollSkillActivation(SkillExecutor.GetSkillChance(bell.data, bell.data.skillType, bell.isAlly, bell.OriginalData))) return;
        if (repeat(target))
        {
            battle.PlaySupportEcho(bell, caster);
            bell.PlayCastEffect(new Color(1f, 0.88f, 0.35f));
            battle.ShowFloatingText(bell, "再響", new Color(1f, 0.88f, 0.35f));
            battle.AddLog($"{bell.DisplayName} の再響！ {caster.DisplayName} が再発動！");
        }
    }

    public static bool Execute(BattleCharacter caster, BattleCharacter target, BattleManager battle)
    {
        if (battle == null || target == null || target.isDead || target.isAlly == caster.isAlly) return false;
        if (caster.data.skillType == SkillType.ShadowCopy)
        {
            string name = target.data.characterName;
            if (!caster.CopyBattleForm(target)) return false;
            battle.PlaySupportBurst(caster, "ShadowMist", .7f);
            battle.AddLog($"{caster.OriginalData.characterName} が {name} に変身！");
            battle.ShowFloatingText(caster, "変身", new Color(0.8f, 0.65f, 1f));
            return true;
        }
        if (caster.data.skillType != SkillType.ThreadPull) return false;
        int damage = Mathf.RoundToInt(caster.GetEffectiveAttack(battle) * caster.GetEffectiveSkillPower());
        caster.UpdateDirection(target.gridPos - caster.gridPos);
        battle.PlaySupportLink(caster, target, "PullThread");
        battle.PlaySupportBurst(target, "ThreadWrap", .65f);
        target.TakeDamage(damage, battle, caster, isBasicAttack: false, actionLabel: $"{caster.DisplayName} の引き糸");
        if (!target.isDead) battle.PullCharacter(caster, target);
        return true;
    }
}
