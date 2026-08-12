using System.Collections.Generic;
using UnityEngine;

public class FormationUI : MonoBehaviour
{
    public static FormationUI Instance;

    [SerializeField] private Transform slotParent;
    [SerializeField] private GameObject slotButtonPrefab;
    [SerializeField] private FormationCharacterListUI characterListUI;

    private List<GameObject> slotButtons = new();
    private CharacterData[] formationSlots;   // ← ここが正しいフィールド名

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

        // キャラクターリストを表示更新
        if (characterListUI != null)
        {
            characterListUI.DisplayCharacters();
        }
    }

    public void SetupSlots(int slotCount)
    {
        // 古いスロット削除
        foreach (Transform child in slotParent)
            Destroy(child.gameObject);

        formationSlots = new CharacterData[slotCount];  // ← 修正

        for (int i = 0; i < slotCount; i++)
        {
            var slot = Instantiate(slotButtonPrefab, slotParent);
            slot.name = $"Slot{i+1}";

            // ボタンテキストに「空」を表示
            var text = slot.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (text != null)
            {
                UnityUIRuntimeTheme.ApplyJapaneseFont(text);
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
    }

    public void SetCharacterToSlot(int index, CharacterData character)
    {
        if (index < 0 || index >= formationSlots.Length) return;

        formationSlots[index] = character;

        var slot = slotParent.GetChild(index);
        var text = slot.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (text != null)
        {
            UnityUIRuntimeTheme.ApplyJapaneseFont(text);
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

        var slot = slotParent.GetChild(index);
        var text = slot.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (text != null)
        {
            UnityUIRuntimeTheme.ApplyJapaneseFont(text);
            text.text = "空";
        }

        if (removedCharacter != null)
        {
            Debug.Log($"{removedCharacter.characterName} を編成から外しました");
        }
    }

    public CharacterData[] GetFormation() => formationSlots;
}
