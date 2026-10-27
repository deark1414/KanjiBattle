using System.Collections.Generic;
using UnityEngine;

public partial class BattleManager
{
    private static float ElementActionDelay(BattleCharacter unit) => unit != null
        && BattleMovementRules.GetMaxSteps(unit.data.category) + unit.MovementModifier > 2 ? 0.45f : 0.3f;

    public void PlayElementEffect(BattleCharacter target, string label, Color color)
    {
        if (target == null) return;
        string sprite = label switch
        {
            "追風" or "移動+1" or "向風" => "WindGust",
            "鼓舞" => "BellEcho",
            "回避" => "LanternFlame",
            "燃焼" => "CoalEmbers",
            _ => null
        };
        if (sprite != null) PlaySupportBurst(target, sprite, .65f);
        if (label == "燃焼") RefreshSupportState(target);
        target.PlayCastEffect(color);
        ShowFloatingText(target, label, color);
        if (gridCells != null)
            StartCoroutine(HighlightCellsRoutine(new List<Vector2Int> { target.gridPos },
                new Color(color.r, color.g, color.b, 0.4f), 0.25f));
    }
}
