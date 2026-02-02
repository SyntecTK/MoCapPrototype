using UnityEngine;
using UnityEngine.InputSystem;

public class OldPlayerController : MonoBehaviour
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

    [Header("Jump Settings")]
    [SerializeField] private float jumpTiltThreshold = 0.7f; // Upward tilt threshold for jump
    [SerializeField] private float jumpHeight = 1.5f;        // Peak height of the jump
    [SerializeField] private float jumpDuration = 0.6f;      // Total time of the jump arc
    [SerializeField] private float sneakTiltThreshold = -0.5f; // New: Sneak detection threshold (how far down the stick must be)
    [SerializeField] private float crouchFreezeDelay = 0.5f; // time Sneaking should play before freezing

    private Animator animator;
    private float horizontalInput; // deprecated: kept for compatibility in jump capture
    private Vector3 velocity;
    // New: planar movement direction on XZ plane
    private Vector3 moveDirectionXZ;
    private float currentMoveSpeed;

    // Circle detection
    private Vector2 rightStickInput;
    private float lastAngle;
    private float totalAngleRotated;
    private float circleStartTime;
    private bool isTrackingCircle;
    private bool isPerformingRoll;

    // Jumping
    private Vector2 leftStickInput;
    private bool isPerformingJump;
    private bool jumpDebounce; // prevents retrigger while stick held up

    // Dance (bottom-half) detection
    private float lastAngleDance;
    private float totalAngleRotatedDance;
    private float danceStartTime;
    private bool isTrackingDance;

    private enum PlayerState { Idle, Walking, Jumping, Rolling }
    private PlayerState state = PlayerState.Idle;

    private float jumpForwardSpeed; // captured horizontal speed at jump start
    // New: captured planar momentum during jump
    private Vector3 jumpForwardVector;
    private bool animationOverrideActive; // locks animation changes while an override clip plays

    private bool actionModifierHeld; // true while right trigger is held
    private string currentAnimation;

    private Coroutine crouchFreezeRoutine;
    private bool crouchFreezeEngaged;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // Split movement from action logic
        UpdateMovement();
        UpdateActions();
    }

    // Movement-only update
    private void UpdateMovement()
    {
        if (state != PlayerState.Rolling)
        {
            // Use planar movement on XZ; keep jump momentum while jumping
            Vector3 planarVelocity = (state == PlayerState.Jumping)
                ? jumpForwardVector
                : moveDirectionXZ * currentMoveSpeed;

            velocity = new Vector3(planarVelocity.x, 0f, planarVelocity.z);
        }

        transform.position += velocity * Time.deltaTime;
    }

    // Attack/Move logic update (inputs that trigger actions)
    private void UpdateActions()
    {
        // Only detect circle (roll) while holding right trigger
        var gamepad = Gamepad.current;
        actionModifierHeld = gamepad != null && gamepad.rightTrigger.ReadValue() > 0.5f;

        if (actionModifierHeld)
        {
            // Top-half: Roll, Bottom-half: Dance
            DetectCircleMotion();      // top-half (existing)
            DetectDanceMotion();       // bottom-half (new)
        }
        else
        {
            if (isTrackingCircle) ResetCircleTracking();
            if (isTrackingDance) ResetDanceTracking();
            CheckJumpInput(); // jump only when RT not held
        }

        // While not moving, keep crouch idle rules responsive even if OnMove wasn't called this frame
        if (!animationOverrideActive && state != PlayerState.Jumping && state != PlayerState.Rolling)
        {
            bool isMoving = currentMoveSpeed > 0.0001f;
            if (!isMoving)
            {
                UpdateIdleSneakState();
            }
            else
            {
                // Movement resumed: ensure animator unfrozen and cancel any pending crouch freeze
                if (crouchFreezeRoutine != null)
                {
                    StopCoroutine(crouchFreezeRoutine);
                    crouchFreezeRoutine = null;
                }
                crouchFreezeEngaged = false;
                if (animator.speed == 0f) animator.speed = 1f;
            }
        }
    }

    public void OnMove(InputValue value)
    {
        if (IsInputBlocked()) return;

        Vector2 inputVector = value.Get<Vector2>();
        leftStickInput = inputVector;
        float inputMagnitudeX = Mathf.Abs(inputVector.x);

        // New: compute XZ movement from the stick
        Vector2 stick = inputVector;
        float mag = stick.magnitude;

        if (mag > 0.1f)
        {
            float moveSpeed = mag < slowWalkThreshold ? slowWalkSpeed : fastWalkSpeed;
            currentMoveSpeed = moveSpeed;
            moveDirectionXZ = new Vector3(stick.x, 0f, stick.y).normalized;
            horizontalInput = Mathf.Sign(inputVector.x) * moveSpeed;

            FaceDirection3D(moveDirectionXZ);

            if ((state == PlayerState.Idle || state == PlayerState.Walking) && !animationOverrideActive)
            {
                // Resume animation if previously frozen
                if (animator.speed == 0f) animator.speed = 1f;
                // Moving: cancel any pending crouch freeze
                if (crouchFreezeRoutine != null)
                {
                    StopCoroutine(crouchFreezeRoutine);
                    crouchFreezeRoutine = null;
                }
                crouchFreezeEngaged = false;

                string targetAnimation;
                if (!actionModifierHeld)
                {
                    bool rightDown = rightStickInput.y <= sneakTiltThreshold;

                    if (rightDown)
                    {
                        targetAnimation = "Sneaking";
                    }
                    else
                    {
                        targetAnimation = mag < slowWalkThreshold ? "Walk_Slow" : "Walk_Fast";
                    }
                }
                else
                {
                    targetAnimation = mag < slowWalkThreshold ? "Walk_Slow" : "Walk_Fast";
                }

                if (currentAnimation != targetAnimation)
                {
                    SetState(PlayerState.Walking, targetAnimation);
                }
            }
        }
        else
        {
            currentMoveSpeed = 0f;
            moveDirectionXZ = Vector3.zero;
            horizontalInput = 0f;

            if (!animationOverrideActive && state != PlayerState.Jumping && state != PlayerState.Rolling)
            {
                // Delegate to helper for idle sneak/crouch behavior
                UpdateIdleSneakState();
            }
        }
    }

    public void OnLook(InputValue value)
    {
        // Only accept right stick from a gamepad; ignore mouse delta or other pointer inputs
        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            rightStickInput = gamepad.rightStick.ReadValue();
        }
        else
        {
            rightStickInput = Vector2.zero; // no gamepad, prevent accidental action triggers
        }
    }

    private void DetectCircleMotion()
    {
        float magnitude = rightStickInput.magnitude;

        if (magnitude > circleDetectionRadius)
        {
            // Only consider inputs in the top half (y >= 0)
            if (rightStickInput.y < 0f)
            {
                if (isTrackingCircle) ResetCircleTracking();
                return;
            }

            float currentAngle = Mathf.Atan2(rightStickInput.y, rightStickInput.x) * Mathf.Rad2Deg;

            if (!isTrackingCircle)
            {
                // Start tracking only when entering from the top half
                isTrackingCircle = true;
                lastAngle = currentAngle;
                totalAngleRotated = 0f;
                circleStartTime = Time.time;
            }
            else
            {
                float angleDelta = Mathf.DeltaAngle(lastAngle, currentAngle);
                totalAngleRotated += angleDelta;
                lastAngle = currentAngle;

                // Half-circle completion (top arc). Expect negative rotation for left->up->right.
                bool completedHalfCircle = Mathf.Abs(totalAngleRotated) >= (180f - angleTolerance);
                bool correctDirection = totalAngleRotated <= 0f; // left(180)->up(90)->right(0) yields negative delta sum

                if (completedHalfCircle && correctDirection)
                {
                    if (Time.time - circleStartTime <= circleCompletionTime)
                    {
                        OnCircleCompleted(clockwise: false); // top half left->right is counter-clockwise relative to angle decreasing
                    }
                    ResetCircleTracking();
                }
                else if (Time.time - circleStartTime > circleCompletionTime)
                {
                    ResetCircleTracking();
                }
            }
        }
        else
        {
            if (isTrackingCircle) ResetCircleTracking();
        }
    }

    // New: bottom-half half-circle detector for Dance (left -> down -> right)
    private void DetectDanceMotion()
    {
        float magnitude = rightStickInput.magnitude;

        if (magnitude > circleDetectionRadius)
        {
            // Only consider inputs in the bottom half (y < 0)
            if (rightStickInput.y >= 0f)
            {
                if (isTrackingDance) ResetDanceTracking();
                return;
            }

            float currentAngle = Mathf.Atan2(rightStickInput.y, rightStickInput.x) * Mathf.Rad2Deg;

            if (!isTrackingDance)
            {
                isTrackingDance = true;
                lastAngleDance = currentAngle;
                totalAngleRotatedDance = 0f;
                danceStartTime = Time.time;
            }
            else
            {
                float angleDelta = Mathf.DeltaAngle(lastAngleDance, currentAngle);
                totalAngleRotatedDance += angleDelta;
                lastAngleDance = currentAngle;

                // Half-circle completion (bottom arc). Expect positive rotation for left->down->right.
                bool completedHalfCircle = Mathf.Abs(totalAngleRotatedDance) >= (180f - angleTolerance);
                bool correctDirection = totalAngleRotatedDance >= 0f; // left(180)->down(-90)->right(0) yields positive delta sum

                if (completedHalfCircle && correctDirection)
                {
                    if (!animationOverrideActive && Time.time - danceStartTime <= circleCompletionTime)
                    {
                        BeginAction(PlayOverrideAnimation("Dance"));
                    }
                    ResetDanceTracking();
                }
                else if (Time.time - danceStartTime > circleCompletionTime)
                {
                    ResetDanceTracking();
                }
            }
        }
        else
        {
            if (isTrackingDance) ResetDanceTracking();
        }
    }

    private void OnCircleCompleted(bool clockwise)
    {
        Debug.Log($"Circle completed! Direction: {(clockwise ? "Clockwise" : "Counter-clockwise")}");

        if (state != PlayerState.Rolling && !animationOverrideActive)
        {
            BeginAction(PerformRoll());
        }
    }

    private System.Collections.IEnumerator PerformRoll()
    {
        SetState(PlayerState.Rolling, "Roll");

        // Wait for the animation to finish
        yield return WaitForAnimationToFinish();

        velocity = Vector3.zero;
        SetState(PlayerState.Idle, "Idle");
    }

    private void CheckJumpInput()
    {
        // Block jump while rolling, overriding animations, already jumping, or RT held
        if (IsInputBlocked() || animationOverrideActive || state == PlayerState.Jumping || actionModifierHeld) return;

        bool rightUp = rightStickInput.y >= jumpTiltThreshold;

        if (!jumpDebounce && (rightUp))
        {
            jumpForwardSpeed = horizontalInput;
            BeginAction(PerformJump());
            jumpDebounce = true; // latch until jump completes
        }

        if (!rightUp)
        {
            // keep debounce if jump is in progress; only cleared at end of jump
            if (state != PlayerState.Jumping)
            {
                jumpDebounce = false;
            }
        }
    }

    private System.Collections.IEnumerator PerformJump()
    {
        SetState(PlayerState.Jumping, "Jump");
        jumpDebounce = true; // ensure no re-trigger during the arc

        // Capture planar momentum at jump start
        jumpForwardVector = moveDirectionXZ * currentMoveSpeed;
        jumpForwardSpeed = horizontalInput; // legacy compatibility (not used for movement anymore)

        // Wait for crossfade before manual motion
        yield return new WaitForSeconds(animationCrossfadeTime);

        float baseY = transform.position.y;
        float elapsed = 0f;
        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / jumpDuration);
            float yOffset = 4f * jumpHeight * t * (1f - t);

            Vector3 pos = transform.position;
            pos.y = baseY + yOffset;
            transform.position = pos;

            yield return null;
        }

        transform.position = new Vector3(transform.position.x, baseY, transform.position.z);

        // Finish any leftover animation time (if jump clip longer)
        yield return WaitForAnimationToFinish(animationCrossfadeTime + jumpDuration);

        SetState(PlayerState.Idle, "Idle");
        jumpDebounce = false; // allow jumping again after finishing
    }

    private void SetState(PlayerState newState, string animationName)
    {
        state = newState;
        PlayAnimation(animationName);
    }

    // Helpers

    // New: 3D facing using rotation toward movement direction
    private void FaceDirection3D(Vector3 dirXZ)
    {
        if (dirXZ.sqrMagnitude < 0.0001f) return;
        transform.forward = dirXZ.normalized;
    }

    private bool IsInputBlocked()
    {
        return state == PlayerState.Rolling;
    }

    private void PlayAnimation(string animationName)
    {
        if (string.IsNullOrEmpty(animationName)) return;
        if (currentAnimation == animationName) return;

        currentAnimation = animationName;
        animator.CrossFade(animationName, animationCrossfadeTime);
    }

    // Wait for the currently playing state's length; optional consumedTime subtracts elapsed section
    private System.Collections.IEnumerator WaitForAnimationToFinish(float consumedTime = 0f)
    {
        // Small delay to allow CrossFade to apply
        yield return new WaitForSeconds(animationCrossfadeTime);

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float clipLength = stateInfo.length;
        float wait = Mathf.Max(0f, clipLength - consumedTime);
        if (wait > 0f) yield return new WaitForSeconds(wait);
    }

    private void BeginAction(System.Collections.IEnumerator coroutine)
    {
        StartCoroutine(coroutine);
    }

    private void ResetCircleTracking()
    {
        isTrackingCircle = false;
        totalAngleRotated = 0f;
    }

    private void ResetDanceTracking()
    {
        isTrackingDance = false;
        totalAngleRotatedDance = 0f;
    }

    // Plays a one-off override animation and locks other animation transitions until it finishes
    private System.Collections.IEnumerator PlayOverrideAnimation(string animationName)
    {
        // Resume animator if frozen before starting override
        if (animator.speed == 0f) animator.speed = 1f;
        // Cancel any pending crouch freeze during override
        if (crouchFreezeRoutine != null)
        {
            StopCoroutine(crouchFreezeRoutine);
            crouchFreezeRoutine = null;
        }
        crouchFreezeEngaged = false;

        animationOverrideActive = true;
        PlayAnimation(animationName);

        yield return WaitForAnimationToFinish();

        animationOverrideActive = false;

        if (state != PlayerState.Jumping && state != PlayerState.Rolling)
        {
            float inputMagnitude = leftStickInput.magnitude;
            if (inputMagnitude > 0.1f)
            {
                bool rightDown = rightStickInput.y <= sneakTiltThreshold;
                string targetAnimation = rightDown
                    ? "Sneaking"
                    : (inputMagnitude < slowWalkThreshold ? "Walk_Slow" : "Walk_Fast");
                SetState(PlayerState.Walking, targetAnimation);
            }
            else
            {
                // Check crouch-idle again after override ends
                bool rightDown = rightStickInput.y <= sneakTiltThreshold;
                if (rightDown)
                {
                    SetState(PlayerState.Idle, "Sneaking");
                    // Start delayed freeze after entering crouch idle
                    StartCrouchFreeze();
                }
                else
                {
                    SetState(PlayerState.Idle, "Idle");
                }
            }
        }
    }

    // Helper: handles sneak end and crouch idle start while not moving
    private void UpdateIdleSneakState()
    {
        bool rightDown = rightStickInput.y <= sneakTiltThreshold;

        if (rightDown)
        {
            // Start/keep crouch idle: show Sneaking and start freeze timer if not already engaged
            if (currentAnimation != "Sneaking")
            {
                SetState(PlayerState.Idle, "Sneaking");
            }

            // Ensure animator plays for a short delay before freezing
            if (animator.speed == 0f) animator.speed = 1f;

            if (!crouchFreezeEngaged && crouchFreezeRoutine == null)
            {
                StartCrouchFreeze();
            }
        }
        else
        {
            // End sneak when neutral or up: cancel freeze timer and unfreeze to Idle
            if (crouchFreezeRoutine != null)
            {
                StopCoroutine(crouchFreezeRoutine);
                crouchFreezeRoutine = null;
            }
            crouchFreezeEngaged = false;

            if (animator.speed == 0f) animator.speed = 1f;
            if (currentAnimation != "Idle")
            {
                SetState(PlayerState.Idle, "Idle");
            }
        }
    }

    private void StartCrouchFreeze()
    {
        // Cancel any existing routine before starting a new one
        if (crouchFreezeRoutine != null)
        {
            StopCoroutine(crouchFreezeRoutine);
        }
        crouchFreezeRoutine = StartCoroutine(CrouchFreezeAfterDelay());
    }

    private System.Collections.IEnumerator CrouchFreezeAfterDelay()
    {
        // Wait the configured delay while Sneaking plays
        float elapsed = 0f;
        while (elapsed < crouchFreezeDelay)
        {
            // If stick returns neutral/up or movement starts, abort
            bool rightDown = rightStickInput.y <= sneakTiltThreshold;
            bool isMoving = currentMoveSpeed > 0.0001f;
            if (!rightDown || isMoving || animationOverrideActive || state == PlayerState.Jumping || state == PlayerState.Rolling)
            {
                crouchFreezeRoutine = null;
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Freeze animation to mimic crouch idle
        animator.speed = 0f;
        crouchFreezeEngaged = true;
        crouchFreezeRoutine = null;
    }
}
