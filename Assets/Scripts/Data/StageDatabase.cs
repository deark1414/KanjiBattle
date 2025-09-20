using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageDatabase", menuName = "Game/Stage Database")]
public class StageDatabase : ScriptableObject
{
    public List<StageData> stages = new();

    public StageData GetStage(int index)
    {
        if (index < 0 || index >= stages.Count) return null;
        return stages[index];
    }
}