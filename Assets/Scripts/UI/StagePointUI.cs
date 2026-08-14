using TMPro;
using UnityEngine;

public class StagePointUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI stagePointText;

    private void Start()
    {
        // 初期表示
        UpdateStagePoints(GameManager.Instance.StagePoints);

        // ステージポイント変更イベントを購読
        GameManager.Instance.OnStagePointsChanged += UpdateStagePoints;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStagePointsChanged -= UpdateStagePoints;
        }
    }

    private void UpdateStagePoints(int points)
    {
        if (stagePointText != null)
        {
            stagePointText.text = $"StagePts: {points}";
        }
    }
}
