using System;
using UnityEngine;

/// <summary>
/// Base class for enemies in runner mode.
/// Enemies move toward the player from the front in rows.
/// </summary>
public class RunnerEnemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float baseSpeed = 5f;
    [SerializeField] private bool useGameSpeed = true;
    
    [Header("Combat")]
    [SerializeField] private int damage = 1;
    [SerializeField] private int scoreValue = 10;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkTrigger = "Walk";
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string deathTrigger = "Death";
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip spawnSound;
    [SerializeField] private AudioClip deathSound;
    
    [Header("Visual")]
    [SerializeField] private ParticleSystem deathParticles;
    
    // Events
    public event Action<RunnerEnemy> OnEnemyDestroyed;
    public event Action<RunnerEnemy> OnEnemyReachedEnd;
    
    // State
    private int _laneIndex;
    private bool _isActive = true;
    private bool _isDying;
    private float _despawnZ = -10f; // Z position where enemy is destroyed
    
    // Properties
    public int LaneIndex => _laneIndex;
    public int Damage => damage;
    public int ScoreValue => scoreValue;
    public bool IsActive => _isActive && !_isDying;

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
        TriggerAnimation(walkTrigger);
    }

    private void Update()
    {
        if (!_isActive || _isDying) return;
        
        MoveForward();
        CheckDespawn();
    }

    #region Movement
    
    private void MoveForward()
    {
        float speed = GetCurrentSpeed();
        
        // Move toward player (negative Z direction)
        transform.position += Vector3.back * speed * Time.deltaTime;
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
        if (transform.position.z <= _despawnZ)
        {
            OnReachedEnd();
        }
    }
    
    #endregion

    #region Initialization
    
    /// <summary>
    /// Initialize the enemy with lane and spawn position
    /// </summary>
    public void Initialize(int laneIndex, Vector3 spawnPosition, float despawnZ = -10f)
    {
        _laneIndex = laneIndex;
        _despawnZ = despawnZ;
        _isActive = true;
        _isDying = false;
        
        transform.position = spawnPosition;
        
        // Face the player (negative Z)
        transform.rotation = Quaternion.LookRotation(Vector3.back);
    }
    
    /// <summary>
    /// Set the lane position for this enemy
    /// </summary>
    public void SetLane(int laneIndex)
    {
        _laneIndex = laneIndex;
        
        if (RunnerGameManager.Instance != null)
        {
            float xPos = RunnerGameManager.Instance.GetLanePosition(laneIndex);
            Vector3 pos = transform.position;
            pos.x = xPos;
            transform.position = pos;
        }
    }
    
    #endregion

    #region Combat
    
    /// <summary>
    /// Called when enemy is defeated by player
    /// </summary>
    public void Defeat()
    {
        if (_isDying) return;
        
        _isDying = true;
        _isActive = false;
        
        // Play death animation
        TriggerAnimation(deathTrigger);
        
        // Play death sound
        PlaySound(deathSound);
        
        // Spawn death particles
        if (deathParticles != null)
        {
            deathParticles.Play();
        }
        
        // Notify game manager
        if (RunnerGameManager.Instance != null)
        {
            RunnerGameManager.Instance.RegisterEnemyDefeated();
        }
        
        OnEnemyDestroyed?.Invoke(this);
        
        // Destroy after delay for animation
        Destroy(gameObject, 1f);
    }
    
    /// <summary>
    /// Called when enemy reaches the end without being defeated
    /// </summary>
    private void OnReachedEnd()
    {
        _isActive = false;
        OnEnemyReachedEnd?.Invoke(this);
        
        // Destroy immediately
        Destroy(gameObject);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Check if collided with player
        RunnerPlayerController player = other.GetComponent<RunnerPlayerController>();
        if (player != null && _isActive && !_isDying)
        {
            // Player collision is handled by player controller
            // Enemy can optionally play attack animation
            TriggerAnimation(attackTrigger);
        }
    }
    
    #endregion

    #region Animation & Audio
    
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

    #region Debug
    
    private void OnDrawGizmos()
    {
        // Draw enemy bounds
        Gizmos.color = _isActive ? Color.red : Color.gray;
        Gizmos.DrawWireCube(transform.position, Vector3.one);
        
        // Draw movement direction
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, Vector3.back * 2f);
    }
    
    #endregion
}
