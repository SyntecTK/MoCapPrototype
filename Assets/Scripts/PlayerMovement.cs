using UnityEngine;
using UnityEngine.InputSystem;

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

        float vertical = value.Get<Vector2>().y;
        bool wantsJump = vertical >= lookJumpThreshold;

        if (wantsJump && !lookHeld && !isJumping)
        {
            TriggerJump();
        }

        lookHeld = wantsJump;
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

        if (currentAnimation != "Dash-Attack")
        {
            PlayerAnimator.Instance?.PlayAnimation("Dash-Attack");
            currentAnimation = "Dash-Attack";
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
        isGyroDashing = false;
    }

    private void FixedUpdate()
    {
        bool isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundMask);

        if (!hasLeftGround && !isGrounded)
            hasLeftGround = true;

        if (isGrounded && hasLeftGround)
            isJumping = false;

        bool isDashing = !Mathf.Approximately(horizontalInput, 0f)
                         && moveMagnitude >= 0.8f
                         && verticalTilt <= 0.5f;
        float currentSpeed = moveSpeed + (isDashing ? dashSpeedBonus : 0f);
        rb.linearVelocity = new Vector2(horizontalInput * currentSpeed, rb.linearVelocity.y);

        if (horizontalInput != 0f)
        {
            var scale = transform.localScale;
            scale.x = Mathf.Sign(horizontalInput) * defaultScaleX;
            transform.localScale = scale;
        }

        if (isGyroDashing)
        {
            if (currentAnimation != "Dash-Attack")
            {
                PlayerAnimator.Instance?.PlayAnimation("Dash-Attack");
                currentAnimation = "Dash-Attack";
            }
            return;
        }

        string targetAnimation;
        if (isJumping)
            targetAnimation = "Jump";
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
