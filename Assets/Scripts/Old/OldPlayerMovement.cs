// using UnityEngine;
// using UnityEngine.InputSystem;
// using System.Collections.Generic;
// using System.Collections;
// using Unity.VisualScripting;
// using System.Runtime.CompilerServices;

// public class OldPlayerMovement : MonoBehaviour
// {
//     enum AnimationState
//     {
//         Idle,
//         Walk,
//         Jump,
//         Crouch,
//         Pirouette,
//         Dodge,
//         DanceIdle,
//         DancePose,
//         DanceWalk,
//         DanceJump,
//         DancePirouette,
//         DanceJumpPirouette
//     }

//     enum DanceState
//     {
//         Solo,       // Normal moves, far from partner
//         Beginning,  // Near partner, DanceIdle as idle, normal moves
//         Dancing     // Performing dance, partner deactivated, all dance moves
//     }

//     [SerializeField] private Transform raycastOrigin;
//     [SerializeField] private float moveSpeed = 5f;
//     [SerializeField] private float dashSpeedBonus = 3f;
//     [SerializeField] private float jumpForce = 7f;
//     [SerializeField] private float lookJumpThreshold = 0.5f;
//     [SerializeField] private LayerMask groundMask;
//     [SerializeField] private float groundCheckDistance = 0.5f;
//     [SerializeField] private float gyroDashDistance = 1.5f; // ~5 feet
//     [SerializeField] private float gyroDashDuration = 0.25f;
//     [SerializeField] private float dodgeDistance = 2f;
//     [SerializeField] private float dodgeDuration = 0.3f;


//     private PlayerInput playerInput;
//     private Rigidbody2D rb;
//     private float horizontalInput;
//     private float defaultScaleX;
//     private float moveMagnitude;
//     private float verticalTilt;
//     private AnimationState currentAnimation;
//     private AnimationState targetAnimation;
//     private bool lookHeld;
//     private bool isJumping;
//     private bool hasLeftGround;
//     private bool isGyroDashing;
//     private bool isDodging;
//     private bool isCrouching;
//     private bool isNearPartner;
//     private bool isDanceJumpPirouetting;
//     private float jumpStartTime;
//     private DanceState danceState = DanceState.Solo;

//     //Dance Partner
//     private Transform partnerTransform;
//     private Animator partnerAnimator;


//     private void Awake()
//     {
//         //partnerTransform = FindAnyObjectByType<PartnerController>().transform;
//         rb = GetComponent<Rigidbody2D>();
//         defaultScaleX = Mathf.Abs(transform.localScale.x);
//         if (playerInput == null)
//             playerInput = GetComponent<PlayerInput>();

//         targetAnimation = AnimationState.Idle;

//         partnerAnimator = partnerTransform.GetComponent<Animator>();
//     }
//     public void OnMove(InputValue value)
//     {
//         var move = value.Get<Vector2>();
//         moveMagnitude = Mathf.Abs(move.x); // horizontal strength only
//         verticalTilt = Mathf.Abs(move.y);
//         horizontalInput = Mathf.Clamp(move.x, -1f, 1f);
//     }
//     public void OnLook(InputValue value)
//     {
//         if (playerInput != null && playerInput.currentControlScheme != "Gamepad")
//             return;

//         Vector2 lookInput = value.Get<Vector2>();
//         float vertical = lookInput.y;

//         // Check for crouch
//         if (vertical <= MotionController.Instance.CrouchThreshold && lookInput.magnitude >= 0.7f)
//         {
//             isCrouching = true;
//         }
//         else
//         {
//             isCrouching = false;
//         }

//         bool wantsJump = vertical >= lookJumpThreshold;
//         lookHeld = wantsJump;

//         // Track right stick for gesture detection (only if not crouching)
//     }

//     public void TriggerPirouette()
//     {
//         // Check for combo move first: DanceJumpPirouette (cancels dance jump)
//         if (danceState == DanceState.Dancing && isJumping)
//         {
//             StartCoroutine(DanceJumpPirouetteRoutine());
//             return;
//         }

//         // Don't interrupt other active moves (only walking and dance jump can be canceled)
//         if (isGyroDashing || isDodging || isDanceJumpPirouetting || isJumping || isCrouching) return;

//         // Trigger dance mode if in Beginning state
//         if (danceState == DanceState.Beginning)
//         {
//             StartDanceMode();
//         }

//         StartCoroutine(PirouetteRoutine());
//     }

//     public void TriggerGyroDash()
//     {
//         // Don't interrupt active moves (only walking can be canceled)
//         if (isGyroDashing || isDodging || isDanceJumpPirouetting || isJumping || isCrouching) return;

//         // Trigger dance mode if in Beginning state
//         if (danceState == DanceState.Beginning)
//         {
//             StartDanceMode();
//         }

//         StartCoroutine(GyroDodgeRoutine());
//     }


//     private void StartDanceMode()
//     {
//         danceState = DanceState.Dancing;
//         partnerTransform.SetParent(transform);
//         partnerTransform.gameObject.SetActive(false);
//     }

//     private void EndDanceMode()
//     {
//         danceState = DanceState.Solo;
//         partnerTransform.SetParent(null);
//         partnerTransform.gameObject.SetActive(true);
//     }

//     private IEnumerator PirouetteRoutine()
//     {
//         isGyroDashing = true;
//         float facing = Mathf.Sign(transform.localScale.x);

//         string pirouetteAnim = (danceState == DanceState.Dancing) ? "DancePirouette" : "Pirouette";
//         AnimationState pirouetteState = (danceState == DanceState.Dancing) ? AnimationState.DancePirouette : AnimationState.Pirouette;

//         if (currentAnimation != pirouetteState)
//         {
//             PlayerAnimator.Instance?.PlayAnimation(pirouetteAnim);
//             currentAnimation = pirouetteState;
//         }

//         float elapsed = 0f;
//         while (elapsed < gyroDashDuration)
//         {
//             float t = elapsed / gyroDashDuration;
//             elapsed += Time.fixedDeltaTime;
//             yield return new WaitForFixedUpdate();
//         }

//         // Wait for Pirouette animation to complete
//         if (PlayerAnimator.Instance != null)
//         {
//             float animationLength = PlayerAnimator.Instance.GetAnimationLength(pirouetteAnim);
//             float remainingTime = Mathf.Max(0, animationLength - gyroDashDuration);
//             yield return new WaitForSeconds(remainingTime);
//         }

//         isGyroDashing = false;
//     }

//     private IEnumerator DanceJumpPirouetteRoutine()
//     {
//         isDanceJumpPirouetting = true;
//         float facing = Mathf.Sign(transform.localScale.x);

//         if (currentAnimation != AnimationState.DanceJumpPirouette)
//         {
//             PlayerAnimator.Instance?.PlayAnimation("DanceJumpPirouette");
//             currentAnimation = AnimationState.DanceJumpPirouette;
//         }

//         float elapsed = 0f;
//         while (elapsed < gyroDashDuration)
//         {
//             float t = elapsed / gyroDashDuration;
//             elapsed += Time.fixedDeltaTime;
//             yield return new WaitForFixedUpdate();
//         }

//         // Wait for DanceJumpPirouette animation to complete
//         if (PlayerAnimator.Instance != null)
//         {
//             float animationLength = PlayerAnimator.Instance.GetAnimationLength("DanceJumpPirouette");
//             float remainingTime = Mathf.Max(0, animationLength - gyroDashDuration);
//             yield return new WaitForSeconds(remainingTime);
//         }

//         isDanceJumpPirouetting = false;
//     }

//     private IEnumerator GyroDodgeRoutine()
//     {
//         isDodging = true;
//         float facing = Mathf.Sign(transform.localScale.x);
//         Vector2 start = rb.position;
//         Vector2 target = start + new Vector2(facing * dodgeDistance, 0f);

//         string dodgeAnim = (danceState == DanceState.Dancing) ? "DanceDodge" : "Dodge";
//         PlayerAnimator.Instance?.PlayAnimation(dodgeAnim);
//         currentAnimation = AnimationState.Dodge;

//         float elapsed = 0f;
//         while (elapsed < dodgeDuration)
//         {
//             float t = elapsed / dodgeDuration;
//             rb.MovePosition(Vector2.Lerp(start, target, t));
//             elapsed += Time.fixedDeltaTime;
//             yield return new WaitForFixedUpdate();
//         }
//         rb.MovePosition(target);
//         isDodging = false;
//     }

//     private void FixedUpdate()
//     {
//         isNearPartner = IsPartnerInDistance();
//         bool isGrounded = Physics2D.Raycast(raycastOrigin.position, Vector2.down, groundCheckDistance, groundMask);

//         if (!hasLeftGround && !isGrounded) hasLeftGround = true;

//         // Handle jump input
//         if (lookHeld && !isJumping && !isDodging && !isGyroDashing && !isDanceJumpPirouetting && isGrounded)
//         {
//             // Trigger dance mode if in Beginning state
//             if (danceState == DanceState.Beginning)
//             {
//                 StartDanceMode();
//             }

//             isJumping = true;
//             hasLeftGround = false;
//             jumpStartTime = Time.time;

//             // In Dance state, use 0 jump force (no actual jump)
//             float actualJumpForce = (danceState == DanceState.Dancing) ? 0f : jumpForce;
//             rb.linearVelocity = new Vector2(rb.linearVelocity.x, actualJumpForce);
//         }

//         // Reset jump when landed
//         if (isJumping && isGrounded && hasLeftGround)
//         {
//             isJumping = false;
//         }

//         // In Dance state, end jump after animation completes (since there's no actual jump)
//         if (isJumping && danceState == DanceState.Dancing && PlayerAnimator.Instance != null)
//         {
//             float animLength = PlayerAnimator.Instance.GetAnimationLength("DanceJump");
//             if (Time.time - jumpStartTime >= animLength)
//             {
//                 isJumping = false;
//             }
//         }

//         bool isDashing = !Mathf.Approximately(horizontalInput, 0f)
//                          && moveMagnitude >= 0.8f
//                          && verticalTilt <= 0.5f
//                          && !isCrouching;
//         float currentSpeed = moveSpeed + (isDashing ? dashSpeedBonus : 0f);

//         if (isCrouching) currentSpeed *= 0.5f;

//         // Disable horizontal movement while jumping
//         if (!isJumping)
//         {
//             rb.linearVelocity = new Vector2(horizontalInput * currentSpeed, rb.linearVelocity.y);
//         }

//         if (horizontalInput != 0f)
//         {
//             var scale = transform.localScale;
//             scale.x = Mathf.Sign(horizontalInput) * defaultScaleX;
//             transform.localScale = scale;
//         }

//         if (isDanceJumpPirouetting)
//         {
//             if (currentAnimation != AnimationState.DanceJumpPirouette)
//             {
//                 PlayerAnimator.Instance?.PlayAnimation("DanceJumpPirouette");
//                 currentAnimation = AnimationState.DanceJumpPirouette;
//             }
//             return;
//         }

//         if (isGyroDashing)
//         {
//             string pirouetteAnim = (danceState == DanceState.Dancing) ? "DancePirouette" : "Pirouette";
//             AnimationState pirouetteState = (danceState == DanceState.Dancing) ? AnimationState.DancePirouette : AnimationState.Pirouette;

//             if (currentAnimation != pirouetteState)
//             {
//                 PlayerAnimator.Instance?.PlayAnimation(pirouetteAnim);
//                 currentAnimation = pirouetteState;
//             }
//             return;
//         }

//         if (isDodging)
//         {
//             if (currentAnimation != AnimationState.Dodge)
//             {
//                 string dodgeAnim = (danceState == DanceState.Dancing) ? "DanceDodge" : "Dodge";
//                 PlayerAnimator.Instance?.PlayAnimation(dodgeAnim);
//                 currentAnimation = AnimationState.Dodge;
//             }
//             return;
//         }

//         if (isJumping)
//         {
//             string jumpAnim = (danceState == DanceState.Dancing) ? "DanceJump" : "Jump";
//             AnimationState jumpState = (danceState == DanceState.Dancing) ? AnimationState.DanceJump : AnimationState.Jump;

//             if (currentAnimation != jumpState)
//             {
//                 PlayerAnimator.Instance?.PlayAnimation(jumpAnim);
//                 currentAnimation = jumpState;
//             }
//             return;
//         }

//         // Update dance state based on proximity
//         DanceState previousState = danceState;

//         if (danceState == DanceState.Dancing)
//         {
//             // Stay in Dancing until we leave partner distance
//             if (!isNearPartner)
//             {
//                 EndDanceMode();
//             }
//         }
//         else if (isNearPartner)
//         {
//             danceState = DanceState.Beginning;
//         }
//         else
//         {
//             danceState = DanceState.Solo;
//         }

//         // Update partner animator based on state transitions
//         if (danceState == DanceState.Beginning && previousState == DanceState.Solo)
//         {
//             partnerAnimator.CrossFade("Begin", 0.1f);
//         }
//         else if (danceState == DanceState.Solo && previousState == DanceState.Beginning)
//         {
//             partnerAnimator.CrossFade("Idle", 0.1f);
//         }

//         // Determine target animation based on dance state
//         switch (danceState)
//         {
//             case DanceState.Solo:
//                 // Normal animations
//                 if (isGrounded)
//                 {
//                     if (isJumping)
//                         targetAnimation = AnimationState.Jump;
//                     else if (!Mathf.Approximately(horizontalInput, 0f))
//                         targetAnimation = AnimationState.Walk;
//                     else
//                         targetAnimation = AnimationState.Idle;
//                 }
//                 break;

//             case DanceState.Beginning:
//                 // DanceIdle as idle, but normal moves
//                 if (isJumping)
//                     targetAnimation = AnimationState.Jump;
//                 else if (!Mathf.Approximately(horizontalInput, 0f))
//                     targetAnimation = AnimationState.Walk;
//                 else
//                     targetAnimation = AnimationState.DancePose;
//                 break;

//             case DanceState.Dancing:
//                 moveSpeed = 0f;
//                 jumpForce = 0f;
//                 if (isJumping)
//                     targetAnimation = AnimationState.DanceJump;
//                 else if (!Mathf.Approximately(horizontalInput, 0f))
//                     targetAnimation = AnimationState.DanceWalk;
//                 else
//                     targetAnimation = AnimationState.DanceIdle;
//                 break;
//         }

//         if (targetAnimation != currentAnimation)
//         {
//             string animation = targetAnimation.ToString().Replace("_", "");
//             PlayerAnimator.Instance?.PlayAnimation(animation);
//             currentAnimation = targetAnimation;
//         }
//     }

//     private bool IsPartnerInDistance()
//     {
//         float distance = Vector2.Distance(transform.position, partnerTransform.position);
//         return distance <= 1.0f;
//     }
// }
