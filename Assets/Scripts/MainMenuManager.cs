using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the main menu scene. Reads scenario config to determine
/// which scene to load when continuing the game.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Scenario Configuration")]
    [Tooltip("Reference to the game scenario config")]
    [SerializeField] private GameScenarioConfig gameScenario;
    
    [Header("Fallback Scene")]
    [Tooltip("Scene to load if scenario config is missing or invalid")]
    [SceneName]
    [SerializeField] private string fallbackSceneName = "basebuilder";
    
    [Header("Scenario Manager")]
    [Tooltip("Prefab of ScenarioManager to instantiate if not already present")]
    [SerializeField] private GameObject scenarioManagerPrefab;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    
    private void Start()
    {
        // Ensure SaveManager is initialized
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.Init();
            
            if (showDebugInfo)
            {
                LogGameProgress();
            }
        }
        
        // Ensure ScenarioManager exists and will persist
        EnsureScenarioManagerExists();
        
        // Auto-start the game after a short delay
        StartCoroutine(StartTheGame());
    }
    
    private void EnsureScenarioManagerExists()
    {
        if (ScenarioManager.HasInstance)
        {
            Debug.Log("[MainMenuManager] ScenarioManager already exists.");
            return;
        }
        
        // Try to instantiate from prefab
        if (scenarioManagerPrefab != null)
        {
            Debug.Log("[MainMenuManager] Instantiating ScenarioManager from prefab...");
            Instantiate(scenarioManagerPrefab);
        }
        else
        {
            Debug.LogWarning("[MainMenuManager] No ScenarioManager prefab assigned. Scenario system may not work correctly.");
        }
    }
    
    /// <summary>
    /// Called when the Play button is clicked.
    /// Continues the scenario from where the player left off.
    /// </summary>
    public void StartPlay()
    {
        // Restore squad configuration from the scenario
        RestoreSquadConfiguration();

        string targetScene = GetSceneForCurrentStep();
        
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning($"[MainMenuManager] Could not determine scene. Using fallback: {fallbackSceneName}");
            targetScene = fallbackSceneName;
        }
        
        Debug.Log($"[MainMenuManager] Continuing game - Loading scene: {targetScene}");
        
        // Just load the scene - ScenarioManager will auto-start there
        SceneManager.LoadScene(targetScene);
    }
    
    /// <summary>
    /// Restores the squad configuration for the current level progress.
    /// Finds the most recent ConfigureSquadStep and executes it.
    /// </summary>
    private void RestoreSquadConfiguration()
    {
        if (SaveManager.Instance == null || SaveManager.Instance.Data == null) return;
        
        var data = SaveManager.Instance.Data;
        int currentLevel = data.CurrentLevel;
        int currentStepIndex = data.CurrentScenarioStepIndex;
        
        if (gameScenario == null) return;
        
        LevelScenarioConfig levelConfig = gameScenario.GetLevel(currentLevel);
        if (levelConfig == null || levelConfig.steps == null) return;
        
        // Find the last ConfigureSquadStep up to currentStepIndex
        ConfigureSquadStep lastConfigStep = null;
        
        // We look up to the current step index. 
        // If we are partly through a level, we want the config that applies to this section.
        int maxIndex = Mathf.Min(currentStepIndex, levelConfig.steps.Count - 1);
        
        for (int i = 0; i <= maxIndex; i++)
        {
            if (levelConfig.steps[i] is ConfigureSquadStep configStep)
            {
                lastConfigStep = configStep;
            }
        }
        
        if (lastConfigStep != null)
        {
            Debug.Log($"[MainMenuManager] Restoring squad config from step: {lastConfigStep.name}");
            // Re-apply the configuration to SquadConfigHolder (simulating the step running)
            lastConfigStep.OnEnter();
        }
        else
        {
            Debug.Log($"[MainMenuManager] No pre-configured squad found for Level {currentLevel} up to step {currentStepIndex}");
        }
    }



    IEnumerator StartTheGame()
    {
               yield return new WaitForSeconds(2f);
        StartPlay();
    }
    
    /// <summary>
    /// Gets the scene where the current step should execute.
    /// Looks at the current step and finds the most recent scene transition.
    /// </summary>
    private string GetSceneForCurrentStep()
    {
        if (gameScenario == null)
        {
            Debug.LogWarning("[MainMenuManager] No GameScenarioConfig assigned!");
            return null;
        }
        
        if (SaveManager.Instance == null || SaveManager.Instance.Data == null)
        {
            // New game - use first level's starting scene
            return GetStartingSceneForLevel(1);
        }
        
        var data = SaveManager.Instance.Data;
        int currentLevel = data.CurrentLevel;
        int currentStepIndex = data.CurrentScenarioStepIndex;
        
        LevelScenarioConfig levelConfig = gameScenario.GetLevel(currentLevel);
        if (levelConfig == null)
        {
            Debug.LogWarning($"[MainMenuManager] No level config for level {currentLevel}");
            return null;
        }
        
        // If scenario completed, check for next scene
        if (data.ScenarioCompleted)
        {
            if (!string.IsNullOrEmpty(levelConfig.nextSceneOnComplete))
            {
                return levelConfig.nextSceneOnComplete;
            }
            // Otherwise use starting scene (player might replay)
            return levelConfig.GetStartingScene();
        }
        
        // Find the scene for the current step by looking backwards for the last scene change
        return FindSceneForStep(levelConfig, currentStepIndex);
    }
    
    /// <summary>
    /// Finds which scene the player should be in for a given step.
    /// First checks the step's activeScene, then looks backwards for scene transitions.
    /// </summary>
    private string FindSceneForStep(LevelScenarioConfig levelConfig, int stepIndex)
    {
        if (levelConfig.steps == null || levelConfig.steps.Count == 0)
        {
            Debug.Log($"[MainMenuManager] No steps in level config, using starting scene");
            return levelConfig.GetStartingScene();
        }
        
        Debug.Log($"[MainMenuManager] FindSceneForStep: stepIndex={stepIndex}, totalSteps={levelConfig.steps.Count}");
        
        // Clamp step index
        int safeIndex = Mathf.Min(stepIndex, levelConfig.steps.Count - 1);
        if (safeIndex < 0) safeIndex = 0;
        
        // First, get the current step
        var currentStep = levelConfig.steps[safeIndex];
        Debug.Log($"[MainMenuManager] Current step: {currentStep.name}, activeScene: '{currentStep.activeScene}'");
        
        // IMPORTANT: Check if current step IS a scene-changing step FIRST - use its TARGET scene
        // (not its activeScene, which is where it runs FROM, not where it goes TO)
        if (currentStep is SceneTransitionStep sceneTransition)
        {
            Debug.Log($"[MainMenuManager] Current step is SceneTransitionStep, using target: {sceneTransition.GetTargetSceneName()}");
            return sceneTransition.GetTargetSceneName();
        }
        if (currentStep is SimpleSceneChangeStep simpleChange)
        {
            Debug.Log($"[MainMenuManager] Current step is SimpleSceneChangeStep, using target: {simpleChange.targetScene}");
            return simpleChange.targetScene;
        }
        if (currentStep is GoToFightStep goToFight)
        {
            Debug.Log($"[MainMenuManager] Current step is GoToFightStep, using target: {goToFight.fightSceneName}");
            return goToFight.fightSceneName;
        }
        
        // For non-scene-changing steps, use their activeScene if defined
        if (!string.IsNullOrEmpty(currentStep.activeScene))
        {
            Debug.Log($"[MainMenuManager] Using current step's activeScene: {currentStep.activeScene}");
            return currentStep.activeScene;
        }
        
        // SPECIAL CASE: If current step expects NO scene, check if the NEXT step is an immediate scene change.
        // This fixes the "Setup Step -> Scene Change" flash issue.
        if (stepIndex + 1 < levelConfig.steps.Count)
        {
            var nextStep = levelConfig.steps[stepIndex + 1];
            if (nextStep is SimpleSceneChangeStep nextSimple)
            {
                Debug.Log($"[MainMenuManager] Current step agnostic, Next step is SimpleSceneChangeStep -> Using target: {nextSimple.targetScene}");
                return nextSimple.targetScene;
            }
            if (nextStep is SceneTransitionStep nextTrans)
            {
                Debug.Log($"[MainMenuManager] Current step agnostic, Next step is SceneTransitionStep -> Using target: {nextTrans.GetTargetSceneName()}");
                return nextTrans.GetTargetSceneName();
            }
        }
        
        // Look backwards from current step to find a step with activeScene or scene transition
        for (int i = safeIndex - 1; i >= 0; i--)
        {
            var step = levelConfig.steps[i];
            Debug.Log($"[MainMenuManager] Checking step {i}: {step.name}, activeScene: '{step.activeScene}'");
            
            // Check if step has activeScene defined
            if (!string.IsNullOrEmpty(step.activeScene))
            {
                Debug.Log($"[MainMenuManager] Found activeScene at step {i}: {step.activeScene}");
                return step.activeScene;
            }
            
            // Fallback: Check for scene transition steps
            if (step is SceneTransitionStep sceneStep)
            {
                Debug.Log($"[MainMenuManager] Found SceneTransitionStep at step {i}: {sceneStep.GetTargetSceneName()}");
                return sceneStep.GetTargetSceneName();
            }
            if (step is SimpleSceneChangeStep simpleStep)
            {
                Debug.Log($"[MainMenuManager] Found SimpleSceneChangeStep at step {i}: {simpleStep.targetScene}");
                return simpleStep.targetScene;
            }
            if (step is GoToFightStep fightStep)
            {
                Debug.Log($"[MainMenuManager] Found GoToFightStep at step {i}: {fightStep.fightSceneName}");
                return fightStep.fightSceneName;
            }
        }
        
        // No scene found - use level's starting scene
        string startingScene = levelConfig.GetStartingScene();
        Debug.Log($"[MainMenuManager] No scene found in steps, using level starting scene: {startingScene}");
        return startingScene;
    }
    
    /// <summary>
    /// Gets the starting scene for a specific level.
    /// </summary>
    private string GetStartingSceneForLevel(int levelNumber)
    {
        if (gameScenario == null) return null;
        
        LevelScenarioConfig levelConfig = gameScenario.GetLevel(levelNumber);
        return levelConfig?.GetStartingScene();
    }
    
    /// <summary>
    /// Starts a new game, resetting all progress.
    /// </summary>
    public void OnNewGameButtonClicked()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.DeleteSave();
            Debug.Log("[MainMenuManager] Save data deleted. Starting fresh.");
        }
        
        string startScene = GetStartingSceneForLevel(1) ?? fallbackSceneName;
        Debug.Log($"[MainMenuManager] New game - Loading scene: {startScene}");
        SceneManager.LoadScene(startScene);
    }
    
    /// <summary>
    /// Checks if there is existing save data.
    /// </summary>
    public bool HasSaveData()
    {
        if (SaveManager.Instance == null) return false;
        
        var data = SaveManager.Instance.Data;
        // Consider it "has save data" if player has progressed beyond initial state
        return data != null && (data.CurrentLevel > 1 || data.CurrentScenarioStepIndex > 0 || data.Coins > 0);
    }
    
    /// <summary>
    /// Gets the current level number for UI display.
    /// </summary>
    public int GetCurrentLevel()
    {
        if (SaveManager.Instance == null || SaveManager.Instance.Data == null)
            return 1;
        
        return SaveManager.Instance.Data.CurrentLevel;
    }
    
    /// <summary>
    /// Gets a progress summary string for UI display.
    /// </summary>
    public string GetProgressSummary()
    {
        if (SaveManager.Instance == null || SaveManager.Instance.Data == null)
            return "New Game";
        
        var data = SaveManager.Instance.Data;
        return $"Level {data.CurrentLevel} - Step {data.CurrentScenarioStepIndex + 1}";
    }
    
    private void LogGameProgress()
    {
        if (SaveManager.Instance == null || SaveManager.Instance.Data == null)
        {
            Debug.Log("[MainMenuManager] === No Save Data ===");
            return;
        }
        
        var data = SaveManager.Instance.Data;
        Debug.Log($"[MainMenuManager] === Game Progress ===\n" +
                  $"  Current Level: {data.CurrentLevel}\n" +
                  $"  Scenario Step: {data.CurrentScenarioStepIndex}\n" +
                  $"  Scenario Completed: {data.ScenarioCompleted}\n" +
                  $"  Coins: {data.Coins}\n" +
                  $"  High Score: {data.HighScore}\n" +
                  $"  Target Scene: {GetSceneForCurrentStep()}");
    }
    
    /// <summary>
    /// Quits the application.
    /// </summary>
    public void OnQuitButtonClicked()
    {
        Debug.Log("[MainMenuManager] Quit requested.");
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
