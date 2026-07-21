using UnityEngine;

public class StageSelectUI : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private GameObject stageButtonPrefab;
    [SerializeField] private StageDatabase stageDatabase;

    private void OnEnable()
    {
        stageDatabase.AssignStageIds();
        DisplayStages();
    }

    public void DisplayStages()
    {
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        foreach (var stage in stageDatabase.stages)
        {
            var btn = Instantiate(stageButtonPrefab, content);
            var ui = btn.GetComponent<StageButtonUI>();
            ui.SetStage(stage);

            bool stageProgressLocked = stage.stageId > GameManager.Instance.GetHighestClearedStageId() + 1;
            bool chapterLocked = !GameManager.Instance.IsChapterUnlocked(stage.chapterId);
            btn.GetComponent<UnityEngine.UI.Button>().interactable = !stageProgressLocked && !chapterLocked;
            ui.SetLocked(chapterLocked);
        }
    }
}
