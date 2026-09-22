using UnityEngine;

// Keeps PlayerPrefs access in one place while preserving the existing save-key format.
public static class PlayerProgressStore
{
    private const string SaveFormatKey = "KanjiBattle.SaveFormatVersion";
    private const int CurrentSaveFormatVersion = 4;
    private static bool formatValidatedThisSession;

    public static int GetInt(string key, int defaultValue = 0) => PlayerPrefs.GetInt(key, defaultValue);
    public static string GetString(string key, string defaultValue = "") => PlayerPrefs.GetString(key, defaultValue);
    public static void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
    public static void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
    public static void Delete(string key) => PlayerPrefs.DeleteKey(key);
    public static void Save() => PlayerPrefs.Save();

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
