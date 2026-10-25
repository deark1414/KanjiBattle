using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class BattleManager
{
    private BattleFusionRules.Plan fusionPlan;
    private BattleFusionRules.Plan fusionChoices;
    private BattleFusionRules.Resolution fusionResolution;
    private BattleFusionView fusionView;
    private BattleFusionPreviewView fusionPreview;
    private Vector2Int[] fusionPositions;
    private readonly List<CharacterData> fusionRuntimeData = new();
    private bool CanUseFusion => FacilityManager.Instance != null && FacilityManager.Instance.IsFusionUnlocked();

    private bool HasFusionConflict => fusionResolution != null && fusionResolution.conflicts.Count > 0;

    private void PrepareFusionPlan()
    {
        fusionChoices = CanUseFusion ? BattleFusionRules.Load(deployedAllies) : BattleFusionRules.Empty(deployedAllies);
        RefreshFusionPlan();
    }

    private void RefreshFusionPlan()
    {
        fusionPositions = allies.Select(unit => unit.gridPos).ToArray();
        if (!CanUseFusion)
        {
            fusionPlan = BattleFusionRules.Empty(deployedAllies);
            fusionResolution = null;
        }
        else
        {
            fusionResolution = BattleFusionRules.ResolveAdjacent(deployedAllies, fusionPositions, fusionChoices);
            fusionChoices.groups.RemoveAll(choice => !fusionResolution.candidates.Any(candidate => BattleFusionRules.SameGroup(candidate, choice)));
            fusionPlan = fusionResolution.plan;
        }
        RefreshFusionPreview();
    }

    private void RefreshFusionPreview()
    {
        if (!IsDeploying) return;
        fusionPreview ??= new BattleFusionPreviewView(transform, gridCells);
        fusionPreview.Draw(fusionPlan, fusionPositions, fusionResolution?.conflicts);
        string summary = fusionPlan.groups.Count == 0 ? "融合なし" : string.Join("　", fusionPlan.groups.Select(group =>
            $"{string.Join("＋", group.members.Select(index => deployedAllies[index].characterName))} → {(BattleFusionRules.IsDiscovered(group.recipeId) ? BattleFusionRules.GetRecipe(group.recipeId).Name : "？")}（{group.members.Length}→1）"));
        if (HasFusionConflict) summary = "組合せ未確定　" + summary;
        deploymentView.SetFusionState(summary, !HasFusionConflict, fusionResolution?.alternatives.Count > 0);
        deploymentView.BringToFront();
        fusionView?.BringToFront();
    }

    private void OpenFusionSelection()
    {
        if (!IsDeploying || !CanUseFusion || fusionView != null || fusionResolution.alternatives.Count == 0) return;
        fusionView = new BattleFusionView(transform, deployedAllies, fusionChoices, fusionResolution.alternatives, () =>
        {
            CloseFusionSelection();
            RefreshFusionPlan();
        });
    }

    private void CloseFusionSelection()
    {
        fusionView?.Dispose();
        fusionView = null;
    }

    private void ApplyFusionPlan()
    {
        if (!CanUseFusion || HasFusionConflict || !BattleFusionRules.IsValid(deployedAllies, fusionPlan)) return;
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
            RegisterFusionRecord(gridMap[position], group, originals);
            BattleFusionRules.Discover(recipe.Id);
            AddLog($"{string.Join("＋", group.members.Select(index => deployedAllies[index].characterName))} → {recipe.Name}", Color.yellow);
        }
    }

    private void ClearFusionRuntimeData()
    {
        ClearAdvancedFusionState();
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
        if (skill == SkillType.DoubleSlash)
        {
            PlayWeaponMotionVfx(caster, target, SkillType.Slash);
            StartCoroutine(RepeatFusionSword(caster, target));
        }
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

    private System.Collections.IEnumerator RepeatFusionSword(BattleCharacter caster, BattleCharacter target)
    {
        yield return BattleWait(.16f);
        if (caster != null && target != null && !caster.isDead)
            PlayWeaponMotionVfx(caster, target, SkillType.Slash);
    }

    private void OnDestroy() => ClearFusionRuntimeData();
}
