using System.Collections;
using UnityEngine;

public class EnemigoVolador : MonoBehaviour
{
    [Header("=== SALUD ===")]
    public int health = 3;
    // CAMBIO 1: Reducir invencibilidad para que sea más fácil de golpear
    public float invincibilityTime = 0.25f; // Era 0.5f
    private bool isInvincible = false;
    public Vector2 knockbackForce = new Vector2(4f, 3f);

    [Header("=== PROTECCIÓN ANTI-VUELO ===")]
    // CAMBIO 2: Reducir tiempo de invencibilidad por knockback
    public float knockbackInvincibilityTime = 0.15f; // Era 0.3f
    private bool isKnockbackInvincible = false;

    [Header("=== DETECCIÓN ===")]
    public float detectionRange = 10f;
    public float attackRange = 2.5f;
    public LayerMask PlayerLayer;
    public LayerMask wallLayer;
    public Transform detectionPoint;

    [Header("=== COMPORTAMIENTO DE VUELO ===")]
    // CAMBIO 3: Reducir velocidad de movimiento para hacerlo más predecible
    public float moveSpeed = 2f; // Era 3f
    public float chaseSpeed = 3.5f; // Era 5f
    public float fleeSpeed = 5f; // Era 7f
    public float guardTime = 0.8f;
    public float attackCooldown = 1.5f;
    // CAMBIO 4: Reducir tiempo de huida
    public float repositionTime = 1.2f; // Era 2f
    public float repositionDistance = 3f; // Era 5f

    [Header("=== MOVIMIENTO VERTICAL ===")]
    public float hoverHeight = 0.5f;
    // CAMBIO 5: Reducir velocidad vertical
    public float verticalSpeed = 2.5f; // Era 4f
    public float smoothTime = 0.3f;

    [Header("=== PATRULLA AÉREA ===")]
    private bool shouldPatrol = true;
    public Vector2 patrolAreaSize = new Vector2(8f, 4f);
    public float waitTimeAtPatrolPoint = 2f;
    public float patrolPointRadius = 0.5f;

    [Header("=== ATAQUE DIAGONAL ===")]
    public Transform attackPoint;
    public float attackRadius = 1f;
    public int attackDamage = 1;
    public float attackDuration = 0.3f;
    // CAMBIO 6: Reducir velocidad de ataque para que sea más esquivable
    public float diagonalAttackSpeed = 8f; // Era 12f
    public float diagonalDashTime = 0.25f;
    public float attackRepositionTime = 0.4f;
    public float attackRepositionDistance = 3f;

    [Header("=== EFECTOS VISUALES ===")]
    public GameObject guardEffect;
    public GameObject attackEffect;
    public Color guardColor = Color.yellow;
    public Color attackColor = Color.red;
    public Color fleeColor = new Color(1f, 0.5f, 0f);

    [Header("=== DEBUG ===")]
    public bool showDebugGizmos = true;

    private enum EnemyState
    {
        Idle,
        Patrol,
        Guard,
        Alert,
        Chase,
        Attack,
        Flee,
        Reposition,
        Stunned // CAMBIO 7: Nuevo estado para cuando recibe daño
    }

    private EnemyState currentState = EnemyState.Idle;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Transform player;
    private Animator animator;
    private Color originalColor;

    private bool isFacingRight = true;
    private float attackTimer = 0f;
    private float guardTimer = 0f;
    private bool isAttacking = false;
    private Vector2 startPosition;
    private Vector2 currentPatrolTarget;
    private float waitTimer = 0f;
    private float fleeTimer = 0f;
    private Vector2 fleeDirection;
    private Vector2 velocitySmooth = Vector2.zero;
    private bool hasReachedRepositionDistance = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        if (rb != null)
        {
            rb.gravityScale = 0f;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            Debug.Log($"Enemigo Volador {gameObject.name} encontró al jugador");
        }
        else
        {
            Debug.LogError("¡No se encontró GameObject con Tag 'Player'!");
        }

        startPosition = transform.position;
        GenerateNewPatrolPoint();

        if (detectionPoint == null)
        {
            detectionPoint = transform;
        }

        if (attackPoint == null)
        {
            GameObject attackPt = new GameObject("AttackPoint");
            attackPt.transform.SetParent(transform);
            attackPt.transform.localPosition = new Vector3(1f, 0f, 0f);
            attackPoint = attackPt.transform;
        }

        currentState = shouldPatrol ? EnemyState.Patrol : EnemyState.Idle;
    }

    void Update()
    {
        // CAMBIO 8: No bloquear Update durante invencibilidad
        attackTimer -= Time.deltaTime;

        Vector3 detectionPos = detectionPoint.position;
        float distanceToPlayer = player != null ? Vector2.Distance(detectionPos, player.position) : Mathf.Infinity;

        bool canSeePlayer = CanSeePlayer(detectionPos, distanceToPlayer);
        bool playerInAttackRange = canSeePlayer && distanceToPlayer <= attackRange;

        if (canSeePlayer && player != null && currentState != EnemyState.Attack &&
            currentState != EnemyState.Flee && currentState != EnemyState.Reposition &&
            currentState != EnemyState.Stunned)
        {
            LookAtPlayer();
        }

        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdle(canSeePlayer);
                break;

            case EnemyState.Patrol:
                HandlePatrol(canSeePlayer);
                break;

            case EnemyState.Guard:
                HandleGuard(canSeePlayer, playerInAttackRange);
                break;

            case EnemyState.Alert:
                HandleAlert(canSeePlayer, playerInAttackRange);
                break;

            case EnemyState.Chase:
                HandleChase(canSeePlayer, playerInAttackRange);
                break;

            case EnemyState.Attack:
                HandleAttack();
                break;

            case EnemyState.Flee:
                HandleFlee();
                break;

            case EnemyState.Reposition:
                HandleReposition(canSeePlayer);
                break;

            case EnemyState.Stunned:
                // Durante stunned, solo esperar a que termine la invencibilidad
                break;
        }

        UpdateAnimations();
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

    void HandleIdle(bool playerDetected)
    {
        rb.linearVelocity = Vector2.zero;

        if (playerDetected)
        {
            EnterAlertState();
        }
    }

    void HandlePatrol(bool playerDetected)
    {
        if (playerDetected)
        {
            EnterAlertState();
            return;
        }

        if (waitTimer > 0)
        {
            waitTimer -= Time.deltaTime;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float minHeight = startPosition.y - patrolAreaSize.y / 2f;
        if (transform.position.y < minHeight)
        {
            Vector2 upwardDirection = Vector2.up;
            rb.linearVelocity = upwardDirection * moveSpeed;
            return;
        }

        Vector2 direction = (currentPatrolTarget - (Vector2)transform.position).normalized;
        Vector2 targetVelocity = direction * moveSpeed;
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref velocitySmooth, smoothTime);

        if ((direction.x > 0 && !isFacingRight) || (direction.x < 0 && isFacingRight))
        {
            Flip();
        }

        if (Vector2.Distance(transform.position, currentPatrolTarget) < patrolPointRadius)
        {
            waitTimer = waitTimeAtPatrolPoint;
            GenerateNewPatrolPoint();
        }
    }

    void HandleGuard(bool playerDetected, bool inAttackRange)
    {
        if (player != null)
        {
            float targetY = player.position.y + hoverHeight;
            Vector2 targetPosition = new Vector2(transform.position.x, targetY);
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            rb.linearVelocity = new Vector2(0, direction.y * verticalSpeed * 0.5f);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (!playerDetected)
        {
            Debug.Log($"{gameObject.name}: Jugador fuera de rango");
            ReturnToPatrol();
            return;
        }

        LookAtPlayer();
        guardTimer += Time.deltaTime;

        if (inAttackRange && guardTimer >= guardTime && attackTimer <= 0)
        {
            ExitGuardState();
            EnterAttackState();
            guardTimer = 0f;
        }
        else if (!inAttackRange && guardTimer >= guardTime)
        {
            ExitGuardState();
            EnterChaseState();
        }
    }

    void HandleAlert(bool playerDetected, bool inAttackRange)
    {
        if (!playerDetected)
        {
            ReturnToPatrol();
            return;
        }

        if (player != null)
        {
            float targetY = player.position.y + hoverHeight;
            Vector2 targetPosition = new Vector2(player.position.x, targetY);
            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;

            Vector2 targetVelocity = direction * (chaseSpeed * 0.8f);
            rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref velocitySmooth, smoothTime * 0.5f);

            LookAtPlayer();

            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer <= detectionRange * 0.6f)
            {
                EnterGuardState();
            }
        }
    }

    void HandleChase(bool playerDetected, bool playerInAttackRange)
    {
        if (!playerDetected)
        {
            Debug.Log($"{gameObject.name}: Perdió de vista al jugador");
            ReturnToPatrol();
            return;
        }

        if (player != null)
        {
            float targetY = player.position.y + hoverHeight;
            Vector2 targetPosition = new Vector2(player.position.x, targetY);

            Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
            Vector2 targetVelocity = direction * chaseSpeed;
            rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref velocitySmooth, smoothTime);

            LookAtPlayer();

            if (playerInAttackRange && attackTimer <= 0)
            {
                EnterAttackState();
            }
        }
    }

    void HandleAttack()
    {
        if (!isAttacking)
        {
            StartCoroutine(PerformDiagonalAttack());
        }
    }

    void HandleFlee()
    {
        fleeTimer += Time.deltaTime;

        Vector2 targetVelocity = fleeDirection * fleeSpeed;
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, targetVelocity, ref velocitySmooth, smoothTime * 0.5f);

        if (player != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer >= repositionDistance)
            {
                hasReachedRepositionDistance = true;
            }
        }

        if (fleeTimer >= repositionTime && hasReachedRepositionDistance)
        {
            EnterRepositionState();
        }
    }

    void HandleReposition(bool playerDetected)
    {
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime * 3f);

        if (rb.linearVelocity.magnitude < 0.1f)
        {
            if (playerDetected)
            {
                EnterAlertState();
            }
            else
            {
                ReturnToPatrol();
            }
        }
    }

    void EnterAlertState()
    {
        currentState = EnemyState.Alert;
        guardTimer = 0f;
        Debug.Log($"{gameObject.name}: ¡Alerta! Jugador detectado");
    }

    void EnterGuardState()
    {
        currentState = EnemyState.Guard;
        guardTimer = 0f;

        if (guardEffect != null)
        {
            guardEffect.SetActive(true);
        }

        if (spriteRenderer != null && !isInvincible)
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

    void EnterChaseState()
    {
        currentState = EnemyState.Chase;
        Debug.Log($"{gameObject.name}: Persiguiendo");
    }

    void EnterAttackState()
    {
        currentState = EnemyState.Attack;

        if (spriteRenderer != null && !isInvincible)
        {
            spriteRenderer.color = attackColor;
        }

        Debug.Log($"{gameObject.name}: Atacando");
    }

    void EnterFleeState()
    {
        currentState = EnemyState.Flee;
        fleeTimer = 0f;
        hasReachedRepositionDistance = false;

        if (player != null)
        {
            fleeDirection = ((Vector2)transform.position - (Vector2)player.position).normalized;
        }
        else
        {
            fleeDirection = isFacingRight ? Vector2.left : Vector2.right;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = fleeColor;
        }

        Debug.Log($"{gameObject.name}: ¡Huyendo para reposicionarse!");
    }

    void EnterRepositionState()
    {
        currentState = EnemyState.Reposition;

        if (spriteRenderer != null && !isInvincible)
        {
            spriteRenderer.color = originalColor;
        }

        Debug.Log($"{gameObject.name}: Reposicionándose");
    }

    IEnumerator PerformDiagonalAttack()
    {
        isAttacking = true;

        if (player == null)
        {
            isAttacking = false;
            EnterGuardState();
            yield break;
        }

        Vector2 startPos = transform.position;
        Vector2 targetPos = player.position;
        Vector2 diagonalDirection = (targetPos - startPos).normalized;

        float dashTimer = 0f;
        while (dashTimer < diagonalDashTime)
        {
            rb.linearVelocity = diagonalDirection * diagonalAttackSpeed;
            dashTimer += Time.deltaTime;
            yield return null;
        }

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
            if (ps != null)
            {
                ps.Play();
            }

            SpriteRenderer sr = effect.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 10;
            }

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

        Vector2 repositionDirection = -diagonalDirection;
        float repositionTimer = 0f;

        while (repositionTimer < attackRepositionTime)
        {
            rb.linearVelocity = repositionDirection * (diagonalAttackSpeed * 0.8f);
            repositionTimer += Time.deltaTime;
            yield return null;
        }

        float slowDownTimer = 0f;
        float slowDownDuration = 0.2f;
        Vector2 currentVel = rb.linearVelocity;

        while (slowDownTimer < slowDownDuration)
        {
            slowDownTimer += Time.deltaTime;
            float t = slowDownTimer / slowDownDuration;
            rb.linearVelocity = Vector2.Lerp(currentVel, Vector2.zero, t);
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;

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
    }

    void ReturnToPatrol()
    {
        ExitGuardState();
        currentState = shouldPatrol ? EnemyState.Patrol : EnemyState.Idle;
        guardTimer = 0f;
    }

    void GenerateNewPatrolPoint()
    {
        float randomX = startPosition.x + Random.Range(-patrolAreaSize.x / 2f, patrolAreaSize.x / 2f);
        float minY = startPosition.y - patrolAreaSize.y / 4f;
        float maxY = startPosition.y + patrolAreaSize.y / 2f;
        float randomY = Random.Range(minY, maxY);
        currentPatrolTarget = new Vector2(randomX, randomY);

        Debug.Log($"{gameObject.name}: Nuevo punto de patrulla en {currentPatrolTarget}");
    }

    // CAMBIO 9: Método TakeDamage mejorado
    public void TakeDamage(int damage, float knockbackDirection)
    {
        if (isInvincible)
        {
            Debug.Log($"{gameObject.name}: Invencible - Daño ignorado");
            return;
        }

        health -= damage;
        Debug.Log($"{gameObject.name}: Recibió {damage} de daño. Vida: {health}");

        // CAMBIO 10: Aplicar knockback más fuerte
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.linearVelocity = new Vector2(
                knockbackForce.x * knockbackDirection * 1.5f, // Más knockback horizontal
                knockbackForce.y * 1.2f // Más knockback vertical
            );
        }

        ExitGuardState();

        if (health <= 0)
        {
            Die();
        }
        else
        {
            // CAMBIO 11: Entrar en estado Stunned al recibir daño
            currentState = EnemyState.Stunned;
            StartCoroutine(StunAndRecover());
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name}: Murió");
        Destroy(gameObject);
    }

    // CAMBIO 12: Nueva corrutina de stun simplificada
    IEnumerator StunAndRecover()
    {
        isInvincible = true;

        float flashDuration = invincibilityTime / 8f;

        // Flash visual
        for (int i = 0; i < 4; i++)
        {
            if (spriteRenderer != null) spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(flashDuration);
            if (spriteRenderer != null) spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }

        isInvincible = false;

        // Después del stun, entrar en estado de alerta para buscar al jugador
        if (player != null)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            if (distanceToPlayer <= detectionRange)
            {
                EnterAlertState();
            }
            else
            {
                ReturnToPatrol();
            }
        }
        else
        {
            ReturnToPatrol();
        }
    }

    void UpdateAnimations()
    {
        if (animator != null)
        {
            animator.SetBool("isGuarding", currentState == EnemyState.Guard);
            animator.SetBool("isAttacking", currentState == EnemyState.Attack);
            animator.SetBool("isFleeing", currentState == EnemyState.Flee);
            animator.SetFloat("speed", rb.linearVelocity.magnitude);
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

        if (shouldPatrol)
        {
            Vector2 center = Application.isPlaying ? startPosition : (Vector2)transform.position;
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireCube(center, new Vector3(patrolAreaSize.x, patrolAreaSize.y, 0f));

            if (Application.isPlaying)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(currentPatrolTarget, patrolPointRadius);
                Gizmos.DrawLine(transform.position, currentPatrolTarget);
            }
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(detectionPos, repositionDistance);

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