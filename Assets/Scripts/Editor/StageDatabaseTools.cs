using UnityEditor;
using UnityEngine;

namespace KanjiBattle.Editor
{
    public static class StageDatabaseTools
    {
        private const string StageDatabasePath = "Assets/ScriptableObjects/Stages/StageDatabase.asset";

        [MenuItem("KanjiBattle/Tools/Repair Stage Database Order")]
        public static void RepairAndSave()
        {
            StageDatabase database = AssetDatabase.LoadAssetAtPath<StageDatabase>(StageDatabasePath);
            if (database == null)
            {
                throw new System.Exception($"StageDatabase not found: {StageDatabasePath}");
            }

            database.RepairMissingStageIdsFromAssetNames();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            Debug.Log("StageDatabase order repaired.");
        }
    }
}
