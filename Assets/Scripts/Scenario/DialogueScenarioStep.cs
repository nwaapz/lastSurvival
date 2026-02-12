using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueStep", menuName = "Scenario/Dialogue Step")]
public class DialogueScenarioStep : ScenarioStep
{
    [Header("Dialogue Content")]
    public List<NarrationLine> lines;

    [Header("Input Settings")]
    public bool advanceOnClick = true;
    
    private int _currentLineIndex;
    private float _timer;
    private bool _waitingForClickRelease;
    private const float MIN_READ_TIME = 0.5f; // Minimum time before next click is accepted

    public override void OnEnter()
    {
        _currentLineIndex = 0;
        _timer = 0f;
        // If mouse is already pressed when step starts, wait for release first
        // This prevents the click that triggered the previous step from advancing dialogue
        _waitingForClickRelease = Input.GetMouseButton(0);
        
        Debug.Log($"[DialogueScenarioStep] OnEnter - lines: {(lines != null ? lines.Count : 0)}, waitingForRelease: {_waitingForClickRelease}");
        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        if (lines != null && _currentLineIndex < lines.Count)
        {
            var line = lines[_currentLineIndex];
            if (Narration_manager.Instance != null)
            {
                Narration_manager.Instance.ShowNarrationLine(line);
            }
            else
            {
                Debug.Log($"[DialogueStep] (No Manager): {line.characterName}: {line.message}");
            }
        }
    }

    public override bool UpdateStep()
    {
        if (lines == null || lines.Count == 0) return true;

        _timer += Time.deltaTime;

        // Wait for mouse release if it was pressed when step started
        if (_waitingForClickRelease)
        {
            if (!Input.GetMouseButton(0))
            {
                _waitingForClickRelease = false;
            }
            return false;
        }

        if (advanceOnClick && Input.GetMouseButtonDown(0) && _timer > MIN_READ_TIME)
        {
            _currentLineIndex++;
            _timer = 0f;

            if (_currentLineIndex >= lines.Count)
            {
                return true; // All lines shown
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
        // Hide the narration popup when dialogue step ends
        if (Narration_manager.Instance != null)
        {
            Narration_manager.Instance.HideNarration();
        }
    }
}
