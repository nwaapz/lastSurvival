using UnityEngine;

[CreateAssetMenu(fileName = "MoveStep", menuName = "Scenario/Move Step")]
public class MoveObjectiveStep : ScenarioStep
{
    [Header("Target")]
    public Vector3 targetPosition;
    public float radius = 2f;
    public string playerTag = "Player";

    [Header("Restrictions")]
    [Tooltip("If true, restricts clicks to the ground near the target? (Not implemented deeply yet)")]
    public bool restrictMovement = false;

    private Transform _playerTransform;

    public override void OnEnter()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            _playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning($"[MoveObjectiveStep] No object found with tag '{playerTag}'. Step will not complete unless found.");
        }

        if (restrictMovement && BaseBuilderClickManager.Instance != null)
        {
            BaseBuilderClickManager.Instance.InteractionFilter = IsInteractionAllowed;
        }
    }

    private bool IsInteractionAllowed(GameObject obj)
    {
        // Allow clicking Ground only?
        // Or allow clicking near the target?
        // For now, let's just allow Ground clicks.
        // Assuming Ground has layer or tag.
        // This is a simple implementation.
        return true; 
    }

    public override bool UpdateStep()
    {
        if (_playerTransform == null)
        {
            // Try to find it again (maybe spawned late)
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null) _playerTransform = player.transform;
            return false;
        }

        float distance = Vector3.Distance(_playerTransform.position, targetPosition);
        return distance <= radius;
    }

    public override void OnExit()
    {
        if (restrictMovement && BaseBuilderClickManager.Instance != null)
        {
            BaseBuilderClickManager.Instance.InteractionFilter = null;
        }
    }
}
