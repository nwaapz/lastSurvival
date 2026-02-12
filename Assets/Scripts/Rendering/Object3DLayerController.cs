using UnityEngine;

/// <summary>
/// Controls the rendering order of a 3D object relative to 2D sprites.
/// Attach this to any 3D object to configure whether it appears above or below 2D sprites.
/// 
/// NOTE: For objects close to camera on Z-axis, you may need to enable 'useCustomRenderQueue'
/// to override depth buffer behavior.
/// </summary>
public class Object3DLayerController : MonoBehaviour
{
    [Header("Sorting Settings")]
    [Tooltip("The sorting layer ID for this 3D object.")]
    [SerializeField] private int sortingLayerID = 0;

    [Tooltip("Order within the sorting layer. Higher values render on top.")]
    [SerializeField] private int orderInLayer = 1;

    [Header("Render Queue (for Z-depth issues)")]
    [Tooltip("Enable to override the material's render queue. Use this when objects close to camera on Z-axis don't respect sorting layers.")]
    [SerializeField] private bool useCustomRenderQueue = false;
    
    [Tooltip("Custom render queue value. Transparent = 3000, Overlay = 4000. Higher values render later (on top).")]
    [SerializeField] private int customRenderQueue = 3000;
    
    [Tooltip("Disable Z-Write so this object doesn't block objects behind it in the depth buffer.")]
    [SerializeField] private bool disableZWrite = false;

    [Header("Options")]
    [SerializeField] private bool applyToChildren = true;

    private Renderer[] renderers;
    private MaterialPropertyBlock propertyBlock;

    private void Awake()
    {
        ApplySorting();
    }

    private void OnValidate()
    {
        ApplySorting();
    }

    private void ApplySorting()
    {
        if (applyToChildren)
        {
            renderers = GetComponentsInChildren<Renderer>(true);
        }
        else
        {
            var renderer = GetComponent<Renderer>();
            renderers = renderer != null ? new Renderer[] { renderer } : new Renderer[0];
        }

        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;

            // Apply sorting layer settings
            renderer.sortingLayerID = sortingLayerID;
            renderer.sortingOrder = orderInLayer;
            
            // Apply render queue override if enabled (fixes Z-depth issues)
            if (useCustomRenderQueue || disableZWrite)
            {
                // We need to modify materials to change render queue and ZWrite
                // Use shared materials in editor, instance materials at runtime
                Material[] materials;
                
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    materials = renderer.sharedMaterials;
                }
                else
                {
                    materials = renderer.materials;
                }
                #else
                materials = renderer.materials;
                #endif
                
                foreach (var mat in materials)
                {
                    if (mat == null) continue;
                    
                    if (useCustomRenderQueue)
                    {
                        mat.renderQueue = customRenderQueue;
                    }
                    
                    if (disableZWrite)
                    {
                        // Disable ZWrite - object won't write to depth buffer
                        mat.SetInt("_ZWrite", 0);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Set the sorting layer and order at runtime
    /// </summary>
    public void SetSorting(string layerName, int order)
    {
        sortingLayerID = SortingLayer.NameToID(layerName);
        orderInLayer = order;
        ApplySorting();
    }

    /// <summary>
    /// Set the sorting layer by ID and order at runtime
    /// </summary>
    public void SetSorting(int layerID, int order)
    {
        sortingLayerID = layerID;
        orderInLayer = order;
        ApplySorting();
    }

    /// <summary>
    /// Set just the order in layer at runtime
    /// </summary>
    public void SetOrderInLayer(int order)
    {
        orderInLayer = order;
        ApplySorting();
    }

    /// <summary>
    /// Render this 3D object above sprites with the same sorting layer
    /// </summary>
    public void RenderAboveSprites(int order = 1)
    {
        orderInLayer = order;
        ApplySorting();
    }

    /// <summary>
    /// Render this 3D object below sprites with the same sorting layer
    /// </summary>
    public void RenderBelowSprites(int order = -1)
    {
        orderInLayer = order;
        ApplySorting();
    }
}
