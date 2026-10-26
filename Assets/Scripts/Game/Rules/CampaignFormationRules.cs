using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CampaignFormationRules
{
    public static List<Vector2Int> EnemyPositions(StageData stage)
    {
        int count=stage.GetEnemies().Count;
        if(stage.enemyPositions==null||stage.enemyPositions.Count==0)
            return Enumerable.Range(0,count).Select(index=>BattleDeploymentRules.DefaultPosition(index,false)).ToList();
        if(stage.enemyPositions.Count!=count||stage.enemyPositions.Distinct().Count()!=count
            ||stage.enemyPositions.Any(cell=>cell.x<0||cell.x>=BattleDeploymentRules.Columns||cell.y<0||cell.y>=BattleDeploymentRules.DeploymentRows))
            throw new ArgumentException("Invalid enemy formation: "+stage.stageId);
        return new List<Vector2Int>(stage.enemyPositions);
    }
}
