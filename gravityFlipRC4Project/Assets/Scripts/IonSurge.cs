using UnityEngine;
using System.Collections;

/// <summary>
/// IonSurge — an energy wave that erupts from the floor (or ceiling) and sweeps
/// upward, filling most of the tunnel height. The player must flip gravity to
/// the opposite surface before the surge reaches them.
///
/// The surge fires on a repeating timer, travels along the Y axis, then resets.
///
/// HIERARCHY SETUP:
///   IonSurge (empty GO — attach this script + Obstacle.cs here)
///     └── SurgeVisual  (Cube, scale (5.5, 0.3, 0.8), emissive cyan/blue material)
///                       ^ Sits flat on the floor. This script moves it upward.
///
/// MATERIAL TIP (URP):
///   Base Color: dark cyan
///   Emission: bright cyan (HDR intensity ~4) — gives a plasma/electric feel
///   Rendering Mode: Transparent, Alpha ~0.75 for a semi-transparent energy look
///
/// COLLIDER:
///   The SurgeVisual should have a Box Collider with Is Trigger = ON
///   Obstacle.cs on the parent catches the trigger and calls HitObstacle().
/// </summary>
[RequireComponent(typeof(Obstacle))]
public class IonSurge : MonoBehaviour
{
    [Header("Surge Settings")]
    [Tooltip("Which surface the surge erupts from.")]
    [SerializeField] private SurgeOrigin origin = SurgeOrigin.Floor;

    [Tooltip("How far the surge travels from its start position (in units).")]
    [SerializeField] private float surgeDistance = 5f;

    [Tooltip("Speed the surge wall travels in units/sec.")]
    [SerializeField] private float surgeSpeed = 6f;

    [Tooltip("How long to pause at rest before firing again.")]
    [SerializeField] private float cooldownTime = 2.5f;

    [Tooltip("Warning flash duration before the surge fires.")]
    [SerializeField] private float warningTime = 0.5f;

    [Header("References")]
    [Tooltip("The SurgeVisual child (the flat cube that moves).")]
    [SerializeField] private GameObject surgeVisual;

    [Header("Visual")]
    [SerializeField] private Color surgeColor   = new Color(0f, 0.8f, 1f);  // cyan
    [SerializeField] private Color warningColor = new Color(1f, 0.6f, 0f);  // orange warning
    [SerializeField] private float surgeEmission   = 4f;
    [SerializeField] private float warningEmission = 2f;

    public enum SurgeOrigin { Floor, Ceiling }

    // ─── Private ──────────────────────────────────────────────────────────────

    private Renderer surgeRenderer;
    private Collider surgeCollider;
    private Vector3  restPosition;
    private Vector3  targetPosition;
    private bool     isSurging   = false;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Start()
    {
        if (surgeVisual != null)
        {
            surgeRenderer = surgeVisual.GetComponent<Renderer>();
            surgeCollider = surgeVisual.GetComponent<Collider>();
        }

        restPosition   = surgeVisual != null ? surgeVisual.transform.localPosition : Vector3.zero;
        float direction = origin == SurgeOrigin.Floor ? 1f : -1f;
        targetPosition  = restPosition + Vector3.up * surgeDistance * direction;

        // Start the surge loop
        StartCoroutine(SurgeCycle());
    }

    private void Update()
    {
        if (GameManager.Instance?.CurrentState == GameManager.GameState.Paused) return;
        if (!isSurging || surgeVisual == null) return;

        // Move surge toward target
        surgeVisual.transform.localPosition = Vector3.MoveTowards(
            surgeVisual.transform.localPosition,
            targetPosition,
            surgeSpeed * Time.deltaTime
        );
    }

    // ─── Surge Cycle ──────────────────────────────────────────────────────────

    private IEnumerator SurgeCycle()
    {
        while (true)
        {
            // Rest phase
            isSurging = false;
            if (surgeCollider != null) surgeCollider.enabled = false;
            ResetSurgePosition();
            SetColor(surgeColor, 1f);    // dim while resting

            yield return new WaitForSeconds(cooldownTime);

            // Warning flash
            yield return StartCoroutine(WarningShriek());

            // Fire
            isSurging = true;
            if (surgeCollider != null) surgeCollider.enabled = true;
            SetColor(surgeColor, surgeEmission);

            // Wait until surge reaches target
            while (surgeVisual != null &&
                   Vector3.Distance(surgeVisual.transform.localPosition, targetPosition) > 0.05f)
            {
                if (GameManager.Instance?.CurrentState != GameManager.GameState.Paused)
                    yield return null;
                else
                    yield return new WaitForSeconds(0.1f);
            }

            // Brief hold at full extension
            yield return new WaitForSeconds(0.2f);
        }
    }

    private IEnumerator WarningShriek()
    {
        float elapsed = 0f;
        bool  toggle  = false;

        while (elapsed < warningTime)
        {
            toggle = !toggle;
            SetColor(toggle ? warningColor : surgeColor,
                     toggle ? warningEmission : 0.5f);
            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private void ResetSurgePosition()
    {
        if (surgeVisual != null)
            surgeVisual.transform.localPosition = restPosition;
    }

    private void SetColor(Color col, float emissionIntensity)
    {
        if (surgeRenderer == null) return;
        surgeRenderer.material.color = col;
        surgeRenderer.material.SetColor(EmissionColor, col * Mathf.Pow(2f, emissionIntensity));
    }

    // ─── Editor Gizmo ─────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (surgeVisual == null) return;
        float dir = origin == SurgeOrigin.Floor ? 1f : -1f;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(
            surgeVisual.transform.position + Vector3.up * surgeDistance * dir * 0.5f,
            new Vector3(5.5f, surgeDistance, 0.8f)
        );
    }
}
