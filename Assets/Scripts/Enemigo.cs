using System.Collections;
using UnityEngine;

public class Enemigo : MonoBehaviour, IAbsorbable, IDashExecutor
{
    [Header("=== SALUD ===")]
    public int health = 3;
    public int maxHealth = 3;
    public float invincibilityTime = 0.5f;
    private bool isInvincible = false;
    public Vector2 knockbackForce = new Vector2(3f, 5f);

    [Header("=== BARRA DE VIDA ===")]
    public Vector3 healthBarOffset = new Vector3(0, 1.2f, 0);
    public bool showHealthBar = true;
    public bool hideHealthBarWhenFull = true;
    private HealthBarUI healthBar;

    [Header("=== PARPADEO AL MORIR ===")]
    public int criticalHealthThreshold = 1;
    public float criticalBlinkSpeed = 0.15f;
    public float criticalStunDuration = 3f;
    private bool isCritical = false;
    private bool isCriticalStunned = false;

    [Header("=== PROTECCIÓN ANTI-VUELO ===")]
    public float knockbackInvincibilityTime = 0.3f;
    private bool isKnockbackInvincible = false;

    [Header("=== SISTEMA DE HABILIDADES ===")]
    public bool startsWithAbility = true;
    public AbilityType startingAbility = AbilityType.Dash;
    private AbilityHolder abilityHolder;
    private bool canBeAbsorbed = false;
    public KeyCode enemyAbilityKey = KeyCode.Z; // Para debug/testing
    private bool isDashing = false;

    [Header("=== DETECCIÓN DE BORDES ===")]
    public Transform edgeCheckPoint;
    public float edgeCheckDistance = 0.5f;
    public LayerMask groundLayer;
    public bool showEdgeDebug = true;
    public float edgeCheckOffset = 0.8f;
    private bool isAtEdge = false;

    [Header("=== DETECCIÓN ===")]
    public float detectionRange = 8f;
    public float attackRange = 2f;
    public LayerMask PlayerLayer;
    public LayerMask wallLayer;
    public Transform detectionPoint;

    [Header("=== COMPORTAMIENTO ===")]
    public float moveSpeed = 3f;
    public float chaseSpeed = 5f;
    public float guardTime = 0.8f;
    public float attackCooldown = 1.5f;
    public float edgeWaitTime = 3f;
    public float chaseTimeout = 5f;
    public float maxEdgeWaitTime = 10f;

    [Header("=== ATAQUE ===")]
    public Transform attackPoint;
    public float attackRadius = 1f;
    public int attackDamage = 1;
    public float attackDuration = 0.3f;

    [Header("=== PATRULLA ===")]
    public bool shouldPatrol = true;
    public float patrolDistance = 5f;
    public float waitTimeAtPatrolPoint = 2f;

    [Header("=== EFECTOS VISUALES ===")]
    public GameObject guardEffect;
    public GameObject attackEffect;
    public Color guardColor = Color.yellow;
    public Color attackColor = Color.red;

    [Header("=== DEBUG ===")]
    public bool showDebugGizmos = true;

    private enum EnemyState
    {
        Idle,
        Patrol,
        Guard,
        Chase,
        Attack,
        Stunned
    }

    private EnemyState currentState = EnemyState.Idle;

    // Componentes cacheados
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Transform player;
    private Animator animator;
    private Color originalColor;

    // Estado
    private bool isFacingRight = true;
    private float attackTimer = 0f;
    private float guardTimer = 0f;
    private bool isAttacking = false;
    private Vector2 startPosition;
    private float patrolTarget;
    private bool movingRight = true;
    private float waitTimer = 0f;
    private float edgeWaitTimer = 0f;
    private float chaseTimer = 0f;
    private float totalEdgeTime = 0f;
    private float playerIgnoreTimer = 0f;

    void Start()
    {
        // Cachear componentes
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Inicializar vida
        health = maxHealth;

        // Crear barra de vida
        if (showHealthBar)
        {
            CreateHealthBar();
        }

        // Inicializar sistema de habilidades
        abilityHolder = GetComponent<AbilityHolder>();
        if (abilityHolder == null)
        {
            abilityHolder = gameObject.AddComponent<AbilityHolder>();
        }

        // Dar habilidad inicial si está configurado
        if (startsWithAbility)
        {
            GiveStartingAbility();
        }

        // Buscar jugador
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log($"Enemigo {gameObject.name} encontró al jugador");
        }
        else
        {
            Debug.LogError("¡No se encontró GameObject con Tag 'Player'!");
        }

        // Inicializar patrulla
        startPosition = transform.position;
        patrolTarget = startPosition.x + patrolDistance;

        // Crear detection point si no existe
        if (detectionPoint == null)
        {
            detectionPoint = transform;
        }

        // Crear edge check point si no existe
        if (edgeCheckPoint == null)
        {
            GameObject edgeCheck = new GameObject("EdgeCheckPoint");
            edgeCheck.transform.SetParent(transform);
            edgeCheck.transform.localPosition = new Vector3(edgeCheckOffset, -0.5f, 0);
            edgeCheckPoint = edgeCheck.transform;
            Debug.LogWarning($"{gameObject.name}: EdgeCheckPoint creado automáticamente. Ajusta su altura (Y) en el inspector.");
        }

        // Estado inicial
        currentState = shouldPatrol ? EnemyState.Patrol : EnemyState.Idle;
    }

    void CreateHealthBar()
    {
        if (HealthBarFactory.Instance != null)
        {
            healthBar = HealthBarFactory.Instance.CreateHealthBar(transform, health, maxHealth, healthBarOffset);
            if (healthBar != null)
            {
                healthBar.hideWhenFull = hideHealthBarWhenFull;
                healthBar.alwaysShow = !hideHealthBarWhenFull;
            }
        }
        else
        {
            // Crear barra de vida programáticamente si no hay factory
            GameObject canvasObj = new GameObject($"HealthBar_{gameObject.name}");
            healthBar = canvasObj.AddComponent<HealthBarUI>();
            healthBar.Initialize(transform, health, maxHealth);
            healthBar.offset = healthBarOffset;
            healthBar.hideWhenFull = hideHealthBarWhenFull;
            healthBar.alwaysShow = !hideHealthBarWhenFull;
        }
    }

    void GiveStartingAbility()
    {
        switch (startingAbility)
        {
            case AbilityType.Dash:
                abilityHolder.SetAbility(new DashAbility());
                Debug.Log($"{gameObject.name} inicia con habilidad: DASH");
                break;
                // Aquí puedes añadir más habilidades en el futuro
        }
    }

    void Update()
    {
        // No hacer nada si está invencible, aturdido, dasheando o stunned crítico
        if (isInvincible || currentState == EnemyState.Stunned || isDashing || isCriticalStunned)
        {
            // Detener movimiento si está stunned
            if (isCriticalStunned)
            {
                rb.linearVelocity = Vector2.zero;
            }
            return;
        }

        // Actualizar timers
        playerIgnoreTimer -= Time.deltaTime;
        attackTimer -= Time.deltaTime;

        // Verificar si está en estado crítico
        CheckCriticalHealth();

        // Verificar borde
        CheckEdge();

        // Calcular detección del jugador
        Vector3 detectionPos = detectionPoint.position;
        float distanceToPlayer = player != null ? Vector2.Distance(detectionPos, player.position) : Mathf.Infinity;

        bool canSeePlayer = CanSeePlayer(detectionPos, distanceToPlayer);
        bool playerDetected = canSeePlayer && (playerIgnoreTimer <= 0f);
        bool playerInAttackRange = playerDetected && distanceToPlayer <= attackRange;

        // Mirar al jugador si está detectado
        if (playerDetected && player != null && currentState != EnemyState.Attack)
        {
            LookAtPlayer();
        }

        // Usar habilidad si puede (solo en combate)
        if ((currentState == EnemyState.Chase || currentState == EnemyState.Guard) &&
            abilityHolder != null && abilityHolder.HasAbility())
        {
            // Usar dash cuando persigue al jugador
            if (currentState == EnemyState.Chase && playerDetected)
            {
                abilityHolder.UseAbility();
            }
        }

        // Máquina de estados
        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdle(playerDetected);
                break;

            case EnemyState.Patrol:
                HandlePatrol(playerDetected);
                break;

            case EnemyState.Guard:
                HandleGuard(playerDetected, playerInAttackRange);
                break;

            case EnemyState.Chase:
                HandleChase(playerDetected, playerInAttackRange);
                break;

            case EnemyState.Attack:
                HandleAttack();
                break;
        }

        UpdateAnimations();
    }

    void CheckCriticalHealth()
    {
        bool wasCritical = isCritical;
        isCritical = (health <= criticalHealthThreshold && health > 0);

        // Si acaba de entrar en estado crítico, iniciar parpadeo y stun
        if (isCritical && !wasCritical)
        {
            StartCoroutine(CriticalHealthSequence());
        }
        else if (!isCritical && wasCritical)
        {
            StopAllCoroutines(); // Detener parpadeo si se cura
            canBeAbsorbed = false;
            isCriticalStunned = false;
        }
    }

    IEnumerator CriticalHealthSequence()
    {
        Debug.Log($"{gameObject.name} está en estado CRÍTICO - Inmóvil por {criticalStunDuration}s");

        // Fase 1: Inmóvil y parpadeando
        isCriticalStunned = true;
        canBeAbsorbed = false;
        rb.linearVelocity = Vector2.zero;
        currentState = EnemyState.Stunned;

        // Parpadeo durante el stun
        float elapsed = 0f;
        while (elapsed < criticalStunDuration)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.red;
            }
            yield return new WaitForSeconds(criticalBlinkSpeed);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
            yield return new WaitForSeconds(criticalBlinkSpeed);

            elapsed += criticalBlinkSpeed * 2f;
        }

        // Fase 2: Ahora puede ser absorbido
        isCriticalStunned = false;
        canBeAbsorbed = true;

        // Forzar mostrar la barra de vida
        if (healthBar != null)
        {
            healthBar.ForceShow();
        }

        Debug.Log($"{gameObject.name} - ¡Ahora puede ser absorbido con E!");

        // Continuar parpadeando hasta que sea absorbido o muera
        while (isCritical && spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(criticalBlinkSpeed);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(criticalBlinkSpeed);
        }
    }

    // ============================================
    // IMPLEMENTACIÓN DE IAbsorbable
    // ============================================
    public bool CanBeAbsorbed()
    {
        return canBeAbsorbed && isCritical;
    }

    public void OnAbsorbed()
    {
        Debug.Log($"{gameObject.name}: ¡Habilidad absorbida/transferida!");

        // Efecto visual de absorción
        if (spriteRenderer != null)
        {
            StartCoroutine(AbsorptionFlash());
        }

        // Si el enemigo ya no tiene habilidad después de la absorción, puede morir
        if (abilityHolder != null && !abilityHolder.HasAbility())
        {
            Debug.Log($"{gameObject.name}: Sin habilidad - muriendo");
            Die();
        }
    }

    IEnumerator AbsorptionFlash()
    {
        for (int i = 0; i < 3; i++)
        {
            if (spriteRenderer != null) spriteRenderer.color = Color.cyan;
            yield return new WaitForSeconds(0.1f);
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
        }
        if (spriteRenderer != null) spriteRenderer.color = originalColor;
    }

    // ============================================
    // IMPLEMENTACIÓN DE IDashExecutor
    // ============================================
    public void PerformDash(float force, float duration)
    {
        if (!isDashing)
        {
            StartCoroutine(EnemyDashCoroutine(force, duration));
        }
    }

    IEnumerator EnemyDashCoroutine(float force, float duration)
    {
        isDashing = true;

        // Dash hacia el jugador si está cerca
        float dashDirection = isFacingRight ? 1f : -1f;

        if (player != null)
        {
            dashDirection = (player.position.x > transform.position.x) ? 1f : -1f;
        }

        // Aplicar velocidad de dash
        rb.linearVelocity = new Vector2(dashDirection * force, 0f);

        // Efecto visual
        if (spriteRenderer != null)
        {
            Color dashColor = new Color(0.3f, 0.8f, 1f);
            spriteRenderer.color = dashColor;
        }

        Debug.Log($"{gameObject.name} ejecutó DASH hacia {(dashDirection > 0 ? "derecha" : "izquierda")}");

        yield return new WaitForSeconds(duration);

        isDashing = false;
        if (spriteRenderer != null && !isInvincible)
        {
            spriteRenderer.color = originalColor;
        }

        // Reducir velocidad
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.3f, rb.linearVelocity.y);
    }

    bool CanSeePlayer(Vector3 detectionPos, float distance)
    {
        if (player == null || distance > detectionRange)
        {
            return false;
        }

        Vector2 directionToPlayer = (player.position - detectionPos).normalized;
        RaycastHit2D hit = Physics2D.Raycast(detectionPos, directionToPlayer, distance, wallLayer | PlayerLayer);

        if (hit.collider != null)
        {
            bool canSee = hit.collider.CompareTag("Player");

            if (showDebugGizmos)
            {
                Debug.DrawLine(detectionPos, hit.point, canSee ? Color.green : Color.red);
            }

            return canSee;
        }

        return false;
    }

    void CheckEdge()
    {
        if (edgeCheckPoint == null) return;

        float direction = isFacingRight ? 1f : -1f;
        Vector2 checkStartPoint = new Vector2(
            transform.position.x + (edgeCheckOffset * direction),
            edgeCheckPoint.position.y
        );

        RaycastHit2D hit = Physics2D.Raycast(checkStartPoint, Vector2.down, edgeCheckDistance, groundLayer);
        isAtEdge = (hit.collider == null);

        if (showEdgeDebug)
        {
            Debug.DrawRay(checkStartPoint, Vector2.down * edgeCheckDistance, isAtEdge ? Color.red : Color.cyan);
        }
    }

    void HandleIdle(bool playerDetected)
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (playerDetected)
        {
            EnterGuardState();
        }
    }

    void HandlePatrol(bool playerDetected)
    {
        if (playerDetected)
        {
            EnterGuardState();
            return;
        }

        if (waitTimer > 0)
        {
            waitTimer -= Time.deltaTime;
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        if (isAtEdge)
        {
            Debug.Log($"{gameObject.name}: Borde detectado, cambiando dirección");
            movingRight = !movingRight;
            waitTimer = waitTimeAtPatrolPoint;
            Flip();
            return;
        }

        float direction = movingRight ? 1f : -1f;
        rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);

        if ((movingRight && !isFacingRight) || (!movingRight && isFacingRight))
        {
            Flip();
        }

        if (movingRight && transform.position.x >= patrolTarget)
        {
            movingRight = false;
            waitTimer = waitTimeAtPatrolPoint;
        }
        else if (!movingRight && transform.position.x <= startPosition.x - patrolDistance)
        {
            movingRight = true;
            waitTimer = waitTimeAtPatrolPoint;
        }
    }

    void HandleGuard(bool playerDetected, bool inAttackRange)
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (!playerDetected)
        {
            Debug.Log($"{gameObject.name}: Jugador fuera de rango");
            ReturnToPatrol();
            return;
        }

        LookAtPlayer();
        guardTimer += Time.deltaTime;

        if (isAtEdge)
        {
            totalEdgeTime += Time.deltaTime;

            if (totalEdgeTime >= maxEdgeWaitTime)
            {
                Debug.Log($"{gameObject.name}: Tiempo máximo en borde alcanzado");
                TurnAroundAndPatrol();
                return;
            }
        }
        else
        {
            totalEdgeTime = 0f;
        }

        if (inAttackRange && guardTimer >= guardTime && attackTimer <= 0)
        {
            ExitGuardState();
            EnterAttackState();
            ResetTimers();
        }
        else if (!inAttackRange && guardTimer >= guardTime)
        {
            if (!isAtEdge)
            {
                ExitGuardState();
                currentState = EnemyState.Chase;
                chaseTimer = 0f;
                totalEdgeTime = 0f;
            }
            else
            {
                edgeWaitTimer += Time.deltaTime;

                if (edgeWaitTimer >= edgeWaitTime)
                {
                    Debug.Log($"{gameObject.name}: Esperó {edgeWaitTime}s en el borde");
                    TurnAroundAndPatrol();
                }
            }
        }

        if (!isAtEdge)
        {
            edgeWaitTimer = 0f;
        }
    }

    void HandleChase(bool playerDetected, bool playerInAttackRange)
    {
        chaseTimer += Time.deltaTime;

        if (chaseTimer >= chaseTimeout)
        {
            Debug.Log($"{gameObject.name}: Timeout de persecución");
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            playerIgnoreTimer = 3f;
            ReturnToPatrol();
            return;
        }

        if (!playerDetected)
        {
            Debug.Log($"{gameObject.name}: Perdió de vista al jugador");
            ReturnToPatrol();
            return;
        }

        if (isAtEdge)
        {
            Debug.Log($"{gameObject.name}: Borde detectado durante persecución");
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            LookAtPlayer();
            EnterGuardState();
            return;
        }

        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * chaseSpeed, rb.linearVelocity.y);

        LookAtPlayer();

        if (playerInAttackRange)
        {
            if (attackTimer <= 0)
            {
                chaseTimer = 0f;
                EnterAttackState();
            }
            else
            {
                EnterGuardState();
            }
        }
    }

    void HandleAttack()
    {
        if (!isAttacking)
        {
            StartCoroutine(PerformAttack());
        }
    }

    void EnterGuardState()
    {
        currentState = EnemyState.Guard;
        guardTimer = 0f;

        if (guardEffect != null)
        {
            guardEffect.SetActive(true);
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = guardColor;
        }

        Debug.Log($"{gameObject.name}: En Guardia");
    }

    void ExitGuardState()
    {
        if (guardEffect != null)
        {
            guardEffect.SetActive(false);
        }

        if (spriteRenderer != null && !isInvincible)
        {
            spriteRenderer.color = originalColor;
        }
    }

    void EnterAttackState()
    {
        currentState = EnemyState.Attack;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = attackColor;
        }

        Debug.Log($"{gameObject.name}: Atacando");
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;

        if (attackEffect != null && attackPoint != null)
        {
            GameObject effect = Instantiate(attackEffect, attackPoint);
            effect.transform.localPosition = Vector3.zero;

            float effectScale = 0.7f;
            effect.transform.localScale = new Vector3(
                (isFacingRight ? 1f : -1f) * effectScale,
                effectScale,
                effectScale
            );

            effect.transform.localRotation = Quaternion.identity;

            ParticleSystem ps = effect.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();

            SpriteRenderer sr = effect.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = 10;

            Destroy(effect, attackDuration + 0.5f);
        }

        if (attackPoint != null)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius);

            foreach (Collider2D hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    MainChar playerScript = hit.GetComponent<MainChar>();
                    if (playerScript != null)
                    {
                        playerScript.TakeDamage(attackDamage);
                        Debug.Log($"{gameObject.name}: Golpeó al jugador por {attackDamage} de daño");
                    }
                }
            }
        }

        yield return new WaitForSeconds(attackDuration);

        isAttacking = false;
        attackTimer = attackCooldown;

        if (spriteRenderer != null && !isInvincible)
        {
            spriteRenderer.color = originalColor;
        }

        EnterGuardState();
    }

    void LookAtPlayer()
    {
        if (player == null) return;

        bool playerOnRight = player.position.x > transform.position.x;

        if ((playerOnRight && !isFacingRight) || (!playerOnRight && isFacingRight))
        {
            Flip();
        }
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;

        Debug.Log($"{gameObject.name}: Volteado - Mirando {(isFacingRight ? "derecha" : "izquierda")}");
    }

    void ReturnToPatrol()
    {
        ExitGuardState();
        currentState = shouldPatrol ? EnemyState.Patrol : EnemyState.Idle;
        ResetTimers();
    }

    void TurnAroundAndPatrol()
    {
        ExitGuardState();
        movingRight = !isFacingRight;
        Flip();
        waitTimer = waitTimeAtPatrolPoint;
        currentState = EnemyState.Patrol;
        ResetTimers();
        playerIgnoreTimer = waitTimeAtPatrolPoint + 2f;
    }

    void ResetTimers()
    {
        edgeWaitTimer = 0f;
        chaseTimer = 0f;
        totalEdgeTime = 0f;
    }

    public void TakeDamage(int damage, float knockbackDirection)
    {
        if (isInvincible || isKnockbackInvincible)
        {
            Debug.Log($"{gameObject.name}: Invencible - Daño ignorado");
            return;
        }

        health -= damage;
        Debug.Log($"{gameObject.name}: Recibió {damage} de daño. Vida: {health}/{maxHealth}");

        // Actualizar barra de vida
        if (healthBar != null)
        {
            healthBar.UpdateHealth(health, maxHealth);
        }

        // Aplicar knockback
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.linearVelocity = new Vector2(
                knockbackForce.x * knockbackDirection,
                knockbackForce.y
            );
        }

        ExitGuardState();
        currentState = EnemyState.Stunned;
        ResetTimers();

        if (health <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(RecoverAndChase());
            StartCoroutine(KnockbackInvincibility());
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name}: Murió");

        // Destruir barra de vida
        if (healthBar != null)
        {
            Destroy(healthBar.gameObject);
        }

        Destroy(gameObject);
    }

    IEnumerator RecoverAndChase()
    {
        isInvincible = true;

        float flashDuration = invincibilityTime / 10f;

        for (int i = 0; i < 5; i++)
        {
            if (spriteRenderer != null) spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(flashDuration);
            if (spriteRenderer != null) spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }

        isInvincible = false;

        if (player != null)
        {
            Debug.Log($"{gameObject.name}: ¡Recuperado! Entrando en modo persecución agresiva");
            currentState = EnemyState.Chase;
            chaseTimer = 0f;
            playerIgnoreTimer = 0f;
            attackTimer = 0f;
        }
        else
        {
            currentState = EnemyState.Idle;
        }
    }

    IEnumerator KnockbackInvincibility()
    {
        isKnockbackInvincible = true;
        yield return new WaitForSeconds(knockbackInvincibilityTime);
        isKnockbackInvincible = false;
    }

    void UpdateAnimations()
    {
        if (animator != null)
        {
            animator.SetBool("isGuarding", currentState == EnemyState.Guard);
            animator.SetBool("isAttacking", currentState == EnemyState.Attack);
            animator.SetFloat("speed", Mathf.Abs(rb.linearVelocity.x));
        }
    }

    void OnDestroy()
    {
        // Limpiar barra de vida al destruir el enemigo
        if (healthBar != null)
        {
            Destroy(healthBar.gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        Vector3 detectionPos = detectionPoint != null ? detectionPoint.position : transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(detectionPos, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(detectionPos, attackRange);

        if (attackPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }

        if (shouldPatrol && Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(
                new Vector2(startPosition.x - patrolDistance, startPosition.y),
                new Vector2(startPosition.x + patrolDistance, startPosition.y)
            );
        }

        if (edgeCheckPoint != null)
        {
            float direction = isFacingRight ? 1f : -1f;
            Vector2 checkStartPoint = new Vector2(
                transform.position.x + (edgeCheckOffset * direction),
                edgeCheckPoint.position.y
            );

            Gizmos.color = isAtEdge ? Color.red : Color.cyan;
            Gizmos.DrawLine(checkStartPoint, checkStartPoint + Vector2.down * edgeCheckDistance);
            Gizmos.DrawWireSphere(checkStartPoint + Vector2.down * edgeCheckDistance, 0.1f);
        }

        if (Application.isPlaying && player != null)
        {
            Vector2 directionToPlayer = (player.position - detectionPos).normalized;
            float distance = Vector2.Distance(detectionPos, player.position);

            if (distance <= detectionRange)
            {
                RaycastHit2D hit = Physics2D.Raycast(detectionPos, directionToPlayer, distance, wallLayer | PlayerLayer);

                if (hit.collider != null)
                {
                    Gizmos.color = hit.collider.CompareTag("Player") ? Color.green : Color.red;
                    Gizmos.DrawLine(detectionPos, hit.point);
                }
            }
        }
    }
}