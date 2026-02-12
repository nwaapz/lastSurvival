using UnityEngine;

/// <summary>
/// Provides a "Look At" functionality for Generic rigs by manually rotating a bone (e.g. Head) 
/// towards a target in LateUpdate. This mimics Animator.SetLookAtPosition behavior.
/// </summary>
public class GenericRigLookAt : MonoBehaviour
{
    [Header("Bone Settings")]
    [Tooltip("The bone to rotate (e.g. Head, Spine, Chest). THIS IS REQUIRED.")]
    [UnityEngine.Serialization.FormerlySerializedAs("headBone")]
    [SerializeField] private Transform boneToRotate;
    
    [Tooltip("Optional: The root/parent object to calculate local angles relative to (usually the character root).")]
    [SerializeField] private Transform characterRoot;

    [Header("Target")]
    [Tooltip("The object to look at. If null, use SetLookAtPosition().")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 targetOffset;

    [Header("Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float weight = 1.0f;
    [SerializeField] private float smoothing = 5f;
    [Range(0f, 180f)]
    [SerializeField] private float maxHeadAngle = 80f;
    
    [Tooltip("Adjust this if your bone's forward axis is not Z. Adds local rotation.")]
    [SerializeField] private Vector3 boneRotationOffset = new Vector3(0, 90, -90); // Common for Mixamo/Generic rigs where Y is often axis

    private Vector3 _currentLookPos;
    private float _currentWeight;
    private Vector3 _explicitTargetPos;
    private bool _useExplicitTarget;

    private void Start()
    {
        if (characterRoot == null)
            characterRoot = transform;

        if (boneToRotate == null)
        {
            Debug.LogWarning($"[GenericRigLookAt] No Bone to rotate assigned on {gameObject.name}. Please assign it in Inspector.", this);
        }
        
        _currentWeight = 0f;
    }

    /// <summary>
    /// Set a static world position to look at.
    /// </summary>
    public void SetLookAtPosition(Vector3 pos)
    {
        _explicitTargetPos = pos;
        _useExplicitTarget = true;
        target = null;
    }

    /// <summary>
    /// Set a transform to follow.
    /// </summary>
    public void SetLookAtTarget(Transform t)
    {
        target = t;
        _useExplicitTarget = false;
    }

    /// <summary>
    /// Clear the look target and return to animation.
    /// </summary>
    public void ClearLookTarget()
    {
        target = null;
        _useExplicitTarget = false;
    }

    private void LateUpdate()
    {
        if (boneToRotate == null) return;

        // Determine target position
        Vector3 targetPos = Vector3.zero;
        bool hasTarget = false;

        if (target != null)
        {
            targetPos = target.position + targetOffset;
            hasTarget = true;
        }
        else if (_useExplicitTarget)
        {
            targetPos = _explicitTargetPos + targetOffset;
            hasTarget = true;
        }

        // Smoothly blend weight
        float targetWeight = hasTarget ? weight : 0f;
        _currentWeight = Mathf.Lerp(_currentWeight, targetWeight, Time.deltaTime * smoothing);

        if (_currentWeight <= 0.01f) return;

        // --- Calculation ---

        // 1. Get direction to target
        Vector3 dirToTarget = targetPos - boneToRotate.position;
        
        // 2. Clamp angle to avoid breaking neck/spine
        // We compare direction against the character's forward
        Vector3 charForward = characterRoot.forward;
        float angle = Vector3.Angle(charForward, dirToTarget);
        
        if (angle > maxHeadAngle)
        {
            // Limit the direction vector to the max angle
            // Simple way: Rotate character forward towards target by maxAngle
            // A more robust way is using RotateTowards
            dirToTarget = Vector3.RotateTowards(charForward, dirToTarget, maxHeadAngle * Mathf.Deg2Rad, 0f);
        }

        // 3. Calculate Look Rotation
        // Create rotation looking at target with character's Up vector
        Quaternion targetRotation = Quaternion.LookRotation(dirToTarget, characterRoot.up);

        // 4. Apply Bone Offset (Correcting for bone axis)
        // Bones are often not Z-forward. We might need to apply an offset.
        // Usually we want: finalRotation = targetRotation * inverse(boneOffset)
        // Or easier: multiply by offset Euler
        targetRotation *= Quaternion.Euler(boneRotationOffset);

        // 5. Blend with current animation rotation
        boneToRotate.rotation = Quaternion.Slerp(boneToRotate.rotation, targetRotation, _currentWeight);
    }

    private void OnDrawGizmos()
    {
        if (boneToRotate != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(boneToRotate.position, boneToRotate.forward * 0.5f);
            
            Gizmos.color = Color.green;
            // Show the 'Up' vector to help understand twist
            Gizmos.DrawRay(boneToRotate.position, boneToRotate.up * 0.3f);
        }
    }
}
