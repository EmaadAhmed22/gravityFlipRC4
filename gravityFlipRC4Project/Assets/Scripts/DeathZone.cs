using UnityEngine;

/// <summary>
/// DeathZone — attach to any trigger collider that should instantly kill the player.
/// Use cases: side walls, kill planes above/below the tunnel, void zones.
///
/// SETUP:
///   1. Create an empty GameObject, add a Box Collider (or any 3D collider).
///   2. Check "Is Trigger" on the collider.
///   3. Add this script.
///   4. Make sure your Player has the tag "Player".
/// </summary>
public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (GameManager.Instance == null) return;

        GameManager.Instance.KillPlayer();
    }
}
