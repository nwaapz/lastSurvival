using UnityEngine;

/// <summary>
/// Scenario step that spawns a character at a named location.
/// Use this to introduce characters into the scene during scenarios.
/// </summary>
[CreateAssetMenu(fileName = "SpawnCharacterStep", menuName = "Scenario/Spawn Character Step")]
public class SpawnCharacterStep : ScenarioStep
{
    [Header("Character to Spawn")]
    [Tooltip("Which character to spawn (Hero, Janet, Pedi, Commander)")]
    public CharacterName characterToSpawn = CharacterName.Hero;
    
    [Header("Spawn Location")]
    [Tooltip("Location where the character should spawn")]
    public LocationName spawnLocation = LocationName.None;
    
    [Tooltip("Legacy: String location ID (use spawnLocation enum instead)")]
    public string spawnLocationId;
    
    [Tooltip("Offset from the location position")]
    public Vector3 spawnOffset = Vector3.zero;
    
    [Header("Camera")]
    [Tooltip("If true, camera will move to focus on the spawn position")] 
    public bool moveCameraToSpawn = true;
    
    [Tooltip("Duration of camera movement to spawn position")]
    public float cameraMoveDuration = 0.5f;

    [Header("Initial State")]
    [Tooltip("Should player be able to control this character after spawn?")]
    public bool enablePlayerControl = false;
    
    [Tooltip("Face this direction on spawn (Y rotation in degrees). Ignored if lookAtTarget is enabled.")]
    public float initialRotationY = 0f;
    
    [Header("Look At Target (After Spawn)")]
    [Tooltip("If true, character will face towards a target after spawning")]
    public bool lookAtTarget = false;
    
    [Tooltip("Look at another character")]
    public CharacterName lookAtCharacter = CharacterName.None;
    
    [Tooltip("Look at a named location (used if lookAtCharacter is None)")]
    public LocationName lookAtLocation = LocationName.None;
    
    [Header("Completion")]
    [Tooltip("If false, waits for camera movement to finish before completing")]
    public bool completeImmediately = true;

    private bool _spawned = false;
    private float _cameraWaitTime = 0f;
    private bool _waitingForCamera = false;
    private bool _initialized = false;
    private int _retryCount = 0;
    private const int MAX_RETRIES = 100; // About 1-2 seconds of retrying

    public override void OnEnter()
    {
        _spawned = false;
        _initialized = false;
        _retryCount = 0;
        _waitingForCamera = false;
        _cameraWaitTime = 0f;
        
        Debug.Log($"[SpawnCharacterStep] OnEnter: Starting spawn of {characterToSpawn} at {spawnLocation}");
        
        TrySpawn();
    }
    
    private void TrySpawn()
    {
        
        // Find spawn location using LocationManager
        Vector3 spawnPosition = GetSpawnPosition();
        if (spawnPosition == Vector3.zero && spawnLocation == LocationName.None && string.IsNullOrEmpty(spawnLocationId))
        {
            Debug.LogWarning($"[SpawnCharacterStep] No location specified! Step will auto-complete.");
            _spawned = true;
            return;
        }
        
        Debug.Log($"[SpawnCharacterStep] Spawn position resolved to: {spawnPosition}");
        
        spawnPosition += spawnOffset;
        Quaternion spawnRotation = Quaternion.Euler(0, initialRotationY, 0);
        
        CharacterController spawnedCharacter = null;
        CharacterManager characterManager = ServiceLocator.Instance?.Get<CharacterManager>();
        
        if (characterManager == null)
        {
            // Services not ready yet - will retry in UpdateStep
            return;
        }
        
        _initialized = true;
        
        // 1. Try to find existing active character
        spawnedCharacter = characterManager.GetCharacter(characterToSpawn);
        if (spawnedCharacter != null)
        {
            Debug.Log($"[SpawnCharacterStep] Found existing active character: {characterToSpawn}");
        }
        
        // 2. If not found in active scene, check inactive objects in scene
        if (spawnedCharacter == null)
        {
            CharacterController[] allCharacters = Resources.FindObjectsOfTypeAll<CharacterController>();
            foreach (var character in allCharacters)
            {
                if (character.characterName == characterToSpawn && character.gameObject.scene.IsValid())
                {
                    spawnedCharacter = character;
                    Debug.Log($"[SpawnCharacterStep] Found inactive character in scene: {characterToSpawn}");
                    break;
                }
            }
        }
        
        // 3. If still not found, Spawn new one using Registry
        if (spawnedCharacter == null)
        {
            Debug.Log($"[SpawnCharacterStep] No existing character found, spawning new from prefab registry...");
            spawnedCharacter = characterManager.SpawnCharacter(characterToSpawn, spawnPosition, spawnRotation);
            
            if (spawnedCharacter == null)
            {
                Debug.LogError($"[SpawnCharacterStep] Failed to spawn {characterToSpawn} from prefab registry!");
                _spawned = true;
                return;
            }
        }

        // 4. Finalize setup
        if (spawnedCharacter != null)
        {
            // Activate (if it was inactive)
            spawnedCharacter.gameObject.SetActive(true);
            
            // Position
            // If it's a NavMesh agent, we might need to Warp?
            // For now, direct transform set is usually okay if agent is disabled or just spawning.
            spawnedCharacter.transform.position = spawnPosition;
            spawnedCharacter.transform.rotation = spawnRotation;
            
            // Apply look-at rotation if enabled
            if (lookAtTarget)
            {
                ApplyLookAtRotation(spawnedCharacter, characterManager);
            }
            
             Debug.Log($"[SpawnCharacterStep] Spawned/Activated {characterToSpawn} at {spawnLocation} ({spawnLocationId})");

            // Optionally move camera to focus on spawn position
            if (moveCameraToSpawn && CameraHelper.Instance != null && Camera.main != null)
            {
                var camHelper = CameraHelper.Instance;
                
                // Calculate camera target - clamp to camera bounds
                float camX = camHelper.unlimitedMovement ? spawnPosition.x : Mathf.Clamp(spawnPosition.x, camHelper.minX, camHelper.maxX);
                float camY = camHelper.unlimitedMovement ? spawnPosition.y : Mathf.Clamp(spawnPosition.y, camHelper.minY, camHelper.maxY);
                float camZ = camHelper.fixedZ;
                
                Vector3 camTarget = new Vector3(camX, camY, camZ);
                Quaternion camRot = Camera.main.transform.rotation;
                float camZoom = Camera.main.orthographicSize;
                
                camHelper.MoveToPosition(camTarget, camRot, camZoom, cameraMoveDuration);
                Debug.Log($"[SpawnCharacterStep] Camera moving to spawn position {camTarget} (spawn was at {spawnPosition})");
                
                // If not completing immediately, wait for camera
                if (!completeImmediately)
                {
                    _waitingForCamera = true;
                    _cameraWaitTime = cameraMoveDuration;
                }
            }
        }
        else
        {
            Debug.LogWarning($"[SpawnCharacterStep] Failed to spawn/find {characterToSpawn}! Verify CharacterManager has PrefabRegistry or character exists in scene.");
            _spawned = true;
            return;
        }
        
        // Configure player control
        spawnedCharacter.SetPlayerControlEnabled(enablePlayerControl);
        
        // Refresh CharacterManager to include newly spawned character
        if (characterManager != null)
        {
            characterManager.RefreshCharacters();
        }
        
        _spawned = true;
    }

    public override bool UpdateStep()
    {
        // If not initialized yet, retry spawning (services might not be ready)
        if (!_initialized)
        {
            _retryCount++;
            Debug.Log($"[SpawnCharacterStep] UpdateStep: Not initialized yet, retry {_retryCount}/{MAX_RETRIES}");
            if (_retryCount > MAX_RETRIES)
            {
                Debug.LogError($"[SpawnCharacterStep] Failed to spawn {characterToSpawn} after {MAX_RETRIES} retries. Services not available.");
                return true; // Give up and complete to avoid infinite hang
            }
            TrySpawn();
            return false;
        }
        
        if (!_spawned)
        {
            Debug.Log($"[SpawnCharacterStep] UpdateStep: Waiting for spawn of {characterToSpawn}...");
            return false;
        }
        
        // If completing immediately, done right away
        if (completeImmediately) return true;
        
        // If waiting for camera, count down
        if (_waitingForCamera)
        {
            _cameraWaitTime -= Time.deltaTime;
            if (_cameraWaitTime <= 0f)
            {
                _waitingForCamera = false;
                Debug.Log($"[SpawnCharacterStep] Camera wait complete for {characterToSpawn}");
                return true;
            }
            return false;
        }
        
        // completeImmediately is false but no camera movement - still complete after spawn
        // (spawn already happened, so we're done)
        Debug.Log($"[SpawnCharacterStep] Spawn complete for {characterToSpawn} (no camera wait)");
        return true;
    }

    public override void OnExit()
    {
        // Cleanup if needed
    }
    
    private void ApplyLookAtRotation(CharacterController character, CharacterManager characterManager)
    {
        Vector3? lookTarget = null;
        
        // First try to look at a character
        if (lookAtCharacter != CharacterName.None && characterManager != null)
        {
            CharacterController targetCharacter = characterManager.GetCharacter(lookAtCharacter);
            if (targetCharacter != null)
            {
                lookTarget = targetCharacter.transform.position;
                Debug.Log($"[SpawnCharacterStep] {characterToSpawn} looking at character {lookAtCharacter}");
            }
            else
            {
                Debug.LogWarning($"[SpawnCharacterStep] Look-at character {lookAtCharacter} not found!");
            }
        }
        
        // If no character target, try location
        if (!lookTarget.HasValue && lookAtLocation != LocationName.None)
        {
            lookTarget = GetLookAtPosition(lookAtLocation);
            if (lookTarget.HasValue)
            {
                Debug.Log($"[SpawnCharacterStep] {characterToSpawn} looking at location {lookAtLocation}");
            }
        }
        
        // Apply rotation if we have a target
        if (lookTarget.HasValue)
        {
            Vector3 direction = lookTarget.Value - character.transform.position;
            direction.y = 0; // Keep rotation on horizontal plane
            if (direction.sqrMagnitude > 0.001f)
            {
                character.transform.rotation = Quaternion.LookRotation(direction);
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
    
    private Vector3 GetSpawnPosition()
    {
        // Try LocationManager first (preferred)
        LocationManager locationManager = ServiceLocator.Instance?.Get<LocationManager>();
        if (locationManager != null)
        {
            // Try enum first
            if (spawnLocation != LocationName.None)
            {
                return locationManager.GetPosition(spawnLocation);
            }
            // Fallback to string
            if (!string.IsNullOrEmpty(spawnLocationId))
            {
                return locationManager.GetPosition(spawnLocationId);
            }
        }
        
        // Fallback: Find NamedLocation directly
        NamedLocation[] allLocations = Object.FindObjectsOfType<NamedLocation>();
        foreach (var loc in allLocations)
        {
            if (spawnLocation != LocationName.None && loc.location == spawnLocation)
            {
                return loc.transform.position;
            }
            if (!string.IsNullOrEmpty(spawnLocationId) && loc.locationId == spawnLocationId)
            {
                return loc.transform.position;
            }
        }
        
        return Vector3.zero;
    }
}
