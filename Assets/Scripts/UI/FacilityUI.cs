using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FacilityUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI buttonText;

    private FacilityData facility;

    public void Setup(FacilityData facilityData)
    {
        facility = facilityData;

        // ボタンを親にしたのでクリックリスナーを付与
        var button = GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClickAction);

        Refresh();
    }

    private void OnClickAction()
    {
        if (!FacilityManager.Instance.IsUnlocked(facility))
        {
            if (FacilityManager.Instance.Unlock(facility))
            {
                Refresh();
            }
        }
        else
        {
            if (FacilityManager.Instance.IsMaxLevel(facility))
            {
                if (FacilityManager.Instance.CanUpgradeLevelCap(facility))
                {
                    if (FacilityManager.Instance.UpgradeLevelCap(facility))
                    {
                        Refresh();
                    }
                }
            }
            else
            {
                if (FacilityManager.Instance.Upgrade(facility))
                {
                    Refresh();
                }
            }
        }
    }

    private void Refresh()
    {
        bool isUnlocked = FacilityManager.Instance.IsUnlocked(facility);
        int level = FacilityManager.Instance.GetLevel(facility);

        nameText.text = facility.facilityName;

        if (!isUnlocked)
        {
            levelText.text = "Locked";

            StageData stage = StageDatabase.Instance.GetStageById(facility.requiredStageId);
            string stageName = stage != null ? stage.stageName : $"Stage {facility.requiredStageId}";
            int cost = FacilityManager.Instance.GetUnlockCost(facility);

            costText.text = $"{stageName} + {cost} pt";
            costText.color = new Color(0.2f, 0.6f, 0.8f);
            buttonText.text = "Unlock";
        }
        else if (FacilityManager.Instance.IsMaxLevel(facility))
        {
            if (FacilityManager.Instance.CanUpgradeLevelCap(facility))
            {
                levelText.text = $"Lv.{level} (Max)";
                var req = FacilityManager.Instance.GetNextFacilityLevelCapRequirement(facility);
                if (req != null)
                {
                    StageData stage = StageDatabase.Instance.GetStageById(req.stageId);
                    string stageName = stage != null ? stage.stageName : $"Stage {req.stageId}";
                    int nextCost = req.requiredStagePoints;
                    costText.text = $"{stageName} + {nextCost}pt";
                }
                else
                {
                    int nextCost = FacilityManager.Instance.GetLevelCapUnlockCost(facility);
                    costText.text = $"{nextCost} pt";
                }
                costText.color = new Color(0.2f, 0.6f, 0.8f);
                buttonText.text = "Unlock Level Cap";
            }
            else
            {
                levelText.text = $"Lv.{level} (Max)";
                var req = FacilityManager.Instance.GetNextFacilityLevelCapRequirement(facility);
                if (req != null)
                {
                    StageData stage = StageDatabase.Instance.GetStageById(req.stageId);
                    string stageName = stage != null ? stage.stageName : $"Stage {req.stageId}";
                    int nextCost = req.requiredStagePoints;
                    costText.text = $"Need {stageName} + {nextCost}pt";
                    costText.color = new Color(0.2f, 0.6f, 0.8f);
                }
                else
                {
                    costText.text = "MAX";
                    costText.color = Color.gray;
                }
                buttonText.text = "Maxed";
            }
        }
        else
        {
            // 通常施設
            levelText.text = $"Lv.{level}";
            costText.text = $"{FacilityManager.Instance.GetUpgradeCost(facility)} pt";
            costText.color = new Color(0.8f, 0.7f, 0.0f); // 暗めの黄色
            buttonText.text = "Upgrade";
        }
    }
}