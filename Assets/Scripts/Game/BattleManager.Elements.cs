using System.Collections.Generic;
using UnityEngine;

public partial class BattleManager
{
    private static float ElementActionDelay(BattleCharacter unit) => unit != null
        && BattleMovementRules.GetMaxSteps(unit.data.category) + unit.MovementModifier > 2 ? 0.45f : 0.3f;

    public void PlayElementEffect(BattleCharacter target, string label, Color color)
    {
        if (target == null) return;
        target.PlayCastEffect(color);
        ShowFloatingText(target, label, color);
        if (gridCells != null)
            StartCoroutine(HighlightCellsRoutine(new List<Vector2Int> { target.gridPos },
                new Color(color.r, color.g, color.b, 0.4f), 0.25f));
    }
}
