using UnityEngine;
using UnityEngine.UI;

public class CharacterListUI : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private GameObject characterEntryPrefab;

    private void Start()
    {
        RefreshList(); // 初回描画
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.onInventoryChanged += RefreshList;
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCostModifiersChanged += RefreshList;
        }
    }

    private void OnDestroy()
    {
        if (PlayerInventory.Instance != null)
        {
            PlayerInventory.Instance.onInventoryChanged -= RefreshList;
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnCostModifiersChanged -= RefreshList;
        }
    }

    public void RefreshList()
    {
        if (PlayerInventory.Instance == null) 
        {
            return;
        }

        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }

        foreach (var kv in PlayerInventory.Instance.GetOwnedCharacters())
        {
            var entry = Instantiate(characterEntryPrefab, content);
            var ui = entry.GetComponent<CharacterEntryUI>();
            ui.SetCharacter(kv.Key, kv.Value.level, kv.Value.count);
        }
    }
}