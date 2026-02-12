using System.Collections.Generic;
using UnityEngine;

public class Narration_manager : SingletonMono<Narration_manager>, IService
{
    [Header("Character Poses Configuration")]
    [SerializeField] private List<CharacterPoses> characterPosesList = new List<CharacterPoses>();

    [Header("narration pop")]
    [SerializeField] Narration_pop narration_Pop;

    // Dictionary to map character names to their poses
    private Dictionary<CharacterName, CharacterPoses> characterPosesDict;

    private void OnEnable()
    {
        InitializeCharacterPoses();
        
        // Self-register with ServiceLocator
        if (ServiceLocator.HasInstance)
        {
            ServiceLocator.Instance.Register<Narration_manager>(this);
        }
    }
    
    protected override void OnDestroy()
    {
        if (ServiceLocator.HasInstance)
        {
            ServiceLocator.Instance.Unregister<Narration_manager>();
        }
        base.OnDestroy();
    }

    private void InitializeCharacterPoses()
    {
        characterPosesDict = new Dictionary<CharacterName, CharacterPoses>();
        
        // Automatically map CharacterPoses based on their CharacterName field
        foreach (var poses in characterPosesList)
        {
            if (poses != null)
            {
                characterPosesDict[poses.CharacterName] = poses;
            }
        }
        
        Debug.Log($"Initialized {characterPosesDict.Count} character pose configurations");
    }

    /// <summary>
    /// Get character poses by character name
    /// </summary>
    public CharacterPoses GetCharacterPoses(CharacterName characterName)
    {
        if (characterPosesDict.TryGetValue(characterName, out CharacterPoses poses))
        {
            return poses;
        }
        
        Debug.LogWarning($"Character poses not found for {characterName}");
        return null;
    }

    /// <summary>
    /// Get a specific pose sprite for a character using enum (recommended)
    /// </summary>
    public Sprite GetCharacterPose(CharacterName characterName, PoseType poseType)
    {
        CharacterPoses poses = GetCharacterPoses(characterName);
        if (poses != null)
        {
            return poses.GetPose(poseType);
        }
        return null;
    }
    
    /// <summary>
    /// Get a specific pose sprite for a character using string name (backward compatibility)
    /// </summary>
    public Sprite GetCharacterPose(CharacterName characterName, string poseName)
    {
        CharacterPoses poses = GetCharacterPoses(characterName);
        if (poses != null)
        {
            return poses.GetPose(poseName);
        }
        return null;
    }

    /// <summary>
    /// Show a narration line by fetching the pose sprite and displaying it with the message
    /// </summary>
    public void ShowNarrationLine(NarrationLine line)
    {
        if (line == null)
        {
            Debug.LogWarning("Narration_manager: NarrationLine is null.");
            return;
        }

        if (narration_Pop == null)
        {
            Debug.LogWarning("Narration_manager: Narration_pop reference is missing.");
            return;
        }

        // Activate the narration popup GameObject if it's inactive
        if (!narration_Pop.gameObject.activeSelf)
        {
            narration_Pop.gameObject.SetActive(true);
        }

        // Get sprite from CharacterPoses configuration
        Sprite poseSprite = GetCharacterPose(line.characterName, line.poseType);

        // Detailed debug logging for troubleshooting
        string lineName = line != null ? line.name : "<NULL_LINE>";
        string spriteName = poseSprite != null ? poseSprite.name : "<NULL_SPRITE>";
        Debug.Log($"[Narration_manager] ShowNarrationLine -> line='{lineName}', character='{line.characterName}', pose='{line.poseType}', sprite='{spriteName}'");

        if (poseSprite == null)
        {
            Debug.LogWarning($"Narration_manager: No pose sprite found for {line.characterName} with pose {line.poseType}.");
        }

        // Show message + sprite in narration popup
        narration_Pop.SetCharacterPanel(line.message, poseSprite);
    }

    public void Init()
    {
        InitializeCharacterPoses();
    }
    
    /// <summary>
    /// Hide the narration popup
    /// </summary>
    public void HideNarration()
    {
        if (narration_Pop != null && narration_Pop.gameObject.activeSelf)
        {
            narration_Pop.gameObject.SetActive(false);
            Debug.Log("[Narration_manager] Narration popup hidden");
        }
    }
}
