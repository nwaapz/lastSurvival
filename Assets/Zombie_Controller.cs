using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controls the zombie enemy for the Runner game mode.
/// Replaces the original game logic with simplified runner mechanics.
/// Includes behavior to chase the player if the zombie passes them.
/// Implements IShootableTarget so players can target it.
/// </summary>
public class Zombie_Controller : MonoBehaviour, IShootableTarget
{
    [Header("Runner Movement")]
    [SerializeField] private float baseSpeed = 2f;
    [SerializeField] private bool useGameSpeed = false;
    
    [Header("NavMesh Settings")]
    [Tooltip("Use NavMeshAgent for pathfinding instead of manual movement")]
    [SerializeField] private bool useNavMesh = true;
    [Tooltip("Attack range - zombie stops moving when within this distance")]
    [SerializeField] private float attackRange = 1.5f;
    [Tooltip("Shows if NavMesh is actually active at runtime (read-only)")]
    [SerializeField] private bool isNavMeshActive = false;
    private NavMeshAgent _navAgent;
    
    /// <summary>
    /// Returns true if NavMesh is actually being used at runtime
    /// </summary>
    public bool IsUsingNavMesh => isNavMeshActive;
    
    [Header("Separation Settings")]
    [SerializeField] private bool useSeparation = true;
    [SerializeField] private float separationRadius = 1.0f;
    [SerializeField] private float separationForce = 5.0f;


    
    [Header("Runner Combat")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private int damage = 1;
    [SerializeField] private int scoreValue = 10;
    [SerializeField] private float attackCooldown = 1f;
    
    // State
    private float _currentHealth;

    public void TakeDamage(float damageAmount)
    {
        if (_isDying || !_isActive) return;

        _currentHealth -= damageAmount;
        
        // Visual feedback
        if (damageFlash != null)
        {
            damageFlash.Flash();
        }
        
        // Show floating damage text
        DamageTextManager.SpawnDamageText(damageAmount, transform.position);
        
        if (_currentHealth <= 0)
        {
            Defeat();
        }
    }
    
    [Header("Runner Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkTrigger = "walk";
    [SerializeField] private string attackTrigger = "attack";
    [SerializeField] private string deathTrigger = "die";
    [SerializeField] private string idleTrigger = "idle";
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip spawnSound;
    [SerializeField] private AudioClip deathSound;
    
    [Header("Visual")]
    [SerializeField] private ParticleSystem deathParticles;
    [SerializeField] private float destroyDelay = 3f;
    [SerializeField] private DamageFlash damageFlash;

    // Events compatiable with Runner System
    public event Action<Zombie_Controller> OnEnemyDestroyed;
    public event Action<Zombie_Controller> OnEnemyReachedEnd;
    
    // State
    private int _laneIndex;
    private bool _isActive = true;
    private bool _isDying;
    private bool _isIdle;
    private float _despawnZ = -10f;
    private Vector3 _despawnPos = new Vector3(0f, 0f, -10f);
    
    // Chasing behavior
    private bool _isChasingPlayer;
    private Transform _playerTransform;
    
    // Attacking behavior
    private bool _isAttacking;
    private float _lastAttackTime;
    private RunnerPlayerController _targetPlayer;
    
    // Properties for Runner System
    public int LaneIndex => _laneIndex;
    public int Damage => damage;
    public int ScoreValue => scoreValue;
    public bool IsActive 
    {
        get
        {
            if (!_isActive || _isDying) return false;
            
            // Check targeting rule: Only target zombies if on Left side of threshold
            // Note: This only affects Player targeting Zombie. Zoning movement is separate.
            if (RunnerBarrelQueue.Instance != null && !RunnerBarrelQueue.Instance.CanShootZombies())
            {
                return false;
            }
            
            return true;
        }
    }
    
    // IShootableTarget implementation
    public Transform TargetTransform => transform;

    private void Start()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        
        // Play spawn sound
        PlaySound(spawnSound);
        
        // Start walk animation
        if (animator != null)
        {
            animator.SetTrigger(walkTrigger);
        }
        
        if (damageFlash == null)
        {
            damageFlash = GetComponent<DamageFlash>();
        }
        
        // Initialize NavMeshAgent if using NavMesh
        if (useNavMesh)
        {
            _navAgent = GetComponent<NavMeshAgent>();
            if (_navAgent == null)
            {
                Debug.LogWarning($"[Zombie_Controller] useNavMesh is true but no NavMeshAgent found on {gameObject.name}. Falling back to manual movement.");
                useNavMesh = false;
            }
            else
            {
                _navAgent.speed = baseSpeed;
                _navAgent.updateRotation = true;
                // Random priority helps zombies avoid each other (lower = higher priority)
                _navAgent.avoidancePriority = UnityEngine.Random.Range(30, 70);
                
                // Warp to nearest NavMesh surface (fixes spawn height issues)
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
                {
                    _navAgent.Warp(hit.position);
                }
            }
        }
    }
    
    /// <summary>
    /// Find the nearest living player from all squad members
    /// </summary>
    private RunnerPlayerController FindNearestPlayer()
    {
        // Use cached squad manager instead of FindObjectsOfType (massive performance gain)
        if (_cachedSquadManager == null)
        {
            _cachedSquadManager = FindObjectOfType<RunnerSquadManager>();
        }
        
        if (_cachedSquadManager == null) return null;
        
        var players = _cachedSquadManager.ActiveMembers;
        if (players == null || players.Count == 0) return null;
        
        RunnerPlayerController nearest = null;
        float nearestDist = float.MaxValue;
        
        foreach (var player in players)
        {
            // Skip dead or null players
            if (player == null || player.CurrentHealth <= 0) continue;
            
            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = player;
            }
        }
        
        return nearest;
    }
    
    // Cached reference to avoid FindObjectOfType calls every frame
    private RunnerSquadManager _cachedSquadManager;


    [Header("Debug")]
    [SerializeField] private bool debugTriggerWalk;

    private void Update()
    {
        // Update inspector status
        isNavMeshActive = useNavMesh && _navAgent != null && _navAgent.isOnNavMesh;
        
        if (debugTriggerWalk)
        {
            debugTriggerWalk = false;
            TriggerAnimation(walkTrigger);
        }

        if (!_isActive || _isDying || _isIdle) return;
        
        // If attacking, don't move - just keep attacking
        if (_isAttacking)
        {
            PerformAttack();
            return;
        }
        
        MoveForward();
        CheckDespawn();
    }

    #region Runner Methods
    
    private void MoveForward()
    {
        float speed = GetCurrentSpeed();
        
        // Find nearest player dynamically (supports multiple squad members)
        RunnerPlayerController nearestPlayer = FindNearestPlayer();
        Transform targetTransform = nearestPlayer != null ? nearestPlayer.transform : null;
        
        if (targetTransform != null)
        {
            // Check distance to player - start attacking if close enough
            float distanceToPlayer = Vector3.Distance(transform.position, targetTransform.position);
            if (distanceToPlayer <= attackRange)
            {
                // Close enough to attack!
                if (useNavMesh && _navAgent != null && _navAgent.isOnNavMesh)
                {
                    _navAgent.isStopped = true;
                }
                StartAttacking(nearestPlayer);
                return;
            }
            
            // === NAVMESH MOVEMENT ===
            if (useNavMesh && _navAgent != null && _navAgent.isOnNavMesh)
            {
                // Use faster speed if chasing (passed despawn line)
                _navAgent.speed = _isChasingPlayer ? speed * 1.5f : speed;
                _navAgent.isStopped = false;
                _navAgent.SetDestination(targetTransform.position);
            }
            // === MANUAL MOVEMENT (fallback) ===
            else
            {
                Vector3 direction = (targetTransform.position - transform.position).normalized;
                
                // Use faster speed if chasing (passed despawn line)
                float moveSpeed = _isChasingPlayer ? speed * 1.5f : speed;
                Vector3 moveVector = direction * moveSpeed;
                
                // Face player
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 10f);
                
                // Apply separation to prevent stacking
                if (useSeparation)
                {
                    Vector3 separation = CalculateSeparation();
                    transform.position += (moveVector + separation * separationForce) * Time.deltaTime;
                }
                else
                {
                    transform.position += moveVector * Time.deltaTime;
                }
                
                // Clamp local X position based on phase using GLOBAL config
                float minX = -3f;
                float maxX = 3f;
                
                if (RunnerGameManager.Instance != null && RunnerGameManager.Instance.LaneConfig != null)
                {
                    var config = RunnerGameManager.Instance.LaneConfig;
                    minX = _isChasingPlayer ? config.ZombieMinChasingX : config.ZombieMinLocalX;
                    maxX = _isChasingPlayer ? config.ZombieMaxChasingX : config.ZombieMaxLocalX;
                }
                
                Vector3 localPos = transform.localPosition;
                localPos.x = Mathf.Clamp(localPos.x, minX, maxX);
                transform.localPosition = localPos;
            }
        }
        else if (!useNavMesh || _navAgent == null || !_navAgent.isOnNavMesh)
        {
            // Fallback: Move toward negative Z if no player found (manual mode only)
            Vector3 moveVector = Vector3.back * speed;
            transform.position += moveVector * Time.deltaTime;
        }
    }
    
    private Vector3 CalculateSeparation()
    {
        Vector3 separationVector = Vector3.zero;
        int count = 0;
        
        // Use OverlapSphere to find nearby zombies
        // We limit max neighbors to check to avoid performance issues
        Collider[] hits = new Collider[10];
        int numHits = Physics.OverlapSphereNonAlloc(transform.position, separationRadius, hits);
        
        for (int i = 0; i < numHits; i++)
        {
            Collider hit = hits[i];
            if (hit == null || hit.gameObject == gameObject) continue;
            
            // Only separate from other zombies that are active (not dead)
            Zombie_Controller otherZombie = hit.GetComponent<Zombie_Controller>();
            if (otherZombie != null && otherZombie.IsActive)
            {
                Vector3 dir = transform.position - hit.transform.position;
                dir.y = 0;
                
                // Linear falloff
                float dist = dir.magnitude;
                if (dist < 0.01f) dist = 0.01f; // Prevent div by zero
                
                if (dist < separationRadius)
                {
                    float strength = 1.0f - (dist / separationRadius);
                    separationVector += dir.normalized * strength;
                    count++;
                }
            }
        }
        
        if (count > 0)
        {
            separationVector.Normalize();
        }
        
        return separationVector;
    }
    
    private float GetCurrentSpeed()
    {
        if (useGameSpeed && RunnerGameManager.Instance != null)
        {
            return RunnerGameManager.Instance.CurrentGameSpeed;
        }
        return baseSpeed;
    }
    
    private void CheckDespawn()
    {
        // If passed the despawn line, start chasing
        if (transform.position.z <= _despawnZ)
        {
            if (!_isChasingPlayer)
            {
                _isChasingPlayer = true;
            }
        }
    }
    
    // Initialize compatible with RunnerEnemySpawner logic
    public void Initialize(int laneIndex, Vector3 spawnPosition, Vector3 despawnPos)
    {
        _laneIndex = laneIndex;
        _despawnPos = despawnPos;
        _despawnZ = despawnPos.z; // Keep for backward compatibility
        _isActive = true;
        _isDying = false;
        _isAttacking = false;
        _isIdle = false;
        _targetPlayer = null;
        
        _currentHealth = maxHealth;
        
        transform.position = spawnPosition;
        
        // Face the player (negative Z)
        transform.rotation = Quaternion.LookRotation(Vector3.back);
    }
    
    // Backward compatible overload
    public void Initialize(int laneIndex, Vector3 spawnPosition, float despawnZ = -10f)
    {
        Initialize(laneIndex, spawnPosition, new Vector3(0f, 0f, despawnZ));
    }
    
    public void Defeat()
    {
        if (_isDying) return;
        
        _isDying = true;
        _isActive = false;
        _isAttacking = false;
        
        TriggerAnimation(deathTrigger);
        TriggerAnimation(deathTrigger);
        // PlaySound(deathSound); // Deprecated in favor of centralized manager
        FightSceneSfxManager.PlayZombieDeathSfx();
        
        // Stop NavMeshAgent if using NavMesh
        if (_navAgent != null && _navAgent.isOnNavMesh)
        {
            _navAgent.isStopped = true;
            _navAgent.ResetPath();
            _navAgent.enabled = false; // Disable completely to prevent pushing other agents
        }
        
        if (deathParticles != null)
        {
            deathParticles.Play();
        }
        
        if (RunnerGameManager.Instance != null)
        {
            RunnerGameManager.Instance.RegisterEnemyDefeated();
        }
        
        StartCoroutine(WaitAndInvokeDestroyed(destroyDelay));
    }
    
    /// <summary>
    /// Make the zombie go idle - stops movement and attacking.
    /// Useful for when player dies and zombies should stop.
    /// </summary>
    public void MakeIdle()
    {
        if (_isDying || !_isActive) return;
        
        // Stop all behavior
        _isIdle = true;
        _isAttacking = false;
        _isChasingPlayer = false;
        _targetPlayer = null;
        
        // Stop NavMeshAgent if using NavMesh
        if (_navAgent != null && _navAgent.isOnNavMesh)
        {
            _navAgent.isStopped = true;
            _navAgent.ResetPath();
        }
        
        // Trigger idle animation
        TriggerAnimation(idleTrigger);
    }
    
    private System.Collections.IEnumerator WaitAndInvokeDestroyed(float delay)
    {
        yield return new WaitForSeconds(delay);
        OnEnemyDestroyed?.Invoke(this);
        // Safety destroy in case no one is listening to the event
        if (gameObject.activeInHierarchy)
        {
            //Destroy(gameObject, 0.1f);
        }
    }
    
    // OnReachedEnd removed - zombies persist until killed
    
    #endregion

    #region Attack
    
    private void StartAttacking(RunnerPlayerController player)
    {
        if (_isAttacking) return;
        
        _isAttacking = true;
        _targetPlayer = player;
        _lastAttackTime = Time.time - attackCooldown; // Allow immediate first attack
        
        TriggerAnimation(attackTrigger);
        Debug.Log("[Zombie] Started attacking player!");
    }
    
    private void PerformAttack()
    {
        if (_targetPlayer == null)
        {
            // Player reference lost, stop attacking
            _isAttacking = false;
            return;
        }
        
        // Check if player is dead
        if (_targetPlayer.CurrentHealth <= 0)
        {
             _isAttacking = false;
             _targetPlayer = null;
             
             // Check if we should go idle (if all players are dead)
             RunnerPlayerController nextTarget = FindNearestPlayer();
             if (nextTarget == null)
             {
                 MakeIdle();
             }
             else
             {
                 // Resume chasing new target
                 _isChasingPlayer = true;
                 TriggerAnimation(walkTrigger);
             }
             return;
        }

        // Check if still in range (simple distance check)
        float distance = Vector3.Distance(transform.position, _targetPlayer.transform.position);
        if (distance > 2f)
        {
            // Player moved away, resume chasing
            _isAttacking = false;
            _isChasingPlayer = true;
            TriggerAnimation(walkTrigger);
            return;
        }
        
        // Face the player
        Vector3 direction = (_targetPlayer.transform.position - transform.position).normalized;
        direction.y = 0;
        if (direction.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 10f);
        }
        
        // Attack on cooldown
        if (Time.time >= _lastAttackTime + attackCooldown)
        {
            _lastAttackTime = Time.time;
            TriggerAnimation(attackTrigger);
            
            // Apply damage to player
            if (_targetPlayer != null)
            {
                _targetPlayer.TakeDamage(damage);
            }
            
            // Notify game manager that player was hit
            if (RunnerGameManager.Instance != null)
            {
                RunnerGameManager.Instance.RegisterPlayerHit();
            }
        }
    }
    
    #endregion

    #region Collision
    
    private void OnTriggerEnter(Collider other)
    {
        if (!_isActive || _isDying) return;
        
        RunnerPlayerController player = other.GetComponent<RunnerPlayerController>();
        if (player != null)
        {
            // Stop and start attacking
            StartAttacking(player);
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (!_isActive || _isDying) return;
        
        RunnerPlayerController player = other.GetComponent<RunnerPlayerController>();
        if (player != null && player == _targetPlayer)
        {
            // Player left attack range, resume chasing
            _isAttacking = false;
            _isChasingPlayer = true;
            TriggerAnimation(walkTrigger);
        }
    }
    
    #endregion
    
    #region Utilities
    
    private void TriggerAnimation(string triggerName)
    {
        if (animator != null && !string.IsNullOrEmpty(triggerName))
        {
            animator.SetTrigger(triggerName);
        }
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    

    
    #endregion
}
