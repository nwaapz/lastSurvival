using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Handles hero movement using NavMesh in the basebuilder scene.
/// Attach this to your hero character GameObject.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class HeroMovementBaseBuilder : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float stoppingDistance = 0.1f;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [Tooltip("Trigger name for Idle state")]
    [SerializeField] private string idleTrigger = "idle";
    [Tooltip("Trigger name for Walk state")]
    [SerializeField] private string walkTrigger = "walk";
    [Tooltip("Trigger name for Run state")]
    [SerializeField] private string runTrigger = "run";
    [SerializeField] private float runSpeedThreshold = 5f; // Speed above this triggers run animation
    
    [Header("Blend Tree Parameters")]
    [Tooltip("Float parameter name for X (strafe) in Blend Tree")]
    [SerializeField] private string velocityXParameter = "X";
    [Tooltip("Float parameter name for Y (forward/back) in Blend Tree")]
    [SerializeField] private string velocityYParameter = "Y";
    
    // Animation state tracking
    private enum AnimState { None, Idle, Walk, Run }
    private AnimState _currentAnimState = AnimState.None;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool showPathGizmos = true;

    // Events
    public event Action OnMovementStarted;
    public event Action OnMovementStopped;
    public event Action<Vector3> OnDestinationReached;
    
    private NavMeshAgent _agent;
    private Vector3 _currentDestination;
    private bool _isMoving;
    private bool _hasDestination;
    private bool _isRunning;

    public bool IsMoving => _isMoving;
    public Vector3 CurrentDestination => _currentDestination;
    public float RemainingDistance => _agent != null ? _agent.remainingDistance : 0f;

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        SetupNavMeshAgent();
    }

    private void Update()
    {
        UpdateMovement();
        UpdateAnimation();
    }

    private void InitializeComponents()
    {
        _agent = GetComponent<NavMeshAgent>();
        
        if (_agent == null)
        {
            Debug.LogError("[HeroMovementBaseBuilder] NavMeshAgent component is missing!", this);
            enabled = false;
            return;
        }

        // Try to find Animator if not assigned
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("[HeroMovementBaseBuilder] No Animator found. Animations will not play.", this);
            }
        }

        if (showDebugLogs)
        {
            Debug.Log("[HeroMovementBaseBuilder] Initialized successfully.", this);
        }
    }

    private void SetupNavMeshAgent()
    {
        if (_agent == null) return;

        _agent.speed = moveSpeed;
        _agent.angularSpeed = rotationSpeed;
        _agent.stoppingDistance = stoppingDistance;
        _agent.autoBraking = true;
        _agent.updateRotation = true;
    }

    /// <summary>
    /// Set a destination for the hero to walk to
    /// </summary>
    public void SetDestination(Vector3 destination)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[HeroMovementBaseBuilder] ----- SetDestination Called -----\n" +
                     $"Requested Destination: {destination}\n" +
                     $"Current Position: {transform.position}", this);
        }
        
        if (_agent == null)
        {
            Debug.LogError("[HeroMovementBaseBuilder] ❌ NavMeshAgent is not available!", this);
            return;
        }

        // Check if destination is on NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(destination, out hit, 2f, NavMesh.AllAreas))
        {
            _currentDestination = hit.position;
            
            if (showDebugLogs)
            {
                float adjustmentDistance = Vector3.Distance(destination, hit.position);
                Debug.Log($"[HeroMovementBaseBuilder] NavMesh Sample Result:\n" +
                         $"  Original Position: {destination}\n" +
                         $"  Adjusted Position: {hit.position}\n" +
                         $"  Adjustment Distance: {adjustmentDistance:F3}m", this);
            }
            
            _agent.SetDestination(_currentDestination);
            _hasDestination = true;

            if (!_isMoving)
            {
                _isMoving = true;
                OnMovementStarted?.Invoke();
                
                if (showDebugLogs)
                {
                    Debug.Log($"[HeroMovementBaseBuilder] ✓ MOVEMENT STARTED\n" +
                             $"  Destination: {_currentDestination}\n" +
                             $"  Distance: {Vector3.Distance(transform.position, _currentDestination):F2}m\n" +
                             $"  Speed: {_agent.speed}m/s", this);
                }
            }
            else
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[HeroMovementBaseBuilder] ➡ DESTINATION UPDATED (already moving)", this);
                }
            }
        }
        else
        {
            Debug.LogWarning($"[HeroMovementBaseBuilder] ❌ Destination {destination} is not on NavMesh!", this);
        }
    }

    /// <summary>
    /// Stop movement immediately
    /// </summary>
    public void Stop()
    {
        if (_agent == null) return;

        if (_agent.isOnNavMesh)
        {
            _agent.ResetPath();
        }
        _hasDestination = false;
        _isRunning = false;
        
        if (_isMoving)
        {
            _isMoving = false;
            OnMovementStopped?.Invoke();
            
            if (showDebugLogs)
            {
                Debug.Log($"[HeroMovementBaseBuilder] ⏸ MOVEMENT STOPPED (manually)\n" +
                         $"  Position: {transform.position}", this);
            }
        }
    }

    /// <summary>
    /// Check if the hero can reach a specific destination
    /// </summary>
    public bool CanReachDestination(Vector3 destination)
    {
        if (_agent == null) return false;

        // First sample the position to find the nearest valid NavMesh point (same as SetDestination)
        NavMeshHit hit;
        Vector3 targetPosition = destination;
        bool foundNavMeshPoint = NavMesh.SamplePosition(destination, out hit, 2f, NavMesh.AllAreas);
        
        if (foundNavMeshPoint)
        {
            targetPosition = hit.position;
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.Log($"[HeroMovementBaseBuilder] Path Calculation:\n" +
                         $"  NavMesh Sample: FAILED (no NavMesh within 2m)\n" +
                         $"  Can Reach: false", this);
            }
            return false;
        }

        NavMeshPath path = new NavMeshPath();
        bool canCalculate = _agent.CalculatePath(targetPosition, path);
        bool isComplete = path.status == NavMeshPathStatus.PathComplete;
        bool canReach = canCalculate && isComplete;
        
        if (showDebugLogs)
        {
            Debug.Log($"[HeroMovementBaseBuilder] Path Calculation:\n" +
                     $"  Original Position: {destination}\n" +
                     $"  Sampled Position: {targetPosition}\n" +
                     $"  Can Calculate: {canCalculate}\n" +
                     $"  Path Status: {path.status}\n" +
                     $"  Can Reach: {canReach}\n" +
                     $"  Path Corners: {(path.corners != null ? path.corners.Length : 0)}", this);
        }
        
        return canReach;
    }

    private void UpdateMovement()
    {
        if (!_hasDestination || _agent == null) return;

        // Check if we've reached the destination
        if (!_agent.pathPending)
        {
            // Check if we are within stopping distance
            // Use a small tolerance to ensure we trigger reliably even if agent doesn't stop exactly at 0 distance
            if (_agent.remainingDistance <= _agent.stoppingDistance + 0.05f)
            {
                ReachDestination();
            }
        }
    }

    private void ReachDestination()
    {
        if (!_hasDestination) return;

        Vector3 finalDestination = _currentDestination;
        float finalDistance = Vector3.Distance(transform.position, finalDestination);
        
        _hasDestination = false;
        _isMoving = false;
        _isRunning = false;
        
        // FORCE STOP: clear path and kill velocity immediately
        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;
        }
        
        // Force animation update immediately to switch to Idle
        UpdateAnimation();
        
        OnDestinationReached?.Invoke(_currentDestination);
        OnMovementStopped?.Invoke();
        
        if (showDebugLogs)
        {
            Debug.Log($"[HeroMovementBaseBuilder] ✓ DESTINATION REACHED\n" +
                     $"  Target: {finalDestination}\n" +
                     $"  Final Position: {transform.position}\n" +
                     $"  Final Distance: {finalDistance:F3}m", this);
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null)
        {
            if (showDebugLogs && _isMoving)
            {
                Debug.LogWarning("[HeroMovementBaseBuilder] ❌ Animator is not assigned!", this);
            }
            return;
        }

        // --- Navigation Animation Sync ---
        // Use agent velocity and project to local space for Blend Tree
        Vector3 velocity = _agent != null ? _agent.velocity : Vector3.zero;
        
        // Project velocity onto character's local axes
        float x = Vector3.Dot(transform.right, velocity);
        float y = Vector3.Dot(transform.forward, velocity);

        // Normalize by moveSpeed to get -1..1 range
        float maxSpeed = moveSpeed > 0.1f ? moveSpeed : 1f;
        float normX = Mathf.Clamp(x / maxSpeed, -1f, 1f);
        float normY = Mathf.Clamp(y / maxSpeed, -1f, 1f);

        // Set the Blend Tree parameters
        if (!string.IsNullOrEmpty(velocityXParameter))
            animator.SetFloat(velocityXParameter, normX);
            
        if (!string.IsNullOrEmpty(velocityYParameter))
            animator.SetFloat(velocityYParameter, normY);

        // Debug log with Predict prefix
        // Log if moving OR if there's significant velocity (to catch cases where _isMoving might be out of sync)
        if (showDebugLogs && (_isMoving || velocity.magnitude > 0.1f))
        {
            Debug.Log($"[Predict] X={normX:F2}, Y={normY:F2} (Moving={_isMoving}, Vel={velocity.magnitude:F2})", this);
        }

        // --- State Management ---
        float currentSpeed = velocity.magnitude;
        bool isActuallyMoving = currentSpeed > 0.1f;
        
        // Determine target animation state based on movement and running mode
        AnimState targetState;
        if (!isActuallyMoving)
        {
            targetState = AnimState.Idle;
        }
        else if (_isRunning)
        {
            targetState = AnimState.Run;
        }
        else
        {
            targetState = AnimState.Walk;
        }

        if (_currentAnimState != targetState)
        {
            _currentAnimState = targetState;

            if (targetState == AnimState.Idle)
            {
                if (!string.IsNullOrEmpty(idleTrigger)) animator.SetTrigger(idleTrigger);
            }
            else if (targetState == AnimState.Walk)
            {
                // Trigger walk state if configured (useful for entering Blend Tree state)
                if (!string.IsNullOrEmpty(walkTrigger)) animator.SetTrigger(walkTrigger);
            }
            else if (targetState == AnimState.Run)
            {
                // Trigger run state
                if (!string.IsNullOrEmpty(runTrigger)) animator.SetTrigger(runTrigger);
            }

            if (showDebugLogs)
            {
                Debug.Log($"[HeroMovementBaseBuilder] Animation State -> {targetState}", this);
            }
        }
    }

    /// <summary>
    /// Set movement speed
    /// </summary>
    public void SetSpeed(float speed)
    {
        moveSpeed = Mathf.Max(0f, speed);
        if (_agent != null)
        {
            _agent.speed = moveSpeed;
        }
    }
    
    /// <summary>
    /// Set whether the character should use run animation
    /// </summary>
    public void SetRunning(bool running)
    {
        _isRunning = running;
        
        if (showDebugLogs)
        {
            Debug.Log($"[HeroMovementBaseBuilder] Running mode: {(running ? "ON" : "OFF")}", this);
        }
    }
    
    /// <summary>
    /// Check if character is currently in running mode
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Set stopping distance
    /// </summary>
    public void SetStoppingDistance(float distance)
    {
        stoppingDistance = Mathf.Max(0f, distance);
        if (_agent != null)
        {
            _agent.stoppingDistance = stoppingDistance;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showPathGizmos || _agent == null || !_hasDestination) return;

        // Draw current destination
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_currentDestination, 0.5f);

        // Draw path
        if (_agent.hasPath)
        {
            Gizmos.color = Color.yellow;
            Vector3[] corners = _agent.path.corners;
            
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }

        // Draw stopping distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _agent.stoppingDistance);
    }

    #region Public Properties
    public float Speed
    {
        get => moveSpeed;
        set => SetSpeed(value);
    }

    public float StoppingDistance
    {
        get => stoppingDistance;
        set => SetStoppingDistance(value);
    }

    public NavMeshAgent Agent => _agent;
    #endregion
    
    #region Editor Validation
#if UNITY_EDITOR
    [ContextMenu("Validate Animation Setup")]
    private void ValidateAnimationSetup()
    {
        Debug.Log("========== ANIMATION SETUP VALIDATION ==========");
        
        // Check Animator
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogError("❌ ANIMATOR NOT FOUND! Please assign an Animator component.", this);
                return;
            }
            else
            {
                Debug.Log($"✓ Animator found: {animator.gameObject.name}", this);
            }
        }
        else
        {
            Debug.Log($"✓ Animator assigned: {animator.gameObject.name}", this);
        }
        
        // Check if animator has a controller
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError("❌ Animator has no AnimatorController assigned!", this);
            return;
        }
        else
        {
            Debug.Log($"✓ AnimatorController: {animator.runtimeAnimatorController.name}", this);
        }
        
        // Check parameters
        var parameters = animator.parameters;
        Debug.Log($"\nAnimator has {parameters.Length} parameters:");
        foreach (var param in parameters)
        {
            Debug.Log($"  - {param.name} ({param.type})");
        }
        
        // Validate our animation triggers exist
        Debug.Log("\nValidating configured animation triggers:");
        
        if (!string.IsNullOrEmpty(idleTrigger))
        {
            bool exists = System.Array.Exists(parameters, p => p.name == idleTrigger && p.type == AnimatorControllerParameterType.Bool);
            if (exists)
                Debug.Log($"✓ Idle param '{idleTrigger}' found", this);
            else
                Debug.LogWarning($"❌ Idle param '{idleTrigger}' NOT FOUND or wrong type (Expected Bool)!", this);
        }
        
        if (!string.IsNullOrEmpty(walkTrigger))
        {
            bool exists = System.Array.Exists(parameters, p => p.name == walkTrigger && p.type == AnimatorControllerParameterType.Bool);
            if (exists)
                Debug.Log($"✓ Walk param '{walkTrigger}' found", this);
            else
                Debug.LogWarning($"❌ Walk param '{walkTrigger}' NOT FOUND or wrong type (Expected Bool)!", this);
        }
        
        if (!string.IsNullOrEmpty(runTrigger))
        {
            bool exists = System.Array.Exists(parameters, p => p.name == runTrigger && p.type == AnimatorControllerParameterType.Bool);
            if (exists)
                Debug.Log($"✓ Run param '{runTrigger}' found", this);
            else
                Debug.LogWarning($"❌ Run param '{runTrigger}' NOT FOUND or wrong type (Expected Bool)!", this);
        }
        
        Debug.Log("\n========== VALIDATION COMPLETE ==========");
    }
#endif
    #endregion
}
