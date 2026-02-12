using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Service that manages all named locations in the map.
/// Maps LocationName enum to transforms for easy access by scenario system.
/// Registered with ServiceLocator for global access.
/// </summary>
public class LocationManager : SingletonMono<LocationManager>, IService
{
    [Header("Location Registry")]
    [Tooltip("Manually assigned locations (optional - can also auto-register from NamedLocation components)")]
    [SerializeField] private List<LocationEntry> manualLocations = new List<LocationEntry>();
    
    [Header("Settings")]
    [Tooltip("If true, automatically find and register all NamedLocation components in scene")]
    [SerializeField] private bool autoRegisterFromScene = true;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private Dictionary<LocationName, Transform> _locationRegistry = new Dictionary<LocationName, Transform>();
    private Dictionary<string, Transform> _stringLocationRegistry = new Dictionary<string, Transform>();

    [Serializable]
    public class LocationEntry
    {
        public LocationName locationName;
        public Transform transform;
    }

    protected override void Awake()
    {
        base.Awake();
        
        // Self-register with ServiceLocator
        if (ServiceLocator.HasInstance)
        {
            ServiceLocator.Instance.Register<LocationManager>(this);
        }
    }
    
    protected override void OnDestroy()
    {
        if (ServiceLocator.HasInstance)
        {
            ServiceLocator.Instance.Unregister<LocationManager>();
        }
        base.OnDestroy();
    }

    public void Init()
    {
        RegisterAllLocations();
        Debug.Log("[LocationManager] Initialized via ServiceLocator");
    }

    private void RegisterAllLocations()
    {
        _locationRegistry.Clear();
        _stringLocationRegistry.Clear();
        
        // Register manual locations first
        foreach (var entry in manualLocations)
        {
            if (entry.transform != null && entry.locationName != LocationName.None)
            {
                RegisterLocation(entry.locationName, entry.transform);
            }
        }
        
        // Auto-register from NamedLocation components in scene
        if (autoRegisterFromScene)
        {
            NamedLocation[] allLocations = FindObjectsOfType<NamedLocation>();
            foreach (var namedLoc in allLocations)
            {
                // Try to parse locationId as enum
                if (Enum.TryParse<LocationName>(namedLoc.locationId, true, out LocationName locName))
                {
                    if (!_locationRegistry.ContainsKey(locName))
                    {
                        RegisterLocation(locName, namedLoc.transform);
                    }
                }
                
                // Also register by string ID for backward compatibility
                if (!string.IsNullOrEmpty(namedLoc.locationId))
                {
                    string key = namedLoc.locationId.ToLowerInvariant();
                    if (!_stringLocationRegistry.ContainsKey(key))
                    {
                        _stringLocationRegistry[key] = namedLoc.transform;
                        
                        if (showDebugLogs)
                        {
                            Debug.Log($"[LocationManager] Registered string location: '{namedLoc.locationId}'");
                        }
                    }
                }
            }
        }
        
        if (showDebugLogs)
        {
            Debug.Log($"[LocationManager] Total locations registered: {_locationRegistry.Count} (enum) + {_stringLocationRegistry.Count} (string)");
        }
    }

    /// <summary>
    /// Register a location with an enum key
    /// </summary>
    public void RegisterLocation(LocationName locationName, Transform transform)
    {
        if (locationName == LocationName.None || transform == null) return;
        
        _locationRegistry[locationName] = transform;
        
        if (showDebugLogs)
        {
            Debug.Log($"[LocationManager] Registered: {locationName} -> {transform.name}");
        }
    }

    /// <summary>
    /// Get transform for a location by enum
    /// </summary>
    public Transform GetLocation(LocationName locationName)
    {
        if (_locationRegistry.TryGetValue(locationName, out Transform transform))
        {
            return transform;
        }
        
        Debug.LogWarning($"[LocationManager] Location '{locationName}' not found!");
        return null;
    }

    /// <summary>
    /// Get position for a location by enum
    /// </summary>
    public Vector3 GetPosition(LocationName locationName)
    {
        Transform t = GetLocation(locationName);
        return t != null ? t.position : Vector3.zero;
    }

    /// <summary>
    /// Get transform for a location by string ID (backward compatibility with NamedLocation.locationId)
    /// </summary>
    public Transform GetLocation(string locationId)
    {
        if (string.IsNullOrEmpty(locationId)) return null;
        
        // First try enum parse
        if (Enum.TryParse<LocationName>(locationId, true, out LocationName locName))
        {
            if (_locationRegistry.TryGetValue(locName, out Transform t))
            {
                return t;
            }
        }
        
        // Fallback to string registry
        string key = locationId.ToLowerInvariant();
        if (_stringLocationRegistry.TryGetValue(key, out Transform transform))
        {
            return transform;
        }
        
        Debug.LogWarning($"[LocationManager] Location '{locationId}' not found!");
        return null;
    }

    /// <summary>
    /// Get position for a location by string ID
    /// </summary>
    public Vector3 GetPosition(string locationId)
    {
        Transform t = GetLocation(locationId);
        return t != null ? t.position : Vector3.zero;
    }

    /// <summary>
    /// Check if a location exists
    /// </summary>
    public bool HasLocation(LocationName locationName)
    {
        return _locationRegistry.ContainsKey(locationName);
    }

    /// <summary>
    /// Check if a location exists by string
    /// </summary>
    public bool HasLocation(string locationId)
    {
        if (string.IsNullOrEmpty(locationId)) return false;
        
        if (Enum.TryParse<LocationName>(locationId, true, out LocationName locName))
        {
            if (_locationRegistry.ContainsKey(locName)) return true;
        }
        
        return _stringLocationRegistry.ContainsKey(locationId.ToLowerInvariant());
    }

    /// <summary>
    /// Get all registered location names
    /// </summary>
    public IEnumerable<LocationName> GetAllLocationNames()
    {
        return _locationRegistry.Keys;
    }

    /// <summary>
    /// Refresh the location registry (call if locations are added/removed at runtime)
    /// </summary>
    public void RefreshLocations()
    {
        RegisterAllLocations();
    }

    /// <summary>
    /// Calculate distance between two locations
    /// </summary>
    public float GetDistance(LocationName from, LocationName to)
    {
        Vector3 fromPos = GetPosition(from);
        Vector3 toPos = GetPosition(to);
        return Vector3.Distance(fromPos, toPos);
    }
}
