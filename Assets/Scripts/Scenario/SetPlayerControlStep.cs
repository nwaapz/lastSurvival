using UnityEngine;

/// <summary>
/// Scenario step that enables or disables player control for characters.
/// Use this to lock/unlock player input during cutscenes or scripted sequences.
/// </summary>
[CreateAssetMenu(fileName = "SetPlayerControlStep", menuName = "Scenario/Set Player Control Step")]
public class SetPlayerControlStep : ScenarioStep
{
    [Header("Control Settings")]
    [Tooltip("Enable or disable player control")]
    public bool enablePlayerControl = true;
    
    [Tooltip("Apply to all characters or just one specific character?")]
    public bool applyToAllCharacters = true;
    
    [Tooltip("If not applying to all, which character to control?")]
    public CharacterName targetCharacter = CharacterName.Hero;
    
    [Header("Completion")]
    [Tooltip("This step completes immediately after changing control state")]
    public bool completeImmediately = true;

    public override void OnEnter()
    {
        CharacterManager characterManager = ServiceLocator.Instance.Get<CharacterManager>();
        if (characterManager == null)
        {
            Debug.LogWarning("[SetPlayerControlStep] CharacterManager not found!");
            return;
        }
        
        if (applyToAllCharacters)
        {
            characterManager.SetAllPlayerControl(enablePlayerControl);
            Debug.Log($"[SetPlayerControlStep] Player control for ALL characters: {(enablePlayerControl ? "ENABLED" : "DISABLED")}");
        }
        else
        {
            characterManager.SetPlayerControl(targetCharacter, enablePlayerControl);
            Debug.Log($"[SetPlayerControlStep] Player control for {targetCharacter}: {(enablePlayerControl ? "ENABLED" : "DISABLED")}");
        }
    }

    public override bool UpdateStep()
    {
        return completeImmediately;
    }

    public override void OnExit()
    {
        // Cleanup if needed
    }
}
