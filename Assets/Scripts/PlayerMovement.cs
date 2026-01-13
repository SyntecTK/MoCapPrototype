using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float dashSpeedBonus = 3f;
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float lookJumpThreshold = 0.5f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundCheckDistance = 0.15f;
    [SerializeField] private float gyroDashDistance = 1.5f; // ~5 feet
    [SerializeField] private float gyroDashDuration = 0.25f;
    [SerializeField] private float dodgeDistance = 2f;
    [SerializeField] private float dodgeDuration = 0.3f;
    [SerializeField] private float gestureTimeWindow = 0.5f;
    [SerializeField] private float gestureMinMagnitude = 0.3f;
    [SerializeField] private int gestureMinSamples = 5;
    [SerializeField] private float crouchThreshold = -0.8f;

    private PlayerInput playerInput;
    private Rigidbody2D rb;
    private float horizontalInput;
    private float defaultScaleX;
    private float moveMagnitude;
    private float verticalTilt;
    private string currentAnimation;
    private bool lookHeld;
    private bool isJumping;
    private bool hasLeftGround;
    private bool isGyroDashing;
    private bool isDodging;
    private bool isCrouching;
    
    // Gesture detection
    private List<Vector2> rightStickSamples = new List<Vector2>();
    private List<float> sampleTimes = new List<float>();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultScaleX = Mathf.Abs(transform.localScale.x);
        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();
    }

    public void OnMove(InputValue value)
    {
        var move = value.Get<Vector2>();
        moveMagnitude = Mathf.Abs(move.x); // horizontal strength only
        verticalTilt = Mathf.Abs(move.y);
        horizontalInput = Mathf.Clamp(move.x, -1f, 1f);
    }

    public void OnLook(InputValue value)
    {
        if (playerInput != null && playerInput.currentControlScheme != "Gamepad")
            return;

        Vector2 lookInput = value.Get<Vector2>();
        float vertical = lookInput.y;
        
        // Check for crouch
        if (vertical <= crouchThreshold && lookInput.magnitude >= 0.7f)
        {
            isCrouching = true;
        }
        else
        {
            isCrouching = false;
        }
        
        bool wantsJump = vertical >= lookJumpThreshold;

        if (wantsJump && !lookHeld && !isJumping && !isDodging)
        {
            TriggerJump();
        }

        lookHeld = wantsJump;
        
        // Track right stick for gesture detection (only if not crouching)
        if (!isCrouching)
        {
            TrackGesture(lookInput);
        }
    }

    private void TrackGesture(Vector2 input)
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
            int direction = DetectHalfCircle();
            if (direction != 0 && !isDodging && !isGyroDashing)
            {
                TriggerDodge(direction);
                rightStickSamples.Clear();
                sampleTimes.Clear();
            }
        }
    }
    
    private int DetectHalfCircle()
    {
        // Check for left-to-right half circle (returns 1)
        // or right-to-left half circle (returns -1)
        
        float startX = rightStickSamples[0].x;
        float endX = rightStickSamples[rightStickSamples.Count - 1].x;
        
        // Need significant horizontal movement
        if (Mathf.Abs(endX - startX) < 0.8f)
            return 0;
        
        // Check if we went through the bottom
        bool wentThroughBottom = false;
        foreach (var sample in rightStickSamples)
        {
            if (sample.y < -0.5f) // Lower half
            {
                wentThroughBottom = true;
                break;
            }
        }
        
        if (!wentThroughBottom)
            return 0;
        
        // Determine direction
        if (startX < -0.3f && endX > 0.3f)
            return 1; // Left to right
        else if (startX > 0.3f && endX < -0.3f)
            return -1; // Right to left
        
        return 0;
    }
    
    public void TriggerJump()
    {
        if (isJumping) return;
        isJumping = true;
        hasLeftGround = false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        PlayerAnimator.Instance?.PlayAnimation("Jump");
    }

    public void TriggerGyroDash()
    {
        if (isGyroDashing) return;
        StartCoroutine(GyroDashRoutine());
    }

    private System.Collections.IEnumerator GyroDashRoutine()
    {
        isGyroDashing = true;
        float facing = Mathf.Sign(transform.localScale.x);
        Vector2 start = rb.position;
        Vector2 target = start + new Vector2(facing * gyroDashDistance, 0f);

        if (currentAnimation != "Pirouette")
        {
            PlayerAnimator.Instance?.PlayAnimation("Pirouette");
            currentAnimation = "Pirouette";
        }

        float elapsed = 0f;
        while (elapsed < gyroDashDuration)
        {
            float t = elapsed / gyroDashDuration;
            rb.MovePosition(Vector2.Lerp(start, target, t));
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        rb.MovePosition(target);
        
        // Wait for Pirouette animation to complete
        if (PlayerAnimator.Instance != null)
        {
            float animationLength = PlayerAnimator.Instance.GetAnimationLength("Pirouette");
            float remainingTime = Mathf.Max(0, animationLength - gyroDashDuration);
            yield return new WaitForSeconds(remainingTime);
        }
        
        isGyroDashing = false;
    }

    private void TriggerDodge(int direction)
    {
        if (isDodging) return;
        StartCoroutine(DodgeRoutine(direction));
    }
    
    private IEnumerator DodgeRoutine(int direction)
    {
        isDodging = true;
        Vector2 start = rb.position;
        Vector2 target = start + new Vector2(direction * dodgeDistance, 0f);

        if (currentAnimation != "Dodge")
        {
            PlayerAnimator.Instance?.PlayAnimation("Dodge");
            currentAnimation = "Dodge";
        }

        float elapsed = 0f;
        while (elapsed < dodgeDuration)
        {
            float t = elapsed / dodgeDuration;
            rb.MovePosition(Vector2.Lerp(start, target, t));
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        rb.MovePosition(target);
        isDodging = false;
    }

    private void FixedUpdate()
    {
        bool isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundMask);

        if (!hasLeftGround && !isGrounded) hasLeftGround = true;

        if (isGrounded && hasLeftGround) isJumping = false;

        bool isDashing = !Mathf.Approximately(horizontalInput, 0f)
                         && moveMagnitude >= 0.8f
                         && verticalTilt <= 0.5f
                         && !isCrouching;
        float currentSpeed = moveSpeed + (isDashing ? dashSpeedBonus : 0f);
        
        if (isCrouching) currentSpeed *= 0.5f;
            
        rb.linearVelocity = new Vector2(horizontalInput * currentSpeed, rb.linearVelocity.y);

        if (horizontalInput != 0f)
        {
            var scale = transform.localScale;
            scale.x = Mathf.Sign(horizontalInput) * defaultScaleX;
            transform.localScale = scale;
        }

        if (isGyroDashing)
        {
            if (currentAnimation != "Pirouette")
            {
                PlayerAnimator.Instance?.PlayAnimation("Pirouette");
                currentAnimation = "Pirouette";
            }
            return;
        }
        
        if (isDodging)
        {
            if (currentAnimation != "Dodge")
            {
                PlayerAnimator.Instance?.PlayAnimation("Dodge");
                currentAnimation = "Dodge";
            }
            return;
        }

        string targetAnimation;
        if (isJumping)
            targetAnimation = "Jump";
        else if (isCrouching)
            targetAnimation = "Crouch";
        else if (Mathf.Approximately(horizontalInput, 0f))
            targetAnimation = "Idle";
        else if (isDashing)
            targetAnimation = "Dash";
        else
            targetAnimation = "Run";

        if (targetAnimation != currentAnimation)
        {
            PlayerAnimator.Instance?.PlayAnimation(targetAnimation);
            currentAnimation = targetAnimation;
        }
    }
}
