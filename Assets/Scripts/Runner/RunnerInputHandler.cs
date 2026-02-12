using UnityEngine;

/// <summary>
/// Handles player input for the runner gameplay.
/// Supports keyboard, touch swipe/drag, and UI buttons.
/// For free movement mode: tracks continuous drag to move player pixel-by-pixel.
/// For lane mode: detects discrete swipe gestures to change lanes.
/// </summary>
public class RunnerInputHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RunnerPlayerController playerController;
    [SerializeField] private RunnerSquadManager squadManager;
    [SerializeField] private Camera mainCamera;
    
    [Header("Keyboard Settings")]
    [SerializeField] private KeyCode leftKey = KeyCode.A;
    [SerializeField] private KeyCode rightKey = KeyCode.D;
    [SerializeField] private KeyCode altLeftKey = KeyCode.LeftArrow;
    [SerializeField] private KeyCode altRightKey = KeyCode.RightArrow;
    
    [Header("Touch/Swipe Settings (Lane Mode)")]
    [SerializeField] private float minSwipeDistance = 50f;
    [SerializeField] private float maxSwipeTime = 0.5f;
    
    [Header("Drag Settings (Free Movement Mode)")]
    [Tooltip("Sensitivity multiplier for converting screen-space drag to world movement")]
    [SerializeField] private float dragSensitivity = 1f;
    
    [Tooltip("Minimum drag distance in pixels before movement starts")]
    [SerializeField] private float minDragThreshold = 5f;
    
    [Header("Input Options")]
    [SerializeField] private bool enableTouchInput = true;
    [SerializeField] private bool enableKeyboardInFreeMode = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    // Touch/drag tracking
    private Vector2 _touchStartPos;
    private Vector2 _lastTouchPos;
    private float _touchStartTime;
    private bool _isTouching;
    private bool _isDragging;
    
    // Input state
    private bool _inputEnabled = true;
    
    // Keyboard movement for free mode
    private float _keyboardMoveSpeed = 5f;

    private void Start()
    {
        // Try to find squad manager first (it will spawn the players)
        if (squadManager == null)
        {
            squadManager = FindObjectOfType<RunnerSquadManager>();
        }
        
        if (squadManager != null)
        {
            Debug.Log("[RunnerInputHandler] Found RunnerSquadManager - will route input to squad");
            // Get leader for UseFreeMovement check
            StartCoroutine(WaitForSquadLeader());
        }
        else
        {
            // No squad - use single player controller
            if (playerController == null)
            {
                playerController = FindObjectOfType<RunnerPlayerController>();
            }
            
            if (playerController == null)
            {
                Debug.LogError("[RunnerInputHandler] No RunnerPlayerController found!");
            }
            else
            {
                bool useFreeMovement = playerController.UseFreeMovement;
                Debug.Log($"[RunnerInputHandler] Started. Mode: {(useFreeMovement ? "FREE MOVEMENT" : "LANE-BASED")}");
            }
        }
        
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        if (mainCamera == null)
        {
            Debug.LogWarning("[RunnerInputHandler] No main camera found! Touch-to-world conversion may fail.");
        }
    }
    
    private System.Collections.IEnumerator WaitForSquadLeader()
    {
        // Wait a frame for squad to spawn
        yield return null;
        
        if (squadManager != null)
        {
            playerController = squadManager.GetLeader();
            if (playerController != null)
            {
                bool useFreeMovement = playerController.UseFreeMovement;
                Debug.Log($"[RunnerInputHandler] Squad leader found. Mode: {(useFreeMovement ? "FREE MOVEMENT" : "LANE-BASED")}");
            }
        }
    }

    private void Update()
    {
        if (!_inputEnabled) return;
        
        // Fix: Refresh player reference if current one died
        if (squadManager != null)
        {
             // Check if controller is null (destroyed) or marked as dead
             if (playerController == null || (playerController != null && playerController.CurrentHealth <= 0))
             {
                 var newLeader = squadManager.GetLeader();
                 if (newLeader != null)
                 {
                     playerController = newLeader;
                     Debug.Log($"[RunnerInputHandler] Leader died, switched input to: {playerController.name}");
                 }
             }
        }
        
        // Only allow input when game is Playing
        if (RunnerGameManager.Instance != null)
        {
            if (RunnerGameManager.Instance.CurrentState != RunnerGameManager.GameState.Playing)
            {
                return;
            }
        }
        
        bool useFreeMovement = playerController != null && playerController.UseFreeMovement;
        
        if (useFreeMovement)
        {
            HandleFreeMovementInput();
        }
        else
        {
            HandleLaneBasedInput();
        }

        // Update input state for animation logic
        bool hasInput = _isTouching;
        if (enableKeyboardInFreeMode && useFreeMovement)
        {
            if (Input.GetKey(leftKey) || Input.GetKey(rightKey) || 
                Input.GetKey(altLeftKey) || Input.GetKey(altRightKey))
            {
                hasInput = true;
            }
        }
        
        // Route input state to squad or single player
        if (squadManager != null)
        {
            squadManager.SetSquadInputActive(hasInput);
        }
        else if (playerController != null)
        {
            playerController.SetInputActive(hasInput);
        }
    }

    #region Free Movement Input

    private void HandleFreeMovementInput()
    {
        // Keyboard input for free movement
        if (enableKeyboardInFreeMode)
        {
            HandleKeyboardFreeMovement();
        }
        
        // Touch/mouse drag for free movement
        if (enableTouchInput)
        {
            HandleDragInput();
        }
    }
    
    private void HandleKeyboardFreeMovement()
    {
        float horizontalInput = 0f;
        
        if (Input.GetKey(leftKey) || Input.GetKey(altLeftKey))
        {
            horizontalInput -= 1f;
        }
        
        if (Input.GetKey(rightKey) || Input.GetKey(altRightKey))
        {
            horizontalInput += 1f;
        }
        
        if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            float delta = horizontalInput * _keyboardMoveSpeed * Time.deltaTime;
            ApplyMovementToSquadOrPlayer(delta);
        }
    }
    
    private void HandleDragInput()
    {
        // Handle mouse input (works in editor and for mouse on PC)
        if (Input.GetMouseButtonDown(0))
        {
            StartDrag(Input.mousePosition);
        }
        else if (Input.GetMouseButton(0) && _isTouching)
        {
            UpdateDrag(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0) && _isTouching)
        {
            EndDrag();
        }
        
        // Handle actual touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            switch (touch.phase)
            {
                case UnityEngine.TouchPhase.Began:
                    StartDrag(touch.position);
                    break;
                    
                case UnityEngine.TouchPhase.Moved:
                case UnityEngine.TouchPhase.Stationary:
                    if (_isTouching)
                    {
                        UpdateDrag(touch.position);
                    }
                    break;
                    
                case UnityEngine.TouchPhase.Ended:
                case UnityEngine.TouchPhase.Canceled:
                    if (_isTouching)
                    {
                        EndDrag();
                    }
                    break;
            }
        }
    }
    
    private void StartDrag(Vector2 screenPosition)
    {
        _touchStartPos = screenPosition;
        _lastTouchPos = screenPosition;
        _touchStartTime = Time.time;
        _isTouching = true;
        _isDragging = false;
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerInputHandler] Drag started at {screenPosition}");
        }
    }
    
    private void UpdateDrag(Vector2 screenPosition)
    {
        Vector2 delta = screenPosition - _lastTouchPos;
        
        // Check if we've started dragging (passed threshold)
        if (!_isDragging)
        {
            float totalDragDistance = (screenPosition - _touchStartPos).magnitude;
            if (totalDragDistance >= minDragThreshold)
            {
                _isDragging = true;
            }
            else
            {
                return;
            }
        }
        
        // Convert screen delta to world movement
        float worldDeltaX = ScreenDeltaToWorldDelta(delta.x);
        
        if (Mathf.Abs(worldDeltaX) > 0.0001f)
        {
            ApplyMovementToSquadOrPlayer(worldDeltaX * dragSensitivity);
            
            if (showDebugLogs)
            {
                Debug.Log($"[RunnerInputHandler] Drag delta: screen={delta.x:F2}, world={worldDeltaX:F4}");
            }
        }
        
        _lastTouchPos = screenPosition;
    }
    
    private void EndDrag()
    {
        _isTouching = false;
        _isDragging = false;
        
        // Return to idle animation when touch ends
        if (playerController != null)
        {
            playerController.TriggerIdle();
        }
        
        if (showDebugLogs)
        {
            Debug.Log("[RunnerInputHandler] Drag ended - returning to idle");
        }
    }
    
    /// <summary>
    /// Convert screen-space horizontal delta to world-space delta
    /// </summary>
    private float ScreenDeltaToWorldDelta(float screenDeltaX)
    {
        if (mainCamera == null) return screenDeltaX * 0.01f;
        
        // Get world positions at player's Z depth
        float playerZ = playerController != null ? playerController.transform.position.z : 0f;
        
        Vector3 screenLeft = new Vector3(0, Screen.height / 2f, mainCamera.WorldToScreenPoint(
            new Vector3(0, 0, playerZ)).z);
        Vector3 screenRight = new Vector3(Screen.width, Screen.height / 2f, screenLeft.z);
        
        Vector3 worldLeft = mainCamera.ScreenToWorldPoint(screenLeft);
        Vector3 worldRight = mainCamera.ScreenToWorldPoint(screenRight);
        
        float screenWidth = Screen.width;
        float worldWidth = worldRight.x - worldLeft.x;
        
        // Calculate ratio and apply to delta
        float ratio = worldWidth / screenWidth;
        return screenDeltaX * ratio;
    }

    #endregion

    #region Lane-Based Input

    private void HandleLaneBasedInput()
    {
        HandleKeyboardInput();
        
        if (enableTouchInput)
        {
            HandleTouchInput();
        }
    }
    
    private void HandleKeyboardInput()
    {
        // Left strafe
        if (Input.GetKeyDown(leftKey) || Input.GetKeyDown(altLeftKey))
        {
            TriggerStrafeLeft();
        }
        
        // Right strafe
        if (Input.GetKeyDown(rightKey) || Input.GetKeyDown(altRightKey))
        {
            TriggerStrafeRight();
        }
    }
    
    private void HandleTouchInput()
    {
        // Handle mouse as touch for editor testing
        if (Input.GetMouseButtonDown(0))
        {
            StartTouch(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(0) && _isTouching)
        {
            EndTouch(Input.mousePosition);
        }
        
        // Handle actual touch input
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            switch (touch.phase)
            {
                case UnityEngine.TouchPhase.Began:
                    StartTouch(touch.position);
                    break;
                    
                case UnityEngine.TouchPhase.Ended:
                case UnityEngine.TouchPhase.Canceled:
                    if (_isTouching)
                    {
                        EndTouch(touch.position);
                    }
                    break;
            }
        }
    }
    
    private void StartTouch(Vector2 position)
    {
        _touchStartPos = position;
        _touchStartTime = Time.time;
        _isTouching = true;
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerInputHandler] Touch started at {position}");
        }
    }
    
    private void EndTouch(Vector2 position)
    {
        _isTouching = false;
        
        float swipeTime = Time.time - _touchStartTime;
        if (swipeTime > maxSwipeTime)
        {
            if (showDebugLogs)
            {
                Debug.Log("[RunnerInputHandler] Swipe too slow, ignored");
            }
            return;
        }
        
        Vector2 swipeDelta = position - _touchStartPos;
        float swipeDistance = swipeDelta.magnitude;
        
        if (swipeDistance < minSwipeDistance)
        {
            if (showDebugLogs)
            {
                Debug.Log("[RunnerInputHandler] Swipe too short, ignored");
            }
            return;
        }
        
        // Determine swipe direction (horizontal only for strafing)
        if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
        {
            if (swipeDelta.x < 0)
            {
                TriggerStrafeLeft();
            }
            else
            {
                TriggerStrafeRight();
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerInputHandler] Swipe detected: {swipeDelta}, Distance: {swipeDistance}");
        }
    }
    
    #endregion

    #region Strafe Actions (Lane Mode)
    
    private void TriggerStrafeLeft()
    {
        if (playerController != null)
        {
            playerController.StrafeLeft();
            
            if (showDebugLogs)
            {
                Debug.Log("[RunnerInputHandler] Strafe Left triggered");
            }
        }
    }
    
    private void TriggerStrafeRight()
    {
        if (playerController != null)
        {
            playerController.StrafeRight();
            
            if (showDebugLogs)
            {
                Debug.Log("[RunnerInputHandler] Strafe Right triggered");
            }
        }
    }
    
    /// <summary>
    /// Called by UI button for left strafe (lane mode only)
    /// </summary>
    public void OnLeftButtonPressed()
    {
        if (playerController != null && playerController.UseFreeMovement)
        {
            // In free movement mode, apply a fixed movement delta
            ApplyMovementToSquadOrPlayer(-0.5f);
        }
        else
        {
            TriggerStrafeLeft();
        }
    }
    
    /// <summary>
    /// Called by UI button for right strafe (lane mode only)
    /// </summary>
    public void OnRightButtonPressed()
    {
        if (playerController != null && playerController.UseFreeMovement)
        {
            // In free movement mode, apply a fixed movement delta
            ApplyMovementToSquadOrPlayer(0.5f);
        }
        else
        {
            TriggerStrafeRight();
        }
    }
    
    /// <summary>
    /// Routes horizontal movement to squad manager (for all members) or single player
    /// </summary>
    private void ApplyMovementToSquadOrPlayer(float deltaX)
    {
        if (squadManager != null)
        {
            squadManager.ApplySquadMovement(deltaX);
        }
        else if (playerController != null)
        {
            playerController.ApplyHorizontalMovement(deltaX);
        }
    }
    
    #endregion

    #region Public Methods
    
    /// <summary>
    /// Enable or disable input handling
    /// </summary>
    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
        
        if (!enabled)
        {
            // Reset touch state when disabling
            _isTouching = false;
            _isDragging = false;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerInputHandler] Input {(enabled ? "enabled" : "disabled")}");
        }
    }
    
    /// <summary>
    /// Set drag sensitivity for free movement mode
    /// </summary>
    public void SetDragSensitivity(float sensitivity)
    {
        dragSensitivity = Mathf.Max(0.1f, sensitivity);
    }
    
    #endregion
}
