using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerSkillSelectionView : IDisposable
{
    private readonly GameObject root;
    private readonly PlayerSkillRules.Plan draft;
    private readonly int cleared;
    private readonly TMP_InputField turnInput;
    private readonly Button apply;
    private readonly TextMeshProUGUI detail;
    private readonly List<(Toggle toggle, int id)> options = new();
    private float canvasScale;

    public PlayerSkillSelectionView(Transform parent, PlayerSkillRules.Plan plan, int cleared,
        Action<PlayerSkillRules.Plan> commit, Action cancel)
    {
        draft=plan.Copy(); this.cleared=cleared;
        root = Surface(parent, "PlayerSkillEditor", new(.07f,.10f,.09f,1f), new(.03f,.05f), new(.97f,.97f));
        canvasScale=root.GetComponentInParent<Canvas>().rootCanvas.scaleFactor;
        Label(root.transform, "Title", "プレイヤースキル", new(.05f,.90f), new(.95f,.98f), 26);
        var group = root.AddComponent<ToggleGroup>(); group.allowSwitchOff = false;
        for (int i=0; i<PlayerSkillRules.Definitions.Count; i++)
        {
            var definition=PlayerSkillRules.Definitions[i];
            float top=.88f-i*.105f;
            var row=Surface(root.transform, "PlayerSkillOption_"+definition.id, new(.16f,.22f,.18f), new(.05f,top-.09f),new(.95f,top));
            var toggle=row.AddComponent<Toggle>(); toggle.group=group;
            var box=Surface(row.transform,"Box",new(.35f,.38f,.30f),new(.025f,.25f),new(.095f,.75f));
            var check=Surface(box.transform,"Check",new(.95f,.78f,.35f),new(.12f,.12f),new(.88f,.88f));
            toggle.targetGraphic=row.GetComponent<Image>(); toggle.graphic=check.GetComponent<Image>();
            bool unlocked=cleared>=definition.unlockStage;
            toggle.interactable=unlocked;
            Label(row.transform,"Label",definition.name+(unlocked ? "" : $"　第{definition.unlockStage}局突破"),new(.13f,.04f),new(.97f,.96f),22);
            options.Add((toggle,definition.id));
            toggle.SetIsOnWithoutNotify(draft.skillId==definition.id);
            toggle.onValueChanged.AddListener(on=>{ if(on){draft.skillId=definition.id; Refresh();} });
        }
        detail=Label(root.transform,"SkillDetail","",new(.05f,.36f),new(.95f,.46f),20);
        detail.textWrappingMode=TextWrappingModes.NoWrap;
        Label(root.transform,"TurnCaption",$"発動ターン（1〜{PlayerSkillRules.MaxTurns}）",new(.05f,.27f),new(.95f,.35f),21);
        Command("PreviousPlayerSkillTurn","−",new(.15f,.16f),new(.32f,.26f),()=>Step(-1));
        Command("NextPlayerSkillTurn","＋",new(.68f,.16f),new(.85f,.26f),()=>Step(1));
        var field=Surface(root.transform,"PlayerSkillTurn",new(.10f,.14f,.12f),new(.35f,.16f),new(.65f,.26f));
        var text=Label(field.transform,"Value","",new(.04f,.04f),new(.96f,.96f),24);
        turnInput=field.AddComponent<TMP_InputField>(); turnInput.textViewport=(RectTransform)field.transform; turnInput.textComponent=text;
        turnInput.contentType=TMP_InputField.ContentType.IntegerNumber; turnInput.characterLimit=2;
        turnInput.SetTextWithoutNotify(draft.turn.ToString());
        turnInput.onValueChanged.AddListener(value=>
        {
            draft.turn=int.TryParse(value,out int number)?number:0; Refresh();
        });
        Command("CancelPlayerSkill","戻る",new(.05f,.035f),new(.48f,.13f),cancel);
        apply=Command("ApplyPlayerSkill","決定",new(.52f,.035f),new(.95f,.13f),()=>commit(draft.Copy()));
        Refresh();
    }
    private void Step(int delta)
    {
        draft.turn=Mathf.Clamp(draft.turn+delta,1,PlayerSkillRules.MaxTurns);
        turnInput.SetTextWithoutNotify(draft.turn.ToString()); Refresh();
    }
    private void Refresh()
    {
        foreach(var option in options) option.toggle.SetIsOnWithoutNotify(option.id==draft.skillId);
        if(detail!=null) detail.text=PlayerSkillRules.Get(draft.skillId)?.Detail ?? "保存したスキルを選び直してください";
        if(apply!=null) apply.interactable=PlayerSkillRules.IsValid(draft,cleared);
        if(turnInput!=null) turnInput.interactable=draft.skillId!=0;
    }
    private Button Command(string name,string value,Vector2 min,Vector2 max,Action action)
    {
        var go=Surface(root.transform,name,new(.26f,.30f,.23f),min,max);
        var button=go.AddComponent<Button>(); button.onClick.AddListener(()=>action());
        Label(go.transform,"Label",value,new(.03f,.03f),new(.97f,.97f),22); return button;
    }
    private static GameObject Surface(Transform parent,string name,Color color,Vector2 min,Vector2 max)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)); go.transform.SetParent(parent,false);
        go.GetComponent<Image>().color=color; var rect=(RectTransform)go.transform;
        rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;return go;
    }
    private static TextMeshProUGUI Label(Transform parent,string name,string value,Vector2 min,Vector2 max,float size)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);
        var text=go.GetComponent<TextMeshProUGUI>();UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
        float scale=text.GetComponentInParent<Canvas>().rootCanvas.scaleFactor;
        text.text=value;text.raycastTarget=false;text.color=new(.97f,.95f,.85f);text.alignment=TextAlignmentOptions.Center;
        text.enableAutoSizing=true;text.fontSizeMin=12/scale;text.fontSizeMax=size/scale;
        text.rectTransform.anchorMin=min;text.rectTransform.anchorMax=max;text.rectTransform.offsetMin=text.rectTransform.offsetMax=Vector2.zero;
        return text;
    }
    public void BringToFront()
    {
        root.transform.SetAsLastSibling();
        float scale=root.GetComponentInParent<Canvas>().rootCanvas.scaleFactor;
        if(Mathf.Approximately(scale,canvasScale)) return;
        foreach(var text in root.GetComponentsInChildren<TextMeshProUGUI>())
        {
            text.fontSizeMin*=canvasScale/scale;
            text.fontSizeMax*=canvasScale/scale;
        }
        canvasScale=scale;
    }
    public void Dispose(){root.SetActive(false);UnityEngine.Object.Destroy(root);}
}
