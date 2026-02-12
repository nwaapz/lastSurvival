using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns enemies in rows from the front of the player.
/// Manages spawn patterns and difficulty progression.
/// Uses Zombie_Controller for enemies.
/// </summary>
public class RunnerEnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject defaultEnemyPrefab;
    [SerializeField] private float spawnZ = 50f;
    [SerializeField] private float spawnX = 0f;
    [SerializeField] private float spawnY = 0f;
    [SerializeField] private float despawnZ = -10f;
    [SerializeField] private float despawnX = 0f;
    [SerializeField] private float despawnY = 0f;
    [SerializeField] private float baseSpawnInterval = 0.5f;
    [SerializeField] private float minSpawnInterval = 0.1f;
    [SerializeField] private float spawnPointSpread = 0.5f;
    [SerializeField] private int enemiesPerSpawn = 2;
    [SerializeField] private float spawnXRandomRange = 2f;
    
    // Public accessor for spawn interval (used by WaveManager)
    public float CurrentSpawnInterval => _currentSpawnInterval > 0 ? _currentSpawnInterval : baseSpawnInterval;
    
    [Header("Difficulty Progression")]
    [SerializeField] private float intervalDecreaseRate = 0.01f;
    [SerializeField] private float difficultyIncreaseTime = 30f;
    
    [Header("Row Patterns")]
    [SerializeField] private bool useRandomPatterns = true;
    [SerializeField] private List<RunnerEnemyRowConfig> predefinedPatterns;
    
    [Header("Pool Settings")]
    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private bool useObjectPooling = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool showSpawnGizmos = true;
    
    // State
    private bool _isSpawning;
    private float _currentSpawnInterval;
    private float _difficultyTimer;
    private int _patternIndex;
    
    // Object Pool - Uses Zombie_Controller
    private Queue<Zombie_Controller> _enemyPool;
    private List<Zombie_Controller> _activeEnemies;
    
    // Lane config reference
    private RunnerLaneConfig _laneConfig;

    private void Awake()
    {
        _enemyPool = new Queue<Zombie_Controller>();
        _activeEnemies = new List<Zombie_Controller>();
        _currentSpawnInterval = baseSpawnInterval;
    }

    private void Start()
    {
        // Get lane config
        if (RunnerGameManager.Instance != null)
        {
            _laneConfig = RunnerGameManager.Instance.LaneConfig;
        }
        
        if (useObjectPooling)
        {
            InitializePool();
        }
    }

    private void Update()
    {
        if (_isSpawning)
        {
            UpdateDifficulty();
        }
    }

    #region Object Pooling
    
    private void InitializePool()
    {
        if (defaultEnemyPrefab == null)
        {
            Debug.LogError("[RunnerEnemySpawner] No enemy prefab assigned!");
            return;
        }
        
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreatePooledEnemy();
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerEnemySpawner] Pool initialized with {initialPoolSize} enemies");
        }
    }
    
    private Zombie_Controller CreatePooledEnemy()
    {
        GameObject enemyObj = Instantiate(defaultEnemyPrefab, transform);
        enemyObj.SetActive(false);
        
        Zombie_Controller enemy = enemyObj.GetComponent<Zombie_Controller>();
        if (enemy == null)
        {
            enemy = enemyObj.AddComponent<Zombie_Controller>();
        }
        
        _enemyPool.Enqueue(enemy);
        return enemy;
    }
    
    private Zombie_Controller GetEnemyFromPool()
    {
        Zombie_Controller enemy;
        
        if (_enemyPool.Count > 0)
        {
            enemy = _enemyPool.Dequeue();
        }
        else
        {
            enemy = CreatePooledEnemy();
            _enemyPool.Dequeue();
        }
        
        enemy.gameObject.SetActive(true);
        _activeEnemies.Add(enemy);
        
        // Subscribe to enemy destroyed event only (no more OnReachedEnd)
        enemy.OnEnemyDestroyed += HandleEnemyDestroyed;
        
        return enemy;
    }
    
    private void ReturnEnemyToPool(Zombie_Controller enemy)
    {
        if (enemy == null) return;
        
        // Unsubscribe from events
        enemy.OnEnemyDestroyed -= HandleEnemyDestroyed;
        
        _activeEnemies.Remove(enemy);
        
        if (useObjectPooling)
        {
            enemy.gameObject.SetActive(false);
            _enemyPool.Enqueue(enemy);
        }
        else
        {
            Destroy(enemy.gameObject);
        }
    }
    
    #endregion

    #region Spawning
    
    public void StartSpawning()
    {
        if (_isSpawning) return;
        
        _isSpawning = true;
        _currentSpawnInterval = baseSpawnInterval;
        _difficultyTimer = 0f;
        _patternIndex = 0;
        
        StartCoroutine(SpawnRoutine());
        
        if (showDebugLogs)
        {
            Debug.Log("[RunnerEnemySpawner] Spawning started");
        }
    }
    
    public void StopSpawning()
    {
        _isSpawning = false;
        StopAllCoroutines();
        
        if (showDebugLogs)
        {
            Debug.Log("[RunnerEnemySpawner] Spawning stopped");
        }
    }
    
    public void ClearAllEnemies()
    {
        foreach (var enemy in _activeEnemies.ToArray())
        {
            ReturnEnemyToPool(enemy);
        }
        _activeEnemies.Clear();
    }
    
    /// <summary>
    /// Make all active zombies go idle (stop movement and attacking).
    /// Useful for when the player dies.
    /// </summary>
    public void MakeAllZombiesIdle()
    {
        foreach (var enemy in _activeEnemies)
        {
            if (enemy != null && enemy.IsActive)
            {
                enemy.MakeIdle();
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerEnemySpawner] Made {_activeEnemies.Count} zombies idle");
        }
    }
    
    private IEnumerator SpawnRoutine()
    {
        while (_isSpawning)
        {
            SpawnRow();
            yield return new WaitForSeconds(_currentSpawnInterval);
        }
    }
    
    private void SpawnRow()
    {
        // Spawn multiple enemies per wave for horde effect
        for (int i = 0; i < enemiesPerSpawn; i++)
        {
            int laneCount = _laneConfig != null ? _laneConfig.LaneCount : 3;
            int randomLane = Random.Range(0, laneCount);
            SpawnEnemyInLane(randomLane, null);
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerEnemySpawner] Spawned {enemiesPerSpawn} enemies");
        }
    }
    
    public void SpawnSingleEnemy(GameObject prefabOverride = null)
    {
        int laneCount = _laneConfig != null ? _laneConfig.LaneCount : 3;
        int randomLane = Random.Range(0, laneCount);
        SpawnEnemyInLane(randomLane, prefabOverride);
    }
    
    /// <summary>
    /// Spawn a single enemy at a specific world position with optional prefab override
    /// </summary>
    public void SpawnSingleEnemyAtPosition(GameObject prefabOverride, Vector3 position)
    {
        int laneCount = _laneConfig != null ? _laneConfig.LaneCount : 3;
        int randomLane = Random.Range(0, laneCount);
        
        Zombie_Controller enemy;
        GameObject prefab = prefabOverride != null ? prefabOverride : defaultEnemyPrefab;
        
        if (useObjectPooling && prefabOverride == null)
        {
            enemy = GetEnemyFromPool();
        }
        else
        {
            GameObject enemyObj = Instantiate(prefab, transform);
            enemy = enemyObj.GetComponent<Zombie_Controller>();
            
            if (enemy == null)
            {
                enemy = enemyObj.AddComponent<Zombie_Controller>();
            }
            
            _activeEnemies.Add(enemy);
            enemy.OnEnemyDestroyed += HandleEnemyDestroyed;
        }
        
        // Add random X offset for variety
        float randomXOffset = Random.Range(-spawnXRandomRange, spawnXRandomRange);
        Vector3 spawnPos = new Vector3(position.x + randomXOffset, position.y, position.z);
        Vector3 despawnPos = new Vector3(despawnX, despawnY, despawnZ);
        
        enemy.Initialize(randomLane, spawnPos, despawnPos);
    }

    /// <summary>
    /// Starts spawning a wave of enemies at a specific position over time.
    /// Used for initial waves.
    /// </summary>
    public void StartCustomWaveSpawning(int count, Vector3 originPosition)
    {
        StartCoroutine(CustomWaveRoutine(count, originPosition));
    }
    
    private IEnumerator CustomWaveRoutine(int totalCount, Vector3 originPosition)
    {
        int spawnedCount = 0;
        int laneCount = _laneConfig != null ? _laneConfig.LaneCount : 3;
        
        if (showDebugLogs) Debug.Log($"[RunnerEnemySpawner] Starting custom wave at {originPosition} (Count: {totalCount})");
        
        while (spawnedCount < totalCount)
        {
            // Spawn a small batch (like SpawnRow)
            int batchSize = enemiesPerSpawn;
            if (spawnedCount + batchSize > totalCount) batchSize = totalCount - spawnedCount;
            
            for (int i = 0; i < batchSize; i++)
            {
                int randomLane = Random.Range(0, laneCount);
                
                Zombie_Controller enemy;
                if (useObjectPooling) enemy = GetEnemyFromPool();
                else 
                { 
                     GameObject enemyObj = Instantiate(defaultEnemyPrefab, transform);
                     enemy = enemyObj.GetComponent<Zombie_Controller>();
                     if (enemy==null) enemy = enemyObj.AddComponent<Zombie_Controller>();
                     _activeEnemies.Add(enemy);
                     enemy.OnEnemyDestroyed += HandleEnemyDestroyed;
                }
                
                // Calculate X pos logic with override origin
                float centerPos = 0f;
                // If origin.x is strictly the center, we add offsets relative to it
                // Or if origin.x effectively replaces spawnX
                
                if (_laneConfig != null) centerPos = _laneConfig.GetLanePosition(laneCount/2);
                
                float laneOffset = (randomLane - (laneCount/2)) * spawnPointSpread;
                float randomXOffset = Random.Range(-spawnXRandomRange, spawnXRandomRange);
                
                Vector3 spawnPos = new Vector3(originPosition.x + laneOffset + randomXOffset, originPosition.y, originPosition.z);
                Vector3 despawnPos = new Vector3(despawnX, despawnY, despawnZ);
                
                enemy.Initialize(randomLane, spawnPos, despawnPos);
                
                spawnedCount++;
            }
            
            // Wait interval
            yield return new WaitForSeconds(CurrentSpawnInterval);
        }
        
        if (showDebugLogs) Debug.Log($"[RunnerEnemySpawner] Finished custom wave at {originPosition}");
    }
    
    private void SpawnEnemyInLane(int laneIndex, GameObject prefabOverride = null)
    {
        Zombie_Controller enemy;
        
        if (useObjectPooling && prefabOverride == null)
        {
            enemy = GetEnemyFromPool();
        }
        else
        {
            GameObject prefab = prefabOverride != null ? prefabOverride : defaultEnemyPrefab;
            GameObject enemyObj = Instantiate(prefab, transform);
            enemy = enemyObj.GetComponent<Zombie_Controller>();
            
            if (enemy == null)
            {
                enemy = enemyObj.AddComponent<Zombie_Controller>();
            }
            
            _activeEnemies.Add(enemy);
            enemy.OnEnemyDestroyed += HandleEnemyDestroyed;
        }
        
        // Calculate spawn position
        int centerLane = 1;
        if (_laneConfig != null)
        {
            centerLane = _laneConfig.LaneCount / 2;
        }
        
        // Calculate offset from center based on lane index
        // If spawnPointSpread is small, they spawn closer together than the actual lane width
        float laneOffset = (laneIndex - centerLane) * spawnPointSpread;
        float centerPos = 0f; // Assuming 0 is the center of the road
        
        if (_laneConfig != null)
        {
             // Get the x-position of the physical center lane to ensure alignment
             centerPos = _laneConfig.GetLanePosition(centerLane);
        }

        // Add random X offset for more organic horde spawning
        float randomXOffset = Random.Range(-spawnXRandomRange, spawnXRandomRange);
        
        Vector3 spawnPos = new Vector3(spawnX + centerPos + laneOffset + randomXOffset, spawnY, spawnZ);
        Vector3 despawnPos = new Vector3(despawnX, despawnY, despawnZ);
        
        enemy.Initialize(laneIndex, spawnPos, despawnPos);
    }
    
    private float GetLaneXPosition(int laneIndex)
    {
        if (_laneConfig != null)
        {
            return _laneConfig.GetLanePosition(laneIndex);
        }
        
        if (RunnerGameManager.Instance != null)
        {
            return RunnerGameManager.Instance.GetLanePosition(laneIndex);
        }
        
        // Default: 3 lanes with 2 unit spacing
        return (laneIndex - 1) * 2f;
    }
    
    private RunnerEnemyRow GetNextRow()
    {
        int laneCount = _laneConfig != null ? _laneConfig.LaneCount : 3;
        
        if (useRandomPatterns)
        {
            return RunnerEnemyRowPatterns.Random(laneCount);
        }
        
        if (predefinedPatterns != null && predefinedPatterns.Count > 0)
        {
            var config = predefinedPatterns[_patternIndex];
            _patternIndex = (_patternIndex + 1) % predefinedPatterns.Count;
            return config.ToRow();
        }
        
        // Default: random pattern
        return RunnerEnemyRowPatterns.Random(laneCount);
    }
    
    #endregion

    #region Difficulty
    
    private void UpdateDifficulty()
    {
        _difficultyTimer += Time.deltaTime;
        
        if (_difficultyTimer >= difficultyIncreaseTime)
        {
            _difficultyTimer = 0f;
            
            _currentSpawnInterval = Mathf.Max(minSpawnInterval, 
                _currentSpawnInterval - intervalDecreaseRate * difficultyIncreaseTime);
            
            if (showDebugLogs)
            {
                Debug.Log($"[RunnerEnemySpawner] Difficulty increased! Spawn interval: {_currentSpawnInterval:F2}s");
            }
        }
    }
    
    #endregion

    #region Event Handlers
    
    private void HandleEnemyDestroyed(Zombie_Controller enemy)
    {
        // Notify wave manager of kill
        RunnerWaveManager waveManager = FindObjectOfType<RunnerWaveManager>();
        if (waveManager != null)
        {
            waveManager.RegisterZombieKill();
        }
        
        // Enemy was defeated by player - return to pool after death animation
        if (useObjectPooling)
        {
            StartCoroutine(DelayedReturnToPool(enemy, 1f));
        }
    }
    
    private IEnumerator DelayedReturnToPool(Zombie_Controller enemy, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnEnemyToPool(enemy);
    }
    
    #endregion

    #region Debug
    
    private void OnDrawGizmos()
    {
        if (!showSpawnGizmos) return;
        
        // Draw spawn line
        Gizmos.color = Color.red;
        float width = 10f;
        Vector3 left = new Vector3(spawnX - width / 2, spawnY, spawnZ);
        Vector3 right = new Vector3(spawnX + width / 2, spawnY, spawnZ);
        Gizmos.DrawLine(left, right);
        
        // Draw despawn line
        Gizmos.color = Color.blue;
        left = new Vector3(despawnX - width / 2, despawnY, despawnZ);
        right = new Vector3(despawnX + width / 2, despawnY, despawnZ);
        Gizmos.DrawLine(left, right);
        
        // Draw lane positions at spawn line
        if (_laneConfig != null)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < _laneConfig.LaneCount; i++)
            {
                float x = _laneConfig.GetLanePosition(i);
                Gizmos.DrawWireSphere(new Vector3(spawnX + x, spawnY, spawnZ), 0.5f);
            }
        }
    }
    
    #endregion
}

/// <summary>
/// Serializable configuration for enemy row patterns
/// </summary>
[System.Serializable]
public class RunnerEnemyRowConfig
{
    [Tooltip("Which lanes have enemies")]
    public bool[] lanesWithEnemies = new bool[3];
    
    [Tooltip("Optional enemy prefab override")]
    public GameObject enemyPrefab;
    
    [Tooltip("Delay after this row")]
    public float delayAfterRow = 1f;
    
    public RunnerEnemyRow ToRow()
    {
        var row = new RunnerEnemyRow(lanesWithEnemies.Length);
        row.lanesWithEnemies = lanesWithEnemies;
        row.enemyPrefabOverride = enemyPrefab;
        row.delayAfterRow = delayAfterRow;
        return row;
    }
}
