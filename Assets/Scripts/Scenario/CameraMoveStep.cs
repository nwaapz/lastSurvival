using UnityEngine;

/// <summary>
/// Scenario step that moves the camera to a named location.
/// Use this to guide the player's view to important areas (Port, Factory, Colosseum, etc.)
/// </summary>
[CreateAssetMenu(fileName = "CameraMoveStep", menuName = "Scenario/Camera Move Step")]
public class CameraMoveStep : ScenarioStep
{
    [Header("Target Location")]
    [Tooltip("Location to move camera to")]
    public LocationName targetLocation = LocationName.None;
    
    [Tooltip("Legacy: String location ID (use targetLocation enum instead)")]
    public string targetLocationId;
    
    [Header("Movement Settings")]
    [Tooltip("Duration of camera movement in seconds")]
    public float movementDuration = 1.5f;
    
    [Tooltip("If true, step completes immediately when movement starts. If false, waits for movement to finish.")]
    public bool completeImmediately = false;
    
    [Header("Optional Narration")]
    [Tooltip("Show dialogue while camera is moving (optional)")]
    public NarrationLine narrationDuringMove;
    
    private bool _movementComplete = false;

    public override void OnEnter()
    {
        _movementComplete = false;
        
        // Show narration if provided
        if (narrationDuringMove != null && Narration_manager.Instance != null)
        {
            Narration_manager.Instance.ShowNarrationLine(narrationDuringMove);
        }
        
        // Move camera
        if (CameraHelper.Instance != null)
        {
            string locationId = GetLocationId();
            CameraHelper.Instance.MoveToLocation(
                locationId, 
                movementDuration, 
                OnCameraMovementComplete
            );
        }
        else
        {
            Debug.LogWarning("[CameraMoveStep] CameraHelper not found! Step will auto-complete.");
            _movementComplete = true;
        }
    }

    private void OnCameraMovementComplete()
    {
        _movementComplete = true;
        Debug.Log($"[CameraMoveStep] Camera reached location: {GetLocationId()}");
    }
    
    private string GetLocationId()
    {
        if (targetLocation != LocationName.None)
            return targetLocation.ToString();
        return targetLocationId;
    }

    public override bool UpdateStep()
    {
        // If set to complete immediately, finish as soon as movement starts
        if (completeImmediately)
        {
            return true;
        }
        
        // Otherwise wait for movement to finish
        return _movementComplete;
    }

    public override void OnExit()
    {
        // Cleanup if needed
    }
}
