using UnityEngine;

/// <summary>
/// Scenario step that waits for the player to click on a specific building.
/// Use this for tutorials or guided interactions.
/// </summary>
[CreateAssetMenu(fileName = "WaitForBuildingClickStep", menuName = "Scenario/Wait For Building Click Step")]
public class WaitForBuildingClickStep : ScenarioStep
{
    [Header("Target Building")]
    [Tooltip("ID of the building the player must click")]
    public string targetBuildingId;
    
    [Header("Highlight")]
    [Tooltip("If true, highlight the target building")]
    public bool highlightBuilding = true;
    
    [Tooltip("Color to highlight the building")]
    public Color highlightColor = Color.yellow;
    
    [Header("Camera")]
    [Tooltip("If true, move camera to show the building")]
    public bool moveCameraToBuilding = true;
    
    [Tooltip("Camera movement duration")]
    public float cameraMoveTime = 1f;
    
    [Header("UI Hint")]
    [Tooltip("Optional hint text to show player")]
    public string hintText = "Click on the building to continue";
    
    [Tooltip("Show arrow pointing to building")]
    public bool showArrow = true;
    
    [Header("Tutorial Hand")]
    [Tooltip("If true, show an animated tutorial hand pointing at the target building")]
    public bool showTutorialHand = false;

    private bool _clicked;
    private BuildingView _targetBuilding;
    
    /// <summary>
    /// The building ID currently allowed to upgrade (only when WaitForBuildingClickStep is active).
    /// Returns null if no step is active.
    /// </summary>
    public static string AllowedBuildingId { get; private set; }

    public override void OnEnter()
    {
        _clicked = false;
        _targetBuilding = null;
        
        // Find the target building (case-insensitive match)
        BuildingView[] allBuildings = Object.FindObjectsOfType<BuildingView>();
        foreach (var building in allBuildings)
        {
            if (building.BuildingDefinition != null && 
                building.BuildingDefinition.Id.Equals(targetBuildingId, System.StringComparison.OrdinalIgnoreCase))
            {
                _targetBuilding = building;
                break;
            }
        }
        
        if (_targetBuilding == null)
        {
            Debug.LogWarning($"[WaitForBuildingClickStep] Building '{targetBuildingId}' not found! Auto-completing.");
            _clicked = true;
            return;
        }
        
        // Move camera to the building
        if (moveCameraToBuilding && CameraHelper.Instance != null)
        {
            MoveCameraToBuilding();
        }
        
        // Subscribe to click events from BaseBuilderClickManager
        if (BaseBuilderClickManager.Instance != null)
        {
            BaseBuilderClickManager.Instance.OnObjectClicked += HandleObjectClicked;
        }
        
        // Also subscribe to building upgrade events (BuildingView handles clicks directly)
        if (BuildingProgressManager.Instance != null)
        {
            BuildingProgressManager.Instance.OnBuildingUpgraded += HandleBuildingUpgraded;
        }
        
        // Show hint
        if (!string.IsNullOrEmpty(hintText))
        {
            // TODO: Show hint UI
            Debug.Log($"[WaitForBuildingClickStep] Hint: {hintText}");
        }
        
        // Show tutorial hand
        if (showTutorialHand && _targetBuilding != null && TutorialHandManager.Instance != null)
        {
            TutorialHandManager.Instance.ShowAtTransform(_targetBuilding.transform);
        }
        
        // Set the allowed building ID for upgrade permission
        AllowedBuildingId = targetBuildingId;
        
        Debug.Log($"[WaitForBuildingClickStep] Waiting for click on: {targetBuildingId}");
    }

    private void HandleObjectClicked(GameObject clickedObject)
    {
        if (clickedObject == null) return;
        
        // Check if clicked object is our target building
        BuildingView building = clickedObject.GetComponent<BuildingView>();
        if (building == null)
        {
            building = clickedObject.GetComponentInParent<BuildingView>();
        }
        
        if (building != null && building == _targetBuilding)
        {
            _clicked = true;
            Debug.Log($"[WaitForBuildingClickStep] Player clicked on {targetBuildingId}!");
        }
    }
    
    private void HandleBuildingUpgraded(string buildingId, int newLevel)
    {
        // Check if the upgraded building is our target (case-insensitive)
        if (buildingId.Equals(targetBuildingId, System.StringComparison.OrdinalIgnoreCase))
        {
            _clicked = true;
            Debug.Log($"[WaitForBuildingClickStep] Player upgraded {targetBuildingId} to level {newLevel}!");
        }
    }

    public override bool UpdateStep()
    {
        return _clicked;
    }

    private void MoveCameraToBuilding()
    {
        if (_targetBuilding == null || CameraHelper.Instance == null) return;
        
        Vector3 buildingPos = _targetBuilding.transform.position;
        CameraHelper.Instance.MoveToPosition(
            new Vector3(buildingPos.x, buildingPos.y, CameraHelper.Instance.fixedZ),
            Camera.main.transform.rotation,
            Camera.main.orthographicSize,
            cameraMoveTime
        );
        Debug.Log($"[WaitForBuildingClickStep] Moving camera to building: {targetBuildingId}");
    }

    public override void OnExit()
    {
        // Unsubscribe from click events
        if (BaseBuilderClickManager.Instance != null)
        {
            BaseBuilderClickManager.Instance.OnObjectClicked -= HandleObjectClicked;
        }
        
        // Unsubscribe from building upgrade events
        if (BuildingProgressManager.Instance != null)
        {
            BuildingProgressManager.Instance.OnBuildingUpgraded -= HandleBuildingUpgraded;
        }
        
        // Hide hint/arrow
        // TODO: Hide hint UI
        
        // Hide tutorial hand
        if (showTutorialHand && TutorialHandManager.Instance != null)
        {
            TutorialHandManager.Instance.Hide();
        }
        
        // Clear the allowed building ID
        AllowedBuildingId = null;
    }
    
    /// <summary>
    /// Check if a building is currently allowed to upgrade.
    /// </summary>
    public static bool IsBuildingAllowedToUpgrade(string buildingId)
    {
        if (string.IsNullOrEmpty(AllowedBuildingId)) return false;
        return AllowedBuildingId.Equals(buildingId, System.StringComparison.OrdinalIgnoreCase);
    }
}
