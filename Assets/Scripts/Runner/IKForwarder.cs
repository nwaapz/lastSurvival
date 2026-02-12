using UnityEngine;

/// <summary>
/// Attach this script to the same GameObject that has the Animator component.
/// It forwards OnAnimatorIK calls to the parent RunnerPlayerController.
/// </summary>
public class IKForwarder : MonoBehaviour
{
    private RunnerPlayerController _controller;

    private void Start()
    {
        // Find the controller in parent hierarchy
        _controller = GetComponentInParent<RunnerPlayerController>();
        
        if (_controller == null)
        {
            Debug.LogError("[IKForwarder] Could not find RunnerPlayerController in parent!");
        }
        else
        {
            Debug.Log("[IKForwarder] Found RunnerPlayerController, will forward IK calls.");
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (_controller != null)
        {
            _controller.HandleAnimatorIK(layerIndex);
        }
    }
}
