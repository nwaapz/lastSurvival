using UnityEngine;
using System.Linq;

/// <summary>
/// Tracks the "Leader" of the squad (player with the highest Z position).
/// Updates its transform only when the game starts or the current leader dies.
/// </summary>
public class RunnerLeaderTracker : MonoBehaviour
{
    private RunnerPlayerController _currentLeader;
    private RunnerSquadManager _squadManager;

    private void Start()
    {
        _squadManager = FindObjectOfType<RunnerSquadManager>();
        
        // Wait one frame to ensure squad is initialized
        StartCoroutine(InitializeRoutine());
    }

    private System.Collections.IEnumerator InitializeRoutine()
    {
        yield return null;
        FindAndSnapToLeader();
    }

    private void FindAndSnapToLeader()
    {
        if (_squadManager == null)
        {
            _squadManager = FindObjectOfType<RunnerSquadManager>();
            if (_squadManager == null) return;
        }

        var availablePlayers = _squadManager.ActiveMembers;
        if (availablePlayers == null || availablePlayers.Count == 0) return;

        // Find alive player with highest Z position
        RunnerPlayerController newLeader = null;
        float maxZ = float.MinValue;

        foreach (var player in availablePlayers)
        {
            if (player != null && player.CurrentHealth > 0)
            {
                if (player.transform.position.z > maxZ)
                {
                    maxZ = player.transform.position.z;
                    newLeader = player;
                }
            }
        }

        if (newLeader != null)
        {
            // If we had a previous leader, unsubscribe
            if (_currentLeader != null)
            {
                _currentLeader.OnHealthChanged -= OnLeaderHealthChanged;
            }

            _currentLeader = newLeader;
            
            // Subscribe to new leader's health events
            _currentLeader.OnHealthChanged += OnLeaderHealthChanged;

            // Snap position only (preserve own rotation)
            transform.position = _currentLeader.transform.position;
            // transform.rotation = _currentLeader.transform.rotation; // Rotation disabled per request

            Debug.Log($"[RunnerLeaderTracker] New Leader Selected: {_currentLeader.name} at Z: {maxZ}");
        }
    }

    private void OnLeaderHealthChanged(float currentHealth)
    {
        if (currentHealth <= 0)
        {
            Debug.Log("[RunnerLeaderTracker] Current leader died. Finding new leader...");
            
            // Unsubscribe happens in FindAndSnapToLeader or here
            if (_currentLeader != null)
            {
                _currentLeader.OnHealthChanged -= OnLeaderHealthChanged;
                _currentLeader = null;
            }
            
            // Find new leader
            FindAndSnapToLeader();
        }
    }

    private void OnDestroy()
    {
        if (_currentLeader != null)
        {
            _currentLeader.OnHealthChanged -= OnLeaderHealthChanged;
        }
    }
}
