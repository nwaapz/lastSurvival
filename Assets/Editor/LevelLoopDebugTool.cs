using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor window for managing Level Loop Mode settings.
/// Provides quick access to loop configuration and testing.
/// </summary>
public class LevelLoopDebugTool : EditorWindow
{
    private Vector2 _scrollPos;
    
    [MenuItem("Tools/Scenario/Level Loop Debug Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<LevelLoopDebugTool>("Level Loop Tool");
        window.minSize = new Vector2(350, 400);
    }
    
    private void OnGUI()
    {
        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        
        EditorGUILayout.LabelField("Level Loop Debug Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        
        // Find ScenarioManager in scene or prefab
        ScenarioManager manager = FindScenarioManager();
        
        if (manager == null)
        {
            EditorGUILayout.HelpBox("ScenarioManager not found in scene. Make sure it exists in your scene or as a prefab.", MessageType.Warning);
            
            if (GUILayout.Button("Create ScenarioManager"))
            {
                CreateScenarioManager();
            }
            
            EditorGUILayout.EndScrollView();
            return;
        }
        
        // Get SerializedObject to edit properties
        SerializedObject serializedManager = new SerializedObject(manager);
        serializedManager.Update();
        
        // Loop Mode Settings Section
        DrawLoopModeSettings(serializedManager);
        
        EditorGUILayout.Space(20);
        
        // Runtime Status (only in play mode)
        DrawRuntimeStatus(manager);
        
        EditorGUILayout.Space(20);
        
        // Quick Actions
        DrawQuickActions(manager, serializedManager);
        
        serializedManager.ApplyModifiedProperties();
        
        EditorGUILayout.EndScrollView();
    }
    
    private void DrawLoopModeSettings(SerializedObject serializedManager)
    {
        EditorGUILayout.LabelField("Loop Mode Configuration", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        SerializedProperty enableLoop = serializedManager.FindProperty("enableLevelLoop");
        SerializedProperty threshold = serializedManager.FindProperty("levelLoopThreshold");
        SerializedProperty loopScene = serializedManager.FindProperty("loopSceneName");
        
        EditorGUILayout.PropertyField(enableLoop, new GUIContent("Enable Level Loop", "When enabled, levels at or above the threshold will loop instead of running scenarios"));
        
        EditorGUI.BeginDisabledGroup(!enableLoop.boolValue);
        
        EditorGUILayout.PropertyField(threshold, new GUIContent("Loop Threshold Level", "Level number where loop mode begins (e.g., 8 means levels 8+ will loop)"));
        
        // Visual indicator
        EditorGUILayout.HelpBox($"Levels 1-{threshold.intValue - 1}: Normal Scenarios\nLevels {threshold.intValue}+: Loop Mode ONLY if no scenario exists\n(scenarios always run normally)", MessageType.Info);
        
        EditorGUILayout.PropertyField(loopScene, new GUIContent("Loop Scene", "Scene to reload in loop mode. Leave empty to reload current scene."));
        
        EditorGUI.EndDisabledGroup();
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawRuntimeStatus(ScenarioManager manager)
    {
        EditorGUILayout.LabelField("Runtime Status", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to see runtime status", MessageType.Info);
        }
        else
        {
            // Current level
            int currentLevel = manager.CurrentLevelNumber;
            EditorGUILayout.LabelField("Current Level:", currentLevel.ToString());
            
            // Loop status
            bool inLoopMode = manager.IsInLoopMode;
            EditorGUILayout.LabelField("In Loop Mode:", inLoopMode ? "🔄 YES" : "No");
            
            if (inLoopMode)
            {
                EditorGUILayout.HelpBox("Game is in loop mode. Clicking Continue will increment level and reload scene.", MessageType.Info);
            }
            
            // Save data
            if (SaveManager.HasInstance && SaveManager.Instance.Data != null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Save Data Level:", SaveManager.Instance.Data.CurrentLevel.ToString());
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawQuickActions(ScenarioManager manager, SerializedObject serializedManager)
    {
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // Preset buttons for common thresholds
        EditorGUILayout.LabelField("Set Loop Threshold:");
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Level 5")) SetThreshold(serializedManager, 5);
        if (GUILayout.Button("Level 8")) SetThreshold(serializedManager, 8);
        if (GUILayout.Button("Level 10")) SetThreshold(serializedManager, 10);
        if (GUILayout.Button("Level 15")) SetThreshold(serializedManager, 15);
        
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("Runtime Actions:");
            
            // Set level actions
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Set to Level 7"))
            {
                SetCurrentLevel(7);
            }
            if (GUILayout.Button("Set to Level 8"))
            {
                SetCurrentLevel(8);
            }
            if (GUILayout.Button("Set to Level 10"))
            {
                SetCurrentLevel(10);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // Loop trigger
            if (manager.IsInLoopMode)
            {
                GUI.backgroundColor = Color.cyan;
                if (GUILayout.Button("🔄 Trigger Level Loop", GUILayout.Height(30)))
                {
                    manager.TriggerLevelLoop();
                }
                GUI.backgroundColor = Color.white;
            }
            else
            {
                EditorGUILayout.HelpBox("Reach loop threshold level to trigger loop mode", MessageType.Info);
            }
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private void SetThreshold(SerializedObject serializedManager, int value)
    {
        SerializedProperty threshold = serializedManager.FindProperty("levelLoopThreshold");
        threshold.intValue = value;
        serializedManager.ApplyModifiedProperties();
        Debug.Log($"[LevelLoopDebugTool] Set loop threshold to level {value}");
    }
    
    private void SetCurrentLevel(int level)
    {
        if (SaveManager.HasInstance && SaveManager.Instance.Data != null)
        {
            SaveManager.Instance.SetLevel(level);
            
            // Also reload scenario
            if (ScenarioManager.HasInstance)
            {
                ScenarioManager.Instance.LoadAndStartCurrentLevelScenario();
            }
            
            Debug.Log($"[LevelLoopDebugTool] Set current level to {level}");
        }
    }
    
    private ScenarioManager FindScenarioManager()
    {
        // Try to find in scene
        ScenarioManager manager = FindObjectOfType<ScenarioManager>();
        
        if (manager == null)
        {
            // Try to find prefab
            string[] guids = AssetDatabase.FindAssets("t:Prefab ScenarioManager");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    manager = prefab.GetComponent<ScenarioManager>();
                    if (manager != null) break;
                }
            }
        }
        
        return manager;
    }
    
    private void CreateScenarioManager()
    {
        GameObject go = new GameObject("ScenarioManager");
        go.AddComponent<ScenarioManager>();
        Selection.activeGameObject = go;
        Debug.Log("[LevelLoopDebugTool] Created new ScenarioManager GameObject");
    }
    
    private void OnInspectorUpdate()
    {
        // Repaint during play mode to update runtime status
        if (Application.isPlaying)
        {
            Repaint();
        }
    }
}
