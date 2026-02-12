using UnityEngine;

/// <summary>
/// Visualizes lane positions in the scene for debugging and design.
/// Can also create runtime lane indicators.
/// </summary>
public class RunnerLaneVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RunnerLaneConfig laneConfig;
    
    [Header("Visualization")]
    [SerializeField] private bool showInGame = false;
    [SerializeField] private bool showInEditor = true;
    [SerializeField] private float laneLength = 100f;
    [SerializeField] private float laneStartZ = -10f;
    
    [Header("Colors")]
    [SerializeField] private Color laneColor = new Color(1f, 1f, 1f, 0.3f);
    [SerializeField] private Color centerLaneColor = new Color(0f, 1f, 0f, 0.5f);
    [SerializeField] private Color boundaryColor = new Color(1f, 0f, 0f, 0.5f);
    
    [Header("Runtime Indicators")]
    [SerializeField] private GameObject laneIndicatorPrefab;
    [SerializeField] private Transform indicatorParent;
    
    private LineRenderer[] _laneLines;

    private void Start()
    {
        if (laneConfig == null && RunnerGameManager.Instance != null)
        {
            laneConfig = RunnerGameManager.Instance.LaneConfig;
        }
        
        if (showInGame)
        {
            CreateRuntimeIndicators();
        }
    }

    private void CreateRuntimeIndicators()
    {
        if (laneConfig == null) return;
        
        // Create line renderers for each lane boundary
        int lineCount = laneConfig.LaneCount + 1;
        _laneLines = new LineRenderer[lineCount];
        
        float halfWidth = laneConfig.LaneWidth / 2f;
        
        for (int i = 0; i < lineCount; i++)
        {
            GameObject lineObj = new GameObject($"LaneLine_{i}");
            lineObj.transform.SetParent(indicatorParent != null ? indicatorParent : transform);
            
            LineRenderer line = lineObj.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.startWidth = 0.05f;
            line.endWidth = 0.05f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            
            // Calculate X position
            float x;
            if (i == 0)
            {
                x = laneConfig.GetLanePosition(0) - halfWidth;
            }
            else if (i == lineCount - 1)
            {
                x = laneConfig.GetLanePosition(laneConfig.LaneCount - 1) + halfWidth;
            }
            else
            {
                x = (laneConfig.GetLanePosition(i - 1) + laneConfig.GetLanePosition(i)) / 2f;
            }
            
            // Set positions
            line.SetPosition(0, new Vector3(x, 0.01f, laneStartZ));
            line.SetPosition(1, new Vector3(x, 0.01f, laneStartZ + laneLength));
            
            // Set color
            Color color = (i == 0 || i == lineCount - 1) ? boundaryColor : laneColor;
            line.startColor = color;
            line.endColor = color;
            
            _laneLines[i] = line;
        }
    }

    private void OnDrawGizmos()
    {
        if (!showInEditor) return;
        if (laneConfig == null) return;
        
        float halfWidth = laneConfig.LaneWidth / 2f;
        
        // Draw lane centers
        for (int i = 0; i < laneConfig.LaneCount; i++)
        {
            float x = laneConfig.GetLanePosition(i);
            
            // Lane center color (green for middle lane)
            bool isCenter = i == laneConfig.LaneCount / 2;
            Gizmos.color = isCenter ? centerLaneColor : laneColor;
            
            // Draw lane center line
            Vector3 start = new Vector3(x, 0.1f, laneStartZ);
            Vector3 end = new Vector3(x, 0.1f, laneStartZ + laneLength);
            Gizmos.DrawLine(start, end);
            
            // Draw lane marker at start
            Gizmos.DrawWireSphere(start, 0.3f);
        }
        
        // Draw lane boundaries
        Gizmos.color = boundaryColor;
        
        // Left boundary
        float leftX = laneConfig.GetLanePosition(0) - halfWidth;
        Gizmos.DrawLine(
            new Vector3(leftX, 0.1f, laneStartZ),
            new Vector3(leftX, 0.1f, laneStartZ + laneLength)
        );
        
        // Right boundary
        float rightX = laneConfig.GetLanePosition(laneConfig.LaneCount - 1) + halfWidth;
        Gizmos.DrawLine(
            new Vector3(rightX, 0.1f, laneStartZ),
            new Vector3(rightX, 0.1f, laneStartZ + laneLength)
        );
        
        // Draw spawn line
        Gizmos.color = Color.red;
        Gizmos.DrawLine(
            new Vector3(leftX, 0.1f, laneStartZ + laneLength),
            new Vector3(rightX, 0.1f, laneStartZ + laneLength)
        );
    }
}
