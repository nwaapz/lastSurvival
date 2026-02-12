using UnityEngine;

/// <summary>
/// Generic MonoBehaviour singleton without FindObjectOfType.
/// Usage: public class GameManager : SingletonMono<GameManager> {}
/// </summary>
public class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
{
    [SerializeField] private bool isPersistent = false;
    
    private static T _instance;

    /// <summary>
    /// Check if an instance exists without triggering error logs.
    /// </summary>
    public static bool HasInstance => _instance != null;

    /// <summary>
    /// Access the singleton instance
    /// </summary>
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                Debug.LogError($"[Singleton] Instance of {typeof(T)} is null. Make sure a GameObject with {typeof(T)} exists in the scene.");
            }
            return _instance;
        }
    }

    /// <summary>
    /// Set the instance in Awake. Destroy duplicates automatically.
    /// </summary>
    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            
            // Only persist across scenes if the inspector boolean is ticked
            if (isPersistent)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Clear instance when destroyed to prevent stale references.
    /// </summary>
    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    /// <summary>
    /// Optional: prevent creating instance after quitting
    /// </summary>
    protected virtual void OnApplicationQuit()
    {
        _instance = null;
    }
}
