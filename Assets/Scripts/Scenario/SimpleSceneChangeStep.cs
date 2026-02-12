using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simple step that immediately loads a scene. No dialogue, no waiting.
/// Completes once the target scene is loaded.
/// </summary>
[CreateAssetMenu(fileName = "SimpleSceneChangeStep", menuName = "Scenario/Simple Scene Change Step")]
public class SimpleSceneChangeStep : ScenarioStep
{
    [Header("Scene")]
    [Tooltip("Scene to load")]
    [SceneName(false)]
    public string targetScene;
    
    private bool _sceneLoaded = false;

    public override void OnEnter()
    {
        _sceneLoaded = false;
        
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError("[SimpleSceneChangeStep] Target scene is empty!");
            _sceneLoaded = true; // Complete with error
            return;
        }
        
        // Check if we're already in the target scene
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == targetScene)
        {
            Debug.Log($"[SimpleSceneChangeStep] Already in target scene: {targetScene}. Step complete.");
            _sceneLoaded = true;
            return;
        }
        
        Debug.Log($"[SimpleSceneChangeStep] Loading scene: {targetScene}");
        
        // Notify ScenarioManager to persist
        if (ScenarioManager.Instance != null)
        {
            ScenarioManager.Instance.PrepareForSceneTransition();
        }
        
        SceneManager.LoadScene(targetScene);
        // Step will complete after scene loads via ScenarioManager.ResumeStepAfterDelay
    }

    public override bool UpdateStep()
    {
        // Complete once we're in the target scene
        if (!_sceneLoaded)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            _sceneLoaded = (currentScene == targetScene);
            
            if (_sceneLoaded)
            {
                Debug.Log($"[SimpleSceneChangeStep] Target scene '{targetScene}' loaded. Step complete.");
            }
        }
        return _sceneLoaded;
    }

    public override void OnExit() { }
}
