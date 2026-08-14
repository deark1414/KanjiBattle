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
        SortStages();
        if (index < 0 || index >= stages.Count) return null;
        return stages[index];
    }

    public void AssignStageIds()
    {
        SortStages();
    }

    public void SortStages()
    {
        stages = stages
            .Where(stage => stage != null)
            .OrderBy(stage => stage.stageId > 0 ? stage.stageId : int.MaxValue)
            .ThenBy(stage => stage.name)
            .ToList();
    }

    public void RepairMissingStageIdsFromAssetNames()
    {
        foreach (var stage in stages)
        {
            if (stage == null || stage.stageId > 0)
            {
                continue;
            }

            if (TryParseTrailingNumber(stage.name, out int parsedId))
            {
                stage.stageId = parsedId;
            }
        }

        SortStages();
    }

    private static bool TryParseTrailingNumber(string value, out int number)
    {
        number = 0;
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        int end = value.Length - 1;
        while (end >= 0 && char.IsDigit(value[end]))
        {
            end--;
        }

        if (end == value.Length - 1)
        {
            return false;
        }

        return int.TryParse(value.Substring(end + 1), out number);
    }

    public StageData GetStageById(int id)
    {
        SortStages();
        return stages.FirstOrDefault(s => s.stageId == id);
    }
}
