using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CastleTreeView : MonoBehaviour
{
    private RectTransform content;
    private TextMeshProUGUI title,description,status;
    private Button action;
    private ScrollRect scroll;
    private Transform tabs;
    private string selected="c11", signature;
    private int branch;
    private float nextRefresh;
    private float fontScale;
    private bool dirty=true;
    private bool focusPending=true;
    private readonly Dictionary<string,Vector2> positions=new();
    private readonly Color[] branchColors={new(.76f,.46f,.36f),new(.40f,.67f,.61f),new(.77f,.67f,.35f),new(.42f,.65f,.40f),new(.59f,.55f,.73f)};
    private void Start()
    {
        var root=(RectTransform)transform;
        fontScale=UiReadability.Scale(GetComponentInParent<Canvas>());
        root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
        var detail=Surface(transform,"TreeDetail",new(.08f,.12f,.10f,.97f),new(.04f,.71f),new(.96f,.90f));
        title=Text(detail.transform,"Title","城下",new(.035f,.72f),new(.96f,.98f),24);
        description=Text(detail.transform,"Description","",new(.035f,.36f),new(.96f,.72f),20);
        status=Text(detail.transform,"Status","",new(.035f,.04f),new(.69f,.35f),18);
        action=Command(detail.transform,"TreePurchase","解放",new(.71f,.04f),new(.97f,.34f),()=>
        {
            if(CastleTree.Purchase(selected))GameAudio.Instance.Play(GameSound.Click);
            dirty=true;
        });
        tabs=Surface(transform,"TreeBranches",new(0,0,0,0),new(.04f,.63f),new(.96f,.69f)).transform;
        for(int i=0;i<CastleTree.Branches.Length;i++)
        {
            int index=i;
            Command(tabs,"TreeBranch_"+i,CastleTree.Branches[i],new(i/5f+.003f,0),new((i+1)/5f-.003f,1),()=>
            {branch=index;selected=CastleTree.Nodes.FirstOrDefault(n=>n.branch==index&&!CastleTree.Owned(n)&&CastleTree.RequiredStage(n)==0)?.id??CastleTree.Nodes.First(n=>n.branch==index).id;dirty=true;focusPending=true;});
        }
        var view=Surface(transform,"TreeViewport",new(.055f,.085f,.075f,.94f),new(.04f,.10f),new(.96f,.61f));
        view.AddComponent<RectMask2D>();scroll=view.AddComponent<ScrollRect>();scroll.horizontal=true;
        scroll.scrollSensitivity=28f;
        scroll.movementType=ScrollRect.MovementType.Clamped;
        var body=new GameObject("TreeContent",typeof(RectTransform));body.transform.SetParent(view.transform,false);
        content=(RectTransform)body.transform;content.anchorMin=content.anchorMax=new(0,1);content.pivot=new(0,1);
        scroll.content=content;scroll.viewport=(RectTransform)view.transform;
        CastleTree.Changed+=Changed;
        Redraw();
    }
    private void Changed()=>dirty=true;
    private void OnDestroy()=>CastleTree.Changed-=Changed;
    private void OnEnable()=>dirty=true;
    private void OnRectTransformDimensionsChange(){dirty=true;focusPending=true;}
    private void Update()
    {
        if(content==null)return;
        if(Time.unscaledTime>=nextRefresh)
        {
            nextRefresh=Time.unscaledTime+.4f;
            string value=$"{UiReadability.Width}:{UiReadability.Height}:{GameManager.Instance?.StagePoints}:{GameManager.Instance?.GetHighestClearedStageId()}:{CastleTree.Slots}:{PlayerInventory.Instance?.GetUnlockedCharacters().Count}";
            if(value!=signature){signature=value;dirty=true;}
        }
        if(dirty)Redraw();
    }
    private void Redraw()
    {
        dirty=false;
        LayoutChrome();
        foreach(Transform child in content){child.gameObject.SetActive(false);Destroy(child.gameObject);}
        var nodes=CastleTree.Nodes.ToList();
        float scale=UiReadability.Scale(GetComponentInParent<Canvas>());
        if(!Mathf.Approximately(fontScale,scale))
        {
            foreach(var text in GetComponentsInChildren<TextMeshProUGUI>())
            {
                text.fontSizeMin*=fontScale/scale;text.fontSizeMax*=fontScale/scale;
            }
            fontScale=scale;
        }
        float width=210/scale,height=100/scale,level=154/scale;
        positions.Clear();float left=.5f;
        var hubs=new Vector2[5];
        for(int b=0;b<5;b++)
        {
            float start=left;
            foreach(var root in nodes.Where(n=>n.branch==b&&n.parent==null))
            {Layout(root,left,0,width,level,scale);left+=Span(root);}
            hubs[b]=new((start+left)*.5f*width,-150/scale);left+=1;
        }
        content.sizeDelta=new(Mathf.Max(scroll.viewport.rect.width,left*width),Mathf.Max(scroll.viewport.rect.height,-positions.Values.Min(p=>p.y)+170/scale));
        var origin=new Vector2(content.sizeDelta.x*.5f,-42/scale);
        for(int b=0;b<5;b++)
        {
            Link("BranchLink_"+b,origin,hubs[b],branchColors[b],scale);
            var hub=Surface(content,"BranchHub_"+b,branchColors[b]*.55f,Vector2.zero,Vector2.zero).GetComponent<RectTransform>();
            MapRect(hub,hubs[b],new(174/scale,48/scale));
            Text(hub,"Name",CastleTree.Branches[b],Vector2.zero,Vector2.one,24).alignment=TextAlignmentOptions.Center;
        }
        var home=Surface(content,"TreeOrigin",new(.33f,.36f,.22f),Vector2.zero,Vector2.zero).GetComponent<RectTransform>();
        MapRect(home,origin,new(174/scale,62/scale));Text(home,"Name","城下",Vector2.zero,Vector2.one,26).alignment=TextAlignmentOptions.Center;
        foreach(var node in nodes)
        {
            Vector2 start=node.parent==null?hubs[node.branch]+new Vector2(0,-24/scale):positions[node.parent]+new Vector2(0,-height*.5f);
            Link("Link_"+node.id,start,positions[node.id]+new Vector2(0,height*.5f),CastleTree.Owned(node)?branchColors[node.branch]:new(.32f,.39f,.35f),scale);
        }
        foreach(var node in nodes)
        {
            bool owned=CastleTree.Owned(node), available=CastleTree.Blocked(node).Length==0;
            var go=Surface(content,"TreeNode_"+node.id,owned?new(.15f,.30f,.23f):available?new(.27f,.25f,.15f):new(.14f,.16f,.16f),Vector2.zero,Vector2.zero);
            var rect=(RectTransform)go.transform;MapRect(rect,positions[node.id],new(width-26/scale,height));
            var button=go.AddComponent<Button>();button.onClick.AddListener(()=>{selected=node.id;dirty=true;ShowDetails();});
            var outline=go.AddComponent<Outline>();outline.effectColor=node.id==selected?new(.95f,.77f,.37f):new(.35f,.42f,.37f);outline.effectDistance=new(1,1);
            float labelLeft=.06f;
            if(node.kind==CastleTree.Kind.Character)
            {
                var icon=Surface(go.transform,"Piece",Color.white,new(.025f,.28f),new(.25f,.92f)).GetComponent<Image>();
                icon.sprite=CastleTree.Character(node)?.icon;icon.preserveAspect=true;icon.raycastTarget=false;labelLeft=.29f;
            }
            Text(go.transform,"Name",CastleTree.Name(node),new(labelLeft,.48f),new(.96f,.94f),22);
            string state=owned?"解放済":CastleTree.RequiredStage(node)>(GameManager.Instance?.GetHighestClearedStageId()??0)?$"第{CastleTree.RequiredStage(node)}局":$"戦果 {CastleTree.Cost(node)}";
            Text(go.transform,"State",state,new(.06f,.04f),new(.96f,.46f),18);
        }
        for(int b=0;b<5;b++)tabs.Find("TreeBranch_"+b).GetComponent<Image>().color=b==branch?new(.35f,.38f,.23f):new(.15f,.20f,.18f);
        ShowDetails();
        Canvas.ForceUpdateCanvases();
        if(focusPending&&positions.TryGetValue(selected,out var point))
        {
            scroll.StopMovement();
            content.anchoredPosition=new(Mathf.Clamp(scroll.viewport.rect.width*.5f-point.x,scroll.viewport.rect.width-content.sizeDelta.x,0),
                Mathf.Clamp(-point.y-scroll.viewport.rect.height*.4f,0,content.sizeDelta.y-scroll.viewport.rect.height));
            focusPending=false;
        }
    }
    private void LayoutChrome()
    {
        bool wide=UiReadability.Height<500&&UiReadability.Width>UiReadability.Height;
        void Place(RectTransform rect,Vector2 min,Vector2 max){rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        Place((RectTransform)title.transform.parent,wide?new(.72f,.10f):new(.04f,.71f),wide?new(.97f,.90f):new(.96f,.90f));
        Place((RectTransform)tabs,wide?new(.04f,.82f):new(.04f,.63f),wide?new(.69f,.91f):new(.96f,.69f));
        Place(scroll.viewport,new(.04f,.10f),wide?new(.69f,.80f):new(.96f,.61f));
        Place(title.rectTransform,wide?new(.04f,.75f):new(.035f,.72f),wide?new(.96f,.98f):new(.96f,.98f));
        Place(description.rectTransform,wide?new(.04f,.42f):new(.035f,.36f),wide?new(.96f,.74f):new(.96f,.72f));
        Place(status.rectTransform,wide?new(.04f,.19f):new(.035f,.04f),wide?new(.96f,.41f):new(.69f,.35f));
        Place(action.GetComponent<RectTransform>(),wide?new(.06f,.025f):new(.71f,.04f),wide?new(.94f,.17f):new(.97f,.34f));
    }
    private static float Span(CastleTree.Node node)=>Mathf.Max(1,CastleTree.Nodes.Where(n=>n.parent==node.id).Sum(Span));
    // Reserve each subtree's width so prerequisite paths spread without crossing.
    private void Layout(CastleTree.Node node,float left,int depth,float unit,float level,float scale)
    {
        positions[node.id]=new((left+Span(node)*.5f)*unit,-(300/scale+depth*level));
        foreach(var child in CastleTree.Nodes.Where(n=>n.parent==node.id)){Layout(child,left,depth+1,unit,level,scale);left+=Span(child);}
    }
    private void Link(string name,Vector2 a,Vector2 b,Color color,float scale)
    {
        float middle=(a.y+b.y)*.5f;
        Segment(name+"_a",a,new(a.x,middle),color,scale);Segment(name+"_b",new(a.x,middle),new(b.x,middle),color,scale);Segment(name+"_c",new(b.x,middle),b,color,scale);
    }
    private void Segment(string name,Vector2 a,Vector2 b,Color color,float scale)
    {
        var line=Surface(content,name,color,Vector2.zero,Vector2.zero).GetComponent<RectTransform>();
        MapRect(line,(a+b)*.5f,new(3/scale,Mathf.Max(3/scale,Vector2.Distance(a,b))));
        line.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(b.x-a.x,b.y-a.y)*Mathf.Rad2Deg);line.GetComponent<Image>().raycastTarget=false;
    }
    private static void MapRect(RectTransform rect,Vector2 point,Vector2 size){rect.anchorMin=rect.anchorMax=new(0,1);rect.pivot=new(.5f,.5f);rect.anchoredPosition=point;rect.sizeDelta=size;}
    private void ShowDetails()
    {
        var node=CastleTree.Nodes.First(n=>n.id==selected);
        title.text=$"{CastleTree.Name(node)}　編成 {GameManager.Instance.GetFacilityFormationSlots()}/8枠";
        description.text=node.kind switch
        {
            CastleTree.Kind.Character => CastleTree.Owned(node)?SkillDescription.GetShort(CastleTree.Character(node).skillType):"仲間加入　"+CastleTree.Branches[node.branch],
            CastleTree.Kind.Slot => "出撃できる素材のコマを1体追加",
            _ => FacilityDescription(node)
        };
        if(node.parent!=null)description.text+="　前提："+CastleTree.Name(CastleTree.Nodes.First(n=>n.id==node.parent));
        status.text=CastleTree.Status(node);
        action.interactable=CastleTree.Blocked(node).Length==0;
        action.GetComponentInChildren<TextMeshProUGUI>().text=CastleTree.Owned(node)?node.kind==CastleTree.Kind.Facility?"強化":"解放済":"解放";
    }
    private static string FacilityDescription(CastleTree.Node node)
    {
        var f=CastleTree.Facility(node);
        if(f==null)return "";
        string effect=f.effectType switch
        {
            FacilityEffectType.HealthBoost => "味方の最大HPを強化",
            FacilityEffectType.AttackBoost => "味方の攻撃力を強化",
            FacilityEffectType.SkillPowerBoost => "技能威力を強化",
            FacilityEffectType.SkillChanceBoost => "追加効果の発動率を強化",
            FacilityEffectType.PlayerLevelCap => "軍師のレベル上限を拡張",
            FacilityEffectType.BattleSpeed => "倍速の上限を拡張",
            FacilityEffectType.StageRetry => "再戦・自動連戦を解放",
            FacilityEffectType.Fusion => "開戦時の融合を解放",
            FacilityEffectType.Training => "獲得経験値を強化",
            FacilityEffectType.TrainingFrequency => "稽古の間隔を短縮",
            _ => "獲得戦果を強化"
        };
        return $"Lv.{FacilityManager.Instance.GetLevel(f)} / {FacilityManager.Instance.GetCurrentFacilityMaxLevel(f)}　{effect}";
    }
    private static GameObject Surface(Transform parent,string name,Color color,Vector2 min,Vector2 max)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);
        go.GetComponent<Image>().color=color;var r=(RectTransform)go.transform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;return go;
    }
    private static TextMeshProUGUI Text(Transform parent,string name,string value,Vector2 min,Vector2 max,float size)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
        var t=go.GetComponent<TextMeshProUGUI>();UnityUIRuntimeTheme.EnsureJapaneseCapableFont(t);
        var r=t.rectTransform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
        float scale=UiReadability.Scale(t.GetComponentInParent<Canvas>());
        t.text=value;t.enableAutoSizing=true;t.fontSizeMin=Mathf.Min(size,16)/scale;t.fontSizeMax=size/scale;
        t.color=new(.94f,.96f,.91f);t.raycastTarget=false;t.alignment=TextAlignmentOptions.MidlineLeft;return t;
    }
    private static Button Command(Transform parent,string name,string value,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action)
    {
        var go=Surface(parent,name,new(.22f,.28f,.24f),min,max);var b=go.AddComponent<Button>();b.onClick.AddListener(action);
        var t=Text(go.transform,"Label",value,new(.03f,.03f),new(.97f,.97f),18);t.alignment=TextAlignmentOptions.Center;return b;
    }
}
