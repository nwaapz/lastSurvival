using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BuildingView))]
public class BuildingViewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        BuildingView view = (BuildingView)target;
        
        // Show Building ID at the top for easy reference
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Building ID (for BuildObjectiveStep)", EditorStyles.boldLabel);
        
        string buildingId = view.BuildingId;
        
        // Show ID with copy button
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.SelectableLabel(buildingId, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        
        if (GUILayout.Button("Copy", GUILayout.Width(50)))
        {
            EditorGUIUtility.systemCopyBuffer = buildingId;
            Debug.Log($"[BuildingView] Copied building ID to clipboard: {buildingId}");
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
        
        // Draw default inspector
        DrawDefaultInspector();
    }
}
