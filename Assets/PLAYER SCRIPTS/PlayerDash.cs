using System.Collections;
using UnityEngine;

/// <summary>
/// Gestiona el dash del jugador: impulso, ghost trail y trail effect.
/// Implementa IDashExecutor para integrarse con el AbilityHolder.
/// </summary>
[RequireComponent(typeof(PlayerState))]
public class PlayerDash : MonoBehaviour, IDashExecutor
{
    [Header("Efectos de Dash")]
    public GameObject dashTrailEffect;
    public Color dashColor = new Color(0.3f, 0.8f, 1f);
    public bool showGhostTrail = true;
    public float ghostTrailFrequency = 0.05f;

    private PlayerState state;
    private Rigidbody2D rb;
    private PlayerGravity gravityModule;

    void Awake()
    {
        state         = GetComponent<PlayerState>();
        rb            = GetComponent<Rigidbody2D>();
        gravityModule = GetComponent<PlayerGravity>();
    }

    /// <summary>Punto de entrada para el sistema de habilidades (IDashExecutor)</summary>
    public void PerformDash(float force, float duration)
    {
        if (!state.isDashing)
            StartCoroutine(DashCoroutine(force, duration));
    }

    private IEnumerator DashCoroutine(float force, float duration)
    {
        state.isDashing = true;

        float dashDirection = state.isFacingRight ? 1f : -1f;
        if (Mathf.Abs(state.moveInput) > 0.1f)
            dashDirection = Mathf.Sign(state.moveInput);

        rb.linearVelocity = new Vector2(dashDirection * force, 0f);
        rb.gravityScale   = 0f;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color originalColor = sr != null ? sr.color : Color.white;
        if (sr != null) sr.color = dashColor;

        if (dashTrailEffect != null)
        {
            GameObject trail = Instantiate(dashTrailEffect, transform.position, Quaternion.identity);
            Destroy(trail, duration + 0.5f);
        }

        if (showGhostTrail)
            StartCoroutine(SpawnGhostTrail(duration, sr));

        float elapsed = 0f;
        while (elapsed < duration)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        state.isDashing = false;
        rb.gravityScale = gravityModule != null
            ? gravityModule.GetDefaultGravityScale()
            : 1f;

        if (sr != null) sr.color = originalColor;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, 0f);
    }

    private IEnumerator SpawnGhostTrail(float duration, SpriteRenderer originalSr)
    {
        float elapsed = 0f;
        while (elapsed < duration && state.isDashing)
        {
            GameObject ghost = new GameObject("GhostTrail_Player");
            ghost.transform.position   = transform.position;
            ghost.transform.localScale = transform.localScale;
            ghost.transform.rotation   = transform.rotation;

            SpriteRenderer ghostSr = ghost.AddComponent<SpriteRenderer>();
            ghostSr.sprite       = originalSr.sprite;
            ghostSr.color        = new Color(dashColor.r, dashColor.g, dashColor.b, 0.5f);
            ghostSr.sortingOrder = originalSr.sortingOrder - 1;

            Destroy(ghost, 0.3f);

            yield return new WaitForSeconds(ghostTrailFrequency);
            elapsed += ghostTrailFrequency;
        }
    }
}
