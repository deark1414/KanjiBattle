#if KANJI_BATTLE_E2E
using System.Linq;
using UnityEngine;

public partial class BattleManager
{
    internal BattleE2EBridge.BattleState CaptureE2EState()
    {
        return new BattleE2EBridge.BattleState
        {
            active = gameObject.activeInHierarchy,
            deploying = IsDeploying,
            fusionOpen = fusionView != null,
            battleId = gridCells == null ? 0 : gridCells[0, 0].GetInstanceID(),
            result = resultPanel != null && resultPanel.activeInHierarchy,
            won = resultText != null && resultText.text.Contains("勝利"),
            originalIds = deployedAllies.Select(data => data.characterId).ToArray(),
            units = gridMap.Values.Where(unit => unit != null && !unit.isDead).Select(unit => new BattleE2EBridge.Unit
            {
                id = unit.data.characterId, name = unit.data.characterName, ally = unit.isAlly,
                x = unit.gridPos.x, y = unit.gridPos.y, hp = unit.currentHP,
                maxHP = unit.maxHP, level = unit.level
            }).ToArray(),
            plan = fusionPlan,
            cells = gridCells == null ? new BattleE2EBridge.Cell[0] : Enumerable.Range(0, cols * rows).Select(index =>
            {
                int x = index % cols, y = index / cols;
                return new BattleE2EBridge.Cell { x = x, y = y, rect = BattleE2EBridge.Bounds(gridCells[x, y] as RectTransform) };
            }).ToArray()
        };
    }
}
#endif
