using UnityEngine;
using System.Collections;

/// <summary>
/// PlayerInvincibilityFlash — attach to the Player GameObject.
/// Flashes the player's renderer(s) during invincibility frames
/// so the player gets clear visual feedback they can't be hit.
/// </summary>
public class PlayerInvincibilityFlash : MonoBehaviour
{
    [Header("Flash Settings")]
    [SerializeField] private float flashInterval = 0.1f;   // seconds between on/off
    [SerializeField] private Color flashColor    = new Color(1f, 0.4f, 0f, 1f); // orange tint

    // Cached references
    private Renderer[] renderers;
    private Color[]    originalColors;
    private Coroutine  flashCoroutine;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i].material.color;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += HandleStateChange;
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleStateChange;
    }

    private void HandleStateChange(GameManager.GameState state)
    {
        if (state == GameManager.GameState.Invincible)
        {
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(FlashRoutine());
        }
        else
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
            }
            ResetColors();
        }
    }

    private IEnumerator FlashRoutine()
    {
        bool showFlash = false;
        while (true)
        {
            showFlash = !showFlash;
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].material.color = showFlash ? flashColor : originalColors[i];

            yield return new WaitForSeconds(flashInterval);
        }
    }

    private void ResetColors()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].material.color = originalColors[i];
        }
    }
}
