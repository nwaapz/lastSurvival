using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scenario step for showing narration/dialogue during tutorials.
/// Supports single line, multiple lines (conversation), and various advance modes.
/// </summary>
[CreateAssetMenu(fileName = "NarrationStep", menuName = "Scenario/Narration Step")]
public class NarrationStep : ScenarioStep
{
    public enum AdvanceMode
    {
        ClickToAdvance,     // Player clicks to advance to next line
        AutoAdvance,        // Automatically advance after duration
        WaitForTyping       // Wait for typing animation, then auto-advance
    }
    
    [Header("Narration Content")]
    [Tooltip("Single narration line (use this OR lines list, not both)")]
    public NarrationLine singleLine;
    
    [Tooltip("Multiple narration lines for a conversation")]
    public List<NarrationLine> lines = new List<NarrationLine>();
    
    [Header("Advance Settings")]
    public AdvanceMode advanceMode = AdvanceMode.ClickToAdvance;
    
    [Tooltip("Time to wait before auto-advancing (for AutoAdvance mode)")]
    public float autoAdvanceDelay = 3f;
    
    [Tooltip("Minimum time before click is accepted (prevents accidental skips)")]
    public float minReadTime = 0.5f;
    
    [Header("Completion")]
    [Tooltip("Hide narration panel when step completes")]
    public bool hideOnComplete = true;
    
    [Tooltip("Pause game time during narration")]
    public bool pauseGame = false;
    
    // Runtime state
    private int _currentLineIndex;
    private float _timer;
    private float _typingTimer;
    private bool _isComplete;
    private float _previousTimeScale;
    
    // Estimated typing duration based on message length
    private float EstimatedTypingDuration => GetCurrentLine()?.message.Length * 0.03f ?? 1f;

    public override void OnEnter()
    {
        _currentLineIndex = 0;
        _timer = 0f;
        _typingTimer = 0f;
        _isComplete = false;
        
        // Pause game if requested
        if (pauseGame)
        {
            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        
        // Build the lines list if only singleLine is provided
        if (singleLine != null && (lines == null || lines.Count == 0))
        {
            lines = new List<NarrationLine> { singleLine };
        }
        
        if (lines == null || lines.Count == 0)
        {
            Debug.LogWarning("[NarrationStep] No narration lines provided! Step will auto-complete.");
            _isComplete = true;
            return;
        }
        
        ShowCurrentLine();
        Debug.Log($"[NarrationStep] Starting narration with {lines.Count} line(s)");
    }

    private void ShowCurrentLine()
    {
        NarrationLine line = GetCurrentLine();
        if (line == null) return;
        
        if (Narration_manager.Instance != null)
        {
            Narration_manager.Instance.ShowNarrationLine(line);
        }
        else
        {
            Debug.Log($"[NarrationStep] {line.characterName}: {line.message}");
        }
        
        _timer = 0f;
        _typingTimer = 0f;
    }
    
    private NarrationLine GetCurrentLine()
    {
        if (lines != null && _currentLineIndex < lines.Count)
        {
            return lines[_currentLineIndex];
        }
        return null;
    }

    public override bool UpdateStep()
    {
        if (_isComplete) return true;
        if (lines == null || lines.Count == 0) return true;
        
        // Use unscaled time if game is paused
        float deltaTime = pauseGame ? Time.unscaledDeltaTime : Time.deltaTime;
        _timer += deltaTime;
        _typingTimer += deltaTime;
        
        bool shouldAdvance = false;
        
        switch (advanceMode)
        {
            case AdvanceMode.ClickToAdvance:
                // Check for click (use GetMouseButtonDown which works even when timeScale is 0)
                if (Input.GetMouseButtonDown(0) && _timer > minReadTime)
                {
                    shouldAdvance = true;
                }
                break;
                
            case AdvanceMode.AutoAdvance:
                if (_timer >= autoAdvanceDelay)
                {
                    shouldAdvance = true;
                }
                break;
                
            case AdvanceMode.WaitForTyping:
                // Wait for typing to finish, then a small delay
                if (_typingTimer >= EstimatedTypingDuration + 0.5f)
                {
                    shouldAdvance = true;
                }
                break;
        }
        
        if (shouldAdvance)
        {
            _currentLineIndex++;
            
            if (_currentLineIndex >= lines.Count)
            {
                // All lines shown
                _isComplete = true;
                Debug.Log("[NarrationStep] Narration complete");
                return true;
            }
            else
            {
                ShowCurrentLine();
            }
        }
        
        return false;
    }

    public override void OnExit()
    {
        // Restore time scale
        if (pauseGame)
        {
            Time.timeScale = _previousTimeScale;
        }
        
        // Hide narration panel if requested
        if (hideOnComplete && Narration_manager.Instance != null)
        {
            // Find and hide the narration popup
            var narrationPop = Object.FindObjectOfType<Narration_pop>();
            if (narrationPop != null)
            {
                narrationPop.gameObject.SetActive(false);
            }
        }
    }
}
