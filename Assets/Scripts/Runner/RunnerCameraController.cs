using UnityEngine;

/// <summary>
/// Camera controller for runner gameplay.
/// Follows the player and provides smooth movement.
/// </summary>
public class RunnerCameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private bool autoFindPlayer = true;
    
    [Header("Position")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -10f);
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followY = false;
    [SerializeField] private bool followZ = false;
    
    [Header("X Position Constraints")]
    [Tooltip("Enable to constrain camera X position between MinX and MaxX")]
    [SerializeField] private bool constrainX = true;
    [SerializeField] private float minX = -6.87f;
    [SerializeField] private float maxX = 1f;
    
    [Header("Smoothing")]
    [SerializeField] private float smoothSpeed = 10f;
    [SerializeField] private bool useSmoothDamp = false;
    [SerializeField] private float smoothDampTime = 0.1f;
    
    [Header("Look At")]
    [SerializeField] private bool lookAtTarget = true;
    [SerializeField] private Vector3 lookAtOffset = Vector3.zero;
    
    [Header("Shake")]
    [SerializeField] private float shakeDuration = 0.2f;
    [SerializeField] private float shakeMagnitude = 0.3f;
    
    [Header("Start Transition")]
    [SerializeField] private Transform startTarget;
    [SerializeField] private float startTransitionDuration = 2f;
    [SerializeField] private AnimationCurve startTransitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("If false, call StartTransition() manually (e.g., from a button)")]
    [SerializeField] private bool autoStartTransition = false;
    
    // State
    private Vector3 _velocity = Vector3.zero;
    private float _shakeTimer;
    private Vector3 _shakeOffset;
    private Vector3 _initialPosition;
    private bool _isInStartTransition = false;

    private void Start()
    {
        _initialPosition = transform.position;
        
        // Auto-assign start target by finding "CamPoint"
        if (startTarget == null)
        {
            GameObject camPoint = GameObject.Find("CamPoint");
            if (camPoint != null)
            {
                startTarget = camPoint.transform;
                Debug.Log($"[RunnerCameraController] Found 'CamPoint', assigning as start target.");
            }
        }

        if (autoFindPlayer && target == null)
        {
            // Try to find SquadManager first
            var squadManager = FindObjectOfType<RunnerSquadManager>();
            if (squadManager != null)
            {
                target = squadManager.transform;
                Debug.Log($"[RunnerCameraController] Auto-assigned target to SquadManager: {squadManager.name}");
            }
            else
            {
                // Fallback to single player
                var player = FindObjectOfType<RunnerPlayerController>();
                if (player != null)
                {
                    target = player.transform;
                    Debug.Log($"[RunnerCameraController] Auto-assigned target to Player: {player.name}");
                }
            }
        }
        
        if (target == null)
        {
            Debug.LogWarning("[RunnerCameraController] No target assigned!");
        }
        else
        {
            Debug.Log($"[RunnerCameraController] Following target: {target.name}. FollowX: {followX}");
        }

        if (startTarget != null && autoStartTransition)
        {
            StartCoroutine(StartTransitionRoutine());
        }
    }

    /// <summary>
    /// Call this to start the camera transition manually (e.g., from a UI button).
    /// </summary>
    public void StartTransition()
    {
        if (startTarget != null)
        {
            StartCoroutine(StartTransitionRoutine());
        }
        else
        {
            Debug.LogWarning("[RunnerCameraController] StartTransition called but no startTarget assigned!");
        }
    }

    private void LateUpdate()
    {
        if (_isInStartTransition) return;
        if (target == null) return;
        
        UpdatePosition();
        UpdateShake();
        UpdateRotation();
    }

    #region Position
    
    private void UpdatePosition()
    {
        Vector3 targetPosition = CalculateTargetPosition();
        
        if (useSmoothDamp)
        {
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref _velocity, smoothDampTime);
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
        }
        
        // Apply shake offset
        transform.position += _shakeOffset;
    }
    
    private Vector3 CalculateTargetPosition()
    {
        Vector3 targetPos = transform.position;
        
        if (followX)
        {
            targetPos.x = target.position.x + offset.x;
        }
        else
        {
            targetPos.x = _initialPosition.x + offset.x;
        }
        
        // Apply X constraints if enabled
        if (constrainX)
        {
            targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        }
        
        if (followY)
        {
            targetPos.y = target.position.y + offset.y;
        }
        else
        {
            targetPos.y = _initialPosition.y + offset.y;
        }
        
        if (followZ)
        {
            targetPos.z = target.position.z + offset.z;
        }
        else
        {
            targetPos.z = _initialPosition.z + offset.z;
        }
        
        return targetPos;
    }
    
    #endregion

    #region Rotation
    
    private void UpdateRotation()
    {
        if (!lookAtTarget || target == null) return;
        
        Vector3 lookTarget = target.position + lookAtOffset;
        transform.LookAt(lookTarget);
    }
    
    #endregion

    #region Camera Shake
    
    /// <summary>
    /// Trigger camera shake effect
    /// </summary>
    public void Shake()
    {
        Shake(shakeDuration, shakeMagnitude);
    }
    
    /// <summary>
    /// Trigger camera shake with custom parameters
    /// </summary>
    public void Shake(float duration, float magnitude)
    {
        _shakeTimer = duration;
        shakeMagnitude = magnitude;
    }
    
    private void UpdateShake()
    {
        if (_shakeTimer > 0)
        {
            _shakeTimer -= Time.deltaTime;
            
            float progress = _shakeTimer / shakeDuration;
            float currentMagnitude = shakeMagnitude * progress;
            
            _shakeOffset = Random.insideUnitSphere * currentMagnitude;
            _shakeOffset.z = 0; // Keep shake in XY plane
        }
        else
        {
            _shakeOffset = Vector3.zero;
        }
    }
    
    #endregion

    #region Public Methods
    
    /// <summary>
    /// Set the camera target
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
    
    /// <summary>
    /// Set camera offset
    /// </summary>
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
    }
    
    /// <summary>
    /// Set whether camera should rotate to look at target
    /// </summary>
    public void SetLookAtTarget(bool enable)
    {
        lookAtTarget = enable;
    }
    
    /// <summary>
    /// Snap camera to target position immediately
    /// </summary>
    public void SnapToTarget()
    {
        if (target == null) return;
        
        transform.position = CalculateTargetPosition();
        UpdateRotation();
    }
    
    #endregion
    #region Start Transition

    private System.Collections.IEnumerator StartTransitionRoutine()
    {
        _isInStartTransition = true;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        
        float timer = 0f;
        
        while (timer < startTransitionDuration)
        {
            timer += Time.deltaTime;
            float t = timer / startTransitionDuration;
            float curveT = startTransitionCurve.Evaluate(t);
            
            transform.position = Vector3.Lerp(startPos, startTarget.position, curveT);
            transform.rotation = Quaternion.Lerp(startRot, startTarget.rotation, curveT);
            
            yield return null;
        }
        
        // Ensure we end up exactly at the target
        transform.position = startTarget.position;
        transform.rotation = startTarget.rotation;
        
        // Update initial position to the current position (Start Target)
        // This ensures the camera maintains this height/depth if not following those axes
        _initialPosition = transform.position;
        
        // Recalculate offset to maintain continuity and prevent snapping
        // If we don't do this, the camera will jump to 'target.position + oldOffset' on the next frame
        if (target != null)
        {
            Vector3 newOffset = Vector3.zero;
            newOffset.x = followX ? (transform.position.x - target.position.x) : 0f;
            newOffset.y = followY ? (transform.position.y - target.position.y) : 0f;
            newOffset.z = followZ ? (transform.position.z - target.position.z) : 0f;
            
            offset = newOffset;
            Debug.Log($"[RunnerCameraController] Transition complete. Recalculated offset to: {offset}");
        }
        
        _isInStartTransition = false;
    }

    #endregion
}
