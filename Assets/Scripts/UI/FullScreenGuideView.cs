using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class FullScreenGuideView : MonoBehaviour,IDisposable
{
    private readonly Dictionary<TextMeshProUGUI,float> fontSizes=new();
    private float lastScale;
    private int lastWidth,lastHeight;
    private TutorialContent.Page currentPage;
    private bool isMenu;
    public static FullScreenGuideView Open(TutorialContent.Page page,Action continued,Action secondary=null,string primary="わかった",string secondaryText="戻る")
    {
        var view=Create();
        view.currentPage=page;
        var title=view.Label(view.transform,"GuideTitle",page.title,new(.07f,.80f),new(.93f,.95f),32);
        title.textWrappingMode=TextWrappingModes.NoWrap;
        var body=view.Label(view.transform,"GuideBody",page.body,new(.06f,.18f),new(.94f,.43f),28);
        body.alignment=TextAlignmentOptions.Center;
        view.DrawActors(page);
        view.Command(view.transform,"GuideContinue",primary,new(secondary==null?.22f:.52f,.055f),new(.88f,.14f),continued);
        if(secondary!=null)view.Command(view.transform,"GuideSkip",secondaryText,new(.12f,.055f),new(.47f,.14f),secondary);
        view.Rescale();return view;
    }
    public static FullScreenGuideView OpenMenu(IReadOnlyList<TutorialContent.Page> pages,Action<TutorialContent.Page> chosen,Action close)
    {
        var view=Create();
        view.isMenu=true;
        view.Label(view.transform,"GuideTitle","遊び方",new(.07f,.87f),new(.93f,.97f),28);
        for(int i=0;i<pages.Count;i++)
        {
            var page=pages[i];float top=.84f-i*.075f;
            view.Command(view.transform,"GuideHelp_"+page.id,page.title,new(.08f,top-.065f),new(.92f,top),()=>chosen(page));
        }
        view.Command(view.transform,"GuideContinue","設定に戻る",new(.22f,.04f),new(.78f,.12f),close);
        view.Rescale();return view;
    }
    private static FullScreenGuideView Create()
    {
        var canvas=FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(c=>c.isRootCanvas&&c.transform.parent==null);
        var go=Surface(canvas.transform,"FullScreenGuide",new(.045f,.075f,.065f,1),Vector2.zero,Vector2.one);
        var layer=go.AddComponent<Canvas>();layer.overrideSorting=true;layer.sortingOrder=2000;go.AddComponent<GraphicRaycaster>();
        var view=go.AddComponent<FullScreenGuideView>();
        var backdrop=new GameObject("Backdrop",typeof(RectTransform),typeof(CanvasRenderer),typeof(RawImage));backdrop.transform.SetParent(go.transform,false);
        Place((RectTransform)backdrop.transform,Vector2.zero,Vector2.one);
        var image=backdrop.GetComponent<RawImage>();image.texture=Resources.Load<Texture2D>("UI/ModernWafuu/HomeHeroLandscape");
        image.color=new(1,1,1,.16f);image.raycastTarget=false;
        return view;
    }
    private void DrawActors(TutorialContent.Page page)
    {
        var db=Resources.Load<CharacterDatabase>("CharacterDatabase");int count=page.actors.Length;
        for(int i=0;i<count;i++)
        {
            var data=db?.GetById(page.actors[i]);if(data==null)continue;
            var (min,max)=ActorBounds(page,i);
            var icon=Surface(transform,"GuidePiece_"+data.characterId,Color.white,min,max).GetComponent<Image>();
            icon.sprite=data.icon;icon.preserveAspect=true;icon.raycastTarget=false;
        }
        if(page.id=="fusion")
        {
            Label(transform,"MaterialPlus","＋",new(.31f,.53f),new(.39f,.68f),26);
            Label(transform,"HiddenFusion","→ ？",new(.65f,.51f),new(.92f,.70f),30);
        }
    }
    private static (Vector2 min,Vector2 max) ActorBounds(TutorialContent.Page page,int index)
    {
        bool shortScreen=UiReadability.Height<500&&UiReadability.Width>UiReadability.Height;
        float center=(index+.5f)/page.actors.Length;
        Vector2 min=new(.09f+center*.82f-.13f,shortScreen?.54f:.44f),max=new(.09f+center*.82f+.13f,shortScreen?.82f:.78f);
        if(page.id=="deployment")
        {
            min.y=shortScreen?index==0?.66f:.59f:index==0?.58f:.49f;
            max.y=shortScreen?index==0?.81f:.74f:index==0?.77f:.68f;
        }
        if(page.id=="fusion"){min.x=index==0?.10f:.40f;max.x=min.x+.20f;}
        return (min,max);
    }
    private TextMeshProUGUI Label(Transform parent,string name,string value,Vector2 min,Vector2 max,float size)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
        var text=go.GetComponent<TextMeshProUGUI>();UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
        Place(text.rectTransform,min,max);text.text=value;text.color=new(.97f,.96f,.87f);text.raycastTarget=false;
        text.alignment=TextAlignmentOptions.Center;text.enableAutoSizing=true;text.textWrappingMode=TextWrappingModes.Normal;
        fontSizes[text]=size;return text;
    }
    private void Command(Transform parent,string name,string label,Vector2 min,Vector2 max,Action action)
    {
        var go=Surface(parent,name,new(.28f,.35f,.23f,1),min,max);var button=go.AddComponent<Button>();
        button.navigation=new Navigation{mode=Navigation.Mode.None};
        if(name=="GuideContinue")UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(go);
        button.onClick.AddListener(()=>action?.Invoke());Label(go.transform,"Label",label,new(.025f,.04f),new(.975f,.96f),21);
    }
    private void Update()
    {
        float scale=UiReadability.Scale(GetComponentInParent<Canvas>());
        if(!Mathf.Approximately(scale,lastScale)||lastWidth!=Screen.width||lastHeight!=Screen.height)Rescale();
    }
    private void Rescale()
    {
        lastScale=UiReadability.Scale(GetComponentInParent<Canvas>());
        lastWidth=Screen.width;lastHeight=Screen.height;
        foreach(var item in fontSizes)
        {
            float size=item.Key.name=="GuideBody"&&UiReadability.Width<600?24:item.Value;
            item.Key.fontSizeMax=size/lastScale;
            item.Key.fontSizeMin=Mathf.Min(item.Value,item.Key.name=="GuideBody"?22:18)/lastScale;
        }
        if(isMenu)
        {
            bool columns=UiReadability.Height<500&&UiReadability.Width>UiReadability.Height;
            for(int i=0;i<TutorialContent.Pages.Count;i++)
            {
                float top=.84f-(columns?i/2:i)*(columns?.135f:.075f);
                float left=columns?.06f+(i%2)*.46f:.08f;
                Place(transform.Find("GuideHelp_"+TutorialContent.Pages[i].id) as RectTransform,new(left,top-(columns?.115f:.065f)),new(left+(columns?.42f:.84f),top));
            }
        }
        if(currentPage==null)return;
        bool shortScreen=UiReadability.Height<500&&UiReadability.Width>UiReadability.Height;
        Place(transform.Find("GuideBody") as RectTransform,new(.06f,.18f),new(.94f,shortScreen?.51f:.43f));
        Place(transform.Find("GuideTitle") as RectTransform,new(.07f,shortScreen?.83f:.80f),new(.93f,.97f));
        for(int i=0;i<currentPage.actors.Length;i++)
        {
            var (min,max)=ActorBounds(currentPage,i);
            var piece=transform.Find("GuidePiece_"+currentPage.actors[i]) as RectTransform;
            if(piece!=null)Place(piece,min,max);
            Place(transform.Find("GuideRole_"+i) as RectTransform,new(min.x,min.y-.06f),new(max.x,min.y));
        }
        if(currentPage.id=="fusion")
        {
            Place(transform.Find("MaterialPlus") as RectTransform,new(.31f,shortScreen?.62f:.53f),new(.39f,shortScreen?.77f:.68f));
            Place(transform.Find("HiddenFusion") as RectTransform,new(.65f,shortScreen?.60f:.51f),new(.92f,shortScreen?.81f:.70f));
        }
    }
    private static GameObject Surface(Transform parent,string name,Color color,Vector2 min,Vector2 max)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));go.transform.SetParent(parent,false);
        go.GetComponent<Image>().color=color;Place((RectTransform)go.transform,min,max);return go;
    }
    private static void Place(RectTransform rect,Vector2 min,Vector2 max)
    {if(rect==null)return;rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;}
    public void Dispose(){gameObject.SetActive(false);Destroy(gameObject);}
}
