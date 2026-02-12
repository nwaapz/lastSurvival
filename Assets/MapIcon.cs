using UnityEngine;

public class MapIcon : MonoBehaviour
{
    [Tooltip("Unique identifier for this map icon (used by scenario system)")]
    [SerializeField] private string iconId;
    
    /// <summary>
    /// Unique identifier for this map icon
    /// </summary>
    public string IconId => iconId;
    
    /// <summary>
    /// Event fired when this icon is clicked
    /// </summary>
    public event System.Action<MapIcon> OnIconClicked;

    /// <summary>
    /// Called when the icon is clicked (can be called externally or via OnMouseDown)
    /// </summary>
    public void OnClick()
    {
        OnIconClicked?.Invoke(this);
        AdvanceScenario();
    }
    
    /// <summary>
    /// Detect clicks via Unity's built-in mouse/touch detection (requires Collider2D)
    /// </summary>
    private void OnMouseDown()
    {
        Debug.Log($"[MapIcon] OnMouseDown detected on: {iconId}");
        OnClick();
    }

    private void AdvanceScenario()
    {
        var scenarioManager = FindAnyObjectByType<ScenarioManager>();
        if (scenarioManager != null)
        {
            scenarioManager.AdvanceStep();
        }
        else
        {
            Debug.LogWarning($"[MapIcon] ScenarioManager not found when clicking icon: {iconId}");
        }
    }
}
