using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scenario step that transitions to the fight scene.
/// Can optionally wait for player input (button click) before transitioning.
/// </summary>
[CreateAssetMenu(fileName = "GoToFightStep", menuName = "Scenario/Go To Fight Step")]
public class GoToFightStep : ScenarioStep
{
    [Header("Fight Scene")]
    [Tooltip("Name of the fight scene to load")]
    [SceneName(false)]
    public string fightSceneName = "FightScene";
    
    [Header("Trigger")]
    [Tooltip("If true, wait for player to click a button. If false, transition immediately.")]
    public bool waitForButtonClick = true;
    
    [Tooltip("ID of the button to wait for (if waitForButtonClick is true)")]
    public string buttonId = "ClashButton";
    
    [Header("Pre-Fight Setup")]
    [Tooltip("Optional: Enemy wave configuration to pass to fight scene")]
    public int enemyCount = 10;
    
    [Tooltip("Optional: Difficulty multiplier")]
    public float difficultyMultiplier = 1f;
    
    [Header("Narration")]
    [Tooltip("Optional dialogue before fight")]
    public NarrationLine preFightDialogue;

    private bool _transitioned;
    private bool _dialogueShown;

    public override void OnEnter()
    {
        _transitioned = false;
        _dialogueShown = preFightDialogue == null;
        
        // Show pre-fight dialogue
        if (preFightDialogue != null && Narration_manager.Instance != null)
        {
            Narration_manager.Instance.ShowNarrationLine(preFightDialogue);
        }
        
        // If not waiting for button, prepare immediate transition
        if (!waitForButtonClick)
        {
            // Will transition in UpdateStep after dialogue
        }
        else
        {
            // Subscribe to button click
            // TODO: Subscribe to UI button with buttonId
            Debug.Log($"[GoToFightStep] Waiting for button: {buttonId}");
        }
    }

    public override bool UpdateStep()
    {
        if (_transitioned) return true;
        
        // Wait for dialogue dismissal
        if (!_dialogueShown)
        {
            if (Input.GetMouseButtonDown(0))
            {
                _dialogueShown = true;
            }
            return false;
        }
        
        // If not waiting for button, transition now
        if (!waitForButtonClick)
        {
            TransitionToFight();
            return true;
        }
        
        // Check for button click (simplified - you may want proper UI integration)
        // For now, check for any click as placeholder
        if (Input.GetMouseButtonDown(0))
        {
            TransitionToFight();
            return true;
        }
        
        return false;
    }

    private void TransitionToFight()
    {
        if (_transitioned) return;
        _transitioned = true;
        
        // Store fight configuration for the fight scene to read
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            // You could store enemy count, difficulty, etc. in save data
            // SaveManager.Instance.Data.NextFightEnemyCount = enemyCount;
        }
        
        Debug.Log($"[GoToFightStep] Transitioning to fight: {fightSceneName}");
        
        // Prepare scenario manager for scene transition
        if (ScenarioManager.Instance != null)
        {
            ScenarioManager.Instance.PrepareForSceneTransition();
        }
        
        SceneManager.LoadScene(fightSceneName);
    }

    public override void OnExit()
    {
        // Cleanup
    }
}
