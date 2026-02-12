using UnityEngine;

/// <summary>
/// Debug utility for testing scenario system.
/// Add to ScenarioManager GameObject to get runtime controls.
/// </summary>
public class ScenarioDebugger : MonoBehaviour
{
    [Header("Debug Controls")]
    [SerializeField] private bool showDebugUI = true;
    [SerializeField] private KeyCode skipStepKey = KeyCode.N; // Press N to skip current step
    [SerializeField] private KeyCode resetProgressKey = KeyCode.R; // Press R to reset scenario
    
    private Rect _windowRect = new Rect(10, 10, 300, 200);
    private bool _showWindow = false;

    private void Update()
    {
        // Toggle debug window
        if (Input.GetKeyDown(KeyCode.F1))
        {
            _showWindow = !_showWindow;
        }
        
        // Skip current step
        if (Input.GetKeyDown(skipStepKey) && ScenarioManager.Instance != null)
        {
            Debug.Log("[ScenarioDebugger] Skipping current step...");
            ScenarioManager.Instance.SkipCurrentStep();
        }
        
        // Reset scenario progress
        if (Input.GetKeyDown(resetProgressKey))
        {
            ResetScenarioProgress();
        }
    }

    private void OnGUI()
    {
        if (!showDebugUI || !_showWindow) return;
        
        _windowRect = GUILayout.Window(0, _windowRect, DrawDebugWindow, "Scenario Debugger");
    }

    private void DrawDebugWindow(int windowID)
    {
        GUILayout.Label("=== Scenario Status ===");
        
        if (ScenarioManager.Instance == null)
        {
            GUILayout.Label("ScenarioManager: NOT FOUND");
            GUI.DragWindow();
            return;
        }
        
        var manager = ScenarioManager.Instance;
        
        GUILayout.Label($"Level: {manager.CurrentLevelNumber}");
        
        // Show loop mode status
        if (manager.IsLevelLoopEnabled)
        {
            string loopStatus = manager.IsInLoopMode ? "🔄 ACTIVE" : $"(starts at level {manager.LevelLoopThreshold})";
            GUILayout.Label($"Loop Mode: {loopStatus}");
        }
        
        GUILayout.Label($"Step: {manager.CurrentStepIndex + 1} / {manager.TotalSteps}");
        
        if (manager.CurrentStep != null)
        {
            GUILayout.Label($"Current: {manager.CurrentStep.name}");
            GUILayout.Label($"Description: {manager.CurrentStep.description}");
        }
        else
        {
            GUILayout.Label("Current: NONE");
            if (manager.IsInLoopMode)
            {
                GUILayout.Label("(In loop mode - waiting for continue)");
            }
        }
        
        GUILayout.Space(10);
        GUILayout.Label("=== Controls ===");
        GUILayout.Label($"F1: Toggle this window");
        GUILayout.Label($"{skipStepKey}: Skip current step");
        GUILayout.Label($"{resetProgressKey}: Reset progress");
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Skip Current Step"))
        {
            manager.SkipCurrentStep();
        }
        
        if (GUILayout.Button("Reset Scenario Progress"))
        {
            ResetScenarioProgress();
        }
        
        if (GUILayout.Button("Reload Current Level"))
        {
            manager.LoadAndStartCurrentLevelScenario();
        }
        
        // Loop mode controls
        if (manager.IsInLoopMode)
        {
            GUILayout.Space(5);
            if (GUILayout.Button("🔄 Trigger Level Loop"))
            {
                manager.TriggerLevelLoop();
            }
        }
        
        GUILayout.Space(10);
        
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            var data = SaveManager.Instance.Data;
            GUILayout.Label("=== Save Data ===");
            GUILayout.Label($"Coins: {data.Coins}");
            GUILayout.Label($"Level: {data.CurrentLevel}");
            GUILayout.Label($"Step Index: {data.CurrentScenarioStepIndex}");
            GUILayout.Label($"Completed: {data.ScenarioCompleted}");
        }
        
        GUI.DragWindow();
    }

    private void ResetScenarioProgress()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            SaveManager.Instance.Data.CurrentScenarioStepIndex = 0;
            SaveManager.Instance.Data.ScenarioCompleted = false;
            SaveManager.Instance.SaveGame();
            Debug.Log("[ScenarioDebugger] Scenario progress reset!");
            
            // Reload scenario
            if (ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.LoadAndStartCurrentLevelScenario();
            }
        }
    }
}
