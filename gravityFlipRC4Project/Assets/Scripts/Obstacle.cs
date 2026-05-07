using UnityEngine;

/// <summary>
/// Obstacle — attach to any hazard GameObject.
/// When the player touches it, one life is removed via the GameManager.
///
/// SETUP:
///   1. Add this component to your obstacle GameObject.
///   2. Make sure the obstacle has a Collider (3D).
///   3. Check "Is Trigger" on the Collider for overlap detection,
///      OR leave unchecked for solid physics collision — both work.
///   4. Make sure your Player GameObject has the tag "Player".
/// </summary>
public class Obstacle : MonoBehaviour
{
    [Header("Obstacle Settings")]
    [Tooltip("Play a hit effect at the collision point (optional).")]
    [SerializeField] private GameObject hitEffectPrefab;

    [Tooltip("Seconds to wait before destroying the hit effect.")]
    [SerializeField] private float hitEffectLifetime = 1f;

    [Tooltip("If true, the obstacle is destroyed after hitting the player (e.g. a one-shot spike).")]
    [SerializeField] private bool destroyOnHit = false;

    // ─── Trigger-based detection (Is Trigger = ON) ────────────────────────────

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        HandleHit(other.transform.position);
    }

    // ─── Collision-based detection (Is Trigger = OFF) ─────────────────────────

    private void OnCollisionEnter(Collision other)
    {
        if (!other.gameObject.CompareTag("Player")) return;
        HandleHit(other.contacts[0].point);
    }

    // ─── Shared Logic ─────────────────────────────────────────────────────────

    private void HandleHit(Vector3 hitPoint)
    {
        // Guard: only react during active gameplay
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        // Spawn optional hit effect
        if (hitEffectPrefab != null)
        {
            GameObject fx = Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
            Destroy(fx, hitEffectLifetime);
        }

        // Tell the GameManager — it handles life loss, respawn, game over
        GameManager.Instance.HitObstacle();

        if (destroyOnHit)
            Destroy(gameObject);
    }
}
