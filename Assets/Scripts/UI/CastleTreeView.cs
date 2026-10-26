using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class CastleTreeView : MonoBehaviour
{
    private RectTransform content,toolbar;
    private Image detailIcon;
    private TextMeshProUGUI title,description,status,zoomText;
    private Button action,respec,zoomOut,zoomIn;
    private GameObject resetDialog;
    private ScrollRect scroll;
    private string selected="c11",signature;
    private float fontScale,lastZoom=.5f,zoom=.5f,nextRefresh;
    private bool dirty=true,centerPending=true;
    private Vector2 requestedCenter;
    private CastleTreeMapLayout map;
    private readonly Color[] colors={new(.76f,.46f,.36f),new(.40f,.67f,.61f),new(.77f,.67f,.35f),new(.42f,.65f,.40f),new(.59f,.55f,.73f)};
    public float Zoom=>zoom;
    public int VisibleCount=>CastleTree.Nodes.Count(CastleTree.Visible);

    private void Start()
    {
        Place((RectTransform)transform,Vector2.zero,Vector2.one);
        fontScale=UiReadability.Scale(GetComponentInParent<Canvas>());
        var detail=Surface(transform,"TreeDetail",new(.065f,.11f,.09f,1),new(.025f,.70f),new(.975f,.91f));
        detailIcon=Surface(detail.transform,"SelectedIcon",Color.white,new(.015f,.10f),new(.23f,.90f)).GetComponent<Image>();
        detailIcon.preserveAspect=true;detailIcon.raycastTarget=false;
        title=Text(detail.transform,"Title","城下",new(.25f,.67f),new(.98f,.98f),24);
        description=Text(detail.transform,"Description","",new(.25f,.30f),new(.98f,.67f),19);
        status=Text(detail.transform,"Status","",new(.25f,.02f),new(.72f,.30f),18);
        action=Command(detail.transform,"TreePurchase","解放",new(.74f,.02f),new(.98f,.29f),()=>
        {if(CastleTree.Purchase(selected))GameAudio.Instance.Play(GameSound.Click);dirty=true;});
        var view=Surface(transform,"TreeViewport",new(.045f,.075f,.065f,1),new(.025f,.10f),new(.975f,.68f));
        view.AddComponent<RectMask2D>();scroll=view.AddComponent<ScrollRect>();
        scroll.horizontal=true;scroll.vertical=true;scroll.inertia=false;scroll.scrollSensitivity=28;
        scroll.movementType=ScrollRect.MovementType.Clamped;
        var body=new GameObject("TreeContent",typeof(RectTransform));body.transform.SetParent(view.transform,false);
        content=(RectTransform)body.transform;content.anchorMin=content.anchorMax=content.pivot=new(.5f,.5f);
        scroll.content=content;scroll.viewport=(RectTransform)view.transform;
        toolbar=(RectTransform)Surface(transform,"TreeMapTools",new(.08f,.13f,.10f,1),Vector2.zero,Vector2.one).transform;
        zoomOut=Command(toolbar,"TreeZoomOut","−",new(0,0),new(.24f,1),()=>SetZoom(zoom-.1f),"縮小");
        zoomText=Text(toolbar,"Zoom","50%",new(.25f,0),new(.51f,1),18);zoomText.alignment=TextAlignmentOptions.Center;
        zoomIn=Command(toolbar,"TreeZoomIn","＋",new(.52f,0),new(.76f,1),()=>SetZoom(zoom+.1f),"拡大");
        Command(toolbar,"TreeCenter","◎",new(.77f,0),Vector2.one,()=>Center(Vector2.zero),"城下へ戻る");
        respec=Command(transform,"TreeRespec","育成の振り直し",Vector2.zero,Vector2.one,ConfirmRespec);
        CastleTree.Changed+=Changed;Redraw();
    }
    private void Changed()=>dirty=true;
    private void OnDestroy()=>CastleTree.Changed-=Changed;
    private void OnEnable()=>dirty=true;
    private void OnDisable(){if(resetDialog!=null)Destroy(resetDialog);}
    private void ConfirmRespec()
    {
        if(resetDialog!=null||CharacterInvestment.Refund==0)return;
        resetDialog=Surface(transform,"TreeRespecDialog",new(0,0,0,.94f),Vector2.zero,Vector2.one);
        Text(resetDialog.transform,"Question","キャラ強化を振り直しますか？",new(.06f,.65f),new(.94f,.80f),25);
        Text(resetDialog.transform,"Refund",$"戦果 {CharacterInvestment.Refund} を全額返却\n仲間・編成枠・機能解放はそのまま",new(.06f,.40f),new(.94f,.63f),21);
        Command(resetDialog.transform,"TreeRespecCancel","戻る",new(.08f,.19f),new(.46f,.32f),()=>Destroy(resetDialog));
        Command(resetDialog.transform,"TreeRespecConfirm","振り直す",new(.54f,.19f),new(.92f,.32f),()=>
        {CharacterInvestment.Respec();Destroy(resetDialog);dirty=true;});
    }
    private void OnRectTransformDimensionsChange()=>dirty=true;
    private void SetZoom(float value){zoom=Mathf.Clamp(value,.5f,1.25f);dirty=true;}
    private void Center(Vector2 point){requestedCenter=point;centerPending=true;dirty=true;}
    private void Update()
    {
        if(content==null)return;
        if(Time.unscaledTime>=nextRefresh)
        {
            nextRefresh=Time.unscaledTime+.4f;
            string value=$"{UiReadability.Width}:{UiReadability.Height}:{GameManager.Instance?.StagePoints}:{GameManager.Instance?.GetHighestClearedStageId()}:{CastleTree.Slots}:{PlayerInventory.Instance?.GetUnlockedCharacters().Count}";
            if(signature!=value){signature=value;dirty=true;}
        }
        if(dirty)Redraw();
    }
    private void Redraw()
    {
        dirty=false;
        Vector2 worldCenter=content!=null?-content.anchoredPosition*fontScale/lastZoom:Vector2.zero;
        foreach(Transform child in content){child.gameObject.SetActive(false);Destroy(child.gameObject);}
        float scale=UiReadability.Scale(GetComponentInParent<Canvas>());
        if(!Mathf.Approximately(fontScale,scale))
        {
            foreach(var text in GetComponentsInChildren<TextMeshProUGUI>())
            {text.fontSizeMin*=fontScale/scale;text.fontSizeMax*=fontScale/scale;}
            fontScale=scale;
        }
        LayoutChrome();
        var nodes=CastleTree.Nodes.Where(CastleTree.Visible).ToList();
        map=new CastleTreeMapLayout(nodes);
        float maxX=map.Positions.Count==0?250:Mathf.Max(250,map.Positions.Values.Max(p=>Mathf.Abs(p.x))+90);
        float maxY=map.Positions.Count==0?250:Mathf.Max(250,map.Positions.Values.Max(p=>Mathf.Abs(p.y))+90);
        content.sizeDelta=scroll.viewport.rect.size+new Vector2(maxX,maxY)*2*zoom/scale;
        foreach(var b in Enumerable.Range(0,5).Where(b=>nodes.Any(n=>n.branch==b)))
        {
            Link("BranchLink_"+b,Vector2.zero,map.Hubs[b],colors[b]);
            var hub=Surface(content,"TreeBranch_"+b,new(.12f,.19f,.15f),Vector2.zero,Vector2.zero);
            MapRect((RectTransform)hub.transform,map.Hubs[b],new(b==2?90:62,32));
            int branch=b;
            hub.AddComponent<Button>().onClick.AddListener(()=>
            {
                var node=nodes.FirstOrDefault(n=>n.branch==branch&&!CastleTree.Owned(n))??nodes.First(n=>n.branch==branch);
                selected=node.id;Center(map.Positions[node.id]);
            });
            Text(hub.transform,"Name",CastleTree.Branches[b],new(.02f,0),new(.98f,1),17).alignment=TextAlignmentOptions.Center;
        }
        foreach(var node in nodes)
        {
            var start=node.parent!=null&&map.Positions.TryGetValue(node.parent,out var parent)?parent:map.Hubs[node.branch];
            Link("Link_"+node.id,start,map.Positions[node.id],CastleTree.Owned(node)?colors[node.branch]:new(.33f,.42f,.36f));
        }
        var home=Surface(content,"TreeOrigin",Color.clear,Vector2.zero,Vector2.zero);
        MapRect((RectTransform)home.transform,Vector2.zero,new(84,84));
        var homeIcon=Icon(home.transform,"Castle",CastleTreeArt.Building("FacilityRankTower"),new(.02f,.16f),new(.98f,1));
        home.AddComponent<Button>().onClick.AddListener(()=>{selected=null;dirty=true;});
        Text(home.transform,"Name","城下",new(0,0),new(1,.32f),18).alignment=TextAlignmentOptions.Center;
        foreach(var node in nodes)
        {
            bool owned=CastleTree.Owned(node),available=CastleTree.Blocked(node).Length==0;
            var go=Surface(content,"TreeNode_"+node.id,owned?new(.15f,.29f,.21f):new(.10f,.15f,.12f),Vector2.zero,Vector2.zero);
            float side=Mathf.Max(46,(node.kind==CastleTree.Kind.Facility?96:76)*zoom);
            MapRect((RectTransform)go.transform,map.Positions[node.id],new(side,side));
            go.AddComponent<Button>().onClick.AddListener(()=>{selected=node.id;dirty=true;});
            var outline=go.AddComponent<Outline>();outline.effectDistance=new(2,2);
            outline.effectColor=node.id==selected?new(1,.81f,.28f):available?colors[node.branch]:new(.29f,.36f,.32f);
            if(node.kind==CastleTree.Kind.Slot)DrawSlot(go.transform);
            else
            {
                var sprite=node.kind==CastleTree.Kind.Character?CastleTree.Character(node)?.icon:
                    node.kind==CastleTree.Kind.Upgrade?CastleTreeArt.Investment(node.stat):CastleTreeArt.Facility(CastleTree.Facility(node));
                var icon=Icon(go.transform,"NodeIcon",sprite,new(.05f,.05f),new(.95f,.95f));
                icon.color=owned||available?Color.white:new(.64f,.69f,.65f);
            }
            if(owned)Surface(go.transform,"OwnedMark",new(.44f,.78f,.49f),new(.76f,.02f),new(.98f,.10f)).GetComponent<Image>().raycastTarget=false;
        }
        ShowDetails();zoomText.text=Mathf.RoundToInt(zoom*100)+"%";
        zoomOut.interactable=zoom>.501f;zoomIn.interactable=zoom<1.249f;
        respec.interactable=CharacterInvestment.Refund>0;
        Canvas.ForceUpdateCanvases();scroll.StopMovement();
        Vector2 center=centerPending?requestedCenter:worldCenter;
        content.anchoredPosition=-center*zoom/scale;
        centerPending=false;lastZoom=zoom;
    }
    private void LayoutChrome()
    {
        bool compact=UiReadability.Height<500;
        Place((RectTransform)title.transform.parent,new(.025f,compact?.68f:.70f),new(.975f,.91f));
        Place(scroll.viewport,new(.025f,.10f),new(.975f,compact?.66f:.68f));
        if(compact)
        {
            Place(title.rectTransform,new(.15f,.50f),new(.74f,.98f));
            Place(description.rectTransform,new(.15f,.02f),new(.74f,.50f));
            Place(status.rectTransform,new(.76f,.50f),new(.99f,.98f));
            Place(action.GetComponent<RectTransform>(),new(.76f,.04f),new(.99f,.46f));
            Place(detailIcon.rectTransform,new(.01f,.04f),new(.14f,.96f));
        }
        else
        {
            Place(title.rectTransform,new(.25f,.67f),new(.98f,.98f));
            Place(description.rectTransform,new(.25f,.30f),new(.98f,.67f));
            Place(status.rectTransform,new(.25f,.02f),new(.72f,.30f));
            Place(action.GetComponent<RectTransform>(),new(.74f,.02f),new(.98f,.29f));
            Place(detailIcon.rectTransform,new(.015f,.10f),new(.23f,.90f));
        }
        toolbar.anchorMin=toolbar.anchorMax=new(.975f,.10f);toolbar.pivot=new(1,0);
        toolbar.sizeDelta=new Vector2(176,40)/fontScale;toolbar.anchoredPosition=new Vector2(-6,6)/fontScale;
        var resetRect=respec.GetComponent<RectTransform>();
        resetRect.anchorMin=resetRect.anchorMax=new(.025f,.10f);resetRect.pivot=new(0,0);
        resetRect.sizeDelta=new Vector2(156,40)/fontScale;resetRect.anchoredPosition=new Vector2(6,6)/fontScale;
    }
    private void DrawSlot(Transform parent)
    {
        var db=Resources.Load<CharacterDatabase>("CharacterDatabase");
        for(int i=0;i<3;i++)
        {
            var icon=Icon(parent,"SlotPiece_"+i,db.GetById(new[]{1,10,20}[i]).icon,new(.04f+i*.22f,.30f),new(.52f+i*.22f,.96f));
            icon.color=new(1,1,1,.30f);
        }
        Text(parent,"SlotPlusOne","+1",Vector2.zero,Vector2.one,25).alignment=TextAlignmentOptions.Center;
    }
    private void ShowDetails()
    {
        var node=CastleTree.Nodes.FirstOrDefault(n=>n.id==selected);
        if(node==null||!CastleTree.Visible(node))
        {
            selected=null;title.text="城下";description.text=$"編成 {CastleTree.Slots} / {CastleTree.MaximumSlots}枠";
            status.text=$"戦果 {GameManager.Instance.StagePoints}";action.gameObject.SetActive(false);
            detailIcon.sprite=CastleTreeArt.Building("FacilityRankTower");return;
        }
        title.text=CastleTree.Name(node);action.gameObject.SetActive(true);
        description.text=node.kind switch
        {
            CastleTree.Kind.Character=>CastleTree.Owned(node)?SkillDescription.GetShort(CastleTree.Character(node).skillType):"仲間加入・"+CastleTree.Branches[node.branch],
            CastleTree.Kind.Slot=>CastleTree.Owned(node)?"素材のコマを1体追加・取得済":$"編成 {CastleTree.Slots} → {Mathf.Min(8,CastleTree.Slots+1)}枠",
            CastleTree.Kind.Upgrade=>CharacterInvestment.Description(node.stat),
            _=>FacilityDescription(node)
        };
        status.text=CastleTree.Status(node);action.interactable=CastleTree.Blocked(node).Length==0;
        action.GetComponentInChildren<TextMeshProUGUI>().text=CastleTree.Owned(node)?node.kind==CastleTree.Kind.Facility?"強化":"取得済":"解放";
        detailIcon.sprite=node.kind is CastleTree.Kind.Character or CastleTree.Kind.Upgrade?CastleTree.Character(node)?.icon:
            node.kind==CastleTree.Kind.Slot?CastleTreeArt.Building("FacilityBarracks"):CastleTreeArt.Facility(CastleTree.Facility(node));
    }
    private static string FacilityDescription(CastleTree.Node node)
    {
        var f=CastleTree.Facility(node);if(f==null)return "";
        string effect=f.effectType switch
        {
            FacilityEffectType.HealthBoost=>"最大HPを強化",
            FacilityEffectType.AttackBoost=>"攻撃力を強化",
            FacilityEffectType.SkillPowerBoost=>"技能威力を強化",
            FacilityEffectType.SkillChanceBoost=>"発動率を強化",
            FacilityEffectType.PlayerLevelCap=>"軍師レベル上限",
            FacilityEffectType.BattleSpeed=>"倍速の上限",
            FacilityEffectType.StageRetry=>"再戦・自動連戦",
            FacilityEffectType.Fusion=>"開戦時の融合",
            FacilityEffectType.Training=>"獲得経験値を強化",
            FacilityEffectType.TrainingFrequency=>"稽古を速める",
            _=>"獲得戦果を強化"
        };
        return $"Lv.{FacilityManager.Instance.GetLevel(f)} / {FacilityManager.Instance.GetCurrentFacilityMaxLevel(f)}　{effect}";
    }
    private void Link(string name,Vector2 a,Vector2 b,Color color)
    {
        var line=Surface(content,name,color,Vector2.zero,Vector2.zero).GetComponent<RectTransform>();
        MapRect(line,(a+b)*.5f,new(3,Vector2.Distance(a,b)*zoom));
        line.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(b.x-a.x,b.y-a.y)*Mathf.Rad2Deg);line.GetComponent<Image>().raycastTarget=false;
    }
    private void MapRect(RectTransform rect,Vector2 point,Vector2 pixels)
    {rect.anchorMin=rect.anchorMax=rect.pivot=new(.5f,.5f);rect.anchoredPosition=point*zoom/fontScale;rect.sizeDelta=pixels/fontScale;}
    private static Image Icon(Transform parent,string name,Sprite sprite,Vector2 min,Vector2 max)
    {
        var icon=Surface(parent,name,Color.white,min,max).GetComponent<Image>();
        icon.sprite=sprite;icon.preserveAspect=true;icon.raycastTarget=false;return icon;
    }
    private static GameObject Surface(Transform parent,string name,Color color,Vector2 min,Vector2 max)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);
        go.GetComponent<Image>().color=color;Place((RectTransform)go.transform,min,max);return go;
    }
    private static void Place(RectTransform rect,Vector2 min,Vector2 max){rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;}
    private static TextMeshProUGUI Text(Transform parent,string name,string value,Vector2 min,Vector2 max,float size)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
        var text=go.GetComponent<TextMeshProUGUI>();UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);Place(text.rectTransform,min,max);
        float scale=UiReadability.Scale(text.GetComponentInParent<Canvas>());
        text.text=value;text.enableAutoSizing=true;text.fontSizeMin=Mathf.Min(size,16)/scale;text.fontSizeMax=size/scale;
        text.color=new(.95f,.97f,.91f);text.raycastTarget=false;text.alignment=TextAlignmentOptions.MidlineLeft;return text;
    }
    private static Button Command(Transform parent,string name,string value,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction clicked,string hint=null)
    {
        var go=Surface(parent,name,new(.20f,.28f,.23f),min,max);var button=go.AddComponent<Button>();button.onClick.AddListener(()=>{ContextualInfoTooltip.Hide();clicked();});
        Text(go.transform,"Label",value,new(.02f,.02f),new(.98f,.98f),20).alignment=TextAlignmentOptions.Center;
        if(hint!=null)
        {
            var trigger=go.AddComponent<EventTrigger>();
            var enter=new EventTrigger.Entry{eventID=EventTriggerType.PointerEnter};enter.callback.AddListener(_=>ContextualInfoTooltip.Show((RectTransform)go.transform,hint,""));
            var exit=new EventTrigger.Entry{eventID=EventTriggerType.PointerExit};exit.callback.AddListener(_=>ContextualInfoTooltip.Hide());
            trigger.triggers.Add(enter);trigger.triggers.Add(exit);
        }
        return button;
    }
}
