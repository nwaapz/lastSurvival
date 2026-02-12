using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class BaseBuilderClickManager : SingletonMono<BaseBuilderClickManager>, IService
{
    [Header("Click Settings")]
    [SerializeField] private LayerMask clickableLayerMask = -1; // All layers by default
    [SerializeField] private float maxClickDuration = 0.3f; // Max time for a tap vs drag
    [SerializeField] private float dragThreshold = 10f; // Minimum pixel movement to consider drag
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // Events for click handling
    public event Action<Vector3> OnWorldClicked; // World position clicked
    public event Action<GameObject> OnObjectClicked; // Specific GameObject clicked
    public event Action<Vector3, Vector3> OnDragStarted; // Start position, current position
    public event Action<Vector3> OnDragUpdate; // Current drag position
    public event Action<Vector3> OnDragEnded; // End position

    // Filter to block interactions (Scenario Manager can use this)
    public Func<GameObject, bool> InteractionFilter;

    private Vector3 _inputStartPosition;
    private float _inputStartTime;
    private bool _isDragging;
    private bool _isInputDown;
    private Camera _mainCamera;

    protected override void Awake()
    {
        base.Awake();
        
        // Self-register with ServiceLocator
        if (ServiceLocator.HasInstance)
        {
            ServiceLocator.Instance.Register<BaseBuilderClickManager>(this);
        }
    }
    
    protected override void OnDestroy()
    {
        if (ServiceLocator.HasInstance)
        {
            ServiceLocator.Instance.Unregister<BaseBuilderClickManager>();
        }
        base.OnDestroy();
    }

    public void Init()
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            Debug.LogError("[BaseBuilderClickManager] No main camera found!");
        }
        
        if (showDebugLogs)
        {
            Debug.Log("[BaseBuilderClickManager] Initialized successfully.");
        }
    }

    void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        // Handle touch input if available
        if (Input.touchCount > 0)
        {
            HandleTouchInput();
        }
        // Fallback to mouse input (always check this if no touches, allows Editor testing with iOS platform)
        else
        {
            HandleMouseInput();
        }
    }

    private void HandleMouseInput()
    {
        // Mouse button down
        if (Input.GetMouseButtonDown(0))
        {
            // Check if clicking on UI
            if (IsPointerOverUI())
            {
                if (showDebugLogs) Debug.Log("[BaseBuilderClickManager] Clicked on UI, ignoring.");
                return;
            }

            _isInputDown = true;
            _inputStartPosition = Input.mousePosition;
            _inputStartTime = Time.time;
            _isDragging = false;

            if (showDebugLogs) Debug.Log($"[BaseBuilderClickManager] Mouse down at {_inputStartPosition}");
        }

        // Mouse button held
        if (Input.GetMouseButton(0) && _isInputDown)
        {
            Vector3 currentPosition = Input.mousePosition;
            float distance = Vector3.Distance(_inputStartPosition, currentPosition);

            if (!_isDragging && distance > dragThreshold)
            {
                _isDragging = true;
                OnDragStarted?.Invoke(_inputStartPosition, currentPosition);
                
                if (showDebugLogs) Debug.Log($"[BaseBuilderClickManager] Drag started from {_inputStartPosition}");
            }

            if (_isDragging)
            {
                OnDragUpdate?.Invoke(currentPosition);
            }
        }

        // Mouse button up
        if (Input.GetMouseButtonUp(0) && _isInputDown)
        {
            Vector3 currentPosition = Input.mousePosition;
            float clickDuration = Time.time - _inputStartTime;
            float distance = Vector3.Distance(_inputStartPosition, currentPosition);

            if (_isDragging)
            {
                OnDragEnded?.Invoke(currentPosition);
                if (showDebugLogs) Debug.Log($"[BaseBuilderClickManager] Drag ended at {currentPosition}");
            }
            else if (clickDuration <= maxClickDuration && distance < dragThreshold)
            {
                // Valid click/tap
                ProcessClick(currentPosition);
            }

            _isInputDown = false;
            _isDragging = false;
        }
    }

    private void HandleTouchInput()
    {
        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                // Check if touching UI
                if (IsPointerOverUI(touch.fingerId))
                {
                    if (showDebugLogs) Debug.Log("[BaseBuilderClickManager] Touched UI, ignoring.");
                    return;
                }

                _isInputDown = true;
                _inputStartPosition = touch.position;
                _inputStartTime = Time.time;
                _isDragging = false;

                if (showDebugLogs) Debug.Log($"[BaseBuilderClickManager] Touch began at {_inputStartPosition}");
                break;

            case TouchPhase.Moved:
                if (!_isInputDown) break;

                float distance = Vector3.Distance(_inputStartPosition, touch.position);

                if (!_isDragging && distance > dragThreshold)
                {
                    _isDragging = true;
                    OnDragStarted?.Invoke(_inputStartPosition, touch.position);
                    
                    if (showDebugLogs) Debug.Log($"[BaseBuilderClickManager] Drag started from {_inputStartPosition}");
                }

                if (_isDragging)
                {
                    OnDragUpdate?.Invoke(touch.position);
                }
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                if (!_isInputDown) break;

                float tapDuration = Time.time - _inputStartTime;
                float tapDistance = Vector3.Distance(_inputStartPosition, touch.position);

                if (_isDragging)
                {
                    OnDragEnded?.Invoke(touch.position);
                    if (showDebugLogs) Debug.Log($"[BaseBuilderClickManager] Drag ended at {touch.position}");
                }
                else if (tapDuration <= maxClickDuration && tapDistance < dragThreshold)
                {
                    // Valid tap
                    ProcessClick(touch.position);
                }

                _isInputDown = false;
                _isDragging = false;
                break;
        }
    }

    private void ProcessClick(Vector3 screenPosition)
    {
        if (_mainCamera == null)
        {
            Debug.LogError("[BaseBuilderClickManager] No camera available for raycasting!");
            return;
        }

        // Don't process clicks if camera is being dragged
        if (CameraHelper.Instance != null && CameraHelper.Instance.IsDragging)
        {
            if (showDebugLogs)
            {
                Debug.Log("[BaseBuilderClickManager] Click ignored - camera is being dragged");
            }
            return;
        }

        // Pre-check: If we have an interaction filter, do a raycast first to check if click is allowed
        if (InteractionFilter != null)
        {
            GameObject hitObject = GetClickedObject(screenPosition);
            if (!InteractionFilter(hitObject))
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[BaseBuilderClickManager] Click blocked by InteractionFilter (clicked: {(hitObject != null ? hitObject.name : "nothing")})");
                }
                return;
            }
        }

        // Convert screen position to world position
        Ray ray = _mainCamera.ScreenPointToRay(screenPosition);
        
        if (showDebugLogs) 
        {
            Debug.Log($"[BaseBuilderClickManager] Click detected at screen pos: {screenPosition}");
            Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2f);
        }

        // 1. Try 3D Raycast
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, clickableLayerMask))
        {
            if (showDebugLogs)
            {
                Debug.Log($"[BaseBuilderClickManager] Hit 3D object: {hit.collider.gameObject.name} at position {hit.point}");
            }

            OnWorldClicked?.Invoke(hit.point);
            OnObjectClicked?.Invoke(hit.collider.gameObject);
            return;
        }

        // 2. Try 2D Raycast - project ray to Z=0 plane where 2D colliders exist
        Vector2 worldPoint2D;
        if (Mathf.Abs(ray.direction.z) > 0.001f)
        {
            // Calculate where the ray intersects the Z=0 plane
            float t = -ray.origin.z / ray.direction.z;
            Vector3 intersectionPoint = ray.origin + ray.direction * t;
            worldPoint2D = new Vector2(intersectionPoint.x, intersectionPoint.y);
        }
        else
        {
            // Fallback for cameras looking straight down (no Z component in direction)
            worldPoint2D = _mainCamera.ScreenToWorldPoint(screenPosition);
        }
        
        RaycastHit2D hit2D = Physics2D.Raycast(worldPoint2D, Vector2.zero, 0f, clickableLayerMask);

        if (hit2D.collider != null)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[BaseBuilderClickManager] Hit 2D object: {hit2D.collider.gameObject.name} at position {worldPoint2D}");
            }

            // Only fire Object Clicked for 2D items (usually interactables like buildings)
            // Do NOT fire OnWorldClicked to prevent hero movement to this location
            OnObjectClicked?.Invoke(hit2D.collider.gameObject);
            return;
        }

        // 3. Clicked empty space
        Vector3 worldPoint = ray.GetPoint(10f); // 10 units from camera
        
        if (showDebugLogs)
        {
            Debug.Log($"[BaseBuilderClickManager] Clicked empty space at world position: {worldPoint}");
        }

        OnWorldClicked?.Invoke(worldPoint);
    }

    /// <summary>
    /// Check if the pointer/touch is over UI
    /// </summary>
    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>
    /// Check if a specific touch is over UI
    /// </summary>
    private bool IsPointerOverUI(int touchId)
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touchId);
    }

    /// <summary>
    /// Get the object at a screen position (used for interaction filtering)
    /// Checks both 3D and 2D colliders
    /// </summary>
    private GameObject GetClickedObject(Vector3 screenPosition)
    {
        if (_mainCamera == null) return null;

        Ray ray = _mainCamera.ScreenPointToRay(screenPosition);
        
        // Try 3D first
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, clickableLayerMask))
        {
            return hit.collider.gameObject;
        }
        
        // Try 2D
        Vector2 worldPoint2D = _mainCamera.ScreenToWorldPoint(screenPosition);
        RaycastHit2D hit2D = Physics2D.Raycast(worldPoint2D, Vector2.zero, 0f, clickableLayerMask);
        if (hit2D.collider != null)
        {
            return hit2D.collider.gameObject;
        }

        return null;
    }

    /// <summary>
    /// Manually trigger a raycast at a specific screen position
    /// Returns the hit GameObject or null
    /// </summary>
    public GameObject RaycastAtPosition(Vector3 screenPosition)
    {
        if (_mainCamera == null) return null;

        Ray ray = _mainCamera.ScreenPointToRay(screenPosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, clickableLayerMask))
        {
            return hit.collider.gameObject;
        }

        return null;
    }

    /// <summary>
    /// Get world position from screen position at a specific distance
    /// </summary>
    public Vector3 GetWorldPositionFromScreen(Vector3 screenPosition, float distance = 10f)
    {
        if (_mainCamera == null) return Vector3.zero;

        Ray ray = _mainCamera.ScreenPointToRay(screenPosition);
        return ray.GetPoint(distance);
    }

    /// <summary>
    /// Check if currently dragging
    /// </summary>
    public bool IsDragging => _isDragging;

    #region Editor Testing
#if UNITY_EDITOR
    [ContextMenu("Test Click Event")]
    private void TestClickEvent()
    {
        Debug.Log("[BaseBuilderClickManager] Testing click event...");
        OnWorldClicked?.Invoke(Vector3.zero);
        OnObjectClicked?.Invoke(gameObject);
    }
#endif
    #endregion
}
