using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float slowWalkSpeed = 2f;
    [SerializeField] private float fastWalkSpeed = 5f;
    [SerializeField] private float slowWalkThreshold = 0.5f;

    [Header("Animation Settings")]
    [SerializeField] private float animationCrossfadeTime = 0.2f;

    [Header("Circle Detection Settings")]
    [SerializeField] private float circleDetectionRadius = 0.5f; // Minimum stick magnitude to count
    [SerializeField] private float circleCompletionTime = 1.5f; // Max time to complete circle
    [SerializeField] private float angleTolerance = 30f; // Degrees of tolerance

    private Animator animator;
    private float horizontalInput;
    private Vector3 velocity;
    private string currentAnimation;

    // Circle detection
    private Vector2 rightStickInput;
    private float lastAngle;
    private float totalAngleRotated;
    private float circleStartTime;
    private bool isTrackingCircle;
    private bool isPerformingRoll;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if(!isPerformingRoll) velocity = new Vector3(horizontalInput, 0, 0);
        transform.position += velocity * Time.deltaTime;

        // Circle detection for right stick
        DetectCircleMotion();
    }

    public void OnMove(InputValue value)
    {
        // Don't allow movement input during roll
        if (isPerformingRoll) return;
        
        Vector2 inputVector = value.Get<Vector2>();
        float inputMagnitude = Mathf.Abs(inputVector.x);

        if (inputMagnitude > 0.1f)
        {
            // Determine speed based on input magnitude
            float moveSpeed = inputMagnitude < slowWalkThreshold ? slowWalkSpeed : fastWalkSpeed;
            horizontalInput = Mathf.Sign(inputVector.x) * moveSpeed;

            // Flip character to face movement direction
            if (inputVector.x > 0)
            {
                transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, Mathf.Abs(transform.localScale.z));
            }
            else if (inputVector.x < 0)
            {
                transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, -Mathf.Abs(transform.localScale.z));
            }

            // Play appropriate walk animation only if not already playing
            string targetAnimation = inputMagnitude < slowWalkThreshold ? "Walk_Slow" : "Walk_Fast";
            if (currentAnimation != targetAnimation)
            {
                animator.CrossFade(targetAnimation, animationCrossfadeTime);
                currentAnimation = targetAnimation;
            }
        }
        else
        {
            horizontalInput = 0;
            
            if (currentAnimation != "Idle")
            {
                animator.CrossFade("Idle", animationCrossfadeTime);
                currentAnimation = "Idle";
            }
        }
    }

    public void OnLook(InputValue value)
    {
        rightStickInput = value.Get<Vector2>();
    }

    private void DetectCircleMotion()
    {
        float magnitude = rightStickInput.magnitude;

        // Check if stick is pushed far enough
        if (magnitude > circleDetectionRadius)
        {
            // Calculate current angle
            float currentAngle = Mathf.Atan2(rightStickInput.y, rightStickInput.x) * Mathf.Rad2Deg;

            if (!isTrackingCircle)
            {
                // Start tracking
                isTrackingCircle = true;
                lastAngle = currentAngle;
                totalAngleRotated = 0f;
                circleStartTime = Time.time;
            }
            else
            {
                // Calculate angle difference
                float angleDelta = Mathf.DeltaAngle(lastAngle, currentAngle);
                totalAngleRotated += angleDelta;
                lastAngle = currentAngle;

                // Check if circle completed (360 degrees in either direction)
                if (Mathf.Abs(totalAngleRotated) >= 200f - angleTolerance)
                {
                    // Check if completed within time limit
                    if (Time.time - circleStartTime <= circleCompletionTime)
                    {
                        OnCircleCompleted(totalAngleRotated > 0);
                    }
                    ResetCircleTracking();
                }
                // Reset if taking too long
                else if (Time.time - circleStartTime > circleCompletionTime)
                {
                    ResetCircleTracking();
                }
            }
        }
        else
        {
            // Reset when stick returns to center
            if (isTrackingCircle)
            {
                ResetCircleTracking();
            }
        }
    }

    private void OnCircleCompleted(bool clockwise)
    {
        Debug.Log($"Circle completed! Direction: {(clockwise ? "Clockwise" : "Counter-clockwise")}");
        
        if (!isPerformingRoll)
        {
            StartCoroutine(PerformRoll());
        }
    }

    private System.Collections.IEnumerator PerformRoll()
    {
        isPerformingRoll = true;
        currentAnimation = "Roll";
        animator.CrossFade("Roll", animationCrossfadeTime);
        
        // Wait for the Roll animation to complete
        // Get the animation length from the animator
        yield return new WaitForSeconds(animationCrossfadeTime);
        
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float rollLength = stateInfo.length;
        
        yield return new WaitForSeconds(rollLength);
        velocity = Vector3.zero; // Stop movement during roll
        // Return to Idle after roll completes
        currentAnimation = "Idle";
        animator.CrossFade("Idle", animationCrossfadeTime);
        isPerformingRoll = false;
    }

    private void ResetCircleTracking()
    {
        isTrackingCircle = false;
        totalAngleRotated = 0f;
    }
}
