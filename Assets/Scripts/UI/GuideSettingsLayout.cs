using TMPro;
using UnityEngine;

public sealed class GuideSettingsLayout : MonoBehaviour
{
    private void LateUpdate()
    {
        var rect=(RectTransform)transform;
        bool portrait=UnityUIRuntimeTheme.IsPortraitNarrowScreen();
        rect.anchorMin=portrait?new(.05f,.08f):Screen.height<500?new(.14f,.025f):new(.26f,.10f);
        rect.anchorMax=portrait?new(.95f,.92f):Screen.height<500?new(.86f,.975f):new(.74f,.90f);
        rect.offsetMin=rect.offsetMax=Vector2.zero;
        float scale=GetComponentInParent<Canvas>().rootCanvas.scaleFactor;
        foreach(var text in GetComponentsInChildren<TextMeshProUGUI>())
        {
            text.enableAutoSizing=true;text.fontSizeMin=12/scale;text.fontSizeMax=(text.name=="Title"?24:18)/scale;
            text.margin=Vector4.zero;
        }
    }
}
