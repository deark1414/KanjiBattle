using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class CampaignTreeAuthoring
{
    public static System.Collections.Generic.List<Vector2Int> Positions(int[] ids)
    {
        int[] columns={2,3,1,4,0,5};
        var rearIds=new System.Collections.Generic.HashSet<int>{16,17,18,19,20,27,31,32};
        int front=0,rear=0;
        var cells=ids.Select(id=>rearIds.Contains(id)?new Vector2Int(columns[rear++],0):new Vector2Int(columns[front++],1)).ToList();
        if(front>6||rear>6)throw new Exception("Deployment row overflow.");
        return cells;
    }
    public static void Apply()
    {
        var plans=JArray.Parse(File.ReadAllText("Assets/Data/campaign-plan.json"));
        var rows=JArray.Parse(File.ReadAllText("Assets/Data/stages.json"));
        var database=AssetDatabase.LoadAssetAtPath<CharacterDatabase>("Assets/Resources/CharacterDatabase.asset");
        var stages=AssetDatabase.LoadAssetAtPath<StageDatabase>("Assets/ScriptableObjects/Stages/StageDatabase.asset");
        if(plans.Count!=40||rows.Count!=40)throw new Exception("Expected 40 campaign stages.");
        for(int index=0;index<40;index++)
        {
            var plan=plans[index];var row=rows.Single(token=>(int)token["stageId"]==index+1);
            var stage=stages.GetStageById(index+1);
            int[] ids=plan["enemies"].Values<int>().ToArray();
            var cells=Positions(ids);
            stage.enemyPool=ids.Select(id=>database.GetById(id)??throw new Exception("Unknown character "+id)).ToList();
            stage.enemyLevel=(int)plan["level"];stage.slotCount=8;stage.restrictFormationSlots=false;
            stage.enemyPositions=cells;
            // Initial traps stay mild; chapter pressure comes from roles and positioning.
            stage.trapCount=index<3?0:index<20?1:2;
            stage.trapDamage=index<3?0:Mathf.Min(30,4+index/2);
            stage.reinforcementInterval=0;stage.reinforcementLimit=0;stage.reinforcementCount=0;stage.reinforcementEnemy.Clear();
            row["enemyIds"]=new JArray(ids);row["enemyLevel"]=stage.enemyLevel;row["slotCount"]=8;
            row["restrictFormationSlots"]=false;row["enemyPositions"]=new JArray(cells.Select(c=>new JObject{{"x",c.x},{"y",c.y}}));
            row["tacticalTheme"]=plan["theme"].DeepClone();
            row["trapCount"]=stage.trapCount;row["trapDamage"]=stage.trapDamage;
            row["reinforcementEnemyIds"]=new JArray();row["reinforcementInterval"]=0;row["reinforcementCount"]=0;row["reinforcementLimit"]=0;
            EditorUtility.SetDirty(stage);
        }
        File.WriteAllText("Assets/Data/stages.json",rows.ToString(Formatting.Indented)+"\n");
        AssetDatabase.SaveAssets();
        Debug.Log("[CampaignTreeAuthoring] Applied 40 stages.");
    }
    public static void BuildReview()
    {
        Apply();BattleRuleRegression.RunFromCommandLine();GameDataValidator.ValidateFromCommandLine();
        KanjiBattle.Editor.WebGLBuild.BuildLocalE2E();
    }
}
