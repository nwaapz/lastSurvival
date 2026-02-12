using UnityEngine;

/// <summary>
/// Integrates HeroMovementBaseBuilder with BaseBuilderClickManager
/// to enable click-to-move functionality for the hero.
/// Attach this to your hero GameObject.
/// </summary>
[RequireComponent(typeof(HeroMovementBaseBuilder))]
public class HeroClickToMoveBaseBuilder : MonoBehaviour
{
    [Header("Click Filtering")]
    [SerializeField] private LayerMask walkableLayerMask = -1;
    [SerializeField] private bool onlyMoveOnWalkableLayer = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private HeroMovementBaseBuilder _movement;
    private Camera _mainCamera;

    private void Awake()
    {
        _movement = GetComponent<HeroMovementBaseBuilder>();
        _mainCamera = Camera.main;
    }

    private void Start()
    {
        SubscribeToClickManager();
    }

    private void OnDestroy()
    {
        UnsubscribeFromClickManager();
    }

    private void SubscribeToClickManager()
    {
        if (BaseBuilderClickManager.Instance != null)
        {
            BaseBuilderClickManager.Instance.OnWorldClicked += HandleWorldClick;
            
            if (showDebugLogs)
            {
                Debug.Log("[HeroClickToMoveBaseBuilder] ✓ Successfully subscribed to BaseBuilderClickManager.OnWorldClicked event", this);
            }
        }
        else
        {
            Debug.LogWarning("[HeroClickToMoveBaseBuilder] ❌ BaseBuilderClickManager not found in scene!", this);
        }
    }

    private void UnsubscribeFromClickManager()
    {
        if (BaseBuilderClickManager.HasInstance)
        {
            BaseBuilderClickManager.Instance.OnWorldClicked -= HandleWorldClick;
        }
    }

    private void HandleWorldClick(Vector3 worldPosition)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[HeroClickToMoveBaseBuilder] ===== CLICK RECEIVED =====\n" +
                     $"World Position: {worldPosition}\n" +
                     $"Hero Position: {transform.position}\n" +
                     $"Distance: {Vector3.Distance(transform.position, worldPosition):F2}m", this);
        }

        // Check if click is on walkable layer
        if (onlyMoveOnWalkableLayer)
        {
            bool isWalkable = IsPositionOnWalkableLayer(worldPosition);
            
            if (showDebugLogs)
            {
                Debug.Log($"[HeroClickToMoveBaseBuilder] Walkable Layer Check: {(isWalkable ? "PASSED" : "FAILED")}", this);
            }
            
            if (!isWalkable)
            {
                if (showDebugLogs)
                {
                    Debug.LogWarning($"[HeroClickToMoveBaseBuilder] ❌ BLOCKED: Position not on walkable layer", this);
                }
                return;
            }
        }

        // Check if hero can reach the destination
        bool canReach = _movement.CanReachDestination(worldPosition);
        
        if (showDebugLogs)
        {
            Debug.Log($"[HeroClickToMoveBaseBuilder] Reachability Check: {(canReach ? "PASSED" : "FAILED")}", this);
        }
        
        if (!canReach)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[HeroClickToMoveBaseBuilder] ❌ BLOCKED: Destination not reachable via NavMesh", this);
            }
            return;
        }

        // All checks passed - move hero
        if (showDebugLogs)
        {
            Debug.Log($"[HeroClickToMoveBaseBuilder] ✓ All checks passed. Sending move command...", this);
        }
        
        _movement.SetDestination(worldPosition);
        
        if (showDebugLogs)
        {
            Debug.Log($"[HeroClickToMoveBaseBuilder] ✓ Move command sent to HeroMovementBaseBuilder", this);
        }
    }

    private bool IsPositionOnWalkableLayer(Vector3 position)
    {
        if (_mainCamera == null)
        {
            if (showDebugLogs) Debug.LogWarning("[HeroClickToMoveBaseBuilder] No camera found, skipping layer check", this);
            return true;
        }

        // Raycast to check layer
        Ray ray = _mainCamera.ScreenPointToRay(_mainCamera.WorldToScreenPoint(position));
        
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            int hitLayer = 1 << hit.collider.gameObject.layer;
            bool isOnWalkable = (walkableLayerMask.value & hitLayer) != 0;
            
            if (showDebugLogs)
            {
                Debug.Log($"[HeroClickToMoveBaseBuilder] Layer Check Details:\n" +
                         $"  Hit Object: {hit.collider.gameObject.name}\n" +
                         $"  Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}\n" +
                         $"  Is Walkable: {isOnWalkable}", this);
            }
            
            return isOnWalkable;
        }

        if (showDebugLogs) Debug.LogWarning("[HeroClickToMoveBaseBuilder] Raycast missed - no collider at position", this);
        return false;
    }

    /// <summary>
    /// Enable or disable click-to-move functionality
    /// </summary>
    public void SetClickToMoveEnabled(bool enabled)
    {
        this.enabled = enabled;
        
        if (!enabled && _movement != null)
        {
            _movement.Stop();
        }
    }

    /// <summary>
    /// Manually command the hero to move to a position (not from click)
    /// </summary>
    public void MoveToPosition(Vector3 position)
    {
        if (_movement.CanReachDestination(position))
        {
            _movement.SetDestination(position);
        }
    }
}
