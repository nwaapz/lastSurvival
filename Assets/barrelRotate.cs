using UnityEngine;

public class barrelRotate : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 180f; // degrees per second
    [SerializeField] private Animator animator;

    private bool _canRotate = false;
    private float _animationDuration = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Auto-find animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // Get animation duration and schedule destruction
        if (animator != null)
        {
            AnimatorClipInfo[] clipInfo = animator.GetCurrentAnimatorClipInfo(0);
            if (clipInfo.Length > 0)
            {
                _animationDuration = clipInfo[0].clip.length;
                Destroy(gameObject, _animationDuration);
                Debug.Log($"[Barrel] Will destroy after {_animationDuration} seconds");
            }
        }

        // Start rotation after 300 milliseconds (0.3 seconds)
        Invoke(nameof(EnableRotation), 0.3f);
    }

    private void EnableRotation()
    {
        _canRotate = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!_canRotate)
            return;

        // Rotate around its own position using the global X axis
        transform.Rotate(Vector3.right * rotationSpeed * Time.deltaTime, Space.World);
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"[Barrel] OnCollisionEnter with {collision.gameObject.name}");
        
        // Check if the colliding object is a zombie
        Zombie_Controller zombie = collision.gameObject.GetComponent<Zombie_Controller>();
        if (zombie != null)
        {
            Debug.Log($"[Barrel] Killing zombie: {zombie.name}");
            // Kill the zombie instantly
            zombie.TakeDamage(9999f);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[Barrel] OnTriggerEnter with {other.gameObject.name}");
        
        // Check if the colliding object is a zombie
        Zombie_Controller zombie = other.GetComponent<Zombie_Controller>();
        if (zombie != null)
        {
            Debug.Log($"[Barrel] Killing zombie: {zombie.name}");
            // Kill the zombie instantly
            zombie.TakeDamage(9999f);
        }
    }
}
