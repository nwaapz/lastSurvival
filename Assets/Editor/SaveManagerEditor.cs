using UnityEditor;
using UnityEngine;
using System.IO;

public static class SaveManagerEditor
{
    [MenuItem("Tools/Clear Save Data")]
    public static void ClearSaveData()
    {
        string saveFilePath = Path.Combine(Application.persistentDataPath, "gamedata.json");
        
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            Debug.Log($"[SaveManagerEditor] Deleted save file at: {saveFilePath}");
        }
        else
        {
            Debug.Log($"[SaveManagerEditor] No save file found at: {saveFilePath}");
        }
        
        // Also reset in-memory data if SaveManager exists (during play mode)
        if (Application.isPlaying && SaveManager.Instance != null)
        {
            SaveManager.Instance.ResetData();
            Debug.Log($"[SaveManagerEditor] Reset in-memory SaveManager data");
        }
    }
    
    [MenuItem("Tools/Open Save Folder")]
    public static void OpenSaveFolder()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}
