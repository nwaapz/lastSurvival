using UnityEngine;

/// <summary>
/// Redirects animation events to the RunnerPlayerController in the parent.
/// Attach this to the child object that has the Animator.
/// </summary>
public class RunnerAnimationEvents : MonoBehaviour
{
    private RunnerPlayerController _controller;

    private void Start()
    {
        _controller = GetComponentInParent<RunnerPlayerController>();
        if (_controller == null)
        {
            Debug.LogError($"[RunnerAnimationEvents] No RunnerPlayerController found in parent of {gameObject.name}!");
        }
    }

    // Called by Animation Event "Fire"
    public void Fire()
    {
        if (_controller != null)
        {
            _controller.Fire();
        }
    }
}
