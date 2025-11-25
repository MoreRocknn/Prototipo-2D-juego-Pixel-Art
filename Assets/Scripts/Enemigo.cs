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

    [Header("=== CONFIGURACIÓN BARRA & CRÍTICO ===")]
    public Vector3 healthBarOffset = new Vector3(0, 1.2f, 0);
    public bool hideHealthBarWhenFull = true;
    public int criticalHealthThreshold = 1;
    public float criticalStunDuration = 3f;

    [Header("=== DETECCIÓN & HABILIDAD ===")]
    public float detectionRange = 8f;
    public float attackRange = 2f;
    public LayerMask PlayerLayer, wallLayer, groundLayer;
    public Transform detectionPoint, edgeCheckPoint;
    public bool startsWithAbility = true;
    public AbilityType startingAbility = AbilityType.Dash;

    [Header("=== DASH AGRESIVO & VISUALES ===")]
    public float dashCooldownTime = 3f;
    public float dashDistanceThreshold = 3.5f;
    public Color dashColor = new Color(0.3f, 0.8f, 1f);
    public bool showDashTrail = true;
    private float dashCooldownTimer = 0f;

    [Header("=== COMPORTAMIENTO ===")]
    public float moveSpeed = 3f;
    public float chaseSpeed = 5f;
    public float guardTime = 0.8f;
    public float attackCooldown = 1.5f;
    public float chaseTimeout = 5f;
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

    [Header("=== VISUALES ===")]
    public GameObject guardEffect, attackEffect;
    public Color guardColor = Color.yellow, attackColor = Color.red;

    private EnemyState currentState = EnemyState.Idle;
    private enum EnemyState { Idle, Patrol, Guard, Chase, Attack, Stunned }

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

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        abilityHolder = gameObject.AddComponent<AbilityHolder>();

        if (sr) originalColor = sr.color;
        startPos = transform.position;
        health = maxHealth;

        if (startsWithAbility) abilityHolder.SetAbility(new DashAbility());

        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (!detectionPoint) detectionPoint = transform;
        if (!edgeCheckPoint) CreateEdgeCheck();

        if (AbilityAbsorptionManager.Instance != null)
        {
            AbilityAbsorptionManager.Instance.RegisterResettable(this);
        }

        SetupHealthBar();

        currentState = shouldPatrol ? EnemyState.Patrol : EnemyState.Idle;
    }

    void OnDestroy()
    {
        if (AbilityAbsorptionManager.Instance != null)
        {
            AbilityAbsorptionManager.Instance.UnregisterResettable(this);
        }
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

        if (startsWithAbility && abilityHolder != null)
        {
            abilityHolder.SetAbility(new DashAbility());
        }

        if (healthBar == null) SetupHealthBar();
        else
        {
            healthBar.gameObject.SetActive(true);
            healthBar.UpdateHealth(health, maxHealth);
        }

        ResetTimers();
        rb.linearVelocity = Vector2.zero;
    }

    void SetupHealthBar()
    {
        if (HealthBarFactory.Instance)
            healthBar = HealthBarFactory.Instance.CreateHealthBar(transform, health, maxHealth, healthBarOffset);
        else
        {
            GameObject hbObj = new GameObject($"HealthBar_{name}");
            healthBar = hbObj.AddComponent<HealthBarUI>();
            healthBar.Initialize(transform, health, maxHealth);
            healthBar.offset = healthBarOffset;
        }
        if (healthBar) healthBar.alwaysShow = !hideHealthBarWhenFull;
    }

    void Update()
    {
        if (isInvincible || currentState == EnemyState.Stunned || isDashing || isCriticalStunned)
        {
            // Permitir que la física funcione incluso aturdido, pero no el movimiento controlado
            if (isCriticalStunned) rb.linearVelocity = Vector2.zero;
            return;
        }

        UpdateTimers();
        CheckCriticalHealth();
        CheckEdge();

        bool seePlayer = CanSeePlayer();
        bool detected = seePlayer && playerIgnoreTimer <= 0;

        float distToPlayer = Mathf.Infinity;
        if (player != null)
        {
            distToPlayer = Vector2.Distance(detectionPoint.position, player.position);
        }

        bool inRange = detected && distToPlayer <= attackRange;

        if (currentState == EnemyState.Chase && detected && abilityHolder.HasAbility() && dashCooldownTimer <= 0)
        {
            if (distToPlayer > dashDistanceThreshold) abilityHolder.UseAbility();
        }

        switch (currentState)
        {
            case EnemyState.Idle: SetVelocity(0); if (detected) EnterState(EnemyState.Guard); break;
            case EnemyState.Patrol: if (detected) EnterState(EnemyState.Guard); else HandlePatrol(); break;
            case EnemyState.Guard: HandleGuard(detected, inRange); break;
            case EnemyState.Chase: HandleChase(detected, inRange); break;
            case EnemyState.Attack: if (!isAttacking) StartCoroutine(PerformAttack()); break;
        }

        UpdateAnim();
    }

    void UpdateAnim()
    {
        if (!anim || !anim.runtimeAnimatorController) return;
        anim.SetBool("isGuarding", currentState == EnemyState.Guard);
        anim.SetBool("isAttacking", currentState == EnemyState.Attack);
        anim.SetFloat("speed", Mathf.Abs(rb.linearVelocity.x));
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;
        SetVelocity(0);

        if (attackEffect && attackPoint)
        {
            GameObject obj = Instantiate(attackEffect, attackPoint);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localScale = new Vector3((isFacingRight ? 1 : -1) * 0.7f, 0.7f, 0.7f);
            Destroy(obj, attackDuration + 0.5f);
        }
        if (sr) sr.color = attackColor;

        if (attackPoint != null)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    hit.GetComponent<MainChar>()?.TakeDamage(attackDamage);
                }
            }
        }

        yield return new WaitForSeconds(attackDuration);

        isAttacking = false;
        attackTimer = attackCooldown;
        EnterState(EnemyState.Guard);
    }

    void HandlePatrol()
    {
        if (waitTimer > 0) { SetVelocity(0); return; }
        if (isAtEdge) { movingRight = !movingRight; waitTimer = waitTimeAtPatrolPoint; Flip(); return; }
        SetVelocity((movingRight ? 1 : -1) * moveSpeed);
        if ((movingRight && transform.position.x >= startPos.x + patrolDistance) ||
            (!movingRight && transform.position.x <= startPos.x - patrolDistance))
        {
            movingRight = !movingRight; waitTimer = waitTimeAtPatrolPoint;
        }
    }

    void HandleGuard(bool detected, bool inRange)
    {
        SetVelocity(0);
        if (!detected) { ReturnToPatrol(); return; }
        LookAtPlayer();
        if (isAtEdge)
        {
            totalEdgeTime += Time.deltaTime;
            if (totalEdgeTime >= maxEdgeWaitTime) { TurnAroundAndPatrol(); return; }
        }
        else totalEdgeTime = 0;

        if (guardTimer >= guardTime)
        {
            if (inRange && attackTimer <= 0) EnterState(EnemyState.Attack);
            else if (!inRange)
            {
                if (!isAtEdge) EnterState(EnemyState.Chase);
                else
                {
                    edgeWaitTimer += Time.deltaTime;
                    if (edgeWaitTimer >= edgeWaitTime) TurnAroundAndPatrol();
                }
            }
        }
        if (!isAtEdge) edgeWaitTimer = 0;
    }

    void HandleChase(bool detected, bool inRange)
    {
        if (chaseTimer >= chaseTimeout || !detected) { playerIgnoreTimer = 3f; ReturnToPatrol(); return; }
        if (isAtEdge) { SetVelocity(0); LookAtPlayer(); EnterState(EnemyState.Guard); return; }
        LookAtPlayer();
        Vector2 dir = (player.position - transform.position).normalized;
        SetVelocity(dir.x * chaseSpeed);
        if (inRange)
        {
            if (attackTimer <= 0) EnterState(EnemyState.Attack);
            else EnterState(EnemyState.Guard);
        }
    }

    // CAMBIO AQUÍ: Permitir daño incluso si está criticalStunned
    public void TakeDamage(int damage, float knockbackDir)
    {
        // Solo ignoramos daño si es invencible (por golpe reciente), pero NO si está aturdido crítico
        if (isInvincible) return;

        health -= damage;
        if (healthBar) healthBar.UpdateHealth(health, maxHealth);

        rb.linearVelocity = new Vector2(knockbackForce.x * knockbackDir, knockbackForce.y);

        // Si no estaba crítico, ahora se aturde normal. Si estaba crítico, sigue crítico hasta morir.
        if (!isCritical)
        {
            currentState = EnemyState.Stunned;
        }

        ResetTimers();

        if (health <= 0) Die();
        else if (!isCritical) StartCoroutine(RecoverAndChase());
    }

    public bool CanBeAbsorbed() => isCritical && !isCriticalStunned;

    public void OnAbsorbed()
    {
        Debug.Log("Enemigo Absorbido");
        StartCoroutine(FlashRoutine(dashColor));
        if (player != null)
        {
            MainChar playerScript = player.GetComponent<MainChar>();
            SpriteRenderer playerSr = player.GetComponent<SpriteRenderer>();
            if (playerScript != null && playerSr != null) playerScript.StartCoroutine(PlayerAbsorptionFlash(playerSr));
        }
        if (abilityHolder && !abilityHolder.HasAbility())
        {
            Die();
        }
    }

    IEnumerator PlayerAbsorptionFlash(SpriteRenderer playerSr)
    {
        Color oldColor = playerSr.color;
        for (int i = 0; i < 4; i++)
        {
            playerSr.color = dashColor; yield return new WaitForSeconds(0.08f);
            playerSr.color = Color.white; yield return new WaitForSeconds(0.08f);
        }
        playerSr.color = oldColor;
    }

    public void PerformDash(float force, float duration) => StartCoroutine(DashRoutine(force, duration));

    IEnumerator DashRoutine(float force, float duration)
    {
        isDashing = true; dashCooldownTimer = dashCooldownTime;
        float dir = (player && player.position.x > transform.position.x) ? 1 : (isFacingRight ? 1 : -1);
        SetVelocity(dir * force);
        if (sr) sr.color = dashColor;
        if (showDashTrail) StartCoroutine(SpawnDashTrail(duration));
        yield return new WaitForSeconds(duration);
        isDashing = false; SetVelocity(rb.linearVelocity.x * 0.5f);
        if (sr && currentState == EnemyState.Guard) sr.color = guardColor;
        else if (sr && !isInvincible && !isCritical) sr.color = originalColor;
    }

    IEnumerator SpawnDashTrail(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && isDashing)
        {
            GameObject ghost = new GameObject("Ghost");
            ghost.transform.position = transform.position; ghost.transform.localScale = transform.localScale;
            SpriteRenderer gSr = ghost.AddComponent<SpriteRenderer>();
            gSr.sprite = sr.sprite; gSr.color = new Color(dashColor.r, dashColor.g, dashColor.b, 0.5f);
            gSr.sortingOrder = sr.sortingOrder - 1; Destroy(ghost, 0.4f);
            yield return new WaitForSeconds(0.05f); elapsed += 0.05f;
        }
    }

    void CheckCriticalHealth()
    {
        bool wasCritical = isCritical; isCritical = (health <= criticalHealthThreshold && health > 0);
        if (isCritical && !wasCritical) StartCoroutine(CriticalHealthSequence());
        else if (!isCritical && wasCritical)
        {
            StopAllCoroutines(); isCriticalStunned = false; if (sr) sr.color = originalColor;
            if (player && Vector2.Distance(transform.position, player.position) <= detectionRange) EnterState(EnemyState.Chase);
            else ReturnToPatrol();
        }
    }

    IEnumerator CriticalHealthSequence()
    {
        isCriticalStunned = true; currentState = EnemyState.Stunned; SetVelocity(0);
        float elapsed = 0;
        while (elapsed < criticalStunDuration && isCritical)
        {
            if (sr) sr.color = Color.red; yield return new WaitForSeconds(0.15f);
            if (sr) sr.color = originalColor; yield return new WaitForSeconds(0.15f);
            elapsed += 0.3f;
        }
        if (!isCritical) yield break;
        isCriticalStunned = false; if (healthBar) healthBar.ForceShow();
        if (player && Vector2.Distance(transform.position, player.position) <= detectionRange) EnterState(EnemyState.Chase);
        else ReturnToPatrol();
        while (isCritical)
        {
            if (sr) sr.color = Color.red; yield return new WaitForSeconds(0.15f);
            if (sr) sr.color = originalColor; yield return new WaitForSeconds(0.15f);
        }
    }

    IEnumerator RecoverAndChase()
    {
        isInvincible = true;
        for (int i = 0; i < 5; i++) { if (sr) sr.color = Color.red; yield return new WaitForSeconds(invincibilityTime / 10f); if (sr) sr.color = originalColor; yield return new WaitForSeconds(invincibilityTime / 10f); }
        isInvincible = false; EnterState(player ? EnemyState.Chase : EnemyState.Idle);
    }

    IEnumerator FlashRoutine(Color c)
    {
        for (int i = 0; i < 3; i++) { if (sr) sr.color = c; yield return new WaitForSeconds(0.1f); if (sr) sr.color = Color.white; yield return new WaitForSeconds(0.1f); }
        if (sr) sr.color = originalColor;
    }

    void EnterState(EnemyState newState)
    {
        currentState = newState; ResetTimers();
        if (guardEffect) guardEffect.SetActive(newState == EnemyState.Guard);
        if (sr && !isInvincible && !isCritical) sr.color = (newState == EnemyState.Guard) ? guardColor : originalColor;
    }
    void ReturnToPatrol() => EnterState(shouldPatrol ? EnemyState.Patrol : EnemyState.Idle);
    void TurnAroundAndPatrol() { movingRight = !isFacingRight; Flip(); waitTimer = waitTimeAtPatrolPoint; playerIgnoreTimer = waitTimeAtPatrolPoint + 2f; EnterState(EnemyState.Patrol); }
    void LookAtPlayer() { if (!player) return; bool playerRight = player.position.x > transform.position.x; if ((playerRight && !isFacingRight) || (!playerRight && isFacingRight)) Flip(); }
    void Flip() { isFacingRight = !isFacingRight; transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z); }
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
        Vector2 origin = new Vector2(transform.position.x + (0.8f * (isFacingRight ? 1 : -1)), edgeCheckPoint.position.y);
        isAtEdge = !Physics2D.Raycast(origin, Vector2.down, 0.5f, groundLayer);
    }
    void CreateEdgeCheck()
    {
        GameObject obj = new GameObject("EdgeCheckPoint"); obj.transform.SetParent(transform); obj.transform.localPosition = new Vector3(0.8f, -0.5f, 0); edgeCheckPoint = obj.transform;
    }
    void SetVelocity(float x) => rb.linearVelocity = new Vector2(x, rb.linearVelocity.y);

    void Die()
    {
        if (healthBar) healthBar.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    void UpdateTimers()
    {
        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;
        if (playerIgnoreTimer > 0) playerIgnoreTimer -= Time.deltaTime;
        if (attackTimer > 0) attackTimer -= Time.deltaTime;
        if (waitTimer > 0) waitTimer -= Time.deltaTime;
        if (currentState == EnemyState.Guard) guardTimer += Time.deltaTime;
        if (currentState == EnemyState.Chase) chaseTimer += Time.deltaTime;
    }
    void ResetTimers() { chaseTimer = 0; guardTimer = 0; edgeWaitTimer = 0; totalEdgeTime = 0; }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, attackRange);
        if (attackPoint) Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}