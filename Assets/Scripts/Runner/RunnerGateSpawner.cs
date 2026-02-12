using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Level-based gate spawner with configurable spawn count, timer, and gate configs per level.
/// Gates spawn on a timer cycle until player dies or wins.
/// </summary>
public class RunnerGateSpawner : MonoBehaviour
{
    [System.Serializable]
    public class GatePairConfig
    {
        public RunnerModifierGate.GateConfig leftConfig;
        public RunnerModifierGate.GateConfig rightConfig;
    }
    
    [System.Serializable]
    public class GateLevelConfig
    {
        [Tooltip("Enable/disable gates for this level")]
        public bool enabled = true;
        
        [Tooltip("Number of unique gate configurations for this level")]
        public int spawnCount = 2;
        
        [Tooltip("Time interval between spawns (seconds)")]
        public float timer = 3f;
        
        [Tooltip("Gate configurations (Left/Right for each gate). Size should match spawnCount.")]
        public GatePairConfig[] gateConfigs;
    }
    
    [Header("Gate Prefab")]
    [Tooltip("The gate prefab to spawn (set once)")]
    [SerializeField] private GameObject gatePrefab;
    
    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private int poolSize = 5;
    [SerializeField] private bool autoStartOnAwake = true;
    [SerializeField] private int startLevelIndex = 0;
    
    [Header("Level Configurations")]
    [SerializeField] private GateLevelConfig[] levelConfigs;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    // Pool
    private List<RunnerModifierGate> _gatePool = new List<RunnerModifierGate>();
    
    // Runtime state
    private int _currentLevelIndex = 0;
    private int _currentGateConfigIndex = 0;
    private float _spawnTimer = 0f;
    private bool _isSpawning = false;
    
    private void Start()
    {
        InitializePool();
        
        // Auto-start spawning if enabled
        if (autoStartOnAwake && levelConfigs != null && levelConfigs.Length > 0)
        {
            // Wait for RunnerGameManager to start the game
            StartCoroutine(WaitForGameStartAndBegin());
        }
    }

    private System.Collections.IEnumerator WaitForGameStartAndBegin()
    {
        // Wait until RunnerGameManager is in Playing state
        while (!RunnerGameManager.HasInstance || RunnerGameManager.Instance.CurrentState != RunnerGameManager.GameState.Playing)
        {
            yield return null;
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
                    Debug.Log($"[RunnerGateSpawner] Using ScenarioManager Level {currentLevel}. Mapped to config index {levelToLoad}");
                }
            }
        }
        else
        {
             Debug.LogWarning("[RunnerGateSpawner] ScenarioManager not found. using default startLevelIndex.");
        }
        
        StartLevel(levelToLoad);
    }
    
    private void Update()
    {
        if (!_isSpawning) return;
        
        // Increase timer
        _spawnTimer += Time.deltaTime;
        
        // Check if time to spawn
        GateLevelConfig currentLevelConfig = GetCurrentLevelConfig();
        if (currentLevelConfig != null && _spawnTimer >= currentLevelConfig.timer)
        {
            SpawnNextGate();
            _spawnTimer = 0f; // Reset timer
        }
    }
    
    /// <summary>
    /// Initialize the gate pool
    /// </summary>
    private void InitializePool()
    {
        if (gatePrefab == null)
        {
            Debug.LogError("[RunnerGateSpawner] Gate prefab is not assigned!");
            return;
        }
        
        for (int i = 0; i < poolSize; i++)
        {
            GameObject gateObj = Instantiate(gatePrefab, transform);
            gateObj.SetActive(false);
            
            RunnerModifierGate gate = gateObj.GetComponent<RunnerModifierGate>();
            if (gate != null)
            {
                _gatePool.Add(gate);
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerGateSpawner] Pool initialized with {poolSize} gates");
        }
    }
    
    /// <summary>
    /// Start spawning gates for a specific level
    /// </summary>
    public void StartLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levelConfigs.Length)
        {
            Debug.LogWarning($"[RunnerGateSpawner] Invalid level index: {levelIndex}. Max: {levelConfigs.Length - 1}");
            return;
        }
        
        GateLevelConfig levelConfig = levelConfigs[levelIndex];
        
        // Check if gates are enabled for this level
        if (!levelConfig.enabled)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[RunnerGateSpawner] Gates disabled for level {levelIndex}. Skipping.");
            }
            return;
        }
        
        _currentLevelIndex = levelIndex;
        _currentGateConfigIndex = 0;
        _spawnTimer = 0f;
        _isSpawning = true;
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerGateSpawner] Started level {levelIndex} - SpawnCount: {levelConfig.spawnCount}, Timer: {levelConfig.timer}s");
        }
    }
    
    /// <summary>
    /// Stop spawning gates
    /// </summary>
    public void StopSpawning()
    {
        _isSpawning = false;
        
        if (showDebugLogs)
        {
            Debug.Log("[RunnerGateSpawner] Spawning stopped");
        }
    }
    
    /// <summary>
    /// Spawn the next gate with current config
    /// </summary>
    private void SpawnNextGate()
    {
        GateLevelConfig levelConfig = GetCurrentLevelConfig();
        if (levelConfig == null || levelConfig.gateConfigs == null || levelConfig.gateConfigs.Length == 0)
        {
            Debug.LogWarning("[RunnerGateSpawner] No gate configs defined for current level!");
            return;
        }
        
        // Stop if we've reached the spawn limit
        if (_currentGateConfigIndex >= levelConfig.spawnCount)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[RunnerGateSpawner] Level {levelConfig.spawnCount} gate limit reached. Stopping.");
            }
            StopSpawning();
            return;
        }
        
        // Get current gate config (cycles through configs)
        int configIndex = _currentGateConfigIndex % levelConfig.gateConfigs.Length;
        GatePairConfig pairConfig = levelConfig.gateConfigs[configIndex];
        
        // Get pooled gate
        RunnerModifierGate gate = GetPooledGate();
        if (gate == null)
        {
            Debug.LogWarning("[RunnerGateSpawner] No available gate in pool!");
            return;
        }
        
        // Position gate at spawn point
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
        
        // Configure and reset gate
        gate.SetConfig(pairConfig.leftConfig, pairConfig.rightConfig);
        gate.ResetGate(spawnPos);
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerGateSpawner] Spawned gate {_currentGateConfigIndex} at {spawnPos}");
        }
        
        // Move to next config
        _currentGateConfigIndex++;
    }
    
    /// <summary>
    /// Get an inactive gate from the pool
    /// </summary>
    private RunnerModifierGate GetPooledGate()
    {
        foreach (var gate in _gatePool)
        {
            if (gate != null && !gate.gameObject.activeInHierarchy)
            {
                return gate;
            }
        }
        
        // Expand pool if needed
        if (gatePrefab != null)
        {
            GameObject gateObj = Instantiate(gatePrefab, transform);
            gateObj.SetActive(false);
            
            RunnerModifierGate newGate = gateObj.GetComponent<RunnerModifierGate>();
            if (newGate != null)
            {
                _gatePool.Add(newGate);
                return newGate;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Get current level config
    /// </summary>
    private GateLevelConfig GetCurrentLevelConfig()
    {
        if (_currentLevelIndex >= 0 && _currentLevelIndex < levelConfigs.Length)
        {
            return levelConfigs[_currentLevelIndex];
        }
        return null;
    }
    
    /// <summary>
    /// Reset spawner for new game
    /// </summary>
    public void ResetSpawner()
    {
        _currentLevelIndex = 0;
        _currentGateConfigIndex = 0;
        _spawnTimer = 0f;
        _isSpawning = false;
        
        // Deactivate all gates
        foreach (var gate in _gatePool)
        {
            if (gate != null)
            {
                gate.gameObject.SetActive(false);
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log("[RunnerGateSpawner] Spawner reset");
        }
    }
}
