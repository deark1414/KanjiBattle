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
    private Button restoreButton;
    private Button clearAllButton;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void OnEnable()
    {
        // 基本は施設の編成枠だが、ステージが制限を持っている場合は小さい方を採用
        if (GameManager.Instance != null && GameManager.Instance.GetSelectedStage() != null)
        {
            int stageSlots = GameManager.Instance.GetSelectedStage().slotCount;
            int facilitySlots = GameManager.Instance.GetFacilityFormationSlots();
            int totalSlots = Mathf.Min(stageSlots, facilitySlots);
            SetupSlots(totalSlots);
        }

        EnsureUtilityButtons();

        // キャラクターリストを表示更新
        if (characterListUI != null)
        {
            characterListUI.DisplayCharacters();
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

            // ボタンテキストに「空」を表示
            var text = slot.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (text != null)
            {
                UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
                text.text = "空";
                text.enableAutoSizing = true;
                text.fontSizeMin = 12f;
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

        var slot = GetSlotObject(index);
        if (slot == null) return;

        var text = slot.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (text != null)
        {
            UnityUIRuntimeTheme.EnsureJapaneseCapableFont(text);
            text.text = character.characterName;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12f;
            text.fontSizeMax = 22f;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.color = new Color(1f, 0.95f, 0.82f);
            text.raycastTarget = false;
            text.transform.SetAsLastSibling();
        }
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

        if (removedCharacter != null)
        {
            Debug.Log($"{removedCharacter.characterName} を編成から外しました");
        }
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
        var usedCounts = new Dictionary<CharacterData, int>();
        foreach (var character in lastBattleFormation)
        {
            if (character == null || slotIndex >= formationSlots.Length) break;
            if (!CanUseCharacter(character, usedCounts)) continue;

            while (slotIndex < formationSlots.Length && formationSlots[slotIndex] != null)
            {
                slotIndex++;
            }
            if (slotIndex >= formationSlots.Length) break;

            SetCharacterToSlot(slotIndex, character);
            usedCounts.TryGetValue(character, out int used);
            usedCounts[character] = used + 1;
            slotIndex++;
        }
    }

    private static bool CanUseCharacter(CharacterData character, Dictionary<CharacterData, int> usedCounts)
    {
        if (PlayerInventory.Instance == null || character == null) return false;
        if (!PlayerInventory.Instance.GetOwnedCharacters().TryGetValue(character, out var info)) return false;
        usedCounts.TryGetValue(character, out int used);
        return used < info.count;
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
        if (restoreButton != null && clearAllButton != null) return;
        if (slotParent == null || slotParent.parent == null) return;

        var host = slotParent.parent.Find("FormationUtilityButtons") as RectTransform;
        if (host == null)
        {
            var hostObject = new GameObject("FormationUtilityButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            hostObject.transform.SetParent(slotParent.parent, false);
            host = hostObject.GetComponent<RectTransform>();
            host.anchorMin = new Vector2(0.04f, 0.74f);
            host.anchorMax = new Vector2(0.96f, 0.82f);
            host.offsetMin = Vector2.zero;
            host.offsetMax = Vector2.zero;

            var layout = hostObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = true;
        }

        restoreButton = EnsureButton(host, "RestoreLastFormationButton", "前回編成");
        clearAllButton = EnsureButton(host, "ClearFormationButton", "全解除");

        restoreButton.onClick.RemoveAllListeners();
        restoreButton.onClick.AddListener(RestoreLastBattleFormation);
        clearAllButton.onClick.RemoveAllListeners();
        clearAllButton.onClick.AddListener(ClearAllSlots);
    }

    private static Button EnsureButton(Transform parent, string name, string label)
    {
        var child = parent.Find(name);
        Button button;
        if (child != null && child.TryGetComponent(out button))
        {
            SetButtonText(button, label);
            return button;
        }

        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = Resources.Load<Sprite>("Kenney/UIRPG/PNG/buttonLong_brown");
        image.type = image.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = image.sprite != null ? Color.white : new Color(0.58f, 0.41f, 0.24f, 1f);
        button = go.GetComponent<Button>();

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
        text.fontSizeMin = 12f;
        text.fontSizeMax = 20f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.95f, 0.82f);
        text.raycastTarget = false;
    }

    public CharacterData[] GetFormation() => formationSlots;
}
