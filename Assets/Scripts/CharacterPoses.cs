using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable class to map pose types to sprites in the Inspector
/// </summary>
[Serializable]
public class PoseMapping
{
    public PoseType poseType;
    public Sprite sprite;
}

[CreateAssetMenu(fileName = "New Character Poses", menuName = "Character/Character Poses")]
public class CharacterPoses : ScriptableObject
{
    [Header("Character Configuration")]
    public CharacterName CharacterName;
    
    [Header("Pose Mappings")]
    [SerializeField] private List<PoseMapping> poseMappings = new List<PoseMapping>();
    
    // Runtime dictionary for fast lookups
    private Dictionary<PoseType, Sprite> poseDict;
    
    /// <summary>
    /// Initialize the pose dictionary from the mappings list
    /// </summary>
    private void InitializeDictionary()
    {
        if (poseDict == null)
        {
            poseDict = new Dictionary<PoseType, Sprite>();
            foreach (var mapping in poseMappings)
            {
                if (mapping.sprite != null)
                {
                    poseDict[mapping.poseType] = mapping.sprite;
                }
            }
        }
    }

    /// <summary>
    /// Get a pose by enum type. Returns null if not found.
    /// </summary>
    public Sprite GetPose(PoseType poseType)
    {
        InitializeDictionary();
        
        if (poseDict.TryGetValue(poseType, out Sprite sprite))
        {
            return sprite;
        }
        
        Debug.LogWarning($"Pose '{poseType}' not found in {name}");
        return null;
    }
    
    /// <summary>
    /// Get a pose by string name (for backward compatibility). Returns null if not found.
    /// </summary>
    public Sprite GetPose(string poseName)
    {
        if (Enum.TryParse(poseName, true, out PoseType poseType))
        {
            return GetPose(poseType);
        }
        
        Debug.LogWarning($"Pose '{poseName}' is not a valid PoseType in {name}");
        return null;
    }

    /// <summary>
    /// Check if a specific pose is configured
    /// </summary>
    public bool HasPose(PoseType poseType)
    {
        InitializeDictionary();
        return poseDict.ContainsKey(poseType);
    }
    
    /// <summary>
    /// Get all configured poses
    /// </summary>
    public Dictionary<PoseType, Sprite> GetAllPoses()
    {
        InitializeDictionary();
        return new Dictionary<PoseType, Sprite>(poseDict);
    }
}
