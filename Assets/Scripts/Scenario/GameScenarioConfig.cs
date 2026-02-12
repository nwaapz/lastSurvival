using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top-level scenario configuration containing all levels.
/// This is the "onion" root - Game contains Levels, Levels contain Steps.
/// 
/// Hierarchy:
///   GameScenarioConfig (1 per game)
///     └── LevelScenarioConfig (1 per level)
///           └── ScenarioStep (many per level - dialogue, spawn, move, etc.)
/// </summary>
[CreateAssetMenu(fileName = "GameScenario", menuName = "Scenario/Game Scenario Config")]
public class GameScenarioConfig : ScriptableObject
{
    [Header("Game Information")]
    [Tooltip("Name of this scenario/campaign")]
    public string scenarioName = "Main Campaign";
    
    [TextArea(2, 4)]
    [Tooltip("Description of this scenario")]
    public string description;
    
    [Header("Levels")]
    [Tooltip("All levels in order. Index 0 = Level 1, Index 1 = Level 2, etc.")]
    public List<LevelScenarioConfig> levels = new List<LevelScenarioConfig>();
    
    [Header("Settings")]
    [Tooltip("Starting level index (0-based). Usually 0.")]
    public int startingLevelIndex = 0;
    
    /// <summary>
    /// Get level config by level number (1-based)
    /// </summary>
    public LevelScenarioConfig GetLevel(int levelNumber)
    {
        int index = levelNumber - 1;
        if (index >= 0 && index < levels.Count)
        {
            return levels[index];
        }
        return null;
    }
    
    /// <summary>
    /// Get level config by index (0-based)
    /// </summary>
    public LevelScenarioConfig GetLevelByIndex(int index)
    {
        if (index >= 0 && index < levels.Count)
        {
            return levels[index];
        }
        return null;
    }
    
    /// <summary>
    /// Total number of levels
    /// </summary>
    public int LevelCount => levels.Count;
}
