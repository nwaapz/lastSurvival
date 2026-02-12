using UnityEngine;
using TMPro;

public class FPSDisplay : MonoBehaviour
{
    [Tooltip("Assign the TextMeshPro component here")]
    [SerializeField] private TMP_Text fpsText;
    
    [Tooltip("How often to update the FPS display (in seconds)")]
    [SerializeField] private float updateInterval = 0.2f;
    
    private float _timeSinceLastUpdate = 0f;
    private int _framesSinceLastUpdate = 0;

    private void Update()
    {
        if (fpsText == null) return;

        _timeSinceLastUpdate += Time.unscaledDeltaTime;
        _framesSinceLastUpdate++;

        if (_timeSinceLastUpdate >= updateInterval)
        {
            float fps = _framesSinceLastUpdate / _timeSinceLastUpdate;
            fpsText.text = $"FPS: {Mathf.RoundToInt(fps)}";
            
            _timeSinceLastUpdate = 0f;
            _framesSinceLastUpdate = 0;
        }
    }
}
