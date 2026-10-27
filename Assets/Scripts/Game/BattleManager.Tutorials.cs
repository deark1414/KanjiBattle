using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public partial class BattleManager
{
    private Vector2Int? tutorialDestination;
    public bool CanGuideDeployment => currentStage!=null&&currentStage.stageId==1&&allies.Any(unit=>unit.data.characterId==20);
    public Button TutorialDeploymentTarget(int step)
    {
        if(!IsDeploying)return null;
        var water=allies.FirstOrDefault(unit=>unit.data.characterId==20);
        if(step==2||water==null)return deploymentView.ConfirmButton;
        if(step==0)return deploymentView.CellButton(water.gridPos);
        if(!tutorialDestination.HasValue||gridMap.ContainsKey(tutorialDestination.Value))
        {
            tutorialDestination=null;
            for(int x=cols-1;x>=0;x--)
            {
                var cell=new Vector2Int(x,rows-1);
                if(!gridMap.ContainsKey(cell)&&!impassableCells.Contains(cell)){tutorialDestination=cell;break;}
            }
        }
        return tutorialDestination.HasValue?deploymentView.CellButton(tutorialDestination.Value):deploymentView.ConfirmButton;
    }
    private void RequestDeploymentGuides()
    {
        tutorialDestination=null;
        var guides=TutorialDirector.Instance;if(guides==null)return;
        guides.RequestOnce("deployment",gameObject,()=>IsDeploying&&playerSkillView==null&&fusionView==null);
        if(CanUseFusion)guides.RequestOnce("fusion",gameObject,()=>IsDeploying&&CanUseFusion);
    }
    private void RequestResultGuide(bool won)
    {
        var guides=TutorialDirector.Instance;if(guides==null)return;
        bool previous=isPaused;
        bool firstStageWin=won&&currentStage!=null&&currentStage.stageId==1;
        Action continued=firstStageWin?()=>UIManager.Instance?.ShowFacility():null;
        if(guides.RequestOnce(won?"victory":"defeat",gameObject,
            ()=>resultPanel!=null&&resultPanel.activeInHierarchy,continued,
            ()=>{if(this!=null)isPaused=previous;},firstStageWin?"城下へ":"結果を見る"))isPaused=true;
    }
}
