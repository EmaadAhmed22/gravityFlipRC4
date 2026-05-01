using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// GameManager for Gravity Flip.
/// Manages game state, score, lives, level loading, and UI coordination.
/// Place on a persistent GameObject in your scene (or use DontDestroyOnLoad).
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─── Singleton ────────────────────────────────────────────────────────────

    public static GameManager Instance { get; private set; }

    // ─── Inspector ────────────────────────────────────────────────────────────

    [Header("Player Reference")]
    [SerializeField] private PlayerMovement player;

    [Header("Game Settings")]
    [SerializeField] private int startingLives = 3;
    [SerializeField] private float deathRespawnDelay = 1.5f;
    [SerializeField] private Transform respawnPoint;

    [Header("Score Settings")]
    [SerializeField] private int pointsPerFlip = 10;
    [SerializeField] private int pointsPerLanding = 5;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string gameOverScene  = "GameOver";

    // ─── State ────────────────────────────────────────────────────────────────
    
    public enum GameState { Playing, Paused, Dead, Win, GameOver }

    private GameState currentState = GameState.Playing;
    private int score = 0;
    private int lives;
    private int flipCount = 0;

    // Events — subscribe from UI scripts
    public System.Action<GameState> OnStateChanged;
    public System.Action<int>       OnScoreChanged;
    public System.Action<int>       OnLivesChanged;
    public System.Action<int>       OnFlipCountChanged;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Comment out the line below if you DON'T want the manager to persist between scenes:
        // DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        lives = startingLives;

        if (player == null)
            player = FindFirstObjectByType<PlayerMovement>();

        if (player != null)
        {
            player.OnGravityFlipped += HandleGravityFlipped;
            player.OnLanded         += HandleLanding;
        }

        SetState(GameState.Playing);
    }

    private void OnDestroy()
    {
        if (player != null)
        {
            player.OnGravityFlipped -= HandleGravityFlipped;
            player.OnLanded         -= HandleLanding;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
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
        Debug.Log($"[GameManager] State → {newState}");
    }

    // ─── Public Game Actions ──────────────────────────────────────────────────

    public void TogglePause()
    {
        if (currentState == GameState.Playing)
            SetState(GameState.Paused);
        else if (currentState == GameState.Paused)
            SetState(GameState.Playing);
    }

    /// <summary>Call from a trigger/collider when the player hits a hazard.</summary>
    public void KillPlayer()
    {
        if (currentState != GameState.Playing) return;
        SetState(GameState.Dead);
    }

    /// <summary>Call from a trigger/collider when the player reaches the goal.</summary>
    public void TriggerWin()
    {
        if (currentState != GameState.Playing) return;
        SetState(GameState.Win);
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
            SetState(GameState.Win); // No more levels — handle credits/end screen
    }

    // ─── Score & Stats ────────────────────────────────────────────────────────

    private void AddScore(int amount)
    {
        score += amount;
        OnScoreChanged?.Invoke(score);
    }

    private void LoseLife()
    {
        lives = Mathf.Max(0, lives - 1);
        OnLivesChanged?.Invoke(lives);
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

    private IEnumerator HandleDeathSequence()
    {
        LoseLife();

        yield return new WaitForSeconds(deathRespawnDelay);

        if (lives <= 0)
        {
            SetState(GameState.GameOver);
        }
        else
        {
            RespawnPlayer();
            SetState(GameState.Playing);
        }
    }

    private void RespawnPlayer()
    {
        if (player == null) return;

        Vector3 spawnPos = respawnPoint != null ? respawnPoint.position : Vector3.zero;
        player.transform.position = spawnPos;

        // Reset player scale in case gravity was flipped at death
        Vector3 scale = player.transform.localScale;
        scale.y = Mathf.Abs(scale.y);
        player.transform.localScale = scale;
    }

    private IEnumerator LoadGameOverScreen()
    {
        yield return new WaitForSeconds(2f);
        Time.timeScale = 1f;

        if (!string.IsNullOrEmpty(gameOverScene) &&
            Application.CanStreamedLevelBeLoaded(gameOverScene))
        {
            SceneManager.LoadScene(gameOverScene);
        }
    }

    // ─── Public Getters ───────────────────────────────────────────────────────

    public GameState CurrentState => currentState;
    public int Score              => score;
    public int Lives              => lives;
    public int FlipCount          => flipCount;
}
