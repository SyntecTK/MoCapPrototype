using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField] private float holdUpThreshold = 0.8f;
    [SerializeField] private float holdUpTime = 0.5f; // Time to hold up

    public float CrouchThreshold => crouchThreshold;

    private List<Vector2> rightStickSamples = new List<Vector2>();
    private List<float> sampleTimes = new List<float>();
    private float holdUpStartTime = -1f;
    private bool combo1Triggered = false;
    private bool combo2Triggered = false;

    public Gesture TrackedGesture(Vector2 input)
    {
        // Only track if in bottom half (y <= 0)
        if (input.magnitude >= gestureMinMagnitude && input.y <= 0)
        {
            rightStickSamples.Add(input.normalized);
            sampleTimes.Add(Time.time);
        }

        // Clean up old samples outside time window
        while (sampleTimes.Count > 0 && Time.time - sampleTimes[0] > gestureTimeWindow)
        {
            rightStickSamples.RemoveAt(0);
            sampleTimes.RemoveAt(0);
        }

        // Check for half-circle gesture
        if (rightStickSamples.Count >= gestureMinSamples)
        {
            bool inputDetected = DetectHalfCircle();
            //bool isDanceJumpCombo = (danceState == DanceState.Dancing && isJumping);
            //bool canPirouette = isDanceJumpCombo || (!isDodging && !isGyroDashing && !isDanceJumpPirouetting && !isJumping && !isCrouching);
            if (inputDetected)
            {
                combo1Triggered = true; // Set flag for one-shot
                rightStickSamples.Clear();
                sampleTimes.Clear();
                return Gesture.HalfCircleRight;
            }
        }

        return Gesture.None;
    }
    private bool DetectHalfCircle()
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

    // Combo detection methods
    public bool IsCombo1Detected()
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
