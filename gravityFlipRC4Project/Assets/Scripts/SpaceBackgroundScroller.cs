using UnityEngine;

/// <summary>
/// SpaceBackgroundScroller — attach to your space skybox/background quad or
/// particle system to create a parallax scrolling effect as the player moves.
///
/// Option A (Particle System): Attach to a World Space particle system
///   set to emit stars/dust — it will auto-follow the player.
///
/// Option B (Scrolling Material): Attach to a quad/plane with a space texture
///   and it will offset the UV over time.
/// </summary>
public class SpaceBackgroundScroller : MonoBehaviour
{
    public enum ScrollMode { FollowPlayer, ScrollMaterial }

    [Header("Mode")]
    [SerializeField] private ScrollMode mode = ScrollMode.FollowPlayer;

    [Header("Follow Player Mode")]
    [SerializeField] private Transform player;
    [Tooltip("Only follow on these axes (e.g. keep Y fixed so background doesn't bob).")]
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followY = false;
    [SerializeField] private bool followZ = true;
    [SerializeField] private float followSpeed = 0.1f;   // parallax lag (0=locked, 1=same speed)

    [Header("Scroll Material Mode")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Vector2 scrollSpeed = new Vector2(0f, -0.02f);

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerMovement>()?.transform;
    }

    private void LateUpdate()
    {
        switch (mode)
        {
            case ScrollMode.FollowPlayer:
                FollowPlayerParallax();
                break;
            case ScrollMode.ScrollMaterial:
                ScrollMaterialUV();
                break;
        }
    }

    // ─── Modes ────────────────────────────────────────────────────────────────

    private void FollowPlayerParallax()
    {
        if (player == null) return;

        Vector3 target = transform.position;
        if (followX) target.x = Mathf.Lerp(transform.position.x, player.position.x, followSpeed);
        if (followY) target.y = Mathf.Lerp(transform.position.y, player.position.y, followSpeed);
        if (followZ) target.z = Mathf.Lerp(transform.position.z, player.position.z, followSpeed);

        transform.position = target;
    }

    private void ScrollMaterialUV()
    {
        if (targetRenderer == null) return;

        Vector2 offset = targetRenderer.material.mainTextureOffset;
        offset += scrollSpeed * Time.deltaTime;
        targetRenderer.material.mainTextureOffset = offset;
    }
}
