using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattlePlaybackControls
{
    private readonly Slider speed;
    private readonly TextMeshProUGUI speedLabel;
    private readonly Toggle repeat;
    private bool refreshing;
    private static Sprite thumbSprite;
    private static Sprite checkSprite;

    public BattlePlaybackControls(Transform parent, Action<int> speedChanged, Action<bool> repeatChanged)
    {
        var root = new GameObject("SpeedControl", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(parent, false);
        speedLabel = Text(root, "Value", new Vector2(0f, 0.55f), Vector2.one);
        var sliderRoot = Image(root, "Slider", Vector2.zero, new Vector2(1f, 0.55f), Color.clear);
        speed = sliderRoot.gameObject.AddComponent<Slider>();
        speed.wholeNumbers = true;
        speed.direction = Slider.Direction.LeftToRight;
        Image(sliderRoot.transform, "Track", new Vector2(0.05f, 0.40f), new Vector2(0.95f, 0.60f), new Color(0.55f, 0.54f, 0.50f));
        var area = new GameObject("HandleArea", typeof(RectTransform)).GetComponent<RectTransform>();
        area.SetParent(sliderRoot.transform, false);
        Place(area, new Vector2(0.05f, 0f), new Vector2(0.95f, 1f));
        var handleArea = Image(area, "HandlePosition", new Vector2(0f, 0f), new Vector2(0f, 1f), Color.clear);
        handleArea.rectTransform.sizeDelta = new Vector2(64f, 0f);
        var handle = Image(handleArea.transform, "Handle", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Color(1f, 0.83f, 0.43f));
        handle.rectTransform.sizeDelta = new Vector2(64f, 64f);
        handle.sprite = thumbSprite != null ? thumbSprite : thumbSprite = CreateControlSprite(false);
        speed.handleRect = handleArea.rectTransform;
        speed.targetGraphic = handle;
        speed.onValueChanged.AddListener(value => { if (!refreshing) speedChanged(Mathf.RoundToInt(value)); });

        var repeatRoot = Image(parent, "AutoRepeatControl", Vector2.zero, Vector2.one, Color.clear);
        repeat = repeatRoot.gameObject.AddComponent<Toggle>();
        var box = Image(repeatRoot.transform, "Box", new Vector2(0f, 0.22f), new Vector2(0f, 0.78f), new Color(0.45f, 0.48f, 0.46f));
        box.rectTransform.sizeDelta = new Vector2(64f, 0f);
        box.rectTransform.pivot = new Vector2(0f, 0.5f);
        var boxAspect = box.gameObject.AddComponent<AspectRatioFitter>();
        boxAspect.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
        boxAspect.aspectRatio = 1f;
        var check = Image(box.transform, "Check", new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f), new Color(1f, 0.95f, 0.82f));
        check.sprite = checkSprite != null ? checkSprite : checkSprite = CreateControlSprite(true);
        Text(repeatRoot.transform, "Label", new Vector2(0.38f, 0f), Vector2.one).text = "連戦";
        repeat.targetGraphic = box;
        repeat.graphic = check;
        repeat.onValueChanged.AddListener(value => repeatChanged(value));
    }

    public void Refresh(int selected, int maximum, float currentSpeed, float maximumSpeed, bool repeating, bool canRepeat)
    {
        refreshing = true;
        speed.maxValue = maximum;
        speed.SetValueWithoutNotify(selected);
        speed.interactable = maximum > 0;
        speedLabel.text = $"x{currentSpeed:0.##}　最大 x{maximumSpeed:0.##}";
        repeat.gameObject.SetActive(canRepeat);
        repeat.SetIsOnWithoutNotify(repeating);
        refreshing = false;
    }

    private static TextMeshProUGUI Text(Transform parent, string name, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
        text.color = new Color(1f, 0.95f, 0.82f);
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = 36f;
        text.fontSizeMax = 42f;
        text.raycastTarget = false;
        Place(text.rectTransform, min, max);
        return text;
    }

    private static Image Image(Transform parent, string name, Vector2 min, Vector2 max, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = name == "Slider" || name == "AutoRepeatControl" || name == "Handle";
        Place(image.rectTransform, min, max);
        return image;
    }

    private static void Place(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static Sprite CreateControlSprite(bool checkmark)
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = checkmark ? "BattleRepeatCheck" : "BattleSpeedThumb";
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            var point = new Vector2(x + 0.5f, y + 0.5f);
            float coverage = checkmark
                ? 4f - Mathf.Min(DistanceToSegment(point, new Vector2(10f, 30f), new Vector2(26f, 16f)),
                    DistanceToSegment(point, new Vector2(26f, 16f), new Vector2(54f, 49f)))
                : 31f - Vector2.Distance(point, new Vector2(32f, 32f));
            pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(coverage));
        }
        texture.SetPixels(pixels);
        texture.Apply(false, true);
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 direction = end - start;
        float position = Mathf.Clamp01(Vector2.Dot(point - start, direction) / direction.sqrMagnitude);
        return Vector2.Distance(point, start + direction * position);
    }
}
