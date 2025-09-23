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

    public void AssignStageIds()
    {
        for (int i = 0; i < stages.Count; i++)
        {
            stages[i].stageId = i + 1; // 1始まり
        }
    }
}