using System.Collections;
using UnityEngine;

/// <summary>
/// Gestiona el sistema de combos tipo Blasphemous:
/// combo de 3 golpes, ataques laterales/abajo, efectos visuales y camera shake.
/// </summary>
[RequireComponent(typeof(PlayerState))]
public class PlayerCombat : MonoBehaviour
{
    [Header("Sistema de Combos")]
    public int combo1Damage = 1;
    public int combo2Damage = 1;
    public int combo3Damage = 3;
    public float comboResetTime = 1.0f;
    public float attackAnimationDuration = 0.3f;

    [Header("Ataque lateral")]
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public LayerMask enemyLayer;
    public LayerMask groundLayer;
    public float playerKnockbackForce = 3f;

    [Header("Efectos de Combo")]
    public GameObject sideAttackEffect;
    public GameObject combo1Effect;
    public GameObject combo2Effect;
    public GameObject combo3Effect;
    public Color combo1Color = Color.white;
    public Color combo2Color = new Color(1f, 0.8f, 0f);
    public Color combo3Color = new Color(1f, 0f, 0f);

    [Header("Camera Shake - Combo 3")]
    public bool enableCombo3Shake = true;
    [Range(0.05f, 0.5f)] public float combo3ShakeDuration = 0.25f;
    [Range(0.1f, 2f)] public float combo3ShakeMagnitude = 0.5f;
    [Range(10f, 50f)] public float combo3ShakeFrequency = 30f;
    public bool useImpactShake = false;

    [Header("Down Attack")]
    public bool enableDownAttack = false;
    public Transform downAttackPoint;
    public float downAttackBounceForce = 25f;
    public float downAttackSmallBounceForce = 12f;

    [Header("Límite de Rebotes")]
    public int maxConsecutiveBounces = 3;
    public float bounceResetTime = 0.5f;

    [Header("Detección de suelo")]
    public GroundCheck groundCheck;

    private PlayerState state;
    private Rigidbody2D rb;

    void Awake()
    {
        state = GetComponent<PlayerState>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        HandleAttackInput();
    }

    private void HandleAttackInput()
    {
        if (state.isInputLocked || state.isDashing) return;

        float verticalInput = (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f)
                            - (Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);

        if (Input.GetKeyDown(KeyCode.X) && !state.isAttacking)
        {
            state.isAttackingDown = enableDownAttack
                                    && verticalInput < 0
                                    && !groundCheck.isGrounded;
            StartCoroutine(PerformComboAttack());
        }
    }

    /// <summary>Llamado desde PlayerCore.Update()</summary>
    public void HandleComboReset()
    {
        if (Time.time - state.lastAttackTime > comboResetTime && state.currentComboStep > 0)
        {
            state.currentComboStep = 0;
            Debug.Log("Combo reseteado");
        }
    }

    public void ResetCombo()
    {
        state.currentComboStep = 0;
    }

    /// <summary>Llamado desde PlayerCore.Update()</summary>
    public void HandleAbilityInput()
    {
        var abilityHolder = GetComponent<AbilityHolder>();
        if (Input.GetKeyDown(KeyCode.Q) && abilityHolder != null)
            abilityHolder.UseAbility();
    }

    /// <summary>Llamado desde PlayerCore.Update()</summary>
    public void HandleBounceReset()
    {
        if (Time.time - state.lastBounceTime > bounceResetTime && !groundCheck.isGrounded)
            state.consecutiveBounces = 0;
    }

    private IEnumerator PerformComboAttack()
    {
        state.isAttacking = true;
        state.lastAttackTime = Time.time;

        if (groundCheck.isGrounded)
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        int damage = 0;
        GameObject effectPrefab = null;
        Color flashColor = Color.white;
        float knockbackMultiplier = 1f;

        switch (state.currentComboStep)
        {
            case 0:
                damage = combo1Damage;
                effectPrefab = combo1Effect ?? sideAttackEffect;
                flashColor = combo1Color;
                knockbackMultiplier = 1f;
                Debug.Log("⚔️ COMBO 1 - Golpe ligero");
                break;
            case 1:
                damage = combo2Damage;
                effectPrefab = combo2Effect ?? sideAttackEffect;
                flashColor = combo2Color;
                knockbackMultiplier = 1.2f;
                Debug.Log("⚔️⚔️ COMBO 2 - Golpe medio");
                break;
            case 2:
                damage = combo3Damage;
                effectPrefab = combo3Effect ?? sideAttackEffect;
                flashColor = combo3Color;
                knockbackMultiplier = 2f;
                Debug.Log("💥⚔️⚔️⚔️ COMBO 3 - GOLPE FINAL!");

                if (enableCombo3Shake && CameraShake.Instance != null)
                {
                    if (useImpactShake)
                        CameraShake.Instance.ShakeImpact(combo3ShakeMagnitude);
                    else
                        CameraShake.Instance.Shake(combo3ShakeDuration, combo3ShakeMagnitude, combo3ShakeFrequency);
                }
                break;
        }

        // Flash de color
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color originalColor = sr != null ? sr.color : Color.white;
        if (sr != null) sr.color = flashColor;

        // Instanciar efecto
        SpawnAttackEffect(effectPrefab);

        // Knockback al jugador en aire
        if (!state.isAttackingDown && !groundCheck.isGrounded)
        {
            float knockDir = state.isFacingRight ? -1 : 1;
            float knockAmount = playerKnockbackForce * (state.currentComboStep == 2 ? 0.3f : 0.5f);
            rb.AddForce(new Vector2(knockDir * knockAmount, 0), ForceMode2D.Impulse);
        }

        // Ejecutar ataque
        if (state.isAttackingDown && enableDownAttack)
            HandleDownAttack();
        else
            HandleSideAttack(damage, knockbackMultiplier);

        // Avanzar combo
        state.currentComboStep++;
        if (state.currentComboStep > 2) state.currentComboStep = 0;

        yield return new WaitForSeconds(attackAnimationDuration);

        if (sr != null) sr.color = originalColor;
        state.isAttacking = false;
    }

    private void SpawnAttackEffect(GameObject prefab)
    {
        if (prefab == null) return;

        Vector3 pos = attackPoint != null ? attackPoint.position : transform.position;
        GameObject instance = Instantiate(prefab, pos, Quaternion.identity);

        Vector3 scale = instance.transform.localScale;
        scale.x *= state.isFacingRight ? 1 : -1;
        instance.transform.localScale = scale;

        ParticleSystem ps = instance.GetComponent<ParticleSystem>();
        if (ps != null) ps.Play();

        Destroy(instance, attackAnimationDuration + 0.5f);
    }

    private void HandleSideAttack(int damage, float knockbackMultiplier)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
        foreach (Collider2D col in hits)
        {
            int dir = state.isFacingRight ? 1 : -1;
            if (TryDealDamage(col, dir, damage, knockbackMultiplier))
            {
                string comboText = state.currentComboStep == 0
                    ? "💥 GOLPE FINAL" : $"Golpe {state.currentComboStep}";
                Debug.Log($"{comboText} - Golpeó a: {col.name} ({damage} daño)");
            }
        }
    }

    private void HandleDownAttack()
    {
        if (downAttackPoint == null) return;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(downAttackPoint.position, attackRange, enemyLayer);
        Collider2D[] hitGround = Physics2D.OverlapCircleAll(downAttackPoint.position, attackRange, groundLayer);

        bool hitSomething = false;

        foreach (Collider2D col in hitEnemies)
        {
            int dir = state.isFacingRight ? 1 : -1;
            if (TryDealDamage(col, dir, combo1Damage))
                hitSomething = true;
        }

        if (hitGround.Length > 0)
        {
            hitSomething = true;
            Debug.Log("¡Pegaste al suelo!");
        }

        if (hitSomething)
        {
            if (state.consecutiveBounces < maxConsecutiveBounces)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, downAttackBounceForce);
                state.consecutiveBounces++;
                state.lastBounceTime = Time.time;
            }
            else
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, downAttackBounceForce * 0.3f);
            }
        }
    }

    private bool TryDealDamage(Collider2D col, int knockbackDir, int damage, float knockbackMultiplier = 1f)
    {
        var enemy = col.GetComponent<Enemigo>();
        if (enemy != null) { enemy.TakeDamage(damage, knockbackDir); return true; }

        var flyingEnemy = col.GetComponent<EnemigoVoladorHealth>();
        if (flyingEnemy != null) { flyingEnemy.TakeDamage(damage, knockbackDir); return true; }

        var boss = col.GetComponent<BossController>();
        if (boss != null) { boss.TakeDamage(damage, knockbackDir); return true; }

        return false;
    }

    void OnDrawGizmos()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
        if (enableDownAttack && downAttackPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(downAttackPoint.position, attackRange);
        }
    }
}