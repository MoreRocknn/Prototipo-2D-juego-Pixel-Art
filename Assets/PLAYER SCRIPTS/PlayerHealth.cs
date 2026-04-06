using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerState))]
public class PlayerHealth : MonoBehaviour
{
    private Animator anim;
    private bool hasDied = false;

    [Header("Vida")]
    [Tooltip("Vida máxima del jugador")]
    public int maxHealth = 3;

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

    [Header("Muerte")]
    [Tooltip("Segundos a esperar antes de mostrar la pantalla de muerte (duración animación)")]
    public float deathAnimationTime = 1.2f;

    private PlayerState state;
    private Rigidbody2D rb;
    private PlayerCombat combat;
    private SpriteRenderer spriteRenderer;
    private HealthBarUI healthBar;

    void Awake()
    {
        anim = GetComponent<Animator>();
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

    public void TakeDamage(int damage)
    {
        if (hasDied) return;
        if (state.isDamageInvincible)
        {
            Debug.Log("Invencible, daño ignorado");
            return;
        }

        currentHealth = Mathf.Max(currentHealth - damage, 0);
        Debug.Log($"Daño recibido: {damage} → Vida: {currentHealth}/{maxHealth}");

        HitImpactSystem.Instance?.OnPlayerHit(GetComponent<SpriteRenderer>());

        healthBar?.UpdateHealth(currentHealth, maxHealth);
        combat?.ResetCombo();
        ApplyKnockback();

        if (currentHealth <= 0)
            Die();
        else
            StartCoroutine(DamageInvincibility());
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        healthBar?.UpdateHealth(currentHealth, maxHealth);
        Debug.Log($"Curado: +{amount} → Vida: {currentHealth}/{maxHealth}");
    }

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

    private void Die()
    {
        if (hasDied) return;
        hasDied = true;

        StopAllCoroutines();

        state.isAttacking = false;
        state.isDashing = false;
        state.isInputLocked = false;
        state.isDamageInvincible = false;

        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;

        StartCoroutine(PlayDeathSequence());
    }

    private IEnumerator PlayDeathSequence()
    {
        yield return null;

        if (anim != null)
            anim.SetTrigger("isDead");

        yield return new WaitForSeconds(deathAnimationTime);

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

    private IEnumerator RespawnAfterDeath()
    {
        yield return new WaitUntil(() => !DeathScreen.Instance.IsShowing);

        if (spriteRenderer != null) spriteRenderer.enabled = false;

        currentHealth = maxHealth;
        state.currentComboStep = 0;
        state.isDamageInvincible = false;

        if (GameManager.Instance != null)
            transform.position = GameManager.Instance.GetRespawnPosition();

        // Resetear el boss DESPUÉS de reposicionar al jugador,
        // así el boss no detecta al jugador en rango al resetearse
        BossController boss = FindFirstObjectByType<BossController>();
        if (boss != null) boss.ResetState();

        anim.Rebind();
        anim.Update(0f);

        if (spriteRenderer != null) spriteRenderer.enabled = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Dynamic;
        enabled = true;

        healthBar?.UpdateHealth(currentHealth, maxHealth);
        healthBar?.ForceShow();

        CameraManager.instance?.RespawnToCamera(transform.position);

        hasDied = false;
    }
}