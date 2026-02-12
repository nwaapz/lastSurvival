using System;
using UnityEngine;

public class BarrelManager : SingletonMono<BarrelManager>, IService
{
    public const int MAX_BARRELS = 1;

    [Header("Barrel Settings")]
    [SerializeField] private GameObject barrelPrefab;
    [Tooltip("Reference transform for barrel spawn position and rotation.")]
    [SerializeField] private Transform referenceBarrel;

    // Track how many barrels have been used this game (resets each session)
    private int _barrelsUsed;

    /// <summary>
    /// Fired when barrel count changes. Parameters: (barrelsUsed, maxBarrels)
    /// </summary>
    public event Action<int, int> OnBarrelCountChanged;

    public int BarrelsUsed => _barrelsUsed;
    public int BarrelsRemaining => MAX_BARRELS - _barrelsUsed;
    public bool CanSpawnBarrel => _barrelsUsed < MAX_BARRELS;

    protected override void Awake()
    {
        base.Awake();
        
        // Self-register with ServiceLocator
        if (ServiceLocator.HasInstance)
        {
            ServiceLocator.Instance.Register<BarrelManager>(this);
        }
    }
    
    protected override void OnDestroy()
    {
        if (ServiceLocator.HasInstance)
        {
            ServiceLocator.Instance.Unregister<BarrelManager>();
        }
        base.OnDestroy();
    }

    public void Init()
    {
        // Reset barrel count at the start of each game session
        _barrelsUsed = 0;
        OnBarrelCountChanged?.Invoke(_barrelsUsed, MAX_BARRELS);
        Debug.Log("[BarrelManager] Barrel count reset. Available: " + BarrelsRemaining);
    }

    /// <summary>
    /// Called from UI button to spawn a barrel.
    /// Deducts cost from economy before spawning.
    /// </summary>
    public void SpawnBarrel()
    {
        if (!CanSpawnBarrel)
        {
            Debug.Log("[BarrelManager] No barrels remaining (max capacity reached).");
            return;
        }

        if (barrelPrefab == null)
        {
            Debug.LogWarning("[BarrelManager] No barrel prefab assigned.");
            return;
        }

        // Get cost from Costmanager
        int cost = 0;
        if (ServiceLocator.Instance != null)
        {
            var costManager = ServiceLocator.Instance.Get<Costmanager>();
            if (costManager != null)
            {
                cost = costManager.GetBarrelCost();
            }
        }

        // Try to spend money
        if (EconomyManager.Instance != null)
        {
            if (!EconomyManager.Instance.SpendMoney(cost))
            {
                Debug.Log("[BarrelManager] Not enough money to spawn barrel.");
                return;
            }
        }

        // Increment used count
        _barrelsUsed++;
        OnBarrelCountChanged?.Invoke(_barrelsUsed, MAX_BARRELS);

        // Spawn the barrel using reference barrel's position and rotation
        Vector3 spawnPos = referenceBarrel != null ? referenceBarrel.position : Vector3.zero;
        Quaternion spawnRot = referenceBarrel != null ? referenceBarrel.rotation : Quaternion.identity;
        
        GameObject barrel = Instantiate(barrelPrefab, spawnPos, spawnRot);
        Debug.Log($"[BarrelManager] Spawned barrel at {spawnPos}. Cost: {cost}. Remaining: {BarrelsRemaining}");
    }

    /// <summary>
    /// Returns formatted string for UI display (e.g., "0/1")
    /// </summary>
    public string GetDisplayText()
    {
        return $"{_barrelsUsed}/{MAX_BARRELS}";
    }
}
