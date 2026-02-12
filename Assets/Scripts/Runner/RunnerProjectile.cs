using UnityEngine;

/// <summary>
/// Simple projectile for the Runner game.
/// Moves forward and damages any IShootableTarget on impact.
/// </summary>
public class RunnerProjectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float speed = 20f;
    [SerializeField] private float damage = 100f; // High enough to kill instantly by default
    [SerializeField] private float lifetime = 3f;
    
    /// <summary>
    /// Get the damage value of this projectile
    /// </summary>
    public float Damage => damage;
    
    [Header("Visuals")]
    [SerializeField] private GameObject hitEffect;
    
    [Header("Raycast Detection")]
    [Tooltip("Use raycast to detect hits for fast-moving projectiles")]
    [SerializeField] private bool useRaycast = true;
    [SerializeField] private LayerMask hitLayers = ~0; // All layers by default
    
    private Vector3 _lastPosition;
    
    private int _currentHitCount;
    private int _maxHitCount;
    private System.Collections.Generic.List<GameObject> _hitObjects = new System.Collections.Generic.List<GameObject>();

    // Controlled by pool
    public void Activate(float duration)
    {
        lifetime = duration;
        _lastPosition = transform.position;
        
        // Reset state
        _currentHitCount = 0;
        _hitObjects.Clear();
        
        // Get penetration setting from pool
        if (RunnerProjectilePool.Instance != null)
        {
            _maxHitCount = RunnerProjectilePool.Instance.MaxPenetration;
        }
        else
        {
            _maxHitCount = 1;
        }
        
        gameObject.SetActive(true);
        CancelInvoke(nameof(Deactivate));
        Invoke(nameof(Deactivate), lifetime);
    }
    
    /// <summary>
    /// Set the damage value for this projectile
    /// </summary>
    public void SetDamage(float damageValue)
    {
        damage = damageValue;
    }

    private void OnEnable()
    {
        _lastPosition = transform.position;
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(Deactivate));
    }

    private void Deactivate()
    {
        gameObject.SetActive(false);
    }

    private void Update()
    {
        // Raycast from last position to current to catch fast-moving hits
        if (useRaycast)
        {
            // FORCE GLOBAL Z DIRECTION: Use Vector3.forward instead of transform.forward
            Vector3 moveDirection = Vector3.forward;
            float moveDistance = speed * Time.deltaTime;
            
            RaycastHit hit;
            if (Physics.Raycast(_lastPosition, moveDirection, out hit, moveDistance + 0.1f, hitLayers))
            {
                // Check if we hit a shootable target
                IShootableTarget target = hit.collider.GetComponent<IShootableTarget>();
                if (target != null && target.IsActive)
                {
                    HitTarget(target, hit.point);
                    // If we destroyed the projectile, stop processing
                    if (!gameObject.activeSelf) return;
                }
            }
        }
        
        _lastPosition = transform.position;
        
        // Move forward along GLOBAL Z axis (Vector3.forward) relative to WORLD, not self
        transform.Translate(Vector3.forward * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check for any shootable target (zombie, barrel, etc.)
        IShootableTarget target = other.GetComponent<IShootableTarget>();
        
        if (target != null && target.IsActive)
        {
            HitTarget(target, transform.position);
        }
    }
    
    private void HitTarget(IShootableTarget target, Vector3 hitPosition)
    {
        // Prevent double hits on the same target
        MonoBehaviour mb = target as MonoBehaviour;
        GameObject targetObj = mb != null ? mb.gameObject : null;
        
        if (targetObj != null)
        {
            if (_hitObjects.Contains(targetObj)) return;
            _hitObjects.Add(targetObj);
        }
        
        // Apply damage
        target.TakeDamage(damage);
        
        // Show hit effect
        if (hitEffect != null)
        {
            Instantiate(hitEffect, hitPosition, Quaternion.identity);
        }
        
        // Penetration logic
        _currentHitCount++;
        
        // Check if we hit a barrel - if so, destroy projectile immediately regardless of penetration settings
        bool isBarrel = targetObj != null && targetObj.GetComponent<RunnerBarrel>() != null;
        
        // Destroy if max penetration reached OR if we hit a barrel
        if (_currentHitCount >= _maxHitCount || isBarrel)
        {
            // Disable projectile (return to pool)
            gameObject.SetActive(false);
        }
    }
}
