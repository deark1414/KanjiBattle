using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class SkillTooltipPresenter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private CharacterData characterData;
    private static GameObject tooltipObject;
    private static TextMeshProUGUI tooltipText;
    private static SkillTooltipPresenter activePresenter;
    private static bool pointerOverTooltip;
    private bool pointerOverOwner;
    private Coroutine hideRoutine;

    public void SetCharacter(CharacterData data)
    {
        characterData = data;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerOverOwner = true;
        Show();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerOverOwner = false;
        ScheduleHide();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (activePresenter == this && tooltipObject != null && tooltipObject.activeSelf)
        {
            HideAll();
        }
        else
        {
            pointerOverOwner = true;
            Show();
        }
    }

    private void OnDisable()
    {
        if (activePresenter == this)
        {
            HideAll();
        }
    }

    private void OnDestroy()
    {
        if (activePresenter == this)
        {
            HideAll();
        }
    }

    private void Show()
    {
        if (characterData == null) return;
        CancelHide();
        EnsureTooltip();
        activePresenter = this;
        tooltipText.text = SkillDescription.GetDetail(characterData);

        var targetRect = transform as RectTransform;
        var tooltipRect = tooltipObject.transform as RectTransform;
        if (targetRect != null && tooltipRect != null)
        {
            Vector3[] corners = new Vector3[4];
            targetRect.GetWorldCorners(corners);
            tooltipRect.position = corners[2];
            tooltipRect.anchoredPosition += new Vector2(-220f, -8f);
        }

        tooltipObject.transform.SetAsLastSibling();
        tooltipObject.SetActive(true);
        GameAudio.Instance.Play(GameSound.Click);
    }

    public static void HideAll()
    {
        pointerOverTooltip = false;
        activePresenter = null;
        if (tooltipObject != null)
        {
            tooltipObject.SetActive(false);
        }
    }

    private void ScheduleHide()
    {
        CancelHide();
        hideRoutine = StartCoroutine(HideWhenPointerLeavesRoutine());
    }

    private void CancelHide()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }
    }

    private System.Collections.IEnumerator HideWhenPointerLeavesRoutine()
    {
        yield return null;
        hideRoutine = null;
        if (activePresenter == this && !pointerOverOwner && !pointerOverTooltip)
        {
            HideAll();
        }
    }

    private static void SetPointerOverTooltip(bool value)
    {
        pointerOverTooltip = value;
        if (activePresenter == null) return;

        if (value)
        {
            activePresenter.CancelHide();
        }
        else
        {
            activePresenter.ScheduleHide();
        }
    }

    private void EnsureTooltip()
    {
        if (tooltipObject != null) return;

        Canvas canvas = GetComponentInParent<Canvas>();
        Transform parent = canvas != null ? canvas.transform : transform.root;
        tooltipObject = new GameObject("SkillTooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        tooltipObject.transform.SetParent(parent, false);

        var rect = tooltipObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(330f, 236f);

        var image = tooltipObject.GetComponent<Image>();
        image.color = new Color(0.15f, 0.12f, 0.10f, 0.94f);
        image.raycastTarget = true;
        var outline = tooltipObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.95f, 0.72f, 0.42f, 0.9f);
        outline.effectDistance = new Vector2(1f, -1f);
        tooltipObject.AddComponent<SkillTooltipSurface>();

        var textObj = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(tooltipObject.transform, false);
        tooltipText = textObj.GetComponent<TextMeshProUGUI>();
        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(tooltipText);
        tooltipText.color = new Color(1f, 0.94f, 0.78f, 1f);
        tooltipText.alignment = TextAlignmentOptions.TopLeft;
        tooltipText.enableAutoSizing = true;
        tooltipText.fontSizeMin = 12f;
        tooltipText.fontSizeMax = 18f;
        tooltipText.textWrappingMode = TextWrappingModes.Normal;
        tooltipText.raycastTarget = false;

        var textRect = tooltipText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14f, 10f);
        textRect.offsetMax = new Vector2(-14f, -10f);

        tooltipObject.SetActive(false);
    }

    private sealed class SkillTooltipSurface : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            SetPointerOverTooltip(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetPointerOverTooltip(false);
        }
    }
}
