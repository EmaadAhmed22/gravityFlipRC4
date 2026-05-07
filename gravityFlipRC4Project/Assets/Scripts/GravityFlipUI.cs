using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// GravityFlipUI — attach to a UIManager GameObject in your scene.
/// Subscribes to GameManager events and updates all HUD panels.
///
/// Requires: TextMeshPro (install via Package Manager if missing).
/// </summary>
public class GravityFlipUI : MonoBehaviour
{
    // ─── HUD (shown during gameplay) ──────────────────────────────────────────

    [Header("HUD")]
    [SerializeField] private GameObject     hudPanel;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI distanceText;
    [SerializeField] private TextMeshProUGUI livesText;

    [Tooltip("Individual heart icons. Assign in order: heart1, heart2, heart3...")]
    [SerializeField] private Image[]        heartIcons;
    [SerializeField] private Sprite        heartFull;
    [SerializeField] private Sprite        heartEmpty;

    [Header("Gravity Flip Cooldown")]
    [SerializeField] private Image         flipCooldownFill;   // Image type = Filled, Method = Radial360
    [SerializeField] private TextMeshProUGUI flipCooldownText;

    // ─── Hit Flash ────────────────────────────────────────────────────────────

    [Header("Hit Flash")]
    [SerializeField] private Image         hitFlashOverlay;    // full-screen red Image, alpha=0 normally
    [SerializeField] private float         flashDuration = 0.3f;
    [SerializeField] private Color         flashColor    = new Color(1f, 0f, 0f, 0.35f);

    // ─── Invincibility Indicator ──────────────────────────────────────────────

    [Header("Invincibility")]
    [SerializeField] private GameObject    invincibilityIndicator; // e.g. a glowing border

    // ─── Pause Panel ──────────────────────────────────────────────────────────

    [Header("Pause Panel")]
    [SerializeField] private GameObject    pausePanel;
    [SerializeField] private Button        resumeButton;
    [SerializeField] private Button        pauseRestartButton;
    [SerializeField] private Button        pauseMainMenuButton;

    // ─── Game Over Panel ──────────────────────────────────────────────────────

    [Header("Game Over Panel")]
    [SerializeField] private GameObject    gameOverPanel;
    [SerializeField] private TextMeshProUGUI gameOverScoreText;
    [SerializeField] private TextMeshProUGUI gameOverDistanceText;
    [SerializeField] private Button        gameOverRestartButton;
    [SerializeField] private Button        gameOverMainMenuButton;

    // ─── Win Panel ────────────────────────────────────────────────────────────

    [Header("Win Panel")]
    [SerializeField] private GameObject    winPanel;
    [SerializeField] private TextMeshProUGUI winScoreText;
    [SerializeField] private Button        nextLevelButton;
    [SerializeField] private Button        winMainMenuButton;

    // ─── Private ──────────────────────────────────────────────────────────────

    private PlayerMovement player;
    private float flipCooldown = 0.3f;   // must match PlayerMovement.gravityFlipCooldown
    private float lastFlipTime = -999f;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Start()
    {
        // Subscribe to GameManager events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged    += UpdateScore;
            GameManager.Instance.OnLivesChanged    += UpdateLives;
            GameManager.Instance.OnDistanceChanged += UpdateDistance;
            GameManager.Instance.OnStateChanged    += HandleStateChange;
            GameManager.Instance.OnPlayerHit       += TriggerHitFlash;

            // Seed initial values
            UpdateScore(GameManager.Instance.Score);
            UpdateLives(GameManager.Instance.Lives);
        }

        // Wire up buttons
        resumeButton?.onClick.AddListener(() => GameManager.Instance?.TogglePause());
        pauseRestartButton?.onClick.AddListener(() => GameManager.Instance?.RestartGame());
        pauseMainMenuButton?.onClick.AddListener(() => GameManager.Instance?.LoadMainMenu());
        gameOverRestartButton?.onClick.AddListener(() => GameManager.Instance?.RestartGame());
        gameOverMainMenuButton?.onClick.AddListener(() => GameManager.Instance?.LoadMainMenu());
        nextLevelButton?.onClick.AddListener(() => GameManager.Instance?.LoadNextLevel());
        winMainMenuButton?.onClick.AddListener(() => GameManager.Instance?.LoadMainMenu());

        // Initial panel state
        ShowOnlyPanel(hudPanel);

        if (hitFlashOverlay != null)
            hitFlashOverlay.color = Color.clear;

        if (invincibilityIndicator != null)
            invincibilityIndicator.SetActive(false);

        // Find player for cooldown tracking
        player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
            player.OnGravityFlipped += HandleFlipForCooldown;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnScoreChanged    -= UpdateScore;
            GameManager.Instance.OnLivesChanged    -= UpdateLives;
            GameManager.Instance.OnDistanceChanged -= UpdateDistance;
            GameManager.Instance.OnStateChanged    -= HandleStateChange;
            GameManager.Instance.OnPlayerHit       -= TriggerHitFlash;
        }

        if (player != null)
            player.OnGravityFlipped -= HandleFlipForCooldown;
    }

    private void Update()
    {
        UpdateFlipCooldownUI();
    }

    // ─── HUD Updates ──────────────────────────────────────────────────────────

    private void UpdateScore(int newScore)
    {
        if (scoreText != null)
            scoreText.text = $"Score: {newScore:N0}";
    }

    private void UpdateDistance(float meters)
    {
        if (distanceText != null)
            distanceText.text = $"{meters:F0}m";
    }

    private void UpdateLives(int remaining)
    {
        // Plain text fallback
        if (livesText != null)
            livesText.text = $"Lives: {remaining}";

        // Heart icon approach — whichever you use in your UI
        if (heartIcons != null)
        {
            for (int i = 0; i < heartIcons.Length; i++)
            {
                if (heartIcons[i] == null) continue;
                bool alive = i < remaining;
                heartIcons[i].sprite = alive ? heartFull : heartEmpty;
                heartIcons[i].color  = alive ? Color.white : new Color(1f, 1f, 1f, 0.3f);
            }
        }
    }

    // ─── Flip Cooldown Bar ────────────────────────────────────────────────────

    private void HandleFlipForCooldown()
    {
        lastFlipTime = Time.time;
    }

    private void UpdateFlipCooldownUI()
    {
        if (flipCooldownFill == null) return;

        float elapsed  = Time.time - lastFlipTime;
        float progress = Mathf.Clamp01(elapsed / flipCooldown);

        flipCooldownFill.fillAmount = progress;

        if (flipCooldownText != null)
            flipCooldownText.text = progress >= 1f ? "READY" : "";
    }

    // ─── Hit Flash ────────────────────────────────────────────────────────────

    private void TriggerHitFlash()
    {
        if (hitFlashOverlay != null)
            StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            float t = elapsed / flashDuration;
            // Fade in then out
            float alpha = flashColor.a * (1f - t);
            hitFlashOverlay.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }
        hitFlashOverlay.color = Color.clear;
    }

    // ─── State Changes ────────────────────────────────────────────────────────

    private void HandleStateChange(GameManager.GameState newState)
    {
        // Invincibility indicator
        if (invincibilityIndicator != null)
            invincibilityIndicator.SetActive(newState == GameManager.GameState.Invincible);

        switch (newState)
        {
            case GameManager.GameState.Playing:
            case GameManager.GameState.Invincible:
                ShowOnlyPanel(hudPanel);
                break;

            case GameManager.GameState.Paused:
                ShowOnlyPanel(pausePanel);
                break;

            case GameManager.GameState.GameOver:
                ShowOnlyPanel(gameOverPanel);
                if (gameOverScoreText != null)
                    gameOverScoreText.text = $"Score: {GameManager.Instance.Score:N0}";
                if (gameOverDistanceText != null)
                    gameOverDistanceText.text = $"Distance: {GameManager.Instance.DistanceTravelled:F0}m";
                break;

            case GameManager.GameState.Win:
                ShowOnlyPanel(winPanel);
                if (winScoreText != null)
                    winScoreText.text = $"Score: {GameManager.Instance.Score:N0}";
                break;
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Hides all panels then shows only the one you pass in.</summary>
    private void ShowOnlyPanel(GameObject panelToShow)
    {
        if (hudPanel      != null) hudPanel.SetActive(false);
        if (pausePanel    != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (winPanel      != null) winPanel.SetActive(false);

        if (panelToShow != null)
            panelToShow.SetActive(true);
    }
}
