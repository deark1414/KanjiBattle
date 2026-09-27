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
            // The active screen header owns this value in the modern layout.
            // Hiding the legacy label prevents it from competing with the page title.
            stagePointText.gameObject.SetActive(false);
        }

        ModernWafuuPresentation.ApplyScreenChrome();
    }
}
