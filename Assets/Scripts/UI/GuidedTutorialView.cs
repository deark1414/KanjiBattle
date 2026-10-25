using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class GuidedTutorialView : MonoBehaviour, IDisposable
{
    private string id;
    private int step;
    private Action completed;
    private RectTransform root, caption, marker,skipRect;
    private readonly RectTransform[] shades=new RectTransform[4];
    private TextMeshProUGUI copy;
    private Button target;
    private Button.ButtonClickedEvent original, wrapped;
    private RectTransform actionRect;
    private Vector2 originalSize;
    private readonly System.Collections.Generic.Dictionary<TMP_Text,Vector2> originalFonts=new();
    private Canvas canvas;
    private Image ghost;
    public int Step => step;
    public string Target => target!=null?target.name:"";
    public float InstructionPixels => copy!=null?copy.fontSize*UiReadability.Scale(canvas):0;

    public static GuidedTutorialView Open(string id,Action completed)
    {
        var canvas=FindObjectsByType<Canvas>(FindObjectsSortMode.None).First(c=>c.isRootCanvas&&c.transform.parent==null);
        var go=new GameObject("GuidedTutorial",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));
        go.transform.SetParent(canvas.transform,false);
        var view=go.AddComponent<GuidedTutorialView>();view.canvas=canvas;view.id=id;view.completed=completed;
        view.root=(RectTransform)go.transform;Place(view.root,Vector2.zero,Vector2.one);
        var layer=go.GetComponent<Canvas>();layer.overrideSorting=true;layer.sortingOrder=2000;
        for(int i=0;i<4;i++)view.shades[i]=view.Panel("InputBlock_"+i,new(0,0,0,.72f));
        view.marker=view.Panel("TargetHighlight",new(1,.81f,.24f,.10f));view.marker.GetComponent<Image>().raycastTarget=false;
        var outline=view.marker.gameObject.AddComponent<Outline>();outline.effectColor=new(1,.81f,.24f);outline.effectDistance=new(3,3);
        view.caption=view.Panel("Instruction",new(.05f,.12f,.10f,.98f));
        view.copy=view.Label(view.caption,"InstructionText",24);
        Place(view.copy.rectTransform,new(.05f,.12f),new(.95f,.88f));
        var skip=view.Panel("GuideSkip",new(.12f,.18f,.16f,1));view.skipRect=skip;Place(skip,new(.60f,.94f),new(.98f,.995f));
        skip.gameObject.AddComponent<Button>().onClick.AddListener(()=>view.completed?.Invoke());
        var skipText=view.Label(skip,"Label",18);skipText.text="案内を終了";Place(skipText.rectTransform,Vector2.zero,Vector2.one);
        var ghostRect=view.Panel("MoveDemonstration",Color.white);view.ghost=ghostRect.GetComponent<Image>();
        view.ghost.raycastTarget=false;view.ghost.preserveAspect=true;ghostRect.gameObject.SetActive(false);
        return view;
    }
    private Button Named(string name)=>FindObjectsByType<Button>(FindObjectsSortMode.None).FirstOrDefault(b=>b.name==name&&b.gameObject.activeInHierarchy);
    private void LateUpdate()
    {
        Place(skipRect,new(.60f,.995f-44f/UiReadability.Height),new(.98f,.995f));
        Button next=null;string text="";
        if(id=="formation") {next=Named("DeployButton");text="この3体で出陣しよう\n光っている「出陣」を押す";}
        if(id=="deployment")
        {
            var battle=FindAnyObjectByType<BattleManager>();
            next=battle!=null?battle.TutorialDeploymentTarget(step):null;
            text=step==0?"水は後ろから味方を支える\nまず「水」のマスを押す":step==1?"光っている後列へ動かそう\nコマ → 移動先の順に押す":"準備完了！ あとは自動で戦う\n「開戦」を押そう";
            if(battle!=null&&step==1)DrawMove(battle);
            else ghost.gameObject.SetActive(false);
        }
        if(next!=target)Bind(next);
        copy.text=text;
        float scale=UiReadability.Scale(canvas);
        copy.fontSizeMin=copy.fontSizeMax=(UiReadability.Width<600?22:26)/scale;
        if(target==null){foreach(var shade in shades)Place(shade,Vector2.zero,Vector2.one);return;}
        if(actionRect!=null)
        {
            actionRect.sizeDelta=originalSize;
            actionRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(48/scale,actionRect.rect.height));
            foreach(var label in originalFonts.Keys)if(label!=null)label.fontSizeMin=label.fontSizeMax=22/scale;
        }
        Vector2 min=Vector2.one,max=Vector2.zero;
        var corners=new Vector3[4];((RectTransform)target.transform).GetWorldCorners(corners);
        foreach(var corner in corners)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root,RectTransformUtility.WorldToScreenPoint(null,corner),null,out var local);
            var p=(local-root.rect.min)/root.rect.size;min=Vector2.Min(min,p);max=Vector2.Max(max,p);
        }
        // The opening is exactly the real control; all surrounding input is blocked.
        Place(shades[0],Vector2.zero,new(min.x,1));Place(shades[1],new(max.x,0),Vector2.one);
        Place(shades[2],new(min.x,0),new(max.x,min.y));Place(shades[3],new(min.x,max.y),new(max.x,1));
        Place(marker,min,max);marker.GetComponent<Image>().color=new(1,.81f,.24f,.12f+.10f*Mathf.Sin(Time.unscaledTime*5));
        float h=Mathf.Min(.28f,132f/UiReadability.Height);
        if((min.y+max.y)*.5f<.5f)Place(caption,new(.04f,.69f),new(.96f,.69f+h));
        else Place(caption,new(.04f,.12f),new(.96f,.12f+h));
    }
    private void Bind(Button next)
    {
        Unbind();target=next;if(target==null)return;
        if(target.name=="DeployButton"||target.name=="ConfirmDeployment")
        {
            actionRect=(RectTransform)target.transform;originalSize=actionRect.sizeDelta;
            foreach(var text in target.GetComponentsInChildren<TMP_Text>())originalFonts[text]=new(text.fontSizeMin,text.fontSizeMax);
        }
        original=target.onClick;wrapped=new Button.ButtonClickedEvent();target.onClick=wrapped;
        wrapped.AddListener(()=>
        {
            var invoke=original;
            bool final=id=="formation"||step==2;
            Unbind();
            if(final)completed?.Invoke();
            invoke.Invoke();
            if(!final)step++;
        });
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(target.gameObject);
    }
    private void Unbind()
    {
        if(target!=null&&target.onClick==wrapped)target.onClick=original;
        if(actionRect!=null)actionRect.sizeDelta=originalSize;
        foreach(var item in originalFonts)if(item.Key!=null){item.Key.fontSizeMin=item.Value.x;item.Key.fontSizeMax=item.Value.y;}
        originalFonts.Clear();actionRect=null;target=null;original=null;wrapped=null;
    }
    private void DrawMove(BattleManager battle)
    {
        var source=battle.TutorialDeploymentTarget(0);var dest=battle.TutorialDeploymentTarget(1);
        if(source==null||dest==null)return;
        ghost.gameObject.SetActive(true);ghost.sprite=Resources.Load<CharacterDatabase>("CharacterDatabase").GetById(20).icon;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root,RectTransformUtility.WorldToScreenPoint(null,source.transform.position),null,out var a);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(root,RectTransformUtility.WorldToScreenPoint(null,dest.transform.position),null,out var b);
        var rect=ghost.rectTransform;rect.anchorMin=rect.anchorMax=new(.5f,.5f);rect.pivot=new(.5f,.5f);
        rect.sizeDelta=new Vector2(52,64)/UiReadability.Scale(canvas);
        rect.anchoredPosition=Vector2.Lerp(a,b,Mathf.SmoothStep(0,1,(Time.unscaledTime*.65f)%1));ghost.color=new(1,1,1,.65f);
    }
    private RectTransform Panel(string name,Color color)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(transform,false);
        go.GetComponent<Image>().color=color;return (RectTransform)go.transform;
    }
    private TextMeshProUGUI Label(Transform parent,string name,float size)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
        var text=go.GetComponent<TextMeshProUGUI>();UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
        float scale=UiReadability.Scale(canvas);
        text.fontSize=size/scale;text.enableAutoSizing=true;text.fontSizeMin=18/scale;text.fontSizeMax=size/scale;
        text.alignment=TextAlignmentOptions.Center;text.color=Color.white;text.raycastTarget=false;return text;
    }
    private static void Place(RectTransform rect,Vector2 min,Vector2 max){rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;}
    public void Dispose(){Unbind();gameObject.SetActive(false);Destroy(gameObject);}
    private void OnDestroy()=>Unbind();
}
