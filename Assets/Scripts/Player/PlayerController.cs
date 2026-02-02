using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public enum PlayerState { Idle, Moving, InitiatingDance, Dancing }
    public PlayerState currentState = PlayerState.Idle;

    //================================ Fields =================================//
    [Header("Control Settings")]
    private float danceRange = 1.8f;
    //Components
    private PlayerMovement playerMovement;
    private PlayerAnimator playerAnimator;

    //Movement
    private Vector2 moveInput;

    //================================ Unity Methods =================================//

    private void Awake()
    {
        playerMovement = GetComponent<PlayerMovement>();
        playerAnimator = GetComponent<PlayerAnimator>();
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
            case PlayerState.Dancing:
                playerAnimator.PlayAnimation("Dance");
                break;
        }
        playerMovement.MovePlayer();
    }

    private void SetState(PlayerState newState)
    {
        if (currentState == newState) return;

        if (currentState == PlayerState.InitiatingDance && newState != PlayerState.Dancing)
        {
            PartnerController.Instance.ReturnToIdle();
        }

        currentState = newState;
    }

    //================================ Input Methods =================================//
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
        playerMovement.SetMoveInput(moveInput);
        SetState(PlayerState.Moving);

        if (moveInput.x > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else if (moveInput.x < 0)
            transform.localScale = new Vector3(-1, 1, 1);
    }

    //================================ State Behaviour =================================//

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

            if (currentState == PlayerState.InitiatingDance)
            {

            }
        }
    }

    private void HandleIdleState()
    {

    }

}