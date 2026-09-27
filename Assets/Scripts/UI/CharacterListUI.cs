using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterListUI : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private GameObject characterEntryPrefab;
    private ScreenInfoRegion informationRegion;
    private RectTransform practiceRegion;
    private RosterPracticeVfx practiceVfx;
    private bool recruitmentInProgress;

    private void Start()
    {
        ApplyListLayout();
        RefreshList();
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.onInventoryChanged += RefreshList;
        }
        ResearchBondService.Instance.OnChanged += RefreshList;
    }

    private void OnEnable()
    {
        ApplyListLayout();
        RefreshList();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        ApplyListLayout();
        RefreshList();
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.onInventoryChanged -= RefreshList;
        }
        ResearchBondService.Instance.OnChanged -= RefreshList;
    }

    public void RefreshList()
    {
        if (PlayerInventory.Instance == null || content == null)
        {
            return;
        }

        ApplyListLayout();
        EnsureInformationRegion();

        ClearEntries();

        var displayedCharacterIds = new HashSet<int>();
        List<CharacterData> owned = PlayerInventory.Instance.GetOwnedCharacters()
            .Select(entry => entry.Key)
            .Where(data => data != null && displayedCharacterIds.Add(data.characterId))
            .OrderBy(data => data.characterId)
            .ToList();
        List<CharacterData> candidates = ResearchBondService.Instance.GetDefeatedCandidates()
            .Where(data => data != null && displayedCharacterIds.Add(data.characterId))
            .ToList();

        float cursorY = 12f;
        foreach (IGrouping<CharacterCategory, CharacterData> group in owned
                     .GroupBy(character => character.category)
                     .OrderBy(group => GetRosterGroupOrder(group.Key)))
        {
            List<CharacterData> groupCharacters = group.ToList();
            int columns = GetRosterGroupColumns(groupCharacters.Count);
            CreateSectionHeader(GetRosterGroupTitle(group.Key), GetRosterGroupSubtitle(group.Key), cursorY, false);
            cursorY += 68f;
            for (int index = 0; index < groupCharacters.Count; index++)
            {
                var entry = Instantiate(characterEntryPrefab, content);
                PrepareFormationCell(entry, index, cursorY, columns, groupCharacters.Count);
                var ui = entry.GetComponent<CharacterEntryUI>();
                ui.SetCharacter(groupCharacters[index], PlayerInventory.Instance.PlayerLevel, PlayerInventory.Instance.PlayerExperience,
                    PlayerInventory.Instance.GetExperienceToNextPlayerLevel(), PlayerInventory.Instance.GetEffectiveLevelCap(),
                    ShowOwnedInformation);
            }
            cursorY += GetSectionHeight(groupCharacters.Count, columns);
        }

        if (candidates.Count > 0)
        {
            CreateSectionHeader("縁", "戦場で出会った駒", cursorY, true);
            cursorY += 64f;
            for (int index = 0; index < candidates.Count; index++)
            {
                CharacterData candidate = candidates[index];
                var entry = Instantiate(characterEntryPrefab, content);
                PrepareBondIndicatorRow(entry, index, cursorY);
                var ui = entry.GetComponent<CharacterEntryUI>();
                ui.SetBondCandidate(
                    candidate,
                    ResearchBondService.Instance.GetBond(candidate),
                    ResearchBondService.Instance.GetBondThreshold(candidate),
                    () => BeginRecruitment(candidate, entry.GetComponent<RectTransform>()),
                    ShowBondInformation);
            }
            cursorY += GetBondSectionHeight(candidates.Count);
        }

        FinalizeEntryLayout(cursorY);
        ShowRosterSummary(owned.Count, candidates.Count);
        ConfigurePracticePresentation(owned);

        // This is a dynamic scrollable grid. Rebuild it after entries are added so
        // every discovered character receives a reachable row in the scroll view.
        Canvas.ForceUpdateCanvases();
        if (content is RectTransform contentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }
    }

    private void ClearEntries()
    {
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            var child = content.GetChild(i);
            child.gameObject.SetActive(false);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }

    private void PrepareFormationCell(GameObject entry, int index, float sectionTop, int columns, int entryCount)
    {
        if (entry == null || content == null)
        {
            return;
        }

        var rect = entry.GetComponent<RectTransform>();
        if (rect == null) return;

        float spacing = UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? 9f : 12f;
        float cellWidth = GetRosterCellWidth(columns, spacing);
        int column = index % columns;
        int row = index / columns;
        float cellHeight = GetRosterCellHeight(cellWidth);
        int cellsInRow = Mathf.Min(columns, entryCount - row * columns);
        float rowWidth = cellsInRow * cellWidth + Mathf.Max(0, cellsInRow - 1) * spacing;
        float listWidth = GetListWidth();
        float startX = Mathf.Max(20f, (listWidth - rowWidth) * 0.5f);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(
            startX + column * (cellWidth + spacing),
            -sectionTop - row * (cellHeight + spacing));
        rect.sizeDelta = new Vector2(cellWidth, cellHeight);
        rect.localRotation = Quaternion.identity;
    }

    private void PrepareBondIndicatorRow(GameObject entry, int index, float sectionTop)
    {
        if (entry == null || content == null)
        {
            return;
        }

        var rect = entry.GetComponent<RectTransform>();
        if (rect == null)
        {
            return;
        }

        const float rowHeight = 84f;
        const float spacing = 8f;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -sectionTop - index * (rowHeight + spacing));
        rect.sizeDelta = new Vector2(-40f, rowHeight);
        rect.localRotation = Quaternion.identity;
    }

    private float GetSectionHeight(int entryCount, int columns)
    {
        float spacing = UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? 9f : 12f;
        float cellHeight = GetRosterCellHeight(GetRosterCellWidth(columns, spacing));
        int rows = Mathf.CeilToInt(entryCount / (float)columns);
        return rows * cellHeight + Mathf.Max(0, rows - 1) * spacing + 42f;
    }

    private static float GetBondSectionHeight(int entryCount)
    {
        const float rowHeight = 84f;
        const float spacing = 8f;
        return entryCount * rowHeight + Mathf.Max(0, entryCount - 1) * spacing + 42f;
    }

    private void FinalizeEntryLayout(float contentHeight)
    {
        if (content is not RectTransform contentRect) return;

        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, Mathf.Max(160f, contentHeight + 18f));
        contentRect.anchoredPosition = Vector2.zero;
    }

    private void CreateSectionHeader(string title, string subtitle, float top, bool muted)
    {
        var header = new GameObject("RosterSection_" + title, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        header.transform.SetParent(content, false);
        var background = header.GetComponent<Image>();
        ModernWafuuPresentation.ApplyFlatSurface(background, muted
            ? new Color(0.11f, 0.10f, 0.08f, 0.82f)
            : new Color(0.15f, 0.07f, 0.025f, 0.86f));
        background.raycastTarget = false;

        var headerRect = header.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = new Vector2(0f, -top);
        headerRect.sizeDelta = new Vector2(-36f, 38f);

        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(header.transform, false);
        var label = labelObject.GetComponent<TextMeshProUGUI>();
        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(label);
        label.text = $"{title}  {subtitle}";
        label.enableAutoSizing = true;
        label.fontSizeMax = 28f;
        label.fontSizeMin = 21f;
        label.alignment = TextAlignmentOptions.Left;
        label.color = new Color(1f, 0.93f, 0.76f, 1f);
        label.outlineColor = new Color(0.07f, 0.04f, 0.02f, 0.94f);
        label.outlineWidth = 0.20f;
        label.raycastTarget = false;
        var labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.03f, 0f);
        labelRect.anchorMax = new Vector2(0.97f, 1f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    private void ApplyListLayout()
    {
        // Keep the screen-level field visible like 行軍 and 城下, while each individual
        // piece remains background-free inside it.
        var listBackground = GetComponent<Image>();
        if (listBackground != null)
        {
            ModernWafuuPresentation.ApplyFlatSurface(listBackground, new Color(0.94f, 0.87f, 0.68f, 0.44f));
            listBackground.raycastTarget = true;
            if (!listBackground.TryGetComponent(out Outline outline))
            {
                outline = listBackground.gameObject.AddComponent<Outline>();
            }
            outline.enabled = true;
            outline.effectColor = new Color(0.18f, 0.10f, 0.04f, 0.62f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        var listRect = GetComponent<RectTransform>();
        if (listRect != null)
        {
            bool compactLandscape = IsCompactLandscape();
            listRect.anchorMin = new Vector2(0.04f, 0.05f);
            listRect.anchorMax = new Vector2(0.96f, compactLandscape ? 0.49f : 0.56f);
            listRect.anchoredPosition = Vector2.zero;
            listRect.sizeDelta = Vector2.zero;
        }

        var grid = content != null ? content.GetComponent<GridLayoutGroup>() : null;
        if (grid != null)
        {
            grid.enabled = false;
        }

        var contentFitter = content != null ? content.GetComponent<ContentSizeFitter>() : null;
        if (contentFitter == null && content != null)
        {
            contentFitter = content.gameObject.AddComponent<ContentSizeFitter>();
        }
        if (contentFitter != null)
        {
            contentFitter.enabled = false;
        }

        var verticalLayout = content != null ? content.GetComponent<VerticalLayoutGroup>() : null;
        if (verticalLayout != null)
        {
            verticalLayout.enabled = false;
        }

        var fitter = content != null ? content.GetComponent<ResponsiveGridFitter>() : null;
        if (fitter != null)
        {
            fitter.enabled = false;
        }
    }

    private void EnsureInformationRegion()
    {
        Transform host = transform.parent != null ? transform.parent : transform;
        bool compactLandscape = IsCompactLandscape();
        informationRegion = ScreenInfoRegion.Ensure(
            host,
            "RosterInformationRegion",
            new Vector2(0.04f, compactLandscape ? 0.75f : 0.81f),
            new Vector2(0.96f, 0.92f));
        informationRegion.ReserveLowerContent(false);
        EnsurePracticeRegion(host);
    }

    private void EnsurePracticeRegion(Transform host)
    {
        if (practiceRegion == null)
        {
            Transform existing = host.Find("RosterPracticeRegion");
            practiceRegion = existing as RectTransform;
        }

        if (practiceRegion == null)
        {
            var region = new GameObject("RosterPracticeRegion", typeof(RectTransform));
            region.transform.SetParent(host, false);
            practiceRegion = region.GetComponent<RectTransform>();
        }

        bool compactLandscape = IsCompactLandscape();
        practiceRegion.anchorMin = new Vector2(0.04f, compactLandscape ? 0.52f : 0.60f);
        practiceRegion.anchorMax = new Vector2(0.96f, compactLandscape ? 0.73f : 0.80f);
        practiceRegion.offsetMin = Vector2.zero;
        practiceRegion.offsetMax = Vector2.zero;
        practiceRegion.SetAsLastSibling();
    }

    private static bool IsCompactLandscape()
    {
        // Unity WebGL reports device pixels on high-density phones, so a raw
        // height threshold cannot distinguish a short landscape viewport.
        // The landscape roster always needs this denser vertical allocation.
        return !UnityUIRuntimeTheme.IsPortraitNarrowScreen();
    }

    private void ShowRosterSummary(int ownedCount, int candidateCount)
    {
        if (informationRegion == null)
        {
            return;
        }

        informationRegion.Configure(
            "軍勢の状況",
            $"加入済み {ownedCount} 駒　/　縁候補 {candidateCount} 駒　/　緑の縁印は加入可能");
    }

    private void ConfigurePracticePresentation(IReadOnlyList<CharacterData> owned)
    {
        practiceVfx ??= GetComponent<RosterPracticeVfx>() ?? gameObject.AddComponent<RosterPracticeVfx>();
        int slotCount = GameManager.Instance != null ? GameManager.Instance.GetFacilityFormationSlots() : 1;
        practiceVfx.ConfigureTraining(practiceRegion, owned, slotCount);
    }

    private void BeginRecruitment(CharacterData candidate, RectTransform source)
    {
        if (recruitmentInProgress || candidate == null || source == null || !ResearchBondService.Instance.CanRecruit(candidate))
        {
            return;
        }

        practiceVfx ??= GetComponent<RosterPracticeVfx>() ?? gameObject.AddComponent<RosterPracticeVfx>();
        StartCoroutine(RecruitAfterPresentation(candidate, source));
    }

    private System.Collections.IEnumerator RecruitAfterPresentation(CharacterData candidate, RectTransform source)
    {
        recruitmentInProgress = true;
        yield return practiceVfx.PlayRecruitment(candidate, source);
        ResearchBondService.Instance.Recruit(candidate);
        recruitmentInProgress = false;
    }

    private void ShowOwnedInformation(CharacterData character)
    {
        if (informationRegion == null || character == null || PlayerInventory.Instance == null)
        {
            return;
        }

        int level = PlayerInventory.Instance.PlayerLevel;
        informationRegion.Configure(
            $"{character.characterName}　現在 Lv.{level}",
            $"HP {PlayerInventory.Instance.GetPlayerMaxHP(character, level)}　攻 {character.GetAttack(level)}　守 {character.GetDefense(level)}　/　{GetShortSkillDescription(character)}");
    }

    private void ShowBondInformation(CharacterData character, int bond, int threshold)
    {
        if (informationRegion == null || character == null)
        {
            return;
        }

        informationRegion.Configure(
            $"{character.characterName}　縁 {bond}/{threshold}",
            GetShortSkillDescription(character));
    }

    private static string GetShortSkillDescription(CharacterData character)
    {
        string detail = SkillDescription.GetDetail(character);
        int end = detail.IndexOf('。');
        return end >= 0 ? detail[..(end + 1)] : detail;
    }

    private int GetRosterGroupColumns(int entryCount)
    {
        if (entryCount <= 1)
        {
            return 1;
        }

        // Natural has four members and intentionally reads as one complete row.
        // Other small groups stay intact rather than being forced into a generic 3 x 4 grid.
        return Mathf.Min(entryCount, UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? 4 : 5);
    }

    private static int GetRosterGroupOrder(CharacterCategory category)
    {
        return category switch
        {
            CharacterCategory.Number1 => 0,
            CharacterCategory.Number2 => 1,
            CharacterCategory.Number3 => 2,
            CharacterCategory.Weapon => 3,
            CharacterCategory.Defense => 4,
            CharacterCategory.Ranged => 5,
            CharacterCategory.Nature => 6,
            CharacterCategory.Animal => 7,
            CharacterCategory.Boss => 8,
            _ => 9
        };
    }

    private static string GetRosterGroupTitle(CharacterCategory category)
    {
        return category switch
        {
            CharacterCategory.Number1 => "低位の数",
            CharacterCategory.Number2 => "中位の数",
            CharacterCategory.Number3 => "上位の数",
            CharacterCategory.Weapon => "武具",
            CharacterCategory.Defense => "守り",
            CharacterCategory.Ranged => "遠隔",
            CharacterCategory.Nature => "自然",
            CharacterCategory.Animal => "獣",
            CharacterCategory.Boss => "主将",
            _ => "軍勢"
        };
    }

    private static string GetRosterGroupSubtitle(CharacterCategory category)
    {
        // The pieces themselves already carry their kanji. Repeating the full
        // roster in a heading makes the list noisier without adding information.
        return string.Empty;
    }

    private float GetRosterCellWidth(int columnCount, float spacing)
    {
        float availableWidth = GetListWidth() - 40f - spacing * (columnCount - 1);
        float minimum = UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? 76f : 108f;
        return Mathf.Max(minimum, availableWidth / columnCount);
    }

    private float GetRosterCellHeight(float width)
    {
        return Mathf.Clamp(width * 0.86f, 96f, 150f);
    }

    private float GetListWidth()
    {
        var listRect = GetComponent<RectTransform>();
        if (listRect != null && listRect.rect.width > 0f)
        {
            return listRect.rect.width;
        }

        return UnityUIRuntimeTheme.IsPortraitNarrowScreen() ? 360f : 920f;
    }

    public bool OwnsContent(Transform candidate)
    {
        return candidate != null && candidate == content;
    }
}
