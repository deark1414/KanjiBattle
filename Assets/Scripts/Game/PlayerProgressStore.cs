using UnityEngine;

// Keeps PlayerPrefs access in one place while preserving the existing save-key format.
public static class PlayerProgressStore
{
    public static int GetInt(string key, int defaultValue = 0) => PlayerPrefs.GetInt(key, defaultValue);
    public static string GetString(string key, string defaultValue = "") => PlayerPrefs.GetString(key, defaultValue);
    public static void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
    public static void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
    public static void Delete(string key) => PlayerPrefs.DeleteKey(key);
    public static void Save() => PlayerPrefs.Save();
}
