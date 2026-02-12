using UnityEngine;

/// <summary>
/// Parent controller for modifier gates.
/// Manages two gate options (left/right) and applies stat modifications to the player.
/// </summary>
public class RunnerModifierGate : MonoBehaviour
{
    [System.Serializable]
    public class GateConfig
    {
        public ModifierType modifierType;
        public OperationType operationType;
        public float value;
    }
    
    public enum ModifierType
    {
        ShootingRange,
        FireRate,
        BulletDamage,
        AddMember,
        MachineGun,
        BulletAmount
    }
    
    public enum OperationType
    {
        Increase,   // Increase by X% (e.g., value=20 means +20%)
        Decrease    // Decrease by X% (e.g., value=20 means -20%)
    }
    
    [Header("Gate Options")]
    [SerializeField] private RunnerGateOption leftGate;
    [SerializeField] private RunnerGateOption rightGate;
    
    [Header("Gate Configuration")]
    [SerializeField] private GateConfig leftConfig;
    [SerializeField] private GateConfig rightConfig;
    
    [Header("Modifier Icons")]
    [Tooltip("Icon for Bullet Damage modifier")]
    [SerializeField] private Sprite damageIcon;
    [Tooltip("Icon for Fire Rate modifier")]
    [SerializeField] private Sprite fireRateIcon;
    [Tooltip("Icon for Shooting Range modifier")]
    [SerializeField] private Sprite rangeIcon;
    [Tooltip("Icon for Machine Gun modifier")]
    [SerializeField] private Sprite machineGunIcon;
    [Tooltip("Icon for Bullet Amount modifier")]
    [SerializeField] private Sprite bulletAmountIcon;
    
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [Tooltip("Z position where gate despawns (should be BEHIND player, e.g., -5 if player is at Z=0)")]
    [SerializeField] private float despawnZ = -5f;
    
    [Header("Visual Feedback")]
    [Tooltip("Color for beneficial modifiers (add/multiply)")]
    [SerializeField] private Color positiveColor = new Color(0.2f, 1f, 0.2f);
    [Tooltip("Color for harmful modifiers (subtract/divide)")]
    [SerializeField] private Color negativeColor = new Color(1f, 0.2f, 0.2f);
    [SerializeField] private ParticleSystem collectEffect;
    [SerializeField] private AudioClip collectSound;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private bool _hasBeenTriggered = false;
    
    private void Start()
    {
        // Auto-find gate options if not assigned
        if (leftGate == null || rightGate == null)
        {
            RunnerGateOption[] options = GetComponentsInChildren<RunnerGateOption>(true); // Include inactive
            if (options.Length >= 2)
            {
                leftGate = options[0];
                rightGate = options[1];
            }
        }
        
        // Setup visuals for both gates
        UpdateGateVisuals();
    }
    
    /// <summary>
    /// Reset the gate for reuse (object pooling)
    /// </summary>
    public void ResetGate(Vector3 spawnPosition)
    {
        // Reset position
        transform.position = spawnPosition;
        
        // Reset trigger flag
        _hasBeenTriggered = false;
        
        // Reactivate both gate options
        if (leftGate != null) leftGate.gameObject.SetActive(true);
        if (rightGate != null) rightGate.gameObject.SetActive(true);
        
        // Reactivate this gate
        gameObject.SetActive(true);
        
        // Update visuals
        UpdateGateVisuals();
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerModifierGate] Gate reset at position {spawnPosition}");
        }
    }
    
    /// <summary>
    /// Set the left and right gate configurations externally (used by spawner)
    /// </summary>
    public void SetConfig(GateConfig left, GateConfig right)
    {
        leftConfig = left;
        rightConfig = right;
        
        // Update visuals with new configs
        UpdateGateVisuals();
    }
    
    private void Update()
    {
        // Move toward player (negative Z direction)
        transform.position += Vector3.back * moveSpeed * Time.deltaTime;
        
        // Deactivate if passed the despawn point (for object pooling)
        if (transform.position.z <= despawnZ)
        {
            gameObject.SetActive(false);
            
            if (showDebugLogs)
            {
                Debug.Log("[RunnerModifierGate] Gate reached despawn point - deactivated for pooling");
            }
        }
    }
    
    /// <summary>
    /// Update the visual display of both gate options
    /// </summary>
    private void UpdateGateVisuals()
    {
        if (leftGate != null)
        {
            string text = GetModifierText(leftConfig);
            bool isPositive = IsModifierPositive(leftConfig);
            Sprite icon = GetModifierIcon(leftConfig.modifierType);
            Color color = isPositive ? positiveColor : negativeColor;
            leftGate.SetVisuals(text, isPositive, icon, color);
        }
        
        if (rightGate != null)
        {
            string text = GetModifierText(rightConfig);
            bool isPositive = IsModifierPositive(rightConfig);
            Sprite icon = GetModifierIcon(rightConfig.modifierType);
            Color color = isPositive ? positiveColor : negativeColor;
            rightGate.SetVisuals(text, isPositive, icon, color);
        }
    }
    
    /// <summary>
    /// Get the icon sprite for a modifier type
    /// </summary>
    private Sprite GetModifierIcon(ModifierType modifierType)
    {
        switch (modifierType)
        {
            case ModifierType.BulletDamage:
                return damageIcon;
            case ModifierType.FireRate:
                return fireRateIcon;
            case ModifierType.ShootingRange:
                return rangeIcon;
            case ModifierType.MachineGun:
                return machineGunIcon;
            case ModifierType.BulletAmount:
                return bulletAmountIcon;
            default:
                return null;
        }
    }
    
    /// <summary>
    /// Generate display text for a modifier (e.g., "Damage +1", "Rate /2")
    /// </summary>
    private string GetModifierText(GateConfig config)
    {
        string operationSymbol = "";
        string statName = "";
        
        // Get operation symbol (always percentage-based)
        switch (config.operationType)
        {
            case OperationType.Increase:
                operationSymbol = "+";
                break;
            case OperationType.Decrease:
                operationSymbol = "-";
                break;
        }
        
        // Get stat name
        switch (config.modifierType)
        {
            case ModifierType.BulletDamage:
                statName = "DMG";
                break;
            case ModifierType.FireRate:
                statName = "RATE";
                break;
            case ModifierType.ShootingRange:
                statName = "RANGE";
                break;
            case ModifierType.MachineGun:
                // Special case: MachineGun doesn't use percentage
                return "MACHINE GUN";
            case ModifierType.BulletAmount:
                statName = "SHOTS";
                return $"{statName} {operationSymbol}{config.value}";
        }
        
        // Format value (remove decimal if whole number)
        string valueStr = config.value % 1 == 0 ? 
            config.value.ToString("0") : 
            config.value.ToString("0.#");
        
        // Always add percentage symbol (percentage-only system)
        return $"{statName} {operationSymbol}{valueStr}%";
    }
    
    /// <summary>
    /// Determine if a modifier is positive (beneficial) or negative
    /// </summary>
    private bool IsModifierPositive(GateConfig config)
    {
        // Simple: Increase is always positive, Decrease is always negative
        return config.operationType == OperationType.Increase;
    }
    
    /// <summary>
    /// Called by child RunnerGateOption when player collides with it
    /// </summary>
    public void OnGateChosen(RunnerGateOption chosenGate, RunnerPlayerController player)
    {
        // Prevent double-collision
        if (_hasBeenTriggered) return;
        _hasBeenTriggered = true;
        
        // Determine which config to use
        GateConfig config = (chosenGate == leftGate) ? leftConfig : rightConfig;
        
        // Apply modifier to ALL squad members
        // Apply modifier to ALL squad members via Manager (updates global state)
        RunnerSquadManager squadManager = FindObjectOfType<RunnerSquadManager>();
        if (squadManager != null)
        {
             squadManager.ApplyModifierToSquad(config.modifierType, config.operationType, config.value);
        }
        else
        {
            // Fallback: Apply only to the player that triggered if no squad manager
            player.ApplyModifier(config.modifierType, config.operationType, config.value);
            if (showDebugLogs)
            {
                Debug.Log($"[RunnerModifierGate] Applied {GetModifierText(config)} to single player (no squad)");
            }
        }
        
        // Visual/audio feedback
        if (collectEffect != null)
        {
            Instantiate(collectEffect, chosenGate.transform.position, Quaternion.identity);
        }
        
        if (collectSound != null)
        {
            AudioSource.PlayClipAtPoint(collectSound, transform.position);
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[RunnerModifierGate] Player chose {(chosenGate == leftGate ? "LEFT" : "RIGHT")} gate: {GetModifierText(config)}");
        }
        
        // Deactivate only the chosen gate side
        chosenGate.gameObject.SetActive(false);
        
        // Gate will deactivate when it reaches despawnZ (handled in Update)
    }
}
