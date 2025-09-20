using System.Collections.Generic;
using UnityEngine;

public class FacilityListUI : MonoBehaviour
{
    [SerializeField] private Transform contentParent;     // GridLayoutGroup をアタッチした親
    [SerializeField] private FacilityUI facilityPrefab;   // FacilityUI プレハブ

    private List<FacilityUI> spawnedFacilities = new List<FacilityUI>();

    private void Start()
    {
        RefreshList();
    }

    public void RefreshList()
    {
        // 既存を全削除
        foreach (var ui in spawnedFacilities)
        {
            Destroy(ui.gameObject);
        }
        spawnedFacilities.Clear();

        // FacilityManager に登録されている全施設を生成
        foreach (var facility in FacilityManager.Instance.GetFacilities())
        {
            var ui = Instantiate(facilityPrefab, contentParent);
            ui.Setup(facility);
            spawnedFacilities.Add(ui);
        }
    }
}