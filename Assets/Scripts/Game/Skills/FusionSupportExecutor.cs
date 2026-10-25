using System.Linq;
using UnityEngine;

public static class FusionSupportExecutor
{
    public static bool IsForgeTarget(CharacterData data) => data != null && !data.isBoss
        && (data.characterId is 10 or 11 or 12 or 13
            || (BattleFusionRules.GetRecipe(data.characterId)?.ForgeEligible ?? false));

    public static void BeginSideTurn(BattleManager battle, bool ally, int turn)
    {
        var friends = battle.gridMap.Values.Where(unit => Living(unit) && unit.isAlly == ally).ToArray();
        foreach (BattleCharacter source in friends)
        {
            if (source.IsStunned()) continue;
            SkillType skill = source.data.skillType;
            if (skill == SkillType.Sentinel)
            {
                foreach (BattleCharacter target in friends.Where(unit =>
                    BattleRangeService.Distance(source.gridPos, unit.gridPos) <= 1))
                {
                    int amount = Mathf.Min(target.maxHP - target.currentHP, Mathf.Max(1, Mathf.RoundToInt(target.maxHP * 0.04f)));
                    if (amount <= 0) continue;
                    target.currentHP += amount;
                    battle.RecordHeal(source, amount);
                    target.UpdateHPBar();
                    battle.PlayDamageVfx(target, amount, isHealing: true);
                    battle.PlaySupportBurst(target, "RainDrops", .6f, .65f);
                    battle.AddLog($"{source.DisplayName} の守護回復！ {target.DisplayName} を {amount} 回復");
                }
            }
            else if (skill == SkillType.Warrior && IsSupportRound(turn))
            {
                source.ApplyAttackBuff(BattleAttackBuff.Warrior, 20, 2);
                BattleCharacter recipient = friends.Where(unit => unit != source)
                    .OrderBy(unit => BattleRangeService.Distance(source.gridPos, unit.gridPos))
                    .ThenBy(unit => unit.InstanceId).FirstOrDefault();
                recipient?.ApplyAttackBuff(BattleAttackBuff.Warrior, 20, 2);
                if (recipient != null) battle.PlaySupportBurst(recipient, "BellEcho", .7f);
                battle.PlayElementEffect(source, "鼓舞", new Color(1f, 0.7f, 0.35f));
                battle.AddLog($"{source.DisplayName} の鼓舞！ 自分と味方の攻撃+20%");
            }
            else if (skill == SkillType.Forge && IsSupportRound(turn))
            {
                int count = 0;
                foreach (BattleCharacter target in friends.Where(unit => IsForgeTarget(unit.data)))
                {
                    target.ApplyAttackBuff(BattleAttackBuff.Forge, 25, 3);
                    battle.PlaySupportForge(target);
                    count++;
                }
                if (count > 0) battle.AddLog($"{source.DisplayName} の鍛造！ 武器{count}体の攻撃+25%");
            }
        }
    }

    public static bool IsSupportRound(int turn) => turn >= 1 && (turn - 1) % 3 == 0;

    public static void EndSideTurn(BattleManager battle, bool ally)
    {
        foreach (BattleCharacter unit in battle.gridMap.Values)
            if (Living(unit) && unit.isAlly == ally)
                unit.TickAttackBuffs();
    }

    private static bool Living(BattleCharacter unit) => unit != null && !unit.isDead && unit.currentHP > 0;
}
