using UnityEditor;
using UnityEngine;

public static class SupportSkillVfxValidation
{
    public static void BuildLocalReview()
    {
        SupportSkillVfxAssets.PromoteAndValidate();
        BattleRuleRegression.RunFromCommandLine();
        GameDataValidator.ValidateFromCommandLine();
        KanjiBattle.Editor.WebGLBuild.BuildLocalE2E();
        Debug.Log("[SupportSkillVfxValidation] Passed. Local review build ready.");
    }
}
