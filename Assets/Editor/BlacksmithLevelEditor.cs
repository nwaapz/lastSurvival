using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor tool to set the blacksmith building level in save data.
/// Window -> 4X Game -> Set Blacksmith Level
/// </summary>
public class BlacksmithLevelEditor : EditorWindow
{
    private const string BLACKSMITH_ID = "blacksmith";
    
    private int targetLevel = 1;
    private int currentLevel = 0;
    
    [MenuItem("Window/4X Game/Set Blacksmith Level")]
    public static void ShowWindow()
    {
        var window = GetWindow<BlacksmithLevelEditor>("Blacksmith Level");
        window.minSize = new Vector2(300, 200);
        window.Show();
    }
    
    private void OnEnable()
    {
        RefreshCurrentLevel();
    }
    
    private void OnFocus()
    {
        RefreshCurrentLevel();
    }
    
    private void RefreshCurrentLevel()
    {
        currentLevel = 0;
        
        if (!Application.isPlaying)
        {
            var data = LoadSaveData();
            if (data != null)
            {
                var entry = data.BuildingLevels.Find(b => b.BuildingId == BLACKSMITH_ID);
                if (entry != null)
                    currentLevel = entry.Level;
            }
        }
        else if (SaveManager.HasInstance && SaveManager.Instance.Data != null)
        {
            var entry = SaveManager.Instance.Data.BuildingLevels.Find(b => b.BuildingId == BLACKSMITH_ID);
            if (entry != null)
                currentLevel = entry.Level;
        }
    }
    
    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Blacksmith Level Editor", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Set the blacksmith building level in save data.\n" +
            "This affects upgrade capacity in fight scenes.",
            MessageType.Info);
        
        GUILayout.Space(10);
        
        // Current Level
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Current State", EditorStyles.boldLabel);
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.IntField("Current Blacksmith Level", currentLevel);
        EditorGUI.EndDisabledGroup();
        if (GUILayout.Button("Refresh"))
            RefreshCurrentLevel();
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // Set Level
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Set Level", EditorStyles.boldLabel);
        targetLevel = EditorGUILayout.IntSlider("Target Level", targetLevel, 0, 6);
        
        if (GUILayout.Button($"Set Blacksmith to Level {targetLevel}", GUILayout.Height(30)))
        {
            if (Application.isPlaying)
                SetLevelRuntime(targetLevel);
            else
                SetLevelEditMode(targetLevel);
        }
        EditorGUILayout.EndVertical();
        
        GUILayout.Space(10);
        
        // Quick Buttons
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Quick Set", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        for (int i = 0; i <= 6; i++)
        {
            if (GUILayout.Button($"{i}", GUILayout.Width(35)))
            {
                if (Application.isPlaying)
                    SetLevelRuntime(i);
                else
                    SetLevelEditMode(i);
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }
    
    private void SetLevelRuntime(int level)
    {
        if (!SaveManager.HasInstance || SaveManager.Instance.Data == null)
        {
            EditorUtility.DisplayDialog("Error", "SaveManager not available.", "OK");
            return;
        }
        
        var entry = SaveManager.Instance.Data.BuildingLevels.Find(b => b.BuildingId == BLACKSMITH_ID);
        if (entry != null)
            entry.Level = level;
        else
            SaveManager.Instance.Data.BuildingLevels.Add(new BuildingSaveEntry { BuildingId = BLACKSMITH_ID, Level = level });
        
        SaveManager.Instance.SaveGame();
        RefreshCurrentLevel();
        Debug.Log($"[BlacksmithLevelEditor] Set blacksmith level to {level}");
    }
    
    private void SetLevelEditMode(int level)
    {
        var data = LoadSaveData() ?? new GameData();
        
        var entry = data.BuildingLevels.Find(b => b.BuildingId == BLACKSMITH_ID);
        if (entry != null)
            entry.Level = level;
        else
            data.BuildingLevels.Add(new BuildingSaveEntry { BuildingId = BLACKSMITH_ID, Level = level });
        
        SaveData(data);
        RefreshCurrentLevel();
        Debug.Log($"[BlacksmithLevelEditor] Set blacksmith level to {level} (edit mode)");
    }
    
    private string GetSavePath() => System.IO.Path.Combine(Application.persistentDataPath, "gamedata.json");
    
    private GameData LoadSaveData()
    {
        string path = GetSavePath();
        if (!System.IO.File.Exists(path)) return null;
        
        try
        {
            return JsonUtility.FromJson<GameData>(System.IO.File.ReadAllText(path));
        }
        catch { return null; }
    }
    
    private void SaveData(GameData data)
    {
        try
        {
            System.IO.File.WriteAllText(GetSavePath(), JsonUtility.ToJson(data, true));
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to save: {e.Message}", "OK");
        }
    }
}
