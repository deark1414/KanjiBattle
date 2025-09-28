using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "StageDatabase", menuName = "Game/Stage Database")]
public class StageDatabase : ScriptableObject
{
    public static StageDatabase Instance { get; private set; }

    public List<StageData> stages = new();

    private void OnEnable()
    {
        Instance = this;
    }

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

    public StageData GetStageById(int id)
    {
        return stages.FirstOrDefault(s => s.stageId == id);
    }
}