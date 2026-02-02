using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    //================================ Fields =================================//
    //Moving
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 3f;
    private Vector2 moveInput;

    //Components
    private Rigidbody2D rb;

    //Controlls
    private bool isMovementEnabled = true;

    //================================ Unity Methods =================================//
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    //================================ Custom Methods =================================//
    public void SetMoveInput(Vector2 input) => moveInput = input;

    public void EnableMovement(bool enabled)
    {
        isMovementEnabled = enabled;
        if (!enabled)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public bool HasMoveInput(float deadzone = 0.05f) => moveInput.sqrMagnitude > deadzone * deadzone;

    public void MovePlayer()
    {
        if (!isMovementEnabled) return;
        float x = moveInput.x * moveSpeed;
        rb.linearVelocity = new Vector2(x, rb.linearVelocity.y);
    }


}