using System;
using UnityEngine;

/// <summary>
/// Main controller for the runner gameplay mode.
/// Manages game state, scoring, and coordinates between player and enemies.
/// </summary>
public class RunnerGameManager : SingletonMono<RunnerGameManager>
{
    

    [Header("player characters health bar")]
    public Transform Canvas;
    public GameObject healthbar;

    [Header("Configuration")]
    [SerializeField] private RunnerLaneConfig laneConfig;
    
    [Header("References")]
    [SerializeField] private RunnerPlayerController playerController;
    [SerializeField] private RunnerEnemySpawner enemySpawner;
    
    [Header("Game Settings")]
    [SerializeField] private float startingGameSpeed = 2f;
    [SerializeField] private float speedIncreaseRate = 0.05f;
    [SerializeField] private float maxGameSpeed = 8f;
    
    private float _currentGameSpeed;
    
    [Header("Scoring")]
    [SerializeField] private int scorePerEnemy = 10;
    [SerializeField] private int scorePerSecond = 1;
    
    
    // Game State
    public enum GameState { NotStarted, Playing, Paused, GameOver, Won }
    private GameState _currentState = GameState.NotStarted;
    
    // Events
    public event Action<GameState> OnGameStateChanged;
    public event Action<int> OnScoreChanged;
    public event Action<int> OnEnemyDefeated;
    public event Action OnPlayerHit;
    
    // Properties
    public GameState CurrentState => _currentState;
    public RunnerLaneConfig LaneConfig => laneConfig;
    public float CurrentGameSpeed => _currentGameSpeed;
    public int CurrentScore { get; private set; }
    public int EnemiesDefeated { get; private set; }
    public float PlayTime { get; private set; }
    
    /// <summary>
    /// True as soon as StartGame is called (even before delay completes).
    /// Use this for rotation/animations that should start immediately.
    /// </summary>
    public bool HasStarted { get; private set; }
    
    private float _scoreTimer;

    [Header("Debug")]
    [SerializeField] private bool autoStart = true;
    
    [Header("Start Delay")]
    [Tooltip("Delay in seconds after pressing Start before the game actually begins")]
    [SerializeField] private float startDelay = 0f;

    private void Awake()
    {
        base.Awake();
        
        // Set target frame rate
        Application.targetFrameRate = 50;

        ValidateReferences();
    }

    private void Start()
    {
        if (autoStart)
        {
            // Small delay to ensure other components are ready
            Invoke(nameof(StartGame), 0.1f);
        }
    }

   

    private void Update()
    {
        if (_currentState != GameState.Playing) return;
        
        UpdatePlayTime();
        UpdateScore();
        UpdateGameSpeed();
    }

    private void ValidateReferences()
    {
        if (laneConfig == null)
        {
            Debug.LogWarning("[RunnerGameManager] LaneConfig not assigned! Creating default config.");
            laneConfig = ScriptableObject.CreateInstance<RunnerLaneConfig>();
        }
        
        if (playerController == null)
        {
            playerController = FindObjectOfType<RunnerPlayerController>();
        }
        
        if (enemySpawner == null)
        {
            enemySpawner = FindObjectOfType<RunnerEnemySpawner>();
        }
    }

    #region Game Flow
    
    /// <summary>
    /// Start the runner game
    /// </summary>
    public void StartGame()
    {
        if (_currentState == GameState.Playing) return;
        
        // Set immediately so rotation/animations can start right away
        HasStarted = true;
        
        // Trigger camera transition immediately (this can start during the delay)
        var cameraController = FindObjectOfType<RunnerCameraController>();
        if (cameraController != null)
        {
            cameraController.StartTransition();
        }
        
        if (startDelay > 0f)
        {
            StartCoroutine(StartGameAfterDelay());
        }
        else
        {
            StartGameImmediate();
        }
    }
    
    private System.Collections.IEnumerator StartGameAfterDelay()
    {
        Debug.Log($"[RunnerGameManager] Waiting {startDelay}s before starting game...");
        yield return new WaitForSeconds(startDelay);
        StartGameImmediate();
    }
    
    private void StartGameImmediate()
    {
        if (_currentState == GameState.Playing) return;
        
        ResetGameState();
        SetGameState(GameState.Playing);
        
        // NOTE: Enemy spawning is now controlled by RunnerWaveManager
        // Do NOT call enemySpawner.StartSpawning() here
        
        Debug.Log("[RunnerGameManager] Game Started!");
    }
    
    /// <summary>
    /// Pause the game
    /// </summary>
    public void PauseGame()
    {
        if (_currentState != GameState.Playing) return;
        
        SetGameState(GameState.Paused);
        Time.timeScale = 0f;
        
        Debug.Log("[RunnerGameManager] Game Paused");
    }
    
    /// <summary>
    /// Resume the game
    /// </summary>
    public void ResumeGame()
    {
        if (_currentState != GameState.Paused) return;
        
        Time.timeScale = 1f;
        SetGameState(GameState.Playing);
        
        Debug.Log("[RunnerGameManager] Game Resumed");
    }
    
    /// <summary>
    /// End the game (player died or quit)
    /// </summary>
    public void EndGame()
    {
        if (_currentState == GameState.GameOver) return;
        
        SetGameState(GameState.GameOver);
        Time.timeScale = 1f;
        
        if (enemySpawner != null)
        {
            enemySpawner.StopSpawning();
        }
        
        Debug.Log($"[RunnerGameManager] Game Over! Score: {CurrentScore}, Enemies Defeated: {EnemiesDefeated}");
    }
    
    /// <summary>
    /// Trigger win condition (all waves complete)
    /// </summary>
    public void TriggerWin()
    {
        if (_currentState == GameState.GameOver || _currentState == GameState.Won) return;
        
        SetGameState(GameState.Won);
        Time.timeScale = 1f;
        
        if (enemySpawner != null)
        {
            enemySpawner.StopSpawning();
        }
        
        Debug.Log($"[RunnerGameManager] VICTORY! Score: {CurrentScore}, Enemies Defeated: {EnemiesDefeated}");
        
        // Notify UI to show win screen
        OnGameStateChanged?.Invoke(GameState.Won);
    }
    
    /// <summary>
    /// Restart the game
    /// </summary>
    public void RestartGame()
    {
        ResetGameState();
        StartGame();
    }
    
    private void ResetGameState()
    {
        CurrentScore = 0;
        EnemiesDefeated = 0;
        PlayTime = 0f;
        _scoreTimer = 0f;
        _currentGameSpeed = startingGameSpeed;
        Time.timeScale = 1f;
        
        OnScoreChanged?.Invoke(CurrentScore);
    }
    
    private void SetGameState(GameState newState)
    {
        if (_currentState == newState) return;
        
        _currentState = newState;
        OnGameStateChanged?.Invoke(_currentState);
    }
    
    #endregion

    #region Game Updates
    
    private void UpdatePlayTime()
    {
        PlayTime += Time.deltaTime;
    }
    
    private void UpdateScore()
    {
        _scoreTimer += Time.deltaTime;
        if (_scoreTimer >= 1f)
        {
            _scoreTimer -= 1f;
            AddScore(scorePerSecond);
        }
    }
    
    private void UpdateGameSpeed()
    {
        if (_currentGameSpeed < maxGameSpeed)
        {
            _currentGameSpeed += speedIncreaseRate * Time.deltaTime;
            _currentGameSpeed = Mathf.Min(_currentGameSpeed, maxGameSpeed);
        }
    }
    
    #endregion

    #region Public Methods
    
    /// <summary>
    /// Add score points
    /// </summary>
    public void AddScore(int points)
    {
        CurrentScore += points;
        OnScoreChanged?.Invoke(CurrentScore);
    }
    
    /// <summary>
    /// Called when an enemy is defeated
    /// </summary>
    public void RegisterEnemyDefeated()
    {
        EnemiesDefeated++;
        AddScore(scorePerEnemy);
        OnEnemyDefeated?.Invoke(EnemiesDefeated);
    }
    
    /// <summary>
    /// Called when player is hit by an enemy
    /// </summary>
    public void RegisterPlayerHit()
    {
        OnPlayerHit?.Invoke();
        
        // TODO: Add health system here
        // For now, just log the hit - don't end game immediately
        Debug.Log("[RunnerGameManager] Player was hit by zombie!");
    }
    
    /// <summary>
    /// Get the X position for a specific lane
    /// </summary>
    public float GetLanePosition(int laneIndex)
    {
        return laneConfig != null ? laneConfig.GetLanePosition(laneIndex) : 0f;
    }
    
    /// <summary>
    /// Make all active zombies go idle (stop movement and attacking).
    /// Called when the player dies.
    /// </summary>
    public void MakeAllZombiesIdle()
    {
        if (enemySpawner != null)
        {
            enemySpawner.MakeAllZombiesIdle();
        }
    }
    
    #endregion
}
