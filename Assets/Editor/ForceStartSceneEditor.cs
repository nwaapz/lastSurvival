#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Editor tool that forces a specific scene to always load when pressing the Play button.
/// Access via menu: Tools > Force Start Scene
/// </summary>
[InitializeOnLoad]
public static class ForceStartSceneEditor
{
    private const string ENABLED_KEY = "ForceStartScene_Enabled";
    private const string SCENE_PATH_KEY = "ForceStartScene_ScenePath";
    private const string MENU_PATH = "Tools/Force Start Scene/";

    static ForceStartSceneEditor()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static bool IsEnabled
    {
        get => EditorPrefs.GetBool(ENABLED_KEY, false);
        set => EditorPrefs.SetBool(ENABLED_KEY, value);
    }

    private static string StartScenePath
    {
        get => EditorPrefs.GetString(SCENE_PATH_KEY, "");
        set => EditorPrefs.SetString(SCENE_PATH_KEY, value);
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!IsEnabled) return;
        
        string scenePath = StartScenePath;
        if (string.IsNullOrEmpty(scenePath)) return;

        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // Don't switch if we're already in the target scene
            string currentScenePath = EditorSceneManager.GetActiveScene().path;
            if (currentScenePath == scenePath) return;

            // Save current scene if needed
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
            }
            else
            {
                // User cancelled, stop entering play mode
                EditorApplication.isPlaying = false;
            }
        }
    }

    // ============ Menu Items ============

    [MenuItem(MENU_PATH + "Enable", false, 0)]
    private static void EnableForceStart()
    {
        IsEnabled = true;
        Debug.Log($"[Force Start Scene] Enabled. Scene: {StartScenePath}");
    }

    [MenuItem(MENU_PATH + "Enable", true)]
    private static bool EnableForceStartValidate()
    {
        return !IsEnabled && !string.IsNullOrEmpty(StartScenePath);
    }

    [MenuItem(MENU_PATH + "Disable", false, 1)]
    private static void DisableForceStart()
    {
        IsEnabled = false;
        Debug.Log("[Force Start Scene] Disabled.");
    }

    [MenuItem(MENU_PATH + "Disable", true)]
    private static bool DisableForceStartValidate()
    {
        return IsEnabled;
    }

    [MenuItem(MENU_PATH + "Set Current Scene as Start Scene", false, 20)]
    private static void SetCurrentSceneAsStart()
    {
        string currentPath = EditorSceneManager.GetActiveScene().path;
        if (!string.IsNullOrEmpty(currentPath))
        {
            StartScenePath = currentPath;
            IsEnabled = true;
            Debug.Log($"[Force Start Scene] Set start scene to: {currentPath}");
        }
        else
        {
            Debug.LogWarning("[Force Start Scene] Current scene is not saved. Please save the scene first.");
        }
    }

    [MenuItem(MENU_PATH + "Select Scene File...", false, 21)]
    private static void SelectSceneFile()
    {
        string path = EditorUtility.OpenFilePanel("Select Start Scene", "Assets/Scenes", "unity");
        if (!string.IsNullOrEmpty(path))
        {
            // Convert absolute path to relative path
            if (path.StartsWith(Application.dataPath))
            {
                path = "Assets" + path.Substring(Application.dataPath.Length);
            }
            StartScenePath = path;
            IsEnabled = true;
            Debug.Log($"[Force Start Scene] Set start scene to: {path}");
        }
    }

    [MenuItem(MENU_PATH + "Show Current Settings", false, 40)]
    private static void ShowSettings()
    {
        string status = IsEnabled ? "ENABLED" : "DISABLED";
        string scene = string.IsNullOrEmpty(StartScenePath) ? "(not set)" : StartScenePath;
        Debug.Log($"[Force Start Scene] Status: {status} | Scene: {scene}");
        EditorUtility.DisplayDialog("Force Start Scene Settings",
            $"Status: {status}\nStart Scene: {scene}", "OK");
    }
}
#endif
