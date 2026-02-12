using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Wave-based zombie spawning system with level configs.
/// Tracks kills per wave and triggers win when all waves are complete.
/// </summary>
/// <summary>
/// Wave-based zombie spawning system with level configs.
/// Tracks kills per wave and triggers win when all waves are complete.
/// </summary>
public class RunnerWaveManager : MonoBehaviour
{
    [System.Serializable]
    public class WaveConfig
    {
        [Tooltip("Delay in seconds before this wave starts spawning")]
        public float delayBeforeWave = 0f;
        
        [Tooltip("Number of zombies to spawn for each prefab in zombiePrefabs array")]
        public int[] zombieCounts;
        
        /// <summary>
        /// Total zombies across all types in this wave
        /// </summary>
        public int TotalZombieCount
        {
            get
            {
                if (zombieCounts == null) return 0;
                int total = 0;
                for (int i = 0; i < zombieCounts.Length; i++)
                    total += zombieCounts[i];
                return total;
            }
        }
    }
    
    [System.Serializable]
    public class WaveLevelConfig
    {
        [Tooltip("All waves for this level")]
        public WaveConfig[] waves;
    }
    
    [Header("Zombie Prefabs")]
    [Tooltip("Array of available zombie prefabs. Each wave uses zombieCounts to specify how many of each type.")]
    [SerializeField] private GameObject[] zombiePrefabs;
    
    [Header("Level Configurations")]
    [SerializeField] private WaveLevelConfig[] levelConfigs;
    
    [Header("References")]
    [SerializeField] private RunnerEnemySpawner enemySpawner;
    
    [Header("Settings")]
    [SerializeField] private bool autoStartOnAwake = true;
    [SerializeField] private int startLevelIndex = 0;
    
    [Header("Initial Waves Configuration")]
    [Tooltip("If assigned, Wave 1 will spawn immediately at this location")]
    [SerializeField] private Transform initialWave1Position;
    [Tooltip("If assigned, Wave 2 will spawn immediately at this location")]
    [SerializeField] private Transform initialWave2Position;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private int currentWaveIndex = 0;
    [SerializeField] private int zombiesKilledThisWave = 0;
    [SerializeField] private int zombiesSpawnedThisWave = 0;
    
    // Events
    public System.Action<int> OnWaveStarted;           // Wave index
    public System.Action<int> OnWaveComplete;          // Wave index
    public System.Action OnLevelComplete;              // All waves done
    public System.Action<int, int> OnZombieKilled;     // Killed count, total for wave
    
    // State
    private int _currentLevelIndex = 0;
    private bool _isRunning = false;
    private Coroutine _spawnCoroutine;
    private int _targetKillsForCurrentPhase = 0; // Tracks required kills for current phase (can be multiple waves)
    private bool _allWavesSpawned = false; // True when all waves have finished spawning
    
    private void Start()
    {
        // Auto-find enemy spawner if not assigned
        if (enemySpawner == null)
        {
            enemySpawner = FindObjectOfType<RunnerEnemySpawner>();
        }
        
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
            startLevelIndex = currentLevel;
            
            if (currentLevel > 0)
            {
                levelToLoad = (currentLevel - 1) % levelConfigs.Length;
                
                if (showDebugLogs)
                {
                    Debug.Log($"[RunnerWaveManager] Using ScenarioManager Level {currentLevel}. Mapped to config index {levelToLoad}");
                }
            }
        }
        
        StartLevel(levelToLoad);
    }
    
    /// <summary>
    /// Start spawning waves for a specific level
    /// </summary>
    public void StartLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levelConfigs.Length)
        {
            Debug.LogWarning($"[RunnerWaveManager] Invalid level index: {levelIndex}. Max: {levelConfigs.Length - 1}");
            return;
        }
        
        _currentLevelIndex = levelIndex;
        currentWaveIndex = 0;
        _isRunning = true;
        _allWavesSpawned = false;
        
        WaveLevelConfig config = levelConfigs[levelIndex];
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerWaveManager] Started level {levelIndex} with {config.waves.Length} waves");
        }
        
        // Check for Initial Waves Pre-spawning Logic
        bool hasInit1 = initialWave1Position != null && config.waves.Length >= 1;
        bool hasInit2 = initialWave2Position != null && config.waves.Length >= 2;
        
        if (hasInit1 || hasInit2)
        {
            zombiesKilledThisWave = 0;
            zombiesSpawnedThisWave = 0;
            _targetKillsForCurrentPhase = 0;
            
            // Spawn Wave 1 Immediate (Sequential spawning at custom pos)
            if (hasInit1)
            {
                WaveConfig w1 = config.waves[0];
                StartCoroutine(SpawnWaveAtPositionCoroutine(w1, initialWave1Position.position));
                _targetKillsForCurrentPhase += w1.TotalZombieCount;
                if (showDebugLogs) Debug.Log($"[RunnerWaveManager] Initial Wave 1 Started Spawning ({w1.TotalZombieCount}) at {initialWave1Position.name}");
            }
            
            // Spawn Wave 2 Immediate if exists
            if (hasInit2)
            {
                WaveConfig w2 = config.waves[1];
                StartCoroutine(SpawnWaveAtPositionCoroutine(w2, initialWave2Position.position));
                _targetKillsForCurrentPhase += w2.TotalZombieCount;
                currentWaveIndex = 1; // Mark as being in Wave 2 (index 1)
                if (showDebugLogs) Debug.Log($"[RunnerWaveManager] Initial Wave 2 Started Spawning ({w2.TotalZombieCount}) at {initialWave2Position.name}");
            }
            
            // Wave 3+ handled by chained spawning coroutine (spawns based on time, not kills)
            if (config.waves.Length >= 3)
            {
                // Add all remaining waves to target kills (player must kill all to win)
                for (int i = 2; i < config.waves.Length; i++)
                {
                    _targetKillsForCurrentPhase += config.waves[i].TotalZombieCount;
                }
                
                currentWaveIndex = 2; // Mark as effectively in Wave 3
                
                // Start the chained spawning - wave 3 spawns immediately, then delay + wave 4, etc.
                StartCoroutine(SpawnRemainingWavesChained(config, 2));
            }
            else
            {
                _allWavesSpawned = true; // Only waves 1-2, already spawning
                currentWaveIndex = hasInit2 ? 1 : 0;
            }
            
            OnWaveStarted?.Invoke(currentWaveIndex);
        }
        else
        {
            // Normal start
            StartWave(0);
        }
    }
    
    /// <summary>
    /// Start a specific wave
    /// </summary>
    private void StartWave(int waveIndex)
    {
        WaveLevelConfig levelConfig = GetCurrentLevelConfig();
        if (levelConfig == null || levelConfig.waves == null || waveIndex >= levelConfig.waves.Length)
        {
            Debug.LogError($"[RunnerWaveManager] Invalid wave index: {waveIndex}");
            return;
        }
        
        currentWaveIndex = waveIndex;
        zombiesKilledThisWave = 0;
        zombiesSpawnedThisWave = 0;
        
        WaveConfig waveConfig = levelConfig.waves[waveIndex];
        _targetKillsForCurrentPhase = waveConfig.TotalZombieCount;
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerWaveManager] Starting Wave {waveIndex + 1}/{levelConfig.waves.Length} - {waveConfig.TotalZombieCount} zombies");
        }
        
        OnWaveStarted?.Invoke(waveIndex);
        
        // Start spawning zombies with interval
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
        }
        _spawnCoroutine = StartCoroutine(SpawnWaveCoroutine(waveConfig));
    }
    
    /// <summary>
    /// Coroutine to spawn zombies for a wave with interval - supports mixed types
    /// </summary>
    private IEnumerator SpawnWaveCoroutine(WaveConfig waveConfig)
    {
        // Iterate through each zombie type
        if (waveConfig.zombieCounts != null)
        {
            for (int typeIndex = 0; typeIndex < waveConfig.zombieCounts.Length; typeIndex++)
            {
                int countForType = waveConfig.zombieCounts[typeIndex];
                GameObject prefab = GetZombiePrefab(typeIndex);
                
                for (int j = 0; j < countForType; j++)
                {
                    SpawnSingleZombie(prefab);
                    zombiesSpawnedThisWave++;
                    
                    float interval = enemySpawner != null ? enemySpawner.CurrentSpawnInterval : 1f;
                    yield return new WaitForSeconds(interval);
                }
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerWaveManager] All {waveConfig.TotalZombieCount} zombies spawned for wave {currentWaveIndex + 1}");
        }
    }
    
    /// <summary>
    /// Coroutine to spawn a wave at a specific position - supports mixed types
    /// </summary>
    private IEnumerator SpawnWaveAtPositionCoroutine(WaveConfig waveConfig, Vector3 position)
    {
        if (waveConfig.zombieCounts != null)
        {
            for (int typeIndex = 0; typeIndex < waveConfig.zombieCounts.Length; typeIndex++)
            {
                int countForType = waveConfig.zombieCounts[typeIndex];
                GameObject prefab = GetZombiePrefab(typeIndex);
                
                for (int j = 0; j < countForType; j++)
                {
                    enemySpawner.SpawnSingleEnemyAtPosition(prefab, position);
                    
                    float interval = enemySpawner != null ? enemySpawner.CurrentSpawnInterval : 1f;
                    yield return new WaitForSeconds(interval);
                }
            }
        }
    }
    
    /// <summary>
    /// Get the zombie prefab at the given index, or null (uses default) if out of range
    /// </summary>
    private GameObject GetZombiePrefab(int index)
    {
        if (zombiePrefabs == null || index < 0 || index >= zombiePrefabs.Length)
            return null;
        return zombiePrefabs[index];
    }
    
    /// <summary>
    /// Coroutine to spawn remaining waves in sequence with per-wave delays.
    /// Each wave waits for its own delayBeforeWave, then spawns.
    /// </summary>
    private IEnumerator SpawnRemainingWavesChained(WaveLevelConfig levelConfig, int startWaveIndex)
    {
        for (int i = startWaveIndex; i < levelConfig.waves.Length; i++)
        {
            WaveConfig waveConfig = levelConfig.waves[i];
            currentWaveIndex = i;
            
            // Wait for this wave's delay BEFORE spawning
            if (waveConfig.delayBeforeWave > 0f)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[RunnerWaveManager] Waiting {waveConfig.delayBeforeWave}s before Wave {i + 1}...");
                }
                yield return new WaitForSeconds(waveConfig.delayBeforeWave);
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"[RunnerWaveManager] Wave {i + 1} Started Spawning ({waveConfig.TotalZombieCount}) at Default Position");
            }
            
            OnWaveStarted?.Invoke(i);
            
            // Spawn all zombies for this wave using mixed types
            if (waveConfig.zombieCounts != null)
            {
                for (int typeIndex = 0; typeIndex < waveConfig.zombieCounts.Length; typeIndex++)
                {
                    int countForType = waveConfig.zombieCounts[typeIndex];
                    GameObject prefab = GetZombiePrefab(typeIndex);
                    
                    for (int j = 0; j < countForType; j++)
                    {
                        SpawnSingleZombie(prefab);
                        
                        float interval = enemySpawner != null ? enemySpawner.CurrentSpawnInterval : 1f;
                        yield return new WaitForSeconds(interval);
                    }
                }
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"[RunnerWaveManager] Wave {i + 1} spawning complete ({waveConfig.TotalZombieCount} zombies)");
            }
        }
        
        // All waves have finished spawning
        _allWavesSpawned = true;
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerWaveManager] All waves finished spawning. Waiting for player to kill remaining zombies...");
        }
    }
    
    /// <summary>
    /// Spawn a single zombie using the enemy spawner
    /// </summary>
    private void SpawnSingleZombie(GameObject prefab = null)
    {
        if (enemySpawner == null)
        {
            Debug.LogError("[RunnerWaveManager] Enemy spawner not assigned!");
            return;
        }
        
        enemySpawner.SpawnSingleEnemy(prefab);
    }
    
    /// <summary>
    /// Call this when a zombie is killed
    /// </summary>
    public void RegisterZombieKill()
    {
        if (!_isRunning) return;
        
        zombiesKilledThisWave++;
        
        // Don't rely on current config lookup if we are in mixed phase, use _targetKillsForCurrentPhase
        int targetKills = _targetKillsForCurrentPhase;
        if (targetKills == 0)
        {
             // Fallback if not set (legacy or error)
             WaveLevelConfig levelConfig = GetCurrentLevelConfig();
             if (levelConfig != null) targetKills = levelConfig.waves[currentWaveIndex].TotalZombieCount;
        }

        OnZombieKilled?.Invoke(zombiesKilledThisWave, targetKills);
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerWaveManager] Zombie killed: {zombiesKilledThisWave}/{targetKills} in Phase (Wave Index {currentWaveIndex})");
        }
        
        // Check if wave is complete
        if (zombiesKilledThisWave >= targetKills)
        {
            OnWaveCompleted();
        }
    }
    
    /// <summary>
    /// Called when all zombies in current phase are killed
    /// </summary>
    private void OnWaveCompleted()
    {
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerWaveManager] All zombies killed! (Wave Index: {currentWaveIndex + 1})");
        }
        
        OnWaveComplete?.Invoke(currentWaveIndex);
        
        WaveLevelConfig levelConfig = GetCurrentLevelConfig();
        
        // If all waves have finished spawning AND all zombies killed, player wins
        if (_allWavesSpawned)
        {
            TriggerLevelWin();
        }
        else if (currentWaveIndex + 1 < levelConfig.waves.Length)
        {
            // For non-chained mode (normal start), start next wave after delay
            StartCoroutine(StartNextWaveAfterDelay());
        }
        else
        {
            // All waves complete - player wins!
            TriggerLevelWin();
        }
    }
    
    private IEnumerator StartNextWaveAfterDelay()
    {
        // Get the NEXT wave's delay
        WaveLevelConfig levelConfig = GetCurrentLevelConfig();
        int nextWaveIndex = currentWaveIndex + 1;
        
        if (levelConfig != null && nextWaveIndex < levelConfig.waves.Length)
        {
            float delay = levelConfig.waves[nextWaveIndex].delayBeforeWave;
            if (delay > 0f)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[RunnerWaveManager] Waiting {delay}s before Wave {nextWaveIndex + 1}...");
                }
                yield return new WaitForSeconds(delay);
            }
        }
        
        StartWave(nextWaveIndex);
    }
    
    /// <summary>
    /// Called when all waves are complete
    /// </summary>
    private void TriggerLevelWin()
    {
        _isRunning = false;
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerWaveManager] LEVEL COMPLETE! All waves finished.");
        }
        
        OnLevelComplete?.Invoke();
        
        // Notify game manager
        if (RunnerGameManager.Instance != null)
        {
            RunnerGameManager.Instance.TriggerWin();
        }
    }
    
    /// <summary>
    /// Stop wave manager
    /// </summary>
    public void StopWaves()
    {
        _isRunning = false;
        
        if (_spawnCoroutine != null)
        {
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
        }
        
        if (showDebugLogs)
        {
            Debug.Log("[RunnerWaveManager] Waves stopped");
        }
    }
    
    /// <summary>
    /// Reset wave manager for new game
    /// </summary>
    public void ResetWaves()
    {
        StopWaves();
        currentWaveIndex = 0;
        zombiesKilledThisWave = 0;
        zombiesSpawnedThisWave = 0;
        _allWavesSpawned = false;
    }
    
    private WaveLevelConfig GetCurrentLevelConfig()
    {
        if (_currentLevelIndex >= 0 && _currentLevelIndex < levelConfigs.Length)
        {
            return levelConfigs[_currentLevelIndex];
        }
        return null;
    }
    
    // Public accessors
    public int CurrentWave => currentWaveIndex + 1;
    public int TotalWaves => GetCurrentLevelConfig()?.waves?.Length ?? 0;
    public int ZombiesKilled => zombiesKilledThisWave;
    public int ZombiesToKill => GetCurrentLevelConfig()?.waves[currentWaveIndex].TotalZombieCount ?? 0;
    public bool IsRunning => _isRunning;
}
