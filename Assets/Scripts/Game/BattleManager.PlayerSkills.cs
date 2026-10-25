using System.Linq;
using TMPro;
using UnityEngine;

public partial class BattleManager
{
    private const int PlayerSkillRecordToken = int.MinValue;
    private readonly PlayerSkillRules.BattleState playerSkill = new();
    private PlayerSkillRules.Plan playerSkillPlan = new();
    private PlayerSkillSelectionView playerSkillView;
    private int PlayerSkillClearedStage => GameManager.Instance != null ? GameManager.Instance.GetHighestClearedStageId() : 0;
    private bool PlayerSkillReady => PlayerSkillRules.IsValid(playerSkillPlan, PlayerSkillClearedStage);
    public PlayerSkillRules.BattleState PlayerSkillState => playerSkill;

    private void PreparePlayerSkill()
    {
        playerSkillPlan = PlayerSkillRules.Load();
        playerSkill.Reset(playerSkillPlan);
        RefreshPlayerSkillStatus();
    }
    private void OpenPlayerSkillSelection()
    {
        if (!IsDeploying || fusionView != null || playerSkillView != null) return;
        playerSkillView = new PlayerSkillSelectionView(transform, playerSkillPlan, PlayerSkillClearedStage, plan =>
        {
            playerSkillPlan = plan.Copy();
            playerSkill.Reset(playerSkillPlan);
            ClosePlayerSkillSelection();
        }, ClosePlayerSkillSelection);
    }
    private void ClosePlayerSkillSelection()
    {
        playerSkillView?.Dispose(); playerSkillView=null;
        RefreshPlayerSkillStatus();
    }
    private void RefreshPlayerSkillStatus()
    {
        string summary = PlayerSkillReady ? PlayerSkillRules.Summary(playerSkillPlan) : "プレイヤースキル：設定を選び直してください";
        deploymentView?.SetPlayerSkillState(summary, PlayerSkillReady);
        Transform header = transform.Find("BattleInformationHeader");
        var label = header != null ? header.Find("Objective")?.GetComponent<TextMeshProUGUI>() : null;
        if (label == null) return;
        label.text = summary + (playerSkillPlan.skillId == 0 ? "" : playerSkill.Fired ? playerSkill.Active ? $"　残り{playerSkill.Remaining}ターン" : "　使用済" : playerSkill.Finished ? "　未発動" : "　待機");
        label.rectTransform.anchorMin = new(.04f, .04f); label.rectTransform.anchorMax = new(.96f, .45f);
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        label.fontSizeMin=12 / label.canvas.rootCanvas.scaleFactor; label.fontSizeMax=16 / label.canvas.rootCanvas.scaleFactor;
        var title = header.Find("Title") as RectTransform;
        if (title != null) { title.anchorMin=new(.04f,.52f); title.anchorMax=new(.96f,.98f); }
    }
    private void BeginPlayerSkillTurn(int turn)
    {
        if (!playerSkill.Begin(turn, allies.Any(LivingForPlayerSkill) && enemies.Any(LivingForPlayerSkill))) return;
        var definition = playerSkill.Definition;
        battleRecords.Register(PlayerSkillRecordToken, "軍師・" + definition.name, null);
        AddLog($"プレイヤースキル【{definition.name}】発動！ 第{turn}ターン　{definition.Detail}", Color.yellow);
        foreach (var unit in allies.Where(LivingForPlayerSkill).ToArray())
        {
            if (definition.effect == PlayerSkillRules.Effect.Heal)
            {
                int amount = Mathf.Min(unit.maxHP-unit.currentHP, Mathf.Max(1, Mathf.RoundToInt(unit.maxHP*definition.percent/100f)));
                if (amount > 0)
                {
                    unit.currentHP += amount; unit.UpdateHPBar();
                    battleRecords.Credit(PlayerSkillRecordToken, healed: amount);
                    PlayDamageVfx(unit, amount, isHealing:true);
                }
            }
            PlaySupportBurst(unit, definition.effect == PlayerSkillRules.Effect.Heal ? "RainDrops"
                : definition.effect == PlayerSkillRules.Effect.Guard ? "GuardShell" : "BellEcho", .7f, .8f);
        }
        RefreshPlayerSkillStatus();
    }
    private static bool LivingForPlayerSkill(BattleCharacter unit) => unit != null && !unit.isDead && unit.currentHP > 0;
    public int ApplyPlayerSkillAttack(BattleCharacter unit, int attack) => unit.isAlly
        ? Mathf.RoundToInt(attack * (1f + playerSkill.AttackPercent / 100f)) : attack;
    public int ApplyPlayerSkillGuard(BattleCharacter unit, int damage)
    {
        if (!unit.isAlly) return damage;
        int reduced = playerSkill.ReduceDamage(damage);
        battleRecords.Credit(PlayerSkillRecordToken, prevented: damage-reduced);
        return reduced;
    }
    private void EndPlayerSkillTurn() { playerSkill.EndTurn(); RefreshPlayerSkillStatus(); }
    private void FinishPlayerSkill() { playerSkill.Finish(); RefreshPlayerSkillStatus(); }
    private bool ResolveBattleBeforePlayerSkill()
    {
        bool allyAlive = allies.Any(LivingForPlayerSkill), enemyAlive = enemies.Any(LivingForPlayerSkill);
        if (allyAlive && enemyAlive) return false;
        ShowResult(!allyAlive && !enemyAlive ? "引き分け" : enemyAlive ? "敗北…" : "勝利！",
            enemyAlive ? Color.red : Color.green, allyAlive && !enemyAlive);
        return true;
    }
}
