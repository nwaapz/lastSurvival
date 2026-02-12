using UnityEngine;

[CreateAssetMenu(fileName = "BuildStep", menuName = "Scenario/Build Step")]
public class BuildObjectiveStep : ScenarioStep
{
    [Header("Objective")]
    public string targetBuildingId;
    public int targetLevel = 1;

    [Header("Restrictions")]
    [Tooltip("If true, the player can ONLY click on the target building.")]
    public bool restrictInteractions = true;
    
    [Header("Camera Movement")]
    [Tooltip("If true, camera will move to show the target building when step starts")]
    public bool moveCameraToBuilding = true;
    
    [Tooltip("Duration of camera movement (if enabled)")]
    public float cameraMoveTime = 1.5f;
    
    [Header("Tutorial Hand")]
    [Tooltip("If true, show an animated tutorial hand pointing at the target building")]
    public bool showTutorialHand = false;

    private bool _isComplete;
    private BuildingView _targetBuildingView;

    public override void OnEnter()
    {
        _isComplete = false;

        if (BuildingProgressManager.Instance != null)
        {
            // Check if already complete
            if (BuildingProgressManager.Instance.GetLevel(targetBuildingId) >= targetLevel)
            {
                Debug.Log($"[BuildObjectiveStep] Building {targetBuildingId} is already at level {targetLevel}. Completing step.");
                _isComplete = true;
                return;
            }

            // Listen for upgrades
            BuildingProgressManager.Instance.OnBuildingUpgraded += HandleBuildingUpgraded;
        }
        else
        {
            Debug.LogWarning("[BuildObjectiveStep] BuildingProgressManager missing. Step will autocomplete.");
            _isComplete = true;
            return;
        }

        // Apply restriction
        if (restrictInteractions && BaseBuilderClickManager.Instance != null)
        {
            BaseBuilderClickManager.Instance.InteractionFilter = IsInteractionAllowed;
        }
        
        // Move camera to building location if enabled
        if (moveCameraToBuilding)
        {
            MoveCameraToTargetBuilding();
        }
        
        // Show tutorial hand
        if (showTutorialHand)
        {
            ShowTutorialHandAtBuilding();
        }
    }

    private void HandleBuildingUpgraded(string buildingId, int newLevel)
    {
        if (buildingId == targetBuildingId && newLevel >= targetLevel)
        {
            _isComplete = true;
        }
    }

    private bool IsInteractionAllowed(GameObject obj)
    {
        // Allow UI? The filter is usually for World objects.
        // If BaseBuilderClickManager handles UI separately, we are fine.
        // But BaseBuilderClickManager checks IsPointerOverUI() first, so UI is safe.

        var view = obj.GetComponent<BuildingView>();
        if (view != null && view.BuildingDefinition != null && view.BuildingDefinition.Id == targetBuildingId)
        {
            return true;
        }

        // Also allow if it's part of the building hierarchy? 
        // Usually collider is on the same object as BuildingView.

        return false;
    }

    public override bool UpdateStep()
    {
        return _isComplete;
    }

    public override void OnExit()
    {
        if (BuildingProgressManager.Instance != null)
        {
            BuildingProgressManager.Instance.OnBuildingUpgraded -= HandleBuildingUpgraded;
        }

        // Clear restriction if we set it
        if (restrictInteractions && BaseBuilderClickManager.Instance != null)
        {
            // Only clear if it's OUR filter.
            // But since ScenarioManager is linear, we assume we own the filter.
            BaseBuilderClickManager.Instance.InteractionFilter = null;
        }
        
        // Hide tutorial hand
        if (showTutorialHand && TutorialHandManager.Instance != null)
        {
            TutorialHandManager.Instance.Hide();
        }
    }
    
    private void MoveCameraToTargetBuilding()
    {
        if (CameraHelper.Instance == null)
        {
            Debug.LogWarning("[BuildObjectiveStep] CameraHelper not found. Cannot move camera.");
            return;
        }
        
        // Try to find the building's NamedLocation first
        NamedLocation location = FindNamedLocationForBuilding(targetBuildingId);
        if (location != null)
        {
            CameraHelper.Instance.MoveToLocation(location, cameraMoveTime);
            Debug.Log($"[BuildObjectiveStep] Moving camera to {location.DisplayName}");
            return;
        }
        
        // Fallback: Find the BuildingView in the scene
        BuildingView[] allBuildings = UnityEngine.Object.FindObjectsOfType<BuildingView>();
        foreach (var building in allBuildings)
        {
            if (building.BuildingDefinition != null && building.BuildingDefinition.Id == targetBuildingId)
            {
                // Move camera to building position with default offset
                Vector3 targetPos = building.transform.position + new Vector3(0, 10, -10);
                Quaternion targetRot = Quaternion.Euler(45, 0, 0);
                CameraHelper.Instance.MoveToPosition(targetPos, targetRot, Camera.main.orthographicSize, cameraMoveTime);
                Debug.Log($"[BuildObjectiveStep] Moving camera to building: {targetBuildingId}");
                return;
            }
        }
        
        Debug.LogWarning($"[BuildObjectiveStep] Could not find building or location for: {targetBuildingId}");
    }
    
    private void ShowTutorialHandAtBuilding()
    {
        if (TutorialHandManager.Instance == null)
        {
            Debug.LogWarning("[BuildObjectiveStep] TutorialHandManager not found.");
            return;
        }
        
        // Find the BuildingView in the scene
        if (_targetBuildingView == null)
        {
            BuildingView[] allBuildings = UnityEngine.Object.FindObjectsOfType<BuildingView>();
            foreach (var building in allBuildings)
            {
                if (building.BuildingDefinition != null && building.BuildingDefinition.Id == targetBuildingId)
                {
                    _targetBuildingView = building;
                    break;
                }
            }
        }
        
        if (_targetBuildingView != null)
        {
            TutorialHandManager.Instance.ShowAtTransform(_targetBuildingView.transform);
            Debug.Log($"[BuildObjectiveStep] Showing tutorial hand at building: {targetBuildingId}");
        }
        else
        {
            Debug.LogWarning($"[BuildObjectiveStep] Could not find building for tutorial hand: {targetBuildingId}");
        }
    }
    
    private NamedLocation FindNamedLocationForBuilding(string buildingId)
    {
        NamedLocation[] allLocations = UnityEngine.Object.FindObjectsOfType<NamedLocation>();
        foreach (var loc in allLocations)
        {
            // Check if location ID matches building ID (e.g., "Barracks" location for "Barracks" building)
            if (loc.locationId.Equals(buildingId, System.StringComparison.OrdinalIgnoreCase))
            {
                return loc;
            }
        }
        return null;
    }
}
