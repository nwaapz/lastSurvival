using UnityEngine;

/// <summary>
/// Scenario step that hides (deactivates) a character.
/// Use this to remove characters from the scene during scenarios.
/// </summary>
[CreateAssetMenu(fileName = "HideUnitStep", menuName = "Scenario/Hide Unit Step")]
public class HideUnitStep : ScenarioStep
{
    [Header("Character to Hide")]
    [Tooltip("Which character to hide")]
    public CharacterName characterToHide = CharacterName.Hero;
    
    [Header("Options")]
    [Tooltip("If true, character is deactivated. If false, just made invisible (renderer disabled).")]
    public bool deactivateGameObject = true;
    
    [Tooltip("Optional: Play animation before hiding (e.g., 'FadeOut', 'Disappear')")]
    public string hideAnimation;
    
    [Tooltip("Delay before hiding (seconds). Use if playing animation.")]
    public float hideDelay = 0f;

    private float _timer;
    private bool _hidden;

    public override void OnEnter()
    {
        _timer = 0f;
        _hidden = false;
        
        if (hideDelay <= 0f)
        {
            HideCharacter();
        }
    }

    public override bool UpdateStep()
    {
        if (_hidden) return true;
        
        _timer += Time.deltaTime;
        if (_timer >= hideDelay)
        {
            HideCharacter();
        }
        
        return _hidden;
    }

    private void HideCharacter()
    {
        CharacterManager characterManager = ServiceLocator.Instance?.Get<CharacterManager>();
        if (characterManager == null)
        {
            Debug.LogWarning("[HideUnitStep] CharacterManager not found!");
            _hidden = true;
            return;
        }
        
        CharacterController character = characterManager.GetCharacter(characterToHide);
        if (character == null)
        {
            Debug.LogWarning($"[HideUnitStep] Character '{characterToHide}' not found!");
            _hidden = true;
            return;
        }
        
        // Play animation if specified
        if (!string.IsNullOrEmpty(hideAnimation))
        {
            Animator animator = character.GetComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger(hideAnimation);
            }
        }
        
        if (deactivateGameObject)
        {
            character.gameObject.SetActive(false);
            Debug.Log($"[HideUnitStep] Deactivated {characterToHide}");
        }
        else
        {
            // Just hide renderers
            Renderer[] renderers = character.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                r.enabled = false;
            }
            Debug.Log($"[HideUnitStep] Made {characterToHide} invisible");
        }
        
        _hidden = true;
    }

    public override void OnExit()
    {
        // Cleanup if needed
    }
}
