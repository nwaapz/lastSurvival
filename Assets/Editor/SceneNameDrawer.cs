using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Custom property drawer that displays a dropdown of all scenes in Build Settings.
/// </summary>
[CustomPropertyDrawer(typeof(SceneNameAttribute))]
public class SceneNameDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.PropertyField(position, property, label);
            EditorGUI.HelpBox(position, "SceneName attribute can only be used on string fields.", MessageType.Error);
            return;
        }

        SceneNameAttribute sceneAttr = (SceneNameAttribute)attribute;
        
        // Get all scenes from Build Settings
        List<string> sceneNames = GetSceneNames(sceneAttr.AllowEmpty);
        
        // Find current selection index
        string currentValue = property.stringValue;
        int currentIndex = sceneNames.IndexOf(currentValue);
        
        // If current value not found and not empty, add it as an option (might be a scene not in build settings)
        if (currentIndex < 0 && !string.IsNullOrEmpty(currentValue))
        {
            sceneNames.Add($"{currentValue} (Not in Build)");
            currentIndex = sceneNames.Count - 1;
        }
        else if (currentIndex < 0)
        {
            currentIndex = 0; // Default to first option (empty or first scene)
        }

        EditorGUI.BeginProperty(position, label, property);
        
        // Draw dropdown
        int newIndex = EditorGUI.Popup(position, label.text, currentIndex, sceneNames.ToArray());
        
        // Update value if changed
        if (newIndex != currentIndex || string.IsNullOrEmpty(property.stringValue))
        {
            if (newIndex >= 0 && newIndex < sceneNames.Count)
            {
                string selectedScene = sceneNames[newIndex];
                
                // Handle "(Not in Build)" suffix
                if (selectedScene.EndsWith(" (Not in Build)"))
                {
                    selectedScene = selectedScene.Replace(" (Not in Build)", "");
                }
                
                // Handle empty option
                if (selectedScene == "(None)")
                {
                    selectedScene = "";
                }
                
                property.stringValue = selectedScene;
            }
        }
        
        EditorGUI.EndProperty();
    }

    private List<string> GetSceneNames(bool allowEmpty)
    {
        List<string> scenes = new List<string>();
        
        if (allowEmpty)
        {
            scenes.Add("(None)");
        }
        
        // Get scenes from Build Settings
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                // Extract scene name from path (e.g., "Assets/Scenes/MainMenu.unity" -> "MainMenu")
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scene.path);
                if (!string.IsNullOrEmpty(sceneName))
                {
                    scenes.Add(sceneName);
                }
            }
        }
        
        // If no scenes in build settings, show a message
        if (scenes.Count == 0 || (allowEmpty && scenes.Count == 1))
        {
            scenes.Add("(No scenes in Build Settings)");
        }
        
        return scenes;
    }
}
