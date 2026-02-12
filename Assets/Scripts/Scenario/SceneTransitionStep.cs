using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "SceneTransitionStep", menuName = "Scenario/Scene Transition Step")]
public class SceneTransitionStep : ScenarioStep
{
    [Header("Scene Configuration")]
    [Tooltip("Name of the scene to load (must match scene name in Build Settings)")]
    [SceneName(false)] // Don't allow empty - scene is required
    public string targetSceneName;
    
    [Tooltip("If true, the step completes immediately after loading. If false, waits for manual completion.")]
    public bool autoComplete = true;
    
    [Header("Optional Narration Before Transition")]
    [Tooltip("Show dialogue before transitioning (leave empty to skip)")]
    public NarrationLine preTransitionDialogue;
    
    private bool _sceneLoaded = false;
    private bool _dialogueShown = false;

    public override void OnEnter()
    {
        _sceneLoaded = false;
        _dialogueShown = false;
        
        // Show optional dialogue first
        if (preTransitionDialogue != null)
        {
            if (Narration_manager.Instance != null)
            {
                Narration_manager.Instance.ShowNarrationLine(preTransitionDialogue);
            }
        }
        else
        {
            _dialogueShown = true; // Skip dialogue if none provided
        }
    }

    public override bool UpdateStep()
    {
        // Wait for dialogue to be dismissed (player click)
        if (!_dialogueShown)
        {
            if (Input.GetMouseButtonDown(0))
            {
                _dialogueShown = true;
            }
            return false;
        }
        
        // Load scene once dialogue is done
        if (!_sceneLoaded)
        {
            LoadScene();
            _sceneLoaded = true;
        }
        
        // If auto-complete, finish immediately after loading
        return autoComplete;
    }

    private void LoadScene()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("[SceneTransitionStep] Target scene name is empty!");
            return;
        }

        Debug.Log($"[SceneTransitionStep] Loading scene: {targetSceneName}");
        
        // Tell ScenarioManager to persist across scene load
        if (ScenarioManager.Instance != null)
        {
            ScenarioManager.Instance.PrepareForSceneTransition();
        }
        
        SceneManager.LoadScene(targetSceneName);
    }

    public override void OnExit()
    {
        // Cleanup if needed
    }
    
    /// <summary>
    /// Gets the target scene name for external queries.
    /// </summary>
    public string GetTargetSceneName()
    {
        return targetSceneName;
    }
}
