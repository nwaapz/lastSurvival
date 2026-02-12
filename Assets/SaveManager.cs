using UnityEngine;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public class BuildingSaveEntry
{
    public string BuildingId;
    public int Level = 0; // 0 = not built yet
}

[System.Serializable]
public class GameData
{
    public int Coins = 0;
    public int CurrentLevel = 1;
    public int HighScore = 0;
    
    // Building progress data
    public List<BuildingSaveEntry> BuildingLevels = new List<BuildingSaveEntry>();
    
    // Scenario progress tracking
    public int CurrentScenarioStepIndex = 0; // Which step in the current level's scenario
    public bool ScenarioCompleted = false;   // Has the current level's scenario been completed?
}

public class SaveManager : SingletonMono<SaveManager>, IService
{
    private string saveFilePath;
    private GameData _data;

    public GameData Data
    {
        get
        {
            if (_data == null)
            {
                Debug.LogWarning("[SaveManager] Data accessed before Init. Force loading...");
                LoadGame();
            }
            return _data;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        if (Instance == this)
        {
            DontDestroyOnLoad(gameObject);
            
            // Self-register with ServiceLocator
            if (ServiceLocator.HasInstance)
            {
                ServiceLocator.Instance.Register<SaveManager>(this);
            }
        }
    }
    
    protected override void OnDestroy()
    {
        if (ServiceLocator.HasInstance)
        {
            ServiceLocator.Instance.Unregister<SaveManager>();
        }
        base.OnDestroy();
    }

    public void Init()
    {
        saveFilePath = Path.Combine(Application.persistentDataPath, "gamedata.json");
        // Only load if not already loaded (lazy loaded)
        if (_data == null)
        {
            LoadGame();
        }
        Debug.Log($"[SaveManager] Initialized. Save path: {saveFilePath}");
    }

    public void SaveGame()
    {
        if (string.IsNullOrEmpty(saveFilePath))
        {
            saveFilePath = Path.Combine(Application.persistentDataPath, "gamedata.json");
        }

        if (_data == null) _data = new GameData();

        string json = JsonUtility.ToJson(_data, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("[SaveManager] Game Saved.");
    }

    public void LoadGame()
    {
        if (string.IsNullOrEmpty(saveFilePath))
        {
            saveFilePath = Path.Combine(Application.persistentDataPath, "gamedata.json");
        }

        if (File.Exists(saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(saveFilePath);
                _data = JsonUtility.FromJson<GameData>(json);
                Debug.Log("[SaveManager] Game Loaded.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to load save file: {e.Message}");
                _data = new GameData(); // Fallback to default
            }
        }
        else
        {
            Debug.Log("[SaveManager] No save file found. Creating new data.");
            _data = new GameData();
        }
    }

    /// <summary>
    /// Use this to delete save data (useful for debugging or reset features)
    /// </summary>
    public void DeleteSave()
    {
        if (string.IsNullOrEmpty(saveFilePath))
        {
            saveFilePath = Path.Combine(Application.persistentDataPath, "gamedata.json");
        }

        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            _data = new GameData();
            Debug.Log("[SaveManager] Save file deleted.");
        }
    }
    
    /// <summary>
    /// Reset in-memory data to fresh state (used by editor tools)
    /// </summary>
    public void ResetData()
    {
        _data = new GameData();
        Debug.Log("[SaveManager] In-memory data reset to defaults.");
    }
    
    /// <summary>
    /// Add coins to the player's balance and save.
    /// </summary>
    public void AddCoins(int amount)
    {
        if (_data == null) return;
        _data.Coins += amount;
        SaveGame();
        Debug.Log($"[SaveManager] Added {amount} coins. Total: {_data.Coins}");
    }

    /// <summary>
    /// Attempt to spend coins. Returns true if successful and saves.
    /// </summary>
    public bool SpendCoins(int amount)
    {
        if (_data == null) return false;
        
        if (_data.Coins >= amount)
        {
            _data.Coins -= amount;
            SaveGame();
            Debug.Log($"[SaveManager] Spent {amount} coins. New Total: {_data.Coins}");
            return true;
        }
        
        Debug.Log($"[SaveManager] Not enough coins to spend {amount}. Current: {_data.Coins}");
        return false;
    }

    /// <summary>
    /// Get current coin balance.
    /// </summary>
    public int GetCoins()
    {
        return _data != null ? _data.Coins : 0;
    }

    /// <summary>
    /// Unified way to advance to the next level. Always resets step index.
    /// </summary>
    public void AdvanceToNextLevel()
    {
        if (_data == null) return;
        
        _data.CurrentLevel++;
        _data.CurrentScenarioStepIndex = 0;
        _data.ScenarioCompleted = false;
        SaveGame();
        Debug.Log($"[SaveManager] Advanced to level {_data.CurrentLevel}, step reset to 0.");
    }
    
    /// <summary>
    /// Set a specific level. Always resets step index.
    /// </summary>
    public void SetLevel(int levelNumber)
    {
        if (_data == null) return;
        
        _data.CurrentLevel = levelNumber;
        _data.CurrentScenarioStepIndex = 0;
        _data.ScenarioCompleted = false;
        SaveGame();
        Debug.Log($"[SaveManager] Set to level {levelNumber}, step reset to 0.");
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            SaveGame();
        }
    }
}
