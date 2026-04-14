using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerState))]
public class PlayerHealth : MonoBehaviour
{
    private Animator anim;
    private bool hasDied = false;

    [Header("Vida")]
    public int maxHealth = 3;
    public int currentHealth;

    [Header("Daño e invencibilidad")]
    public float damageInvincibilityTime = 1f;
    public Vector2 damageKnockbackForce = new Vector2(5f, 5f);
    public Color damageColor = new Color(1f, 0.3f, 0.3f);

    [Header("Barra de vida")]
    public bool showHealthBar = true;
    public Vector3 healthBarOffset = new Vector3(0f, 1.2f, 0f);

    [Header("Muerte")]
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

    // ── Fix 3: suscripción al evento de pantalla negra ────────────────────
    void OnEnable()
    {
        DeathScreen.OnPantallaNegraCompleta += ResetearEnemigos;
    }

    void OnDisable()
    {
        DeathScreen.OnPantallaNegraCompleta -= ResetearEnemigos;
    }

    // Se ejecuta mientras la pantalla está en negro → sin pop-in visible
    void ResetearEnemigos()
    {
        // EnemyManager ya hace RespawnAllEnemies, pero lo llamábamos
        // en PlayDeathSequence ANTES del fade → pop-in visible.
        // Ahora se llama aquí, tapado por el negro.
        EnemyManager.Instance?.RespawnAllEnemies();
    }

    private void SpawnHealthBar()
    {
        if (!showHealthBar) return;
        if (HealthBarFactory.Instance == null) { Debug.LogWarning("[PlayerHealth] No hay HealthBarFactory en la escena."); return; }
        healthBar = HealthBarFactory.Instance.CreateHealthBar(transform, currentHealth, maxHealth, healthBarOffset);
        healthBar.alwaysShow = true;
        healthBar.ForceShow();
    }

    public void TakeDamage(int damage)
    {
        if (hasDied) return;
        if (state.isDamageInvincible) { Debug.Log("Invencible, daño ignorado"); return; }

        currentHealth = Mathf.Max(currentHealth - damage, 0);
        Debug.Log($"Daño recibido: {damage} → Vida: {currentHealth}/{maxHealth}");

        HitImpactSystem.Instance?.OnPlayerHit(GetComponent<SpriteRenderer>());
        healthBar?.UpdateHealth(currentHealth, maxHealth);
        combat?.ResetCombo();
        ApplyKnockback();

        if (currentHealth <= 0) Die();
        else StartCoroutine(DamageInvincibility());
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
            if (dist < closest) { closest = dist; knockbackDir = transform.position.x > enemy.transform.position.x ? 1f : -1f; }
        }
        rb.linearVelocity = new Vector2(knockbackDir * damageKnockbackForce.x, damageKnockbackForce.y);
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

        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;

        StartCoroutine(PlayDeathSequence());
    }

    private IEnumerator PlayDeathSequence()
    {
        yield return null;
        if (anim != null) anim.SetTrigger("isDead");
        yield return new WaitForSeconds(deathAnimationTime);

        // ── Fix 3: EnemyManager.RespawnAllEnemies() se movió a ResetearEnemigos()
        //    que se llama desde el evento OnPantallaNegraCompleta, tapado por el negro.
        //    Aquí solo lanzamos las cosas que no tienen pop-in visual.
        AbilityAbsorptionManager.Instance?.OnPlayerDeath();
        GetComponent<HealingSystem>()?.OnPlayerDeath();
        GetComponent<BloodPoolTransform>()?.ResetUses();

        DeathScreen.Instance?.Show(); // dispara el fade → al terminar lanza OnPantallaNegraCompleta → ResetearEnemigos

        if (GameManager.Instance != null)
            StartCoroutine(RespawnAfterDeath());
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    private IEnumerator RespawnAfterDeath()
    {
        yield return new WaitUntil(() => !DeathScreen.Instance.IsShowing);

        // Ocultar al jugador mientras reposicionamos
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        currentHealth = maxHealth;
        state.currentComboStep = 0;
        state.isDamageInvincible = false;

        // Mover al jugador al checkpoint
        Vector3 respawnPos = GameManager.Instance != null
            ? GameManager.Instance.GetRespawnPosition()
            : transform.position;
        transform.position = respawnPos;

        // ── Fix 2: cámara TP instantáneo DESPUÉS de mover al jugador ─────────
        // RespawnToCamera activa la cámara guardada en el último checkpoint
        // y hace un cut (sin blend) para que no se vea el viaje de cámara.
        CameraManager.instance?.RespawnToCamera(respawnPos);

        anim.Rebind();
        anim.Update(0f);

        if (spriteRenderer != null) spriteRenderer.enabled = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Dynamic;
        enabled = true;

        healthBar?.UpdateHealth(currentHealth, maxHealth);
        healthBar?.ForceShow();

        hasDied = false;
    }
}