using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : Singleton<PlayerController>
{
    public enum PlayerState { Idle, Moving, InitiatingDance, PartnerDance }
    public PlayerState currentState = PlayerState.Idle;

    //================================ Fields =================================//
    [Header("Distance to Initiate Dance")]
    [SerializeField] private float danceRange = 1.8f;
    [Header("Gyro Side Step")]
    [SerializeField] private float sideStepDistance = 2f;
    [SerializeField] private float sideStepDuration = 1.5f;

    //Components
    private PlayerMovement playerMovement;
    private PlayerAnimator playerAnimator;
    private DanceComboHandler comboHandler;
    private GyroController gyro;

    private GameObject grabHitbox;

    public PlayerMovement PlayerMovement => playerMovement;
    public PlayerAnimator PlayerAnimator => playerAnimator;

    //Movement
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isPerformingSoloMove = false;

    //Misc
    private Vector3 startPosition = Vector3.zero;
    private bool isMoveLocked = false;
    private Coroutine activeDanceMoveCoroutine;

    //================================ Unity Methods =================================//

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerAnimator = GetComponent<PlayerAnimator>();
        comboHandler = GetComponent<DanceComboHandler>();
        gyro = GetComponent<GyroController>();

        grabHitbox = GetComponentInChildren<GrabHitbox>().gameObject;

        comboHandler.OnComboFailed += OnComboFailed;
    }

    private void Update()
    {
        HandleStateSelection();
        if (!isPerformingSoloMove)
        {

            switch (currentState)
            {
                case PlayerState.Idle:
                    playerAnimator.PlayAnimation("Idle");
                    break;
                case PlayerState.Moving:
                    playerAnimator.PlayAnimation("Walk");
                    break;
                case PlayerState.InitiatingDance:
                    playerAnimator.PlayAnimation("DancePose");
                    PartnerController.Instance.InitiateDance();
                    break;
                case PlayerState.PartnerDance:
                    PartnerController.Instance.Deactivate();
                    comboHandler.UpdateCombo();
                    // If combo completes or fails, handle in comboHandler (e.g., set back to InitiatingDance)
                    break;
            }
        }

        if (currentState != PlayerState.PartnerDance) playerMovement.MovePlayer();
    }

    private void LateUpdate()
    {
        if (currentState == PlayerState.InitiatingDance)
        {
            if (isMoveLocked) return;
            TryExecuteDanceInput(currentState);
        }
        else if (currentState == PlayerState.PartnerDance)
        {
            if (!comboHandler.IsInputWindowOpen()) return;
            TryExecuteDanceInput(currentState);
        }
        else if (currentState == PlayerState.Idle || currentState == PlayerState.Moving)
        {
            if (isMoveLocked) return;
            TryExecuteDanceInput(currentState);
        }
    }

    //================================ Dance Input Handling =================================//
    private void TryExecuteDanceInput(PlayerState state)
    {
        if (state == PlayerState.InitiatingDance || state == PlayerState.PartnerDance)
        {
            if (gyro != null && gyro.IsGyroUpDetected())
            {
                ExecuteDanceMove("PN_05", false);
            }
            else if (gyro != null && gyro.IsGyroSideDetected())
            {
                ExecuteDanceMove("PN_06", true, gyro.WasLastGyroSideRight());
            }
            else if (MotionController.Instance != null && MotionController.Instance.IsMotionInputRightSweepDetected())
            {
                if (playerAnimator.GetCurrentAnimationName() == "PN_04")
                {
                    ActivateGrabHitbox();
                    ExecuteDanceMove("K_PN_01", false);
                }
                else
                {
                    ExecuteDanceMove("PN_03", false);
                }
            }
            else if (IsRightStickUpDetected())
            {
                ExecuteDanceMove("PN_04", false);
            }
        }
        else if (state == PlayerState.Idle || state == PlayerState.Moving)
        {
            if (MotionController.Instance != null && MotionController.Instance.IsMotionInputRightSweepDetected())
            {
                ExecuteSoloMove("Pirouette", true, transform.localScale.x > 0);
            }
            else if (IsRightStickUpDetected())
            {
                ExecuteSoloMove("Jump", false);
            }
        }
    }

    private bool IsRightStickUpDetected()
    {
        if (Gamepad.current == null) return false;

        Vector2 rightStick = Gamepad.current.rightStick.ReadValue();
        return rightStick.y > 0.8f;
    }

    //================================ State Management =================================//

    private void SetState(PlayerState newState)
    {
        if (currentState == newState) return;

        PlayerState previousState = currentState;
        currentState = newState;

        if (newState == PlayerState.InitiatingDance && previousState != PlayerState.PartnerDance)
        {
            startPosition = transform.position;
        }

        if (previousState == PlayerState.InitiatingDance && newState != PlayerState.PartnerDance)
        {
            PartnerController.Instance.ReturnToIdle();
        }

        if (newState == PlayerState.PartnerDance)
        {
            PartnerController.Instance.Deactivate();
            comboHandler.StartCombo();
        }
        else if (previousState == PlayerState.PartnerDance)
        {
            PartnerController.Instance.Activate();

            if (!comboHandler.IsComboActive())
            {
                SetState(PlayerState.InitiatingDance);
            }
        }
    }

    private void HandleStateSelection()
    {
        if (currentState == PlayerState.Idle || currentState == PlayerState.Moving)
        {
            SetState(playerMovement.HasMoveInput() ? PlayerState.Moving : PlayerState.Idle);

            Vector3 partnerPos = PartnerController.Instance.PartnerLocation();
            float distSqr = (partnerPos - transform.position).sqrMagnitude;
            float danceRangeSqr = danceRange * danceRange;

            if (distSqr <= danceRangeSqr)
            {
                SetState(PlayerState.InitiatingDance);
            }
        }
    }

    //================================ Input Methods =================================//
    public void OnMove(InputValue value)
    {
        if (currentState == PlayerState.PartnerDance) return;

        moveInput = value.Get<Vector2>();
        playerMovement.SetMoveInput(moveInput);
        SetState(PlayerState.Moving);

        if (moveInput.x > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else if (moveInput.x < 0)
            transform.localScale = new Vector3(-1, 1, 1);
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
        // In InitiateDance, handle combo instead of jump
        if (currentState == PlayerState.InitiatingDance && lookInput.y > 0.8f)
        {
            // Trigger combo check below
        }
        else if (currentState != PlayerState.InitiatingDance && lookInput.y > 0.8f)
        {
            // Handle jump in other states (assuming PlayerMovement has jump logic)
            //playerMovement.TriggerJump();
        }
    }


    //================================ Event Handlers =================================//
    private void OnComboFailed()
    {
        isMoveLocked = false;
        if (gyro != null) gyro.ClearPendingTriggers();
        DeactivateGrabHitbox();
        SetState(PlayerState.InitiatingDance);
        ResetPlayerPosition();
    }

    private void ResetPlayerPosition()
    {
        transform.position = startPosition;
        transform.localScale = new Vector3(1, 1, 1);

    }

    //================================ Move Movements =================================//
    /// <summary>
    /// Smoothly moves the player sideways during dance.
    /// </summary>
    /// <param name="moveRight">True to move right, false to move left</param>
    public void DanceSideWays(bool moveRight)
    {
        // Stoppe die vorherige Bewegung falls noch aktiv
        if (activeDanceMoveCoroutine != null)
        {
            StopCoroutine(activeDanceMoveCoroutine);
        }

        if (moveRight)
            transform.localScale = new Vector3(1, 1, 1);
        else
            transform.localScale = new Vector3(-1, 1, 1);

        float direction = moveRight ? 1f : -1f;
        activeDanceMoveCoroutine = StartCoroutine(SmoothDanceMove(direction));
    }

    private IEnumerator SmoothDanceMove(float direction)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + new Vector3(direction * sideStepDistance, 0f, 0f);

        float elapsed = 0f;

        while (elapsed < sideStepDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / sideStepDuration;

            // Smooth easing (ease out)
            t = 1f - Mathf.Pow(1f - t, 2f);

            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        // Ensure we end exactly at the target
        transform.position = endPos;
    }

    /// <summary>
    /// Executes a dance move with animation and optional sideways movement
    /// </summary>
    private void ExecuteDanceMove(string animationName, bool hasSideMovement, bool moveRight = false)
    {
        float animLength = playerAnimator.GetAnimationLength(animationName);
        playerAnimator.PlayAnimation(animationName, true);
        comboHandler.OnMoveStarted(playerAnimator.GetCurrentAnimationName(), animLength);

        if (hasSideMovement)
        {
            DanceSideWays(moveRight);
        }

        // Special handling for grab move
        if (animationName == "K_PN_01")
        {
            ActivateGrabHitbox();
            StartCoroutine(DeactivateGrabHitboxAfterDelay(animLength));
        }

        StartCoroutine(LockInputForDuration(animLength));
        SetState(PlayerState.PartnerDance);
    }

    private void ExecuteSoloMove(string animationName, bool hasSideMovement, bool moveRight = false)
    {
        float animLength = playerAnimator.GetAnimationLength(animationName);
        playerAnimator.PlayAnimation(animationName, true);
        isPerformingSoloMove = true;

        if (hasSideMovement)
        {
            DanceSideWays(moveRight);
        }

        StartCoroutine(LockInputForDuration(animLength));
        StartCoroutine(ResetSoloMoveFlag(animLength));
    }

    //================================ Input Lock =================================//
    /// <summary>
    /// Locks input for a specified duration minus a small window for the next input.
    /// </summary>
    private IEnumerator LockInputForDuration(float duration)
    {
        isMoveLocked = true;
        // Allow input slightly before the animation ends (input window)
        float inputWindowBuffer = 0.3f; // Adjust as needed
        float lockTime = Mathf.Max(0f, duration - inputWindowBuffer);
        yield return new WaitForSeconds(lockTime);
        isMoveLocked = false;
    }

    //================================ Misc =================================//

    private void ActivateGrabHitbox()
    {
        if (grabHitbox != null)
            grabHitbox.GetComponent<BoxCollider2D>().enabled = true;
    }

    private void DeactivateGrabHitbox()
    {
        if (grabHitbox != null)
            grabHitbox.GetComponent<BoxCollider2D>().enabled = false;
    }

    private IEnumerator DeactivateGrabHitboxAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        DeactivateGrabHitbox();
    }

    private IEnumerator ResetSoloMoveFlag(float delay)
    {
        yield return new WaitForSeconds(delay);
        isPerformingSoloMove = false;
    }

}
