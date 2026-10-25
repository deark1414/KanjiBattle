using System;

public partial class BattleManager
{
    private void RequestDeploymentGuides()
    {
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
