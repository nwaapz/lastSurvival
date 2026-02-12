using UnityEngine;
using UnityEditor;
using System.Reflection;

/// <summary>
/// Editor tool to manually set game level and scenario step for testing.
/// Access via Window > Debug > Level & Step Tool
/// </summary>
public class LevelStepDebugTool : EditorWindow
{
    private int targetLevel = 1;
    private int targetStep = 0;

    [MenuItem("Window/Debug/Level & Step Tool")]
    public static void ShowWindow()
    {
        GetWindow<LevelStepDebugTool>("Level & Step Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Level & Step Debug Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Current values display
        EditorGUILayout.LabelField("Current Values", EditorStyles.boldLabel);
        
        if (Application.isPlaying)
        {
            int currentLevel = 1;
            int currentStep = 0;

            if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
            {
                currentLevel = SaveManager.Instance.Data.CurrentLevel;
            }

            if (ScenarioManager.Instance != null)
            {
                currentStep = ScenarioManager.Instance.CurrentStepIndex;
            }

            EditorGUILayout.LabelField("Current Level:", currentLevel.ToString());
            EditorGUILayout.LabelField("Current Step:", currentStep.ToString());
        }
        else
        {
            EditorGUILayout.HelpBox("Enter Play Mode to see current values", MessageType.Info);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Set Values", EditorStyles.boldLabel);

        targetLevel = EditorGUILayout.IntField("Target Level", targetLevel);
        targetStep = EditorGUILayout.IntField("Target Step", targetStep);

        EditorGUILayout.Space();

        GUI.enabled = Application.isPlaying;

        if (GUILayout.Button("Set Level"))
        {
            SetLevel(targetLevel);
        }

        if (GUILayout.Button("Set Step"))
        {
            SetStep(targetStep);
        }

        if (GUILayout.Button("Set Both"))
        {
            SetLevel(targetLevel);
            SetStep(targetStep);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Reset Tutorials (PlayerPrefs)"))
        {
            ResetTutorials();
        }

        GUI.enabled = true;

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Changes take effect immediately in Play Mode.", MessageType.Info);
    }

    private void SetLevel(int level)
    {
        if (SaveManager.Instance != null)
        {
            // Use unified method that also resets step to 0
            SaveManager.Instance.SetLevel(level);
            
            // Also update ScenarioManager's in-memory state
            if (ScenarioManager.Instance != null)
            {
                var levelField = typeof(ScenarioManager).GetField("currentLevelNumber", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                var stepField = typeof(ScenarioManager).GetField("currentStepIndex", 
                    BindingFlags.NonPublic | BindingFlags.Instance);
                
                levelField?.SetValue(ScenarioManager.Instance, level);
                stepField?.SetValue(ScenarioManager.Instance, 0);
            }
            
            Debug.Log($"[LevelStepDebugTool] Level set to {level}, step reset to 0");
        }
        else
        {
            Debug.LogWarning("[LevelStepDebugTool] SaveManager not available");
        }
    }

    private void SetStep(int step)
    {
        if (ScenarioManager.Instance != null)
        {
            // Use reflection to set private field
            var field = typeof(ScenarioManager).GetField("currentStepIndex", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(ScenarioManager.Instance, step);
                
                // Also update SaveManager
                if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
                {
                    SaveManager.Instance.Data.CurrentScenarioStepIndex = step;
                    SaveManager.Instance.SaveGame();
                }
                
                Debug.Log($"[LevelStepDebugTool] Step set to {step}");
            }
            else
            {
                Debug.LogWarning("[LevelStepDebugTool] Could not find currentStepIndex field");
            }
        }
        else
        {
            Debug.LogWarning("[LevelStepDebugTool] ScenarioManager not available");
        }
    }

    private void ResetTutorials()
    {
        PlayerPrefs.DeleteKey("Tutorial_Range_Done");
        PlayerPrefs.DeleteKey("Tutorial_Damage_Done");
        PlayerPrefs.DeleteKey("Tutorial_FireRate_Done");
        PlayerPrefs.Save();
        Debug.Log("[LevelStepDebugTool] All tutorials reset");
    }
}
