using UnityEngine;

/// <summary>
/// MovingObstacle — attach alongside Obstacle.cs to make hazards move.
/// Supports ping-pong (side to side / up and down) and rotation.
/// Great for laser walls, rotating barriers, sliding spikes etc.
/// </summary>
public class MovingObstacle : MonoBehaviour
{
    public enum MoveAxis { X, Y, Z }

    [Header("Ping-Pong Movement")]
    [SerializeField] private bool   enableMovement = true;
    [SerializeField] private MoveAxis moveAxis     = MoveAxis.X;
    [SerializeField] private float  moveDistance   = 3f;    // units either side of start pos
    [SerializeField] private float  moveSpeed      = 2f;

    [Header("Rotation")]
    [SerializeField] private bool   enableRotation = false;
    [SerializeField] private Vector3 rotationAxis  = Vector3.forward;
    [SerializeField] private float  rotationSpeed  = 90f;   // degrees per second

    // ─── Private ──────────────────────────────────────────────────────────────

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        if (GameManager.Instance?.CurrentState == GameManager.GameState.Paused) return;

        if (enableMovement) HandleMovement();
        if (enableRotation) HandleRotation();
    }

    private void HandleMovement()
    {
        float offset = Mathf.Sin(Time.time * moveSpeed) * moveDistance;
        Vector3 pos = startPosition;

        switch (moveAxis)
        {
            case MoveAxis.X: pos.x += offset; break;
            case MoveAxis.Y: pos.y += offset; break;
            case MoveAxis.Z: pos.z += offset; break;
        }

        transform.position = pos;
    }

    private void HandleRotation()
    {
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
    }
}
