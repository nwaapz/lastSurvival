using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Scenario step that shows a button at a specific world location.
/// When clicked, can transition to another scene (e.g., "Clash" button to Fight scene).
/// </summary>
[CreateAssetMenu(fileName = "ShowButtonStep", menuName = "Scenario/Show Button at Location Step")]
public class ShowButtonAtLocationStep : ScenarioStep
{
    [Header("Button Configuration")]
    [Tooltip("Text to display on the button (e.g., 'CLASH!', 'FIGHT!')")]
    public string buttonText = "CLASH!";
    
    [Tooltip("Location where the button should appear")]
    public string targetLocationId;
    
    [Header("Action")]
    [Tooltip("Scene to load when button is clicked (e.g., 'Fight')")]
    public string sceneToLoad;
    
    [Tooltip("If true, step completes when button is clicked. If false, button just loads scene.")]
    public bool completeOnClick = true;
    
    [Header("Visual Settings")]
    [Tooltip("Offset from location in screen space (pixels)")]
    public Vector2 screenOffset = new Vector2(0, 50);

    private GameObject _buttonObject;
    private bool _buttonClicked = false;
    private Canvas _worldCanvas;

    public override void OnEnter()
    {
        _buttonClicked = false;
        
        // Find the target location
        NamedLocation location = FindLocationById(targetLocationId);
        if (location == null)
        {
            Debug.LogWarning($"[ShowButtonAtLocationStep] Location '{targetLocationId}' not found! Step will auto-complete.");
            _buttonClicked = true;
            return;
        }
        
        // Create the button
        CreateButtonAtLocation(location);
    }

    private void CreateButtonAtLocation(NamedLocation location)
    {
        // Find or create a Canvas
        _worldCanvas = Object.FindObjectOfType<Canvas>();
        if (_worldCanvas == null)
        {
            Debug.LogWarning("[ShowButtonAtLocationStep] No Canvas found in scene! Cannot create button.");
            _buttonClicked = true;
            return;
        }
        
        // Create button GameObject
        _buttonObject = new GameObject($"ClashButton_{targetLocationId}");
        _buttonObject.transform.SetParent(_worldCanvas.transform, false);
        
        // Add RectTransform
        RectTransform rectTransform = _buttonObject.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(150, 60);
        
        // Position button at location (world to screen)
        PositionButtonAtLocation(location, rectTransform);
        
        // Add Button component
        Button button = _buttonObject.AddComponent<Button>();
        button.onClick.AddListener(OnButtonClicked);
        
        // Add Image (background)
        Image image = _buttonObject.AddComponent<Image>();
        image.color = new Color(1f, 0.3f, 0.3f, 0.9f); // Red-ish color for "Clash"
        
        // Add Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(_buttonObject.transform, false);
        
        Text text = textObj.AddComponent<Text>();
        text.text = buttonText;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 24;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        Debug.Log($"[ShowButtonAtLocationStep] Created '{buttonText}' button at {targetLocationId}");
    }

    private void PositionButtonAtLocation(NamedLocation location, RectTransform rectTransform)
    {
        // Convert world position to screen position
        Vector3 worldPos = location.transform.position;
        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        
        // Apply offset
        screenPos.x += screenOffset.x;
        screenPos.y += screenOffset.y;
        
        // Set position
        rectTransform.position = screenPos;
    }

    private void OnButtonClicked()
    {
        Debug.Log($"[ShowButtonAtLocationStep] Button clicked! Loading scene: {sceneToLoad}");
        
        _buttonClicked = true;
        
        // Hide button
        if (_buttonObject != null)
        {
            Object.Destroy(_buttonObject);
        }
        
        // Load scene
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            // Tell ScenarioManager to persist
            if (ScenarioManager.Instance != null)
            {
                ScenarioManager.Instance.PrepareForSceneTransition();
            }
            
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    public override bool UpdateStep()
    {
        // Update button position if it exists (in case camera moves)
        if (_buttonObject != null && !_buttonClicked)
        {
            NamedLocation location = FindLocationById(targetLocationId);
            if (location != null)
            {
                RectTransform rectTransform = _buttonObject.GetComponent<RectTransform>();
                PositionButtonAtLocation(location, rectTransform);
            }
        }
        
        return completeOnClick && _buttonClicked;
    }

    public override void OnExit()
    {
        // Clean up button if it still exists
        if (_buttonObject != null)
        {
            Object.Destroy(_buttonObject);
        }
    }
    
    private NamedLocation FindLocationById(string locationId)
    {
        NamedLocation[] allLocations = Object.FindObjectsOfType<NamedLocation>();
        foreach (var loc in allLocations)
        {
            if (loc.locationId == locationId)
            {
                return loc;
            }
        }
        return null;
    }
}
