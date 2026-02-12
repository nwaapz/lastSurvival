using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object pool for runner projectiles that handles multiple prefab types.
/// Each prefab type has its own separate pool to prevent mixing bullet types.
/// </summary>
public class RunnerProjectilePool : MonoBehaviour
{
    public static RunnerProjectilePool Instance { get; private set; }

    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private float projectileLifetime = 3f;
    [Tooltip("How many targets a projectile can hit before being destroyed. 1 = destroy on first hit.")]
    [SerializeField] private int maxPenetration = 1;
    
    public float ProjectileLifetime => projectileLifetime;
    public int MaxPenetration => maxPenetration;

    // Dictionary to store separate pools for each prefab type
    // Key = prefab instance ID, Value = list of pooled projectiles for that prefab
    private Dictionary<int, List<RunnerProjectile>> _pools = new Dictionary<int, List<RunnerProjectile>>();
    
    // Track which prefab each projectile came from
    private Dictionary<RunnerProjectile, int> _projectilePrefabMap = new Dictionary<RunnerProjectile, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Get a projectile from the pool or create a new one.
    /// Only returns projectiles that match the requested prefab type.
    /// </summary>
    /// <param name="position">Spawn position</param>
    /// <param name="rotation">Spawn rotation</param>
    /// <param name="prefab">Prefab to instantiate if pool is empty</param>
    /// <returns>The RunnerProjectile component</returns>
    public RunnerProjectile GetProjectile(Vector3 position, Quaternion rotation, GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("[RunnerProjectilePool] Cannot get projectile: Prefab is null!");
            return null;
        }
        
        int prefabId = prefab.GetInstanceID();
        
        // Ensure pool exists for this prefab type
        if (!_pools.ContainsKey(prefabId))
        {
            _pools[prefabId] = new List<RunnerProjectile>();
        }
        
        List<RunnerProjectile> pool = _pools[prefabId];
        
        // Search for inactive object in this prefab's pool
        foreach (var item in pool)
        {
            if (item != null && !item.gameObject.activeInHierarchy)
            {
                item.transform.position = position;
                item.transform.rotation = rotation;
                // Don't activate yet - let caller set damage first
                return item;
            }
        }

        // Create new if none found in this prefab's pool
        GameObject newItemObj = Instantiate(prefab, position, rotation);
        newItemObj.transform.SetParent(transform); // Keep hierarchy clean
        newItemObj.SetActive(false); // Deactivate initially
        
        RunnerProjectile projectile = newItemObj.GetComponent<RunnerProjectile>();
        if (projectile != null)
        {
            pool.Add(projectile);
            _projectilePrefabMap[projectile] = prefabId;
            // Don't activate yet - let caller set damage first
            return projectile;
        }
        else
        {
            Debug.LogError("[RunnerProjectilePool] Prefab does not have RunnerProjectile component!");
            Destroy(newItemObj);
            return null;
        }
    }
    
    /// <summary>
    /// Get the total count of pooled projectiles across all prefab types.
    /// </summary>
    public int TotalPooledCount
    {
        get
        {
            int count = 0;
            foreach (var pool in _pools.Values)
            {
                count += pool.Count;
            }
            return count;
        }
    }
    
    /// <summary>
    /// Get the count of pooled projectiles for a specific prefab type.
    /// </summary>
    public int GetPoolCountForPrefab(GameObject prefab)
    {
        if (prefab == null) return 0;
        
        int prefabId = prefab.GetInstanceID();
        if (_pools.ContainsKey(prefabId))
        {
            return _pools[prefabId].Count;
        }
        return 0;
    }
}
