using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageButtonUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stageNameText;
    private StageData stageData;

    private void Awake()
    {
        // ボタン押下時にOnClickを呼ぶ
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    public void SetStage(StageData data)
    {
        stageData = data;
        stageNameText.text = data.stageName;
    }

    private void OnClick()
    {
         if (stageData != null)
        {
            // GameManagerに保持
            GameManager.Instance.SetSelectedStage(stageData);
            
            UIManager.Instance.ShowFormation();
        }
    }
}