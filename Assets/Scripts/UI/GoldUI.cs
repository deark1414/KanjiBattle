using TMPro;
using UnityEngine;

public class GoldUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goldText;

    private void Start()
    {
        // 初期表示
        UpdateGold(GameManager.Instance.Gold);

        // ゴールド変更イベントを購読
        GameManager.Instance.OnGoldChanged += UpdateGold;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGoldChanged -= UpdateGold;
        }
    }

    private void UpdateGold(int gold)
    {
        if (goldText != null)
        {
            UnityUIRuntimeTheme.ApplyJapaneseFont(goldText);
            goldText.text = $"Gold: {gold}";
        }
    }
}
