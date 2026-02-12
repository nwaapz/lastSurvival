using UnityEngine;
using TMPro;

/// <summary>
/// Displays the current objective/task to the player.
/// Subscribe to ScenarioManager events to update when steps change.
/// </summary>
public class ObjectiveUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject objectivePanel;
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private TextMeshProUGUI stepCounterText; // e.g., "Step 2/5"
    
    [Header("Settings")]
    [SerializeField] private bool hideWhenNoObjective = true;

    private void Start()
    {
        // Subscribe to scenario events
        if (ScenarioManager.Instance != null)
        {
            ScenarioManager.Instance.OnStepChanged += UpdateObjective;
            ScenarioManager.Instance.OnScenarioComplete += OnScenarioComplete;
            
            // Display current step if scenario is already running
            if (ScenarioManager.Instance.CurrentStep != null)
            {
                UpdateObjective(ScenarioManager.Instance.CurrentStep);
            }
        }
        else
        {
            Debug.LogWarning("[ObjectiveUI] ScenarioManager not found!");
            if (hideWhenNoObjective && objectivePanel != null)
            {
                objectivePanel.SetActive(false);
            }
        }
    }

    private void OnDestroy()
    {
        if (ScenarioManager.Instance != null)
        {
            ScenarioManager.Instance.OnStepChanged -= UpdateObjective;
            ScenarioManager.Instance.OnScenarioComplete -= OnScenarioComplete;
        }
    }

    private void UpdateObjective(ScenarioStep step)
    {
        if (step == null)
        {
            ClearObjective();
            return;
        }

        // Show panel
        if (objectivePanel != null)
        {
            objectivePanel.SetActive(true);
        }

        // Update objective text
        if (objectiveText != null)
        {
            // Use the step's description field
            string description = !string.IsNullOrEmpty(step.description) 
                ? step.description 
                : $"Complete: {step.name}";
            
            objectiveText.text = description;
        }

        // Update step counter (e.g., "Step 2/5")
        if (stepCounterText != null && ScenarioManager.Instance != null)
        {
            int currentIndex = ScenarioManager.Instance.CurrentStepIndex;
            int totalSteps = ScenarioManager.Instance.TotalSteps;
            
            if (totalSteps > 0)
            {
                stepCounterText.text = $"Step {currentIndex + 1}/{totalSteps}";
            }
            else
            {
                stepCounterText.text = "";
            }
        }
    }

    private void OnScenarioComplete()
    {
        if (objectiveText != null)
        {
            objectiveText.text = "Mission Complete!";
        }
        
        if (stepCounterText != null)
        {
            stepCounterText.text = "";
        }
        
        // Optionally hide after a delay
        if (hideWhenNoObjective)
        {
            Invoke(nameof(ClearObjective), 3f);
        }
    }

    private void ClearObjective()
    {
        if (objectivePanel != null && hideWhenNoObjective)
        {
            objectivePanel.SetActive(false);
        }
        
        if (objectiveText != null)
        {
            objectiveText.text = "";
        }
        
        if (stepCounterText != null)
        {
            stepCounterText.text = "";
        }
    }
}
