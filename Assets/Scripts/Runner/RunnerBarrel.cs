using System;
using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// Individual barrel component with health and health bar.
/// Config (health value, sprite, modifier) is set by RunnerBarrelQueue at spawn time.
/// Implements IShootableTarget so players can target it like zombies.
/// </summary>
public class RunnerBarrel : MonoBehaviour, IShootableTarget
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float _currentHealth;
    
    [Header("Health Bar")]
    [Tooltip("Text to display health")]
    [SerializeField] private TMP_Text healthText;
    
    [Tooltip("Text to display the modifier (e.g. +1 or +10%)")]
    [SerializeField] private TMP_Text modifierText;
    
    [Header("Visual")]
    [Tooltip("The child object to rotate (if null, this script will be used, which we want to avoid for rotation)")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private SpriteRenderer spriteRenderer;

    public Transform VisualRoot => visualRoot;
    
    [Header("Effects")]
    [SerializeField] private Transform hitEffectPosition;
    
    [Header("Hit Animation")]
    [Tooltip("Punch scale amount when hit (e.g. 0.2 = 20% scale punch)")]
    [SerializeField] private float hitPunchScale = 0.15f;
    
    [Tooltip("Duration of the punch animation")]
    [SerializeField] private float hitPunchDuration = 0.2f;
    
    [Tooltip("Number of vibrations in the punch")]
    [SerializeField] private int hitPunchVibrato = 10;
    
    [Tooltip("Elasticity of the punch (0 = no elasticity, 1 = full elasticity)")]
    [Range(0f, 1f)]
    [SerializeField] private float hitPunchElasticity = 0.5f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    /// <summary>
    /// Event fired when barrel is destroyed (health <= 0)
    /// </summary>
    public event Action<RunnerBarrel> OnDestroyed;
    
    /// <summary>
    /// Index of this barrel in the queue (set by RunnerBarrelQueue)
    /// </summary>
    public int QueueIndex { get; private set; }
    
    public float CurrentHealth => _currentHealth;
    public float MaxHealth => maxHealth;
    
    // IShootableTarget implementation
    public bool IsActive
    {
        get
        {
            // Must be alive
            if (_currentHealth <= 0) return false;
            
            // Must satisfy shooting rules (e.g. Leader X pos threshold)
            if (RunnerBarrelQueue.Instance != null && !RunnerBarrelQueue.Instance.CanShootBarrels())
            {
                return false;
            }
            
            return true;
        }
    }
    
    // Track active tween to prevent overlapping
    private Tween _hitTween;
    private Vector3 _originalScale;
    public Transform TargetTransform => transform;
    
    // Pool tracking
    private bool _isPooled = false;
    
    private void Awake()
    {
        _currentHealth = maxHealth;
        _originalScale = transform.localScale;
        UpdateHealthBar();
    }
    
    /// <summary>
    /// Initialize barrel with config from queue
    /// </summary>
    public void Initialize(float health, Sprite sprite, int queueIndex, RunnerModifierGate.ModifierType modifierType, float value)
    {
        maxHealth = health;
        _currentHealth = health;
        QueueIndex = queueIndex;
        
        if (sprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
        }
        
        // Update Modifier Text
        if (modifierText != null)
        {
            if (modifierType == RunnerModifierGate.ModifierType.AddMember || 
                modifierType == RunnerModifierGate.ModifierType.BulletAmount)
            {
                // For members and bullets, it's a flat number, not percentage
                string sign = value >= 0 ? "+" : "";
                modifierText.text = $"{sign}{value}";
            }
            else
            {
                modifierText.text = $"+{value}%";
            }
        }
        
        UpdateHealthBar();
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerBarrel] Initialized at index {queueIndex} with health {health}");
        }
    }
    
    /// <summary>
    /// Take damage from bullets
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (_currentHealth <= 0) return;
        
        _currentHealth -= damage;
        _currentHealth = Mathf.Max(0, _currentHealth);
        
        UpdateHealthBar();
        
        // Play hit animation
        PlayHitAnimation();

        // Play hit particle (from Queue config) - uses object pooling
        if (RunnerBarrelQueue.Instance != null && RunnerBarrelQueue.Instance.HitParticlePrefab != null)
        {
            Vector3 spawnPos = hitEffectPosition != null ? hitEffectPosition.position : transform.position;
            
            // Use particle pool if available, otherwise create it
            if (RunnerParticlePool.Instance == null)
            {
                GameObject poolObj = new GameObject("RunnerParticlePool");
                poolObj.AddComponent<RunnerParticlePool>();
            }
            
            RunnerParticlePool.Instance.GetParticle(RunnerBarrelQueue.Instance.HitParticlePrefab, spawnPos);
        }
        
        // Play audio via manager
        FightSceneSfxManager.PlayBarrelHitSfx();
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerBarrel] Took {damage} damage. Health: {_currentHealth}/{maxHealth}");
        }
        
        if (_currentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Play punch scale animation when hit
    /// </summary>
    private void PlayHitAnimation()
    {
        // Kill any active hit tween to prevent stacking
        _hitTween?.Kill();
        
        // Reset scale to original before animation
        transform.localScale = _originalScale;
        
        // DOTween punch scale - scales up and down creating a "hit" effect
        // The punch is relative to the current scale
        _hitTween = transform.DOPunchScale(
            punch: _originalScale * hitPunchScale,
            duration: hitPunchDuration,
            vibrato: hitPunchVibrato,
            elasticity: hitPunchElasticity
        ).SetEase(Ease.OutElastic);
    }
    
    private void UpdateHealthBar()
    {
        if (healthText != null)
        {
            healthText.text = Mathf.CeilToInt(_currentHealth).ToString();
        }
    }
    
    private void Die()
    {
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerBarrel] Destroyed!");
        }
        
        // Notify queue (queue will handle pooling)
        OnDestroyed?.Invoke(this);
    }
    
    /// <summary>
    /// Deactivate and reset for pool reuse
    /// </summary>
    public void Deactivate()
    {
        // Kill any active tweens
        _hitTween?.Kill();
        
        // Reset scale
        transform.localScale = _originalScale;
        
        // Reset health
        _currentHealth = 0;
        
        // Clear event subscribers to prevent duplicate calls
        OnDestroyed = null;
        
        // Mark as pooled
        _isPooled = true;
        
        // Deactivate GameObject
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Check if this barrel is currently pooled (inactive)
    /// </summary>
    public bool IsPooled => _isPooled;
    
    /// <summary>
    /// Called when retrieved from pool - mark as active
    /// </summary>
    public void MarkActive()
    {
        _isPooled = false;
    }
    
    /// <summary>
    /// Called when hit by player bullet (via trigger or collision)
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Check if it's a player projectile
        RunnerProjectile projectile = other.GetComponent<RunnerProjectile>();
        if (projectile != null)
        {
            TakeDamage(projectile.Damage);
        }
    }
}
