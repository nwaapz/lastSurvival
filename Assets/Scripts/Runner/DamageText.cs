using UnityEngine;
using TMPro;

/// <summary>
/// Handles the animation and lifecycle of a single floating damage text.
/// Moves up and fades out over time, then returns to pool.
/// </summary>
public class DamageText : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float lifeTime = 1f;
    [SerializeField] private float fadeOutSpeed = 2f;
    [SerializeField] private Vector3 moveDirection = Vector3.up;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0f, 1f, 1f); // Reused for timing if needed, or ignored in favor of explicit code
    [SerializeField] private float popInDuration = 0.15f;
    [SerializeField] private float popOutDuration = 0.2f;

    [Header("References")]
    [SerializeField] private TMP_Text textComponent;

    private float _timer;
    private Color _startColor;
    private Vector3 _baseScale = Vector3.one;

    private void Awake()
    {
        if (textComponent == null)
            textComponent = GetComponent<TMP_Text>();
    }

    /// <summary>
    /// Initialize the damage text for a new spawn
    /// </summary>
    public void Initialize(float damageAmount, Color color)
    {
        if (textComponent == null) return;

        // Set text value
        textComponent.text = "-" + Mathf.RoundToInt(damageAmount).ToString();
        
        // Reset state
        textComponent.color = color;
        _startColor = color;
        _timer = 0f;
        
        // Start at scale 0
        transform.localScale = Vector3.zero;
        gameObject.SetActive(true);
        
        // Ensure it faces camera initially
        FaceCamera();
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        // Move
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        // Animation logic
        float currentScale = 1f;

        if (_timer < popInDuration)
        {
            // Phase 1: Pop In (0 -> 1)
            float t = _timer / popInDuration;
            // Use simple elastic/overshoot effect or just smooth step
            currentScale = Mathf.Lerp(0f, 1.2f, t); // Overshoot slightly to 1.2
        }
        else if (_timer < popInDuration + 0.1f)
        {
             // Phase 1.5: Settle (1.2 -> 1.0)
             float t = (_timer - popInDuration) / 0.1f;
             currentScale = Mathf.Lerp(1.2f, 1.0f, t);
        }
        else if (_timer > lifeTime - popOutDuration)
        {
            // Phase 3: Pop Out (1 -> 0)
            float t = (_timer - (lifeTime - popOutDuration)) / popOutDuration;
            currentScale = Mathf.Lerp(1f, 0f, t);
            
            // Optional: Also fade out
            float alpha = Mathf.Lerp(_startColor.a, 0f, t);
            textComponent.color = new Color(_startColor.r, _startColor.g, _startColor.b, alpha);
        }
        else
        {
            // Phase 2: Hold
            currentScale = 1f;
        }

        transform.localScale = _baseScale * currentScale;

        if (_timer >= lifeTime)
        {
            gameObject.SetActive(false);
        }
        
        FaceCamera();
    }
    
    // Simple billboard effect
    private void FaceCamera()
    {
        if (Camera.main != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }
    }
}
