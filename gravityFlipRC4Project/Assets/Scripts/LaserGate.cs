using UnityEngine;

/// <summary>
/// LaserGate — a sci-fi laser beam that pulses on and off on a timer.
/// When active (beam visible), touching it removes a life.
/// When inactive, the player can pass through safely.
///
/// HIERARCHY SETUP:
///   LaserGate (empty GO — attach this script + Obstacle.cs here)
///     ├── EmitterTop    (Cylinder, scale ~0.3,0.3,0.3, emissive red material)
///     ├── EmitterBottom (Cylinder, same material)
///     └── Beam          (Cube, scale (0.08, 6, 0.15), emissive red, Is Trigger ON)
///                        ^ The beam bridges EmitterTop to EmitterBottom vertically
///
/// The script enables/disables the Beam child and its collider on a pulse cycle.
/// Obstacle.cs on the parent handles the life-loss when the beam hits the player.
///
/// MATERIAL TIP (URP):
///   Create a Material → Lit shader → set Base Color to red
///   Enable Emission → set Emission Color to bright red (HDR, intensity ~3)
///   This makes it glow without needing a light source.
/// </summary>
[RequireComponent(typeof(Obstacle))]
public class LaserGate : MonoBehaviour
{
    [Header("Pulse Timing")]
    [Tooltip("Seconds the laser beam is ON (dangerous).")]
    [SerializeField] private float activeTime   = 1.2f;
    [Tooltip("Seconds the laser beam is OFF (safe to pass).")]
    [SerializeField] private float inactiveTime = 0.8f;
    [Tooltip("Offset so not all lasers in a segment pulse in sync.")]
    [SerializeField] private float phaseOffset  = 0f;

    [Header("References")]
    [Tooltip("The Beam child GameObject (the long thin cube).")]
    [SerializeField] private GameObject beam;
    [Tooltip("The two emitter node GameObjects (top and bottom cylinders).")]
    [SerializeField] private Renderer[] emitterRenderers;

    [Header("Visual")]
    [SerializeField] private Color activeColor   = new Color(1f, 0.1f, 0.1f);   // red
    [SerializeField] private Color inactiveColor = new Color(0.2f, 0.2f, 0.2f); // dark grey
    [Tooltip("Emission intensity when active (HDR).")]
    [SerializeField] private float activeEmission   = 3f;
    [SerializeField] private float inactiveEmission  = 0f;

    // ─── Private ──────────────────────────────────────────────────────────────

    private Collider beamCollider;
    private Renderer beamRenderer;
    private bool     isActive = true;
    private float    timer    = 0f;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Start()
    {
        if (beam != null)
        {
            beamCollider = beam.GetComponent<Collider>();
            beamRenderer = beam.GetComponent<Renderer>();
        }

        // Apply phase offset so lasers in a row don't all pulse together
        timer = phaseOffset % (activeTime + inactiveTime);

        SetBeamState(true);
    }

    private void Update()
    {
        if (GameManager.Instance?.CurrentState == GameManager.GameState.Paused) return;

        timer += Time.deltaTime;

        float cycleLength = activeTime + inactiveTime;
        float cyclePos    = timer % cycleLength;

        bool shouldBeActive = cyclePos < activeTime;

        if (shouldBeActive != isActive)
            SetBeamState(shouldBeActive);
    }

    // ─── State ────────────────────────────────────────────────────────────────

    private void SetBeamState(bool active)
    {
        isActive = active;

        // Toggle beam visibility and collision
        if (beam != null)
            beam.SetActive(active);

        // Shift emitter node color to show charge state
        if (emitterRenderers != null)
        {
            foreach (Renderer r in emitterRenderers)
            {
                if (r == null) continue;
                Color col     = active ? activeColor   : inactiveColor;
                float emission = active ? activeEmission : inactiveEmission;
                r.material.color = col;
                r.material.SetColor(EmissionColor, col * Mathf.Pow(2f, emission));
            }
        }

        // Beam renderer tint
        if (beamRenderer != null)
        {
            beamRenderer.material.SetColor(EmissionColor,
                activeColor * Mathf.Pow(2f, activeEmission));
        }
    }

    // ─── Editor Gizmo ─────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(0.1f, 6f, 0.15f));
    }
}
