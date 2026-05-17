using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ProceduralLevelGenerator — attach to an empty GameObject called "LevelGenerator".
/// Spawns pre-built track segments ahead of the player and despawns them behind.
/// Each segment prefab should be a self-contained tunnel/platform chunk with
/// obstacles already placed inside it.
/// </summary>
public class ProceduralLevelGenerator : MonoBehaviour
{
    [Header("Segment Prefabs")]
    [Tooltip("Your track segment prefabs. Mix of safe and obstacle-filled chunks.")]
    [SerializeField] private GameObject[] segmentPrefabs;

    [Tooltip("A guaranteed safe/empty segment — always used at the very start.")]
    [SerializeField] private GameObject startSegment;

    [Header("Generation Settings")]
    [SerializeField] private int   segmentsAhead    = 5;   // how many to keep spawned ahead
    [SerializeField] private float segmentLength    = 30f; // must match your prefab length on Z
    [SerializeField] private int   safeSegmentsAtStart = 3; // obstacle-free grace period

    [Header("References")]
    [SerializeField] private Transform player;

    // ─── Private ──────────────────────────────────────────────────────────────

    private Queue<GameObject> activeSegments = new Queue<GameObject>();
    private float nextSpawnZ = 0f;
    private int   totalSpawned = 0;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerMovement>()?.transform;

        // Spawn initial batch
        for (int i = 0; i < segmentsAhead + 2; i++)
            SpawnNextSegment();
    }

    private void Update()
    {
        if (player == null) return;
        if (GameManager.Instance?.CurrentState == GameManager.GameState.GameOver) return;
        if (GameManager.Instance?.CurrentState == GameManager.GameState.Win) return;

        // Spawn new segments as player moves forward
        while (nextSpawnZ < player.position.z + segmentsAhead * segmentLength)
            SpawnNextSegment();

        // Despawn segments the player has passed
        while (activeSegments.Count > 0)
        {
            GameObject oldest = activeSegments.Peek();
            if (oldest.transform.position.z + segmentLength < player.position.z - segmentLength)
            {
                activeSegments.Dequeue();
                Destroy(oldest);
            }
            else break;
        }
    }

    // ─── Spawn ────────────────────────────────────────────────────────────────

    private void SpawnNextSegment()
    {
        GameObject prefab = ChooseSegment();
        GameObject segment = Instantiate(prefab, new Vector3(0f, 0f, nextSpawnZ), Quaternion.identity);
        activeSegments.Enqueue(segment);
        nextSpawnZ += segmentLength;
        totalSpawned++;
    }

    private GameObject ChooseSegment()
    {
        // Grace period — use safe start segment
        if (totalSpawned < safeSegmentsAtStart && startSegment != null)
            return startSegment;

        // Pick a random segment from the pool
        if (segmentPrefabs == null || segmentPrefabs.Length == 0)
        {
            Debug.LogWarning("[LevelGenerator] No segment prefabs assigned!");
            return startSegment;
        }

        return segmentPrefabs[Random.Range(0, segmentPrefabs.Length)];
    }
}
