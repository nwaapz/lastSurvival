using UnityEngine;

/// <summary>
/// Movement mode for character movement steps
/// </summary>
public enum MovementMode
{
    Walk,
    Run
}

/// <summary>
/// Scenario step that commands a specific character to move to a location.
/// Uses CharacterName enum to identify which character to move.
/// Waits for the character to reach the destination before completing.
/// </summary>
[CreateAssetMenu(fileName = "MoveCharacterStep", menuName = "Scenario/Move Character to Location Step")]
public class MoveUnitToLocationStep : ScenarioStep
{
    [Header("Character to Move")]
    [Tooltip("Which character to move (Hero, Janet, Pedi, Commander)")]
    public CharacterName characterToMove = CharacterName.Hero;
    
    [Header("Movement Mode")]
    [Tooltip("Walk = normal speed, Run = faster speed with run animation")]
    public MovementMode movementMode = MovementMode.Walk;
    
    [Tooltip("Speed multiplier when running (e.g., 2.0 = double speed)")]
    public float runSpeedMultiplier = 2f;
    
    [Header("Destination")]
    [Tooltip("Location where the character should move to")]
    public LocationName targetLocation = LocationName.None;
    
    [Tooltip("Legacy: String location ID (use targetLocation enum instead)")]
    public string targetLocationId;
    
    [Tooltip("How close the character needs to get to the location (in units)")]
    public float arrivalRadius = 1f;
    
    [Header("Camera Follow")]
    [Tooltip("If true, camera will follow the character during movement")]
    public bool cameraFollowsUnit = false;
    
    [Tooltip("Smoothing for camera follow (lower = snappier)")]
    public float cameraFollowSmoothing = 0.3f;
    
    [Header("Look At Target (After Arrival)")]
    [Tooltip("If true, character will face towards a target after reaching destination")]
    public bool lookAtTarget = false;
    
    [Tooltip("Look at another character")]
    public CharacterName lookAtCharacter = CharacterName.None;
    
    [Tooltip("Look at a named location (used if lookAtCharacter is None)")]
    public LocationName lookAtLocation = LocationName.None;

    private CharacterController _character;
    private Vector3 _targetPosition;
    private bool _hasArrived = false;
    private bool _movementStarted = false;
    private Transform _previousCameraTarget;
    private float _originalSpeed;

    public override void OnEnter()
    {
        _hasArrived = false;
        _movementStarted = false;
        
        // Find the character using CharacterManager from ServiceLocator
        CharacterManager characterManager = ServiceLocator.Instance.Get<CharacterManager>();
        if (characterManager == null)
        {
            Debug.LogWarning("[MoveUnitToLocationStep] CharacterManager not found! Step will auto-complete.");
            _hasArrived = true;
            return;
        }
        
        _character = characterManager.GetCharacter(characterToMove);
        if (_character == null)
        {
            Debug.LogWarning($"[MoveUnitToLocationStep] Character '{characterToMove}' not found! Step will auto-complete.");
            _hasArrived = true;
            return;
        }
        
        // Find the target location using LocationManager
        _targetPosition = GetTargetPosition();
        if (_targetPosition == Vector3.zero && targetLocation == LocationName.None && string.IsNullOrEmpty(targetLocationId))
        {
            Debug.LogWarning($"[MoveUnitToLocationStep] No location specified! Step will auto-complete.");
            _hasArrived = true;
            return;
        }
        
        // Store original speed and apply run speed if needed
        if (_character.Movement != null)
        {
            _originalSpeed = _character.Movement.Speed;
            
            if (movementMode == MovementMode.Run)
            {
                float runSpeed = _originalSpeed * runSpeedMultiplier;
                _character.Movement.SetSpeed(runSpeed);
                Debug.Log($"[MoveUnitToLocationStep] Set run speed: {runSpeed} (original: {_originalSpeed})");
            }
        }
        
        // Command the character to move with the specified mode
        _character.MoveToPosition(_targetPosition, movementMode == MovementMode.Run);
        Debug.Log($"[MoveUnitToLocationStep] Commanding {characterToMove} to {movementMode} to {GetLocationDisplayName()} at {_targetPosition}");
        
        // Start camera follow if enabled
        if (cameraFollowsUnit && CameraHelper.Instance != null)
        {
            _previousCameraTarget = CameraHelper.Instance.FollowTarget;
            CameraHelper.Instance.SetFollowTarget(_character.transform, cameraFollowSmoothing);
            Debug.Log($"[MoveUnitToLocationStep] Camera now following {characterToMove}");
        }
    }

    public override bool UpdateStep()
    {
        if (_hasArrived) return true;
        
        if (_character == null)
        {
            Debug.LogWarning($"[MoveUnitToLocationStep] Character is null, completing step");
            _hasArrived = true;
            return true;
        }
        
        // Check if character has arrived using multiple methods:
        // 1. Distance check (primary)
        float distance = Vector3.Distance(_character.transform.position, _targetPosition);
        bool withinRadius = distance <= arrivalRadius;
        
        // 2. Movement system stopped (backup - handles NavMesh position adjustment)
        // BUT only check this AFTER movement has started to avoid false positives on first frame
        bool isCurrentlyMoving = _character.Movement != null && _character.Movement.IsMoving;
        
        // Track when movement actually starts
        if (isCurrentlyMoving && !_movementStarted)
        {
            _movementStarted = true;
            Debug.Log($"[MoveUnitToLocationStep] Movement started for {characterToMove}");
        }
        
        // Only consider "movement stopped" if movement had started first
        bool movementStopped = _movementStarted && !isCurrentlyMoving;
        
        // 3. Check if character is very close to their actual destination (NavMesh adjusted)
        bool nearActualDestination = false;
        if (_character.Movement != null && _character.Movement.Agent != null)
        {
            float distToAgentDest = Vector3.Distance(_character.transform.position, _character.Movement.CurrentDestination);
            nearActualDestination = distToAgentDest <= _character.Movement.StoppingDistance + 0.1f;
        }
        
        if (withinRadius || movementStopped)
        {
            _hasArrived = true;
            Debug.Log($"[MoveUnitToLocationStep] {characterToMove} arrived at {GetLocationDisplayName()}! (distance: {distance:F2}, withinRadius: {withinRadius}, movementStopped: {movementStopped}, nearActualDest: {nearActualDestination})");
            
            // Apply look-at rotation if enabled
            if (lookAtTarget)
            {
                ApplyLookAtRotation();
            }
            
            return true;
        }
        
        return false;
    }

    public override void OnExit()
    {
        // Restore original speed if we changed it
        if (_character != null && _character.Movement != null && movementMode == MovementMode.Run)
        {
            _character.Movement.SetSpeed(_originalSpeed);
            _character.SetRunning(false);
            Debug.Log($"[MoveUnitToLocationStep] Restored original speed: {_originalSpeed}");
        }
        
        // Stop camera follow when step ends
        if (cameraFollowsUnit && CameraHelper.Instance != null)
        {
            CameraHelper.Instance.StopFollowing();
            Debug.Log($"[MoveUnitToLocationStep] Camera stopped following {characterToMove}");
        }
    }
    
    private Vector3 GetTargetPosition()
    {
        // Try LocationManager first (preferred)
        LocationManager locationManager = ServiceLocator.Instance?.Get<LocationManager>();
        if (locationManager != null)
        {
            // Try enum first
            if (targetLocation != LocationName.None)
            {
                return locationManager.GetPosition(targetLocation);
            }
            // Fallback to string
            if (!string.IsNullOrEmpty(targetLocationId))
            {
                return locationManager.GetPosition(targetLocationId);
            }
        }
        
        // Fallback: Find NamedLocation directly
        NamedLocation[] allLocations = Object.FindObjectsOfType<NamedLocation>();
        foreach (var loc in allLocations)
        {
            if (targetLocation != LocationName.None && loc.location == targetLocation)
            {
                return loc.transform.position;
            }
            if (!string.IsNullOrEmpty(targetLocationId) && loc.locationId == targetLocationId)
            {
                return loc.transform.position;
            }
        }
        
        return Vector3.zero;
    }
    
    private string GetLocationDisplayName()
    {
        if (targetLocation != LocationName.None)
            return targetLocation.ToString();
        return targetLocationId;
    }
    
    private void ApplyLookAtRotation()
    {
        if (_character == null) return;
        
        Vector3? lookTarget = null;
        
        // First try to look at a character
        if (lookAtCharacter != CharacterName.None)
        {
            CharacterManager characterManager = ServiceLocator.Instance?.Get<CharacterManager>();
            if (characterManager != null)
            {
                CharacterController targetCharacter = characterManager.GetCharacter(lookAtCharacter);
                if (targetCharacter != null)
                {
                    lookTarget = targetCharacter.transform.position;
                    Debug.Log($"[MoveUnitToLocationStep] {characterToMove} looking at character {lookAtCharacter}");
                }
                else
                {
                    Debug.LogWarning($"[MoveUnitToLocationStep] Look-at character {lookAtCharacter} not found!");
                }
            }
        }
        
        // If no character target, try location
        if (!lookTarget.HasValue && lookAtLocation != LocationName.None)
        {
            lookTarget = GetLookAtPosition(lookAtLocation);
            if (lookTarget.HasValue)
            {
                Debug.Log($"[MoveUnitToLocationStep] {characterToMove} looking at location {lookAtLocation}");
            }
        }
        
        // Apply rotation if we have a target
        if (lookTarget.HasValue)
        {
            Vector3 direction = lookTarget.Value - _character.transform.position;
            direction.y = 0; // Keep rotation on horizontal plane
            if (direction.sqrMagnitude > 0.001f)
            {
                _character.transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
    
    private Vector3? GetLookAtPosition(LocationName location)
    {
        LocationManager locationManager = ServiceLocator.Instance?.Get<LocationManager>();
        if (locationManager != null)
        {
            Vector3 pos = locationManager.GetPosition(location);
            if (pos != Vector3.zero)
                return pos;
        }
        
        // Fallback: Find NamedLocation directly
        NamedLocation[] allLocations = Object.FindObjectsOfType<NamedLocation>();
        foreach (var loc in allLocations)
        {
            if (loc.location == location)
            {
                return loc.transform.position;
            }
        }
        
        return null;
    }
}
