using System.Collections.Generic;
using UnityEngine;

public class Narration_Steps : MonoBehaviour
{
    [SerializeField] List<NarrationLine> narrationLines;
    int _currentIndex = 0;

    public void PlayCurrentStep()
    {
        if (narrationLines == null || narrationLines.Count == 0)
        {
            Debug.LogWarning("Narration_Steps: No narration lines assigned.");
            return;
        }

        if (_currentIndex < 0 || _currentIndex >= narrationLines.Count)
        {
            Debug.LogWarning($"Narration_Steps: Index {_currentIndex} is out of range.");
            return;
        }

        var line = narrationLines[_currentIndex];

        // Step -> Narration_manager -> CharacterPoses -> Narration_pop
        Narration_manager.Instance.ShowNarrationLine(line);
    }

    public void PlayNextStep()
    {
        if (narrationLines == null || narrationLines.Count == 0)
            return;

        _currentIndex++;
        if (_currentIndex >= narrationLines.Count)
        {
            _currentIndex = narrationLines.Count - 1;
            return;
        }

        PlayCurrentStep();
    }

    public void ResetSteps()
    {
        _currentIndex = 0;
    }

    [ContextMenu("Test - Play First Narration Line")]
    private void TestPlayFirstLine()
    {
        if (narrationLines == null || narrationLines.Count == 0)
        {
            Debug.LogWarning("Narration_Steps: No narration lines assigned for testing.");
            return;
        }

        _currentIndex = 0;
        PlayCurrentStep();
    }
}
