using UnityEngine;

/// <summary>
/// Identifies a character in the scene by their CharacterName enum.
/// Attach this to each character GameObject that has HeroMovementBaseBuilder.
/// This allows the scenario system to find and control specific characters.
/// </summary>
[RequireComponent(typeof(HeroMovementBaseBuilder))]
[RequireComponent(typeof(HeroClickToMoveBaseBuilder))]
public class CharacterController : MonoBehaviour
{
    [Header("Character Identity")]
    [Tooltip("Which character this is (Hero, Janet, Pedi, Commander)")]
    public CharacterName characterName;
    
    [Header("Player Control")]
    [Tooltip("If false, player cannot click to move this character")]
    [SerializeField] private bool playerControlEnabled = true;
    
    private HeroMovementBaseBuilder _movement;
    private HeroClickToMoveBaseBuilder _clickToMove;

    private void Awake()
    {
        _movement = GetComponent<HeroMovementBaseBuilder>();
        _clickToMove = GetComponent<HeroClickToMoveBaseBuilder>();
    }

    /// <summary>
    /// Enable or disable player control (click-to-move)
    /// </summary>
    public void SetPlayerControlEnabled(bool enabled)
    {
        playerControlEnabled = enabled;
        
        if (_clickToMove != null)
        {
            _clickToMove.SetClickToMoveEnabled(enabled);
        }
        
        Debug.Log($"[CharacterController] {characterName} player control: {(enabled ? "ENABLED" : "DISABLED")}");
    }

    /// <summary>
    /// Command this character to move to a position (scenario control)
    /// </summary>
    public void MoveToPosition(Vector3 position, bool isRunning = false)
    {
        // Set running state on movement component
        if (_movement != null)
        {
            _movement.SetRunning(isRunning);
        }
        
        if (_clickToMove != null)
        {
            _clickToMove.MoveToPosition(position);
        }
    }
    
    /// <summary>
    /// Set whether the character is running (affects animation)
    /// </summary>
    public void SetRunning(bool isRunning)
    {
        if (_movement != null)
        {
            _movement.SetRunning(isRunning);
        }
    }

    /// <summary>
    /// Stop this character's movement
    /// </summary>
    public void Stop()
    {
        if (_movement != null)
        {
            _movement.Stop();
        }
    }

    /// <summary>
    /// Check if character is currently moving
    /// </summary>
    public bool IsMoving => _movement != null && _movement.IsMoving;

    /// <summary>
    /// Get the movement component
    /// </summary>
    public HeroMovementBaseBuilder Movement => _movement;

    /// <summary>
    /// Get the click-to-move component
    /// </summary>
    public HeroClickToMoveBaseBuilder ClickToMove => _clickToMove;
    
    /// <summary>
    /// Check if player control is currently enabled
    /// </summary>
    public bool PlayerControlEnabled => playerControlEnabled;

    private void Start()
    {
        // Apply initial player control state
        SetPlayerControlEnabled(playerControlEnabled);
    }
}
