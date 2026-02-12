using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scenario step that configures which squad members will be spawned in the Fight scene.
/// Place this step BEFORE the scene transition to Fight scene.
/// The configuration is stored and read by RunnerSquadManager on scene load.
/// </summary>
[CreateAssetMenu(fileName = "ConfigureSquadStep", menuName = "Scenario/Configure Squad Step")]
public class ConfigureSquadStep : ScenarioStep
{
    [Header("Squad Configuration")]
    [Tooltip("List of character prefabs to spawn in this level. The RunnerSquadManager will use these.")]
    public List<SquadMemberConfig> squadMembers = new List<SquadMemberConfig>();
    
    [System.Serializable]
    public class SquadMemberConfig
    {
        [Tooltip("Character prefab with RunnerPlayerController")]
        public GameObject prefab;
        
        [Tooltip("Position offset from squad center (X = left/right, Z = forward/back)")]
        public Vector3 positionOffset;
        
        [Tooltip("Whether this member is active for this level")]
        public bool isActive = true;
    }
    
    [Header("Formation")]
    [Tooltip("If true, use these prefabs to replace the RunnerSquadManager's default configuration")]
    public bool overrideSquadMembers = true;
    
    [Tooltip("If overrideSquadMembers is false, just set how many of the default members to activate")]
    public int activeMemberCount = 1;
    
    /// <summary>
    /// Called when asset is created - set default values
    /// </summary>
    private void Reset()
    {
        // This step should NOT block and should work from any scene
        isBlocking = false;
        saveOnComplete = false;
        activeScene = ""; // Empty = any scene
        description = "Configure squad members for Fight scene";
    }
    
    public override void OnEnter()
    {
        // Store configuration for the Fight scene to use
        SquadConfigHolder.Instance.SetConfiguration(this);
        Debug.Log($"[ConfigureSquadStep] Squad configured with {squadMembers.Count} members");
    }
    
    public override bool UpdateStep()
    {
        // This step completes immediately - all work done in OnEnter
        return true;
    }
    
    public override void OnExit()
    {
        // Configuration is stored, nothing to clean up
    }
}

/// <summary>
/// Singleton MonoBehaviour that holds squad configuration between scenes.
/// Uses DontDestroyOnLoad to persist across scene transitions.
/// Created automatically when first accessed.
/// </summary>
public class SquadConfigHolder : MonoBehaviour
{
    private static SquadConfigHolder _instance;
    private static bool _applicationIsQuitting = false;
    
    // Reset statics when entering Play Mode (handles domain reload disabled scenario)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _applicationIsQuitting = false;
        _instance = null;
    }
    
    public static bool HasInstance => _instance != null;
    
    public static SquadConfigHolder Instance
    {
        get
        {
            // Don't create new instance if application is quitting
            if (_applicationIsQuitting)
            {
                return null;
            }
            
            if (_instance == null)
            {
                // Try to find existing instance first
                _instance = FindObjectOfType<SquadConfigHolder>();
                
                if (_instance == null)
                {
                    // Create a new GameObject with this component
                    GameObject holderObject = new GameObject("SquadConfigHolder");
                    _instance = holderObject.AddComponent<SquadConfigHolder>();
                    DontDestroyOnLoad(holderObject);
                    Debug.Log("[SquadConfigHolder] Created new persistent instance");
                }
            }
            return _instance;
        }
    }
    
    private void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }
    
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _applicationIsQuitting = true;
        }
    }
    
    private void Awake()
    {
        // Ensure only one instance exists
        if (_instance != null && _instance != this)
        {
            Debug.Log("[SquadConfigHolder] Duplicate instance found, destroying...");
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    // Current configuration
    public bool HasConfiguration { get; private set; }
    public bool OverrideSquadMembers { get; private set; }
    public int ActiveMemberCount { get; private set; }
    public List<ConfigureSquadStep.SquadMemberConfig> SquadMembers { get; private set; }
    
    public void SetConfiguration(ConfigureSquadStep step)
    {
        HasConfiguration = true;
        OverrideSquadMembers = step.overrideSquadMembers;
        ActiveMemberCount = step.activeMemberCount;
        
        // Copy the member configs
        SquadMembers = new List<ConfigureSquadStep.SquadMemberConfig>(step.squadMembers);
        
        Debug.Log($"[SquadConfigHolder] Configuration stored: Override={OverrideSquadMembers}, Members={SquadMembers.Count}, HasConfiguration={HasConfiguration}");
    }
    
    public void ClearConfiguration()
    {
        HasConfiguration = false;
        SquadMembers = null;
        Debug.Log("[SquadConfigHolder] Configuration cleared");
    }
}
