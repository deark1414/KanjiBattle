public static class BattleActionRules
{
    // These skills are the unit's normal role, not a chance to perform that role.
    public static bool IsReliableRoleSkill(SkillType skill) => skill is
        SkillType.Slash or SkillType.Spear or SkillType.Arrow or SkillType.Gun
        or SkillType.Stone or SkillType.Fireball or SkillType.WaterHeal
        or SkillType.DoubleSlash or SkillType.ScatterShot;
}
