using UnityEngine;

/// <summary>
/// Static obstacles in runner mode that the player must avoid.
/// Unlike enemies, obstacles don't move but can be in specific lanes.
/// </summary>
public class RunnerObstacle : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int damage = 1;
    [SerializeField] private bool destroyOnHit = false;
    
    [Header("Visual")]
    [SerializeField] private ParticleSystem hitParticles;
    
    [Header("Audio")]
    [SerializeField] private AudioClip hitSound;
    
    private int _laneIndex;
    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Initialize obstacle in a specific lane
    /// </summary>
    public void Initialize(int laneIndex, Vector3 position)
    {
        _laneIndex = laneIndex;
        transform.position = position;
    }

    private void OnTriggerEnter(Collider other)
    {
        RunnerPlayerController player = other.GetComponent<RunnerPlayerController>();
        if (player != null)
        {
            HandlePlayerCollision();
        }
    }

    private void HandlePlayerCollision()
    {
        // Play effects
        if (hitParticles != null)
        {
            hitParticles.Play();
        }
        
        if (_audioSource != null && hitSound != null)
        {
            _audioSource.PlayOneShot(hitSound);
        }
        
        // Notify game manager
        if (RunnerGameManager.Instance != null)
        {
            RunnerGameManager.Instance.RegisterPlayerHit();
        }
        
        if (destroyOnHit)
        {
            Destroy(gameObject, 0.5f);
        }
    }
}
