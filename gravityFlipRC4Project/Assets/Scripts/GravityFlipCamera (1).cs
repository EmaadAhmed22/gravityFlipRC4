using UnityEngine;

/// <summary>
/// GravityFlipCamera — attach this to the Main Camera (child of Player prefab).
/// Smoothly rotates and repositions the camera when gravity flips,
/// and follows the player with configurable offset + smoothing.
/// </summary>
public class GravityFlipCamera : MonoBehaviour
{
    [Header("Follow Settings")]
    [SerializeField] private float followSmoothing = 8f;

    [Header("Offset Settings")]
    [Tooltip("Camera offset when gravity is normal (player standing on floor).")]
    [SerializeField] private Vector3 normalOffset = new Vector3(0f, 3f, -8f);
    [Tooltip("Camera offset when gravity is flipped (player on ceiling).")]
    [SerializeField] private Vector3 flippedOffset = new Vector3(0f, -3f, -8f);

    [Header("Flip Animation")]
    [SerializeField] private float flipRotationDuration = 0.35f;
    [SerializeField] private AnimationCurve flipCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Look Settings")]
    [Tooltip("How far ahead of the player the camera looks (in the movement direction).")]
    [SerializeField] private float lookAheadDistance = 2f;
    [SerializeField] private float lookAheadSmoothing = 5f;

    // ─── Private State ────────────────────────────────────────────────────────

    private PlayerMovement playerMovement;
    private Transform playerTransform;

    private Vector3 currentOffset;
    private Vector3 targetOffset;

    private Quaternion currentRotation;
    private Quaternion targetRotation;

    private float flipProgress = 1f;   // 1 = flip complete
    private float flipTimer    = 0f;
    private bool  isFlipping   = false;

    private Vector3 lookAheadTarget;
    private Vector3 smoothedLookAhead;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Detach from player so we can control position ourselves
        // (keeps prefab hierarchy clean but gives us full control)
        playerTransform = transform.parent;
        transform.SetParent(null);

        playerMovement = playerTransform.GetComponent<PlayerMovement>();

        currentOffset   = normalOffset;
        targetOffset    = normalOffset;
        currentRotation = Quaternion.Euler(10f, 0f, 0f);  // slight downward tilt
        targetRotation  = currentRotation;

        transform.position = playerTransform.position + currentOffset;
        transform.rotation = currentRotation;
    }

    private void OnEnable()
    {
        if (playerMovement != null)
            playerMovement.OnGravityFlipped += HandleGravityFlipped;
    }

    private void OnDisable()
    {
        if (playerMovement != null)
            playerMovement.OnGravityFlipped -= HandleGravityFlipped;
    }

    private void LateUpdate()
    {
        if (playerTransform == null) return;

        UpdateFlipAnimation();
        UpdateLookAhead();
        ApplyCameraTransform();
    }

    // ─── Gravity Flip Handler ─────────────────────────────────────────────────

    private void HandleGravityFlipped()
    {
        bool flipped = playerMovement.IsGravityFlipped;

        targetOffset   = flipped ? flippedOffset : normalOffset;
        targetRotation = flipped
            ? Quaternion.Euler(-10f, 0f, 0f)   // tilt up when on ceiling
            : Quaternion.Euler( 10f, 0f, 0f);  // tilt down when on floor

        // Kick off the flip animation from current state
        flipTimer    = 0f;
        flipProgress = 0f;
        isFlipping   = true;
    }

    // ─── Update Helpers ───────────────────────────────────────────────────────

    private void UpdateFlipAnimation()
    {
        if (!isFlipping) return;

        flipTimer    += Time.deltaTime;
        flipProgress  = Mathf.Clamp01(flipTimer / flipRotationDuration);

        float t = flipCurve.Evaluate(flipProgress);

        currentOffset   = Vector3.Lerp(currentOffset,   targetOffset,   t);
        currentRotation = Quaternion.Slerp(currentRotation, targetRotation, t);

        if (flipProgress >= 1f)
            isFlipping = false;
    }

    private void UpdateLookAhead()
    {
        // Peek ahead based on the player's horizontal velocity
        Rigidbody rb = playerTransform.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            if (horizontalVel.sqrMagnitude > 0.1f)
                lookAheadTarget = horizontalVel.normalized * lookAheadDistance;
            else
                lookAheadTarget = Vector3.zero;
        }

        smoothedLookAhead = Vector3.Lerp(smoothedLookAhead, lookAheadTarget,
                                         lookAheadSmoothing * Time.deltaTime);
    }

    private void ApplyCameraTransform()
    {
        // Target world position = player pos + offset + look-ahead nudge
        Vector3 desiredPos = playerTransform.position + currentOffset + smoothedLookAhead;

        transform.position = Vector3.Lerp(transform.position, desiredPos,
                                          followSmoothing * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, currentRotation,
                                              followSmoothing * Time.deltaTime);
    }

    // ─── Editor Helpers ───────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(playerTransform.position + normalOffset,  0.2f);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(playerTransform.position + flippedOffset, 0.2f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(playerTransform.position, transform.position);
    }
#endif
}
