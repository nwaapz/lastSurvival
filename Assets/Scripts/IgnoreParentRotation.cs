using UnityEngine;

public class IgnoreParentRotation : MonoBehaviour
{
    private Transform parent;
    private Quaternion wantedWorldRotation;

    void Awake()
    {
        parent = transform.parent;
        wantedWorldRotation = transform.rotation; // keep whatever rotation it starts with
    }

    void LateUpdate()
    {
        if (!parent) return;

        // keep following the parent position (optional)
        // If you want to keep local offset, use parent.TransformPoint(localOffset) instead.
        // transform.position = parent.position;

        // do NOT follow parent rotation
        transform.rotation = wantedWorldRotation;
    }
}
