using UnityEngine;

public class CornerAnchor : MonoBehaviour
{
    public enum Corner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    [SerializeField] Corner anchorCorner = Corner.TopRight;
    [SerializeField] Vector2 offset = Vector2.zero; // Offset from corner in world units
    [SerializeField] Camera targetCamera;

    void Start()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    void LateUpdate()
    {
        if (targetCamera == null)
            return;

        UpdatePosition();
    }

    void UpdatePosition()
    {
        Vector3 viewportPos = Vector3.zero;

        switch (anchorCorner)
        {
            case Corner.TopLeft:
                viewportPos = new Vector3(0, 1, targetCamera.nearClipPlane);
                break;
            case Corner.TopRight:
                viewportPos = new Vector3(1, 1, targetCamera.nearClipPlane);
                break;
            case Corner.BottomLeft:
                viewportPos = new Vector3(0, 0, targetCamera.nearClipPlane);
                break;
            case Corner.BottomRight:
                viewportPos = new Vector3(1, 0, targetCamera.nearClipPlane);
                break;
        }

        // Convert viewport position to world position
        Vector3 worldPos = targetCamera.ViewportToWorldPoint(viewportPos);
        worldPos.z = transform.position.z; // Maintain sprite's Z position

        // Apply offset
        worldPos.x += offset.x;
        worldPos.y += offset.y;

        transform.position = worldPos;
    }

    // Call this if you want to force an update outside of LateUpdate
    public void ForceUpdate()
    {
        UpdatePosition();
    }

  
}
