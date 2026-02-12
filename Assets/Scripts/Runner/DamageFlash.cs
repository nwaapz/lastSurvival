using UnityEngine;
using System.Collections;

/// <summary>
/// Flashes the renderer red when taking damage.
/// </summary>
public class DamageFlash : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Color flashColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;
    [Tooltip("If empty, it will grab all renderers in children.")]
    [SerializeField] private Renderer[] renderers;
    
    // Shader property IDs for performance
    private static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorID = Shader.PropertyToID("_Color");
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
    
    private Coroutine _flashRoutine;
    private MaterialPropertyBlock _propBlock;
    
    private void Awake()
    {
        _propBlock = new MaterialPropertyBlock();
        
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<Renderer>();
        }
    }
    
    public void Flash()
    {
        if (_flashRoutine != null)
        {
            StopCoroutine(_flashRoutine);
        }
        _flashRoutine = StartCoroutine(FlashRoutine());
    }
    
    private IEnumerator FlashRoutine()
    {
        // Set flash color
        foreach (var r in renderers)
        {
            if (r == null) continue;
            
            r.GetPropertyBlock(_propBlock);
            
            // Try setting both URP and Standard names
            _propBlock.SetColor(BaseColorID, flashColor);
            _propBlock.SetColor(ColorID, flashColor);
            // Also emission for extra pop if supported
            _propBlock.SetColor(EmissionColorID, flashColor * 2f);
            
            r.SetPropertyBlock(_propBlock);
        }
        
        yield return new WaitForSeconds(flashDuration);
        
        // Restore
        foreach (var r in renderers)
        {
            if (r == null) continue;
            
            // clearing the property block restores original material values
            r.SetPropertyBlock(null); 
        }
        
        _flashRoutine = null;
    }
}
