using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the player character in dodge/runner mode.
/// Player stands in place and strafes left/right to avoid incoming enemies.
/// Supports both free horizontal movement (pixel-by-pixel via swipe) and lane-based strafing.
/// </summary>
public class RunnerPlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float strafeDuration = 0.2f;
    [SerializeField] private AnimationCurve strafeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string strafeLeftTrigger = "StrafeLeft";
    [SerializeField] private string strafeRightTrigger = "StrafeRight";
    [SerializeField] private string idleTrigger = "Idle";
    [SerializeField] private float stopDelayLength = 0.6f;
    [SerializeField] private string dieTrigger = "die";
    
    // ... at the top ...
    [Header("IK Settings")]
    [Tooltip("Enable IK pass for shooting animation")]
    [SerializeField] private bool enableIK = true;
    [Tooltip("Layer index for shooting (should match Animator)")]
    [SerializeField] private int shootingLayerIndex = 1;
    [Tooltip("Target position offset relative to player for Looking/Aiming (Local Space)")]
    [SerializeField] private Vector3 lookAtTargetOffset = new Vector3(0, 1.0f, 10f);
    [Tooltip("Global weight for LookAt IK")]
    [Range(0, 1)]
    [SerializeField] private float lookAtWeight = 1.0f;
    [Tooltip("Body weight for LookAt IK (0 = head only, 1 = full body including arms)")]
    [Range(0, 1)]
    [SerializeField] private float lookAtBodyWeight = 0.8f;
    [Tooltip("Head weight for LookAt IK")]
    [Range(0, 1)]
    [SerializeField] private float lookAtHeadWeight = 1.0f;

    // ... existing fields ...
    [Tooltip("Which hand to affect")]
    [SerializeField] private AvatarIKGoal ikHand = AvatarIKGoal.RightHand;
    [Tooltip("Offset to apply to the hand position (Y = height)")]
    [SerializeField] private Vector3 ikHandOffset = new Vector3(0, -0.3f, 0);
    [Range(0, 1)]
    [SerializeField] private float ikWeight = 1f; // Enabled for dual-hand lowering
    
    // ...


    
    [Header("Directional Blend Animation")]
    [Tooltip("Enable blend tree based on dot product (strafe vs walk toward enemy)")]
    [SerializeField] private bool useDirectionalBlend = true;
    [Tooltip("Animator float parameter for blend weight (0=strafe, 1=walk forward)")]
    [SerializeField] private string blendParameter = "MoveBlend";
    [Tooltip("Animator float parameter for movement direction (-1=left, 1=right)")]
    [SerializeField] private string directionParameter = "MoveDirection";
    [Tooltip("Animator bool parameter for whether player is moving")]
    [SerializeField] private string isMovingParameter = "IsMoving";
    [Tooltip("How fast the blend parameter transitions")]
    [SerializeField] private float blendSmoothSpeed = 3f;
    
    [Header("Visual Feedback")]
    [SerializeField] private ParticleSystem strafeParticles;
    [SerializeField] private DamageFlash damageFlash;
    
    [Header("Rotation")]
    [Tooltip("If true, only rotates on Y axis (keeps player upright)")]
    [SerializeField] private bool lockYAxisOnly = true;
    [Tooltip("Compensate for mesh rotation offset during gameplay (e.g. 45 or -45)")]
    [SerializeField] private float modelRotationOffset = 0f;
    [Tooltip("Rotation offset on Y axis applied when game is NOT playing (idle pose before start)")]
    [SerializeField] private float idleRotationOffset = 0f;
    
    [Header("Movement Control")]
    [Tooltip("If true, movement is clamped to lane configuration bounds.")]
    [SerializeField] private bool clampToLaneBounds = true;
    
    [Header("Zombie Detection")]
    [Tooltip("Enable turning toward nearest zombie")]
    [SerializeField] private bool detectZombies = true;
    
    [Tooltip("Maximum distance to detect zombies")]
    [SerializeField] private float zombieDetectionRange = 30f;
    [Tooltip("How fast to rotate toward zombie")]
    [SerializeField] private float rotationSpeed = 5f;
    
    [Header("Weapon Configs")]
    [SerializeField] private Configs.WeaponConfig startingWeaponConfig;
    [SerializeField] private Configs.WeaponConfig machineGunConfig;
    
    [Header("Shooting Components")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private string shootTrigger = "shoot";
    [SerializeField] private string isShootingBool = "IsShooting";
    
    [Header("Machine Gun Animation")]
    [Tooltip("Trigger parameter for machine gun animation")]
    [SerializeField] private string machineGunTrigger = "machineGun";
    [Tooltip("Bool parameter for machine gun shooting state")]
    [SerializeField] private string isMachineGunBool = "IsMachineGun";
    [Tooltip("Rotation offset when in machine gun mode")]
    [SerializeField] private float machineGunRotationOffset = 0f;
    [Tooltip("Rotation offset when in regular (pistol) mode")]
    [SerializeField] private float regularRotationOffset = -45f;

    // Runtime state
    private Configs.WeaponConfig _currentWeaponConfig;
    private float _currentDamage;
    private float _currentFireRate;
    private float _currentRange;
    private GameObject _currentWeaponModel;
    private Transform _currentMuzzlePoint;
    private ParticleSystem _currentMuzzleFlash;
    
    // Track machine gun mode state
    private bool _isMachineGunMode = false;
    public bool IsMachineGunMode => _isMachineGunMode;

    // Base Modifiers (Percentage based, 0 = 0%)
    private float _damageModifier = 0f;
    private float _fireRateModifier = 0f;
    private float _rangeModifier = 0f;
    private int _bulletCountModifier = 0;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float _currentHealth;

    [Header("Healthbar UI")]
    [SerializeField] private Image playerHealthBar;
    [SerializeField] private Camera mainCamera;
    [Tooltip("Transform position where the healthbar will be anchored. If not set, defaults to player position + 2 units up.")]
    [SerializeField] private Transform healthbarAnchor;
    private GameObject _instantiatedHealthbar;
    private RectTransform _healthbarRectTransform;
    
    public float MaxHealth => maxHealth;
    public float CurrentHealth => _currentHealth;
    
    // Events
    public event Action<int> OnLaneChanged;
    public event Action OnStrafeStarted;
    public event Action OnStrafeCompleted;
    public event Action<float> OnPositionChanged; // For free movement mode
    public event Action<float> OnHealthChanged;
    
    // Lane-based movement state
    private int _currentLane;
    private int _targetLane;
    private bool _isStrafing;
    private float _strafeProgress;
    private float _strafeStartX;
    private float _strafeTargetX;
    
    // Free movement state
    private float _targetXPosition;
    private float _currentVelocity;
    private bool _isFreeMoving;
    private int _lastStrafeDirection; // -1 = left, 0 = none, 1 = right
    
    // Directional blend state
    private float _currentBlendValue; // 0 = pure strafe, 1 = walk toward/away
    private float _currentDirectionValue; // -1 = left, 1 = right
    private float _targetBlendValue;
    private float _targetDirectionValue;
    private bool _isCurrentlyMoving;
    private bool _hasInput;
    private float _stopDelayTimer;
    private float _nextFireTime;
    private IShootableTarget _currentTarget;
    
    // Properties
    public int CurrentLane => _currentLane;
    public bool IsStrafing => _isStrafing;
    public float CurrentXPosition => transform.position.x;
    public bool UseFreeMovement => _laneConfig != null && _laneConfig.UseFreeMovement;
    public float BulletDamage => _currentDamage;
    
    private RunnerLaneConfig _laneConfig;
    private bool _isDead;
    
    // Squad formation offset (set by RunnerSquadManager)
    private float _squadXOffset = 0f;
    private Vector3 _squadOffset = Vector3.zero;
    
    /// <summary>
    /// Returns true if this player is the squad leader (has offset 0,0,0).
    /// Only the leader should trigger gates.
    /// </summary>
    public bool IsLeader => _squadOffset == Vector3.zero;
    
    /// <summary>
    /// Set the squad formation X offset. This is added to all position calculations.
    /// </summary>
    public void SetSquadOffset(float xOffset)
    {
        _squadXOffset = xOffset;
    }
    
    /// <summary>
    /// Set the full squad formation offset (used for IsLeader check).
    /// </summary>
    public void SetSquadOffset(Vector3 offset)
    {
        _squadOffset = offset;
        _squadXOffset = offset.x;
    }
    
    /// <summary>
    /// Get current velocity for external systems to query movement state
    /// </summary>
    public float CurrentVelocity => _currentVelocity;
    
    /// <summary>
    /// Sync movement state from another controller (e.g., squad leader).
    /// Call this immediately after spawning to prevent fast "catch up" movement.
    /// </summary>
    /// <param name="velocity">The velocity to sync to</param>
    /// <param name="targetX">The target X position (will have offset applied internally)</param>
    public void SyncMovementState(float velocity, float targetX)
    {
        _currentVelocity = velocity;
        _targetXPosition = targetX;
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerPlayerController] Synced movement state: velocity={velocity:F2}, targetX={targetX:F2}");
        }
    }

    private void Start()
    {
        // Auto-assign camera if not set
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Instantiate healthbar from RunnerGameManager
        if (RunnerGameManager.Instance != null)
        {
            if (RunnerGameManager.Instance.healthbar != null && RunnerGameManager.Instance.Canvas != null)
            {
                // Instantiate the healthbar prefab
                _instantiatedHealthbar = Instantiate(RunnerGameManager.Instance.healthbar);
                
                // Parent it under the Canvas
                _instantiatedHealthbar.transform.SetParent(RunnerGameManager.Instance.Canvas, false);
                
                // Get the RectTransform for positioning
                _healthbarRectTransform = _instantiatedHealthbar.GetComponent<RectTransform>();
                
                // Get the Image component and assign to playerHealthBar
                playerHealthBar = _instantiatedHealthbar.GetComponent<Image>();
                if (playerHealthBar == null)
                {
                    // If healthbar is not directly an Image, try to find it in children
                    playerHealthBar = _instantiatedHealthbar.GetComponentInChildren<Image>();
                }
                
                // Initial position update
                UpdateHealthbarPosition();
                
                // Start with healthbar hidden - will show when player takes damage
                _instantiatedHealthbar.SetActive(false);
                
                Debug.Log("[RunnerPlayerController] Healthbar instantiated (hidden until damage taken)");
            }
            else
            {
                Debug.LogWarning("[RunnerPlayerController] RunnerGameManager is missing healthbar prefab or Canvas reference!");
            }
        }
        
        if (startingWeaponConfig != null)
        {
            EquipWeapon(startingWeaponConfig);
        }
        else
        {
            Debug.LogError("[RunnerPlayerController] No Starting Weapon Config assigned!");
        }

        Initialize();
    }


    private void Update()
    {
        if (_isDead) return;

        if (UseFreeMovement)
        {
            UpdateFreeMovement();
        }
        else if (_isStrafing)
        {
            UpdateStrafe();
        }
        
        // Always face the look at target if assigned
        UpdateLookAtTarget();
        UpdateShooting();
        
        // Update directional blend animation parameters
        if (useDirectionalBlend)
        {
            UpdateDirectionalBlend();
        }

        if(Input.GetKeyDown(KeyCode.T))
        {
            animator.ResetTrigger(shootTrigger);
            animator.ResetTrigger(idleTrigger);
            animator.SetTrigger(idleTrigger);
        }
    }

    /// <summary>
    /// Set whether movement should be clamped to lane bounds
    /// </summary>
    public void SetClampToLaneBounds(bool enableClamping)
    {
        clampToLaneBounds = enableClamping;
        
        // Also skip clamping in next update if disabling
        if (!enableClamping && _isFreeMoving)
        {
            // Just ensure target is still valid
        }
    }

    private void LateUpdate()
    {
        // Update healthbar position to follow player
        if (_healthbarRectTransform != null)
        {
            UpdateHealthbarPosition();
        }
    }

    /// <summary>
    /// Initialize the player controller
    /// </summary>
    public void Initialize()
    {
        // Get lane config from game manager
        if (RunnerGameManager.Instance != null)
        {
            _laneConfig = RunnerGameManager.Instance.LaneConfig;
        }
        
        // Create default config if not found
        if (_laneConfig == null)
        {
            Debug.LogWarning("[RunnerPlayerController] No LaneConfig found! Creating default config.");
            _laneConfig = ScriptableObject.CreateInstance<RunnerLaneConfig>();
        }
        
        // Auto-assign animator if missing
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogError("[RunnerPlayerController] No Animator assigned or found in children!");
            }
        }
        
        if (_laneConfig.UseFreeMovement)
        {
            // Free movement mode - use current spawn position (set by SquadManager)
            // This respects where the player was spawned rather than overriding it
            _targetXPosition = transform.position.x;
            
            Debug.Log($"[RunnerPlayerController] Initialized in FREE MOVEMENT mode. Bounds: [{_laneConfig.MinXPosition}, {_laneConfig.MaxXPosition}], Starting X={_targetXPosition}, SquadOffset={_squadXOffset}");
        }
        else
        {
            // Lane-based mode
            _currentLane = _laneConfig.StartingLane;
            _targetLane = _currentLane;
            SnapToLane(_currentLane);
            
            Debug.Log($"[RunnerPlayerController] Initialized in LANE mode. Lane count: {_laneConfig.LaneCount}, Starting lane: {_currentLane}");
        }
        
        // Find animator if not assigned
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                Debug.Log($"[RunnerPlayerController] Found Animator: {animator.name}");
            }
        }
        
        // Disable root motion to prevent animation overriding script rotation
        if (animator != null)
        {
            animator.applyRootMotion = false;
            
            // Critical fix: Ensure death animation plays even if camera moves away or character is off-screen
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            
            // IK Diagnostic: Check if Avatar is humanoid (required for OnAnimatorIK)
            if (animator.avatar != null && animator.avatar.isHuman)
            {
                Debug.Log($"[IK DIAGNOSTIC] Animator has HUMANOID avatar. OnAnimatorIK should work. enableIK={enableIK}");
            }
            else
            {
                Debug.LogError($"[IK DIAGNOSTIC] Animator avatar is NOT HUMANOID or is NULL! OnAnimatorIK will NOT be called. Avatar={(animator.avatar != null ? animator.avatar.name : "NULL")}");
            }
        }
        else
        {
            Debug.LogError("[IK DIAGNOSTIC] Animator is NULL after initialization!");
        }

        // Setting the Health
        _currentHealth = maxHealth;
        OnHealthChanged?.Invoke(_currentHealth);
        
        if (playerHealthBar != null)
        {
            playerHealthBar.fillAmount = _currentHealth / MaxHealth;
        }
    }

    /// <summary>
    /// Take damage from enemies
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (_isDead) return;

        _currentHealth -= damage;
        
        if (_instantiatedHealthbar != null && !_instantiatedHealthbar.activeSelf)
        {
            _instantiatedHealthbar.SetActive(true);
        }
        
        if (playerHealthBar != null)
        {
            playerHealthBar.fillAmount = _currentHealth / MaxHealth;
        }
        
        // Flash effect when taking damage
        if (damageFlash != null)
        {
            damageFlash.Flash();
        }
        
        OnHealthChanged?.Invoke(_currentHealth);

        if (_currentHealth <= 0)
        {
             _isDead = true;
             detectZombies = false;
             _currentTarget = null;
             
             // Stop all movement state
             _isCurrentlyMoving = false;
             _isFreeMoving = false;
             _isStrafing = false;
             _targetBlendValue = 0f;
             
             // Handle death (notify GameManager)
             Debug.Log("[RunnerPlayerController] Player Died!");
             
             if (animator != null)
             {
                 // Reset movement parameters to ensure walk animation stops
                 if (!string.IsNullOrEmpty(isMovingParameter)) animator.SetBool(isMovingParameter, false);
                 if (!string.IsNullOrEmpty(blendParameter)) animator.SetFloat(blendParameter, 0f);
                 
                 // Reset ALL movement triggers to prevent overrides
                 if (!string.IsNullOrEmpty(strafeLeftTrigger)) animator.ResetTrigger(strafeLeftTrigger);
                 if (!string.IsNullOrEmpty(strafeRightTrigger)) animator.ResetTrigger(strafeRightTrigger);
                 if (!string.IsNullOrEmpty(idleTrigger)) animator.ResetTrigger(idleTrigger);
                 
                 // clear any pending shoot triggers
                 if (!string.IsNullOrEmpty(shootTrigger))
                 {
                     animator.ResetTrigger(shootTrigger);
                 }
                 
                 // Trigger death animation
                 if (!string.IsNullOrEmpty(dieTrigger))
                 {
                     animator.SetTrigger(dieTrigger);
                     // Also try Play to force state change immediately if transition is blocked
                     // animator.Play("Die"); // Uncomment if state name is known to be "Die" or "Death"
                 }
                 
                 // Disable upper body layers so death animation (usually full body) plays correctly
                 for (int i = 1; i < animator.layerCount; i++)
                 {
                     animator.SetLayerWeight(i, 0f);
                 }
             }
             
             // Destroy the object after 4 seconds
             StartCoroutine(WaitAndDestroy(4f));

             // Only end game directly if NOT managed by a RunnerSquadManager
             // Squad manager handles game over when all members die
             RunnerSquadManager squadManager = FindObjectOfType<RunnerSquadManager>();
             if (squadManager == null && RunnerGameManager.Instance != null)
             {
                 // Solo player - end game immediately
                 RunnerGameManager.Instance.MakeAllZombiesIdle();
                 RunnerGameManager.Instance.EndGame();
             }
             // If squad manager exists, it will handle the squad wipe logic via OnHealthChanged event
         }
    }

    /// <summary>
    /// Update healthbar position in screen space based on player's world position
    /// </summary>
    private void UpdateHealthbarPosition()
    {
        if (_healthbarRectTransform == null || mainCamera == null) return;

        // Get world position from anchor transform, or fallback to player position + 2 units up
        Vector3 worldPosition = healthbarAnchor != null 
            ? healthbarAnchor.position 
            : transform.position + Vector3.up * 2f;
        
        // Convert to screen space
        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
        
        // Set the healthbar position
        _healthbarRectTransform.position = screenPosition;
    }

    /// <summary>
    /// Update input state. If false, IsMoving will cut off immediately upon velocity drop or release.
    /// </summary>
    public void SetInputActive(bool active)
    {
        _hasInput = active;
    }

    #region Free Movement

    /// <summary>
    /// Apply horizontal movement delta (called from input handler during drag/swipe)
    /// </summary>
    /// <param name="deltaX">Horizontal movement delta in world units</param>
    public void ApplyHorizontalMovement(float deltaX)
    {
        if (_isDead) return;
        if (!UseFreeMovement) return;
        
        _targetXPosition += deltaX;
        
        // Clamp to bounds
        if (_laneConfig != null)
        {
            _targetXPosition = _laneConfig.ClampXPosition(_targetXPosition);
        }
        
        _isFreeMoving = true;
        
        if (showDebugLogs && Mathf.Abs(deltaX) > 0.01f)
        {
            Debug.Log($"[RunnerPlayerController] Free movement delta: {deltaX:F3}, Target X: {_targetXPosition:F2}");
        }
    }
    
    /// <summary>
    /// Set target X position directly (for UI or programmatic control)
    /// </summary>
    /// <param name="xPosition">Target X position</param>
    /// <param name="skipClamp">If true, skip individual clamping (used by squad manager which handles formation-aware clamping)</param>
    public void SetTargetXPosition(float xPosition, bool skipClamp = false)
    {
        if (_isDead) return;
        if (!UseFreeMovement) return;
        
        if (skipClamp)
        {
            _targetXPosition = xPosition;
        }
        else
        {
            _targetXPosition = _laneConfig != null 
                ? _laneConfig.ClampXPosition(xPosition) 
                : xPosition;
        }
        
        _isFreeMoving = true;
    }
    
    private void UpdateFreeMovement()
    {
        if (_laneConfig == null) return;
        
        float currentX = transform.position.x;
        float smoothTime = _laneConfig.MoveSmoothTime;
        
        // Use SmoothDamp for smooth, natural-feeling movement
        float newX = Mathf.SmoothDamp(currentX, _targetXPosition, ref _currentVelocity, smoothTime);
        
        // Clamp to bounds ONLY if enabled
        if (clampToLaneBounds)
        {
            newX = _laneConfig.ClampXPosition(newX);
        }
        
        // Update IsMoving state based on velocity threshold with delay
        float velocityMag = Mathf.Abs(_currentVelocity);
        
        // Hysteresis: 
        // - To START moving: Need significant velocity (> 0.1f) to ignore noise
        // - To KEEP moving: As long as we have input, even slow movement (> 0.01f) counts
        bool isMovingFastEnough = velocityMag > 0.1f;
        if (_isCurrentlyMoving && _hasInput)
        {
            isMovingFastEnough = velocityMag > 0.01f;
        }
        
        // Only start/keep moving if we have active input
        if (isMovingFastEnough && _hasInput)
        {
            // Moving fast enough AND has input -> IsMoving
            _stopDelayTimer = 0f;
            if (!_isCurrentlyMoving) _isCurrentlyMoving = true;
        }
        else if (_isCurrentlyMoving)
        {
            // Velocity dropped, but wait before turning off IsMoving
            _stopDelayTimer += Time.deltaTime;
            if (_stopDelayTimer > stopDelayLength)
            {
                _isCurrentlyMoving = false;
            }
        }

        // Check if we've effectively reached the target
        if (Mathf.Abs(newX - _targetXPosition) < 0.001f && velocityMag < 0.01f)
        {
            newX = _targetXPosition;
            _currentVelocity = 0f;
            _isFreeMoving = false;
        }
        
        // Apply position
        if (Mathf.Abs(newX - currentX) > 0.0001f)
        {
            SetXPosition(newX);
            OnPositionChanged?.Invoke(newX);
            
            // Determine movement direction
            int newDirection = 0;
            if (_currentVelocity > 0.1f) newDirection = 1;
            else if (_currentVelocity < -0.1f) newDirection = -1;
            
            // Only update animation if direction changed
            if (newDirection != _lastStrafeDirection && newDirection != 0)
            {
                _lastStrafeDirection = newDirection;
                
                if (useDirectionalBlend)
                {
                    CalculateDirectionalBlend(newDirection);
                }
                else
                {
                    TriggerAnimation(newDirection > 0 ? strafeRightTrigger : strafeLeftTrigger);
                }
            }
            // Continuous update for directional blend
            else if (useDirectionalBlend && newDirection != 0)
            {
                CalculateDirectionalBlend(newDirection);
            }
        }
        else
        {
             if (_lastStrafeDirection != 0)
            {
                _lastStrafeDirection = 0;
                if (useDirectionalBlend) StopMovement();
            }
        }
    }
    
    /// <summary>
    /// Update player rotation - Forced to look forward only
    /// </summary>
    private void UpdateLookAtTarget()
    {
        // Always look forward
        Vector3 direction = Vector3.forward;
        _currentTarget = null; // No targeting logic
        
        // Determine which rotation offset to use based on game state
        float currentOffset;
        // Use HasStarted so rotation switches immediately when button is clicked (before delay)
        bool hasGameStarted = RunnerGameManager.HasInstance && RunnerGameManager.Instance.HasStarted;
        
        if (hasGameStarted)
        {
            currentOffset = modelRotationOffset;
        }
        else
        {
            currentOffset = idleRotationOffset;
        }
        
        if (lockYAxisOnly)
        {
            direction.y = 0;
        }
        
        if (direction.sqrMagnitude > 0.001f)
        {
            // Simple forward rotation
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            
            // Apply rotation offset
            if (Mathf.Abs(currentOffset) > 0.01f)
            {
                targetRotation *= Quaternion.Euler(0, currentOffset, 0);
            }
            
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    private bool _isShooting;

    private void UpdateShooting()
    {
        // Only shoot if game is in Playing state
        if (!RunnerGameManager.HasInstance || RunnerGameManager.Instance.CurrentState != RunnerGameManager.GameState.Playing)
        {
            if (_isShooting)
            {
                StopShooting();
            }
            return;
        }
        
        // Game is playing - shoot continuously
        _isShooting = true;
        
        if (Time.time >= _nextFireTime)
        {
            StartShooting();
            float rate = _currentFireRate > 0 ? _currentFireRate : 2f;
            _nextFireTime = Time.time + (1f / rate);
        }
    }

    private void StopShooting()
    {
        print("!!!!STOPPPPPED");
        _isShooting = false;
        
        if (animator != null)
        {
            // Reset shoot trigger to stop any pending shoot animations
            if (!string.IsNullOrEmpty(shootTrigger))
            {
                animator.ResetTrigger(shootTrigger);
            }
            
            // Reset machine gun trigger
            if (!string.IsNullOrEmpty(machineGunTrigger))
            {
                animator.ResetTrigger(machineGunTrigger);
            }
            
            // Set IsShooting bool to false - this is more reliable than triggers
            if (!string.IsNullOrEmpty(isShootingBool))
            {
                animator.SetBool(isShootingBool, false);
                Debug.Log($"[RunnerPlayerController] STOPPED SHOOTING - Set {isShootingBool} = false");
            }
            
            // Set IsMachineGun bool to false
            if (!string.IsNullOrEmpty(isMachineGunBool))
            {
                animator.SetBool(isMachineGunBool, false);
            }
            
            // Also trigger idle animation as backup
            if (!string.IsNullOrEmpty(idleTrigger))
            {
                animator.ResetTrigger(idleTrigger);
                animator.SetTrigger(idleTrigger);
            }
        }
    }

    private void StartShooting()
    {
        if (animator != null)
        {
            // Set IsShooting bool to true
            if (!string.IsNullOrEmpty(isShootingBool))
            {
                animator.SetBool(isShootingBool, true);
            }
            
            // Trigger the appropriate animation based on mode
            if (_isMachineGunMode)
            {
                // Machine gun mode - use machineGun trigger
                if (!string.IsNullOrEmpty(isMachineGunBool))
                {
                    animator.SetBool(isMachineGunBool, true);
                }
                
                if (!string.IsNullOrEmpty(machineGunTrigger))
                {
                    animator.ResetTrigger(idleTrigger);
                    animator.ResetTrigger(shootTrigger);
                    animator.ResetTrigger(machineGunTrigger);
                    animator.SetTrigger(machineGunTrigger);
                }
                
                // Switch weapon models handled by EquipWeapon logic
                
                if (showDebugLogs) Debug.Log($"[RunnerPlayerController] START SHOOTING (Machine Gun Mode)");
            }
            else
            {
                // Regular mode - use shoot trigger
                if (!string.IsNullOrEmpty(shootTrigger))
                {
                    animator.ResetTrigger(idleTrigger);
                    animator.ResetTrigger(machineGunTrigger);
                    animator.ResetTrigger(shootTrigger);
                    animator.SetTrigger(shootTrigger);
                }
                
                // Switch weapon models handled by EquipWeapon logic
                
                if (showDebugLogs) Debug.Log($"[RunnerPlayerController] START SHOOTING (Regular Mode)");
            }
        }
        else
        {
            if (showDebugLogs) Debug.LogWarning("[RunnerPlayerController] Cannot shoot - Animator is null!");
            
            // Fallback if no animator: fire immediately
            Fire();
        }
    }
    
    /// <summary>
    /// Switch between machine gun mode and regular (pistol) mode.
    /// </summary>
    /// <param name="enable">True to enable machine gun mode, false for regular mode</param>
    public void SetMachineGunMode(bool enable)
    {
        _isMachineGunMode = enable;
        
        // Update rotation offset based on mode (Affects character body rotation in Update)
        modelRotationOffset = enable ? machineGunRotationOffset : regularRotationOffset;
        
        // Switch weapon config
        Configs.WeaponConfig targetConfig = enable ? machineGunConfig : startingWeaponConfig;
        if (targetConfig != null)
        {
            EquipWeapon(targetConfig);
        }
        
        if (animator != null)
        {
            // Update animator parameters
            if (!string.IsNullOrEmpty(isMachineGunBool))
            {
                animator.SetBool(isMachineGunBool, enable);
            }
            
            if (enable && !string.IsNullOrEmpty(machineGunTrigger))
            {
                animator.ResetTrigger(shootTrigger);
                animator.SetTrigger(machineGunTrigger);
            }
            else if (!enable && !string.IsNullOrEmpty(shootTrigger))
            {
                animator.ResetTrigger(machineGunTrigger);
                animator.SetTrigger(shootTrigger);
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerPlayerController] Machine Gun Mode: {enable}");
        }
    }
    
    /// <summary>
    /// Equip a weapon from config. Finds the model child, sets stats, and applies current modifiers.
    /// </summary>
    private void EquipWeapon(Configs.WeaponConfig config)
    {
        _currentWeaponConfig = config;
        
        // 1. Find and enable the weapon model
        if (_currentWeaponModel != null)
        {
            _currentWeaponModel.SetActive(false);
        }
        
        _currentMuzzlePoint = null; // Reset muzzle point
        // Destroy old muzzle flash if exists
        if (_currentMuzzleFlash != null)
        {
            Destroy(_currentMuzzleFlash.gameObject);
            _currentMuzzleFlash = null;
        }

        if (!string.IsNullOrEmpty(config.weaponModelName))
        {
            Transform foundModel = FindRecursive(transform, config.weaponModelName); 
            
            if (foundModel != null)
            {
                _currentWeaponModel = foundModel.gameObject;
                _currentWeaponModel.SetActive(true);
                
                // Try to find the muzzle point inside the model
                if (!string.IsNullOrEmpty(config.muzzlePointName))
                {
                    _currentMuzzlePoint = FindRecursive(_currentWeaponModel.transform, config.muzzlePointName);
                }
            }
            else
            {
                Debug.LogWarning($"[RunnerPlayerController] Could not find weapon model child named: {config.weaponModelName}");
            }
        }
        
        // Instantiate Muzzle Flash if found
        if (config.muzzleFlashPrefab != null)
        {
            Transform flashParent = _currentMuzzlePoint != null ? _currentMuzzlePoint : (firePoint != null ? firePoint : transform);
            
            GameObject flashObj = Instantiate(config.muzzleFlashPrefab, flashParent.position, flashParent.rotation);
            flashObj.transform.SetParent(flashParent);
            flashObj.transform.localPosition = Vector3.zero;
            flashObj.transform.localRotation = Quaternion.identity;
            
            _currentMuzzleFlash = flashObj.GetComponent<ParticleSystem>();
            if (_currentMuzzleFlash == null)
            {
                _currentMuzzleFlash = flashObj.GetComponentInChildren<ParticleSystem>();
            }
        }
        
        // 2. Set Stats (Base + Modifiers)
        RecalculateStats();
    }
    
    private void RecalculateStats()
    {
        if (_currentWeaponConfig == null) return;
        
        // Apply percentage modifiers: Base * (1 + Modifier/100)
        _currentDamage = _currentWeaponConfig.damage + (_currentWeaponConfig.damage * _damageModifier / 100f);
        _currentFireRate = _currentWeaponConfig.fireRate + (_currentWeaponConfig.fireRate * _fireRateModifier / 100f);
        _currentRange = _currentWeaponConfig.bulletRange + (_currentWeaponConfig.bulletRange * _rangeModifier / 100f);
        
        // Clamp values
        _currentDamage = Mathf.Max(1f, _currentDamage);
        _currentFireRate = Mathf.Clamp(_currentFireRate, 0.1f, 20f);
        _currentRange = Mathf.Clamp(_currentRange, 5f, 100f);
        
        // Update detection range
        SetDetectionRange(_currentRange);
        
        if (showDebugLogs)
        {
            Debug.Log($"[Stats] Damage: {_currentDamage}, Rate: {_currentFireRate}, Range: {_currentRange}");
        }
    }

    /// <summary>
    /// Called by Animation Event "Fire"
    /// </summary>
    public void Fire()
    {
        if (_isDead) return;
        if (!_isShooting) return;
        if (_currentWeaponConfig == null) return;
        
        GameObject projectilePrefab = _currentWeaponConfig.projectilePrefab;
        
        if (projectilePrefab != null)
        {
            // Use Muzzle Point if available, else FirePoint, else Default
            Transform spawnTransform = _currentMuzzlePoint != null ? _currentMuzzlePoint : (firePoint != null ? firePoint : transform);
            Vector3 baseSpawnPos = spawnTransform.position;
            // If fallback to transform, adjust up/forward
            if (_currentMuzzlePoint == null && firePoint == null)
            {
                baseSpawnPos += Vector3.up + transform.forward;
            }
            
            // Calculate rotation based on mode to correct bullet direction
            float currentRotationOffset = _isMachineGunMode ? machineGunRotationOffset : regularRotationOffset;
            Quaternion spawnRot = transform.rotation * Quaternion.Euler(0, -currentRotationOffset, 0);
            
            // Ensure pool exists
            if (RunnerProjectilePool.Instance == null)
            {
                GameObject poolObj = new GameObject("RunnerProjectilePool");
                poolObj.AddComponent<RunnerProjectilePool>();
            }

            // Multishot Logic
            int count = _currentWeaponConfig.bulletCount + _bulletCountModifier;
            float spacing = _currentWeaponConfig.bulletSpacing;
            
            // Ensure at least 1 bullet
            if (count < 1) count = 1;
            
            // Helper to get right direction for spacing
            // Since we rotate the bullet spawnRot, we should use that rotation's right vector for spacing
            Vector3 spacingDir = spawnRot * Vector3.right;

            for (int i = 0; i < count; i++)
            {
                // Calculate offset: centered distribution
                // 1 bullet: offset 0
                // 2 bullets: -0.5, +0.5
                // 3 bullets: -1, 0, +1
                float offsetMultiplier = i - (count - 1) / 2f;
                Vector3 spawnOffset = spacingDir * (offsetMultiplier * spacing);
                Vector3 finalSpawnPos = baseSpawnPos + spawnOffset;

                // Get projectile from pool
                RunnerProjectile projectile = RunnerProjectilePool.Instance.GetProjectile(finalSpawnPos, spawnRot, projectilePrefab);
                
                if (projectile != null)
                {
                    projectile.SetDamage(_currentDamage);
                    
                    float projSpeed = 20f; 
                    float lifetime = _currentRange / projSpeed;
                    projectile.Activate(lifetime);
                }
            }
            
            // Play Audio
            FightSceneSfxManager.Instance.PlayWeaponSfx(_currentWeaponConfig.shootAudio, _currentWeaponConfig.audioVolume);
            
            // Muzzle Flash
            if (_currentMuzzleFlash != null)
            {
                _currentMuzzleFlash.Play();
            }
        }
    }
    
    /// <summary>
    /// Find the nearest active shootable target (zombie, barrel, etc.) within detection range
    /// </summary>
    private IShootableTarget FindNearestTarget()
    {
        IShootableTarget nearest = null;
        float nearestDistance = zombieDetectionRange;
        
        // Find all MonoBehaviours that implement IShootableTarget
        MonoBehaviour[] allBehaviours = FindObjectsOfType<MonoBehaviour>();
        
        foreach (var behaviour in allBehaviours)
        {
            IShootableTarget target = behaviour as IShootableTarget;
            if (target == null || !target.IsActive) continue;
            
            // Ignore targets behind the player
            if (target.TargetTransform.position.z < transform.position.z) continue;
            
            float distance = Vector3.Distance(transform.position, target.TargetTransform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = target;
            }
        }
        
        return nearest;
    }

    /// <summary>
    /// Set the zombie detection range (shooting range)
    /// </summary>
    public void SetDetectionRange(float range)
    {
        zombieDetectionRange = range;
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerPlayerController] Detection range set to {zombieDetectionRange}");
        }
    }
    
    private void SetXPosition(float x)
    {
        Vector3 pos = transform.position;
        pos.x = x;
        transform.position = pos;
    }

    #endregion

    #region Strafe Movement (Lane-based)
    
    /// <summary>
    /// Move one lane to the left
    /// </summary>
    public void StrafeLeft()
    {
        if (_isDead) return;
        if (UseFreeMovement) return; // Use ApplyHorizontalMovement for free mode
        if (_isStrafing) return;
        
        int newLane = _currentLane - 1;
        if (_laneConfig != null)
        {
            newLane = _laneConfig.ClampLaneIndex(newLane);
        }
        else
        {
            newLane = Mathf.Max(0, newLane);
        }
        
        if (newLane != _currentLane)
        {
            StartStrafe(newLane);
            
            if (useDirectionalBlend)
            {
                CalculateDirectionalBlend(-1f); // -1 = left
            }
            else
            {
                TriggerAnimation(strafeLeftTrigger);
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"[RunnerPlayerController] Strafing LEFT: Lane {_currentLane} -> {newLane}");
            }
        }
    }
    
    /// <summary>
    /// Move one lane to the right
    /// </summary>
    public void StrafeRight()
    {
        if (_isDead) return;
        if (UseFreeMovement) return; // Use ApplyHorizontalMovement for free mode
        if (_isStrafing) return;
        
        int maxLane = _laneConfig != null ? _laneConfig.LaneCount - 1 : 2;
        int newLane = Mathf.Min(_currentLane + 1, maxLane);
        
        if (newLane != _currentLane)
        {
            StartStrafe(newLane);
            
            if (useDirectionalBlend)
            {
                CalculateDirectionalBlend(1f); // 1 = right
            }
            else
            {
                TriggerAnimation(strafeRightTrigger);
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"[RunnerPlayerController] Strafing RIGHT: Lane {_currentLane} -> {newLane}");
            }
        }
    }
    
    /// <summary>
    /// Move to a specific lane
    /// </summary>
    public void MoveToLane(int laneIndex)
    {
        if (_isDead) return;
        if (UseFreeMovement) return;
        if (_isStrafing) return;
        
        if (_laneConfig != null)
        {
            laneIndex = _laneConfig.ClampLaneIndex(laneIndex);
        }
        
        if (laneIndex != _currentLane)
        {
            bool goingLeft = laneIndex < _currentLane;
            StartStrafe(laneIndex);
            
            if (useDirectionalBlend)
            {
                CalculateDirectionalBlend(goingLeft ? -1f : 1f);
            }
            else
            {
                TriggerAnimation(goingLeft ? strafeLeftTrigger : strafeRightTrigger);
            }
        }
    }
    
    private void StartStrafe(int targetLane)
    {
        _targetLane = targetLane;
        _isStrafing = true;
        _strafeProgress = 0f;
        _strafeStartX = transform.position.x;
        _strafeTargetX = GetLaneXPosition(targetLane);
        
        OnStrafeStarted?.Invoke();
        
        if (strafeParticles != null)
        {
            strafeParticles.Play();
        }
    }
    
    private void UpdateStrafe()
    {
        _strafeProgress += Time.deltaTime / strafeDuration;
        
        if (_strafeProgress >= 1f)
        {
            CompleteStrafe();
            return;
        }
        
        // Apply easing curve
        float curveValue = strafeCurve.Evaluate(_strafeProgress);
        float newX = Mathf.Lerp(_strafeStartX, _strafeTargetX, curveValue);
        
        Vector3 pos = transform.position;
        pos.x = newX;
        transform.position = pos;
    }
    
    private void CompleteStrafe()
    {
        _isStrafing = false;
        _strafeProgress = 1f;
        _currentLane = _targetLane;
        
        // Snap to exact lane position
        SnapToLane(_currentLane);
        
        // Return to idle animation
        TriggerIdle();
        
        OnLaneChanged?.Invoke(_currentLane);
        OnStrafeCompleted?.Invoke();
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerPlayerController] Strafe complete. Now at lane {_currentLane}");
        }
    }
    
    private void SnapToLane(int laneIndex)
    {
        Vector3 pos = transform.position;
        pos.x = GetLaneXPosition(laneIndex);
        transform.position = pos;
    }
    
    private float GetLaneXPosition(int laneIndex)
    {
        if (_laneConfig != null)
        {
            return _laneConfig.GetLanePosition(laneIndex);
        }
        
        // Default: 3 lanes with 2 unit spacing
        return (laneIndex - 1) * 2f;
    }
    
    #endregion

    #region Animation
    
    private void TriggerAnimation(string triggerName)
    {
        if (animator != null && !string.IsNullOrEmpty(triggerName))
        {
            animator.SetTrigger(triggerName);
        }
    }
    
    /// <summary>
    /// Trigger idle animation (called when touch/movement ends)
    /// </summary>
    public void TriggerIdle()
    {
        if (_isDead) return;
        
        TriggerAnimation(idleTrigger);
        
        // Also stop directional blend movement
        if (useDirectionalBlend)
        {
            StopMovement();
        }
        
        if (showDebugLogs)
        {
            Debug.Log("[RunnerPlayerController] Triggered Idle animation");
        }
    }
    
    /// <summary>
    /// Updates the directional blend animation parameters based on movement and look direction.
    /// Uses dot product to determine if movement is strafe (perpendicular) or walk (toward/away from enemy).
    /// </summary>
    private void UpdateDirectionalBlend()
    {
        if (animator == null) return;
        
        // Smoothly interpolate blend values
        _currentBlendValue = Mathf.Lerp(_currentBlendValue, _targetBlendValue, Time.deltaTime * blendSmoothSpeed);
        _currentDirectionValue = Mathf.Lerp(_currentDirectionValue, _targetDirectionValue, Time.deltaTime * blendSmoothSpeed);
        
        // Set animator parameters
        if (!string.IsNullOrEmpty(blendParameter))
        {
            animator.SetFloat(blendParameter, _currentBlendValue);
        }
        
        if (!string.IsNullOrEmpty(directionParameter))
        {
            animator.SetFloat(directionParameter, _currentDirectionValue);
        }
        
        if (!string.IsNullOrEmpty(isMovingParameter))
        {
            animator.SetBool(isMovingParameter, _isCurrentlyMoving);
        }
    }
    
    /// <summary>
    /// Calculate and set the directional blend based on movement direction.
    /// Call this when the player starts moving in a direction.
    /// </summary>
    /// <param name="movementDirectionSign">-1 for left movement, 1 for right movement</param>
    public void CalculateDirectionalBlend(float movementDirectionSign)
    {
        if (!useDirectionalBlend || _currentTarget == null)
        {
            // Fallback: if no target, treat as pure strafe
            _targetBlendValue = 0f;
            _targetDirectionValue = movementDirectionSign;
            _isCurrentlyMoving = true;
            return;
        }
        
        // Use global right because movement is always along the global X axis
        // We compare this fixed movement axis against the dynamic direction to the enemy
        Vector3 globalRight = Vector3.right;
        
        // Get direction from player to enemy (the look target)
        Vector3 toEnemy = (_currentTarget.TargetTransform.position - transform.position).normalized;
        toEnemy.y = 0; // Keep on horizontal plane
        toEnemy.Normalize();
        
        // The movement direction in world space
        // If movementDirectionSign > 0, moving right; if < 0, moving left
        Vector3 movementDirection = globalRight * movementDirectionSign;
        movementDirection.y = 0;
        movementDirection.Normalize();
        
        // Calculate dot product between movement direction and direction to enemy
        // dot = 1 means moving directly toward enemy (walk forward)
        // dot = -1 means moving directly away from enemy (walk backward)
        // dot = 0 means moving perpendicular to enemy (pure strafe)
        float dot = Vector3.Dot(movementDirection, toEnemy);
        // Debug.Log($"[DotCheck] Dot: {dot} | Distance: {Vector3.Distance(transform.position, _currentTarget.transform.position)}");
        
        // The blend value is the absolute value of the dot product
        // 0 = pure strafe, 1 = pure walk forward/backward
        _targetBlendValue = Mathf.Abs(dot);
        
        // Direction combines the strafe direction with forward/backward info
        // For a 2D blend tree: X = left/right (-1 to 1), Y = strafe/walk blend (0 to 1)
        // For now, we encode direction as:
        // - If dot > 0 (moving toward enemy): positive direction
        // - If dot < 0 (moving away from enemy): negative direction
        // This allows blend tree to differentiate walk forward vs walk backward
        _targetDirectionValue = movementDirectionSign;
        
        _isCurrentlyMoving = true;
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerPlayerController] DirectionalBlend - Dot: {dot:F2}, Blend: {_targetBlendValue:F2}, " +
                     $"Direction: {_targetDirectionValue:F2}, Moving {(movementDirectionSign > 0 ? "RIGHT" : "LEFT")}, " +
                     $"Toward Enemy: {(dot > 0.5f ? "YES" : dot < -0.5f ? "AWAY" : "STRAFE")}");
        }
    }
    
    /// <summary>
    /// Simplified method to set movement direction without recalculating blend.
    /// Used for continuous movement updates.
    /// </summary>
    public void SetMovementDirection(float directionSign)
    {
        if (_currentTarget != null && useDirectionalBlend)
        {
            CalculateDirectionalBlend(directionSign);
        }
        else
        {
            _targetDirectionValue = directionSign;
            _isCurrentlyMoving = Mathf.Abs(directionSign) > 0.01f;
        }
    }
    
    /// <summary>
    /// Stop movement and return to idle blend state
    /// </summary>
    public void StopMovement()
    {
        _isCurrentlyMoving = false;
        _targetBlendValue = 0f;
        _targetDirectionValue = 0f;
        
        if (showDebugLogs)
        {
            Debug.Log("[RunnerPlayerController] Movement stopped - returning to idle blend");
        }
    }

    // IK Pass Implementation - Called by IKForwarder on the child Animator object
    public void HandleAnimatorIK(int layerIndex)
    {
        // Debug.Log($"[IK DEBUG] HandleAnimatorIK called! layerIndex={layerIndex}");
        
        // 1. Check if IK is globally enabled and Animator exists
        if (!enableIK || animator == null) return;

        // 2. Check if we are actually shooting (User Request: Only activate IK when shooting)
        if (!_isShooting) return;

        // Only apply IK on the specific shooting layer
        if (layerIndex == shootingLayerIndex)
        {
            // --- 1. Look At IK (Aims the spine/head) ---
            // Even if body is masked, head might still rotate
            Vector3 lookTarget = transform.TransformPoint(lookAtTargetOffset);
            animator.SetLookAtWeight(lookAtWeight, lookAtBodyWeight, lookAtHeadWeight, 1f, 0.5f);
            animator.SetLookAtPosition(lookTarget);
            
            // --- 2. Dual Hand IK (Force hands down together) ---
            if (ikWeight > 0)
            {
                // Apply to Right Hand
                ApplyHandIK(AvatarIKGoal.RightHand, ikHandOffset, ikWeight);
                
                // Apply to Left Hand (Same offset so they stay together)
                ApplyHandIK(AvatarIKGoal.LeftHand, ikHandOffset, ikWeight);
            }
        }
    }

    private void ApplyHandIK(AvatarIKGoal goal, Vector3 offset, float weight)
    {
        animator.SetIKPositionWeight(goal, weight);
        
        // Get current animation position
        Transform bone = animator.GetBoneTransform(goal == AvatarIKGoal.RightHand ? HumanBodyBones.RightHand : HumanBodyBones.LeftHand);
        if (bone != null)
        {
            // Calculate target: Current Pos + Offset
            Vector3 targetPos = bone.position + offset;
            animator.SetIKPosition(goal, targetPos);
        }
    }

    #endregion

    #region Collision
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if hit by enemy
        Zombie_Controller enemy = other.GetComponent<Zombie_Controller>();
        if (enemy != null)
        {
            HandleEnemyCollision(enemy);
        }
    }
    
    private void HandleEnemyCollision(Zombie_Controller enemy)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerPlayerController] Hit by enemy: {enemy.name}");
        }
        
        // Zombie handles attack logic - don't duplicate here
    }
    
    #endregion

    #region Debug Visualization
    
    private void OnDrawGizmos()
    {
        if (_laneConfig == null) return;
        
        if (_laneConfig.UseFreeMovement)
        {
            // Draw movement bounds for free movement mode
            Gizmos.color = Color.yellow;
            float y = transform.position.y;
            float z = transform.position.z;
            
            // Left bound
            Vector3 leftBound = new Vector3(_laneConfig.MinXPosition, y, z);
            Gizmos.DrawWireSphere(leftBound, 0.3f);
            
            // Right bound
            Vector3 rightBound = new Vector3(_laneConfig.MaxXPosition, y, z);
            Gizmos.DrawWireSphere(rightBound, 0.3f);
            
            // Line between bounds
            Gizmos.DrawLine(leftBound, rightBound);
            
            // Current target position
            Gizmos.color = Color.green;
            Vector3 targetPos = new Vector3(_targetXPosition, y, z);
            Gizmos.DrawSphere(targetPos, 0.15f);
        }
        else
        {
            // Draw lane positions for lane-based mode
            Gizmos.color = Color.cyan;
            for (int i = 0; i < _laneConfig.LaneCount; i++)
            {
                float x = _laneConfig.GetLanePosition(i);
                Vector3 pos = new Vector3(x, transform.position.y, transform.position.z);
                Gizmos.DrawWireSphere(pos, 0.3f);
            }
            
            // Highlight current lane
            Gizmos.color = Color.green;
            float currentX = _laneConfig.GetLanePosition(_currentLane);
            Vector3 currentPos = new Vector3(currentX, transform.position.y, transform.position.z);
            Gizmos.DrawSphere(currentPos, 0.2f);
        }
    }
    
    
    #endregion
    
    #region Gate Modifiers
    
    /// <summary>
    /// Apply a stat modifier from a gate collision
    /// </summary>
    public void ApplyModifier(RunnerModifierGate.ModifierType type, RunnerModifierGate.OperationType operation, float value)
    {
        float oldValue = 0f;
        float newValue = 0f;
        string statName = "";
        
        switch (type)
        {
            case RunnerModifierGate.ModifierType.ShootingRange:
                _rangeModifier = ApplyOperation(_rangeModifier, operation, value);
                RecalculateStats();
                statName = "Shooting Range";
                break;
                
            case RunnerModifierGate.ModifierType.FireRate:
                _fireRateModifier = ApplyOperation(_fireRateModifier, operation, value);
                RecalculateStats();
                statName = "Fire Rate";
                break;
                
            case RunnerModifierGate.ModifierType.BulletDamage:
                _damageModifier = ApplyOperation(_damageModifier, operation, value);
                RecalculateStats();
                statName = "Bullet Damage";
                break;
                
            case RunnerModifierGate.ModifierType.MachineGun:
                // MachineGun modifier toggles machine gun mode
                // Increase = enable machine gun, Decrease = disable machine gun
                bool enableMachineGun = (operation == RunnerModifierGate.OperationType.Increase);
                SetMachineGunMode(enableMachineGun);
                statName = "Machine Gun Mode";
                break;

            case RunnerModifierGate.ModifierType.BulletAmount:
                // Additive modifier
                int amount = Mathf.RoundToInt(value);
                if (operation == RunnerModifierGate.OperationType.Decrease) amount = -amount;
                _bulletCountModifier += amount;
                statName = "Bullet Amount";
                break;
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerPlayerController] Applied modifier: {statName} {oldValue:F1} → {newValue:F1} (clamped to {GetClampedValue(type):F1})");
        }
    }
    
    /// <summary>
    /// Apply the modifier value to the current accumulator.
    /// Uses additive stacking for percentages (e.g. 0% + 20% = 20%).
    /// </summary>
    private float ApplyOperation(float currentModifier, RunnerModifierGate.OperationType operation, float value)
    {
        switch (operation)
        {
            case RunnerModifierGate.OperationType.Increase:
                return currentModifier + value;
            
            case RunnerModifierGate.OperationType.Decrease:
                return currentModifier - value;
            
            default:
                return currentModifier;
        }
    }
    
    /// <summary>
    /// Get the clamped value for a stat type
    /// </summary>
    private float GetClampedValue(RunnerModifierGate.ModifierType type)
    {
        switch (type)
        {
            case RunnerModifierGate.ModifierType.ShootingRange:
                return _currentRange;
            case RunnerModifierGate.ModifierType.FireRate:
                return _currentFireRate;
            case RunnerModifierGate.ModifierType.BulletDamage:
                return _currentDamage;
            default:
                return 0f;
        }
    }
    
    #endregion

    private System.Collections.IEnumerator WaitAndDestroy(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
    
    /// <summary>
    /// Helper to find a child recursively by name
    /// </summary>
    private Transform FindRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
