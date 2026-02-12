using UnityEngine;

/// <summary>
/// Configuration for the lane/movement system in runner gameplay.
/// Supports both lane-based movement and free horizontal movement.
/// </summary>
[CreateAssetMenu(fileName = "RunnerLaneConfig", menuName = "Runner/Lane Config")]
public class RunnerLaneConfig : ScriptableObject
{
    [Header("Movement Mode")]
    [Tooltip("If true, player can move freely left/right with swipe. If false, uses lane-based movement.")]
    [SerializeField] private bool useFreeMovement = true;
    
    [Header("Free Movement Settings")]
    [Tooltip("Speed multiplier for horizontal movement based on swipe delta")]
    [SerializeField] private float moveSpeed = 8f;
    
    [Tooltip("Smoothing for movement (lower = more responsive, higher = smoother)")]
    [Range(0f, 0.5f)]
    [SerializeField] private float moveSmoothTime = 0.1f;
    
    [Tooltip("Minimum X position the player can move to")]
    [SerializeField] private float minXPosition = -3f;
    
    [Tooltip("Maximum X position the player can move to")]
    [SerializeField] private float maxXPosition = 3f;
    
    [Header("Lane Settings (Used when Free Movement is disabled)")]
    [Tooltip("Number of lanes (typically 3 or 5)")]
    [Range(3, 7)]
    [SerializeField] private int laneCount = 3;
    
    [Tooltip("Distance between each lane")]
    [SerializeField] private float laneWidth = 2f;
    
    [Tooltip("Starting lane index (0-based, middle lane for 3 lanes = 1)")]
    [SerializeField] private int startingLane = 1;

    [Header("Zombie Movement Constraints")]
    [Tooltip("Min X position for zombie movement in normal phase")]
    [SerializeField] private float zombieMinLocalX = -3.0f;
    [Tooltip("Max X position for zombie movement in normal phase")]
    [SerializeField] private float zombieMaxLocalX = 3.0f;
    
    [Tooltip("Min X position for zombie chasing phase")]
    [SerializeField] private float zombieMinChasingX = -10.0f;
    [Tooltip("Max X position for zombie chasing phase")]
    [SerializeField] private float zombieMaxChasingX = 10.0f;

    // Free Movement Properties
    public bool UseFreeMovement => useFreeMovement;
    public float MoveSpeed => moveSpeed;
    public float MoveSmoothTime => moveSmoothTime;
    public float MinXPosition => minXPosition;
    public float MaxXPosition => maxXPosition;
    
    // Lane Properties
    public int LaneCount => laneCount;
    public float LaneWidth => laneWidth;

    public int StartingLane => startingLane;
    
    // Zombie Properties
    public float ZombieMinLocalX => zombieMinLocalX;
    public float ZombieMaxLocalX => zombieMaxLocalX;
    public float ZombieMinChasingX => zombieMinChasingX;
    public float ZombieMaxChasingX => zombieMaxChasingX;
    
    /// <summary>
    /// Get the X position for a specific lane index
    /// </summary>
    public float GetLanePosition(int laneIndex)
    {
        // Center the lanes around 0
        float totalWidth = (laneCount - 1) * laneWidth;
        float leftmostLane = -totalWidth / 2f;
        return leftmostLane + (laneIndex * laneWidth);
    }
    
    /// <summary>
    /// Get all lane positions
    /// </summary>
    public float[] GetAllLanePositions()
    {
        float[] positions = new float[laneCount];
        for (int i = 0; i < laneCount; i++)
        {
            positions[i] = GetLanePosition(i);
        }
        return positions;
    }
    
    /// <summary>
    /// Clamp lane index to valid range
    /// </summary>
    public int ClampLaneIndex(int index)
    {
        return Mathf.Clamp(index, 0, laneCount - 1);
    }
    
    /// <summary>
    /// Get the starting X position for free movement mode (center of bounds)
    /// </summary>
    public float GetStartingXPosition()
    {
        return (minXPosition + maxXPosition) / 2f;
    }
    
    /// <summary>
    /// Clamp X position to valid movement bounds
    /// </summary>
    public float ClampXPosition(float x)
    {
        return Mathf.Clamp(x, minXPosition, maxXPosition);
    }
}
