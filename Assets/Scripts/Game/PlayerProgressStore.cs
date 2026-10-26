using UnityEngine;

// Keeps PlayerPrefs access in one place while preserving the existing save-key format.
public static class PlayerProgressStore
{
#if UNITY_EDITOR
    private static System.Collections.Generic.Dictionary<string, object> isolated;
    public static void BeginIsolatedEditorSession()
    {
        isolated = new System.Collections.Generic.Dictionary<string, object>();
        formatValidatedThisSession = true;
    }
#endif
    private const string SaveFormatKey = "KanjiBattle.SaveFormatVersion";
    private const int CurrentSaveFormatVersion = 5;
    private static bool formatValidatedThisSession;

    public static int GetInt(string key, int defaultValue = 0)
    {
#if UNITY_EDITOR
        if (isolated != null) return isolated.TryGetValue(key, out var value) && value is int number ? number : defaultValue;
#endif
        return PlayerPrefs.GetInt(key, defaultValue);
    }
    public static string GetString(string key, string defaultValue = "")
    {
#if UNITY_EDITOR
        if (isolated != null) return isolated.TryGetValue(key, out var value) && value is string text ? text : defaultValue;
#endif
        return PlayerPrefs.GetString(key, defaultValue);
    }
    public static void SetInt(string key, int value)
    {
#if UNITY_EDITOR
        if (isolated != null) { isolated[key] = value; return; }
#endif
        PlayerPrefs.SetInt(key, value);
    }
    public static void SetString(string key, string value)
    {
#if UNITY_EDITOR
        if (isolated != null) { isolated[key] = value; return; }
#endif
        PlayerPrefs.SetString(key, value);
    }
    public static void Delete(string key)
    {
#if UNITY_EDITOR
        if (isolated != null) { isolated.Remove(key); return; }
#endif
        PlayerPrefs.DeleteKey(key);
    }
    public static void Save()
    {
#if UNITY_EDITOR
        if (isolated != null) return;
#endif
        PlayerPrefs.Save();
    }

    // Progression formats are intentionally not migrated. An old or unknown save starts fresh.
    public static bool EnsureCurrentFormat()
    {
        if (formatValidatedThisSession)
        {
            return true;
        }

        bool hasFormatMarker = PlayerPrefs.HasKey(SaveFormatKey);
        bool hasAnySavedData = HasAnyKanjiBattleSaveData();
        bool isCurrent = hasFormatMarker && PlayerPrefs.GetInt(SaveFormatKey, -1) == CurrentSaveFormatVersion;
        if (!isCurrent && hasAnySavedData)
        {
            PlayerPrefs.DeleteAll();
            Debug.Log("[PlayerProgressStore] 非対応の進行データを検出したため、新規セーブとして初期化しました。");
        }

        PlayerPrefs.SetInt(SaveFormatKey, CurrentSaveFormatVersion);
        PlayerPrefs.Save();
        formatValidatedThisSession = true;
        return isCurrent;
    }

    private static bool HasAnyKanjiBattleSaveData()
    {
        return PlayerPrefs.HasKey(SaveFormatKey)
            || PlayerPrefs.HasKey("KanjiBattle.StagePoints")
            || PlayerPrefs.HasKey("KanjiBattle.HighestClearedStageId")
            || PlayerPrefs.HasKey("KanjiBattle.UnlockedChapter")
            || PlayerPrefs.HasKey("KanjiBattle.BattleSpeedIndex")
            || PlayerPrefs.HasKey("KanjiBattle.Inventory.Owned")
            || PlayerPrefs.HasKey("KanjiBattle.Facilities.Unlocked")
            || PlayerPrefs.HasKey("KanjiBattle.Facilities.Levels")
            || PlayerPrefs.HasKey("KanjiBattle.Facilities.CapUnlocks")
            || PlayerPrefs.HasKey("KanjiBattle.ResearchBond.Bond")
            || PlayerPrefs.HasKey("KanjiBattle.ResearchBond.Defeated")
            ;
    }
}
