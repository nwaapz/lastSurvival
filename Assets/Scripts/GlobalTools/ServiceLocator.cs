using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Self-registering ServiceLocator. Services register themselves via Register<T>().
/// Persists across scene loads.
/// </summary>
public class ServiceLocator : SingletonMono<ServiceLocator>
{
    private readonly Dictionary<Type, IService> _serviceMap = new Dictionary<Type, IService>();

    protected override void Awake()
    {
        base.Awake();
        if (Instance == this)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// Register a service. Called by services in their Awake().
    /// </summary>
    public void Register<T>(T service) where T : class, IService
    {
        var type = typeof(T);
        
        if (_serviceMap.ContainsKey(type))
        {
            // Already registered - could be a duplicate or scene reload
            Debug.LogWarning($"[ServiceLocator] Service {type.Name} already registered. Replacing with new instance.");
            _serviceMap[type] = service;
        }
        else
        {
            _serviceMap.Add(type, service);
            Debug.Log($"[ServiceLocator] Registered: {type.Name}");
        }
        
        // Initialize the service
        service.Init();
    }

    /// <summary>
    /// Unregister a service. Called by services in their OnDestroy().
    /// </summary>
    public void Unregister<T>() where T : class, IService
    {
        var type = typeof(T);
        
        if (_serviceMap.ContainsKey(type))
        {
            _serviceMap.Remove(type);
            Debug.Log($"[ServiceLocator] Unregistered: {type.Name}");
        }
    }

    /// <summary>
    /// Get a registered service. Returns null if not found.
    /// </summary>
    public T Get<T>() where T : class
    {
        if (_serviceMap.TryGetValue(typeof(T), out var service))
        {
            return service as T;
        }

        // Don't log error - service might legitimately not exist in current scene
        return null;
    }
    
    /// <summary>
    /// Check if a service is registered.
    /// </summary>
    public bool Has<T>() where T : class
    {
        return _serviceMap.ContainsKey(typeof(T));
    }
}
