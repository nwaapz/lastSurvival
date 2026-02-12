using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class ScenarioStep : ScriptableObject
{
    [Header("Base Step Settings")]
    [Tooltip("Description for debugging/editor")]
    public string description;
    
    [Tooltip("The scene this step should execute in. If current scene differs, will auto-transition. Leave empty to run in any scene.")]
    [SceneName]
    public string activeScene;
    
    [Tooltip("If true, this step will prevent proceeding until it reports complete.")]
    public bool isBlocking = true;
    
    [Tooltip("If true, progress will be saved when this step completes. Use for important checkpoints.")]
    public bool saveOnComplete = true;

    /// <summary>
    /// Checks if we're in the correct scene for this step.
    /// </summary>
    public bool IsInCorrectScene()
    {
        if (string.IsNullOrEmpty(activeScene))
            return true; // No scene requirement
            
        return SceneManager.GetActiveScene().name == activeScene;
    }
    
    /// <summary>
    /// Gets the required scene for this step, or null if any scene is fine.
    /// </summary>
    public string GetRequiredScene()
    {
        return string.IsNullOrEmpty(activeScene) ? null : activeScene;
    }

    /// <summary>
    /// Called when the step starts.
    /// </summary>
    public virtual void OnEnter() { }

    /// <summary>
    /// Called every frame while the step is active.
    /// Returns true if the step is complete.
    /// </summary>
    public virtual bool UpdateStep() 
    { 
        return true; // By default, non-blocking steps finish immediately
    }

    /// <summary>
    /// Called when the step is finished or skipped.
    /// </summary>
    public virtual void OnExit() { }
}
