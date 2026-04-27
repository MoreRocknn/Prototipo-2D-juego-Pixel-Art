using System.Collections;
using UnityEngine;

public class Enemigo : MonoBehaviour, IAbsorbable, IDashExecutor, IResettable
{
    [Header("=== SALUD ===")]
    public int health = 3;
    public int maxHealth = 3;
    public float invincibilityTime = 0.5f;
    public Vector2 knockbackForce = new Vector2(3f, 5f);
    public bool isBoss = false;

    public bool IsBoss => isBoss;


    [Header("=== RETRASO Y SUAVIZADO ===")]
    [Tooltip("Tiempo que tarda en reaccionar tras ver al jugador")]
    public float reactionDelay = 0.5f;
    [Range(1f, 20f)]
    [Tooltip("Suavizado de aceleración (1 = pesado, 20 = instantáneo)")]
    public float movementSmoothing = 5f;
    private float reactionTimer = 0f;
    private float targetVelocityX = 0f;
    private float currentVelocityX = 0f;

    [Header("=== CONFIGURACIÓN BARRA & CRÍTICO ===")]
    public Vector3 healthBarOffset = new Vector3(0, 1.2f, 0);
    public bool hideHealthBarWhenFull = true;
    public int criticalHealthThreshold = 1;
    public float criticalStunDuration = 3f;

    [Header("=== INMORTALIDAD CON DASH ===")]
    public bool immortalWhenCriticalWithDash = true;
    private bool isImmortalForAbsorption = false;

    [Header("=== DETECCIÓN & HABILIDAD ===")]
    public float detectionRange = 8f;
    public float attackRange = 2f;
    public LayerMask PlayerLayer, wallLayer, groundLayer;
    public Transform detectionPoint, edgeCheckPoint;
    public bool startsWithAbility = true;
    public AbilityType startingAbility = AbilityType.Dash;

    [Header("=== ENEMIGO DE ELITE (DASH) ===")]
    public bool isEliteWithDash = false;
    public bool disableAbilityIfPlayerHasDash = true;
    private bool abilityDisabledByPlayerProgress = false;

    [Header("=== DASH AGRESIVO ===")]
    public float dashForce = 20f;
    public float dashDuration = 0.35f;
    public float dashCooldownTime = 2f;
    public float dashMinDistance = 3f;
    public float dashMaxDistance = 10f;
    public float dashPredictionTime = 0.2f;
    public Color dashColor = new Color(0.3f, 0.8f, 1f);
    public bool showDashTrail = true;
    private float dashCooldownTimer = 0f;
    private bool isDashingToPlayer = false;

    [Header("=== COMPORTAMIENTO ===")]
    public float moveSpeed = 3f;
    public float chaseSpeed = 5f;
    public float guardTime = 0.8f;
    public float attackCooldown = 1.5f;
    public float chaseTimeout = 8f;
    public float extendedChaseRange = 15f;
    public float edgeWaitTime = 3f;
    public float maxEdgeWaitTime = 10f;

    [Header("=== ATAQUE & PATRULLA ===")]
    public Transform attackPoint;
    public float attackRadius = 1f;
    public int attackDamage = 1;
    public float attackDuration = 0.3f;
    public bool shouldPatrol = true;
    public float patrolDistance = 5f;
    public float waitTimeAtPatrolPoint = 2f;

    [Header("=== ANIMACIONES ===")]
    public string animSpeed = "speed";
    public string animIsGrounded = "isGrounded";
    public string animIsGuarding = "isGuarding";
    public string animIsAttacking = "isAttacking";
    public string animIsDashing = "isDashing";
    public string animIsCritical = "isCritical";
    public string animIsStunned = "isStunned";
    public string animAttackTrigger = "attack";

    [Header("=== VISUALES ===")]
    public GameObject guardEffect, attackEffect;
    public Color guardColor = Color.yellow, attackColor = Color.red;
    public Color immortalColor = new Color(1f, 0.84f, 0f);

    [Header("=== SANGRE ===")]
    [Tooltip("Prefab con el componente BloodEffect")]
    public GameObject bloodEffectPrefab;
    [Tooltip("Offset respecto al centro del enemigo donde aparece la sangre")]
    public Vector3 bloodOffset = new Vector3(0f, 0.3f, 0f);

    private EnemyState currentState = EnemyState.Idle;
    private enum EnemyState { Idle, Patrol, Guard, Chase, Attack, Stunned, Dashing, WaitingAbsorption }

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Animator anim;
    private Transform player;
    private HealthBarUI healthBar;
    private AbilityHolder abilityHolder;

    private Color originalColor;
    private Vector2 startPos;
    private bool isFacingRight = true, movingRight = true, isAtEdge;
    private bool isInvincible, isCritical, isCriticalStunned, isAttacking, isDashing;
    private float guardTimer, attackTimer, waitTimer, chaseTimer, edgeWaitTimer, totalEdgeTime, playerIgnoreTimer;
    private bool hasSeenPlayer = false;
    private Rigidbody2D playerRb;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        abilityHolder = gameObject.AddComponent<AbilityHolder>();

        if (sr) originalColor = sr.color;
        startPos = transform.position;
        health = maxHealth;

        CheckAndSetupAbility();

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player) playerRb = player.GetComponent<Rigidbody2D>();

        if (!detectionPoint) detectionPoint = transform;
        if (!edgeCheckPoint) CreateEdgeCheck();

        if (AbilityAbsorptionManager.Instance != null)
            AbilityAbsorptionManager.Instance.RegisterResettable(this);

        SetupHealthBar();
        currentState = shouldPatrol ? EnemyState.Patrol : EnemyState.Idle;
    }

    void ApplyDelayedMovement()
    {
        currentVelocityX = Mathf.Lerp(currentVelocityX, targetVelocityX, movementSmoothing * Time.deltaTime);
        rb.linearVelocity = new Vector2(currentVelocityX, rb.linearVelocity.y);
    }

    void CheckAndSetupAbility()
    {
        if (isEliteWithDash && disableAbilityIfPlayerHasDash)
        {
            if (GameManager.Instance != null && GameManager.Instance.HasPermanentDash())
            {
                abilityDisabledByPlayerProgress = true;
                return;
            }
        }

        if (startsWithAbility)
        {
            DashAbility enemyDash = new DashAbility();
            enemyDash.limitedUses = false;
            abilityHolder.SetAbility(enemyDash);
        }
    }

    void OnDestroy()
    {
        if (AbilityAbsorptionManager.Instance != null)
            AbilityAbsorptionManager.Instance.UnregisterResettable(this);
    }

    public void ResetState()
    {
        gameObject.SetActive(true);
        health = maxHealth;
        transform.position = startPos;
        currentState = shouldPatrol ? EnemyState.Patrol : EnemyState.Idle;

        if (sr) sr.color = originalColor;
        isCritical = false;
        isCriticalStunned = false;
        isInvincible = false;
        isDashing = false;
        isDashingToPlayer = false;
        hasSeenPlayer = false;
        isImmortalForAbsorption = false;
        isAttacking = false;

        if (isEliteWithDash && disableAbilityIfPlayerHasDash)
        {
            if (GameManager.Instance != null && GameManager.Instance.HasPermanentDash())
            {
                abilityDisabledByPlayerProgress = true;
                if (abilityHolder != null) abilityHolder.RemoveAbility();
            }
            else
            {
                abilityDisabledByPlayerProgress = false;
                if (startsWithAbility && abilityHolder != null)
                {
                    DashAbility d = new DashAbility();
                    d.limitedUses = false;
                    abilityHolder.SetAbility(d);
                }
            }
        }

        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(true);
            healthBar.ResetVisibility();
        }

        ResetTimers();
        rb.linearVelocity = Vector2.zero;
    }

    void SetupHealthBar()
    {
        if (HealthBarFactory.Instance)
            healthBar = HealthBarFactory.Instance.CreateHealthBar(transform, health, maxHealth, healthBarOffset);
    }

    void Update()
    {
        if (isInvincible || currentState == EnemyState.Stunned || isCriticalStunned)
        {
            if (isCriticalStunned) rb.linearVelocity = Vector2.zero;
            UpdateAnimations();
            return;
        }

        if (isDashing) { UpdateAnimations(); return; }

        if (currentState == EnemyState.WaitingAbsorption)
        {
            rb.linearVelocity = Vector2.zero;
            if (player) LookAtPlayer();
            UpdateAnimations();
            return;
        }

        UpdateTimers();
        CheckCriticalHealth();
        CheckEdge();

        bool seePlayer = CanSeePlayer();
        float distToPlayer = player != null ? Vector2.Distance(detectionPoint.position, player.position) : Mathf.Infinity;

        if (seePlayer) hasSeenPlayer = true;

        bool detected = (seePlayer && playerIgnoreTimer <= 0) ||
                        (hasSeenPlayer && distToPlayer <= extendedChaseRange && playerIgnoreTimer <= 0);
        bool inRange = seePlayer && distToPlayer <= attackRange;

        if (currentState == EnemyState.Chase && detected && !abilityDisabledByPlayerProgress &&
            abilityHolder != null && abilityHolder.HasAbility() && dashCooldownTimer <= 0)
        {
            if (distToPlayer >= dashMinDistance && distToPlayer <= dashMaxDistance && !isAtEdge)
            {
                isDashingToPlayer = true;
                abilityHolder.UseAbility();
            }
        }

        switch (currentState)
        {
            case EnemyState.Idle:
                SetVelocity(0);
                if (detected) EnterState(EnemyState.Guard);
                break;
            case EnemyState.Patrol:
                if (detected) EnterState(EnemyState.Guard);
                else HandlePatrol();
                break;
            case EnemyState.Guard:
                HandleGuard(detected, inRange);
                break;
            case EnemyState.Chase:
                HandleChase(detected, inRange);
                break;
            case EnemyState.Attack:
                if (!isAttacking) StartCoroutine(PerformAttack());
                break;
        }

        UpdateAnimations();
    }

    void UpdateAnimations()
    {
        if (anim == null) return;
        anim.SetFloat(animSpeed, Mathf.Abs(rb.linearVelocity.x));
        anim.SetBool(animIsGuarding, currentState == EnemyState.Guard);
        anim.SetBool(animIsAttacking, isAttacking);
        anim.SetBool(animIsDashing, isDashing);
        anim.SetBool(animIsCritical, isCritical);
        anim.SetBool(animIsStunned, isCriticalStunned);
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;
        SetVelocity(0);
        if (anim) anim.SetTrigger(animAttackTrigger);
        if (sr) sr.color = attackColor;

        yield return new WaitForSeconds(attackDuration * 0.5f);

        if (attackPoint != null)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, PlayerLayer);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                    hit.GetComponent<PlayerCore>()?.TakeDamage(attackDamage);
            }
        }

        yield return new WaitForSeconds(attackDuration * 0.5f);

        if (sr) sr.color = originalColor;
        isAttacking = false;
        attackTimer = attackCooldown;
        EnterState(EnemyState.Guard);
    }

    void HandlePatrol()
    {
        if (waitTimer > 0) { SetVelocity(0); return; }
        if (isAtEdge) { movingRight = !movingRight; waitTimer = waitTimeAtPatrolPoint; Flip(); return; }
        SetVelocity((movingRight ? 1 : -1) * moveSpeed);
        if ((movingRight && !isFacingRight) || (!movingRight && isFacingRight)) Flip();
    }

    void HandleGuard(bool detected, bool inRange)
    {
        SetVelocity(0);
        if (player) LookAtPlayer();
        guardTimer += Time.deltaTime;

        if (!detected || guardTimer >= guardTime)
        {
            if (detected && inRange && attackTimer <= 0) EnterState(EnemyState.Attack);
            else if (detected) EnterState(EnemyState.Chase);
            else ReturnToPatrol();
        }
    }

    void HandleChase(bool detected, bool inRange)
    {
        if (!detected || chaseTimer >= chaseTimeout) { ReturnToPatrol(); return; }
        if (inRange && attackTimer <= 0) { EnterState(EnemyState.Attack); return; }
        if (player) LookAtPlayer();
        if (isAtEdge) { SetVelocity(0); return; }

        float dir = player.position.x > transform.position.x ? 1 : -1;
        SetVelocity(dir * chaseSpeed);
        chaseTimer += Time.deltaTime;
    }

    public void PerformDash(float force, float duration)
    {
        StartCoroutine(DashCoroutine(force, duration));
    }

    IEnumerator DashCoroutine(float force, float duration)
    {
        isDashing = true;
        currentState = EnemyState.Dashing;
        if (player) LookAtPlayer();

        float dir = isFacingRight ? 1 : -1;
        if (sr) sr.color = dashColor;

        float origGrav = rb.gravityScale;
        rb.gravityScale = 0;
        rb.linearVelocity = new Vector2(dir * force, 0);

        if (showDashTrail && sr) StartCoroutine(SpawnDashGhostTrail(duration));

        yield return new WaitForSeconds(duration);

        rb.gravityScale = origGrav;
        rb.linearVelocity = Vector2.zero;
        if (sr) sr.color = originalColor;

        isDashing = false;
        isDashingToPlayer = false;
        dashCooldownTimer = dashCooldownTime;
        EnterState(EnemyState.Chase);
    }

    IEnumerator SpawnDashGhostTrail(float duration)
    {
        float elapsed = 0;
        while (elapsed < duration && isDashing)
        {
            GameObject ghost = new GameObject("Ghost");
            ghost.transform.position = transform.position;
            ghost.transform.localScale = transform.localScale;
            SpriteRenderer g = ghost.AddComponent<SpriteRenderer>();
            g.sprite = sr.sprite;
            g.color = new Color(dashColor.r, dashColor.g, dashColor.b, 0.5f);
            g.sortingOrder = sr.sortingOrder - 1;
            Destroy(ghost, 0.3f);
            yield return new WaitForSeconds(0.05f);
            elapsed += 0.05f;
        }
    }

    // ========================================
    // TAKE DAMAGE
    // ========================================
    public void TakeDamage(int damage, int knockbackDirection)
    {
        if (isInvincible) return;

        // Si es inmortal, solo flashea
        if (isImmortalForAbsorption)
        {
            StartCoroutine(FlashRoutine(immortalColor));
            return;
        }

        health -= damage;
        if (healthBar != null) healthBar.UpdateHealth(health, maxHealth);

        rb.linearVelocity = new Vector2(knockbackDirection * knockbackForce.x, knockbackForce.y);

        // ── Spawn sangre ──────────────────────────────────────
        SpawnBlood(knockbackDirection);
        // ─────────────────────────────────────────────────────

        // ========================================
        // SI LLEGARÍA A 0 O MENOS
        // ========================================
        if (health <= 0)
        {
            if (immortalWhenCriticalWithDash && !abilityDisabledByPlayerProgress &&
                abilityHolder != null && abilityHolder.HasAbility())
            {
                health = 1;
                if (healthBar != null) healthBar.UpdateHealth(health, maxHealth);
            }
            else
            {
                Die();
            }
        }
        else
        {
            StartCoroutine(InvincibilityCoroutine());
        }
    }

    void SpawnBlood(int knockDir)
    {
        if (!bloodEffectPrefab) return;
        GameObject bloodGO = Instantiate(bloodEffectPrefab, transform.position + bloodOffset, Quaternion.identity);
        BloodEffect be = bloodGO.GetComponent<BloodEffect>();
        if (be) be.Play(knockDir);
    }

    IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        for (int i = 0; i < 5; i++)
        {
            if (sr) sr.color = Color.red;
            yield return new WaitForSeconds(invincibilityTime / 10f);
            if (sr) sr.color = originalColor;
            yield return new WaitForSeconds(invincibilityTime / 10f);
        }
        isInvincible = false;
    }

    public bool CanBeAbsorbed()
    {
        if (abilityDisabledByPlayerProgress) return false;
        return isCritical && abilityHolder != null && abilityHolder.HasAbility();
    }

    public void OnAbsorbed()
    {
        isImmortalForAbsorption = false;
        isCritical = false;
        if (abilityHolder != null) abilityHolder.RemoveAbility();
        Die();
    }

    void CheckCriticalHealth()
    {
        if (abilityDisabledByPlayerProgress) { isCritical = false; return; }

        bool wasCritical = isCritical;
        isCritical = (health <= criticalHealthThreshold && health > 0);

        if (isCritical && !wasCritical)
        {
            if (immortalWhenCriticalWithDash && abilityHolder != null && abilityHolder.HasAbility())
                isImmortalForAbsorption = true;

            StartCoroutine(CriticalHealthSequence());
        }
    }

    IEnumerator CriticalHealthSequence()
    {
        isCriticalStunned = true;
        currentState = EnemyState.Stunned;
        SetVelocity(0);

        Color flashColor = isImmortalForAbsorption ? immortalColor : Color.red;
        float elapsed = 0;

        while (elapsed < criticalStunDuration && isCritical)
        {
            if (sr) sr.color = flashColor;
            yield return new WaitForSeconds(0.15f);
            if (sr) sr.color = originalColor;
            yield return new WaitForSeconds(0.15f);
            elapsed += 0.3f;
        }

        if (!isCritical) yield break;

        isCriticalStunned = false;
        if (healthBar) healthBar.ForceShow();

        if (isImmortalForAbsorption)
            EnterState(EnemyState.WaitingAbsorption);
        else
            EnterState(EnemyState.Chase);

        while (isCritical)
        {
            if (sr) sr.color = flashColor;
            yield return new WaitForSeconds(0.15f);
            if (sr) sr.color = originalColor;
            yield return new WaitForSeconds(0.15f);
        }
    }

    IEnumerator FlashRoutine(Color c)
    {
        for (int i = 0; i < 3; i++)
        {
            if (sr) sr.color = c;
            yield return new WaitForSeconds(0.1f);
            if (sr) sr.color = Color.white;
            yield return new WaitForSeconds(0.1f);
        }
        if (sr) sr.color = originalColor;
    }

    void EnterState(EnemyState newState)
    {
        currentState = newState;
        ResetTimers();
        if (guardEffect) guardEffect.SetActive(newState == EnemyState.Guard);
        if (sr && !isInvincible && !isCritical && !isImmortalForAbsorption)
            sr.color = (newState == EnemyState.Guard) ? guardColor : originalColor;
    }

    void ReturnToPatrol() => EnterState(shouldPatrol ? EnemyState.Patrol : EnemyState.Idle);

    void LookAtPlayer()
    {
        if (!player) return;
        bool playerRight = player.position.x > transform.position.x;
        if ((playerRight && !isFacingRight) || (!playerRight && isFacingRight)) Flip();
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }

    bool CanSeePlayer()
    {
        if (!player || Vector2.Distance(transform.position, player.position) > detectionRange) return false;
        Vector2 dir = (player.position - detectionPoint.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(detectionPoint.position, dir, detectionRange, wallLayer | PlayerLayer);
        return hit.collider != null && hit.collider.CompareTag("Player");
    }

    void CheckEdge()
    {
        if (!edgeCheckPoint) return;
        Vector2 origin = new Vector2(transform.position.x + (2f * (isFacingRight ? 1 : -1)), edgeCheckPoint.position.y);
        isAtEdge = !Physics2D.Raycast(origin, Vector2.down, 1f, groundLayer);
    }

    void CreateEdgeCheck()
    {
        GameObject obj = new GameObject("EdgeCheck");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = new Vector3(0.8f, -0.5f, 0);
        edgeCheckPoint = obj.transform;
    }

    void SetVelocity(float x) => rb.linearVelocity = new Vector2(x, rb.linearVelocity.y);

    public void RestoreFullHealth()
    {
        health = maxHealth;
        if (healthBar != null) healthBar.UpdateHealth(health, maxHealth);
    }

    void Die()
    {
        // Sangre extra al morir
        if (bloodEffectPrefab)
        {
            GameObject bloodGO = Instantiate(bloodEffectPrefab, transform.position + bloodOffset, Quaternion.identity);
            BloodEffect be = bloodGO.GetComponent<BloodEffect>();
            if (be) be.PlayDeath();
        }

        if (healthBar != null) healthBar.gameObject.SetActive(false);
        if (EnemyManager.Instance != null) EnemyManager.Instance.OnEnemyDeath(gameObject);
        else Destroy(gameObject);
    }

    void UpdateTimers()
    {
        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;
        if (playerIgnoreTimer > 0) playerIgnoreTimer -= Time.deltaTime;
        if (attackTimer > 0) attackTimer -= Time.deltaTime;
        if (waitTimer > 0) waitTimer -= Time.deltaTime;
    }

    void ResetTimers() { chaseTimer = guardTimer = edgeWaitTimer = totalEdgeTime = 0; }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, attackRange);
        if (attackPoint) Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}
