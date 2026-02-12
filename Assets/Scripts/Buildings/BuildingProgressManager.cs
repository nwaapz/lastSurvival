using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages building progress across the game.
/// Integrates with SaveManager for persistence.
/// Register with ServiceLocator as IService.
/// </summary>
public class BuildingProgressManager : SingletonMono<BuildingProgressManager>, IService
{
    [Header("Configuration")]
    [Tooltip("All available building definitions in the game")]
    public List<BuildingDefinition> AllBuildings = new List<BuildingDefinition>();
    
    // Runtime lookup: buildingId -> current level
    private Dictionary<string, int> _buildingLevels = new Dictionary<string, int>();
    
    /// <summary>
    /// Fired when a building is upgraded. Parameters: buildingId, newLevel
    /// </summary>
    public event Action<string, int> OnBuildingUpgraded;

    /// <summary>
    /// Fired when building progress is loaded from save
    /// </summary>
    public event Action OnProgressLoaded;
    
    protected override void Awake()
    {
        base.Awake();
        
        // Self-register with ServiceLocator
        if (ServiceLocator.HasInstance)
        {
            ServiceLocator.Instance.Register<BuildingProgressManager>(this);
        }
    }
    
    protected override void OnDestroy()
    {
        if (ServiceLocator.HasInstance)
        {
            ServiceLocator.Instance.Unregister<BuildingProgressManager>();
        }
        base.OnDestroy();
    }

    public void Init()
    {
        LoadFromSave();
        OnProgressLoaded?.Invoke();
        Debug.Log($"[BuildingProgressManager] Initialized with {_buildingLevels.Count} buildings.");
    }
    
    /// <summary>
    /// Load building progress from SaveManager
    /// </summary>
    private void LoadFromSave()
    {
        _buildingLevels.Clear();
        
        if (SaveManager.Instance == null || SaveManager.Instance.Data == null)
        {
            Debug.LogWarning("[BuildingProgressManager] SaveManager not available. Using defaults.");
            InitializeDefaults();
            return;
        }
        
        var savedLevels = SaveManager.Instance.Data.BuildingLevels;
        
        // Load saved levels
        foreach (var entry in savedLevels)
        {
            _buildingLevels[entry.BuildingId] = entry.Level;
            Debug.Log($"[BuildingProgressManager] Loaded building '{entry.BuildingId}' at level {entry.Level}");
        }
        
        // Ensure all defined buildings have an entry (default to 0 = not built)
        foreach (var building in AllBuildings)
        {
            if (!_buildingLevels.ContainsKey(building.Id))
            {
                _buildingLevels[building.Id] = 0;
                Debug.Log($"[BuildingProgressManager] New building detected '{building.Id}', defaulting to level 0");
            }
        }
    }
    
    /// <summary>
    /// Initialize all buildings to level 0 (not built)
    /// </summary>
    private void InitializeDefaults()
    {
        foreach (var building in AllBuildings)
        {
            _buildingLevels[building.Id] = 0;
        }
    }
    
    /// <summary>
    /// Save current building progress to SaveManager
    /// </summary>
    private void SaveToSave()
    {
        if (SaveManager.Instance == null || SaveManager.Instance.Data == null)
        {
            Debug.LogWarning("[BuildingProgressManager] SaveManager not available. Cannot save.");
            return;
        }
        
        var saveList = SaveManager.Instance.Data.BuildingLevels;
        saveList.Clear();
        
        foreach (var kvp in _buildingLevels)
        {
            saveList.Add(new BuildingSaveEntry
            {
                BuildingId = kvp.Key,
                Level = kvp.Value
            });
        }
        
        SaveManager.Instance.SaveGame();
    }
    
    /// <summary>
    /// Get the current level of a building by ID
    /// </summary>
    public int GetLevel(string buildingId)
    {
        if (_buildingLevels.TryGetValue(buildingId, out int level))
        {
            return level;
        }
        return 0; // Not built
    }
    
    /// <summary>
    /// Get the current level of a building by definition
    /// </summary>
    public int GetLevel(BuildingDefinition building)
    {
        return GetLevel(building.Id);
    }
    
    /// <summary>
    /// Get the sprite for a building's current level
    /// </summary>
    public Sprite GetCurrentSprite(BuildingDefinition building)
    {
        int level = GetLevel(building.Id);
        return building.GetSpriteForLevel(level);
    }
    
    /// <summary>
    /// Check if a building can be upgraded
    /// </summary>
    public bool CanUpgrade(string buildingId)
    {
        var building = GetBuildingDefinition(buildingId);
        if (building == null) return false;
        
        int currentLevel = GetLevel(buildingId);
        return currentLevel < building.MaxLevel;
    }
    
    /// <summary>
    /// Check if a building can be upgraded
    /// </summary>
    public bool CanUpgrade(BuildingDefinition building)
    {
        return CanUpgrade(building.Id);
    }
    
    /// <summary>
    /// Upgrade a building by one level
    /// </summary>
    /// <returns>True if upgrade succeeded, false if already at max level</returns>
    public bool UpgradeBuilding(string buildingId)
    {
        var building = GetBuildingDefinition(buildingId);
        if (building == null)
        {
            Debug.LogWarning($"[BuildingProgressManager] Building '{buildingId}' not found.");
            return false;
        }
        
        int currentLevel = GetLevel(buildingId);
        if (currentLevel >= building.MaxLevel)
        {
            Debug.Log($"[BuildingProgressManager] Building '{buildingId}' already at max level {building.MaxLevel}.");
            return false;
        }
        
        int newLevel = currentLevel + 1;
        _buildingLevels[buildingId] = newLevel;
        SaveToSave();
        
        // Notify listeners
        OnBuildingUpgraded?.Invoke(buildingId, newLevel);
        
        Debug.Log($"[BuildingProgressManager] Upgraded '{buildingId}' to level {newLevel}.");
        return true;
    }
    
    /// <summary>
    /// Upgrade a building by one level
    /// </summary>
    public bool UpgradeBuilding(BuildingDefinition building)
    {
        return UpgradeBuilding(building.Id);
    }
    
    /// <summary>
    /// Set a building to a specific level (useful for debugging or special cases)
    /// </summary>
    public void SetLevel(string buildingId, int level)
    {
        var building = GetBuildingDefinition(buildingId);
        int maxLevel = building != null ? building.MaxLevel : 5;
        
        _buildingLevels[buildingId] = Mathf.Clamp(level, 1, maxLevel);
        SaveToSave();
    }
    
    /// <summary>
    /// Get a BuildingDefinition by ID
    /// </summary>
    public BuildingDefinition GetBuildingDefinition(string buildingId)
    {
        return AllBuildings.Find(b => b.Id == buildingId);
    }
    
    /// <summary>
    /// Reset all buildings to level 1
    /// </summary>
    public void ResetAllBuildings()
    {
        InitializeDefaults();
        SaveToSave();
        Debug.Log("[BuildingProgressManager] All buildings reset to level 0.");
    }
}
