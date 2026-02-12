using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a queue of barrels with per-level configuration.
/// Barrels spawn at a transform position, spaced behind each other.
/// When a barrel is destroyed, its modifier is applied to the squad and the queue advances.
/// </summary>
public class RunnerBarrelQueue : MonoBehaviour
{
    [System.Serializable]
    public class BarrelConfig
    {
        [Tooltip("Health for this barrel")]
        public float health = 100f;
        
        [Tooltip("Modifier type to apply when destroyed")]
        public RunnerModifierGate.ModifierType modifierType;
        
        [Tooltip("Modifier value (always Increase operation)")]
        public float value = 10f;
    }
    
    [System.Serializable]
    public class BarrelWaveConfig
    {
        [Tooltip("Delay in seconds before this wave starts spawning")]
        public float delayBeforeWave = 0f;
        
        [Tooltip("Barrels to spawn in this wave")]
        public BarrelConfig[] barrelConfigs;
        
        /// <summary>
        /// Total barrels in this wave
        /// </summary>
        public int TotalBarrelCount => barrelConfigs != null ? barrelConfigs.Length : 0;
    }
    
    [System.Serializable]
    public class BarrelLevelConfig
    {
        [Tooltip("Enable/disable barrels for this level")]
        public bool enabled = true;
        
        [Tooltip("All waves for this level")]
        public BarrelWaveConfig[] waves;
    }
    
    [Header("Barrel Prefab")]
    [Tooltip("3D barrel prefab with RunnerBarrel component")]
    [SerializeField] private GameObject barrelPrefab;
    
    [Header("Global Type Visuals")]
    [Tooltip("Sprite for Range modifier")]
    [SerializeField] private Sprite rangeSprite;
    [Tooltip("Sprite for Fire Rate modifier")]
    [SerializeField] private Sprite fireRateSprite;
    [Tooltip("Sprite for OneShot Damage modifier")]
    [SerializeField] private Sprite damageSprite;
    [Tooltip("Sprite for Add Member modifier")]
    [SerializeField] private Sprite addMemberSprite;
    [Tooltip("Sprite for Machine Gun modifier")]
    [SerializeField] private Sprite machineGunSprite;
    [Tooltip("Sprite for Bullet Amount modifier")]
    [SerializeField] private Sprite bulletAmountSprite;

    [Header("Effects")]
    [Tooltip("Particle system to play when a barrel is hit")]
    [SerializeField] private ParticleSystem hitParticlePrefab;

    public ParticleSystem HitParticlePrefab => hitParticlePrefab;
    
    [Header("Spawn Settings")]
    [Tooltip("First barrel spawns here, others spawn behind")]
    [SerializeField] private Transform spawnPoint;
    
    [Tooltip("Distance between barrels (Z axis)")]
    [SerializeField] private float barrelSpacing = 2f;
    
    [Header("Level Configurations")]
    [Tooltip("Index 0 = Level 1, Index 1 = Level 2, etc.")]
    [SerializeField] private BarrelLevelConfig[] levelConfigs;
    
    [Header("Queue Behavior")]
    [Tooltip("Speed at which barrels slide forward when queue advances")]
    [SerializeField] private float slideSpeed = 5f;
    [Tooltip("Rotation speed on Z axis while sliding (degrees/sec)")]
    [SerializeField] private float slideRotationSpeed = -360f;
    
    [Header("Shooting Rules")]
    [Tooltip("Squad must be to the left of this X position to shoot barrels")]
    [SerializeField] private float shootingStartXThreshold = 0f;
    
    [Header("Auto Movement")]
    [Tooltip("If assigned, barrels will auto-move towards this threshold")]
    [SerializeField] private Transform movementThreshold;
    
    [Tooltip("If true, barrels are destroyed when reaching threshold. If false, queue stops when first barrel reaches it.")]
    [SerializeField] private bool Destroy = false;
    
    [Tooltip("Speed at which barrels move forward automatically")]
    [SerializeField] private float autoMoveSpeed = 5f;
    
    [Header("Auto Start")]
    [SerializeField] private bool autoStartOnAwake = true;
    [SerializeField] private int startLevelIndex = 0;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    [Header("Object Pool")]
    [Tooltip("Initial number of barrels to pre-spawn in the pool")]
    [SerializeField] private int initialPoolSize = 10;
    
    // Object pool for barrels
    private List<RunnerBarrel> _barrelPool = new List<RunnerBarrel>();
    
    // Active barrels in queue order
    private List<RunnerBarrel> _activeBarrels = new List<RunnerBarrel>();
    private List<BarrelConfig> _activeConfigs = new List<BarrelConfig>();
    
    // Sliding state
    private bool _isSliding = false;
    private List<Vector3> _targetPositions = new List<Vector3>();
    
    // Auto-movement state
    private bool _isAutoMoving = false;
    private bool _hasStopped = false; // Track if auto-movement has stopped at threshold
    
    private int _currentLevelIndex = 0;
    
    public static RunnerBarrelQueue Instance { get; private set; }
    
    private void Awake()
    {
        Instance = this;
        InitializePool();
    }
    
    private void Start()
    {
        if (autoStartOnAwake && levelConfigs != null && levelConfigs.Length > 0)
        {
            StartCoroutine(WaitForScenarioManagerAndStart());
        }
    }
    
    private IEnumerator WaitForScenarioManagerAndStart()
    {
        // Wait for end of frame to let other services initialize
        yield return new WaitForEndOfFrame();
        
        // Wait until RunnerGameManager is in Playing state
        while (!RunnerGameManager.HasInstance || RunnerGameManager.Instance.CurrentState != RunnerGameManager.GameState.Playing)
        {
            yield return null;
        }
        
        // Wait for ScenarioManager to be available and have a valid level
        float timeout = 2f;
        float elapsed = 0f;
        while ((ScenarioManager.Instance == null || ScenarioManager.Instance.CurrentLevelNumber <= 0) && elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
        
        int levelToLoad = startLevelIndex;
        
        // Try to get level from ScenarioManager
        if (ScenarioManager.Instance != null)
        {
            int currentLevel = ScenarioManager.Instance.CurrentLevelNumber;
            
            // Show the actual level in the inspector
            startLevelIndex = currentLevel;
            
            // Adjust for 0-based array (Level 1 -> Index 0)
            // Use modulo to loop if level exceeds config count
            if (currentLevel > 0)
            {
                levelToLoad = (currentLevel - 1) % levelConfigs.Length;
                
                if (showDebugLogs)
                {
                    Debug.Log($"[RunnerBarrelQueue] Using ScenarioManager Level {currentLevel}. Mapped to config index {levelToLoad}");
                }
            }
        }
        else
        {
             Debug.LogWarning("[RunnerBarrelQueue] ScenarioManager not found. using default startLevelIndex.");
        }
        
        StartLevel(levelToLoad);
    }
    
    private void Update()
    {
        // Handle smooth sliding when queue advances
        if (_isSliding)
        {
            UpdateSliding();
            // Don't run auto-movement while sliding - let barrels finish sliding first
            return;
        }
        
        // Handle auto-movement towards threshold
        if (_isAutoMoving && movementThreshold != null)
        {
            UpdateAutoMovement();
        }
    }
    
    /// <summary>
    /// Start spawning barrels for a specific level
    /// </summary>
    public void StartLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levelConfigs.Length)
        {
            Debug.LogWarning($"[RunnerBarrelQueue] Invalid level index: {levelIndex}. Max: {levelConfigs.Length - 1}");
            return;
        }
        
        // Clear any existing barrels
        ClearBarrels();
        
        _currentLevelIndex = levelIndex;
        BarrelLevelConfig levelConfig = levelConfigs[levelIndex];
        
        // Check if barrels are enabled for this level
        if (!levelConfig.enabled)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[RunnerBarrelQueue] Barrels disabled for level {levelIndex}. Skipping.");
            }
            return;
        }
        
        if (levelConfig.waves == null || levelConfig.waves.Length == 0)
        {
            Debug.LogWarning("[RunnerBarrelQueue] No barrel waves for this level!");
            return;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerBarrelQueue] Started level {levelIndex} with {levelConfig.waves.Length} barrel waves");
        }
        
        // Start spawning waves with coroutine (handles delay between waves)
        StartCoroutine(SpawnBarrelWavesChained(levelConfig));
        
        // Start auto-movement if threshold is assigned
        if (movementThreshold != null)
        {
            _isAutoMoving = true;
            if (showDebugLogs) Debug.Log("[RunnerBarrelQueue] Auto-movement started.");
        }
    }
    
    /// <summary>
    /// Coroutine to spawn barrel waves in sequence with delays before each wave.
    /// </summary>
    private IEnumerator SpawnBarrelWavesChained(BarrelLevelConfig levelConfig)
    {
        Vector3 basePos = spawnPoint != null ? spawnPoint.position : transform.position;
        int globalBarrelIndex = 0;
        
        for (int waveIndex = 0; waveIndex < levelConfig.waves.Length; waveIndex++)
        {
            BarrelWaveConfig waveConfig = levelConfig.waves[waveIndex];
            
            if (waveConfig.barrelConfigs == null || waveConfig.barrelConfigs.Length == 0)
            {
                if (showDebugLogs) Debug.Log($"[RunnerBarrelQueue] Wave {waveIndex + 1} has no barrels. Skipping.");
                continue;
            }
            
            // Wait for this wave's delay BEFORE spawning
            if (waveConfig.delayBeforeWave > 0f)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[RunnerBarrelQueue] Waiting {waveConfig.delayBeforeWave}s before Wave {waveIndex + 1}...");
                }
                yield return new WaitForSeconds(waveConfig.delayBeforeWave);
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"[RunnerBarrelQueue] Spawning Wave {waveIndex + 1}/{levelConfig.waves.Length} with {waveConfig.TotalBarrelCount} barrels");
            }
            
            // Spawn all barrels in this wave
            for (int i = 0; i < waveConfig.barrelConfigs.Length; i++)
            {
                BarrelConfig config = waveConfig.barrelConfigs[i];
                
                // Calculate position (spaced behind each other along Z axis)
                Vector3 spawnPos = basePos + Vector3.forward * (globalBarrelIndex * barrelSpacing);
                
                // Spawn barrel
                SpawnBarrel(spawnPos, config, globalBarrelIndex);
                globalBarrelIndex++;
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerBarrelQueue] All {levelConfig.waves.Length} barrel waves spawned. Total barrels: {globalBarrelIndex}");
        }
    }
    
    private void SpawnBarrel(Vector3 position, BarrelConfig config, int queueIndex)
    {
        if (barrelPrefab == null)
        {
            Debug.LogError("[RunnerBarrelQueue] Barrel prefab is not assigned!");
            return;
        }
        
        // Get barrel from pool
        RunnerBarrel barrel = GetBarrelFromPool(position);
        
        if (barrel != null)
        {
            // Select Global Sprite
            Sprite targetSprite = null;
            switch (config.modifierType)
            {
                case RunnerModifierGate.ModifierType.ShootingRange:
                    targetSprite = rangeSprite;
                    break;
                case RunnerModifierGate.ModifierType.FireRate:
                    targetSprite = fireRateSprite;
                    break;
                case RunnerModifierGate.ModifierType.BulletDamage:
                    targetSprite = damageSprite;
                    break;
                case RunnerModifierGate.ModifierType.AddMember:
                    targetSprite = addMemberSprite;
                    break;
                case RunnerModifierGate.ModifierType.MachineGun:
                    targetSprite = machineGunSprite;
                    break;
                case RunnerModifierGate.ModifierType.BulletAmount:
                    targetSprite = bulletAmountSprite;
                    break;
            }

            // Initialize barrel with config
            barrel.Initialize(config.health, targetSprite, queueIndex, config.modifierType, config.value);
            
            // Subscribe to destruction event
            barrel.OnDestroyed += OnBarrelDestroyed;
            
            _activeBarrels.Add(barrel);
            _activeConfigs.Add(config);
        }
    }
    
    #region Object Pool
    
    /// <summary>
    /// Pre-instantiate barrels for the pool
    /// </summary>
    private void InitializePool()
    {
        if (barrelPrefab == null) return;
        
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject barrelObj = Instantiate(barrelPrefab, Vector3.zero, barrelPrefab.transform.rotation, transform);
            RunnerBarrel barrel = barrelObj.GetComponent<RunnerBarrel>();
            
            if (barrel != null)
            {
                barrel.Deactivate();
                _barrelPool.Add(barrel);
            }
            else
            {
                Destroy(barrelObj);
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerBarrelQueue] Pool initialized with {_barrelPool.Count} barrels");
        }
    }
    
    /// <summary>
    /// Get a barrel from the pool or create a new one
    /// </summary>
    private RunnerBarrel GetBarrelFromPool(Vector3 position)
    {
        // Search for inactive barrel in pool
        foreach (var barrel in _barrelPool)
        {
            if (barrel != null && !barrel.gameObject.activeInHierarchy)
            {
                barrel.transform.position = position;
                barrel.transform.rotation = barrelPrefab.transform.rotation;
                barrel.MarkActive();
                barrel.gameObject.SetActive(true);
                return barrel;
            }
        }
        
        // No available barrel in pool - create new one
        if (barrelPrefab == null)
        {
            Debug.LogError("[RunnerBarrelQueue] Cannot create barrel: Prefab is null!");
            return null;
        }
        
        GameObject newBarrelObj = Instantiate(barrelPrefab, position, barrelPrefab.transform.rotation, transform);
        RunnerBarrel newBarrel = newBarrelObj.GetComponent<RunnerBarrel>();
        
        if (newBarrel != null)
        {
            _barrelPool.Add(newBarrel);
            newBarrel.MarkActive();
            
            if (showDebugLogs)
            {
                Debug.Log($"[RunnerBarrelQueue] Pool expanded. New size: {_barrelPool.Count}");
            }
            
            return newBarrel;
        }
        else
        {
            Debug.LogError("[RunnerBarrelQueue] Barrel prefab is missing RunnerBarrel component!");
            Destroy(newBarrelObj);
            return null;
        }
    }
    
    /// <summary>
    /// Return a barrel to the pool
    /// </summary>
    private void ReturnBarrelToPool(RunnerBarrel barrel)
    {
        if (barrel == null) return;
        
        // Unsubscribe from event
        barrel.OnDestroyed -= OnBarrelDestroyed;
        
        // Deactivate for pool reuse
        barrel.Deactivate();
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerBarrelQueue] Barrel returned to pool. Active: {_barrelPool.Count - GetInactivePoolCount()}/{_barrelPool.Count}");
        }
    }
    
    /// <summary>
    /// Get count of inactive barrels in pool
    /// </summary>
    private int GetInactivePoolCount()
    {
        int count = 0;
        foreach (var barrel in _barrelPool)
        {
            if (barrel != null && !barrel.gameObject.activeInHierarchy)
            {
                count++;
            }
        }
        return count;
    }
    
    #endregion
    
    [Header("Squad Add Settings")]
    [Tooltip("Horizontal offset step for new members")]
    [SerializeField] private float memberSpawnDeltaX = 2f;
    [Tooltip("Vertical/Z offset step for new members")]
    [SerializeField] private float memberSpawnDeltaY = 2f;

    // ... (rest of code)

    private void OnBarrelDestroyed(RunnerBarrel barrel)
    {
        int index = _activeBarrels.IndexOf(barrel);
        if (index < 0) return;
        
        // Get the config for this barrel
        BarrelConfig config = _activeConfigs[index];
        
        // Apply modifier logic
        if (config.modifierType == RunnerModifierGate.ModifierType.AddMember)
        {
             // Special case: Add members
             AddSquadMembers((int)config.value);
        }
        else
        {
             // Apply stat modifier
             ApplyModifierToSquad(config.modifierType, config.value);
        }
        
        // Remove from active lists
        _activeBarrels.RemoveAt(index);
        _activeConfigs.RemoveAt(index);
        
        // Return to pool
        ReturnBarrelToPool(barrel);
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerBarrelQueue] Barrel destroyed. Applied {config.modifierType} +{config.value}. Remaining: {_activeBarrels.Count}");
        }
        
        // Only trigger sliding if barrels have stopped at the threshold
        // If barrels are still auto-moving, just let them continue naturally
        if (_activeBarrels.Count > 0 && _hasStopped)
        {
            StartSlidingForward();
        }
    }
    
    private void ApplyModifierToSquad(RunnerModifierGate.ModifierType modifierType, float value)
    {
        RunnerSquadManager squadManager = FindObjectOfType<RunnerSquadManager>();
        
        if (squadManager != null)
        {
            // Always use Increase operation
            squadManager.ApplyModifierToSquad(modifierType, RunnerModifierGate.OperationType.Increase, value);
            
            if (showDebugLogs)
            {
                Debug.Log($"[RunnerBarrelQueue] Applied {modifierType} +{value}% to squad");
            }
        }
        else
        {
            Debug.LogWarning("[RunnerBarrelQueue] No RunnerSquadManager found to apply modifier!");
        }
    }

    private void AddSquadMembers(int count)
    {
        RunnerSquadManager squadManager = FindObjectOfType<RunnerSquadManager>();
        if (squadManager != null)
        {
            squadManager.AddMembers(count, memberSpawnDeltaX, memberSpawnDeltaY);
            
            if (showDebugLogs)
            {
                Debug.Log($"[RunnerBarrelQueue] Requesting to add {count} members");
            }
        }
    }
    
    /// <summary>
    /// Slides barrels forward to fill gaps (used when a barrel is destroyed by player).
    /// Barrels slide forward towards the threshold position, not backward to spawn.
    /// </summary>
    private void StartSlidingForward()
    {
        if (_activeBarrels.Count == 0) return;
        
        _targetPositions.Clear();
        
        Vector3 basePosition;
        
        // If stopped at threshold, use threshold position as the target for first barrel
        if (_hasStopped && movementThreshold != null)
        {
            // First barrel should move to threshold position
            basePosition = new Vector3(
                _activeBarrels[0].transform.position.x,
                _activeBarrels[0].transform.position.y,
                movementThreshold.position.z
            );
        }
        else
        {
            // Otherwise, use the first barrel's current position
            basePosition = _activeBarrels[0].transform.position;
        }
        
        for (int i = 0; i < _activeBarrels.Count; i++)
        {
            // Each barrel slides forward, spaced behind the first position
            Vector3 targetPos = basePosition + Vector3.forward * (i * barrelSpacing);
            _targetPositions.Add(targetPos);
        }
        
        _isSliding = true;
    }
    
    private void UpdateSliding()
    {
        bool allReached = true;
        
        for (int i = 0; i < _activeBarrels.Count; i++)
        {
            if (_activeBarrels[i] == null) continue;
            
            Vector3 currentPos = _activeBarrels[i].transform.position;
            Vector3 targetPos = _targetPositions[i];
            
            if (Vector3.Distance(currentPos, targetPos) > 0.01f)
            {
                _activeBarrels[i].transform.position = Vector3.MoveTowards(currentPos, targetPos, slideSpeed * Time.deltaTime);
                
                // Rotate on Z axis while sliding
                if (_activeBarrels[i].VisualRoot != null)
                {
                    _activeBarrels[i].VisualRoot.Rotate(0, 0, slideRotationSpeed * Time.deltaTime);
                }
                else
                {
                    _activeBarrels[i].transform.Rotate(0, 0, slideRotationSpeed * Time.deltaTime);
                }
                
                allReached = false;
            }
            else
            {
                _activeBarrels[i].transform.position = targetPos;
            }
        }
        
        if (allReached)
        {
            _isSliding = false;
            
            if (showDebugLogs)
            {
                Debug.Log("[RunnerBarrelQueue] Queue slide complete");
            }
        }
    }
    
    /// <summary>
    /// Continuously moves barrels forward towards the threshold.
    /// Checks if barrels pass the threshold and either stops or destroys them.
    /// </summary>
    private void UpdateAutoMovement()
    {
        // If no barrels yet (waiting for first wave delay), just return but don't disable auto-movement
        if (_activeBarrels.Count == 0)
        {
            return;
        }
        
        float thresholdZ = movementThreshold.position.z;
        
        // Check threshold condition FIRST before moving
        if (Destroy)
        {
            // Destroy mode: Check each barrel individually
            for (int i = _activeBarrels.Count - 1; i >= 0; i--)
            {
                if (_activeBarrels[i] != null && _activeBarrels[i].transform.position.z < thresholdZ)
                {
                    DestroyBarrelAtThreshold(i);
                }
            }
        }
        else
        {
            // Stop mode: Check FIRST barrel only
            if (_activeBarrels.Count > 0 && _activeBarrels[0] != null && 
                _activeBarrels[0].transform.position.z < thresholdZ)
            {
                _isAutoMoving = false;
                _hasStopped = true; // Mark that we've stopped at threshold
                if (showDebugLogs) Debug.Log("[RunnerBarrelQueue] Auto-movement stopped: First barrel reached threshold.");
                return;
            }
        }
        
        // Move all barrels forward (towards negative Z / towards player)
        foreach (var barrel in _activeBarrels)
        {
            if (barrel != null)
            {
                barrel.transform.position += Vector3.back * autoMoveSpeed * Time.deltaTime;
                
                // Rotate while moving
                if (barrel.VisualRoot != null)
                {
                    barrel.VisualRoot.Rotate(0, 0, slideRotationSpeed * Time.deltaTime);
                }
                else
                {
                    barrel.transform.Rotate(0, 0, slideRotationSpeed * Time.deltaTime);
                }
            }
        }
    }
    
    /// <summary>
    /// Returns a barrel to the pool when it reaches the threshold (no modifier applied).
    /// </summary>
    private void DestroyBarrelAtThreshold(int index)
    {
        if (index < 0 || index >= _activeBarrels.Count) return;
        
        RunnerBarrel barrel = _activeBarrels[index];
        if (barrel == null) return;
        
        // Remove from active lists
        _activeBarrels.RemoveAt(index);
        _activeConfigs.RemoveAt(index);
        
        // Return to pool
        ReturnBarrelToPool(barrel);
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerBarrelQueue] Barrel returned to pool at threshold. Remaining: {_activeBarrels.Count}");
        }
    }
    
    /// <summary>
    /// Clear all active barrels (return to pool)
    /// </summary>
    public void ClearBarrels()
    {
        foreach (var barrel in _activeBarrels)
        {
            if (barrel != null)
            {
                ReturnBarrelToPool(barrel);
            }
        }
        
        _activeBarrels.Clear();
        _activeConfigs.Clear();
        _targetPositions.Clear();
        _isSliding = false;
        _hasStopped = false;
    }
    
    /// <summary>
    /// Get remaining barrel count
    /// </summary>
    /// <summary>
    /// Check if squad leader is on the RIGHT side of the threshold (target Barrels)
    /// </summary>
    public bool CanShootBarrels()
    {
        RunnerSquadManager squadManager = FindObjectOfType<RunnerSquadManager>();
        if (squadManager == null) return false;
        
        RunnerPlayerController leader = squadManager.GetLeader();
        if (leader == null) return false;
        
        // "Right side of the threshold" -> X > Threshold
        return leader.transform.position.x > shootingStartXThreshold;
    }
    
    /// <summary>
    /// Check if squad leader is on the LEFT side of the threshold (target Zombies)
    /// </summary>
    public bool CanShootZombies()
    {
        RunnerSquadManager squadManager = FindObjectOfType<RunnerSquadManager>();
        if (squadManager == null) return true; // Default to true if no manager
        
        RunnerPlayerController leader = squadManager.GetLeader();
        if (leader == null) return true;
        
        // "Left side of the threshold" -> X <= Threshold
        return leader.transform.position.x <= shootingStartXThreshold;
    }

    /// <summary>
    /// Get remaining barrel count
    /// </summary>
    public int RemainingBarrels => _activeBarrels.Count;
}
