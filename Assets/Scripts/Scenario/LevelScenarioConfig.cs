using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines the scenario (story sequence) for a specific level.
/// Create one ScriptableObject per level with all the steps the player must complete.
/// </summary>
[CreateAssetMenu(fileName = "Level_Scenario", menuName = "Scenario/Level Scenario Config")]
public class LevelScenarioConfig : ScriptableObject
{
    [Header("Level Information")]
    [Tooltip("Which level this scenario is for (should match SaveManager.CurrentLevel)")]
    public int levelNumber = 1;
    
    [Header("Scenario Steps")]
    [Tooltip("Ordered list of steps for this level. Player completes them sequentially.")]
    public List<ScenarioStep> steps = new List<ScenarioStep>();
    
    [Header("Completion Settings")]
    [Tooltip("If true, completing this scenario advances to the next level")]
    public bool advanceLevelOnComplete = true;
    
    [Tooltip("Optional: Scene to load after scenario completes (leave empty to stay in current scene)")]
    [SceneName]
    public string nextSceneOnComplete;
    
    [Header("Starting Scene")]
    [Tooltip("The scene where this level starts. Used by MainMenuManager to determine which scene to load.")]
    [SceneName]
    public string startingSceneName;
    
    /// <summary>
    /// Gets the starting scene for this level.
    /// Falls back to checking SceneTransitionStep if not explicitly set.
    /// </summary>
    public string GetStartingScene()
    {
        // Use explicit starting scene if set
        if (!string.IsNullOrEmpty(startingSceneName))
        {
            return startingSceneName;
        }
        
        // Fallback: look for first SceneTransitionStep
        if (steps != null)
        {
            foreach (var step in steps)
            {
                if (step is SceneTransitionStep transitionStep)
                {
                    string sceneName = transitionStep.GetTargetSceneName();
                    if (!string.IsNullOrEmpty(sceneName))
                    {
                        return sceneName;
                    }
                }
            }
        }
        
        return null;
    }
}
