using UnityEngine;

/// <summary>
/// Example script showing how to use BaseBuilderClickManager
/// Attach this to any GameObject in the scene to see click handling in action
/// </summary>
public class BaseBuilderClickExample : MonoBehaviour
{
    [Header("Visual Feedback")]
    [SerializeField] private GameObject clickIndicatorPrefab;
    [SerializeField] private float indicatorLifetime = 1f;

    private void Start()
    {
        // Subscribe to click events when the scene starts
        if (BaseBuilderClickManager.Instance != null)
        {
            BaseBuilderClickManager.Instance.OnWorldClicked += HandleWorldClick;
            BaseBuilderClickManager.Instance.OnObjectClicked += HandleObjectClick;
            BaseBuilderClickManager.Instance.OnDragStarted += HandleDragStart;
            BaseBuilderClickManager.Instance.OnDragUpdate += HandleDragUpdate;
            BaseBuilderClickManager.Instance.OnDragEnded += HandleDragEnd;
            
            Debug.Log("[BaseBuilderClickExample] Subscribed to click manager events.");
        }
        else
        {
            Debug.LogWarning("[BaseBuilderClickExample] BaseBuilderClickManager not found in scene!");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events when destroyed to prevent memory leaks
        if (BaseBuilderClickManager.HasInstance)
        {
            BaseBuilderClickManager.Instance.OnWorldClicked -= HandleWorldClick;
            BaseBuilderClickManager.Instance.OnObjectClicked -= HandleObjectClick;
            BaseBuilderClickManager.Instance.OnDragStarted -= HandleDragStart;
            BaseBuilderClickManager.Instance.OnDragUpdate -= HandleDragUpdate;
            BaseBuilderClickManager.Instance.OnDragEnded -= HandleDragEnd;
        }
    }

    private void HandleWorldClick(Vector3 worldPosition)
    {
        Debug.Log($"[Example] World clicked at: {worldPosition}");
        
        // Spawn visual indicator at click position
        if (clickIndicatorPrefab != null)
        {
            GameObject indicator = Instantiate(clickIndicatorPrefab, worldPosition, Quaternion.identity);
            Destroy(indicator, indicatorLifetime);
        }
    }

    private void HandleObjectClick(GameObject clickedObject)
    {
        Debug.Log($"[Example] Object clicked: {clickedObject.name}");
        
        // Example: Change color of clicked object
        var renderer = clickedObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Random.ColorHSV();
        }
        
        // Example: Trigger specific behavior based on object tag
        switch (clickedObject.tag)
        {
            case "Building":
                Debug.Log("[Example] Building clicked! Opening building menu...");
                // Open building upgrade menu, etc.
                break;
                
            case "Unit":
                Debug.Log("[Example] Unit clicked! Selecting unit...");
                // Select unit, show info panel, etc.
                break;
                
            case "Resource":
                Debug.Log("[Example] Resource clicked! Collecting resource...");
                // Collect resource, play animation, etc.
                break;
                
            default:
                Debug.Log($"[Example] Unhandled object type: {clickedObject.tag}");
                break;
        }
    }

    private void HandleDragStart(Vector3 startPos, Vector3 currentPos)
    {
        Debug.Log($"[Example] Drag started from: {startPos}");
        // Could show selection box, drag indicator, etc.
    }

    private void HandleDragUpdate(Vector3 currentPos)
    {
        // Update drag visual, selection box, etc.
        // This fires every frame during drag, so avoid heavy operations
    }

    private void HandleDragEnd(Vector3 endPos)
    {
        Debug.Log($"[Example] Drag ended at: {endPos}");
        // Finalize selection, execute drag action, etc.
    }
}
