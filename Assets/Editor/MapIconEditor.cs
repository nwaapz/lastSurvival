using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapIcon))]
public class MapIconEditor : Editor
{
    private SerializedProperty iconIdProperty;

    private void OnEnable()
    {
        iconIdProperty = serializedObject.FindProperty("iconId");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(iconIdProperty, new GUIContent("Icon ID", "Unique identifier for scenario system"));
        
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("Clicking this icon will advance the current scenario step.", MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }
}
