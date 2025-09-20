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
        // スロットを現在のステージ仕様に合わせて初期化
        if (GameManager.Instance != null && GameManager.Instance.GetSelectedStage() != null)
        {
            SetupSlots(GameManager.Instance.GetSelectedStage().slotCount);
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
            if (text != null) text.text = "空";

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
        if (text != null) text.text = character.characterName; // 名前を表示
    }

    public void ClearSlot(int index)
    {
        if (index < 0 || index >= formationSlots.Length) return;

        var removedCharacter = formationSlots[index];
        formationSlots[index] = null;

        var slot = slotParent.GetChild(index);
        var text = slot.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        if (text != null) text.text = "空"; // 空に戻す

        if (removedCharacter != null)
        {
            Debug.Log($"{removedCharacter.characterName} を編成から外しました");
        }
    }

    public CharacterData[] GetFormation() => formationSlots;
}