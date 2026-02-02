using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : Singleton<PlayerController>
{
    public enum PlayerState { Idle, Moving, InitiatingDance, PartnerDance, Dancing }
    public PlayerState currentState = PlayerState.Idle;

    //================================ Fields =================================//
    [Header("Distance to Initiate Dance")]
    [SerializeField] private float danceRange = 1.8f;
    [Header("Gyro Side Step")]
    [SerializeField] private float sideStepDistance = 2f;
    [SerializeField] private float sideStepDuration = 0.5f;

    //Components
    private PlayerMovement playerMovement;
    private PlayerAnimator playerAnimator;
    private DanceComboHandler comboHandler;
    private GyroController gyro;

    public PlayerMovement PlayerMovement => playerMovement;
    public PlayerAnimator PlayerAnimator => playerAnimator;

    //Movement
    private Vector2 moveInput;
    private Vector2 lookInput;

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

        comboHandler.OnComboFailed += OnComboFailed;
    }

    private void Update()
    {
        HandleStateSelection();
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
            case PlayerState.Dancing:
                playerAnimator.PlayAnimation("Dance");
                break;
        }

        if (currentState != PlayerState.PartnerDance) playerMovement.MovePlayer();
    }

    private void LateUpdate()
    {
        if (currentState == PlayerState.InitiatingDance)
        {
            if (isMoveLocked) return;

            if (gyro != null && gyro.IsGyroUpDetected())
            {
                ExecuteDanceMove("PN_05", false);
            }
            else if (gyro != null && gyro.IsGyroSideDetected())
            {
                ExecuteDanceMove("PN_06", true, gyro.WasLastGyroSideRight());
            }
        }
        // Handle inputs for PartnerDance (combo chain moves)
        else if (currentState == PlayerState.PartnerDance)
        {
            // Only accept input during the combo's input window
            if (!comboHandler.IsInputWindowOpen()) return;

            if (gyro != null && gyro.IsGyroUpDetected())
            {
                ExecuteDanceMove("PN_05", false);
            }
            else if (gyro != null && gyro.IsGyroSideDetected())
            {
                ExecuteDanceMove("PN_06", true, gyro.WasLastGyroSideRight());
            }
        }
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
        Debug.Log("Reset");
        isMoveLocked = false;
        if (gyro != null) gyro.ClearPendingTriggers(); // Clear any buffered inputs
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

        StartCoroutine(LockInputForDuration(animLength));
        SetState(PlayerState.PartnerDance);
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

}
