using UnityEngine;

/// <summary>
/// Waits for the Continue button to be clicked in the Fight scene win screen.
/// The step completes when the player clicks Continue after winning.
/// NOTE: FightSceneUIManager was removed. This step is currently disabled.
/// </summary>
[CreateAssetMenu(fileName = "WaitForContinueStep", menuName = "Scenario/Wait For Continue Button Step")]
public class WaitForContinueButtonStep : ScenarioStep
{
    private bool _continueClicked = false;
    private RunnerUIManager _uiManager;

    public override void OnEnter()
    {
        _continueClicked = false;
        
        // Find the UI manager
        _uiManager = FindObjectOfType<RunnerUIManager>();
        
        if (_uiManager == null)
        {
            Debug.LogWarning("[WaitForContinueButtonStep] RunnerUIManager not found! Auto-completing.");
            _continueClicked = true;
            return;
        }
        
        Debug.Log("[WaitForContinueButtonStep] Waiting for Continue button click...");
        _uiManager.OnContinueClicked += OnContinueHandler;
    }

    private void OnContinueHandler()
    {
        Debug.Log("[WaitForContinueButtonStep] Continue clicked! Completing step.");
        _continueClicked = true;
    }

    public override bool UpdateStep()
    {
        return _continueClicked;
    }

    public override void OnExit()
    {
        if (_uiManager != null)
        {
            _uiManager.OnContinueClicked -= OnContinueHandler;
        }
    }
}
