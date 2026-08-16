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
            ApplyDropdownLayout();
            ApplyDropdownTextStyle();
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
        ApplyDropdownLayout();
        ApplyDropdownTextStyle();
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
        ApplyDropdownTextStyle();
    }

    private void ApplyDropdownTextStyle()
    {
        if (categoryDropdown == null)
        {
            return;
        }

        StyleDropdownText(categoryDropdown.captionText);
        StyleDropdownText(categoryDropdown.itemText);
    }

    private void ApplyDropdownLayout()
    {
        if (categoryDropdown == null)
        {
            return;
        }

        var rect = categoryDropdown.GetComponent<RectTransform>();
        if (rect != null)
        {
            if (UnityUIRuntimeTheme.IsPortraitNarrowScreen())
            {
                rect.anchorMin = new Vector2(0.04f, 0.035f);
                rect.anchorMax = new Vector2(0.52f, 0.105f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }
            else
            {
                rect.sizeDelta = new Vector2(Mathf.Max(rect.sizeDelta.x, 170f), 46f);
            }
        }

        if (categoryDropdown.template != null)
        {
            categoryDropdown.template.sizeDelta = new Vector2(Mathf.Max(categoryDropdown.template.sizeDelta.x, UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? 210f : 190f), 240f);
            var item = categoryDropdown.template.Find("Viewport/Content/Item") as RectTransform;
            if (item != null)
            {
                item.sizeDelta = new Vector2(item.sizeDelta.x, 38f);
                var layout = item.GetComponent<UnityEngine.UI.LayoutElement>();
                if (layout == null) layout = item.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                layout.minHeight = 38f;
                layout.preferredHeight = 38f;
            }
        }
    }

    private static void StyleDropdownText(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        if (text is TextMeshProUGUI textMesh)
        {
            UnityUIRuntimeTheme.EnsureJapaneseCapableFont(textMesh);
        }
        text.color = new Color(0.16f, 0.10f, 0.05f, 1f);
        text.enableAutoSizing = true;
        text.fontSizeMin = 14f;
        text.fontSizeMax = 22f;
        text.overflowMode = TextOverflowModes.Truncate;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.SetAllDirty();
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
