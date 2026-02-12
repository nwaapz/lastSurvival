using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents a row of enemies that spawn together.
/// Defines which lanes have enemies in this row.
/// </summary>
[System.Serializable]
public class RunnerEnemyRow
{
    [Tooltip("Which lanes have enemies (true = enemy present)")]
    public bool[] lanesWithEnemies;
    
    [Tooltip("Optional: specific enemy prefab for this row (null = use default)")]
    public GameObject enemyPrefabOverride;
    
    [Tooltip("Delay before spawning next row")]
    public float delayAfterRow = 1f;
    
    /// <summary>
    /// Create a row with specified lane pattern
    /// </summary>
    public RunnerEnemyRow(int laneCount)
    {
        lanesWithEnemies = new bool[laneCount];
    }
    
    /// <summary>
    /// Create a row with enemies in specific lanes
    /// </summary>
    public RunnerEnemyRow(params int[] enemyLanes)
    {
        int maxLane = 0;
        foreach (int lane in enemyLanes)
        {
            if (lane > maxLane) maxLane = lane;
        }
        
        lanesWithEnemies = new bool[maxLane + 1];
        foreach (int lane in enemyLanes)
        {
            if (lane >= 0 && lane < lanesWithEnemies.Length)
            {
                lanesWithEnemies[lane] = true;
            }
        }
    }
    
    /// <summary>
    /// Get list of lane indices that have enemies
    /// </summary>
    public List<int> GetEnemyLanes()
    {
        List<int> lanes = new List<int>();
        for (int i = 0; i < lanesWithEnemies.Length; i++)
        {
            if (lanesWithEnemies[i])
            {
                lanes.Add(i);
            }
        }
        return lanes;
    }
    
    /// <summary>
    /// Check if there's an opening (safe lane) in this row
    /// </summary>
    public bool HasOpening()
    {
        foreach (bool hasEnemy in lanesWithEnemies)
        {
            if (!hasEnemy) return true;
        }
        return false;
    }
    
    /// <summary>
    /// Get the first safe lane (no enemy)
    /// </summary>
    public int GetFirstSafeLane()
    {
        for (int i = 0; i < lanesWithEnemies.Length; i++)
        {
            if (!lanesWithEnemies[i]) return i;
        }
        return -1; // No safe lane
    }
}

/// <summary>
/// Predefined row patterns for easy configuration
/// </summary>
public static class RunnerEnemyRowPatterns
{
    /// <summary>
    /// Single enemy in left lane
    /// </summary>
    public static RunnerEnemyRow LeftOnly(int laneCount = 3)
    {
        var row = new RunnerEnemyRow(laneCount);
        row.lanesWithEnemies[0] = true;
        return row;
    }
    
    /// <summary>
    /// Single enemy in middle lane
    /// </summary>
    public static RunnerEnemyRow MiddleOnly(int laneCount = 3)
    {
        var row = new RunnerEnemyRow(laneCount);
        row.lanesWithEnemies[laneCount / 2] = true;
        return row;
    }
    
    /// <summary>
    /// Single enemy in right lane
    /// </summary>
    public static RunnerEnemyRow RightOnly(int laneCount = 3)
    {
        var row = new RunnerEnemyRow(laneCount);
        row.lanesWithEnemies[laneCount - 1] = true;
        return row;
    }
    
    /// <summary>
    /// Enemies on left and right, middle is safe
    /// </summary>
    public static RunnerEnemyRow LeftAndRight(int laneCount = 3)
    {
        var row = new RunnerEnemyRow(laneCount);
        row.lanesWithEnemies[0] = true;
        row.lanesWithEnemies[laneCount - 1] = true;
        return row;
    }
    
    /// <summary>
    /// Enemies on left and middle, right is safe
    /// </summary>
    public static RunnerEnemyRow LeftAndMiddle(int laneCount = 3)
    {
        var row = new RunnerEnemyRow(laneCount);
        row.lanesWithEnemies[0] = true;
        row.lanesWithEnemies[laneCount / 2] = true;
        return row;
    }
    
    /// <summary>
    /// Enemies on middle and right, left is safe
    /// </summary>
    public static RunnerEnemyRow MiddleAndRight(int laneCount = 3)
    {
        var row = new RunnerEnemyRow(laneCount);
        row.lanesWithEnemies[laneCount / 2] = true;
        row.lanesWithEnemies[laneCount - 1] = true;
        return row;
    }
    
    /// <summary>
    /// Random pattern with at least one safe lane
    /// </summary>
    public static RunnerEnemyRow Random(int laneCount = 3)
    {
        var row = new RunnerEnemyRow(laneCount);
        
        // Ensure at least one lane is safe
        int safeLane = UnityEngine.Random.Range(0, laneCount);
        
        for (int i = 0; i < laneCount; i++)
        {
            if (i != safeLane)
            {
                row.lanesWithEnemies[i] = UnityEngine.Random.value > 0.5f;
            }
        }
        
        return row;
    }
}
