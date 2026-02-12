using UnityEngine;

/// <summary>
/// Collectible items in runner mode that give points or power-ups.
/// </summary>
public class RunnerCollectible : MonoBehaviour
{
    public enum CollectibleType
    {
        Coin,
        ScoreBonus,
        SpeedBoost,
        Shield,
        Magnet
    }
    
    [Header("Settings")]
    [SerializeField] private CollectibleType type = CollectibleType.Coin;
    [SerializeField] private int scoreValue = 5;
    [SerializeField] private float effectDuration = 5f;
    
    [Header("Movement")]
    [SerializeField] private bool moveWithWorld = true;
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.3f;
    
    [Header("Visual")]
    [SerializeField] private ParticleSystem collectParticles;
    [SerializeField] private MeshRenderer meshRenderer;
    
    [Header("Audio")]
    [SerializeField] private AudioClip collectSound;
    
    private Vector3 _startPosition;
    private float _bobTimer;
    private bool _collected;
    private AudioSource _audioSource;

    private void Start()
    {
        _startPosition = transform.position;
        _audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (_collected) return;
        
        UpdateAnimation();
        
        if (moveWithWorld)
        {
            MoveWithWorld();
        }
    }

    private void UpdateAnimation()
    {
        // Rotation
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        
        // Bobbing
        _bobTimer += Time.deltaTime * bobSpeed;
        float bobOffset = Mathf.Sin(_bobTimer) * bobHeight;
        Vector3 pos = transform.position;
        pos.y = _startPosition.y + bobOffset;
        transform.position = pos;
    }

    private void MoveWithWorld()
    {
        if (RunnerGameManager.Instance == null) return;
        
        float speed = RunnerGameManager.Instance.CurrentGameSpeed;
        transform.position += Vector3.back * speed * Time.deltaTime;
        _startPosition.z = transform.position.z;
        
        // Destroy if past player
        if (transform.position.z < -10f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_collected) return;
        
        RunnerPlayerController player = other.GetComponent<RunnerPlayerController>();
        if (player != null)
        {
            Collect();
        }
    }

    private void Collect()
    {
        _collected = true;
        
        // Play effects
        if (collectParticles != null)
        {
            collectParticles.transform.SetParent(null);
            collectParticles.Play();
            Destroy(collectParticles.gameObject, 2f);
        }
        
        if (_audioSource != null && collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }
        
        // Apply effect based on type
        ApplyEffect();
        
        // Hide and destroy
        if (meshRenderer != null)
        {
            meshRenderer.enabled = false;
        }
        
        Destroy(gameObject, 0.1f);
    }

    private void ApplyEffect()
    {
        if (RunnerGameManager.Instance == null) return;
        
        switch (type)
        {
            case CollectibleType.Coin:
            case CollectibleType.ScoreBonus:
                RunnerGameManager.Instance.AddScore(scoreValue);
                break;
                
            case CollectibleType.SpeedBoost:
                // Could implement speed boost power-up
                RunnerGameManager.Instance.AddScore(scoreValue);
                break;
                
            case CollectibleType.Shield:
                // Could implement shield power-up
                RunnerGameManager.Instance.AddScore(scoreValue);
                break;
                
            case CollectibleType.Magnet:
                // Could implement coin magnet power-up
                RunnerGameManager.Instance.AddScore(scoreValue);
                break;
        }
    }

    /// <summary>
    /// Initialize collectible at position
    /// </summary>
    public void Initialize(int laneIndex, Vector3 position)
    {
        transform.position = position;
        _startPosition = position;
    }
}
