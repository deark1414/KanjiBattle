using System.Linq;
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
    private void Start()
    {
        var root=(RectTransform)transform;
        fontScale=GetComponentInParent<Canvas>().rootCanvas.scaleFactor;
        root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
        var detail=Surface(transform,"TreeDetail",new(.08f,.12f,.10f,.97f),new(.04f,.71f),new(.96f,.90f));
        title=Text(detail.transform,"Title","城下",new(.035f,.72f),new(.96f,.98f),22);
        description=Text(detail.transform,"Description","",new(.035f,.36f),new(.96f,.72f),16);
        status=Text(detail.transform,"Status","",new(.035f,.04f),new(.69f,.35f),15);
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
            {branch=index;selected=CastleTree.Nodes.First(n=>n.branch==index).id;dirty=true;scroll.verticalNormalizedPosition=1;});
        }
        var view=Surface(transform,"TreeViewport",new(.055f,.085f,.075f,.94f),new(.04f,.10f),new(.96f,.61f));
        view.AddComponent<RectMask2D>();scroll=view.AddComponent<ScrollRect>();scroll.horizontal=false;
        scroll.scrollSensitivity=28f;
        scroll.movementType=ScrollRect.MovementType.Clamped;
        var body=new GameObject("TreeContent",typeof(RectTransform));body.transform.SetParent(view.transform,false);
        content=(RectTransform)body.transform;content.anchorMin=new(0,1);content.anchorMax=new(1,1);content.pivot=new(.5f,1);
        scroll.content=content;scroll.viewport=(RectTransform)view.transform;
        CastleTree.Changed+=Changed;
        Redraw();
    }
    private void Changed()=>dirty=true;
    private void OnDestroy()=>CastleTree.Changed-=Changed;
    private void OnEnable()=>dirty=true;
    private void OnRectTransformDimensionsChange()=>dirty=true;
    private void Update()
    {
        if(content==null)return;
        if(Time.unscaledTime>=nextRefresh)
        {
            nextRefresh=Time.unscaledTime+.4f;
            string value=$"{Screen.width}:{Screen.height}:{GameManager.Instance?.StagePoints}:{GameManager.Instance?.GetHighestClearedStageId()}:{CastleTree.Slots}:{PlayerInventory.Instance?.GetUnlockedCharacters().Count}";
            if(value!=signature){signature=value;dirty=true;}
        }
        if(dirty)Redraw();
    }
    private void Redraw()
    {
        dirty=false;
        foreach(Transform child in content){child.gameObject.SetActive(false);Destroy(child.gameObject);}
        bool portrait=UnityUIRuntimeTheme.IsPortraitNarrowScreen();
        var nodes=CastleTree.Nodes.Where(n=>!portrait||n.branch==branch).ToList();
        float scale=GetComponentInParent<Canvas>().rootCanvas.scaleFactor;
        if(!Mathf.Approximately(fontScale,scale))
        {
            foreach(var text in GetComponentsInChildren<TextMeshProUGUI>())
            {
                text.fontSizeMin*=fontScale/scale;text.fontSizeMax*=fontScale/scale;
            }
            fontScale=scale;
        }
        int columns=portrait?1:5;
        float width=scroll.viewport.rect.width/columns;
        float height=(Screen.height<500?76:106)/scale, gap=20/scale;
        int maxRows=nodes.GroupBy(n=>n.branch).Max(g=>g.Count());
        content.sizeDelta=new(0, maxRows*(height+gap)+20/scale);
        var position=new System.Collections.Generic.Dictionary<string,Vector2>();
        for(int b=0;b<5;b++)
        {
            var lane=nodes.Where(n=>n.branch==b).ToList();
            for(int row=0;row<lane.Count;row++)position[lane[row].id]=new((portrait?0:b)*width+width*.5f,-(row*(height+gap)+10/scale));
        }
        foreach(var node in nodes)
        {
            if(node.parent==null||!position.ContainsKey(node.parent))continue;
            Vector2 start=position[node.parent]+new Vector2(0,-height*.5f), end=position[node.id]+new Vector2(0,-height*.5f);
            var line=Surface(content,"Link_"+node.id,new(.46f,.53f,.40f),Vector2.zero,Vector2.zero).GetComponent<RectTransform>();
            line.anchorMin=line.anchorMax=new(0,1);line.pivot=new(.5f,.5f);
            line.anchoredPosition=(start+end)*.5f;
            line.sizeDelta=new(2/scale,Vector2.Distance(start,end));
            line.localRotation=Quaternion.Euler(0,0,-Mathf.Atan2(end.x-start.x,end.y-start.y)*Mathf.Rad2Deg);
            line.GetComponent<Image>().raycastTarget=false;
        }
        foreach(var node in nodes)
        {
            bool owned=CastleTree.Owned(node), available=CastleTree.Blocked(node).Length==0;
            var go=Surface(content,"TreeNode_"+node.id,owned?new(.15f,.30f,.23f):available?new(.27f,.25f,.15f):new(.14f,.16f,.16f),Vector2.zero,Vector2.zero);
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=new(0,1);rect.pivot=new(.5f,1);
            rect.anchoredPosition=position[node.id];rect.sizeDelta=new(width-18/scale,height);
            var button=go.AddComponent<Button>();button.onClick.AddListener(()=>{selected=node.id;dirty=true;ShowDetails();});
            var outline=go.AddComponent<Outline>();outline.effectColor=node.id==selected?new(.95f,.77f,.37f):new(.35f,.42f,.37f);outline.effectDistance=new(1,1);
            float labelLeft=.06f;
            if(node.kind==CastleTree.Kind.Character)
            {
                var icon=Surface(go.transform,"Piece",Color.white,new(.025f,.28f),new(.25f,.92f)).GetComponent<Image>();
                icon.sprite=CastleTree.Character(node)?.icon;icon.preserveAspect=true;icon.raycastTarget=false;labelLeft=.29f;
            }
            Text(go.transform,"Name",CastleTree.Name(node),new(labelLeft,.48f),new(.96f,.94f),18);
            Text(go.transform,"State",CastleTree.Status(node),new(.06f,.04f),new(.96f,.46f),13);
        }
        for(int b=0;b<5;b++)tabs.Find("TreeBranch_"+b).GetComponent<Image>().color=b==branch?new(.35f,.38f,.23f):new(.15f,.20f,.18f);
        ShowDetails();
    }
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
        float scale=t.GetComponentInParent<Canvas>().rootCanvas.scaleFactor;
        t.text=value;t.enableAutoSizing=true;t.fontSizeMin=11/scale;t.fontSizeMax=size/scale;
        t.color=new(.94f,.96f,.91f);t.raycastTarget=false;t.alignment=TextAlignmentOptions.MidlineLeft;return t;
    }
    private static Button Command(Transform parent,string name,string value,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action)
    {
        var go=Surface(parent,name,new(.22f,.28f,.24f),min,max);var b=go.AddComponent<Button>();b.onClick.AddListener(action);
        var t=Text(go.transform,"Label",value,new(.03f,.03f),new(.97f,.97f),18);t.alignment=TextAlignmentOptions.Center;return b;
    }
}
