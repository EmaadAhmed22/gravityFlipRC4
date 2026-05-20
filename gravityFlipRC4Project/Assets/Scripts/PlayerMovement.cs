using UnityEngine;

/// <summary>
/// Handles player movement and gravity flipping for Gravity Flip.
/// Attach this to the Player GameObject alongside a Rigidbody and Collider.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float deceleration = 10f;
    [Tooltip("Constant forward speed (Z+). Player steers left/right with A/D only.")]
    [SerializeField] private float autoForwardSpeed = 8f;

    [Header("Gravity Settings")]
    [SerializeField] private float gravityStrength = 20f;
    [SerializeField] private float gravityFlipCooldown = 0.3f;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private Transform groundCheckPoint;

    // State
    private Rigidbody rb;
    private bool isGravityFlipped = false;
    private bool isGrounded = false;
    private float lastFlipTime = -999f;

    // Events for GameManager / UI
    public System.Action OnGravityFlipped;
    public System.Action OnLanded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;         // We apply custom gravity
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void Update()
    {
        HandleGravityFlipInput();
        CheckGrounded();
    }

    private void FixedUpdate()
    {
        ApplyCustomGravity();
        HandleMovement();
    }

    // ─── Input ────────────────────────────────────────────────────────────────

    private void HandleGravityFlipInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryFlipGravity();
        }
    }

    // ─── Gravity ──────────────────────────────────────────────────────────────

    private void TryFlipGravity()
    {
        if (Time.time - lastFlipTime < gravityFlipCooldown) return;

        isGravityFlipped = !isGravityFlipped;
        lastFlipTime = Time.time;

        // Kill vertical momentum then add a kick away from the surface
        Vector3 vel = rb.linearVelocity;
        vel.y = 0f;
        rb.linearVelocity = vel;

        // Flip the player visually
        Vector3 scale = transform.localScale;
        scale.y *= -1f;
        transform.localScale = scale;

        OnGravityFlipped?.Invoke();
    }

    private void ApplyCustomGravity()
    {
        float gravityDir = isGravityFlipped ? 1f : -1f;
        rb.AddForce(new Vector3(0f, gravityDir * gravityStrength, 0f), ForceMode.Acceleration);
    }

    // ─── Movement ─────────────────────────────────────────────────────────────

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");

        // Auto-forward: Z velocity is always locked to autoForwardSpeed.
        // Player can only steer left/right with A/D.
        float targetX = horizontal * moveSpeed;
        float rate = Mathf.Abs(horizontal) > 0.1f ? acceleration : deceleration;

        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetX, rate * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector3(newX, rb.linearVelocity.y, autoForwardSpeed);
    }

    // ─── Ground Check ─────────────────────────────────────────────────────────

    private void CheckGrounded()
    {
        Vector3 checkDir = isGravityFlipped ? Vector3.up : Vector3.down;
        Vector3 origin = groundCheckPoint != null ? groundCheckPoint.position : transform.position;

        bool wasGrounded = isGrounded;
        isGrounded = Physics.Raycast(origin, checkDir, groundCheckDistance + 0.05f, groundLayer);

        if (!wasGrounded && isGrounded)
        {
            OnLanded?.Invoke();
        }
    }

    // ─── Public Accessors ─────────────────────────────────────────────────────

    public bool IsGrounded => isGrounded;
    public bool IsGravityFlipped => isGravityFlipped;

    /// <summary>Called by GameManager to ramp difficulty over time.</summary>
    public void SetForwardSpeed(float speed) => autoForwardSpeed = speed;

    /// <summary>Called by GameManager to freeze the player (e.g. on death/win).</summary>
    public void SetMovementEnabled(bool enabled)
    {
        rb.isKinematic = !enabled;
        this.enabled = enabled;
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize ground check ray in Scene view
        Vector3 checkDir = isGravityFlipped ? Vector3.up : Vector3.down;
        Vector3 origin = groundCheckPoint != null ? groundCheckPoint.position : transform.position;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawRay(origin, checkDir * (groundCheckDistance + 0.05f));
    }
}
