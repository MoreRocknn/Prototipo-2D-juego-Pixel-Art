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

    // ========================================
    // CONFIGURACIÓN DE ENEMIGO DE ELITE
    // ========================================
    [Header("=== ENEMIGO DE ELITE (DASH) ===")]
    [Tooltip("Si es true, este enemigo es el 'elite' que da el Dash")]
    public bool isEliteWithDash = false;
    [Tooltip("Si el jugador ya tiene el Dash, este enemigo pierde su habilidad y no puede ser absorbido")]
    public bool disableAbilityIfPlayerHasDash = true;

    // Variable interna para saber si este enemigo perdió su habilidad
    private bool abilityDisabledByPlayerProgress = false;

    [Header("=== DASH AGRESIVO MEJORADO ===")]
    public float dashForce = 20f;
    public float dashDuration = 0.35f;
    public float dashCooldownTime = 2f;
    public float dashMinDistance = 3f;
    public float dashMaxDistance = 10f;
    public float dashPredictionTime = 0.2f;
    public bool canDashThroughPlayer = false;
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
    public string animTakeDamage = "takeDamage";
    public string animDie = "die";
    public string animAttackTrigger = "attack";

    [Header("=== VISUALES ===")]
    public GameObject guardEffect, attackEffect;
    public Color guardColor = Color.yellow, attackColor = Color.red;
    public Color immortalColor = new Color(1f, 0.84f, 0f);

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

    private Vector2 lastKnownPlayerPosition;
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

        // Verificar si debe tener habilidad o no
        CheckAndSetupAbility();

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player)
        {
            playerRb = player.GetComponent<Rigidbody2D>();
        }

        if (!detectionPoint) detectionPoint = transform;
        if (!edgeCheckPoint) CreateEdgeCheck();

        if (AbilityAbsorptionManager.Instance != null)
        {
            AbilityAbsorptionManager.Instance.RegisterResettable(this);
        }

        SetupHealthBar();

        currentState = shouldPatrol ? EnemyState.Patrol : EnemyState.Idle;
    }

    void CheckAndSetupAbility()
    {
        if (isEliteWithDash && disableAbilityIfPlayerHasDash)
        {
            if (GameManager.Instance != null && GameManager.Instance.HasPermanentDash())
            {
                abilityDisabledByPlayerProgress = true;
                Debug.Log($"[{gameObject.name}] Enemigo elite SIN habilidad - El jugador ya tiene el Dash");
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
        isDashing = false;
        isDashingToPlayer = false;
        hasSeenPlayer = false;
        isImmortalForAbsorption = false;
        isAttacking = false;

        // Re-verificar si debe tener habilidad
        if (isEliteWithDash && disableAbilityIfPlayerHasDash)
        {
            if (GameManager.Instance != null && GameManager.Instance.HasPermanentDash())
            {
                abilityDisabledByPlayerProgress = true;
                if (abilityHolder != null)
                {
                    abilityHolder.RemoveAbility();
                }
            }
            else
            {
                abilityDisabledByPlayerProgress = false;
                if (startsWithAbility && abilityHolder != null)
                {
                    DashAbility enemyDash = new DashAbility();
                    enemyDash.limitedUses = false;
                    abilityHolder.SetAbility(enemyDash);
                }
            }
        }
        else if (startsWithAbility && abilityHolder != null && !abilityDisabledByPlayerProgress)
        {
            DashAbility enemyDash = new DashAbility();
            enemyDash.limitedUses = false;
            abilityHolder.SetAbility(enemyDash);
        }

        if (healthBar == null) SetupHealthBar();
        else
        {
            healthBar.gameObject.SetActive(true);
            healthBar.UpdateHealth(health, maxHealth);
        }

        ResetTimers();
        rb.linearVelocity = Vector2.zero;
        UpdateAnimations();
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
        if (isInvincible || currentState == EnemyState.Stunned || isCriticalStunned)
        {
            if (isCriticalStunned) rb.linearVelocity = Vector2.zero;
            UpdateAnimations();
            return;
        }

        if (isDashing)
        {
            UpdateAnimations();
            return;
        }

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
        float distToPlayer = Mathf.Infinity;

        if (player != null)
        {
            distToPlayer = Vector2.Distance(detectionPoint.position, player.position);

            if (seePlayer)
            {
                lastKnownPlayerPosition = player.position;
                hasSeenPlayer = true;
            }
        }

        bool detected = (seePlayer && playerIgnoreTimer <= 0) ||
                        (hasSeenPlayer && distToPlayer <= extendedChaseRange && playerIgnoreTimer <= 0);

        bool inRange = seePlayer && distToPlayer <= attackRange;

        // Intentar dash si tiene habilidad y está persiguiendo
        if (currentState == EnemyState.Chase && detected && !abilityDisabledByPlayerProgress &&
            abilityHolder != null && abilityHolder.HasAbility() && dashCooldownTimer <= 0)
        {
            if (distToPlayer >= dashMinDistance && distToPlayer <= dashMaxDistance && !isAtEdge)
            {
                Vector2 predictedPos = PredictPlayerPosition();
                float predictedDist = Vector2.Distance(transform.position, predictedPos);

                if (predictedDist >= dashMinDistance)
                {
                    isDashingToPlayer = true;
                    abilityHolder.UseAbility();
                }
            }
        }

        // Máquina de estados
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
                HandleGuard(detected, inRange, seePlayer);
                break;
            case EnemyState.Chase:
                HandleChase(detected, inRange, seePlayer);
                break;
            case EnemyState.Attack:
                // *** IMPORTANTE: Ejecutar ataque ***
                if (!isAttacking) StartCoroutine(PerformAttack());
                break;
        }

        UpdateAnimations();
    }

    void UpdateAnimations()
    {
        if (anim == null) return;

        anim.SetFloat(animSpeed, Mathf.Abs(rb.linearVelocity.x));
        anim.SetBool(animIsGrounded, IsGrounded());
        anim.SetBool(animIsGuarding, currentState == EnemyState.Guard);
        anim.SetBool(animIsAttacking, currentState == EnemyState.Attack || isAttacking);
        anim.SetBool(animIsDashing, isDashing);
        anim.SetBool(animIsCritical, isCritical);
        anim.SetBool(animIsStunned, currentState == EnemyState.Stunned || isCriticalStunned);
    }

    bool IsGrounded()
    {
        Vector2 origin = new Vector2(transform.position.x, transform.position.y - 0.5f);
        return Physics2D.Raycast(origin, Vector2.down, 0.2f, groundLayer);
    }

    Vector2 PredictPlayerPosition()
    {
        if (!player || !playerRb) return player ? (Vector2)player.position : (Vector2)transform.position;
        Vector2 playerVelocity = playerRb.linearVelocity;
        Vector2 currentPos = player.position;
        return currentPos + (playerVelocity * dashPredictionTime);
    }

    // ========================================
    // CORRUTINA DE ATAQUE
    // ========================================
    IEnumerator PerformAttack()
    {
        isAttacking = true;
        SetVelocity(0);

        // Trigger de animación
        if (anim) anim.SetTrigger(animAttackTrigger);

        // Efecto visual
        if (attackEffect && attackPoint)
        {
            GameObject obj = Instantiate(attackEffect, attackPoint);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localScale = new Vector3((isFacingRight ? 1 : -1) * 0.7f, 0.7f, 0.7f);
            Destroy(obj, attackDuration + 0.5f);
        }

        if (sr) sr.color = attackColor;

        // Esperar un frame + mitad de la duración del ataque
        yield return null;
        yield return new WaitForSeconds(attackDuration * 0.5f);

        // Hacer daño al jugador si está en rango
        if (attackPoint != null)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, PlayerLayer);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    MainChar playerScript = hit.GetComponent<MainChar>();
                    if (playerScript != null)
                    {
                        playerScript.TakeDamage(attackDamage);
                        Debug.Log($"[{gameObject.name}] Golpeó al jugador por {attackDamage} de daño");
                    }
                }
            }
        }

        // Esperar la otra mitad
        yield return new WaitForSeconds(attackDuration * 0.5f);

        // Restaurar color
        if (sr) sr.color = originalColor;

        isAttacking = false;
        attackTimer = attackCooldown;

        // Volver a guardia
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
            movingRight = !movingRight;
            waitTimer = waitTimeAtPatrolPoint;
        }

        if ((movingRight && !isFacingRight) || (!movingRight && isFacingRight)) Flip();
    }

    void HandleGuard(bool detected, bool inRange, bool seePlayer)
    {
        SetVelocity(0);
        if (player) LookAtPlayer();

        guardTimer += Time.deltaTime;

        if (!detected || guardTimer >= guardTime)
        {
            if (detected && inRange && attackTimer <= 0)
            {
                EnterState(EnemyState.Attack);
            }
            else if (detected)
            {
                EnterState(EnemyState.Chase);
            }
            else
            {
                ReturnToPatrol();
            }
        }
    }

    void HandleChase(bool detected, bool inRange, bool seePlayer)
    {
        if (!detected || chaseTimer >= chaseTimeout)
        {
            if (isAtEdge) TurnAroundAndPatrol();
            else ReturnToPatrol();
            return;
        }

        // Atacar si está en rango y el cooldown terminó
        if (inRange && attackTimer <= 0)
        {
            EnterState(EnemyState.Attack);
            return;
        }

        if (player) LookAtPlayer();

        if (isAtEdge)
        {
            HandleEdgeWhileChasing();
            return;
        }

        float dir = player.position.x > transform.position.x ? 1 : -1;
        SetVelocity(dir * chaseSpeed);

        chaseTimer += Time.deltaTime;
    }

    void HandleEdgeWhileChasing()
    {
        SetVelocity(0);
        edgeWaitTimer += Time.deltaTime;
        totalEdgeTime += Time.deltaTime;

        if (totalEdgeTime >= maxEdgeWaitTime)
        {
            TurnAroundAndPatrol();
            return;
        }

        if (edgeWaitTimer >= edgeWaitTime)
        {
            edgeWaitTimer = 0;
            movingRight = !movingRight;
            Flip();
        }
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

        // Si está haciendo dash hacia el jugador, calcular dirección
        if (isDashingToPlayer && player != null)
        {
            Vector2 targetPos = PredictPlayerPosition();
            Vector2 dashDirection = ((Vector3)targetPos - transform.position).normalized;
            dir = dashDirection.x >= 0 ? 1 : -1;

            if ((dir > 0 && !isFacingRight) || (dir < 0 && isFacingRight)) Flip();
        }

        if (sr) sr.color = dashColor;

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0;
        rb.linearVelocity = new Vector2(dir * force, 0);

        if (showDashTrail && sr)
        {
            StartCoroutine(SpawnDashGhostTrail(duration));
        }

        yield return new WaitForSeconds(duration);

        rb.gravityScale = originalGravity;
        rb.linearVelocity = Vector2.zero;

        if (sr) sr.color = originalColor;
        isDashing = false;
        isDashingToPlayer = false;
        dashCooldownTimer = dashCooldownTime;

        // Después del dash, volver a perseguir o atacar
        if (player && Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            EnterState(EnemyState.Attack);
        }
        else
        {
            EnterState(EnemyState.Chase);
        }
    }

    IEnumerator SpawnDashGhostTrail(float duration)
    {
        float elapsed = 0;
        while (elapsed < duration && isDashing)
        {
            GameObject ghost = new GameObject("DashGhost");
            ghost.transform.position = transform.position;
            ghost.transform.localScale = transform.localScale;
            SpriteRenderer gSr = ghost.AddComponent<SpriteRenderer>();
            gSr.sprite = sr.sprite;
            gSr.color = new Color(dashColor.r, dashColor.g, dashColor.b, 0.5f);
            gSr.sortingOrder = sr.sortingOrder - 1;
            Destroy(ghost, 0.4f);
            yield return new WaitForSeconds(0.05f);
            elapsed += 0.05f;
        }
    }

    public void TakeDamage(int damage, int knockbackDirection)
    {
        if (isInvincible) return;

        if (isImmortalForAbsorption && !abilityDisabledByPlayerProgress)
        {
            Debug.Log($"{gameObject.name} es INMORTAL - ¡Solo puede ser absorbido!");
            StartCoroutine(FlashRoutine(immortalColor));
            return;
        }

        health -= damage;
        if (healthBar) healthBar.UpdateHealth(health, maxHealth);

        rb.linearVelocity = new Vector2(knockbackDirection * knockbackForce.x, knockbackForce.y);

        if (health <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvincibilityCoroutine());
        }
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
        if (abilityDisabledByPlayerProgress)
            return false;

        return isCritical && abilityHolder != null && abilityHolder.HasAbility();
    }

    public void OnAbsorbed()
    {
        Debug.Log($"{gameObject.name} fue absorbido");
        isImmortalForAbsorption = false;
        isCritical = false;

        if (abilityHolder != null)
        {
            abilityHolder.RemoveAbility();
        }

        Die();
    }

    void CheckCriticalHealth()
    {
        bool wasCritical = isCritical;

        if (abilityDisabledByPlayerProgress)
        {
            isCritical = false;
            isImmortalForAbsorption = false;
            return;
        }

        isCritical = (health <= criticalHealthThreshold && health > 0);

        if (isCritical && !wasCritical)
        {
            StartCoroutine(CriticalHealthSequence());

            if (immortalWhenCriticalWithDash && abilityHolder != null && abilityHolder.HasAbility())
            {
                isImmortalForAbsorption = true;
                Debug.Log("¡Enemigo ahora es INMORTAL! Solo puede ser absorbido");
            }
        }
        else if (!isCritical && wasCritical)
        {
            StopAllCoroutines();
            isCriticalStunned = false;
            isImmortalForAbsorption = false;

            if (sr) sr.color = originalColor;
            if (player && Vector2.Distance(transform.position, player.position) <= detectionRange)
                EnterState(EnemyState.Chase);
            else
                ReturnToPatrol();
        }
    }

    IEnumerator CriticalHealthSequence()
    {
        isCriticalStunned = true;
        currentState = EnemyState.Stunned;
        SetVelocity(0);

        float elapsed = 0;
        Color flashColor = isImmortalForAbsorption ? immortalColor : Color.red;

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
        {
            EnterState(EnemyState.WaitingAbsorption);
        }
        else
        {
            if (player && Vector2.Distance(transform.position, player.position) <= detectionRange)
                EnterState(EnemyState.Chase);
            else
                ReturnToPatrol();
        }

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

        if (guardEffect)
            guardEffect.SetActive(newState == EnemyState.Guard);

        if (sr && !isInvincible && !isCritical && !isImmortalForAbsorption)
            sr.color = (newState == EnemyState.Guard) ? guardColor : originalColor;
        else if (sr && isImmortalForAbsorption)
            sr.color = immortalColor;
    }

    void ReturnToPatrol() => EnterState(shouldPatrol ? EnemyState.Patrol : EnemyState.Idle);

    void TurnAroundAndPatrol()
    {
        movingRight = !isFacingRight;
        Flip();
        waitTimer = waitTimeAtPatrolPoint;
        playerIgnoreTimer = waitTimeAtPatrolPoint + 2f;
        EnterState(EnemyState.Patrol);
    }

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
        Vector2 origin = new Vector2(transform.position.x + (0.8f * (isFacingRight ? 1 : -1)), edgeCheckPoint.position.y);
        isAtEdge = !Physics2D.Raycast(origin, Vector2.down, 0.5f, groundLayer);
    }

    void CreateEdgeCheck()
    {
        GameObject obj = new GameObject("EdgeCheckPoint");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = new Vector3(0.8f, -0.5f, 0);
        edgeCheckPoint = obj.transform;
    }

    void SetVelocity(float x) => rb.linearVelocity = new Vector2(x, rb.linearVelocity.y);

    public void RestoreFullHealth()
    {
        health = maxHealth;
        if (healthBar) healthBar.UpdateHealth(health, maxHealth);
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} murió");

        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
        }

        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.OnEnemyDeath(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void UpdateTimers()
    {
        if (dashCooldownTimer > 0) dashCooldownTimer -= Time.deltaTime;
        if (playerIgnoreTimer > 0) playerIgnoreTimer -= Time.deltaTime;
        if (attackTimer > 0) attackTimer -= Time.deltaTime;
        if (waitTimer > 0) waitTimer -= Time.deltaTime;
    }

    void ResetTimers()
    {
        chaseTimer = 0;
        guardTimer = 0;
        edgeWaitTimer = 0;
        totalEdgeTime = 0;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, extendedChaseRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        if (attackPoint) Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, dashMinDistance);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, dashMaxDistance);
    }
}