using UnityEngine;
using UnityEngine.UI;

public class StageSelectUI : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private GameObject stageButtonPrefab;
    [SerializeField] private StageDatabase stageDatabase;

    private void OnEnable()
    {
        if (stageDatabase != null)
        {
            stageDatabase.AssignStageIds();
        }
        DisplayStages();
    }

    public void DisplayStages()
    {
        if (content == null || stageDatabase == null || stageButtonPrefab == null)
        {
            return;
        }

        foreach (Transform child in content)
        {
            child.gameObject.SetActive(false);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }

        var gameManager = GameManager.Instance != null ? GameManager.Instance : FindAnyObjectByType<GameManager>();
        if (gameManager == null)
        {
            return;
        }

        int focusIndex = 0;
        int index = 0;
        foreach (var stage in stageDatabase.stages)
        {
            var btn = Instantiate(stageButtonPrefab, content);
            ConfigureStageButtonLayout(btn);
            var ui = btn.GetComponent<StageButtonUI>();
            ui.SetStage(stage);

            bool stageProgressLocked = stage.stageId > gameManager.GetHighestClearedStageId() + 1;
            bool chapterLocked = !gameManager.IsChapterUnlocked(stage.chapterId);
            bool locked = stageProgressLocked || chapterLocked;
            btn.GetComponent<UnityEngine.UI.Button>().interactable = !locked;
            ui.SetState(locked, gameManager.IsStageCleared(stage.stageId));

            if (!locked)
            {
                focusIndex = index;
            }
            index++;
        }

        FinalizeContentLayout(focusIndex);
    }

    private static void ConfigureStageButtonLayout(GameObject buttonObject)
    {
        if (buttonObject == null) return;

        var rect = buttonObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, 58f);
        }

        var layout = buttonObject.GetComponent<LayoutElement>();
        if (layout == null) layout = buttonObject.AddComponent<LayoutElement>();
        layout.minHeight = 58f;
        layout.preferredHeight = 58f;
        layout.flexibleHeight = 0f;
    }

    private void FinalizeContentLayout(int focusIndex)
    {
        var contentRect = content as RectTransform;
        if (contentRect == null || stageButtonPrefab == null || stageDatabase == null) return;

        const float rowHeight = 58f;
        const float spacing = 8f;
        int count = stageDatabase.stages != null ? stageDatabase.stages.Count : 0;
        float height = count > 0 ? count * rowHeight + Mathf.Max(0, count - 1) * spacing + 10f : 0f;
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, height);
        contentRect.anchoredPosition = Vector2.zero;

        var layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout != null)
        {
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        ScrollToFocus(contentRect, focusIndex, rowHeight, spacing);
    }

    private void ScrollToFocus(RectTransform contentRect, int focusIndex, float rowHeight, float spacing)
    {
        var scrollRect = GetComponentInChildren<ScrollRect>();
        RectTransform viewport = scrollRect != null && scrollRect.viewport != null
            ? scrollRect.viewport
            : contentRect.parent as RectTransform;

        if (viewport == null)
        {
            return;
        }

        float maxY = Mathf.Max(0f, contentRect.rect.height - viewport.rect.height);
        float targetY = Mathf.Clamp(focusIndex * (rowHeight + spacing), 0f, maxY);
        contentRect.anchoredPosition = new Vector2(contentRect.anchoredPosition.x, targetY);
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = maxY <= 0f ? 1f : 1f - targetY / maxY;
        }
    }
}
