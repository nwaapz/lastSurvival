// 11/22/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using UnityEditor;
using UnityEngine;

public class CameraHelper : SingletonMono<CameraHelper>, IService
{
    [Header("Camera Controls")]
    public int cameraPanButton = 1; // 0 = Left Click, 1 = Right Click, 2 = Middle Click
    public float panSpeed = 10f; // Speed of panning
    public float zoomSpeed = 5f; // Speed of zooming
    public float dragThreshold = 15f; // Pixels to move before considering it a drag
    
    [Header("Zoom Limits")]
    public float minZoom = 5f; // Minimum zoom level
    public float maxZoom = 20f; // Maximum zoom level

    [Header("Debug")]
    [Tooltip("When enabled, camera movement has no position limits")]
    public bool unlimitedMovement = false;

    public float minX = -61f; // Minimum camera X position
    public float maxX = 57f;  // Maximum camera X position
    public float minY = 34.7f; // Minimum camera Y position
    public float maxY = 53.5f;  // Maximum camera Y position
    public float fixedZ = 10f; // Fixed camera Z position

    private Vector3 dragOrigin;
    private bool isDragging = false;
    private float lastClickTime;
    private const float doubleClickTime = 0.3f; // Time interval for double click detection
    
    // Touch support
    private Vector2 lastTouchPosition;
    private bool isTouchDragging = false;
    
    // Smooth movement to locations
    private bool isMovingToLocation = false;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float targetZoom;
    private float movementProgress = 0f;
    private Vector3 movementStartPosition;
    private Quaternion movementStartRotation;
    private float movementStartZoom;
    private float movementDuration = 1.5f;
    private System.Action onMovementComplete;
    
    // Follow target
    private Transform _followTarget;
    private bool _isFollowing = false;
    private float _followSmoothing = 0.3f;
    private Vector3 _followVelocity;

    protected override void Awake()
    {
        base.Awake();
        
        // Self-register with ServiceLocator
        if (ServiceLocator.HasInstance)
        {
            ServiceLocator.Instance.Register<CameraHelper>(this);
        }
    }
    
    protected override void OnDestroy()
    {
        if (ServiceLocator.HasInstance)
        {
            ServiceLocator.Instance.Unregister<CameraHelper>();
        }
        base.OnDestroy();
    }

    public void Init()
    {
    }

    void Update()
    {
        if (isMovingToLocation)
        {
            HandleSmoothMovement();
        }
        else if (_isFollowing && _followTarget != null)
        {
            HandleFollowTarget();
        }
        else
        {
            HandlePanning();
            HandleTouchPanning();
            HandleZooming();
            HandleClickAndDoubleClick();
        }
    }

    private void HandlePanning()
    {
        if (Input.GetMouseButtonDown(cameraPanButton))
        {
            dragOrigin = Input.mousePosition;
            isDragging = false;
        }

        if (Input.GetMouseButton(cameraPanButton))
        {
            float dragDistance = Vector3.Distance(Input.mousePosition, dragOrigin);
            
            // Only start dragging if moved beyond threshold
            if (!isDragging && dragDistance > dragThreshold)
            {
                isDragging = true;
            }
            
            // Only pan camera if actually dragging
            if (isDragging)
            {
                Vector3 difference = Camera.main.ScreenToViewportPoint(dragOrigin - Input.mousePosition);
                dragOrigin = Input.mousePosition;

                // Move along X and Y axes (Z is constant): Screen X -> World X, Screen Y -> World Y
                Vector3 move = new Vector3(difference.x * panSpeed, difference.y * panSpeed, 0);
                Camera.main.transform.Translate(move, Space.World);

                ClampCameraPosition();
            }
        }
        
        if (Input.GetMouseButtonUp(cameraPanButton))
        {
            isDragging = false;
        }
    }

    private void ClampCameraPosition()
    {
        if (Camera.main == null)
        {
            return;
        }

        Vector3 pos = Camera.main.transform.position;
        
        if (!unlimitedMovement)
        {
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY); // Clamp Y between minY and maxY
        }
        
        pos.z = fixedZ; // Keep Z constant
        Camera.main.transform.position = pos;
    }

    private void HandleTouchPanning()
    {
        // Only handle single touch (two-finger is for pinch zoom)
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                lastTouchPosition = touch.position;
                isTouchDragging = false;
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                float dragDistance = Vector2.Distance(touch.position, lastTouchPosition);
                
                if (!isTouchDragging && dragDistance > dragThreshold)
                {
                    isTouchDragging = true;
                }
                
                if (isTouchDragging)
                {
                    Vector2 delta = touch.deltaPosition;
                    
                    // Move along X and Y axes (Z is constant)
                    Vector3 move = new Vector3(
                        -delta.x * panSpeed * Time.deltaTime * 0.1f,
                        -delta.y * panSpeed * Time.deltaTime * 0.1f,
                        0
                    );
                    
                    Camera.main.transform.Translate(move, Space.World);
                    ClampCameraPosition();
                }
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isTouchDragging = false;
            }
        }
    }
    
    private void HandleZooming()
    {
        // Mouse wheel zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0.0f)
        {
            Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - scroll * zoomSpeed, minZoom, maxZoom);
        }

        // Touch pinch zoom
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
            Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

            float prevMagnitude = (touch0PrevPos - touch1PrevPos).magnitude;
            float currentMagnitude = (touch0.position - touch1.position).magnitude;
            float diff = currentMagnitude - prevMagnitude;

            float pinchAmount = diff * 0.01f * zoomSpeed;
            Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - pinchAmount, minZoom, maxZoom);
        }
    }

    private void HandleClickAndDoubleClick()
    {
        // Only detect clicks if we didn't drag
        if (Input.GetMouseButtonUp(0) && !isDragging)
        {
            float timeSinceLastClick = Time.time - lastClickTime;

            if (timeSinceLastClick <= doubleClickTime)
            {
                OnDoubleClick();
            }
            else
            {
                OnSingleClick();
            }

            lastClickTime = Time.time;
        }
    }

    private void OnSingleClick()
    {
        // Note: BaseBuilderClickManager handles actual click-to-move
        // This is just for camera-specific click actions if needed
        // Debug.Log("Single click detected!");
    }

    private void OnDoubleClick()
    {
        // Note: BaseBuilderClickManager handles actual clicks
        // This is just for camera-specific double-click actions if needed
        // Debug.Log("Double click detected!");
    }
    
    /// <summary>
    /// Check if camera is currently being dragged (useful for other systems to know)
    /// </summary>
    public bool IsDragging => isDragging;
    
    /// <summary>
    /// Check if camera is currently moving to a location
    /// </summary>
    public bool IsMovingToLocation => isMovingToLocation;
    
    /// <summary>
    /// Move camera smoothly to a named location
    /// </summary>
    public void MoveToLocation(string locationId, float duration = 1.5f, System.Action onComplete = null)
    {
        NamedLocation location = FindLocationById(locationId);
        if (location == null)
        {
            Debug.LogWarning($"[CameraHelper] Location '{locationId}' not found!");
            onComplete?.Invoke();
            return;
        }
        
        MoveToLocation(location, duration, onComplete);
    }
    
    /// <summary>
    /// Move camera smoothly to a NamedLocation
    /// </summary>
    public void MoveToLocation(NamedLocation location, float duration = 1.5f, System.Action onComplete = null)
    {
        if (location == null)
        {
            Debug.LogWarning("[CameraHelper] Location is null!");
            onComplete?.Invoke();
            return;
        }
        
        Vector3 targetPos = location.transform.position + location.cameraOffset;
        Quaternion targetRot = Quaternion.Euler(location.cameraRotation);
        
        MoveToPosition(targetPos, targetRot, Camera.main.orthographicSize, duration, onComplete);
        Debug.Log($"[CameraHelper] Moving to location: {location.DisplayName}");
    }
    
    /// <summary>
    /// Move camera smoothly to a specific position and rotation
    /// </summary>
    public void MoveToPosition(Vector3 position, Quaternion rotation, float zoom, float duration = 1.5f, System.Action onComplete = null)
    {
        if (Camera.main == null)
        {
            Debug.LogWarning("[CameraHelper] Main camera not found!");
            onComplete?.Invoke();
            return;
        }
        
        // Store start state
        movementStartPosition = Camera.main.transform.position;
        movementStartRotation = Camera.main.transform.rotation;
        movementStartZoom = Camera.main.orthographicSize;
        
        // Store target state
        targetPosition = position;
        targetRotation = rotation;
        targetZoom = zoom;
        
        // Setup movement
        movementDuration = duration;
        movementProgress = 0f;
        isMovingToLocation = true;
        onMovementComplete = onComplete;
    }
    
    /// <summary>
    /// Stop camera movement immediately
    /// </summary>
    public void StopMovement()
    {
        isMovingToLocation = false;
        movementProgress = 0f;
        onMovementComplete?.Invoke();
        onMovementComplete = null;
    }
    
    /// <summary>
    /// Current follow target (null if not following)
    /// </summary>
    public Transform FollowTarget => _followTarget;
    
    /// <summary>
    /// Is camera currently following a target?
    /// </summary>
    public bool IsFollowing => _isFollowing;
    
    /// <summary>
    /// Set a target for the camera to follow
    /// </summary>
    public void SetFollowTarget(Transform target, float smoothing = 0.3f)
    {
        _followTarget = target;
        _followSmoothing = smoothing;
        _isFollowing = target != null;
        _followVelocity = Vector3.zero;
        
        if (_isFollowing)
        {
            Debug.Log($"[CameraHelper] Now following: {target.name}");
        }
    }
    
    /// <summary>
    /// Stop following the current target
    /// </summary>
    public void StopFollowing()
    {
        _isFollowing = false;
        _followTarget = null;
        _followVelocity = Vector3.zero;
        Debug.Log("[CameraHelper] Stopped following target");
    }
    
    private void HandleFollowTarget()
    {
        if (Camera.main == null || _followTarget == null) return;
        
        // Calculate target camera position (keep current Z offset)
        Vector3 currentPos = Camera.main.transform.position;
        Vector3 targetPos = new Vector3(
            _followTarget.position.x,
            _followTarget.position.y,
            currentPos.z // Keep Z fixed
        );
        
        // Smooth follow
        Vector3 newPos = Vector3.SmoothDamp(currentPos, targetPos, ref _followVelocity, _followSmoothing);
        
        // Apply clamping if not unlimited
        if (!unlimitedMovement)
        {
            newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
            newPos.y = Mathf.Clamp(newPos.y, minY, maxY);
        }
        newPos.z = fixedZ;
        
        Camera.main.transform.position = newPos;
    }
    
    private void HandleSmoothMovement()
    {
        if (Camera.main == null)
        {
            StopMovement();
            return;
        }
        
        movementProgress += Time.deltaTime / movementDuration;
        
        if (movementProgress >= 1f)
        {
            // Movement complete
            Camera.main.transform.position = targetPosition;
            Camera.main.transform.rotation = targetRotation;
            Camera.main.orthographicSize = targetZoom;
            
            isMovingToLocation = false;
            onMovementComplete?.Invoke();
            onMovementComplete = null;
        }
        else
        {
            // Smooth interpolation (ease in-out)
            float t = Mathf.SmoothStep(0f, 1f, movementProgress);
            
            Camera.main.transform.position = Vector3.Lerp(movementStartPosition, targetPosition, t);
            Camera.main.transform.rotation = Quaternion.Slerp(movementStartRotation, targetRotation, t);
            Camera.main.orthographicSize = Mathf.Lerp(movementStartZoom, targetZoom, t);
        }
    }
    
    private NamedLocation FindLocationById(string locationId)
    {
        NamedLocation[] allLocations = FindObjectsOfType<NamedLocation>();
        foreach (var loc in allLocations)
        {
            if (loc.locationId == locationId)
            {
                return loc;
            }
        }
        return null;
    }

    private void OnDrawGizmos()
    {
        if (unlimitedMovement) return;

        Gizmos.color = Color.yellow;
        // Draw a rectangle representing the camera movement limits in X-Y plane
        // Z is constant at fixedZ
        
        // Four corners of the movement bounds
        Vector3 bottomLeft = new Vector3(minX, minY, fixedZ);
        Vector3 bottomRight = new Vector3(maxX, minY, fixedZ);
        Vector3 topLeft = new Vector3(minX, maxY, fixedZ);
        Vector3 topRight = new Vector3(maxX, maxY, fixedZ);
        
        // Draw the rectangle
        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);
        
        // Draw corner markers
        Gizmos.DrawWireSphere(bottomLeft, 1f);
        Gizmos.DrawWireSphere(bottomRight, 1f);
        Gizmos.DrawWireSphere(topLeft, 1f);
        Gizmos.DrawWireSphere(topRight, 1f);
    }
}
