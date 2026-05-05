using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// GameManager for Gravity Flip.
/// Single source of truth for game state, lives, score, and difficulty.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────

    public static GameManager Instance { get; private set; }

    // ─── Inspector ────────────────────────────────────────────────────────────

    [Header("Player Reference")]
    [SerializeField] private PlayerMovement player;

    [Header("Lives Settings")]
    [SerializeField] private int startingLives = 3;
    [SerializeField] private float respawnDelay = 1.5f;
    [SerializeField] private float invincibilityDuration = 2f;   // seconds of immunity after hit
    [SerializeField] private Transform respawnPoint;

    [Header("Score Settings")]
    [SerializeField] private int pointsPerFlip     = 10;
    [SerializeField] private int pointsPerLanding  = 5;
    [Tooltip("Points awarded per unit of distance travelled forward.")]
    [SerializeField] private float pointsPerMeter  = 1f;

    [Header("Difficulty / Speed Ramp")]
    [Tooltip("Starting forward speed passed to PlayerMovement.")]
    [SerializeField] private float startSpeed      = 8f;
    [Tooltip("Maximum forward speed the game ramps up to.")]
    [SerializeField] private float maxSpeed        = 20f;
    [Tooltip("How many units of distance before speed reaches max.")]
    [SerializeField] private float speedRampDistance = 500f;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuScene  = "MainMenu";
    [SerializeField] private string gameOverScene  = "GameOver";

    // ─── State ────────────────────────────────────────────────────────────────

    public enum GameState { Playing, Paused, Invincible, Dead, Win, GameOver }

    private GameState currentState  = GameState.Playing;
    private int   score             = 0;
    private int   lives;
    private int   flipCount         = 0;
    private float distanceTravelled = 0f;
    private float startZ;

    // Events — subscribe from UI scripts, no coupling needed
    public System.Action<GameState> OnStateChanged;
    public System.Action<int>       OnScoreChanged;
    public System.Action<int>       OnLivesChanged;
    public System.Action<int>       OnFlipCountChanged;
    public System.Action<float>     OnDistanceChanged;
    public System.Action            OnPlayerHit;       // hook up screen flash, shake, etc.

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        lives = startingLives;

        if (player == null)
            player = FindFirstObjectByType<PlayerMovement>();

        if (player != null)
        {
            startZ = player.transform.position.z;
            player.SetForwardSpeed(startSpeed);

            player.OnGravityFlipped += HandleGravityFlipped;
            player.OnLanded         += HandleLanding;
        }

        SetState(GameState.Playing);
    }

    private void OnDestroy()
    {
        if (player == null) return;
        player.OnGravityFlipped -= HandleGravityFlipped;
        player.OnLanded         -= HandleLanding;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();

        if (currentState == GameState.Playing || currentState == GameState.Invincible)
        {
            TrackDistance();
            RampSpeed();
        }
    }

    // ─── State Machine ────────────────────────────────────────────────────────

    private void SetState(GameState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                player?.SetMovementEnabled(true);
                break;

            case GameState.Invincible:
                // Player keeps moving — just immune to further obstacle hits
                Time.timeScale = 1f;
                player?.SetMovementEnabled(true);
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                player?.SetMovementEnabled(false);
                break;

            case GameState.Dead:
                player?.SetMovementEnabled(false);
                StartCoroutine(HandleDeathSequence());
                break;

            case GameState.Win:
                Time.timeScale = 0f;
                player?.SetMovementEnabled(false);
                break;

            case GameState.GameOver:
                player?.SetMovementEnabled(false);
                StartCoroutine(LoadGameOverScreen());
                break;
        }

        OnStateChanged?.Invoke(newState);
        Debug.Log($"[GameManager] State -> {newState}");
    }

    // ─── Public Game Actions ──────────────────────────────────────────────────

    /// <summary>
    /// Called by Obstacle.cs when the player touches a hazard.
    /// Removes a life and grants invincibility frames, or triggers Game Over.
    /// </summary>
    public void HitObstacle()
    {
        if (currentState != GameState.Playing) return;  // ignore if invincible, paused, etc.

        LoseLife();
        OnPlayerHit?.Invoke();

        if (lives <= 0)
            SetState(GameState.GameOver);
        else
            StartCoroutine(InvincibilityFrames());
    }

    /// <summary>
    /// Called by a WinTrigger script when the player reaches the goal zone.
    /// </summary>
    public void TriggerWin()
    {
        if (currentState != GameState.Playing && currentState != GameState.Invincible) return;
        SetState(GameState.Win);
    }

    /// <summary>
    /// Instant kill — use for falling off the level, death zones, etc.
    /// Unlike HitObstacle, this skips invincibility frames.
    /// </summary>
    public void KillPlayer()
    {
        if (currentState == GameState.Dead || currentState == GameState.GameOver) return;
        SetState(GameState.Dead);
    }

    public void TogglePause()
    {
        if (currentState == GameState.Playing || currentState == GameState.Invincible)
            SetState(GameState.Paused);
        else if (currentState == GameState.Paused)
            SetState(GameState.Playing);
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
    }

    public void LoadNextLevel()
    {
        Time.timeScale = 1f;
        int next = SceneManager.GetActiveScene().buildIndex + 1;
        if (next < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(next);
        else
            SetState(GameState.Win);
    }

    // ─── Distance & Speed Ramp ────────────────────────────────────────────────

    private void TrackDistance()
    {
        if (player == null) return;
        distanceTravelled = Mathf.Max(0f, player.transform.position.z - startZ);
        AddScore(Mathf.RoundToInt(Time.deltaTime * pointsPerMeter));
        OnDistanceChanged?.Invoke(distanceTravelled);
    }

    private void RampSpeed()
    {
        if (player == null || speedRampDistance <= 0f) return;
        float t = Mathf.Clamp01(distanceTravelled / speedRampDistance);
        player.SetForwardSpeed(Mathf.Lerp(startSpeed, maxSpeed, t));
    }

    // ─── Score & Lives ────────────────────────────────────────────────────────

    private void AddScore(int amount)
    {
        score += amount;
        OnScoreChanged?.Invoke(score);
    }

    private void LoseLife()
    {
        lives = Mathf.Max(0, lives - 1);
        OnLivesChanged?.Invoke(lives);
        Debug.Log($"[GameManager] Life lost. Lives remaining: {lives}");
    }

    // ─── Player Event Handlers ────────────────────────────────────────────────

    private void HandleGravityFlipped()
    {
        flipCount++;
        AddScore(pointsPerFlip);
        OnFlipCountChanged?.Invoke(flipCount);
    }

    private void HandleLanding()
    {
        AddScore(pointsPerLanding);
    }

    // ─── Coroutines ───────────────────────────────────────────────────────────

    private IEnumerator InvincibilityFrames()
    {
        SetState(GameState.Invincible);
        Debug.Log($"[GameManager] Invincible for {invincibilityDuration}s");
        yield return new WaitForSeconds(invincibilityDuration);
        if (currentState == GameState.Invincible)
            SetState(GameState.Playing);
    }

    private IEnumerator HandleDeathSequence()
    {
        LoseLife();
        yield return new WaitForSeconds(respawnDelay);
        if (lives <= 0)
            SetState(GameState.GameOver);
        else
        {
            RespawnPlayer();
            SetState(GameState.Playing);
        }
    }

    private void RespawnPlayer()
    {
        if (player == null) return;
        Vector3 pos = respawnPoint != null ? respawnPoint.position : Vector3.zero;
        player.transform.position = pos;

        // Reset gravity flip visual
        Vector3 scale = player.transform.localScale;
        scale.y = Mathf.Abs(scale.y);
        player.transform.localScale = scale;
    }

    private IEnumerator LoadGameOverScreen()
    {
        yield return new WaitForSeconds(2f);
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(gameOverScene))
            SceneManager.LoadScene(gameOverScene);
    }

    // ─── Public Getters ───────────────────────────────────────────────────────

    public GameState CurrentState  => currentState;
    public int   Score             => score;
    public int   Lives             => lives;
    public int   FlipCount         => flipCount;
    public float DistanceTravelled => distanceTravelled;
    public bool  IsInvincible      => currentState == GameState.Invincible;
}
