using UnityEngine;

/// <summary>
/// Attached to individual gate sprites (left/right options).
/// Handles collision detection and notifies parent when player chooses this option.
/// </summary>
[RequireComponent(typeof(Collider))]
public class RunnerGateOption : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RunnerModifierGate parentGate;
    
    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("TextMesh component to display the operation text (e.g., '+1', '/2')")]
    [SerializeField] private TextMesh operationText;
    [Tooltip("SpriteRenderer to display the modifier type icon (Damage, FireRate, Range)")]
    [SerializeField] private SpriteRenderer modifierIcon;
    
    private void Start()
    {
        // Auto-find parent if not assigned
        if (parentGate == null)
        {
            parentGate = GetComponentInParent<RunnerModifierGate>();
        }
        
        // Auto-find sprite renderer if not assigned
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        
        // Auto-find TextMesh if not assigned
        if (operationText == null)
        {
            operationText = GetComponentInChildren<TextMesh>();
        }
        
        // Ensure trigger is enabled
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }
    
    /// <summary>
    /// Set the visual appearance of this gate option
    /// </summary>
    /// <param name="text">Operation text to display (e.g., "+1", "/2", "×3")</param>
    /// <param name="isPositive">Whether this is a positive/beneficial modifier</param>
    /// <param name="iconSprite">Icon sprite for the modifier type (optional)</param>
    /// <param name="gateColor">Color to tint the gate background</param>
    public void SetVisuals(string text, bool isPositive, Sprite iconSprite = null, Color? gateColor = null)
    {
        // Update operation text display
        if (operationText != null)
        {
            operationText.text = text;
        }
        
        // Update modifier icon
        if (modifierIcon != null && iconSprite != null)
        {
            modifierIcon.sprite = iconSprite;
            modifierIcon.enabled = true;
        }
        else if (modifierIcon != null)
        {
            modifierIcon.enabled = false;
        }
        
        // Apply color tint to background
        if (spriteRenderer != null && gateColor.HasValue)
        {
            spriteRenderer.color = gateColor.Value;
        }
        else if (spriteRenderer != null)
        {
            // Fallback default colors if no color provided
            spriteRenderer.color = isPositive ? new Color(0.2f, 1f, 0.2f) : new Color(1f, 0.2f, 0.2f);
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Debug: Log all collisions
        Debug.Log($"[RunnerGateOption] Trigger entered by: {other.gameObject.name} (Tag: {other.tag})");
        
        // Check if player collided - look on object and parents
        RunnerPlayerController player = other.GetComponent<RunnerPlayerController>();
        if (player == null)
        {
            player = other.GetComponentInParent<RunnerPlayerController>();
        }
        
        if (player != null && parentGate != null)
        {
            // Only the squad leader (offset 0,0,0) triggers gates
            if (!player.IsLeader)
            {
                Debug.Log($"[RunnerGateOption] Player {player.name} is NOT the leader - ignoring gate collision");
                return;
            }
            
            Debug.Log($"[RunnerGateOption] LEADER PLAYER DETECTED! Notifying parent gate.");
            // Notify parent that this option was chosen
            parentGate.OnGateChosen(this, player);
        }
        else
        {
            if (player == null)
                Debug.LogWarning($"[RunnerGateOption] No RunnerPlayerController found on {other.gameObject.name}");
            if (parentGate == null)
                Debug.LogWarning($"[RunnerGateOption] Parent gate is null!");
        }
    }
}
