using System.Linq;
using UnityEngine;

public static class FusionReactionRules
{
    public static bool IsProjectile(SkillType skill) => skill is SkillType.Arrow or SkillType.Gun
        or SkillType.Stone or SkillType.Fireball or SkillType.ScatterShot;

    public static bool TryProtect(BattleCharacter target, BattleCharacter attacker, BattleManager battle, bool skillHit)
    {
        if (target.CocoonShield)
        {
            target.CocoonShield = false;
            battle?.SetCocoonMarker(target, false);
            battle?.PlayElementEffect(target, "保護", Color.white);
            battle?.AddLog($"{target.DisplayName} の繭が攻撃を防いだ！");
            return true;
        }
        if (battle == null || !skillHit || !IsProjectile(attacker.data.skillType)) return false;
        bool protectedByLamp = battle.gridMap.Values.Any(unit => unit != null && unit != target && !unit.isDead
            && unit.currentHP > 0 && unit.isAlly == target.isAlly && unit.data.skillType == SkillType.Lantern
            && BattleRangeService.Distance(unit.gridPos, target.gridPos) == 1);
        if (!protectedByLamp || !SkillExecutor.RollSkillActivation(70)) return false;
        battle.PlayElementEffect(target, "回避", new Color(1f, .85f, .45f));
        battle.AddLog($"{target.DisplayName} は灯に守られ遠距離攻撃を回避！");
        return true;
    }

    public static void AfterDamage(BattleCharacter target, BattleCharacter attacker, int loss,
        BattleDamageOrigin origin, BattleManager battle)
    {
        if (origin != BattleDamageOrigin.Direct || attacker == null || attacker == target
            || attacker.isAlly == target.isAlly || loss <= 0) return;
        SkillType skill = target.data.skillType;
        if (skill == SkillType.Coal && target.currentHP > 0 && !target.CoalAwake)
        {
            target.CoalAwake = true;
            battle?.PlayElementEffect(target, "燃焼", new Color(1f, .4f, .1f));
            battle?.AddLog($"{target.DisplayName} が燃え上がった！ 攻撃+40%");
        }
        if (skill == SkillType.Grass && target.currentHP > 0
            && (long)target.currentHP * 4 <= (long)target.maxHP * 3) battle?.SplitGrass(target);
        if (skill == SkillType.Mountain && !attacker.isDead && SkillExecutor.RollSkillActivation(30))
        {
            battle?.PlaySupportBurst(target, "MountainRebound", .6f);
            battle?.PlaySupportLink(target, attacker, "MortarShell", .1f);
            battle?.AddLog($"{target.DisplayName} の山びこ！ {loss}を返した");
            attacker.TakeDamage(loss, battle, target, ignoreDefense: true, origin: BattleDamageOrigin.Returned);
        }
        if (skill == SkillType.Fortress && battle != null && SkillExecutor.RollSkillActivation(25))
        {
            int damage = Mathf.Max(1, Mathf.RoundToInt(loss * .25f));
            foreach (BattleCharacter enemy in battle.gridMap.Values.Where(unit => unit != null && !unit.isDead
                && unit.currentHP > 0 && unit.isAlly != target.isAlly
                && BattleRangeService.Distance(unit.gridPos, target.gridPos) == 1).ToArray())
                enemy.TakeDamage(damage, battle, target, origin: BattleDamageOrigin.Reflected);
        }
    }
}
