using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FormationUI : MonoBehaviour
{
    public static FormationUI Instance;

    [SerializeField] private Transform slotParent;
    [SerializeField] private GameObject slotButtonPrefab;
    [SerializeField] private FormationCharacterListUI characterListUI;

    private List<GameObject> slotButtons = new();
    private CharacterData[] formationSlots;   // ← ここが正しいフィールド名
    private static readonly List<CharacterData> lastBattleFormation = new();
    private Button clearAllButton;
    private Button backToMarchButton;
    private Button deployButton;
    private ScreenInfoRegion informationRegion;
    private CharacterData inspectedCharacter;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void OnEnable()
    {
        ApplySlotAreaLayout();
        ApplyActionLabels();

        // 基本は施設の編成枠だが、ステージが制限を持っている場合は小さい方を採用
        if (GameManager.Instance != null && GameManager.Instance.GetSelectedStage() != null)
        {
            int stageSlots = GameManager.Instance.GetSelectedStage().slotCount;
            int facilitySlots = GameManager.Instance.GetFacilityFormationSlots();
            int totalSlots = Mathf.Min(stageSlots, facilitySlots);
            SetupSlots(totalSlots);
        }

        EnsureUtilityButtons();
        EnsurePrimaryActionButtons();

        // キャラクターリストを表示更新
        if (characterListUI != null)
        {
            characterListUI.DisplayCharacters();
        }

        RefreshInformationRegion();
    }

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        ApplySlotAreaLayout();
        EnsureUtilityButtons();
        EnsurePrimaryActionButtons();
        RefreshInformationRegion();
    }

    private void ApplySlotAreaLayout()
    {
        var slotRect = slotParent as RectTransform;
        if (slotRect == null)
        {
            return;
        }

        // Keep the active formation visually separate from both the deploy band
        // above and the owned-piece list below. The old fixed 146px tiles leaked
        // into neighboring regions on short landscape viewports.
        slotRect.anchorMin = new Vector2(0.04f, 0.505f);
        slotRect.anchorMax = new Vector2(0.96f, 0.645f);
        slotRect.offsetMin = Vector2.zero;
        slotRect.offsetMax = Vector2.zero;

        Canvas.ForceUpdateCanvases();
        // Reserve a visible top margin before the piece artwork. Without it the
        // selected piece appeared glued to the deployment band above.
        float cellHeight = Mathf.Clamp(slotRect.rect.height - 24f, 82f, 112f);

        var grid = slotRect.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            grid.enabled = true;
            grid.cellSize = new Vector2(164f, cellHeight);
            grid.spacing = new Vector2(12f, 10f);
            grid.padding = new RectOffset(4, 4, 30, 4);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.childAlignment = TextAnchor.UpperCenter;
        }

        var fitter = slotRect.GetComponent<ResponsiveGridFitter>();
        if (fitter != null)
        {
            fitter.enabled = false;
            Destroy(fitter);
        }

        var sizeFitter = slotRect.GetComponent<ContentSizeFitter>();
        if (sizeFitter != null)
        {
            sizeFitter.enabled = false;
        }

        if (grid != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(slotRect);
        }
    }

    public void SetupSlots(int slotCount)
    {
        ClearSlotButtons();

        slotButtons.Clear();
        formationSlots = new CharacterData[slotCount];  // ← 修正

        for (int i = 0; i < slotCount; i++)
        {
            var slot = Instantiate(slotButtonPrefab, slotParent);
            slot.name = $"Slot{i+1}";
            slotButtons.Add(slot);
            ApplySlotAppearance(slot);

            // ボタンテキストに「空」を表示
            var text = slot.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (text != null)
            {
                UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
                text.text = "空";
                text.enableAutoSizing = true;
                text.fontSizeMin = UnityUIRuntimeTheme.MinimumTextSize;
                text.fontSizeMax = 22f;
                text.alignment = TMPro.TextAlignmentOptions.Center;
                text.color = new Color(1f, 0.95f, 0.82f);
                text.raycastTarget = false;
            }

            int index = i;
            slot.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
            {
                // クリックでキャラ解除
                ClearSlot(index);
            });
        }

        RestoreLastBattleFormation(false);
        EnsureStarterFormation();
        RefreshInformationRegion();
    }

    private void ClearSlotButtons()
    {
        for (int i = slotParent.childCount - 1; i >= 0; i--)
        {
            var child = slotParent.GetChild(i);
            child.gameObject.SetActive(false);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }

    public void SetCharacterToSlot(int index, CharacterData character)
    {
        if (index < 0 || index >= formationSlots.Length) return;

        formationSlots[index] = character;
        inspectedCharacter = character;

        var slot = GetSlotObject(index);
        if (slot == null) return;

        var text = slot.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (text != null)
        {
            UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
            text.text = character.characterName;
            text.enableAutoSizing = true;
            text.fontSizeMin = UnityUIRuntimeTheme.MinimumTextSize;
            text.fontSizeMax = 22f;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.color = new Color(1f, 0.95f, 0.82f);
            text.raycastTarget = false;
            text.transform.SetAsLastSibling();
        }

        SetSlotPieceIcon(slot, character);

        RefreshInformationRegion();
    }

    public void ShowCharacterDetails(CharacterData character)
    {
        if (character == null)
        {
            return;
        }

        inspectedCharacter = character;
        RefreshInformationRegion();
    }

    public void ClearSlot(int index)
    {
        if (index < 0 || index >= formationSlots.Length) return;

        var removedCharacter = formationSlots[index];
        formationSlots[index] = null;

        var slot = GetSlotObject(index);
        if (slot == null) return;

        var text = slot.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (text != null)
        {
            text.text = "空";
        }

        SetSlotPieceIcon(slot, null);

        if (removedCharacter != null)
        {
            Debug.Log($"{removedCharacter.characterName} を編成から外しました");
        }

        RefreshInformationRegion();
    }

    public void ClearAllSlots()
    {
        if (formationSlots == null) return;

        for (int i = 0; i < formationSlots.Length; i++)
        {
            ClearSlot(i);
        }
    }

    public void RestoreLastBattleFormation()
    {
        RestoreLastBattleFormation(true);
    }

    public void RememberCurrentFormation()
    {
        lastBattleFormation.Clear();
        if (formationSlots == null) return;

        foreach (var character in formationSlots)
        {
            if (character != null)
            {
                lastBattleFormation.Add(character);
            }
        }
    }

    private void RestoreLastBattleFormation(bool clearFirst)
    {
        if (formationSlots == null || lastBattleFormation.Count == 0)
        {
            return;
        }

        if (clearFirst)
        {
            ClearAllSlots();
        }

        int slotIndex = 0;
        foreach (var character in lastBattleFormation)
        {
            if (character == null || slotIndex >= formationSlots.Length) break;
            if (!CanUseCharacter(character)) continue;

            while (slotIndex < formationSlots.Length && formationSlots[slotIndex] != null)
            {
                slotIndex++;
            }
            if (slotIndex >= formationSlots.Length) break;

            SetCharacterToSlot(slotIndex, character);
            slotIndex++;
        }
    }

    private static bool CanUseCharacter(CharacterData character)
    {
        if (PlayerInventory.Instance == null || character == null) return false;
        return PlayerInventory.Instance.GetOwnedCharacters().ContainsKey(character);
    }

    private GameObject GetSlotObject(int index)
    {
        if (index < 0 || index >= slotButtons.Count)
        {
            return null;
        }

        return slotButtons[index];
    }

    private void EnsureUtilityButtons()
    {
        if (slotParent == null || slotParent.parent == null) return;

        var host = slotParent.parent.Find("FormationUtilityButtons") as RectTransform;
        if (host == null)
        {
            var hostObject = new GameObject("FormationUtilityButtons", typeof(RectTransform));
            hostObject.transform.SetParent(slotParent.parent, false);
            host = hostObject.GetComponent<RectTransform>();
        }

        // Keep this beside the formation tray rather than in a row between the
        // formation and the owned-piece list. The previous row collided with the
        // list on wide screens and made the action look detached from its target.
        // Align the utility action with the bottom of the piece art. This keeps
        // the row visually coherent while leaving a small breathing space above
        // the active formation.
        host.anchorMin = new Vector2(0.76f, 0.560f);
        host.anchorMax = new Vector2(0.92f, 0.620f);
        host.offsetMin = Vector2.zero;
        host.offsetMax = Vector2.zero;
        var layout = host.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
            layout.enabled = false;
        }

        clearAllButton = EnsureButton(host, "ClearFormationButton", "全解除");
        Transform restore = host.Find("RestoreLastFormationButton");
        if (restore != null)
        {
            restore.gameObject.SetActive(false);
        }
        clearAllButton.onClick.RemoveAllListeners();
        clearAllButton.onClick.AddListener(ClearAllSlots);
    }

    private void EnsurePrimaryActionButtons()
    {
        if (slotParent == null || slotParent.parent == null)
        {
            return;
        }

        var host = slotParent.parent.Find("FormationPrimaryActions") as RectTransform;
        if (host == null)
        {
            var hostObject = new GameObject("FormationPrimaryActions", typeof(RectTransform));
            hostObject.transform.SetParent(slotParent.parent, false);
            host = hostObject.GetComponent<RectTransform>();
        }

        host.anchorMin = new Vector2(0.08f, 0.670f);
        host.anchorMax = new Vector2(0.92f, 0.730f);
        host.offsetMin = Vector2.zero;
        host.offsetMax = Vector2.zero;
        host.SetAsLastSibling();

        Transform root = slotParent.parent;
        var existingBack = root.Find("BackToMarchButton");
        backToMarchButton = existingBack != null
            ? existingBack.GetComponent<Button>()
            : EnsureButton(root, "BackToMarchButton", "行軍へ戻る");
        SetButtonText(backToMarchButton, "行軍へ戻る");
        ModernWafuuPresentation.ApplyButtonTreatment(backToMarchButton);
        deployButton = EnsureButton(host, "DeployButton", "出陣");
        LayoutPrimaryActionButton(deployButton, Vector2.zero, Vector2.one);
        LayoutSecondaryActionButton(backToMarchButton, new Vector2(0.04f, 0.025f), new Vector2(0.42f, 0.065f));
        backToMarchButton.transform.SetAsLastSibling();
        ApplyDeployButtonTreatment(deployButton);

        backToMarchButton.onClick.RemoveAllListeners();
        backToMarchButton.onClick.AddListener(() => UIManager.Instance?.ShowStageSelect());
        deployButton.onClick.RemoveAllListeners();
        deployButton.onClick.AddListener(() => UIManager.Instance?.StartBattleFromFormation());
    }

    private static void LayoutPrimaryActionButton(Button button, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void LayoutSecondaryActionButton(Button button, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void ApplyDeployButtonTreatment(Button button)
    {
        if (button == null)
        {
            return;
        }

        var image = button.GetComponent<Image>();
        ModernWafuuPresentation.ApplyFlatSurface(image, new Color(0.67f, 0.10f, 0.06f, 1f));
        var outline = button.GetComponent<Outline>() ?? button.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.74f, 0.30f, 0.88f);
        outline.effectDistance = new Vector2(2f, -2f);

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.89f, 0.73f, 1f);
        colors.pressedColor = new Color(0.86f, 0.68f, 0.50f, 1f);
        colors.disabledColor = new Color(0.45f, 0.31f, 0.27f, 0.95f);
        button.colors = colors;
    }

    private void ApplyActionLabels()
    {
        Transform start = transform.Find("StartBattleButton");
        var startText = start != null ? start.GetComponentInChildren<TextMeshProUGUI>(true) : null;
        if (startText != null)
        {
            UnityUIRuntimeTheme.EnsureJapaneseCapableFont(startText);
            startText.text = "出陣";
        }

        // The bottom primary-action row owns the explicit navigation and deployment actions.
        if (start != null)
        {
            start.gameObject.SetActive(false);
        }

        Transform back = transform.Find("BackButton");
        if (back != null)
        {
            back.gameObject.SetActive(false);
        }
    }

    private void RefreshInformationRegion()
    {
        informationRegion = ScreenInfoRegion.Ensure(
            transform,
            "FormationInformationRegion",
            new Vector2(0.04f, 0.75f),
            new Vector2(0.96f, 0.90f));
        if (informationRegion == null)
        {
            return;
        }

        StageData stage = GameManager.Instance != null ? GameManager.Instance.GetSelectedStage() : null;
        int selected = 0;
        int capacity = formationSlots != null ? formationSlots.Length : 0;
        if (formationSlots != null)
        {
            foreach (CharacterData character in formationSlots)
            {
                if (character != null) selected++;
            }
        }

        string title;
        string body;
        if (inspectedCharacter != null && PlayerInventory.Instance != null)
        {
            int level = PlayerInventory.Instance.PlayerLevel;
            title = $"{inspectedCharacter.characterName}　現在 Lv.{level}　{SkillDescription.GetShort(inspectedCharacter.skillType)}";
            int maxHP = PlayerInventory.Instance.GetPlayerMaxHP(inspectedCharacter, level);
            body = $"HP {maxHP}　攻 {inspectedCharacter.GetAttack(level)}　守 {inspectedCharacter.GetDefense(level)}\n{GetSkillSummary(inspectedCharacter)}";
        }
        else
        {
            title = stage != null
                ? $"第{stage.stageId:00}局  {stage.stageName}"
                : "出陣編成";
            body = capacity > 0
                ? $"編成 {selected} / {capacity}　所有駒を選ぶと、ここに現在の能力とスキル概要を表示します。"
                : "出陣する駒を選んでください。";
        }
        informationRegion.Configure(title, body);
        if (deployButton != null)
        {
            deployButton.interactable = selected > 0;
        }
    }

    private static string GetSkillSummary(CharacterData character)
    {
        string detail = SkillDescription.GetDetail(character);
        int firstLineEnd = detail.IndexOf('\n');
        if (firstLineEnd >= 0)
        {
            detail = detail[(firstLineEnd + 1)..];
        }

        int sentenceEnd = detail.IndexOf('。');
        return sentenceEnd >= 0 ? detail[..(sentenceEnd + 1)] : detail;
    }

    private static void ApplySlotAppearance(GameObject slot)
    {
        if (slot == null)
        {
            return;
        }

        var image = slot.GetComponent<Image>();
        ModernWafuuPresentation.ApplyListCardSurface(image, new Color(0.15f, 0.12f, 0.08f, 0.96f));
        var outline = slot.GetComponent<Outline>() ?? slot.AddComponent<Outline>();
        outline.effectColor = new Color(0.83f, 0.61f, 0.29f, 0.70f);
        outline.effectDistance = new Vector2(1f, -1f);

        var layout = slot.GetComponent<LayoutElement>() ?? slot.AddComponent<LayoutElement>();
        layout.minWidth = 190f;
        layout.minHeight = 96f;
        layout.preferredWidth = 190f;
        layout.preferredHeight = 96f;
    }

    private static void SetSlotPieceIcon(GameObject slot, CharacterData character)
    {
        if (slot == null)
        {
            return;
        }

        Transform existing = slot.transform.Find("PieceIcon");
        Image icon;
        if (existing != null)
        {
            icon = existing.GetComponent<Image>();
        }
        else
        {
            var iconObject = new GameObject("PieceIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(AspectRatioFitter));
            iconObject.transform.SetParent(slot.transform, false);
            icon = iconObject.GetComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var newIconFitter = iconObject.GetComponent<AspectRatioFitter>();
            newIconFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            newIconFitter.aspectRatio = 1f;
        }

        icon.sprite = character != null ? character.icon : null;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = new Vector2(0.03f, 0.15f);
        iconRect.anchorMax = new Vector2(0.97f, 0.88f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;
        var fitter = icon.GetComponent<AspectRatioFitter>();
        if (fitter != null)
        {
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;
        }
        icon.gameObject.SetActive(icon.sprite != null);

        var label = slot.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            RectTransform labelRect = label.rectTransform;
            bool hasIcon = icon.sprite != null;
            // 駒絵に漢字名が入っているため、編成枠で同じ名前を重ねない。
            // 空枠だけは選択可能な場所と分かるように既存ラベルを残す。
            label.gameObject.SetActive(!hasIcon);
            labelRect.anchorMin = hasIcon ? new Vector2(0.06f, 0.02f) : new Vector2(0.06f, 0.12f);
            labelRect.anchorMax = hasIcon ? new Vector2(0.94f, 0.18f) : new Vector2(0.94f, 0.88f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.alignment = TextAlignmentOptions.Center;
            label.transform.SetAsLastSibling();
        }
    }

    private static Button EnsureButton(Transform parent, string name, string label)
    {
        var child = parent.Find(name);
        Button button;
        if (child != null && child.TryGetComponent(out button))
        {
            SetButtonText(button, label);
            ModernWafuuPresentation.ApplyButtonTreatment(button);
            return button;
        }

        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        button = go.GetComponent<Button>();
        ModernWafuuPresentation.ApplyButtonTreatment(button);

        var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(go.transform, false);
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(8f, 2f);
        rect.offsetMax = new Vector2(-8f, -2f);
        SetButtonText(button, label);

        return button;
    }

    private static void SetButtonText(Button button, string label)
    {
        var text = button.GetComponentInChildren<TextMeshProUGUI>();
        if (text == null) return;

        UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
        text.text = label;
        text.enableAutoSizing = true;
        text.fontSizeMin = UnityUIRuntimeTheme.MinimumTextSize;
        text.fontSizeMax = 20f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.95f, 0.82f);
        text.raycastTarget = false;
    }

    public CharacterData[] GetFormation() => formationSlots;

    public IReadOnlyList<CharacterData> GetFormationPreview()
    {
        var preview = new List<CharacterData>();
        if (formationSlots != null)
        {
            foreach (CharacterData character in formationSlots)
            {
                if (character != null)
                {
                    preview.Add(character);
                }
            }
        }

        if (preview.Count == 0)
        {
            preview.AddRange(lastBattleFormation.FindAll(character => character != null));
        }

        return preview;
    }

    public bool HasSelectedCharacter()
    {
        if (formationSlots == null)
        {
            return false;
        }

        foreach (CharacterData character in formationSlots)
        {
            if (character != null)
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureStarterFormation()
    {
        if (HasSelectedCharacter() || PlayerInventory.Instance == null || formationSlots == null || formationSlots.Length == 0)
        {
            return;
        }

        foreach (CharacterData character in PlayerInventory.Instance.GetUnlockedCharacters())
        {
            if (character != null && character.characterId == 1)
            {
                SetCharacterToSlot(0, character);
                return;
            }
        }
    }
}
