using UnityEngine;
using System.Collections.Generic;
using System;

public class DanceComboHandler : MonoBehaviour
{
    //================================ Events =================================//
    public event Action OnComboFailed;
    //================================ Fields =================================//

    [SerializeField] private float inputWindowDuration = 0.4f; // Time window at end of animation to accept input
    [SerializeField] private float failTimeout = 1.2f; // Fallback if something goes wrong

    private Transform playerTransform;
    private Vector3 playerStartPos;
    private float moveStartTime;
    private float currentMoveDuration;
    private bool isActive = false;
    private bool inputWindowOpen = false;
    private bool waitingForNextMove = false;

    private GyroController gyroController;

    // List of available dance moves with their animation names
    [System.Serializable]
    public class DanceMove
    {
        public string animationName;
        public float duration; // Set manually or fetch from animator
    }

    [SerializeField] private List<DanceMove> availableMoves = new List<DanceMove>();

    private void Awake()
    {
        playerTransform = PlayerController.Instance.transform;
        gyroController = GetComponent<GyroController>();
    }

    public void StartCombo()
    {
        playerStartPos = playerTransform.position;
        PartnerController.Instance.Deactivate();
        isActive = true;
        inputWindowOpen = false;
        waitingForNextMove = false;
    }

    /// <summary>
    /// Call this when a new dance move is triggered (from PlayerController)
    /// </summary>
    public void OnMoveStarted(string animationName, float duration)
    {
        moveStartTime = Time.time;
        currentMoveDuration = duration;
        inputWindowOpen = false;
        waitingForNextMove = false;

        if (gyroController != null)
        {
            gyroController.ClearPendingTriggers();
        }
    }

    public void UpdateCombo()
    {
        if (!isActive) return;

        float elapsed = Time.time - moveStartTime;

        if (elapsed >= currentMoveDuration - inputWindowDuration && elapsed < currentMoveDuration)
        {
            if (!inputWindowOpen)
            {
                inputWindowOpen = true;
                waitingForNextMove = true;
            }
        }

        if (elapsed > currentMoveDuration + failTimeout)
        {
            FailCombo();
        }
    }

    /// <summary>
    /// Checks if player is inputting a valid next move
    /// </summary>
    private bool TryGetNextMove(out string animationName, out float duration)
    {
        animationName = "";
        duration = 0f;

        if (!inputWindowOpen) return false;

        if (gyroController == null) return false;

        // Check for gyro up input
        if (gyroController.IsGyroUpDetected())
        {
            animationName = "PN_05";
            duration = GetMoveDuration(animationName);
            return true;
        }

        // Check for gyro side input
        if (gyroController.IsGyroSideDetected())
        {
            animationName = "PN_06";
            duration = GetMoveDuration(animationName);
            return true;
        }

        // Add more input checks here for other moves
        // e.g., button presses, stick directions, etc.

        return false;
    }

    /// <summary>
    /// Gets the duration for a specific move from the list or returns a default
    /// </summary>
    private float GetMoveDuration(string animationName)
    {
        foreach (var move in availableMoves)
        {
            if (move.animationName == animationName)
                return move.duration;
        }

        // Default duration if not found in list
        return 1.5f;
    }

    private void FailCombo()
    {
        Debug.Log("Combo Failed - Resetting position");
        playerTransform.position = playerStartPos;
        PartnerController.Instance.Activate();
        isActive = false;
        inputWindowOpen = false;
        waitingForNextMove = false;

        OnComboFailed?.Invoke();
    }

    public void EndComboSuccess()
    {
        Debug.Log("Combo completed successfully!");
        PartnerController.Instance.Activate();
        isActive = false;
        inputWindowOpen = false;
        waitingForNextMove = false;
    }

    public bool IsComboActive() => isActive;
    public bool IsInputWindowOpen() => inputWindowOpen;
}
