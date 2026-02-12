using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton manager for spawning floating damage text efficiently using object pooling.
/// </summary>
public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private DamageText damageTextPrefab;
    [SerializeField] private int initialPoolSize = 20;
    
    [Header("Visuals")]
    [SerializeField] private Color defaultDamageColor = Color.white;
    [SerializeField] private Color criticalDamageColor = Color.red;
    [SerializeField] private Vector3 spawnOffset = new Vector3(0, 2f, 0); // Above entity head
    [Tooltip("Randomize horizontal position slightly to avoid overlapping")]
    [SerializeField] private float randomOffsetRange = 0.5f;

    private List<DamageText> _pool = new List<DamageText>();
    private Transform _poolContainer;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // Optional: keep across scenes if needed
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializePool();
    }

    private void InitializePool()
    {
        _poolContainer = new GameObject("DamageTextPool").transform;
        _poolContainer.SetParent(transform);

        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewPoolObject();
        }
    }

    private DamageText CreateNewPoolObject()
    {
        if (damageTextPrefab == null)
        {
            Debug.LogError("[DamageTextManager] Prefab is NULL!");
            return null;
        }

        DamageText obj = Instantiate(damageTextPrefab, _poolContainer);
        obj.gameObject.SetActive(false);
        _pool.Add(obj);
        return obj;
    }

    private DamageText GetFromPool()
    {
        foreach (var item in _pool)
        {
            if (!item.gameObject.activeInHierarchy)
            {
                return item;
            }
        }
        
        // Pool empty, create new
        return CreateNewPoolObject();
    }

    /// <summary>
    /// Show a floating damage number at the specified position
    /// </summary>
    /// <param name="damage">Amount of damage</param>
    /// <param name="worldPosition">Position of the entity hit</param>
    /// <param name="isCritical">If true, uses critical color</param>
    public void ShowDamage(float damage, Vector3 worldPosition, bool isCritical = false)
    {
        if (damageTextPrefab == null) return;

        DamageText textObj = GetFromPool();
        if (textObj == null) return;

        // Calculate spawn position with offset and randomness
        Vector3 randomOffset = new Vector3(
            Random.Range(-randomOffsetRange, randomOffsetRange),
            Random.Range(-randomOffsetRange * 0.5f, randomOffsetRange * 0.5f),
            0
        );
        
        textObj.transform.position = worldPosition + spawnOffset + randomOffset;
        
        // Determine color
        Color color = isCritical ? criticalDamageColor : defaultDamageColor;
        
        textObj.Initialize(damage, color);
    }
    
    // Static helper for easy access
    public static void SpawnDamageText(float damage, Vector3 position, bool isCritical = false)
    {
        if (Instance != null)
        {
            Instance.ShowDamage(damage, position, isCritical);
        }
    }
}
