using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines a member of the player squad with their prefab and position offset.
/// </summary>
[System.Serializable]
public class SquadMember
{
    [Tooltip("Character prefab to spawn")]
    public GameObject prefab;
    
    [Tooltip("Position offset from squad center (X = left/right, Z = forward/back)")]
    public Vector3 positionOffset;
    
    [Tooltip("Whether this squad member is active/enabled")]
    public bool isActive = true;
    
    [HideInInspector]
    public RunnerPlayerController spawnedController;
}

/// <summary>
/// Manages multiple player characters in a squad formation.
/// All squad members move together and share input.
/// Game over only occurs when ALL squad members die.
/// </summary>
public class RunnerSquadManager : MonoBehaviour
{
    [Header("Squad Configuration")]
    [Tooltip("List of squad members to spawn")]
    [SerializeField] private List<SquadMember> squadMembers = new List<SquadMember>();
    
    [Tooltip("Transform that acts as the squad's center point. If null, uses this transform.")]
    [SerializeField] private Transform squadCenter;
    
    [Header("Default Spacing")]
    [Tooltip("Default horizontal spacing between squad members")]
    [SerializeField] private float defaultXSpacing = 1.5f;
    
    [Tooltip("Default forward/back spacing between squad members")]
    [SerializeField] private float defaultZSpacing = 1.0f;
    
    [Header("Formation Presets")]
    [Tooltip("Auto-arrange members in a formation pattern")]
    [SerializeField] private FormationType formationType = FormationType.Custom;
    
    public enum FormationType
    {
        Custom,      // Use manually configured offsets
        Line,        // Horizontal line (left to right)
        Column,      // Vertical column (front to back)  
        VFormation,  // V-shaped wedge
        Diamond      // Diamond pattern
    }
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    
    [Header("Position Control")]
    [Tooltip("If true, clamp squad position to lane bounds. Disable to freely position squad.")]
    [SerializeField] private bool clampToLaneBounds = false;
    
    [Tooltip("If false, disables automatic position updates. Squad stays exactly where spawned.")]
    [SerializeField] private bool enablePositionUpdates = true;
    
    [Header("Custom Movement Restrictions")]
    [Tooltip("Use custom X limits instead of lane bounds")]
    [SerializeField] private bool useCustomLimits = true;
    [SerializeField] private float minXLimit = -5f;
    [SerializeField] private float maxXLimit = 5f;

    [Header("Dynamic Member Spawning")]
    [Tooltip("Prefab to use when dynamically adding members (e.g., from gates). If null, uses first squad member's prefab.")]
    [SerializeField] private GameObject addMemberPrefab;
    
    [Header("Global Stats Overrides")]
    [Tooltip("If true, overrides the shooting range for ALL spawned squad members")]
    public bool overrideShootingRange = false;
    [Tooltip("The global shooting range applied to all members if override is enabled")]
    public float globalShootingRange = 30f;
    
    // Events
    public event Action OnSquadWiped;           // All members dead
    public event Action<int> OnMemberDied;      // Index of member that died
    public event Action<int> OnMemberAdded;     // Index of new member
    
    // Runtime state
    private List<RunnerPlayerController> _activeMembers = new List<RunnerPlayerController>();
    private int _aliveMemberCount;
    
    // Formation movement state
    private float _squadCenterX;          // Current X position of squad center
    private float _initialSpawnX;         // Initial X position of the squad (for relative limits)
    private float _targetCenterX;         // Target X position (for smooth movement)
    private float _centerVelocity;        // For SmoothDamp
    private float _formationMinOffset;    // Leftmost member offset
    private float _formationMaxOffset;    // Rightmost member offset
    private RunnerLaneConfig _laneConfig;
    private bool _wasMoving;              // Animation state tracking
    
    // Properties
    public int TotalMembers => squadMembers.Count;
    public int AliveMembers => _aliveMemberCount;
    public List<RunnerPlayerController> ActiveMembers => _activeMembers;
    public Transform SquadCenter => squadCenter != null ? squadCenter : transform;
    public float SquadCenterX => _squadCenterX;
    
    // Persistent stats
    private float _currentShootingRange;

    // Backup of default squad configuration (for retry)
    private List<SquadMember> _defaultSquadMembers;
    
    private void Awake()
    {
        // Always use this transform for positioning (ignore squadCenter field)
        squadCenter = transform;
        
        // Backup the default squad configuration from the inspector
        // This allows us to restore defaults on retry
        _defaultSquadMembers = new List<SquadMember>();
        foreach (var member in squadMembers)
        {
            if (member != null)
            {
                _defaultSquadMembers.Add(new SquadMember
                {
                    prefab = member.prefab,
                    positionOffset = member.positionOffset,
                    isActive = member.isActive
                });
            }
        }
        Debug.Log($"[RunnerSquadManager] Backed up {_defaultSquadMembers.Count} default squad members");
    }
    
    private void Start()
    {
        // Get lane config for bounds
        if (RunnerGameManager.HasInstance)
        {
            _laneConfig = RunnerGameManager.Instance.LaneConfig;
        }

        // Auto-assign camera target to this manager so camera follows the squad center
        var cameraController = FindObjectOfType<RunnerCameraController>();
        
        if (cameraController == null)
        {
            Debug.LogError("[RunnerSquadManager] No RunnerCameraController found in scene!");
        }
        
        if (cameraController != null)
        {
            cameraController.SetTarget(transform);
            
            // Set offset to ZERO.
            // For X: This centers the camera on the squad (Target X + 0).
            // For Y/Z (which are not followed): This keeps the camera at its initial position (Initial + 0).
            // This prevents the camera from jumping relative to the squad.
            cameraController.SetOffset(Vector3.zero);
            
            // Disable automatic rotation (LookAt) to preserve manual camera angle
            cameraController.SetLookAtTarget(false);
            
            Debug.Log("[RunnerSquadManager] Configured Camera to follow Squad Center");
        }
        
        // Wait for scenario configuration before spawning squad
        StartCoroutine(WaitForScenarioAndSpawn());
    }
    
    /// <summary>
    /// Wait for ScenarioManager to run ConfigureSquadStep before spawning the squad.
    /// This ensures the ConfigureSquadStep has time to set the configuration.
    /// </summary>
    private System.Collections.IEnumerator WaitForScenarioAndSpawn()
    {
        Debug.Log("[RunnerSquadManager] Waiting for scenario configuration...");
        
        // Wait for ScenarioManager to be available
        float timeout = 2f;
        float elapsed = 0f;
        
        while (!ScenarioManager.HasInstance && elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
        
        // Give ScenarioManager time to run the ConfigureSquadStep
        // Wait until either:
        // 1. SquadConfigHolder has configuration, or
        // 2. ScenarioManager has moved past step 0 (ConfigureSquadStep), or
        // 3. Timeout expires (for non-scenario gameplay)
        elapsed = 0f;
        timeout = 1f; // Short timeout - ConfigureSquadStep completes immediately
        
        while (elapsed < timeout)
        {
            // Check if configuration is ready
            if (SquadConfigHolder.HasInstance && SquadConfigHolder.Instance.HasConfiguration)
            {
                Debug.Log("[RunnerSquadManager] Configuration ready, proceeding to spawn");
                break;
            }
            
            // Check if ScenarioManager has moved past ConfigureSquadStep
            if (ScenarioManager.HasInstance && ScenarioManager.Instance.CurrentStepIndex > 0)
            {
                Debug.Log("[RunnerSquadManager] ScenarioManager moved past step 0, checking configuration");
                break;
            }
            
            yield return null;
            elapsed += Time.deltaTime;
        }
        
        if (elapsed >= timeout)
        {
            Debug.Log("[RunnerSquadManager] Timeout waiting for scenario, using defaults");
        }
        
        // Check if scenario has configured squad members
        ApplyScenarioConfiguration();
        
        SpawnSquad();
    }
    
    /// <summary>
    /// Apply configuration from scenario step (if any)
    /// </summary>
    private void ApplyScenarioConfiguration()
    {
        Debug.Log($"[RunnerSquadManager] Checking for scenario configuration...");
        
        if (!SquadConfigHolder.HasInstance || !SquadConfigHolder.Instance.HasConfiguration)
        {
            // No scenario config - restore from defaults if squad is empty
            if (squadMembers.Count == 0 && _defaultSquadMembers != null && _defaultSquadMembers.Count > 0)
            {
                Debug.Log($"[RunnerSquadManager] Restoring {_defaultSquadMembers.Count} default squad members");
                squadMembers.Clear();
                foreach (var defaultMember in _defaultSquadMembers)
                {
                    squadMembers.Add(new SquadMember
                    {
                        prefab = defaultMember.prefab,
                        positionOffset = defaultMember.positionOffset,
                        isActive = defaultMember.isActive
                    });
                }
            }
            else
            {
                Debug.Log($"[RunnerSquadManager] No scenario config, using existing {squadMembers.Count} squad members");
            }
            return;
        }
        
        var config = SquadConfigHolder.Instance;
        Debug.Log($"[RunnerSquadManager] Found config! Override={config.OverrideSquadMembers}, Members={config.SquadMembers?.Count ?? 0}, ActiveCount={config.ActiveMemberCount}");
        
        if (config.OverrideSquadMembers && config.SquadMembers != null && config.SquadMembers.Count > 0)
        {
            // Replace squad members with scenario-configured ones
            squadMembers.Clear();
            
            foreach (var memberConfig in config.SquadMembers)
            {
                if (memberConfig.prefab != null)
                {
                    SquadMember member = new SquadMember
                    {
                        prefab = memberConfig.prefab,
                        positionOffset = memberConfig.positionOffset,
                        isActive = memberConfig.isActive
                    };
                    squadMembers.Add(member);
                    Debug.Log($"[RunnerSquadManager] Added member: {memberConfig.prefab.name} at {memberConfig.positionOffset}");
                }
                else
                {
                    Debug.LogWarning("[RunnerSquadManager] Configured member has null prefab!");
                }
            }
            
            Debug.Log($"[RunnerSquadManager] Applied scenario squad config: {squadMembers.Count} members");
        }
        else
        {
            // Just set active member count
            SetActiveMemberCount(config.ActiveMemberCount);
            Debug.Log($"[RunnerSquadManager] Set active member count from scenario: {config.ActiveMemberCount}");
        }
        
        // NOTE: We NO LONGER clear the configuration here.
        // This allows the config to persist across retries (scene reload).
        // The config is only set fresh when ScenarioManager runs ConfigureSquadStep again.
    }
    
    private void Update()
    {
        // Update squad movement with smooth damping (only if enabled)
        if (enablePositionUpdates && _laneConfig != null && _activeMembers.Count > 0)
        {
            UpdateSquadPositions();
        }
    }
    
    /// <summary>
    /// Spawn all configured squad members
    /// </summary>
    public void SpawnSquad()
    {
        // Clear any existing members
        ClearSquad();
        
        // Initialize persistent stats from defaults
        // This ensures every new run starts fresh, but upgrades persist DURING the run (via ApplyModifierToSquad)
        _currentShootingRange = overrideShootingRange ? globalShootingRange : 30f;
        
        // Apply formation if not custom
        if (formationType != FormationType.Custom)
        {
            ApplyFormation();
        }
        
        // Spawn each member
        Debug.Log($"[RunnerSquadManager] SpawnSquad: Attempting to spawn {squadMembers.Count} members");
        for (int i = 0; i < squadMembers.Count; i++)
        {
            var member = squadMembers[i];
            if (member == null)
            {
                Debug.LogWarning($"[RunnerSquadManager] Member {i} is NULL - skipping");
                continue;
            }
            if (member.prefab == null)
            {
                Debug.LogWarning($"[RunnerSquadManager] Member {i} has NULL prefab - skipping");
                continue;
            }
            if (!member.isActive)
            {
                Debug.Log($"[RunnerSquadManager] Member {i} ({member.prefab.name}) is INACTIVE - skipping");
                continue;
            }
            
            Debug.Log($"[RunnerSquadManager] Spawning member {i}: {member.prefab.name}");
            SpawnMember(member, i);
        }
        
        _aliveMemberCount = _activeMembers.Count;
        
        // Calculate formation bounds for clamping
        CalculateFormationBounds();
        
        // Initialize squad center position from this transform
        // This allows positioning the squad by moving the RunnerSquadManager in the scene
        _squadCenterX = transform.position.x;
        _initialSpawnX = _squadCenterX; // Store initial position for relative limits
        _targetCenterX = _squadCenterX;
        
        Debug.Log($"[RunnerSquadManager] Squad spawned with {_aliveMemberCount} members. Center X: {_squadCenterX}");
    }
    
    private void SpawnMember(SquadMember member, int index)
    {
        // Always use this transform's position for spawning (not the squadCenter reference)
        Vector3 spawnPosition = transform.position + member.positionOffset;
        Quaternion spawnRotation = transform.rotation;
        
        Debug.Log($"[RunnerSquadManager] SPAWN: Manager pos={transform.position}, offset={member.positionOffset}, spawnPos={spawnPosition}");
        
        GameObject spawned = Instantiate(member.prefab, spawnPosition, spawnRotation);
        spawned.name = $"SquadMember_{index}_{member.prefab.name}";
        
        RunnerPlayerController controller = spawned.GetComponent<RunnerPlayerController>();
        if (controller == null)
        {
            Debug.LogError($"[RunnerSquadManager] Prefab {member.prefab.name} has no RunnerPlayerController!");
            Destroy(spawned);
            return;
        }
        
        // Set squad offset BEFORE Initialize() is called in Start()
        // This ensures the controller respects its formation position
        controller.SetSquadOffset(member.positionOffset);
        
        // Sync clamping setting
        // If we use custom limits, we disable controller clamping so Manager can handle it
        bool enableControllerClamping = !useCustomLimits && clampToLaneBounds;
        controller.SetClampToLaneBounds(enableControllerClamping);
        
        // Subscribe to health events
        controller.OnHealthChanged += (health) => OnMemberHealthChanged(controller, health);
        


        // Apply global overrides
        if (overrideShootingRange)
        {
            // Use the CURRENT modified range (base + upgrades)
            controller.SetDetectionRange(_currentShootingRange);
        }
        
        member.spawnedController = controller;
        _activeMembers.Add(controller);
        
        OnMemberAdded?.Invoke(index);
    }
    
    /// <summary>
    /// Apply movement to the squad as a unit (called by input handler)
    /// </summary>
    public void ApplySquadMovement(float deltaX)
    {
        if (_laneConfig == null) return;
        
        // Calculate new target position
        float newTarget = _targetCenterX + deltaX;
        
        _targetCenterX = GetClampedPosition(newTarget);
    }

    /// <summary>
    /// Helper to clamp position based on settings
    /// </summary>
    private float GetClampedPosition(float targetX)
    {
        if (useCustomLimits)
        {
            // Apply limits relative to the initial spawn position
            return Mathf.Clamp(targetX, _initialSpawnX + minXLimit, _initialSpawnX + maxXLimit);
        }
        else if (clampToLaneBounds && _laneConfig != null)
        {
            // Clamp based on formation bounds to keep all members in lane
            float effectiveMin = _laneConfig.MinXPosition - _formationMinOffset;
            float effectiveMax = _laneConfig.MaxXPosition - _formationMaxOffset;
            return Mathf.Clamp(targetX, effectiveMin, effectiveMax);
        }
        
        return targetX;
    }

    /// <summary>
    /// Update all squad member positions based on squad center
    /// </summary>
    private void UpdateSquadPositions()
    {
        // Calculate the target squad center
        if (clampToLaneBounds || useCustomLimits)
        {
            _squadCenterX = GetClampedPosition(_targetCenterX);
        }
        else
        {
            // No clamping - use target position directly
            _squadCenterX = _targetCenterX;
        }
        
        // Update the manager's transform to match squad center (so camera can follow it)
        Vector3 newPos = transform.position;
        newPos.x = _squadCenterX;
        transform.position = newPos;
        
        // Update each member's target position - let controller handle smooth movement
        for (int i = 0; i < squadMembers.Count && i < _activeMembers.Count; i++)
        {
            var member = squadMembers[i];
            var controller = _activeMembers[i];
            
            if (controller == null || controller.CurrentHealth <= 0) continue;
            
            // Calculate target position for this member
            float memberTargetX = _squadCenterX + member.positionOffset.x;
            
            // Let controller handle smooth movement and animation
            // skipClamp=true because squad manager handles formation-aware bounds
            controller.SetTargetXPosition(memberTargetX, skipClamp: true);
        }
    }
    
    /// <summary>
    /// Calculate the formation bounds (min/max member offsets)
    /// </summary>
    private void CalculateFormationBounds()
    {
        _formationMinOffset = 0f;
        _formationMaxOffset = 0f;
        
        foreach (var member in squadMembers)
        {
            if (member != null && member.isActive)
            {
                if (member.positionOffset.x < _formationMinOffset)
                    _formationMinOffset = member.positionOffset.x;
                if (member.positionOffset.x > _formationMaxOffset)
                    _formationMaxOffset = member.positionOffset.x;
            }
        }
        
        Debug.Log($"[RunnerSquadManager] Formation bounds: min={_formationMinOffset}, max={_formationMaxOffset}");
    }
    
    /// <summary>
    /// Set input active state for all squad members
    /// </summary>
    public void SetSquadInputActive(bool active)
    {
        foreach (var controller in _activeMembers)
        {
            if (controller != null)
            {
                controller.SetInputActive(active);
            }
        }
    }
    
    /// <summary>
    /// Handle a squad member's health changing
    /// </summary>
    private void OnMemberHealthChanged(RunnerPlayerController controller, float health)
    {
        if (health <= 0)
        {
            int index = _activeMembers.IndexOf(controller);
            OnMemberDied?.Invoke(index);
            
            // Recalculate alive count to be robust against multiple calls
            int newAliveCount = 0;
            foreach (var member in _activeMembers)
            {
                if (member != null && member.CurrentHealth > 0)
                {
                    newAliveCount++;
                }
            }
            _aliveMemberCount = newAliveCount;
            
            Debug.Log($"[RunnerSquadManager] Squad member died. {_aliveMemberCount} remaining.");
            
            // Check for squad wipe
            if (_aliveMemberCount <= 0)
            {
                Debug.Log("[RunnerSquadManager] SQUAD WIPED - All members dead!");
                OnSquadWiped?.Invoke();
                
                // Notify game manager
                if (RunnerGameManager.HasInstance)
                {
                    RunnerGameManager.Instance.MakeAllZombiesIdle();
                    RunnerGameManager.Instance.EndGame();
                }
            }
        }
    }
    
    /// <summary>
    /// Add a new member to the squad at runtime
    /// </summary>
    public void AddMember(GameObject prefab, Vector3 offset)
    {
        if (prefab == null) return;
        
        SquadMember newMember = new SquadMember
        {
            prefab = prefab,
            positionOffset = offset,
            isActive = true
        };
        
        squadMembers.Add(newMember);
        SpawnMember(newMember, squadMembers.Count - 1);
        _aliveMemberCount++;
    }
    
    /// <summary>
    /// Remove a member from the squad
    /// </summary>
    public void RemoveMember(int index)
    {
        if (index < 0 || index >= _activeMembers.Count) return;
        
        var controller = _activeMembers[index];
        if (controller != null)
        {
            Destroy(controller.gameObject);
        }
        
        _activeMembers.RemoveAt(index);
        if (index < squadMembers.Count)
        {
            squadMembers[index].spawnedController = null;
        }
    }
    
    #region Scenario Activation Control
    
    /// <summary>
    /// Set a specific squad member's active status by index.
    /// Must be called BEFORE SpawnSquad() or followed by RespawnSquad().
    /// </summary>
    public void SetMemberActive(int index, bool active)
    {
        if (index < 0 || index >= squadMembers.Count)
        {
            Debug.LogWarning($"[RunnerSquadManager] Invalid member index: {index}");
            return;
        }
        
        squadMembers[index].isActive = active;
        Debug.Log($"[RunnerSquadManager] Member {index} active set to: {active}");
    }
    
    /// <summary>
    /// Set the number of active squad members (activates first N members).
    /// Must be called BEFORE SpawnSquad() or followed by RespawnSquad().
    /// </summary>
    /// <param name="count">Number of members to activate (starting from index 0)</param>
    public void SetActiveMemberCount(int count)
    {
        count = Mathf.Clamp(count, 0, squadMembers.Count);
        
        for (int i = 0; i < squadMembers.Count; i++)
        {
            squadMembers[i].isActive = (i < count);
        }
        
        Debug.Log($"[RunnerSquadManager] Active member count set to: {count}");
    }
    
    /// <summary>
    /// Get the total number of configured squad members (active or not)
    /// </summary>
    public int GetMemberCount()
    {
        return squadMembers.Count;
    }
    
    /// <summary>
    /// Get the number of currently active (isActive=true) squad member configurations
    /// </summary>
    public int GetActiveMemberConfigCount()
    {
        int count = 0;
        foreach (var member in squadMembers)
        {
            if (member != null && member.isActive) count++;
        }
        return count;
    }
    
    /// <summary>
    /// Respawn the squad with current activation settings.
    /// Call this after changing member activation status at runtime.
    /// </summary>
    public void RespawnSquad()
    {
        SpawnSquad();
    }
    
    /// <summary>
    /// Dynamically add members to the squad.
    /// Positions are calculated alternating sides based on base player.
    /// </summary>
    public void AddMembers(int count, float deltaX, float deltaZ)
    {
        if (squadMembers.Count == 0 && count > 0)
        {
            Debug.LogError("[RunnerSquadManager] No existing squad to clone prefab from!");
            return;
        }
        
        // Get leader's movement state to sync with new members
        RunnerPlayerController leader = GetLeader();
        float leaderVelocity = leader != null ? leader.CurrentVelocity : 0f;
        
        // Use dedicated addMemberPrefab if set, otherwise fall back to first squad member's prefab
        GameObject prefabToUse = addMemberPrefab != null ? addMemberPrefab : squadMembers[0].prefab;
        int startIndex = squadMembers.Count;
        
        for (int i = 0; i < count; i++)
        {
            int index = startIndex + i;
            
            // Calculate offset based on index (Alternating sides V-formation)
            // 0 (Center) - assumed exists
            // 1: Right 1
            // 2: Left 1
            // 3: Right 2
            // 4: Left 2
            
            float side = (index % 2 != 0) ? 1f : -1f; // Odd = Right (1), Even = Left (-1)
            int row = (index + 1) / 2;
            
            // Use provided deltas
            // deltaX is horizontal spacing
            // deltaZ is depth spacing (Y in user input mapped to Z here)
            Vector3 offset = new Vector3(
                side * row * deltaX, 
                0, 
                -row * deltaZ
            );
            
            SquadMember newMember = new SquadMember();
            newMember.prefab = prefabToUse;
            newMember.isActive = true;
            newMember.positionOffset = offset;
            
            squadMembers.Add(newMember);
            
            // Spawn the new member
            SpawnMember(newMember, index);
            
            // Sync movement state from leader to prevent fast catch-up movement
            // The new member spawns at its correct offset position, so sync target to its spawn X
            if (newMember.spawnedController != null)
            {
                float newMemberTargetX = _squadCenterX + offset.x;
                newMember.spawnedController.SyncMovementState(leaderVelocity, newMemberTargetX);
                _aliveMemberCount++;
            }
        }
        
        // Recalculate formation bounds to include new members
        CalculateFormationBounds();
        
        Debug.Log($"[RunnerSquadManager] Added {count} new members. Total: {squadMembers.Count}");
    }

    #endregion
    
    /// <summary>
    /// Apply a modifier to the entire squad and update global stats for future spawns
    /// </summary>
    public void ApplyModifierToSquad(RunnerModifierGate.ModifierType type, RunnerModifierGate.OperationType operation, float value)
    {
        // 1. Update the global persistent state for FUTURE spawns
        if (overrideShootingRange && type == RunnerModifierGate.ModifierType.ShootingRange)
        {
             _currentShootingRange = ApplyOperation(_currentShootingRange, operation, value);
             _currentShootingRange = Mathf.Clamp(_currentShootingRange, 5f, 100f);
             Debug.Log($"[RunnerSquadManager] Global Shooting Range updated to: {_currentShootingRange}");
        }
        
        // 2. Apply to all CURRENT active members
        foreach (var member in _activeMembers)
        {
            if (member != null && member.CurrentHealth > 0)
            {
                member.ApplyModifier(type, operation, value);
            }
        }
    }

    private float ApplyOperation(float current, RunnerModifierGate.OperationType op, float percentage)
    {
        switch (op)
        {
            case RunnerModifierGate.OperationType.Increase: 
                return current + (current * percentage / 100f);
            case RunnerModifierGate.OperationType.Decrease: 
                return current - (current * percentage / 100f);
            default: 
                return current;
        }
    }

    /// <summary>
    /// Clear all spawned squad members
    /// </summary>
    public void ClearSquad()
    {
        // First destroy tracked members
        foreach (var controller in _activeMembers)
        {
            if (controller != null)
            {
                Destroy(controller.gameObject);
            }
        }
        
        // Also look for any other player controllers in the scene (rouge/testing objects)
        // and destroy them to prevent duplicates
        var allPlayers = FindObjectsOfType<RunnerPlayerController>();
        foreach (var p in allPlayers)
        {
            if (p != null)
            {
                Debug.Log($"[RunnerSquadManager] Destroying existing player found in scene: {p.name}");
                Destroy(p.gameObject);
            }
        }
        
        _activeMembers.Clear();
        
        foreach (var member in squadMembers)
        {
            if (member != null)
            {
                member.spawnedController = null;
            }
        }
        
        _aliveMemberCount = 0;
    }
    
    /// <summary>
    /// Apply a formation pattern to squad member offsets
    /// </summary>
    private void ApplyFormation()
    {
        int count = squadMembers.Count;
        if (count == 0) return;
        
        switch (formationType)
        {
            case FormationType.Line:
                ApplyLineFormation();
                break;
            case FormationType.Column:
                ApplyColumnFormation();
                break;
            case FormationType.VFormation:
                ApplyVFormation();
                break;
            case FormationType.Diamond:
                ApplyDiamondFormation();
                break;
        }
    }
    
    private void ApplyLineFormation()
    {
        int count = squadMembers.Count;
        float totalWidth = (count - 1) * defaultXSpacing;
        float startX = -totalWidth / 2f;
        
        for (int i = 0; i < count; i++)
        {
            squadMembers[i].positionOffset = new Vector3(startX + i * defaultXSpacing, 0, 0);
        }
    }
    
    private void ApplyColumnFormation()
    {
        for (int i = 0; i < squadMembers.Count; i++)
        {
            squadMembers[i].positionOffset = new Vector3(0, 0, -i * defaultZSpacing);
        }
    }
    
    private void ApplyVFormation()
    {
        int count = squadMembers.Count;
        for (int i = 0; i < count; i++)
        {
            // First member at front, others fan out behind
            if (i == 0)
            {
                squadMembers[i].positionOffset = Vector3.zero;
            }
            else
            {
                int side = (i % 2 == 1) ? -1 : 1;  // Alternate left/right
                int row = (i + 1) / 2;
                squadMembers[i].positionOffset = new Vector3(
                    side * row * defaultXSpacing,
                    0,
                    -row * defaultZSpacing
                );
            }
        }
    }
    
    private void ApplyDiamondFormation()
    {
        // Diamond: center, front, back, left, right, then corners
        Vector3[] positions = new Vector3[]
        {
            Vector3.zero,                                           // Center
            new Vector3(0, 0, defaultZSpacing),                     // Front
            new Vector3(0, 0, -defaultZSpacing),                    // Back
            new Vector3(-defaultXSpacing, 0, 0),                    // Left
            new Vector3(defaultXSpacing, 0, 0),                     // Right
            new Vector3(-defaultXSpacing, 0, defaultZSpacing),      // Front-Left
            new Vector3(defaultXSpacing, 0, defaultZSpacing),       // Front-Right
            new Vector3(-defaultXSpacing, 0, -defaultZSpacing),     // Back-Left
            new Vector3(defaultXSpacing, 0, -defaultZSpacing),      // Back-Right
        };
        
        for (int i = 0; i < squadMembers.Count && i < positions.Length; i++)
        {
            squadMembers[i].positionOffset = positions[i];
        }
    }
    
    /// <summary>
    /// Get the first alive squad member (for targeting, etc.)
    /// </summary>
    public RunnerPlayerController GetLeader()
    {
        foreach (var controller in _activeMembers)
        {
            if (controller != null && controller.CurrentHealth > 0)
            {
                return controller;
            }
        }
        return null;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        // Always use this transform for gizmos (matches spawn logic)
        
        // Draw squad center
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        
        // Draw member positions
        Gizmos.color = Color.cyan;
        foreach (var member in squadMembers)
        {
            if (member != null && member.isActive)
            {
                Vector3 pos = transform.position + member.positionOffset;
                Gizmos.DrawWireCube(pos, new Vector3(0.5f, 1f, 0.5f));
                Gizmos.DrawLine(transform.position, pos);
            }
        }
    }
}
