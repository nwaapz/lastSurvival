using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class StylizedSeaMaterialCreator
{
    [MenuItem("Tools/Create Stylized Sea Material")]
    public static void CreateMaterial()
    {
        // Find the shader
        Shader shader = Shader.Find("Custom/StylizedSea");
        if (shader == null)
        {
            Debug.LogError("Shader 'Custom/StylizedSea' not found. Please ensure StylizedSea.shader is in the project and compiles correctly.");
            return;
        }

        // Create material
        Material material = new Material(shader);
        material.name = "StylizedSea_URP";

        // Save to Assets/_Materials or Assets/
        string path = "Assets/_Materials/StylizedSea_URP.mat";
        if (!AssetDatabase.IsValidFolder("Assets/_Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "_Materials");
        }

        // Ensure unique name
        path = AssetDatabase.GenerateUniqueAssetPath(path);

        AssetDatabase.CreateAsset(material, path);
        AssetDatabase.SaveAssets();

        Selection.activeObject = material;
        EditorGUIUtility.PingObject(material);

        Debug.Log($"Created Stylized Sea Material at {path}");
    }
}
