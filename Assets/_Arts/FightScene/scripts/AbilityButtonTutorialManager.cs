using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

/// <summary>
/// Self-contained ability button tutorial for Level 1.
/// Automatically shows tutorials for Range, Damage, and FireRate buttons if not yet shown.
/// Freezes game, flashes button, waits for click, then moves to next.
/// </summary>
public class AbilityButtonTutorialManager : MonoBehaviour
{
    [Header("Ability Buttons (Tutorial targets)")]
    [SerializeField] private Button rangeButton;
    [SerializeField] private Button damageButton;
    [SerializeField] private Button fireRateButton;

    [Header("All Buttons to Block")]
    [Tooltip("All 6 buttons that should be blocked until tutorials complete")]
    [SerializeField] private Button[] allButtonsToBlock;

    [Header("Flash Settings")]
    [SerializeField] private Color flashColor = new Color(1f, 1f, 0f, 1f);
    [SerializeField] private float flashSpeed = 0.5f;

    [Header("Tutorial Timing (seconds from scene start)")]
    [SerializeField] private float rangeTutorialTime = 3f;
    [SerializeField] private float damageTutorialTime = 6f;
    [SerializeField] private float fireRateTutorialTime = 9f;

    [Header("Debug")]
    [SerializeField] private bool resetTutorialsOnStart = false;

    // PlayerPrefs keys for tracking tutorial completion
    private const string PREF_RANGE_TUTORIAL = "Tutorial_Range_Done";
    private const string PREF_DAMAGE_TUTORIAL = "Tutorial_Damage_Done";
    private const string PREF_FIRERATE_TUTORIAL = "Tutorial_FireRate_Done";

    private Button _currentButton;
    private Image _currentButtonImage;
    private Color _originalColor;
    private Tweener _flashTween;
    private Tweener _scaleTween;
    private Vector3 _originalScale;
    private bool _waitingForClick;
    private bool _tutorialInProgress;

    private void Awake()
    {
        Debug.Log("[AbilityButtonTutorial] Awake() called");
    }

    private void Start()
    {
        // Debug: reset tutorials if enabled
        if (resetTutorialsOnStart)
        {
            ResetAllTutorials();
        }

        // Only run tutorials in Level 1
        int currentLevel = 1;
        if (SaveManager.Instance != null && SaveManager.Instance.Data != null)
        {
            currentLevel = SaveManager.Instance.Data.CurrentLevel;
        }
        
        Debug.Log($"[AbilityButtonTutorial] Start() - Level: {currentLevel}, AllComplete: {AreAllTutorialsComplete()}");

        if (currentLevel == 1 && !AreAllTutorialsComplete())
        {
            // Block all buttons until tutorials are done
            SetAllButtonsInteractable(false);

            // Start each tutorial at its specific time
            if (!PlayerPrefs.HasKey(PREF_RANGE_TUTORIAL))
            {
                StartCoroutine(ScheduleTutorial(rangeButton, PREF_RANGE_TUTORIAL, rangeTutorialTime));
            }

            if (!PlayerPrefs.HasKey(PREF_DAMAGE_TUTORIAL))
            {
                StartCoroutine(ScheduleTutorial(damageButton, PREF_DAMAGE_TUTORIAL, damageTutorialTime));
            }

            if (!PlayerPrefs.HasKey(PREF_FIRERATE_TUTORIAL))
            {
                StartCoroutine(ScheduleTutorial(fireRateButton, PREF_FIRERATE_TUTORIAL, fireRateTutorialTime));
            }
        }
        else
        {
            // Not Level 1 or  tutorials  already complete - ensure buttons are enabled
            SetAllButtonsInteractable(true);
        }
    }

    private bool AreAllTutorialsComplete()
    {
        return PlayerPrefs.HasKey(PREF_RANGE_TUTORIAL) &&
               PlayerPrefs.HasKey(PREF_DAMAGE_TUTORIAL) &&
               PlayerPrefs.HasKey(PREF_FIRERATE_TUTORIAL);
    }

    private void SetAllButtonsInteractable(bool interactable)
    {
        if (allButtonsToBlock == null) return;

        foreach (var button in allButtonsToBlock)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
        Debug.Log($"[AbilityButtonTutorial] All buttons interactable: {interactable}");
    }

    private IEnumerator ScheduleTutorial(Button button, string prefKey, float triggerTime)
    {
        // Wait until the specified time
        yield return new WaitForSeconds(triggerTime);

        // Wait if another tutorial is in progress
        while (_tutorialInProgress)
        {
            yield return null;
        }

        // Check again in case it was completed while waiting
        if (!PlayerPrefs.HasKey(prefKey))
        {
            yield return RunSingleTutorial(button, prefKey);
        }
    }

    private IEnumerator RunSingleTutorial(Button button, string prefKey)
    {
        if (button == null)
        {
            Debug.LogWarning($"[AbilityButtonTutorial]  Button not assigned for {prefKey}");
            yield break;
        }

        _tutorialInProgress = true;
        _currentButton = button;
        _waitingForClick = false;

        // Get button image - try targetGraphic first (what Button uses)
        _currentButtonImage = button.targetGraphic as Image;
        if (_currentButtonImage == null)
        {
            _currentButtonImage = button.GetComponent<Image>();
        }
        if (_currentButtonImage == null)
        {
            _currentButtonImage = button.GetComponentInChildren<Image>();
        }

        if (_currentButtonImage != null)
        {
            _originalColor = _currentButtonImage.color;
            Debug.Log($"[AbilityButtonTutorial] Found image: {_currentButtonImage.name}, color: {_originalColor}");
        }
        else
        {
            Debug.LogWarning($"[AbilityButtonTutorial] No Image found on button {button.name}!");
        }

        // Freeze game
        Time.timeScale = 0f;
        Debug.Log($"[AbilityButtonTutorial] Starting tutorial for {button.name}. Game frozen.");

        // Enable only this button for the tutorial
        button.interactable = true;

        // Start flashing
        StartFlashing();

        // Listen for click
        button.onClick.AddListener(OnButtonClicked);

        // Wait for click (using unscaled time)
        while (!_waitingForClick)
        {
            yield return null;
        }

        // Stop flashing and restore
        StopFlashing();
        button.onClick.RemoveListener(OnButtonClicked);

        // Mark as complete
        PlayerPrefs.SetInt(prefKey, 1);
        PlayerPrefs.Save();

        // Unfreeze game
        Time.timeScale = 1f;
        _tutorialInProgress = false;
        Debug.Log($"[AbilityButtonTutorial] Tutorial complete for {button.name}. Game resumed.");

        // Check if all tutorials are now complete - enable all buttons
        if (AreAllTutorialsComplete())
        {
            SetAllButtonsInteractable(true);
            Debug.Log("[AbilityButtonTutorial] All tutorials complete! All buttons enabled.");
        }
        else
        {
            // Disable this button again until all tutorials are done
            button.interactable = false;
        }
    }

    private void OnButtonClicked()
    {
        _waitingForClick = true;
    }

    private void StartFlashing()
    {
        if (_currentButton == null)
        {
            Debug.LogError("[AbilityButtonTutorial] _currentButton is null in StartFlashing!");
            return;
        }

        // Ensure DOTween is initialized
        DOTween.Init();

        // Store original scale
        _originalScale = _currentButton.transform.localScale;
        Debug.Log($"[AbilityButtonTutorial] Original scale: {_originalScale}, flashSpeed: {flashSpeed}");

        // Scale pulse effect (more visible)
        _scaleTween = _currentButton.transform
            .DOScale(_originalScale * 1.2f, flashSpeed)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(UpdateType.Normal, true); // isIndependentUpdate = true

        if (_scaleTween == null)
        {
            Debug.LogError("[AbilityButtonTutorial] Failed to create scale tween!");
        }
        else
        {
            Debug.Log($"[AbilityButtonTutorial] Scale tween created successfully");
        }

        // Color flash effect
        if (_currentButtonImage != null)
        {
            _flashTween = _currentButtonImage
                .DOColor(flashColor, flashSpeed)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(UpdateType.Normal, true);
            
            Debug.Log($"[AbilityButtonTutorial] Color tween created for {_currentButtonImage.name}");
        }

        Debug.Log($"[AbilityButtonTutorial] Started flashing for {_currentButton.name}");
    }

    private void StopFlashing()
    {
        // Stop color flash
        if (_flashTween != null && _flashTween.IsActive())
        {
            _flashTween.Kill();
            _flashTween = null;
        }

        // Stop scale pulse
        if (_scaleTween != null && _scaleTween.IsActive())
        {
            _scaleTween.Kill();
            _scaleTween = null;
        }

        // Restore original color
        if (_currentButtonImage != null)
        {
            _currentButtonImage.color = _originalColor;
        }

        // Restore original scale
        if (_currentButton != null)
        {
            _currentButton.transform.localScale = _originalScale;
        }
    }

    private void OnDestroy()
    {
        StopFlashing();
        Time.timeScale = 1f; // Safety: ensure game isn't stuck frozen
    }

    /// <summary>
    /// Call this to reset all tutorials (for testing).
    /// </summary>
    [ContextMenu("Reset All Tutorials")]
    public void ResetAllTutorials()
    {
        PlayerPrefs.DeleteKey(PREF_RANGE_TUTORIAL);
        PlayerPrefs.DeleteKey(PREF_DAMAGE_TUTORIAL);
        PlayerPrefs.DeleteKey(PREF_FIRERATE_TUTORIAL);
        PlayerPrefs.Save();
        Debug.Log("[AbilityButtonTutorial] All tutorials reset.");
    }
}
