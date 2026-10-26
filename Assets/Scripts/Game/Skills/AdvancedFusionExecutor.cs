using System.Linq;
using UnityEngine;

public static class AdvancedFusionExecutor
{
    public static bool IsMagnetRear(SkillType skill) => FusionReactionRules.IsProjectile(skill)
        || skill is SkillType.WaterHeal or SkillType.Rain or SkillType.Forge or SkillType.BellEcho
            or SkillType.WindChime or SkillType.Dice or SkillType.Sand or SkillType.Lantern;

    public static Vector2Int MagnetDestination(Vector2Int source, Vector2Int target, SkillType skill)
    {
        Vector2Int toward = BattleRangeService.NormalizeDirection(source - target);
        return target + (IsMagnetRear(skill) ? -toward : toward);
    }

    public static int DiceBonusPercent(int turn, float effect = 1) =>
        Mathf.RoundToInt(Mathf.Clamp((100 + (Mathf.Clamp(turn, 1, 9) - 1) * 25) * effect, 0, 300));

    public static BattleCharacter FindFriendlyTarget(BattleManager battle, BattleCharacter caster, bool unprotected) =>
        battle.gridMap.Values.Where(unit => unit != null && !unit.isDead && unit.currentHP > 0
            && unit.isAlly == caster.isAlly && (!unprotected || !unit.CocoonShield))
        .OrderBy(unit => (double)unit.currentHP / unit.maxHP).ThenBy(unit => unit.InstanceId).FirstOrDefault();

    public static bool Execute(BattleCharacter caster, BattleCharacter target, BattleManager battle)
    {
        if (battle == null || target == null || target.isDead || target.currentHP <= 0) return false;
        SkillType skill = caster.data.skillType;
        if (skill is SkillType.Rain or SkillType.Cocoon)
        {
            if (target.isAlly != caster.isAlly) return false;
            if (skill == SkillType.Rain) battle.CastRain(caster, battle.FindRainCenter(caster));
            else
            {
                if (target.CocoonShield) return false;
                target.CocoonShield = true;
                target.CocoonRecordToken = caster.RecordToken;
                battle.SetCocoonMarker(target, true);
                battle.PlayElementEffect(target, "繭", Color.white);
                battle.AddLog($"{caster.DisplayName} が {target.DisplayName} に繭の保護を付与");
            }
            return true;
        }
        if (skill != SkillType.Wave || caster.isAlly == target.isAlly
            || !BattleRangeService.CanTargetCell(SkillType.Wave, caster.gridPos, target.gridPos, battle.Cols, battle.Rows)) return false;
        Vector2Int direction = BattleRangeService.NormalizeDirection(target.gridPos - caster.gridPos);
        var victims = TargetingService.GetSwordWedgeTargets(battle, caster, direction)
            .OrderByDescending(unit => unit.gridPos.x * direction.x + unit.gridPos.y * direction.y).ToArray();
        foreach (BattleCharacter victim in victims)
        {
            battle.PlaySupportLink(caster, victim, "WaveCrest");
            battle.PlayElementEffect(victim, "波", new Color(.35f, .8f, 1f));
            victim.TakeDamage(Mathf.Max(1, Mathf.RoundToInt(caster.GetEffectiveAttack(battle)
                * caster.GetEffectiveSkillPower() * .8f)), battle, caster, actionLabel: "波の押し流し");
            if (!victim.isDead) battle.TryFusionDisplace(victim, victim.gridPos + direction);
        }
        return victims.Length > 0;
    }
}
