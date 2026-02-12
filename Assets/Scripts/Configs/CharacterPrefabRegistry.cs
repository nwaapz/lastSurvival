using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Configures which prefab to use for each CharacterName enum.
/// cCreate one of these assets and assign it to CharacterManager or spawn steps.
/// </summary>
[CreateAssetMenu(fileName = "CharacterPrefabRegistry", menuName = "Configs/Character Prefab Registry")]
public class CharacterPrefabRegistry : ScriptableObject
{
    [System.Serializable]
    public class CharacterPrefabEntry
    {
        public CharacterName characterName;
        public GameObject prefab;
    }
    
    public List<CharacterPrefabEntry> entries = new List<CharacterPrefabEntry>();
    
    public GameObject GetPrefab(CharacterName characterName)
    {
        foreach (var entry in entries)
        {
            if (entry.characterName == characterName)
            {
                return entry.prefab;
            }
        }
        return null;
    }
}
