using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Example script demonstrating how to use the enum-based pose system
/// </summary>
public class PoseSystemExample : MonoBehaviour
{
    [SerializeField] private Image characterImage;
    [SerializeField] private CharacterName character = CharacterName.Hero;
    
    // Example: Get a pose using the enum (Type-safe, recommended)
    public void ShowIdlePose()
    {
        Sprite pose = Narration_manager.Instance.GetCharacterPose(character, PoseType.Idle);
        if (pose != null && characterImage != null)
        {
            characterImage.sprite = pose;
        }
    }
    
    // Example: Get a pose using string (Backward compatible, but not type-safe)
    public void ShowPoseByString(string poseName)
    {
        Sprite pose = Narration_manager.Instance.GetCharacterPose(character, poseName);
        if (pose != null && characterImage != null)
        {
            characterImage.sprite = pose;
        }
    }
    
    // Example: Change pose based on enum
    public void SetPose(PoseType poseType)
    {
        Sprite pose = Narration_manager.Instance.GetCharacterPose(character, poseType);
        if (pose != null && characterImage != null)
        {
            characterImage.sprite = pose;
        }
    }
    
    // Example: Check if a pose exists before using it
    public void SetPoseIfExists(PoseType poseType)
    {
        CharacterPoses poses = Narration_manager.Instance.GetCharacterPoses(character);
        if (poses != null && poses.HasPose(poseType))
        {
            characterImage.sprite = poses.GetPose(poseType);
        }
        else
        {
            Debug.Log($"Pose {poseType} not configured for {character}");
        }
    }
}
