using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class OutlineEffect : MonoBehaviour
{
    [Header("Outline Settings")]
    public Color outlineColor = Color.black;
    [Range(0.001f, 0.1f)]
    public float outlineWidth = 0.01f;
    
    [Header("References")]
    public Material outlineMaterial;
    
    private Renderer targetRenderer;
    private GameObject outlineObject;
    private Renderer outlineRenderer;
    
    private static Material sharedOutlineMaterial;
    
    void Start()
    {
        SetupOutline();
    }
    
    void SetupOutline()
    {
        targetRenderer = GetComponent<Renderer>();
        
        // Create outline material if not assigned
        if (outlineMaterial == null)
        {
            var shader = Shader.Find("Custom/OutlineMask");
            if (shader != null)
            {
                outlineMaterial = new Material(shader);
            }
            else
            {
                Debug.LogError("OutlineEffect: Could not find Custom/OutlineMask shader!");
                return;
            }
        }
        
        // Create a duplicate mesh for the outline
        outlineObject = new GameObject("Outline");
        outlineObject.transform.SetParent(transform, false);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one;
        
        // Copy mesh filter/renderer
        if (targetRenderer is SkinnedMeshRenderer skinnedMesh)
        {
            var outlineSkinned = outlineObject.AddComponent<SkinnedMeshRenderer>();
            outlineSkinned.sharedMesh = skinnedMesh.sharedMesh;
            outlineSkinned.bones = skinnedMesh.bones;
            outlineSkinned.rootBone = skinnedMesh.rootBone;
            outlineSkinned.material = outlineMaterial;
            outlineRenderer = outlineSkinned;
        }
        else if (targetRenderer is MeshRenderer meshRenderer)
        {
            var meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                outlineObject.AddComponent<MeshFilter>().sharedMesh = meshFilter.sharedMesh;
                var outlineMeshRenderer = outlineObject.AddComponent<MeshRenderer>();
                outlineMeshRenderer.material = outlineMaterial;
                outlineRenderer = outlineMeshRenderer;
            }
        }
        
        UpdateOutlineMaterial();
    }
    
    void Update()
    {
        UpdateOutlineMaterial();
    }
    
    void UpdateOutlineMaterial()
    {
        if (outlineMaterial != null)
        {
            outlineMaterial.SetColor("_OutlineColor", outlineColor);
            outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
        }
    }
    
    void OnDestroy()
    {
        if (outlineObject != null)
        {
            Destroy(outlineObject);
        }
        if (outlineMaterial != null && !outlineMaterial.name.Contains("Instance"))
        {
            Destroy(outlineMaterial);
        }
    }
    
    void OnValidate()
    {
        if (Application.isPlaying && outlineMaterial != null)
        {
            UpdateOutlineMaterial();
        }
    }
}
