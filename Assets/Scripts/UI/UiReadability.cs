using System.Runtime.InteropServices;
using UnityEngine;

public static class UiReadability
{
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern float KB_CanvasPixelRatio();
#endif
    private static int frame=-1;
    private static float ratio=1;
    public static float PixelRatio
    {
        get
        {
            if(frame!=Time.frameCount)
            {
                frame=Time.frameCount;
#if UNITY_WEBGL && !UNITY_EDITOR
                ratio=Mathf.Clamp(KB_CanvasPixelRatio(),.5f,4f);
#endif
            }
            return ratio;
        }
    }
    public static float Width=>Screen.width/PixelRatio;
    public static float Height=>Screen.height/PixelRatio;
    public static float Scale(Canvas canvas)=>canvas.rootCanvas.scaleFactor/PixelRatio;
}
