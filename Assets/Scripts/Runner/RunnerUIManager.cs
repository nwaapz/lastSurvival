using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages UI elements for the runner gameplay.
/// Handles score display, game over screen, and control buttons.
/// </summary>
public class RunnerUIManager : MonoBehaviour
{
    [Header("HUD Elements")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI highScoreText;
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private Slider healthSlider;
    
    
    [Header("Game Over Panel")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;
    [SerializeField] private TextMeshProUGUI enemiesDefeatedText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    
    [Header("Result Menu (Win/Lose)")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Image resultImage;
    [SerializeField] private Sprite winImage;
    [SerializeField] private Sprite loseImage;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button retryButton;
    
    [Header("Pause Panel")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseQuitButton;
    
    [Header("Start Panel")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private Button startButton;
    
    [Header("Control Buttons")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    
    [Header("Settings")]
    [SerializeField] private string scoreFormat = "Score: {0}";
    [SerializeField] private string highScoreFormat = "Best: {0}";
    [SerializeField] private string distanceFormat = "{0}m";
    
    private const string HIGH_SCORE_KEY = "RunnerHighScore";
    private int _highScore;

    // Events
    public System.Action OnContinueClicked;
    public System.Action OnRetryClicked;
    
    private void Start()
    {
        LoadHighScore();
        SetupButtons();
        SubscribeToEvents();
        
        // Initial UI state
        ShowStartPanel();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    #region Setup
    
    private void SetupButtons()
    {
        // Start panel
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClicked);
        }
        
        // Game over panel
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(OnRestartButtonClicked);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuButtonClicked);
        }
        
        // Pause panel
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(OnResumeButtonClicked);
        }
        
        if (pauseQuitButton != null)
        {
            pauseQuitButton.onClick.AddListener(OnMainMenuButtonClicked);
        }
        
        // Result panel (Win/Lose)
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        }
        
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetryButtonClicked);
        }
        
        // Control buttons
        if (leftButton != null)
        {
            var inputHandler = FindObjectOfType<RunnerInputHandler>();
            if (inputHandler != null)
            {
                leftButton.onClick.AddListener(inputHandler.OnLeftButtonPressed);
            }
        }
        
        if (rightButton != null)
        {
            var inputHandler = FindObjectOfType<RunnerInputHandler>();
            if (inputHandler != null)
            {
                rightButton.onClick.AddListener(inputHandler.OnRightButtonPressed);
            }
        }
    }
    
    #endregion

    private void SubscribeToEvents()
    {
        if (RunnerGameManager.Instance != null)
        {
            RunnerGameManager.Instance.OnScoreChanged += UpdateScoreDisplay;
            RunnerGameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }

        var player = FindObjectOfType<RunnerPlayerController>();
        if (player != null)
        {
            player.OnHealthChanged += UpdateHealthDisplay;
            
            // Initialize slider max value
            if (healthSlider != null)
            {
                healthSlider.maxValue = player.MaxHealth;
                healthSlider.value = player.CurrentHealth;
            }
        }
    }

    private void UnsubscribeFromEvents()
    {
        // Use HasInstance to avoid error log when singleton is already destroyed
        if (RunnerGameManager.HasInstance)
        {
            RunnerGameManager.Instance.OnScoreChanged -= UpdateScoreDisplay;
            RunnerGameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
        
        var player = FindObjectOfType<RunnerPlayerController>();
        if (player != null)
        {
            player.OnHealthChanged -= UpdateHealthDisplay;
        }
    }

    #region UI Updates
    
    private void UpdateScoreDisplay(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = string.Format(scoreFormat, score);
        }
        
        // Update high score if beaten
        if (score > _highScore)
        {
            _highScore = score;
            UpdateHighScoreDisplay();
        }
    }
    
    private void UpdateHighScoreDisplay()
    {
        if (highScoreText != null)
        {
            highScoreText.text = string.Format(highScoreFormat, _highScore);
        }
    }
    
    private void UpdateDistanceDisplay(float distance)
    {
        if (distanceText != null)
        {
            distanceText.text = string.Format(distanceFormat, Mathf.FloorToInt(distance));
        }
    }

    private void UpdateHealthDisplay(float currentHealth)
    {
        // Calculate percentage for logging
        var player = FindObjectOfType<RunnerPlayerController>();
        if (player != null && player.MaxHealth > 0)
        {
            float percentage = currentHealth / player.MaxHealth;
            Debug.Log($"[RunnerUIManager] Player Health: {percentage * 100:F0}%");
        }

        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
    }
    
    private void HandleGameStateChanged(RunnerGameManager.GameState state)
    {
        switch (state)
        {
            case RunnerGameManager.GameState.NotStarted:
                ShowStartPanel();
                break;
                
            case RunnerGameManager.GameState.Playing:
                HideAllPanels();
                break;
                
            case RunnerGameManager.GameState.Paused:
                ShowPausePanel();
                break;
                
            case RunnerGameManager.GameState.GameOver:
                ShowLosePanel();
                break;
                
            case RunnerGameManager.GameState.Won:
                ShowWinPanel();
                break;
        }
    }
    #endregion

    #region Panel Management

    private void ShowStartPanel()
    {
        HideAllPanels();
        if (startPanel != null)
        {
            startPanel.SetActive(true);
        }
    }
    
    private void ShowGameOverPanel()
    {
        HideAllPanels();
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            
            // Update final stats
            if (RunnerGameManager.Instance != null)
            {
                if (finalScoreText != null)
                {
                    finalScoreText.text = $"Score: {RunnerGameManager.Instance.CurrentScore}";
                }
                
                if (enemiesDefeatedText != null)
                {
                    enemiesDefeatedText.text = $"Enemies Defeated: {RunnerGameManager.Instance.EnemiesDefeated}";
                }
            }
        }
        
        // Save high score
        SaveHighScore();
    }
    
    /// <summary>
    /// Show WIN result panel with win image and continue button
    /// </summary>
    private void ShowWinPanel()
    {
        HideAllPanels();
        
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            
            // Set win image
            if (resultImage != null && winImage != null)
            {
                resultImage.sprite = winImage;
            }
            
            // Show continue, hide retry
            if (continueButton != null) continueButton.gameObject.SetActive(true);
            if (retryButton != null) retryButton.gameObject.SetActive(false);
        }
        
        SaveHighScore();
        
        Debug.Log("[RunnerUIManager] Showing WIN panel");
    }
    
    /// <summary>
    /// Show LOSE result panel with lose image and retry button
    /// </summary>
    private void ShowLosePanel()
    {
        HideAllPanels();
        
        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            
            // Set lose image
            if (resultImage != null && loseImage != null)
            {
                resultImage.sprite = loseImage;
            }
            
            // Show retry, hide continue
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            if (retryButton != null) retryButton.gameObject.SetActive(true);
        }
        
        SaveHighScore();
        
        Debug.Log("[RunnerUIManager] Showing LOSE panel");
    }
    
    private void ShowPausePanel()
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
    }
    
    private void HideAllPanels()
    {
        if (startPanel != null) startPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
    }
    
    #endregion

    #region Button Handlers
    
    private void OnStartButtonClicked()
    {
        if (RunnerGameManager.Instance != null)
        {
            RunnerGameManager.Instance.StartGame();
        }
    }
    
    private void OnRestartButtonClicked()
    {
        if (RunnerGameManager.Instance != null)
        {
            RunnerGameManager.Instance.RestartGame();
        }
    }
    
    private void OnResumeButtonClicked()
    {
        if (RunnerGameManager.Instance != null)
        {
            RunnerGameManager.Instance.ResumeGame();
        }
    }
    
    private void OnMainMenuButtonClicked()
    {
        // Load main menu scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
    
    /// <summary>
    /// Called by pause button
    /// </summary>
    public void OnPauseButtonClicked()
    {
        if (RunnerGameManager.Instance != null)
        {
            RunnerGameManager.Instance.PauseGame();
        }
    }
    
    /// <summary>
    /// Continue button - advance to next level
    /// </summary>
    private void OnContinueButtonClicked()
    {
        OnContinueClicked?.Invoke();
        
        // If scenario is running, let it handle the progression
        if (ScenarioManager.Instance != null && ScenarioManager.Instance.IsRunning)
        {
            Debug.Log("[RunnerUIManager] Continue clicked - deferring to ScenarioManager");
            // Determine if we should block default behavior?
            // Ideally, the scenario step (Wait) completes, and the scenario manager handles the next load.
            // If we reload here, we might break the scenario flow.
            return;
        }

        // Default behavior (non-scenario mode):
        // Advance to next level using SaveManager
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.AdvanceToNextLevel();
        }
        
        // Reload current scene for next level
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        
        Debug.Log("[RunnerUIManager] Continue clicked - advancing to next level (Default)");
    }
    
    /// <summary>
    /// Retry button - replay current level
    /// </summary>
    private void OnRetryButtonClicked()
    {
        OnRetryClicked?.Invoke();
        
        // Reload current scene to retry the level
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        
        Debug.Log("[RunnerUIManager] Retry clicked - replaying level");
    }
    
    #endregion

    #region High Score
    
    private void LoadHighScore()
    {
        _highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        UpdateHighScoreDisplay();
    }
    
    private void SaveHighScore()
    {
        if (RunnerGameManager.Instance != null && 
            RunnerGameManager.Instance.CurrentScore > PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0))
        {
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, RunnerGameManager.Instance.CurrentScore);
            PlayerPrefs.Save();
        }
    }
    
    #endregion
}
