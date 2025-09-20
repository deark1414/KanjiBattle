using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FacilityUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI costText;

    private FacilityData facility;

    public void Setup(FacilityData facilityData)
    {
        facility = facilityData;

        // ボタンを親にしたのでクリックリスナーを付与
        var button = GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClickUpgrade);

        Refresh();
    }

    private void OnClickUpgrade()
    {
        if (FacilityManager.Instance.Upgrade(facility))
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        int level = FacilityManager.Instance.GetLevel(facility);
        nameText.text = facility.facilityName;
        levelText.text = $"Lv.{level}";
        costText.text = level >= facility.maxLevel ? "MAX" :
            $"{FacilityManager.Instance.GetUpgradeCost(facility)} G";
    }
}