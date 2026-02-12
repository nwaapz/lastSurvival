using UnityEngine;

public class EconomyManager : SingletonMono<EconomyManager>, IService
{
    [Header("Starting Balance")]
    [SerializeField] private int startingBalance = 1000;

    private int _currentBalance;

    public int CurrentBalance => _currentBalance;
    
    protected override void Awake()
    {
        base.Awake();
        if (Instance == this)
        {
            DontDestroyOnLoad(gameObject);
            
            // Self-register with ServiceLocator
            if (ServiceLocator.HasInstance)
            {
                ServiceLocator.Instance.Register<EconomyManager>(this);
            }
        }
    }
    
    protected override void OnDestroy()
    {
        if (ServiceLocator.HasInstance)
        {
            ServiceLocator.Instance.Unregister<EconomyManager>();
        }
        base.OnDestroy();
    }

    public void Init()
    {
        int currentLevel = 1;
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            currentLevel = SaveManager.Instance.Data.CurrentLevel;
        }

        if (currentLevel == 1)
        {
            // Level 1 is a tutorial/hook - give player lots of money to play with
            // This balance is NOT saved - real economy starts from level 2
            _currentBalance = 10000;
        }
        else
        {
            // Real game economy starts from level 2
            // Load persistent balance if available
            if (SaveManager.Instance != null)
            {
                _currentBalance = SaveManager.Instance.GetCoins();
                // If it's a fresh save (0 coins) and we expect starting balance, 
                // we might want to handle that, but for now we trust the save.
            }
            else
            {
                _currentBalance = startingBalance;
            }
        }
        
        NotifyBalanceChanged();
    }
    
    /// <summary>
    /// Returns true if current level is the tutorial level (level 1).
    /// Tutorial level economy is not saved.
    /// </summary>
    public bool IsTutorialLevel()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            return SaveManager.Instance.Data.CurrentLevel == 1;
        }
        return true;
    }

    /// <summary>
    /// Add money to the player's balance
    /// </summary>
    public void AddMoney(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[EconomyManager] Attempted to add non-positive amount: {amount}");
            return;
        }

        _currentBalance += amount;
        
        // Sync with SaveManager if we are in real economy levels
        if (!IsTutorialLevel() && SaveManager.Instance != null)
        {
            SaveManager.Instance.AddCoins(amount);
        }
        
        Debug.Log($"[EconomyManager] Added ${amount}. New balance: ${_currentBalance}");
        NotifyBalanceChanged();
    }

    /// <summary>
    /// Spend money from the player's balance
    /// Returns true if successful, false if insufficient funds
    /// </summary>
    public bool SpendMoney(int amount)
    {
        if (amount <= 0)
        {
            Debug.LogWarning($"[EconomyManager] Attempted to spend non-positive amount: {amount}");
            return false;
        }

        if (_currentBalance < amount)
        {
            Debug.LogWarning($"[EconomyManager] Insufficient funds. Required: ${amount}, Available: ${_currentBalance}");
            return false;
        }

        _currentBalance -= amount;
        
        // Sync with SaveManager if we are in real economy levels
        if (!IsTutorialLevel() && SaveManager.Instance != null)
        {
            // Sync spending. Note: SaveManager.SpendCoins returns bool usage success, 
            // but we already validated locally, so we just execution it.
            // Ideally they are always in sync.
            SaveManager.Instance.SpendCoins(amount);
        }

        Debug.Log($"[EconomyManager] Spent ${amount}. New balance: ${_currentBalance}");
        NotifyBalanceChanged();
        return true;
    }

    /// <summary>
    /// Check if the player can afford a certain amount
    /// </summary>
    public bool CanAfford(int amount)
    {
        return _currentBalance >= amount;
    }

    /// <summary>
    /// Notify UI manager to update the balance display
    /// </summary>
    private void NotifyBalanceChanged()
    {
        // FightSceneUIManager removed - UI update disabled for now
        // TODO: Add runner-specific UI notification if needed
    }
}
