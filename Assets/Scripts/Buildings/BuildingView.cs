using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Component for building GameObjects in the scene.
/// Updates SpriteRenderer based on current building level.
/// Click/tap on building to upgrade via UI Button (recommended) or Collider2D.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class BuildingView : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The building definition this view represents")]
    public BuildingDefinition BuildingDefinition;
    
    /// <summary>
    /// The building ID (read-only, from BuildingDefinition). Use this value for BuildObjectiveStep.targetBuildingId
    /// </summary>
    public string BuildingId => BuildingDefinition != null ? BuildingDefinition.Id : "(No BuildingDefinition assigned)";
    
    [Tooltip("Allow upgrading by clicking/tapping on this building")]
    public bool CanUpgradeByClick = true;
    
    [Header("UI Click Detection (Recommended)")]
    [Tooltip("Optional UI Button for click detection. Create a Canvas with a transparent Button over the building.")]
    public Button ClickButton;
    
    [Header("References")]
    [Tooltip("SpriteRenderer to update (auto-assigned if not set)")]
    public SpriteRenderer SpriteRenderer;
    
    [Tooltip("Particle system to play on successful upgrade")]
    public ParticleSystem UpgradeParticles;
    
    [Header("Animation")]
    [Tooltip("Duration of fade out/in animations in seconds")]
    public float FadeDuration = 0.25f;
    
    [Header("Collider")]
    [Tooltip("Minimum collider size when sprite is null (for level 0 buildings)")]
    public Vector2 MinColliderSize = new Vector2(2f, 2f);
    
    [Header("Click Detection")]
    [Tooltip("Maximum distance between touch start and end to count as a tap")]
    public float TapThreshold = 10f;
    
    private bool _isAnimating = false;
    private BoxCollider2D _boxCollider;
    
    // Touch/click tracking
    private bool _inputStartedOnThis = false;
    private Vector3 _inputStartPosition;
    private bool _subscribedToProgressManager = false;

    [SerializeField] TextMeshProUGUI levelText;
    private void Awake()
    {
        if (SpriteRenderer == null)
        {
            SpriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        _boxCollider = GetComponent<BoxCollider2D>();
    }
    
    private void Start()
    {
        // Subscribe to upgrade events
        TrySubscribeToProgressManager();

        // Wire up UI Button click if assigned (recommended method)
        if (ClickButton != null)
        {
            ClickButton.onClick.AddListener(OnClickButtonPressed);
            Debug.Log($"[BuildingView] UI Button wired for {gameObject.name}");
        }
        else
        {
            // Fallback: Subscribe to BaseBuilderClickManager for click detection
            // (OnMouseDown/OnMouseUp may not work reliably with 2D colliders and angled cameras)
            if (BaseBuilderClickManager.Instance != null)
            {
                BaseBuilderClickManager.Instance.OnObjectClicked += OnObjectClickedFallback;
            }
        }
        
        // Initial sprite update
        Debug.Log($"[LVLTEXT] {gameObject.name} Start() calling UpdateSprite()");
        UpdateSprite();
    }
    
    private void Update()
    {
        // Retry subscription if it failed during Start
        if (!_subscribedToProgressManager)
        {
            TrySubscribeToProgressManager();
        }
    }
    
    private void TrySubscribeToProgressManager()
    {
        if (_subscribedToProgressManager) return;
        
        if (BuildingProgressManager.Instance != null)
        {
            BuildingProgressManager.Instance.OnBuildingUpgraded += OnBuildingUpgraded;
            BuildingProgressManager.Instance.OnProgressLoaded += OnProgressLoaded;
            _subscribedToProgressManager = true;
            Debug.Log($"[LVLTEXT] {gameObject.name} subscribed to BuildingProgressManager");
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events - safely check if instance exists
        if (_subscribedToProgressManager && BuildingProgressManager.HasInstance)
        {
            BuildingProgressManager.Instance.OnBuildingUpgraded -= OnBuildingUpgraded;
            BuildingProgressManager.Instance.OnProgressLoaded -= OnProgressLoaded;
        }
        
        // Clean up UI Button listener
        if (ClickButton != null)
        {
            ClickButton.onClick.RemoveListener(OnClickButtonPressed);
        }
        
        // Unsubscribe from click manager
        if (BaseBuilderClickManager.HasInstance)
        {
            BaseBuilderClickManager.Instance.OnObjectClicked -= OnObjectClickedFallback;
        }
    }
    
    /// <summary>
    /// Called when the UI Button is clicked (recommended click detection method)
    /// </summary>
    private void OnClickButtonPressed()
    {
        if (!CanUpgradeByClick) return;
        
        Debug.Log($"[BuildingView] UI Button clicked on {gameObject.name}");
        TryUpgrade();
    }
    
    /// <summary>
    /// Fallback click handler when BaseBuilderClickManager detects a click on this object
    /// </summary>
    private void OnObjectClickedFallback(GameObject clickedObject)
    {
        if (!CanUpgradeByClick) return;
        if (clickedObject != gameObject) return;
        
        // Don't process if camera is being dragged
        if (CameraHelper.Instance != null && CameraHelper.Instance.IsDragging)
        {
            return;
        }
        
        Debug.Log($"[BuildingView] Click detected via BaseBuilderClickManager on {gameObject.name}");
        TryUpgrade();
    }

    private void OnProgressLoaded()
    {
        UpdateSprite();
    }
    
    /// <summary>
    /// Called when any building is upgraded
    /// </summary>
    private void OnBuildingUpgraded(string buildingId, int newLevel)
    {
        Debug.Log($"[LVLTEXT] {gameObject.name} received OnBuildingUpgraded: buildingId={buildingId}, newLevel={newLevel}, myBuildingId={BuildingDefinition?.Id}");
        
        // Only update if this is our building
        if (BuildingDefinition != null && BuildingDefinition.Id == buildingId)
        {
            Debug.Log($"[LVLTEXT] {gameObject.name} MATCHED! Updating level text to {newLevel}");
            // Update level text immediately so it shows new level right away
            UpdateLevelText(newLevel);
            StartCoroutine(PlayUpgradeAnimation(newLevel));
        }
    }
    
    /// <summary>
    /// Play the full upgrade animation sequence:
    /// 1. Fade out current sprite (250ms)
    /// 2. Play particle effect
    /// 3. Fade in new sprite (250ms)
    /// </summary>
    private IEnumerator PlayUpgradeAnimation(int newLevel)
    {
        _isAnimating = true;
        
        // 1. Fade out current sprite
        yield return StartCoroutine(FadeSprite(1f, 0f, FadeDuration));
        
        // 2. Update to new sprite and play particles
        UpdateSpriteImmediate();
        // Ensure level text shows correct new level after sprite update
        UpdateLevelText(newLevel);
        PlayUpgradeEffect();
        
        // 3. Fade in new sprite
        yield return StartCoroutine(FadeSprite(0f, 1f, FadeDuration));
        
        _isAnimating = false;
    }
    
    /// <summary>
    /// Fade sprite alpha from start to end over duration
    /// </summary>
    private IEnumerator FadeSprite(float fromAlpha, float toAlpha, float duration)
    {
        if (SpriteRenderer == null) yield break;
        
        float elapsed = 0f;
        Color color = SpriteRenderer.color;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            color.a = Mathf.Lerp(fromAlpha, toAlpha, t);
            SpriteRenderer.color = color;
            yield return null;
        }
        
        color.a = toAlpha;
        SpriteRenderer.color = color;
    }
    
    /// <summary>
    /// Play the upgrade particle effect
    /// </summary>
    private void PlayUpgradeEffect()
    {
        if (UpgradeParticles != null)
        {
            UpgradeParticles.Play();
        }
    }
    
    /// <summary>
    /// Update the sprite based on current level (called on Start)
    /// </summary>
    public void UpdateSprite()
    {
        UpdateSpriteImmediate();
        
        // Ensure full opacity on initial load
        if (SpriteRenderer != null)
        {
            Color color = SpriteRenderer.color;
            color.a = 1f;
            SpriteRenderer.color = color;
        }
    }
    
    /// <summary>
    /// Update sprite immediately without affecting alpha (used during animation)
    /// </summary>
    private void UpdateSpriteImmediate()
    {
        if (BuildingDefinition == null)
        {
            Debug.LogWarning($"[BuildingView] No BuildingDefinition assigned on {gameObject.name}");
            return;
        }
        
        if (SpriteRenderer == null)
        {
            Debug.LogWarning($"[BuildingView] No SpriteRenderer on {gameObject.name}");
            return;
        }
        
        int level = GetCurrentLevel();
        
        // Update level text display (only during initial load, not during animations)
        if (!_isAnimating)
        {
            UpdateLevelText(level);
        }
        
        // Level 0 = building not built yet, show nothing
        if (level <= 0)
        {
            SpriteRenderer.sprite = null;
            EnsureColliderSize();
            return;
        }
        
        Sprite sprite = null;
        
        if (BuildingProgressManager.Instance != null)
        {
            sprite = BuildingProgressManager.Instance.GetCurrentSprite(BuildingDefinition);
        }
        else
        {
            // Fallback: use level 1 sprite if manager not available
            sprite = BuildingDefinition.GetSpriteForLevel(1);
        }
        
        SpriteRenderer.sprite = sprite;
    }
    
    /// <summary>
    /// Ensure BoxCollider2D has a minimum size for click detection when sprite is null
    /// </summary>
    private void EnsureColliderSize()
    {
        if (_boxCollider != null && _boxCollider.size.magnitude < 0.1f)
        {
            _boxCollider.size = MinColliderSize;
        }
    }
    
    /// <summary>
    /// Update the level text UI to show current building level
    /// </summary>
    private void UpdateLevelText(int level)
    {
        Debug.Log($"[LVLTEXT] {gameObject.name} UpdateLevelText called with level={level}, levelText assigned={levelText != null}");
        if (levelText != null)
        {
            levelText.text = level.ToString();
            Debug.Log($"[LVLTEXT] {gameObject.name} levelText.text set to '{levelText.text}'");
        }
    }
    
    /// <summary>
    /// Get the current level of this building
    /// </summary>
    public int GetCurrentLevel()
    {
        if (BuildingProgressManager.Instance != null && BuildingDefinition != null)
        {
            return BuildingProgressManager.Instance.GetLevel(BuildingDefinition);
        }
        return 0; // Default to not built
    }
    
    /// <summary>
    /// Upgrade this building (convenience method)
    /// </summary>
    public bool Upgrade()
    {
        if (BuildingProgressManager.Instance != null && BuildingDefinition != null)
        {
            return BuildingProgressManager.Instance.UpgradeBuilding(BuildingDefinition);
        }
        return false;
    }
    
    // Mouse/touch down - record start position
    private void OnMouseDown()
    {
        if (!CanUpgradeByClick) return;
        
        _inputStartedOnThis = true;
        _inputStartPosition = Input.mousePosition;
    }
    
    // Mouse/touch up - check if valid tap on this building
    private void OnMouseUp()
    {
        if (!CanUpgradeByClick || !_inputStartedOnThis)
        {
            _inputStartedOnThis = false;
            return;
        }
        
        _inputStartedOnThis = false;
        
        // Don't process if camera is being dragged
        if (CameraHelper.Instance != null && CameraHelper.Instance.IsDragging)
        {
            return;
        }
        
        // Check if touch end position is close to start position (not a drag)
        Vector3 inputEndPosition = Input.mousePosition;
        float distance = Vector3.Distance(_inputStartPosition, inputEndPosition);
        
        if (distance > TapThreshold)
        {
            return;
        }
        
        // Check if touch end is still on this building's collider
        if (!IsPointerOverThisBuilding(inputEndPosition))
        {
            return;
        }
        
        TryUpgrade();
    }
    
    /// <summary>
    /// Check if the screen position is over this building's collider
    /// </summary>
    private bool IsPointerOverThisBuilding(Vector3 screenPosition)
    {
        Camera cam = Camera.main;
        if (cam == null) return false;
        
        Vector2 worldPoint = cam.ScreenToWorldPoint(screenPosition);
        RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);
        
        return hit.collider != null && hit.collider.gameObject == gameObject;
    }
    
    /// <summary>
    /// Attempt to upgrade and log result
    /// </summary>
    private void TryUpgrade()
    {
        // Block clicks during animation
        if (_isAnimating)
        {
            return;
        }
        
        if (BuildingDefinition == null)
        {
            Debug.LogWarning($"[BuildingView] Cannot upgrade - no BuildingDefinition on {gameObject.name}");
            return;
        }
        
        // Check if this building is allowed to upgrade (only during WaitForBuildingClickStep)
        if (!WaitForBuildingClickStep.IsBuildingAllowedToUpgrade(BuildingDefinition.Id))
        {
            Debug.Log($"[BuildingView] {BuildingDefinition.DisplayName} is not allowed to upgrade right now");
            return;
        }
        
        if (BuildingProgressManager.Instance == null)
        {
            Debug.LogWarning("[BuildingView] Cannot upgrade - BuildingProgressManager not available");
            return;
        }
        
        if (!BuildingProgressManager.Instance.CanUpgrade(BuildingDefinition))
        {
            Debug.Log($"[BuildingView] {BuildingDefinition.DisplayName} is already at max level");
            return;
        }
        
        Upgrade();
    }
}
