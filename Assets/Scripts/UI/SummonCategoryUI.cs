using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class SummonCategoryUI : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown categoryDropdown;

    private List<CharacterCategory> availableCategories = new();

    private void Start()
    {
        SetupOptions();
        if (FacilityManager.Instance != null)
        {
            FacilityManager.Instance.OnSummonCategoryUnlocked += HandleCategoryUnlocked;
        }
        categoryDropdown.onValueChanged.AddListener(OnCategorySelected);
        Refresh();
    }

    private void OnDisable()
    {
        if (FacilityManager.Instance != null)
        {
            FacilityManager.Instance.OnSummonCategoryUnlocked -= HandleCategoryUnlocked;
        }
    }

    private void SetupOptions()
    {
        categoryDropdown.ClearOptions();
        availableCategories.Clear();

        List<string> options = new List<string>();

        // まず「None」 (= 全カテゴリ)
        availableCategories.Add(CharacterCategory.None);
        options.Add("All");

        // FacilityManager から解放済みカテゴリを列挙
        foreach (CharacterCategory category in System.Enum.GetValues(typeof(CharacterCategory)))
        {
            if (category == CharacterCategory.None) continue;

            if (FacilityManager.Instance.IsCategoryUnlocked(category))
            {
                availableCategories.Add(category);
                options.Add(category.ToString());
            }
        }

        categoryDropdown.AddOptions(options);
    }

    private void OnCategorySelected(int index)
    {
        if (index < 0 || index >= availableCategories.Count) return;

        var selected = availableCategories[index];
        GameManager.Instance.ActiveSummonCategory = selected;

        Debug.Log($"[SummonCategoryUI] {selected} が選択されました");
    }

    public void Refresh()
    {
        var active = GameManager.Instance.ActiveSummonCategory;
        int idx = availableCategories.IndexOf(active);
        if (idx >= 0)
        {
            categoryDropdown.value = idx;
        }
    }

    private void HandleCategoryUnlocked(CharacterCategory category)
    {
        // 再構築
        SetupOptions();
        Refresh();
    }
}