using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MotionController : Singleton<MotionController>
{
    public enum Gesture
    {
        None,
        HalfCircleRight,
        HalfCircleLeft
    }

    [SerializeField] private float gestureTimeWindow = 0.5f;
    [SerializeField] private float gestureMinMagnitude = 0.3f;
    [SerializeField] private int gestureMinSamples = 5;
    [SerializeField] private float crouchThreshold = -0.8f;

    public float CrouchThreshold => crouchThreshold;

    private List<Vector2> rightStickSamples = new List<Vector2>();
    private List<float> sampleTimes = new List<float>();
    private bool combo1Triggered = false;
    private bool combo2Triggered = false;

    private void Update()
    {
        if (Gamepad.current == null) return;

        Vector2 rightStick = Gamepad.current?.rightStick.ReadValue() ?? Vector2.zero;
        float currentTime = Time.time;

        if (rightStick.magnitude > gestureMinMagnitude)
        {
            rightStickSamples.Add(rightStick);
            sampleTimes.Add(currentTime);
        }
        else if (rightStickSamples.Count > 0)
        {
            // Stick returned to neutral - evaluate the gesture now
            if (rightStickSamples.Count >= gestureMinSamples)
            {
                DetectRightToBottomLeftSweep();
            }
            // Clear samples when stick is released
            rightStickSamples.Clear();
            sampleTimes.Clear();
        }

        while (sampleTimes.Count > 0 && currentTime - sampleTimes[0] > gestureTimeWindow)
        {
            sampleTimes.RemoveAt(0);
            rightStickSamples.RemoveAt(0);
        }
    }


    public bool DetectHalfCircle()
    {

        float startX = rightStickSamples[0].x;
        float endX = rightStickSamples[rightStickSamples.Count - 1].x;

        if (!(startX > 0.3f && endX < -0.3f))
            return false;


        bool wentThroughBottom = false;
        foreach (var sample in rightStickSamples)
        {
            if (sample.y < -0.5f)
            {
                wentThroughBottom = true;
                break;
            }
        }

        if (!wentThroughBottom)
            return false;

        return true;
    }

    /// <summary>
    /// Detects a ~100° sweep starting from the right side, going through the bottom, ending at bottom-left.
    /// Right = 0°, Bottom = -90°, Bottom-Left = ~-100° to -135°
    /// </summary>
    public bool DetectRightToBottomLeftSweep()
    {
        if (rightStickSamples.Count < gestureMinSamples)
            return false;

        // Find the rightmost point (start of gesture)
        int rightMostIndex = -1;
        float rightMostX = -1f;
        for (int i = 0; i < rightStickSamples.Count; i++)
        {
            if (rightStickSamples[i].x > rightMostX && Mathf.Abs(rightStickSamples[i].y) < 0.6f)
            {
                rightMostX = rightStickSamples[i].x;
                rightMostIndex = i;
            }
        }

        // Must have started on the right side
        if (rightMostX < 0.4f || rightMostIndex < 0)
            return false;

        // Find if we passed through bottom AFTER the rightmost point
        int bottomIndex = -1;
        for (int i = rightMostIndex; i < rightStickSamples.Count; i++)
        {
            if (rightStickSamples[i].y < -0.5f)
            {
                bottomIndex = i;
                break;
            }
        }

        if (bottomIndex < 0)
            return false;

        // Find if we ended in bottom-left AFTER passing through bottom
        bool endedBottomLeft = false;
        for (int i = bottomIndex; i < rightStickSamples.Count; i++)
        {
            Vector2 sample = rightStickSamples[i];
            // Bottom-left zone: x < 0, y < 0
            if (sample.x < -0.2f && sample.y < -0.2f)
            {
                endedBottomLeft = true;
                break;
            }
        }

        if (!endedBottomLeft)
            return false;

        // Verify the motion was in the correct direction (clockwise)
        // Check that samples generally progress: right -> bottom -> bottom-left
        bool validMotion = rightMostIndex < bottomIndex;

        if (validMotion)
        {
            Debug.Log("Right Sweep Motion Detected!");
            combo1Triggered = true;
            rightStickSamples.Clear();
            sampleTimes.Clear();
            return true;
        }

        return false;
    }
    // Combo detection methods
    public bool IsMotionInputRightSweepDetected()
    {
        if (combo1Triggered)
        {
            combo1Triggered = false;
            return true;
        }
        return false;
    }

    public bool IsCombo2Detected()
    {
        if (combo2Triggered)
        {
            combo2Triggered = false;
            return true;
        }
        return false;
    }

    public bool IsCombo3Detected() { return false; } // Not used here
}
