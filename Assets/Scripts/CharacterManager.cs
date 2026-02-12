using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all characters in the scene.
/// Allows finding characters by their CharacterName enum.
/// Used by scenario system to control specific characters.
/// Registered with ServiceLocator for easy access.
/// </summary>
public class CharacterManager : SingletonMono<CharacterManager>, IService
{
    [Header("Configuration")]
    [Tooltip("Registry mapping CharacterName to Prefabs")]
    [SerializeField] private CharacterPrefabRegistry prefabRegistry;
    
    private Dictionary<CharacterName, CharacterController> _characters = new Dictionary<CharacterName, CharacterController>();

    protected override void Awake()
    {
        base.Awake();
        
        // Self-register with ServiceLocator
        if (ServiceLocator.HasInstance)
        {
            ServiceLocator.Instance.Register<CharacterManager>(this);
        }
    }
    
    protected override void OnDestroy()
    {
        if (ServiceLocator.HasInstance)
        {
            ServiceLocator.Instance.Unregister<CharacterManager>();
        }
        base.OnDestroy();
    }

    public void Init()
    {
        RegisterAllCharacters();
        Debug.Log("[CharacterManager] Initialized via ServiceLocator");
    }
    
    /// <summary>
    /// Spawn a character at a specific position and rotation.
    /// Uses the assigned CharacterPrefabRegistry.
    /// </summary>
    public CharacterController SpawnCharacter(CharacterName characterName, Vector3 position, Quaternion rotation)
    {
        Debug.Log($"[CharacterManager] SpawnCharacter called for {characterName} at {position}");
        
        if (prefabRegistry == null)
        {
            Debug.LogError("[CharacterManager] Cannot spawn character - No Prefab Registry assigned!");
            return null;
        }
        
        Debug.Log($"[CharacterManager] Prefab Registry has {prefabRegistry.entries.Count} entries");
        
        GameObject prefab = prefabRegistry.GetPrefab(characterName);
        if (prefab == null)
        {
            Debug.LogError($"[CharacterManager] No prefab found for {characterName} in registry!");
            return null;
        }
        
        Debug.Log($"[CharacterManager] Found prefab: {prefab.name}");
        
        GameObject instance = Instantiate(prefab, position, rotation);
        CharacterController controller = instance.GetComponent<CharacterController>();
        
        if (controller == null)
        {
            Debug.LogWarning($"[CharacterManager] Prefab for {characterName} is missing CharacterController component!");
            // Try to add it? Or fail? Better to fail and fix prefab.
        }
        else
        {
            // Ensure name matches
            controller.characterName = characterName;
            
            // Register immediately
            _characters[characterName] = controller;
            Debug.Log($"[CharacterManager] Spawned and registered: {characterName}");
        }
        
        return controller;
    }

    private void RegisterAllCharacters()
    {
        _characters.Clear();
        
        CharacterController[] allCharacters = FindObjectsOfType<CharacterController>();
        foreach (var character in allCharacters)
        {
            if (_characters.ContainsKey(character.characterName))
            {
                Debug.LogWarning($"[CharacterManager] Duplicate character found: {character.characterName}. Using first instance.");
                continue;
            }
            
            _characters[character.characterName] = character;
            Debug.Log($"[CharacterManager] Registered character: {character.characterName}");
        }
        
        Debug.Log($"[CharacterManager] Total characters registered: {_characters.Count}");
    }

    /// <summary>
    /// Get a character by their CharacterName
    /// </summary>
    public CharacterController GetCharacter(CharacterName characterName)
    {
        if (_characters.TryGetValue(characterName, out CharacterController character))
        {
            return character;
        }
        
        Debug.LogWarning($"[CharacterManager] Character '{characterName}' not found in scene!");
        return null;
    }

    /// <summary>
    /// Enable or disable player control for a specific character
    /// </summary>
    public void SetPlayerControl(CharacterName characterName, bool enabled)
    {
        CharacterController character = GetCharacter(characterName);
        if (character != null)
        {
            character.SetPlayerControlEnabled(enabled);
        }
    }

    /// <summary>
    /// Enable or disable player control for ALL characters
    /// </summary>
    public void SetAllPlayerControl(bool enabled)
    {
        foreach (var character in _characters.Values)
        {
            character.SetPlayerControlEnabled(enabled);
        }
        
        Debug.Log($"[CharacterManager] All character player control: {(enabled ? "ENABLED" : "DISABLED")}");
    }

    /// <summary>
    /// Command a character to move to a position
    /// </summary>
    public void MoveCharacterTo(CharacterName characterName, Vector3 position)
    {
        CharacterController character = GetCharacter(characterName);
        if (character != null)
        {
            character.MoveToPosition(position);
        }
    }

    /// <summary>
    /// Stop a character's movement
    /// </summary>
    public void StopCharacter(CharacterName characterName)
    {
        CharacterController character = GetCharacter(characterName);
        if (character != null)
        {
            character.Stop();
        }
    }

    /// <summary>
    /// Check if a character is moving
    /// </summary>
    public bool IsCharacterMoving(CharacterName characterName)
    {
        CharacterController character = GetCharacter(characterName);
        return character != null && character.IsMoving;
    }

    /// <summary>
    /// Refresh the character registry (call if characters are spawned/destroyed at runtime)
    /// </summary>
    public void RefreshCharacters()
    {
        RegisterAllCharacters();
    }
}
