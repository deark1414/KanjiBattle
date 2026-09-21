public enum SkillExecutionFailure
{
    None,
    InvalidCaster,
    NoSkill,
    PassiveOrReactiveSkill,
    ChanceFailed,
    NoValidTarget,
    UnsupportedEffect
}

public readonly struct SkillExecutionResult
{
    public bool Succeeded { get; }
    public SkillType SkillType { get; }
    public SkillExecutionFailure Failure { get; }

    private SkillExecutionResult(bool succeeded, SkillType skillType, SkillExecutionFailure failure)
    {
        Succeeded = succeeded;
        SkillType = skillType;
        Failure = failure;
    }

    public static SkillExecutionResult Success(SkillType skillType) =>
        new SkillExecutionResult(true, skillType, SkillExecutionFailure.None);

    public static SkillExecutionResult Failed(SkillType skillType, SkillExecutionFailure failure) =>
        new SkillExecutionResult(false, skillType, failure);
}
