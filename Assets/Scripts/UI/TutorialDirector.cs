using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class TutorialDirector : MonoBehaviour
{
    private sealed class Request
    {
        public string id,primary;
        public GameObject owner;
        public Func<bool> relevant;
        public Action continued,released;
    }
    public static TutorialDirector Instance { get; private set; }
    private readonly Queue<Request> pending = new();
    private Request active;
    private FullScreenGuideView view;
    private GuidedTutorialView guided;
    private UIManager ui;
    private Coroutine startup;
    private bool suppressed;
    public string CurrentId { get; private set; } = "";
    public bool IsOpen => view!=null||guided!=null;
    public int GuideStep => guided!=null?guided.Step:-1;
    public string GuideTarget => guided!=null?guided.Target:"";
    public static TutorialDirector Ensure(UIManager owner)
    {
        if(Instance==null)Instance=owner.gameObject.AddComponent<TutorialDirector>();
        Instance.ui=owner;return Instance;
    }
    private void OnDestroy(){CancelAll();if(Instance==this)Instance=null;}
    public void BeginStartup()
    {
        CancelAll();
        if(startup!=null)StopCoroutine(startup);
#if KANJI_BATTLE_E2E
        string fixture=PlayerProgressStore.GetString("KanjiBattle.E2E.Fixture");
        if(!string.IsNullOrEmpty(fixture)&&fixture!="tutorial")
        {
            suppressed=true;startup=null;ui.ShowTop();return;
        }
#endif
        startup=StartCoroutine(StartNextFrame());
    }
    private IEnumerator StartNextFrame()
    {
        yield return null;
        startup=null;
        if(suppressed)yield break;
        GameAudio.Instance.PlayBgm(GameBgm.Top);
        StageData first=StageDatabase.Instance?.GetStageById(1);
        if(!TutorialProgress.HasSeen("entry") && GameManager.Instance.GetHighestClearedStageId()==0 && first!=null)
        {
            GameManager.Instance.SetSelectedStage(first);ui.ShowFormation();TutorialProgress.MarkSeen("entry");
        }
        else ui.ShowTop();
        if(TutorialProgress.OpeningEnabled)ShowOpening();
    }
    public bool RequestOnce(string id,GameObject owner,Func<bool> relevant=null,Action continued=null,Action released=null,string primary=null)
    {
        if(suppressed || TutorialProgress.HasSeen(id)||active?.id==id||pending.Any(item=>item.id==id))return false;
        pending.Enqueue(new Request{id=id,owner=owner,relevant=relevant,continued=continued,released=released,primary=primary});return true;
    }
    private void Update()
    {
        if(active!=null && !Relevant(active)){CloseView();ReleaseActive();}
        if(IsOpen||suppressed||startup!=null)return;
        while(pending.Count>0)
        {
            var request=pending.Dequeue();
            if(!Relevant(request)||TutorialProgress.HasSeen(request.id)){request.released?.Invoke();continue;}
            active=request;CurrentId=request.id;
            if(request.id=="formation"||(request.id=="deployment"&&FindAnyObjectByType<BattleManager>()?.CanGuideDeployment==true))guided=GuidedTutorialView.Open(request.id,Complete);
            else view=FullScreenGuideView.Open(TutorialContent.Get(request.id),()=>Complete(),null,request.primary??"わかった");
            break;
        }
    }
    private static bool Relevant(Request item) => item.owner!=null && item.owner.activeInHierarchy && (item.relevant==null||item.relevant());
    private void ReleaseActive(){var previous=active;active=null;previous?.released?.Invoke();}
    private void Complete()
    {
        var previous=active;if(previous==null)return;
        TutorialProgress.MarkSeen(previous.id);CloseView();ReleaseActive();previous.continued?.Invoke();
    }
    private void CloseView(){if(view!=null)view.Dispose();if(guided!=null)guided.Dispose();view=null;guided=null;CurrentId="";}
    public void CancelAll()
    {
        CloseView();ReleaseActive();
        while(pending.Count>0)pending.Dequeue().released?.Invoke();
    }
    public void ShowOpening()
    {
        CloseView();ReleaseActive();CurrentId="opening";
        view=FullScreenGuideView.Open(TutorialContent.Opening,CloseView,CloseView,"始める","スキップ");
    }
    public void ShowHelp()
    {
        CloseView();ReleaseActive();CurrentId="help";
        view=FullScreenGuideView.OpenMenu(TutorialContent.Pages,page=>ShowReplay(page),CloseView);
    }
    private void ShowReplay(TutorialContent.Page page)
    {
        CloseView();CurrentId="replay:"+page.id;
        view=FullScreenGuideView.Open(page,ShowHelp,null,"一覧に戻る");
    }
#if KANJI_BATTLE_E2E
    public void SuppressForDebug(bool value)
    {
        suppressed=value;
        if(startup!=null){StopCoroutine(startup);startup=null;}
        CancelAll();
    }
#endif
}
