using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Simulates a 17th century candlelight flicker effect on a 2D spotlight.
/// Creates subtle, organic variations in light intensity.
/// </summary>
[RequireComponent(typeof(Light2D))]
public class CandleLightFlicker : MonoBehaviour
{
    [Header("Intensity Settings")]
    [SerializeField] private float baseIntensity = 1f;
    [SerializeField, Range(0f, 0.5f)] private float flickerAmount = 0.15f;
    
    [Header("Flicker Speed")]
    [SerializeField] private float flickerSpeed = 3f;
    [SerializeField] private float randomSpeedVariation = 1.5f;
    
    [Header("Noise Settings")]
    [SerializeField] private float primaryNoiseScale = 1f;
    [SerializeField] private float secondaryNoiseScale = 2.3f;
    
    private Light2D light2D;
    private float timeOffset;
    private float currentSpeed;
    
    private void Awake()
    {
        light2D = GetComponent<Light2D>();
        timeOffset = Random.Range(0f, 100f);
        currentSpeed = flickerSpeed;
    }
    
    private void Start()
    {
        if (baseIntensity <= 0f)
        {
            baseIntensity = light2D.intensity;
        }
    }
    
    private void Update()
    {
        float time = Time.time + timeOffset;
        
        // Primary slow flicker (main candle movement)
        float primaryNoise = Mathf.PerlinNoise(time * currentSpeed * primaryNoiseScale, 0f);
        
        // Secondary faster flicker (flame flutter)
        float secondaryNoise = Mathf.PerlinNoise(time * currentSpeed * secondaryNoiseScale, 10f);
        
        // Combine noises with different weights
        float combinedNoise = (primaryNoise * 0.7f) + (secondaryNoise * 0.3f);
        
        // Map to intensity range
        float intensityVariation = Mathf.Lerp(-flickerAmount, flickerAmount, combinedNoise);
        light2D.intensity = baseIntensity + intensityVariation;
        
        // Occasionally vary the speed slightly for more organic feel
        if (Random.value < 0.01f)
        {
            currentSpeed = flickerSpeed + Random.Range(-randomSpeedVariation, randomSpeedVariation);
        }
    }
    
    /// <summary>
    /// Sets the base intensity at runtime.
    /// </summary>
    public void SetBaseIntensity(float intensity)
    {
        baseIntensity = intensity;
    }
    
    /// <summary>
    /// Sets the flicker amount at runtime.
    /// </summary>
    public void SetFlickerAmount(float amount)
    {
        flickerAmount = Mathf.Clamp01(amount);
    }
}
