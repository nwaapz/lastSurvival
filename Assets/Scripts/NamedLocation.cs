using UnityEngine;

/// <summary>
/// Marks a location in the world with a name so the camera can move to it.
/// Place this on empty GameObjects at important locations (Port, Factory, Colosseum, etc.)
/// </summary>
public class NamedLocation : MonoBehaviour
{
    [Header("Location Info")]
    [Tooltip("Select from predefined locations (recommended)")]
    public LocationName location = LocationName.None;
    
    [Tooltip("Legacy: String identifier (use Location enum instead). Auto-synced from enum if empty.")]
    public string locationId;
    
    [Tooltip("Display name shown to player (optional, defaults to locationId)")]
    public string displayName;
    
    [Header("Camera Settings")]
    [Tooltip("Camera will move to this offset from the location's position (usually just use the location position itself for 2D)")]
    public Vector3 cameraOffset = Vector3.zero;
    
    [Tooltip("Camera rotation when viewing this location (usually not needed for 2D games)")]
    public Vector3 cameraRotation = Vector3.zero;
    
    [Header("Visual Debug")]
    [Tooltip("Show gizmo in Scene view for easier placement")]
    public bool showGizmo = true;
    public Color gizmoColor = Color.cyan;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? LocationId : displayName;
    
    /// <summary>
    /// Returns the location ID - prefers enum name, falls back to string
    /// </summary>
    public string LocationId
    {
        get
        {
            if (location != LocationName.None)
            {
                return location.ToString();
            }
            return locationId;
        }
    }

    private void OnValidate()
    {
        // Auto-sync locationId from enum if enum is set and string is empty
        if (location != LocationName.None && string.IsNullOrEmpty(locationId))
        {
            locationId = location.ToString();
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo) return;
        
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 1f);
        
        // Draw camera position preview
        Vector3 camPos = transform.position + cameraOffset;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, camPos);
        Gizmos.DrawWireCube(camPos, Vector3.one * 0.5f);
        
#if UNITY_EDITOR
        // Draw label
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2, 
            $"📍 {DisplayName}", 
            new GUIStyle() { normal = new GUIStyleState() { textColor = gizmoColor }, fontSize = 12 });
#endif
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        
        // Draw camera frustum preview when selected
        Gizmos.color = Color.green;
        Vector3 camPos = transform.position + cameraOffset;
        Gizmos.DrawSphere(camPos, 0.3f);
        
        // Draw view direction
        Quaternion rot = Quaternion.Euler(cameraRotation);
        Gizmos.DrawRay(camPos, rot * Vector3.forward * 5f);
    }
}
