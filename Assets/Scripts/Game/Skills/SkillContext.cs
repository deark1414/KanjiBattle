public class SkillContext
{
    public BattleManager BattleManager { get; }
    public BattleCharacter Caster { get; }
    public BattleCharacter Target { get; }

    public SkillContext(BattleManager battleManager, BattleCharacter caster, BattleCharacter target)
    {
        BattleManager = battleManager;
        Caster = caster;
        Target = target;
    }
}
