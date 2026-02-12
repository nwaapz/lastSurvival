using UnityEngine;

/// <summary>
/// Scenario step that waits for the player to click on a specific map icon.
/// Blocks all other input until the target icon is clicked.
/// Use this for tutorials or guided interactions.
/// </summary>
[CreateAssetMenu(fileName = "WaitForMapIconClickStep", menuName = "Scenario/Wait For Map Icon Click Step")]
public class WaitForMapIconClickStep : ScenarioStep
{
    [Header("Target Icon")]
    [Tooltip("ID of the map icon the player must click")]
    public string targetIconId;
    
    [Tooltip("If true, activate (SetActive true) the target icon when step starts")]
    public bool activateIcon = true;
    
    [Header("Input Blocking")]
    [Tooltip("If true, block all other clicks except the target icon")]
    public bool blockOtherInput = true;
    
    [Header("Camera")]
    [Tooltip("If true, move camera to show the icon")]
    public bool moveCameraToIcon = true;
    
    [Tooltip("Camera movement duration")]
    public float cameraMoveTime = 0.5f;
    
    [Header("UI Hint")]
    [Tooltip("Optional hint text to show player")]
    public string hintText = "Tap the icon to continue";
    
    [Header("Tutorial Hand")]
    [Tooltip("If true, show an animated tutorial hand pointing at the target icon")]
    public bool showTutorialHand = false;

    private bool _clicked;
    private MapIcon _targetIcon;

    public override void OnEnter()
    {
        _clicked = false;
        _targetIcon = null;
        
        // Find the target icon (including inactive ones)
        MapIcon[] allIcons = Resources.FindObjectsOfTypeAll<MapIcon>();
        foreach (var icon in allIcons)
        {
            // Skip prefabs (only find scene objects)
            if (!icon.gameObject.scene.IsValid()) continue;
            
            if (icon.IconId == targetIconId)
            {
                _targetIcon = icon;
                break;
            }
        }
        
        if (_targetIcon == null)
        {
            Debug.LogWarning($"[WaitForMapIconClickStep] Icon '{targetIconId}' not found! Auto-completing.");
            _clicked = true;
            return;
        }
        
        // Activate the icon if needed
        if (activateIcon && !_targetIcon.gameObject.activeSelf)
        {
            _targetIcon.gameObject.SetActive(true);
            Debug.Log($"[WaitForMapIconClickStep] Activated icon: {targetIconId}");
        }
        
        Debug.Log($"[WaitForMapIconClickStep] Waiting for click on icon: {targetIconId}");
        
        // Move camera to icon
        if (moveCameraToIcon && CameraHelper.Instance != null)
        {
            Vector3 iconPos = _targetIcon.transform.position;
            CameraHelper.Instance.MoveToPosition(
                new Vector3(iconPos.x, iconPos.y, CameraHelper.Instance.fixedZ),
                Camera.main.transform.rotation,
                Camera.main.orthographicSize,
                cameraMoveTime
            );
        }
        
        // Set up input blocking
        if (blockOtherInput && BaseBuilderClickManager.Instance != null)
        {
            BaseBuilderClickManager.Instance.InteractionFilter = FilterInteraction;
        }
        
        // Subscribe to click event on target icon
        _targetIcon.OnIconClicked += HandleIconClicked;
        
        // Also subscribe to general click events to catch clicks on the icon's collider
        if (BaseBuilderClickManager.Instance != null)
        {
            BaseBuilderClickManager.Instance.OnObjectClicked += HandleObjectClicked;
        }
        
        // Show hint
        if (!string.IsNullOrEmpty(hintText))
        {
            Debug.Log($"[WaitForMapIconClickStep] Hint: {hintText}");
            // TODO: Show hint UI via a HintManager or similar
        }
        
        // Show tutorial hand
        if (showTutorialHand && _targetIcon != null && TutorialHandManager.Instance != null)
        {
            TutorialHandManager.Instance.ShowAtTransform(_targetIcon.transform);
        }
    }
    
    /// <summary>
    /// Filter function that only allows interaction with the target icon
    /// </summary>
    private bool FilterInteraction(GameObject clickedObject)
    {
        // If nothing was hit by the raycast, we need to do our own check
        // This can happen if the MapIcon uses a different detection method (e.g., UI button, OnMouseDown)
        if (clickedObject == null)
        {
            // Allow the click through - the MapIcon's own click detection will handle it
            // We'll verify in HandleIconClicked/HandleObjectClicked if it's the right icon
            return true;
        }
        
        // Check if clicked object is our target icon
        MapIcon icon = clickedObject.GetComponent<MapIcon>();
        if (icon == null)
        {
            icon = clickedObject.GetComponentInParent<MapIcon>();
        }
        
        // Only allow interaction with target icon
        return icon != null && icon == _targetIcon;
    }
    
    private void HandleIconClicked(MapIcon icon)
    {
        if (icon == _targetIcon)
        {
            _clicked = true;
            Debug.Log($"[WaitForMapIconClickStep] Player clicked on target icon: {targetIconId}");
        }
    }
    
    private void HandleObjectClicked(GameObject clickedObject)
    {
        if (clickedObject == null) return;
        
        // Check if clicked object is our target icon
        MapIcon icon = clickedObject.GetComponent<MapIcon>();
        if (icon == null)
        {
            icon = clickedObject.GetComponentInParent<MapIcon>();
        }
        
        if (icon != null && icon == _targetIcon)
        {
            _clicked = true;
            Debug.Log($"[WaitForMapIconClickStep] Player clicked on target icon (via collider): {targetIconId}");
        }
    }

    public override bool UpdateStep()
    {
        return _clicked;
    }

    public override void OnExit()
    {
        // Unsubscribe from events
        if (_targetIcon != null)
        {
            _targetIcon.OnIconClicked -= HandleIconClicked;
        }
        
        if (BaseBuilderClickManager.Instance != null)
        {
            BaseBuilderClickManager.Instance.OnObjectClicked -= HandleObjectClicked;
            BaseBuilderClickManager.Instance.InteractionFilter = null; // Remove filter
        }
        
        Debug.Log($"[WaitForMapIconClickStep] Step completed, input restored.");
        
        // Hide tutorial hand
        if (showTutorialHand && TutorialHandManager.Instance != null)
        {
            TutorialHandManager.Instance.Hide();
        }
    }
}
