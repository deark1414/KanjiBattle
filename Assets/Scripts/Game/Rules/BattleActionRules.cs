public static class BattleActionRules
{
    // These skills are the unit's normal role, not a chance to perform that role.
    public static bool IsReliableRoleSkill(SkillType skill) => skill is
        SkillType.Slash or SkillType.Spear or SkillType.Arrow or SkillType.Gun
        or SkillType.Stone or SkillType.Fireball or SkillType.WaterHeal
        or SkillType.DoubleSlash or SkillType.ScatterShot or SkillType.ThreadPull or SkillType.ShadowCopy
        or SkillType.ChainLightning or SkillType.IronAdapt or SkillType.Warrior or SkillType.Sentinel
        or SkillType.Wave or SkillType.Rain or SkillType.Cocoon;

    public static SkillType GeometrySkill(SkillType skill) => skill switch
    {
        SkillType.Warrior or SkillType.Wave => SkillType.Slash,
        SkillType.Sentinel => SkillType.Spear,
        _ => skill
    };

    public static SkillType GetAttackSkill(BattleCharacter unit) => unit.data.skillType != SkillType.IronAdapt
        ? GeometrySkill(unit.data.skillType) : unit.IronForm == IronForm.Spear ? SkillType.Spear
            : unit.IronForm == IronForm.Sword ? SkillType.Slash : SkillType.None;
}
