using UnityEngine;

[CreateAssetMenu(fileName = "New Narration Line", menuName = "Narration/Narration Line")]
public class NarrationLine : ScriptableObject
{
    [Header("Character Settings")]
    public CharacterName characterName;   // dropdown of CharacterName enum
    public PoseType poseType;             // dropdown of PoseType enum

    [Header("Text Settings")]
    [TextArea(3, 10)]
    public string message;                // multi-line message text
}
