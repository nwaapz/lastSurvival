using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object pool for particle systems.
/// Reuses particle effects instead of instantiating/destroying them repeatedly.
/// </summary>
public class RunnerParticlePool : MonoBehaviour
{
    public static RunnerParticlePool Instance { get; private set; }
    
    [Header("Settings")]
    [SerializeField] private int initialPoolSize = 10;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    // Pool per prefab type
    private Dictionary<ParticleSystem, List<ParticleSystem>> _pools = new Dictionary<ParticleSystem, List<ParticleSystem>>();
    
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
    /// Get a particle system from the pool or create a new one.
    /// The particle will automatically return to pool when it finishes playing.
    /// </summary>
    public ParticleSystem GetParticle(ParticleSystem prefab, Vector3 position)
    {
        if (prefab == null)
        {
            Debug.LogError("[RunnerParticlePool] Prefab is null!");
            return null;
        }
        
        // Get or create pool for this prefab type
        if (!_pools.ContainsKey(prefab))
        {
            _pools[prefab] = new List<ParticleSystem>();
        }
        
        List<ParticleSystem> pool = _pools[prefab];
        
        // Find inactive particle in pool
        foreach (var particle in pool)
        {
            if (particle != null && !particle.gameObject.activeInHierarchy)
            {
                particle.transform.position = position;
                particle.gameObject.SetActive(true);
                particle.Play();
                
                if (showDebugLogs)
                {
                    Debug.Log($"[RunnerParticlePool] Reused particle from pool. Pool size: {pool.Count}");
                }
                
                return particle;
            }
        }
        
        // Create new particle if none available
        ParticleSystem newParticle = Instantiate(prefab, position, Quaternion.identity, transform);
        
        // Add auto-return component
        var returner = newParticle.gameObject.AddComponent<ParticlePoolReturner>();
        returner.Initialize(newParticle);
        
        pool.Add(newParticle);
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerParticlePool] Created new particle. Pool size: {pool.Count}");
        }
        
        newParticle.Play();
        return newParticle;
    }
}

/// <summary>
/// Helper component that returns particle to pool when it stops playing.
/// </summary>
public class ParticlePoolReturner : MonoBehaviour
{
    private ParticleSystem _particleSystem;
    
    public void Initialize(ParticleSystem ps)
    {
        _particleSystem = ps;
    }
    
    private void Update()
    {
        // Return to pool when particle stops playing
        if (_particleSystem != null && !_particleSystem.isPlaying && gameObject.activeInHierarchy)
        {
            gameObject.SetActive(false);
        }
    }
}
