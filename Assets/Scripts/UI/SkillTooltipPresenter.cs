using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class SkillTooltipPresenter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private CharacterData characterData;
    private string statusLine;
    private static GameObject tooltipObject;
    private static TextMeshProUGUI tooltipText;
    private static SkillTooltipPresenter activePresenter;
    private static bool pointerOverTooltip;
    private bool pointerOverOwner;
    private Coroutine hideRoutine;

    public void SetCharacter(CharacterData data, string status = null)
    {
        characterData = data;
        statusLine = status;
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
        ContextualInfoTooltip.Hide();
        EnsureTooltip();
        activePresenter = this;
        tooltipText.text = string.IsNullOrEmpty(statusLine)
            ? SkillDescription.GetDetail(characterData)
            : $"{statusLine}\n\n{SkillDescription.GetDetail(characterData)}";

        var targetRect = transform as RectTransform;
        var tooltipRect = tooltipObject.transform as RectTransform;
        if (targetRect != null && tooltipRect != null)
        {
            Vector3[] corners = new Vector3[4];
            targetRect.GetWorldCorners(corners);
            var rootRect = tooltipRect.parent as RectTransform;
            if (rootRect != null)
            {
                Vector3 local = rootRect.InverseTransformPoint(corners[2]);
                Vector2 halfSize = tooltipRect.rect.size * 0.5f;
                const float margin = 18f;
                Rect bounds = rootRect.rect;
                local.x = Mathf.Clamp(local.x - halfSize.x, bounds.xMin + halfSize.x + margin, bounds.xMax - halfSize.x - margin);
                local.y = Mathf.Clamp(local.y - halfSize.y, bounds.yMin + halfSize.y + margin, bounds.yMax - halfSize.y - margin);
                tooltipRect.localPosition = local;
            }
            else
            {
                tooltipRect.position = corners[2];
            }
        }

        tooltipObject.transform.SetAsLastSibling();
        tooltipObject.SetActive(true);
        GameAudio.Instance.Play(GameSound.Click);
    }

    public static void HideAll()
    {
        activePresenter = null;
        pointerOverTooltip = false;
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

    private static void SetTooltipPointer(bool isOver)
    {
        pointerOverTooltip = isOver;
        if (isOver && activePresenter != null)
        {
            activePresenter.CancelHide();
        }
        else if (!isOver && activePresenter != null && !activePresenter.pointerOverOwner)
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
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = UnityUIRuntimeTheme.IsPortraitNarrowScreen()
            ? new Vector2(360f, 288f)
            : new Vector2(460f, 278f);

        var image = tooltipObject.GetComponent<Image>();
        ModernWafuuPresentation.ApplyFlatSurface(image, new Color(0.15f, 0.11f, 0.065f, 0.98f));
        // Tooltips are visual-only. They must never sit above a recruitable piece
        // and consume the next tap intended for that piece.
        image.raycastTarget = false;
        var outline = tooltipObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.96f, 0.76f, 0.38f, 0.92f);
        outline.effectDistance = new Vector2(2f, -2f);

        var textObj = new GameObject("Description", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(tooltipObject.transform, false);
        tooltipText = textObj.GetComponent<TextMeshProUGUI>();
        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(tooltipText);
        tooltipText.color = new Color(1f, 0.94f, 0.78f, 1f);
        tooltipText.alignment = TextAlignmentOptions.TopLeft;
        tooltipText.enableAutoSizing = true;
        tooltipText.fontSizeMin = 21f;
        tooltipText.fontSizeMax = 25f;
        tooltipText.textWrappingMode = TextWrappingModes.Normal;
        tooltipText.raycastTarget = false;

        var textRect = tooltipText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 16f);
        textRect.offsetMax = new Vector2(-20f, -16f);

        tooltipObject.SetActive(false);
    }

    private sealed class SkillTooltipHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            SetTooltipPointer(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetTooltipPointer(false);
        }
    }
}
