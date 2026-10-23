using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class BattleManager
{
    private BattleFusionRules.Plan fusionPlan;
    private BattleFusionView fusionView;
    private readonly List<CharacterData> fusionRuntimeData = new();
    private bool CanUseFusion => FacilityManager.Instance != null && FacilityManager.Instance.IsFusionUnlocked();

    private void PrepareFusionPlan() => fusionPlan = CanUseFusion
        ? BattleFusionRules.Load(deployedAllies) : BattleFusionRules.Empty(deployedAllies);

    private void OpenFusionSelection()
    {
        if (!IsDeploying || !CanUseFusion || fusionView != null) return;
        fusionView = new BattleFusionView(transform, deployedAllies, fusionPlan, CloseFusionSelection);
    }

    private void CloseFusionSelection()
    {
        fusionView?.Dispose();
        fusionView = null;
    }

    private void ApplyFusionPlan()
    {
        if (!CanUseFusion || !BattleFusionRules.IsValid(deployedAllies, fusionPlan)) return;
        BattleCharacter[] originals = allies.ToArray();
        foreach (BattleFusionRules.Group group in fusionPlan.groups)
        {
            BattleFusionRules.Recipe recipe = BattleFusionRules.GetRecipe(group.recipeId);
            int level = originals[group.origin].level;
            Vector2Int position = originals[group.origin].gridPos;
            CharacterData data = CharacterData.CreateBattleFusion(recipe,
                group.members.Select(index => deployedAllies[index]).ToArray(), level);
            fusionRuntimeData.Add(data);
            foreach (int index in group.members)
            {
                BattleCharacter material = originals[index];
                gridMap.Remove(material.gridPos);
                occupied.Remove(material.gridPos);
                allies.Remove(material);
                material.gameObject.SetActive(false);
                Destroy(material.gameObject);
            }
            SpawnCharacter(data, position, true, level);
            BattleFusionRules.Discover(recipe.Id);
            AddLog($"{string.Join("＋", group.members.Select(index => deployedAllies[index].characterName))} → {recipe.Name}", Color.yellow);
        }
    }

    private void ClearFusionRuntimeData()
    {
        foreach (CharacterData data in fusionRuntimeData)
            if (data != null) Destroy(data);
        fusionRuntimeData.Clear();
    }

    public void PlayFusionSkillVfx(BattleCharacter caster, BattleCharacter target, SkillType skill)
    {
        StartSkillCinematic(0.5f);
        BeginSkillImpactWindow(0.5f);
        HighlightSkillRange(caster, target, skill, Color.yellow);
        caster.PlayCastEffect(Color.yellow);
        if (skill == SkillType.DoubleSlash) PlayWeaponMotionVfx(caster, target, SkillType.Slash);
        foreach (Vector2Int cell in BattleRangeService.GetEffectCells(skill, caster.gridPos, target.gridPos, cols, rows))
        {
            if (!gridMap.TryGetValue(cell, out BattleCharacter victim) || victim == null || victim.isAlly == caster.isAlly || victim.isDead) continue;
            if (skill == SkillType.DoubleSlash)
                StartCoroutine(SlashVfxRoutine(victim.transform as RectTransform, true));
            else
                StartCoroutine(SpriteProjectileVfxRoutine(caster.transform as RectTransform, victim.transform as RectTransform, "Gun", useArc: false));
        }
        GameAudio.Instance.PlaySkillSound(skill == SkillType.DoubleSlash ? SkillType.Slash : SkillType.Gun);
    }

    private void OnDestroy() => ClearFusionRuntimeData();
}
