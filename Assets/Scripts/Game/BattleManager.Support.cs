using UnityEngine;

public partial class BattleManager
{
    public bool PullCharacter(BattleCharacter caster, BattleCharacter target)
    {
        if (caster == null || target == null || caster.isDead || target.isDead) return false;
        Vector2Int destination = target.gridPos + BattleRangeService.NormalizeDirection(caster.gridPos - target.gridPos);
        if (!IsCellFree(destination)) return false;
        MoveCharacter(target, destination, target.isAlly ? "味方" : "敵");
        AddLog($"{target.DisplayName} を1マス引き寄せた！");
        return true;
    }
}
