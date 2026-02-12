using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenarioManager : SingletonMono<ScenarioManager>
{
    [Header("Game Scenario (Top Level)")]
    [Tooltip("The main game scenario containing all levels")]
    [SerializeField] private GameScenarioConfig gameScenario;
    
    [Header("Legacy: Level Scenarios (Fallback)")]
    [Tooltip("All level scenario configs. Used if GameScenarioConfig is not assigned.")]
    [SerializeField] private List<LevelScenarioConfig> levelScenarios = new List<LevelScenarioConfig>();
    
    [Header("Settings")]
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool persistAcrossScenes = true;
    
    [Header("Level Loop Mode (for levels beyond scenario content)")]
    [Tooltip("If true, levels at or above the threshold will loop (increment level + reload scene) instead of running scenarios")]
    [SerializeField] private bool enableLevelLoop = true;
    
    [Tooltip("Level number at which loop mode begins (e.g., 8 means levels 8+ will loop)")]
    [SerializeField] private int levelLoopThreshold = 8;
    
    [Tooltip("Scene to reload when in loop mode. Leave empty to reload current scene.")]
    [SceneName]
    [SerializeField] private string loopSceneName;

    [Header("Debug - Current State")]
    [SerializeField] private int currentLevelNumber = -1;
    [SerializeField] private LevelScenarioConfig currentLevel;
    [SerializeField] private int currentStepIndex = -1;
    [SerializeField] private ScenarioStep currentStep;
    [SerializeField] private bool isRunning = false;
    
    // Renamed for clarity
    private LevelScenarioConfig currentScenario => currentLevel;
    
    // Events for UI integration
    public System.Action<ScenarioStep> OnStepChanged;
    public System.Action OnScenarioComplete;
    
    // Public accessors for UI
    public ScenarioStep CurrentStep => currentStep;
    public int CurrentStepIndex => currentStepIndex;
    public int TotalSteps => currentScenario != null ? currentScenario.steps.Count : 0;
    public int CurrentLevelNumber => currentLevelNumber;
    public int TotalLevels => gameScenario != null ? gameScenario.LevelCount : levelScenarios.Count;
    public GameScenarioConfig GameScenario => gameScenario;
    public LevelScenarioConfig CurrentLevel => currentLevel;
    public bool IsRunning => isRunning;
    
    // Level Loop Mode accessors
    public bool IsLevelLoopEnabled => enableLevelLoop;
    public int LevelLoopThreshold => levelLoopThreshold;
    /// <summary>
    /// True when we're in loop mode: level is at/above threshold AND no scenario is currently running.
    /// If a scenario exists for the level, it runs normally even if above threshold.
    /// </summary>
    public bool IsInLoopMode => enableLevelLoop && currentLevelNumber >= levelLoopThreshold && currentLevel == null && !isRunning;
    
    private bool _preparingForTransition = false;
    private bool _stepEntered = false; // Track if current step's OnEnter has been called
    private bool _waitOneFrameAfterEnter = false; // Prevent immediate completion after entering a step

    protected override void Awake()
    {
        base.Awake();
        
        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }
    }
    
    [Header("Scene Settings")]
    [Tooltip("Scene names where ScenarioManager should NOT auto-start (e.g., MainMenu)")]
    [SerializeField] private List<string> noAutoStartScenes = new List<string> { "MainMenu" };
    
    private void Start()
    {
        if (autoStart && ShouldAutoStartInCurrentScene())
        {
            StartCoroutine(WaitForServicesAndStart());
        }
    }
    
    private bool ShouldAutoStartInCurrentScene()
    {
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        
        // Don't auto-start in excluded scenes (like MainMenu)
        if (noAutoStartScenes.Contains(currentScene))
        {
            Debug.Log($"[ScenarioManager] In '{currentScene}' - skipping auto-start. Waiting for Play button.");
            return false;
        }
        
        return true;
    }
    
    private System.Collections.IEnumerator WaitForServicesAndStart()
    {
        // Wait for ServiceLocator
        while (ServiceLocator.Instance == null)
        {
            yield return null;
        }
        
        // Wait for critical services to be registered
        // We don't check for specific ones hardcoded to avoid dependency, 
        // but we wait a frame or two to let ServiceLocator.Init() finish.
        yield return new WaitForEndOfFrame();
        yield return null;
        
        // Optional: explicitly check for SaveManager since we need it immediately
        float timeout = 5f;
        float timer = 0f;
        while (SaveManager.Instance == null && timer < timeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        
        LoadAndStartCurrentLevelScenario();
    }
    
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[ScenarioManager] OnSceneLoaded: {scene.name}, isRunning={isRunning}, preparingForTransition={_preparingForTransition}");
        
        // Skip excluded scenes (like MainMenu)
        if (noAutoStartScenes.Contains(scene.name))
        {
            Debug.Log($"[ScenarioManager] Scene '{scene.name}' is in noAutoStartScenes. Not starting scenario.");
            return;
        }
        
        // If we were preparing for a transition (mid-scenario scene change), resume the step
        if (_preparingForTransition)
        {
            _preparingForTransition = false;
            Debug.Log($"[ScenarioManager] Scene loaded after transition: {scene.name}. Resuming scenario at step {currentStepIndex}.");
            
            if (isRunning && currentStep != null && currentStep.IsInCorrectScene())
            {
                // Wait a frame for services to initialize before starting the step
                StartCoroutine(ResumeStepAfterDelay());
            }
            return;
        }
        
        // If scenario IS running but we didn't prepare for transition (e.g., Retry button),
        // we need to re-enter the current step to refresh subscriptions
        if (isRunning && currentStep != null)
        {
            Debug.Log($"[ScenarioManager] Scene reloaded while running (likely Retry). Re-entering current step: {currentStep.name}");
            _stepEntered = false; // Reset so we can re-enter
            StartCoroutine(ResumeStepAfterDelay());
            return;
        }
        
        // If scenario not running yet, auto-start it
        if (!isRunning && autoStart)
        {
            Debug.Log($"[ScenarioManager] Auto-starting scenario in scene: {scene.name}");
            StartCoroutine(WaitForServicesAndStart());
        }
    }
    
    private IEnumerator ResumeStepAfterDelay()
    {
        // Wait for end of frame to let scene services initialize
        yield return new WaitForEndOfFrame();
        
        // Wait one more frame to be safe
        yield return null;
        
        // Wait for ServiceLocator to be available
        float timeout = 2f;
        float elapsed = 0f;
        while (ServiceLocator.Instance == null && elapsed < timeout)
        {
            yield return null;
            elapsed += Time.deltaTime;
        }
        
        if (currentStep != null && !_stepEntered)
        {
            Debug.Log($"[STEP_TRACE] ResumeStepAfterDelay: Entering step {currentStepIndex}: {currentStep.name}");
            currentStep.OnEnter();
            _stepEntered = true;
            _waitOneFrameAfterEnter = true; // Wait one frame before checking completion
            OnStepChanged?.Invoke(currentStep);
        }
    }

    /// <summary>
    /// Load and start the scenario for the current level from SaveManager
    /// </summary>
    public void LoadAndStartCurrentLevelScenario()
    {
        Debug.Log($"[ScenarioManager] LoadAndStartCurrentLevelScenario called. SaveManager.HasInstance={SaveManager.HasInstance}");
        
        if (!SaveManager.HasInstance || SaveManager.Instance == null)
        {
            Debug.LogError("[ScenarioManager] ❌ START FAILED: SaveManager instance is null! Ensure SaveManager is in the scene.");
            return;
        }

        if (SaveManager.Instance.Data == null)
        {
            Debug.LogError("[ScenarioManager] ❌ START FAILED: SaveManager.Data is null! Save data not loaded.");
            return;
        }
        
        int level = SaveManager.Instance.Data.CurrentLevel;
        Debug.Log($"[ScenarioManager] Attempting to load scenario for Level {level}...");
        LoadScenarioForLevel(level);
    }
    
    /// <summary>
    /// Load and start a specific level's scenario
    /// </summary>
    public void LoadScenarioForLevel(int levelNumber)
    {
        currentLevelNumber = levelNumber;
        
        // First, try to find the scenario config for this level
        currentLevel = GetScenarioForLevel(levelNumber);
        
        // Check if we should enter Loop Mode:
        // - Loop mode enabled
        // - Level is at or above threshold
        // - AND no scenario exists for this level (or scenario has no steps)
        bool noValidScenario = currentLevel == null || currentLevel.steps == null || currentLevel.steps.Count == 0;
        
        if (enableLevelLoop && levelNumber >= levelLoopThreshold && noValidScenario)
        {
            Debug.Log($"[ScenarioManager] 🔄 LOOP MODE: Level {levelNumber} is at/above threshold ({levelLoopThreshold}) and has no scenario. Waiting for continue button to loop.");
            
            // Clear scenario state but keep running to listen for continue
            currentLevel = null;
            currentStep = null;
            currentStepIndex = 0;
            isRunning = false; // Not running a scenario, just in loop mode
            
            // Reset scenario step index in save data for loop mode
            if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
            {
                SaveManager.Instance.Data.CurrentScenarioStepIndex = 0;
                SaveManager.Instance.Data.ScenarioCompleted = false;
            }
            
            return;
        }
        
        // Normal scenario loading - scenario exists
        if (currentLevel == null)
        {
            Debug.LogError($"[ScenarioManager] ❌ START FAILED: No scenario config found for Level {levelNumber}! Check GameScenarioConfig assignments.");
            return;
        }
        
        if (currentLevel.steps == null || currentLevel.steps.Count == 0)
        {
            Debug.LogError($"[ScenarioManager] ❌ START FAILED: Scenario for Level {levelNumber} has 0 steps! Add steps to the LevelScenarioConfig.");
            return;
        }
        
        Debug.Log($"[ScenarioManager] ✅ Found scenario for Level {levelNumber} with {currentLevel.steps.Count} steps. Running normally.");
        
        // Load saved step index or start from beginning
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            if (SaveManager.Instance.Data.ScenarioCompleted)
            {
                Debug.LogWarning($"[ScenarioManager] ⚠️ Scenario for Level {levelNumber} is marked as COMPLETED in save data. Skipping.");
                return;
            }
            currentStepIndex = SaveManager.Instance.Data.CurrentScenarioStepIndex;
        }
        else
        {
            currentStepIndex = 0;
        }
        
        // IMPORTANT: Re-run any ConfigureSquadStep steps that we're skipping past
        // This ensures squad configuration is always set, even when resuming from a later step
        for (int i = 0; i < currentStepIndex && i < currentLevel.steps.Count; i++)
        {
            var skippedStep = currentLevel.steps[i];
            if (skippedStep is ConfigureSquadStep configStep)
            {
                Debug.Log($"[ScenarioManager] 🔄 Re-running skipped ConfigureSquadStep at index {i} to restore squad config");
                configStep.OnEnter();
                // Don't call OnExit - this is just to restore config state
            }
        }
        
        isRunning = true;
        
        // Get the step we should resume at
        currentStep = currentLevel.steps[currentStepIndex];
        
        // Check if we're in the correct scene for this step
        if (!currentStep.IsInCorrectScene())
        {
            string requiredScene = currentStep.GetRequiredScene();
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Debug.Log($"[ScenarioManager] ⏸️ Step '{currentStep.name}' requires scene '{requiredScene}' but we're in '{currentScene}'. Waiting for correct scene...");
            // Don't start the step - wait for scene to be loaded by MainMenuManager or other caller
            return;
        }
        
        // We're in the correct scene - start the step
        Debug.Log($"[STEP_TRACE] LoadScenarioForLevel: Starting step {currentStepIndex}: {currentStep.name}");
        currentStep.OnEnter();
        _stepEntered = true;
        _waitOneFrameAfterEnter = true; // Wait one frame before checking completion
        OnStepChanged?.Invoke(currentStep);
    }
    
    private LevelScenarioConfig GetScenarioForLevel(int levelNumber)
    {
        // First, try to get from GameScenarioConfig (new hierarchy)
        if (gameScenario != null)
        {
            LevelScenarioConfig level = gameScenario.GetLevel(levelNumber);
            if (level != null)
            {
                return level;
            }
        }
        
        // Fallback: Try legacy levelScenarios list
        // Try to find by level number field
        foreach (var scenario in levelScenarios)
        {
            if (scenario != null && scenario.levelNumber == levelNumber)
            {
                return scenario;
            }
        }
        
        // Fallback: use array index (level 1 = index 0)
        int index = levelNumber - 1;
        if (index >= 0 && index < levelScenarios.Count)
        {
            return levelScenarios[index];
        }
        
        return null;
    }

    public void StopScenario()
    {
        if (currentStep != null)
        {
            currentStep.OnExit();
        }
        isRunning = false;
        currentStep = null;
    }

    private void Update()
    {
        if (!isRunning || currentStep == null) return;
        
        // Don't update step if OnEnter hasn't been called yet (waiting for scene transition)
        if (!_stepEntered) return;

        // Wait one frame after entering a step before checking completion
        // This prevents steps from completing immediately after scene transitions
        if (_waitOneFrameAfterEnter)
        {
            _waitOneFrameAfterEnter = false;
            return;
        }

        // Check if current step is complete
        if (currentStep.UpdateStep())
        {
            Debug.Log($"[STEP_TRACE] Update() detected step complete. Step {currentStepIndex}: {currentStep.name}");
            AdvanceStep();
        }
    }

    public void AdvanceStep()
    {
        // Log call stack to find who's calling AdvanceStep
        Debug.Log($"[STEP_TRACE] AdvanceStep() called. Current step: {currentStepIndex} -> {currentStepIndex + 1}. Stack:\n{UnityEngine.StackTraceUtility.ExtractStackTrace()}");
        
        // Store the completed step's saveOnComplete flag BEFORE incrementing
        bool shouldSaveProgress = currentStep != null && currentStep.saveOnComplete;
        
        // Exit current step
        if (currentStep != null)
        {
            Debug.Log($"[ScenarioManager] [StepFinished] Step {currentStepIndex}: {currentStep.name}");
            currentStep.OnExit();
        }

        currentStepIndex++;
        
        // Check if we are done with this scenario BEFORE saving progress
        // This prevents saving an invalid step index when completing a level
        if (currentScenario == null || currentStepIndex >= currentScenario.steps.Count)
        {
            Debug.Log($"[ScenarioManager] Scenario Complete for level {currentLevelNumber}!");
            OnScenarioCompleted();
            return;
        }
        
        // Save progress only if the COMPLETED step allows it (and we're not completing the level)
        if (shouldSaveProgress)
        {
            SaveStepProgress();
        }
        else
        {
            Debug.Log($"[ScenarioManager] Completed step has saveOnComplete=false, skipping save.");
        }

        // Get next step
        currentStep = currentScenario.steps[currentStepIndex];
        _stepEntered = false; // Reset flag for new step
        
        // Check if we need to change scenes first
        if (!currentStep.IsInCorrectScene())
        {
            string requiredScene = currentStep.GetRequiredScene();
            Debug.Log($"[ScenarioManager] Step '{currentStep.name}' requires scene '{requiredScene}'. Transitioning...");
            
            // Prepare for transition and load scene
            PrepareForSceneTransition();
            SceneManager.LoadScene(requiredScene);
            return; // Will resume after scene loads
        }
        
        // Enter the step
        Debug.Log($"[STEP_TRACE] AdvanceStep: Entering step {currentStepIndex}: {currentStep.name}");
        currentStep.OnEnter();
        _stepEntered = true;
        _waitOneFrameAfterEnter = true; // Wait one frame before checking completion
        
        // Notify listeners (for UI updates)
        OnStepChanged?.Invoke(currentStep);
    }
    
    private void OnScenarioCompleted()
    {
        isRunning = false;
        currentStep = null;
        currentStepIndex = 0; // Reset in-memory step index
        
        // Mark scenario as complete in save data
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            SaveManager.Instance.Data.ScenarioCompleted = true;
            
            // Advance to next level if configured
            if (currentScenario != null && currentScenario.advanceLevelOnComplete)
            {
                SaveManager.Instance.AdvanceToNextLevel();
                currentLevelNumber++; // Also update in-memory level number
            }
            else
            {
                SaveManager.Instance.SaveGame();
            }
        }
        
        // Notify listeners
        OnScenarioComplete?.Invoke();
        
        // Load next scene if specified
        if (currentScenario != null && !string.IsNullOrEmpty(currentScenario.nextSceneOnComplete))
        {
            Debug.Log($"[ScenarioManager] Loading next scene: {currentScenario.nextSceneOnComplete}");
            SceneManager.LoadScene(currentScenario.nextSceneOnComplete);
        }
        else if (currentScenario != null && currentScenario.advanceLevelOnComplete)
        {
            // No scene transition specified, but we advanced to next level
            // Start the next level's scenario immediately
            Debug.Log($"[ScenarioManager] Starting next level scenario (Level {currentLevelNumber}) in current scene.");
            LoadScenarioForLevel(currentLevelNumber);
        }
    }
    
    private void SaveStepProgress()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            SaveManager.Instance.Data.CurrentScenarioStepIndex = currentStepIndex;
            SaveManager.Instance.SaveGame();
        }
    }
    
    /// <summary>
    /// Called by SceneTransitionStep before loading a new scene
    /// </summary>
    public void PrepareForSceneTransition()
    {
        _preparingForTransition = true;
        Debug.Log("[ScenarioManager] Preparing for scene transition. Will persist.");
    }
    
    /// <summary>
    /// Allow other scripts to force skip the current step
    /// </summary>
    public void SkipCurrentStep()
    {
        if (isRunning && currentStep != null)
        {
            AdvanceStep();
        }
    }
    
    /// <summary>
    /// Triggers the level loop: increments level and reloads the current scene.
    /// Called when in loop mode and player clicks Continue after winning.
    /// </summary>
    public void TriggerLevelLoop()
    {
        if (!enableLevelLoop || currentLevelNumber < levelLoopThreshold)
        {
            Debug.LogWarning($"[ScenarioManager] TriggerLevelLoop called but not in loop mode. Level={currentLevelNumber}, Threshold={levelLoopThreshold}");
            return;
        }
        
        Debug.Log($"[ScenarioManager] 🔄 LOOP: Incrementing level from {currentLevelNumber} to {currentLevelNumber + 1} and reloading scene.");
        
        // Increment level
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            SaveManager.Instance.AdvanceToNextLevel();
        }
        currentLevelNumber++;
        
        // Determine which scene to load
        string sceneToLoad = loopSceneName;
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            // Default to current scene
            sceneToLoad = SceneManager.GetActiveScene().name;
        }
        
        Debug.Log($"[ScenarioManager] 🔄 LOOP: Reloading scene '{sceneToLoad}' for Level {currentLevelNumber}");
        
        // Prepare for transition
        PrepareForSceneTransition();
        
        // Reload the scene
        SceneManager.LoadScene(sceneToLoad);
    }
}
