using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Manages a tutorial hand/finger graphic that points to UI elements or world positions.
/// Shows an animated tapping finger to guide players during tutorials.
/// </summary>
public class TutorialHandManager : SingletonMono<TutorialHandManager>, IService
{
    [Header("Hand UI")]
    [Tooltip("The hand/finger image to display")]
    [SerializeField] private RectTransform handImage;
    
    [Tooltip("Canvas for the tutorial hand (should be Screen Space - Overlay)")]
    [SerializeField] private Canvas tutorialCanvas;
    
    [Header("Animation Settings")]
    [Tooltip("How far the hand moves during tap animation")]
    [SerializeField] private float tapDistance = 20f;
    
    [Tooltip("Duration of one tap cycle")]
    [SerializeField] private float tapDuration = 0.5f;
    
    [Tooltip("Scale pulse amount")]
    [SerializeField] private float pulseScale = 1.1f;
    
    [Header("Offset")]
    [Tooltip("Offset from target position (in screen pixels)")]
    [SerializeField] private Vector2 handOffset = new Vector2(30f, -30f);

    private Sequence _tapSequence;
    private Transform _followTarget;
    private Vector3 _staticWorldPosition;
    private bool _isFollowingWorldPosition;
    private Camera _mainCamera;
    private float _animatedYOffset; // Animated offset for up/down motion

    public void Init()
    {
        _mainCamera = Camera.main;
        Hide();
    }

    private void LateUpdate()
    {
        if (handImage == null || !handImage.gameObject.activeSelf) return;
        
        // Update position if following a target
        if (_followTarget != null)
        {
            UpdateHandPosition(_followTarget.position);
        }
        else if (_isFollowingWorldPosition)
        {
            UpdateHandPosition(_staticWorldPosition);
        }
    }

    private void UpdateHandPosition(Vector3 worldPosition)
    {
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) return;
        
        Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPosition);
        // Apply base offset plus animated Y offset for up/down motion
        Vector3 totalOffset = new Vector3(handOffset.x, handOffset.y + _animatedYOffset, 0f);
        handImage.position = screenPos + totalOffset;
    }

    /// <summary>
    /// Show the tutorial hand at a world position, following a transform.
    /// </summary>
    public void ShowAtTransform(Transform target)
    {
        if (handImage == null)
        {
            Debug.LogWarning("[TutorialHandManager] Hand image not assigned!");
            return;
        }
        
        _followTarget = target;
        _isFollowingWorldPosition = false;
        
        if (target != null)
        {
            UpdateHandPosition(target.position);
        }
        
        ShowAndAnimate();
    }

    /// <summary>
    /// Show the tutorial hand at a static world position.
    /// </summary>
    public void ShowAtWorldPosition(Vector3 worldPosition)
    {
        if (handImage == null)
        {
            Debug.LogWarning("[TutorialHandManager] Hand image not assigned!");
            return;
        }
        
        _followTarget = null;
        _staticWorldPosition = worldPosition;
        _isFollowingWorldPosition = true;
        
        UpdateHandPosition(worldPosition);
        ShowAndAnimate();
    }

    /// <summary>
    /// Show the tutorial hand at a screen position.
    /// </summary>
    public void ShowAtScreenPosition(Vector2 screenPosition)
    {
        if (handImage == null)
        {
            Debug.LogWarning("[TutorialHandManager] Hand image not assigned!");
            return;
        }
        
        _followTarget = null;
        _isFollowingWorldPosition = false;
        
        handImage.position = screenPosition + handOffset;
        ShowAndAnimate();
    }

    /// <summary>
    /// Show the tutorial hand pointing at a RectTransform (UI element).
    /// </summary>
    public void ShowAtUIElement(RectTransform uiElement)
    {
        if (handImage == null || uiElement == null)
        {
            Debug.LogWarning("[TutorialHandManager] Hand image or UI element not assigned!");
            return;
        }
        
        _followTarget = null;
        _isFollowingWorldPosition = false;
        
        // Get the center of the UI element in screen space
        Vector3[] corners = new Vector3[4];
        uiElement.GetWorldCorners(corners);
        Vector3 center = (corners[0] + corners[2]) / 2f;
        
        handImage.position = center + (Vector3)handOffset;
        ShowAndAnimate();
    }

    private void ShowAndAnimate()
    {
        handImage.gameObject.SetActive(true);
        
        // Kill any existing animation
        _tapSequence?.Kill();
        
        // Reset scale and animated offset
        handImage.localScale = Vector3.one;
        _animatedYOffset = 0f;
        
        // Create tapping animation sequence using the offset value
        _tapSequence = DOTween.Sequence();
        
        // Tap down (move offset negative = hand moves down)
        _tapSequence.Append(DOTween.To(() => _animatedYOffset, x => _animatedYOffset = x, -tapDistance, tapDuration * 0.3f)
            .SetEase(Ease.OutQuad));
        _tapSequence.Join(handImage.DOScale(pulseScale, tapDuration * 0.3f)
            .SetEase(Ease.OutQuad));
        
        // Tap up (return offset to 0)
        _tapSequence.Append(DOTween.To(() => _animatedYOffset, x => _animatedYOffset = x, 0f, tapDuration * 0.3f)
            .SetEase(Ease.InQuad));
        _tapSequence.Join(handImage.DOScale(1f, tapDuration * 0.3f)
            .SetEase(Ease.InQuad));
        
        // Pause
        _tapSequence.AppendInterval(tapDuration * 0.4f);
        
        // Loop forever
        _tapSequence.SetLoops(-1);
        _tapSequence.SetUpdate(true); // Use unscaled time
        
        Debug.Log("[TutorialHandManager] Showing tutorial hand");
    }

    /// <summary>
    /// Hide the tutorial hand.
    /// </summary>
    public void Hide()
    {
        _tapSequence?.Kill();
        _tapSequence = null;
        _followTarget = null;
        _isFollowingWorldPosition = false;
        _animatedYOffset = 0f;
        
        if (handImage != null)
        {
            handImage.gameObject.SetActive(false);
            handImage.localScale = Vector3.one;
        }
        
        Debug.Log("[TutorialHandManager] Hiding tutorial hand");
    }

    /// <summary>
    /// Check if the tutorial hand is currently visible.
    /// </summary>
    public bool IsVisible => handImage != null && handImage.gameObject.activeSelf;

    protected override void Awake()
    {
        base.Awake();
        
        // Self-register with ServiceLocator
        if (ServiceLocator.HasInstance)
        {
            ServiceLocator.Instance.Register<TutorialHandManager>(this);
        }
    }

    protected override void OnDestroy()
    {
        _tapSequence?.Kill();
        
        if (ServiceLocator.HasInstance)
        {
            ServiceLocator.Instance.Unregister<TutorialHandManager>();
        }
        base.OnDestroy();
    }
}
