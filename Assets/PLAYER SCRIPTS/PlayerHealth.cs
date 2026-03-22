using System.Collections;
using UnityEngine;

// ============================================================
//  PLAYER HEALTH
//  Gestiona la vida del jugador:
//
//  - TakeDamage: recibir daño con knockback y parpadeo
//  - Invincibility frames: no puede recibir daño durante un tiempo
//  - Die / Respawn: morir y reaparecer desde el último checkpoint
//  - Heal: recuperar vida (por ejemplo al usar una poción)
//  - HealthBar: la barra de vida se crea automáticamente al iniciar
// ============================================================
[RequireComponent(typeof(PlayerState))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]
    [Tooltip("Vida máxima del jugador")]
    public int maxHealth = 3;

    // La vida actual es pública para poder verla en el Inspector durante el juego
    public int currentHealth;

    [Header("Daño e invencibilidad")]
    [Tooltip("Segundos de invencibilidad después de recibir daño")]
    public float damageInvincibilityTime = 1f;

    [Tooltip("Fuerza del empujón al recibir daño. X = horizontal, Y = vertical")]
    public Vector2 damageKnockbackForce = new Vector2(5f, 5f);

    [Tooltip("Color del parpadeo al recibir daño")]
    public Color damageColor = new Color(1f, 0.3f, 0.3f);

    [Header("Barra de vida")]
    [Tooltip("¿Mostrar barra de vida encima del jugador?")]
    public bool showHealthBar = true;

    [Tooltip("Posición de la barra relativa al jugador. Y positivo = encima")]
    public Vector3 healthBarOffset = new Vector3(0f, 1.2f, 0f);

    // Referencias internas
    private PlayerState state;
    private Rigidbody2D rb;
    private PlayerCombat combat;
    private SpriteRenderer spriteRenderer;
    private HealthBarUI healthBar;

    void Awake()
    {
        state = GetComponent<PlayerState>();
        rb = GetComponent<Rigidbody2D>();
        combat = GetComponent<PlayerCombat>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        currentHealth = maxHealth;
        SpawnHealthBar();
    }

    // ── Crear la barra de vida al iniciar ──────────────────────
    private void SpawnHealthBar()
    {
        if (!showHealthBar) return;

        if (HealthBarFactory.Instance == null)
        {
            Debug.LogWarning("[PlayerHealth] No hay HealthBarFactory en la escena.");
            return;
        }

        healthBar = HealthBarFactory.Instance.CreateHealthBar(
            transform, currentHealth, maxHealth, healthBarOffset
        );

        healthBar.alwaysShow = true;
        healthBar.ForceShow();
    }

    // ── Recibir daño ───────────────────────────────────────────
    public void TakeDamage(int damage)
    {
        if (state.isDamageInvincible)
        {
            Debug.Log("Invencible, daño ignorado");
            return;
        }

        currentHealth = Mathf.Max(currentHealth - damage, 0);
        Debug.Log($"Daño recibido: {damage} → Vida: {currentHealth}/{maxHealth}");

        // Game feel: hitstop + shake cuando el jugador recibe daño
        HitImpactSystem.Instance?.OnPlayerHit(GetComponent<SpriteRenderer>());

        healthBar?.UpdateHealth(currentHealth, maxHealth);
        combat?.ResetCombo();
        ApplyKnockback();

        if (currentHealth <= 0)
            Die();
        else
            StartCoroutine(DamageInvincibility());
    }

    // ── Curar al jugador ────────────────────────────────────────
    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        healthBar?.UpdateHealth(currentHealth, maxHealth);
        Debug.Log($"Curado: +{amount} → Vida: {currentHealth}/{maxHealth}");
    }

    // ── Knockback ───────────────────────────────────────────────
    private void ApplyKnockback()
    {
        float knockbackDir = 1f;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        float closest = Mathf.Infinity;
        foreach (GameObject enemy in enemies)
        {
            float dist = Vector2.Distance(transform.position, enemy.transform.position);
            if (dist < closest)
            {
                closest = dist;
                knockbackDir = transform.position.x > enemy.transform.position.x ? 1f : -1f;
            }
        }

        rb.linearVelocity = new Vector2(
            knockbackDir * damageKnockbackForce.x,
            damageKnockbackForce.y
        );
    }

    // ── Parpadeo de invencibilidad ──────────────────────────────
    private IEnumerator DamageInvincibility()
    {
        state.isDamageInvincible = true;

        Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        float interval = damageInvincibilityTime / 10f;

        for (int i = 0; i < 5; i++)
        {
            if (spriteRenderer != null) spriteRenderer.color = damageColor;
            yield return new WaitForSeconds(interval);
            if (spriteRenderer != null) spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(interval);
        }

        state.isDamageInvincible = false;
    }

    // ── Muerte ──────────────────────────────────────────────────
    private void Die()
    {
        Debug.Log("¡Jugador murió!");

        // Mostrar pantalla de muerte estilo Dark Souls
        DeathScreen.Instance?.Show();

        AbilityAbsorptionManager.Instance?.OnPlayerDeath();
        GetComponent<HealingSystem>()?.OnPlayerDeath();
        GetComponent<BloodPoolTransform>()?.ResetUses();
        EnemyManager.Instance?.RespawnAllEnemies();

        if (GameManager.Instance != null)
            StartCoroutine(RespawnAfterDeath());
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
    }

    // ── Respawn ─────────────────────────────────────────────────
    private IEnumerator RespawnAfterDeath()
    {
        enabled = false;
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        yield return new WaitForSeconds(1f);

        currentHealth = maxHealth;
        state.currentComboStep = 0;
        state.isDamageInvincible = false;

        if (GameManager.Instance != null)
            transform.position = GameManager.Instance.GetRespawnPosition();

        if (spriteRenderer != null) spriteRenderer.enabled = true;
        rb.linearVelocity = Vector2.zero;
        enabled = true;

        healthBar?.UpdateHealth(currentHealth, maxHealth);
        healthBar?.ForceShow();
    }
}