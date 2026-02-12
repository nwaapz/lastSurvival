using UnityEngine;

/// <summary>
/// ScriptableObject defining a building type.
/// Create new building types via Assets > Create > Buildings > Building Definition
/// </summary>
[CreateAssetMenu(fileName = "NewBuilding", menuName = "Buildings/Building Definition")]
public class BuildingDefinition : ScriptableObject
{
    [Header("Identification")]
    [Tooltip("Unique identifier for this building (used for save/load)")]
    public string Id;
    
    [Tooltip("Display name shown in UI")]
    public string DisplayName;
    
    [Header("Level Sprites")]
    [Tooltip("Sprites for each level (index 0 = level 1, index 4 = level 5)")]
    public Sprite[] LevelSprites = new Sprite[5];
    
    [Header("Settings")]
    [Tooltip("Maximum level this building can reach")]
    public int MaxLevel = 5;
    
    /// <summary>
    /// Get the sprite for a specific level (1-based)
    /// </summary>
    public Sprite GetSpriteForLevel(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, LevelSprites.Length - 1);
        return LevelSprites[index];
    }
    
    private void OnValidate()
    {
        // Auto-generate ID from asset name if empty
        if (string.IsNullOrEmpty(Id))
        {
            Id = name.ToLower().Replace(" ", "_");
        }
        
        // Ensure array is correct size
        if (LevelSprites == null || LevelSprites.Length != MaxLevel)
        {
            System.Array.Resize(ref LevelSprites, MaxLevel);
        }
    }
}
