using System.Collections.Generic;
using UnityEngine;

public static class BattleMovementRules
{
    public static int GetMaxSteps(CharacterCategory category)
    {
        return category == CharacterCategory.Animal ? 2 : 1;
    }

    public static int GetDestinationIndex(CharacterCategory category, List<Vector2Int> path, bool targetCellOccupied)
    {
        if (path == null || path.Count < 2) return 0;

        int lastWalkableIndex = targetCellOccupied ? path.Count - 2 : path.Count - 1;
        return Mathf.Clamp(GetMaxSteps(category), 1, Mathf.Max(1, lastWalkableIndex));
    }
}
