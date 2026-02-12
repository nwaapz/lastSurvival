using UnityEngine;

/// <summary>
/// Scrolls the ground/environment to create the illusion of forward movement.
/// The player stays in place while the world moves toward them.
/// </summary>
public class RunnerGroundScroller : MonoBehaviour
{
    [Header("Ground Tiles")]
    [SerializeField] private Transform[] groundTiles;
    [SerializeField] private float tileLength = 20f;
    
    [Header("Scrolling")]
    [SerializeField] private bool useGameSpeed = true;
    [SerializeField] private float customSpeed = 5f;
    
    [Header("Material Scrolling")]
    [SerializeField] private Renderer groundRenderer;
    [SerializeField] private bool scrollMaterial = false;
    [SerializeField] private Vector2 scrollDirection = new Vector2(0, 1);
    
    private float _scrollOffset;

    private void Update()
    {
        if (RunnerGameManager.Instance != null && 
            RunnerGameManager.Instance.CurrentState != RunnerGameManager.GameState.Playing)
        {
            return;
        }
        
        float speed = GetCurrentSpeed();
        
        ScrollTiles(speed);
        
        if (scrollMaterial)
        {
            ScrollMaterial(speed);
        }
    }

    private float GetCurrentSpeed()
    {
        if (useGameSpeed && RunnerGameManager.Instance != null)
        {
            return RunnerGameManager.Instance.CurrentGameSpeed;
        }
        return customSpeed;
    }

    #region Tile Scrolling
    
    private void ScrollTiles(float speed)
    {
        if (groundTiles == null || groundTiles.Length == 0) return;
        
        foreach (var tile in groundTiles)
        {
            if (tile == null) continue;
            
            // Move tile toward player (negative Z)
            tile.position += Vector3.back * speed * Time.deltaTime;
            
            // Check if tile has passed the camera
            if (tile.position.z < -tileLength)
            {
                // Reposition to the front
                RepositionTile(tile);
            }
        }
    }
    
    private void RepositionTile(Transform tile)
    {
        // Find the furthest tile
        float maxZ = float.MinValue;
        foreach (var t in groundTiles)
        {
            if (t != null && t.position.z > maxZ)
            {
                maxZ = t.position.z;
            }
        }
        
        // Place this tile after the furthest one
        Vector3 pos = tile.position;
        pos.z = maxZ + tileLength;
        tile.position = pos;
    }
    
    #endregion

    #region Material Scrolling
    
    private void ScrollMaterial(float speed)
    {
        if (groundRenderer == null) return;
        
        _scrollOffset += speed * Time.deltaTime * 0.1f; // Scale factor for texture
        
        Vector2 offset = scrollDirection * _scrollOffset;
        groundRenderer.material.mainTextureOffset = offset;
    }
    
    #endregion

    #region Public Methods
    
    /// <summary>
    /// Set custom scroll speed
    /// </summary>
    public void SetSpeed(float speed)
    {
        customSpeed = speed;
    }
    
    /// <summary>
    /// Reset scroll position
    /// </summary>
    public void ResetScroll()
    {
        _scrollOffset = 0f;
        
        if (groundRenderer != null)
        {
            groundRenderer.material.mainTextureOffset = Vector2.zero;
        }
    }
    
    #endregion
}
