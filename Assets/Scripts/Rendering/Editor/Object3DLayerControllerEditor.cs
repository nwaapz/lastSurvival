using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Object3DLayerController))]
[CanEditMultipleObjects]
public class Object3DLayerControllerEditor : Editor
{
    private SerializedProperty sortingLayerID;
    private SerializedProperty orderInLayer;
    private SerializedProperty applyToChildren;

    private void OnEnable()
    {
        sortingLayerID = serializedObject.FindProperty("sortingLayerID");
        orderInLayer = serializedObject.FindProperty("orderInLayer");
        applyToChildren = serializedObject.FindProperty("applyToChildren");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Sorting Settings", EditorStyles.boldLabel);

        // Sorting Layer dropdown (same as SpriteRenderer)
        DrawSortingLayerField();

        // Order in Layer
        EditorGUILayout.PropertyField(orderInLayer, new GUIContent("Order in Layer"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(applyToChildren, new GUIContent("Apply To Children"));

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSortingLayerField()
    {
        // Get all sorting layers
        SortingLayer[] sortingLayers = SortingLayer.layers;
        string[] layerNames = new string[sortingLayers.Length];
        int[] layerIDs = new int[sortingLayers.Length];

        int currentIndex = 0;
        for (int i = 0; i < sortingLayers.Length; i++)
        {
            layerNames[i] = sortingLayers[i].name;
            layerIDs[i] = sortingLayers[i].id;

            if (sortingLayers[i].id == sortingLayerID.intValue)
            {
                currentIndex = i;
            }
        }

        // Draw the popup
        EditorGUI.BeginChangeCheck();
        int newIndex = EditorGUILayout.Popup("Sorting Layer", currentIndex, layerNames);
        if (EditorGUI.EndChangeCheck())
        {
            sortingLayerID.intValue = layerIDs[newIndex];
        }
    }
}
