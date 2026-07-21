using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class SummonCategoryUI : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown categoryDropdown;

    private List<CharacterCategory> availableCategories = new();
    private bool subscribed;

    private void Start()
    {
        if (categoryDropdown != null)
        {
            categoryDropdown.onValueChanged.AddListener(OnCategorySelected);
        }
    }

    private void OnEnable()
    {
        Subscribe();
        RebuildOptions();
    }

    private void OnDisable()
    {
        if (FacilityManager.Instance != null && subscribed)
        {
            FacilityManager.Instance.OnSummonCategoryUnlocked -= HandleCategoryUnlocked;
            subscribed = false;
        }
    }

    private void Subscribe()
    {
        if (FacilityManager.Instance == null || subscribed)
        {
            return;
        }

        FacilityManager.Instance.OnSummonCategoryUnlocked += HandleCategoryUnlocked;
        subscribed = true;
    }

    private void RebuildOptions()
    {
        if (categoryDropdown == null)
        {
            return;
        }

        categoryDropdown.ClearOptions();
        availableCategories.Clear();

        var options = new List<string>();
        availableCategories.Add(CharacterCategory.None);
        options.Add("All");

        if (FacilityManager.Instance != null)
        {
            foreach (CharacterCategory category in System.Enum.GetValues(typeof(CharacterCategory)))
            {
                if (category == CharacterCategory.None || category == CharacterCategory.Boss)
                {
                    continue;
                }

                if (FacilityManager.Instance.IsCategoryUnlocked(category))
                {
                    availableCategories.Add(category);
                    options.Add(GetCategoryLabel(category));
                }
            }
        }

        categoryDropdown.AddOptions(options);
        Refresh();
        categoryDropdown.RefreshShownValue();
    }

    private void OnCategorySelected(int index)
    {
        if (index < 0 || index >= availableCategories.Count || GameManager.Instance == null)
        {
            return;
        }

        CharacterCategory selected = availableCategories[index];
        GameManager.Instance.ActiveSummonCategory = selected;
        Debug.Log($"[SummonCategoryUI] {selected} が選択されました");
    }

    public void Refresh()
    {
        if (categoryDropdown == null || GameManager.Instance == null)
        {
            return;
        }

        CharacterCategory active = GameManager.Instance.ActiveSummonCategory;
        int index = availableCategories.IndexOf(active);
        if (index < 0)
        {
            index = 0;
            GameManager.Instance.ActiveSummonCategory = CharacterCategory.None;
        }

        categoryDropdown.SetValueWithoutNotify(index);
        categoryDropdown.RefreshShownValue();
    }

    private void HandleCategoryUnlocked(CharacterCategory category)
    {
        RebuildOptions();
    }

    private static string GetCategoryLabel(CharacterCategory category)
    {
        switch (category)
        {
            case CharacterCategory.Number1: return "数字1";
            case CharacterCategory.Number2: return "数字2";
            case CharacterCategory.Number3: return "数字3";
            case CharacterCategory.Weapon: return "武器";
            case CharacterCategory.Defense: return "防御";
            case CharacterCategory.Ranged: return "遠隔";
            case CharacterCategory.Nature: return "自然";
            case CharacterCategory.Animal: return "動物";
            default: return category.ToString();
        }
    }
}
