using System.Collections;
using UnityEngine;

/// <summary>
/// Enemigo de melee simple: patrulla, detecta al jugador por visión y lo persigue/ataca.
/// Más ligero que Enemigo.cs — sin dash, sin absorción, sin crítico.
/// </summary>
public class EnemigoMelee : MonoBehaviour, IResettable
{
    // ─────────────────────────────────────────────────────────
    // INSPECTOR
    // ─────────────────────────────────────────────────────────
    [Header("=== SALUD ===")]
    public int maxHealth = 2;
    public float invincibilityTime = 0.4f;
    public Vector2 knockbackForce = new Vector2(3f, 4f);
    public bool isBoss = false;
    public bool IsBoss => isBoss;

    [Header("=== DETECCIÓN ===")]
    public float detectionRange = 7f;
    public float attackRange = 1.2f;
    public LayerMask playerLayer, wallLayer, groundLayer;
    public Transform detectionPoint;   // si es null usa transform

    [Header("=== MOVIMIENTO ===")]
    public float moveSpeed = 2.5f;
    public float chaseSpeed = 4f;
    [Range(1f, 20f)]
    public float movementSmoothing = 6f;
    public float chaseTimeout = 6f;

    [Header("=== PATRULLA ===")]
    public bool shouldPatrol = true;
    public float patrolDistance = 4f;
    public float waitAtPatrolPoint = 1.5f;

    [Header("=== ATAQUE ===")]
    public Transform attackPoint;
    public float attackRadius = 0.8f;
    public int attackDamage = 1;
    public float attackDuration = 0.25f;
    public float attackCooldown = 1.2f;
    public float guardTime = 0.6f;

    [Header("=== BARRA DE VIDA ===")]
    public Vector3 healthBarOffset = new Vector3(0f, 1.2f, 0f);

    [Header("=== ANIMACIONES ===")]
    public string animSpeed = "speed";
    public string animIsGuarding = "isGuarding";
    public string animIsAttacking = "isAttacking";
    public string animAttackTrigger = "attack";

    [Header("=== VISUALES ===")]
    public Color guardColor = Color.yellow;
    public Color attackColor = Color.red;

    // ─────────────────────────────────────────────────────────
    // PRIVADOS
    // ─────────────────────────────────────────────────────────
    private enum State { Idle, Patrol, Guard, Chase, Attack }
    private State state;

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Animator anim;
    private Transform player;
    private HealthBarUI healthBar;

    private int currentHealth;
    private Color originalColor;
    private Vector2 startPos;

    private bool isFacingRight = true;
    private bool movingRight = true;
    private bool isAtEdge;
    private bool isInvincible;
    private bool isAttacking;
    private bool hasSeenPlayer;

    private float guardTimer, attackTimer, waitTimer, chaseTimer, velX, targetVelX;

    // ─────────────────────────────────────────────────────────
    // INIT
    // ─────────────────────────────────────────────────────────
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        if (sr) originalColor = sr.color;
        startPos = transform.position;
        currentHealth = maxHealth;

        if (!detectionPoint) detectionPoint = transform;
        EnsureEdgeCheck();

        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (AbilityAbsorptionManager.Instance != null)
            AbilityAbsorptionManager.Instance.RegisterResettable(this);

        if (HealthBarFactory.Instance)
            healthBar = HealthBarFactory.Instance.CreateHealthBar(
                transform, currentHealth, maxHealth, healthBarOffset);

        state = shouldPatrol ? State.Patrol : State.Idle;
    }

    void OnDestroy()
    {
        if (AbilityAbsorptionManager.Instance != null)
            AbilityAbsorptionManager.Instance.UnregisterResettable(this);
    }

    // ─────────────────────────────────────────────────────────
    // UPDATE
    // ─────────────────────────────────────────────────────────
    void Update()
    {
        if (isInvincible) { UpdateAnim(); return; }

        TickTimers();
        CheckEdge();

        float dist = player ? Vector2.Distance(detectionPoint.position, player.position) : Mathf.Infinity;
        bool seePlayer = CanSeePlayer(dist);
        if (seePlayer) hasSeenPlayer = true;

        bool detected = seePlayer || (hasSeenPlayer && dist <= detectionRange * 1.5f);
        bool inRange = seePlayer && dist <= attackRange;

        switch (state)
        {
            case State.Idle:
                ApplyVelocity(0f);
                if (detected) Enter(State.Guard);
                break;

            case State.Patrol:
                if (detected) Enter(State.Guard);
                else Patrol();
                break;

            case State.Guard:
                Guard(detected, inRange);
                break;

            case State.Chase:
                Chase(detected, inRange);
                break;

            case State.Attack:
                if (!isAttacking) StartCoroutine(DoAttack());
                break;
        }

        UpdateAnim();
    }

    void FixedUpdate()
    {
        velX = Mathf.Lerp(velX, targetVelX, movementSmoothing * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(velX, rb.linearVelocity.y);
    }

    // ─────────────────────────────────────────────────────────
    // ESTADOS
    // ─────────────────────────────────────────────────────────
    void Patrol()
    {
        if (waitTimer > 0) { ApplyVelocity(0f); return; }
        if (isAtEdge) { movingRight = !movingRight; waitTimer = waitAtPatrolPoint; Flip(); return; }

        float dir = movingRight ? 1f : -1f;
        ApplyVelocity(dir * moveSpeed);
        AlignFlip(dir);

        float traveled = Mathf.Abs(transform.position.x - startPos.x);
        if (traveled >= patrolDistance)
        {
            movingRight = !movingRight;
            waitTimer = waitAtPatrolPoint;
        }
    }

    void Guard(bool detected, bool inRange)
    {
        ApplyVelocity(0f);
        if (player) LookAt(player.position.x);
        guardTimer += Time.deltaTime;

        if (guardTimer < guardTime) return;

        if (detected && inRange && attackTimer <= 0) Enter(State.Attack);
        else if (detected) Enter(State.Chase);
        else BackToPatrol();
    }

    void Chase(bool detected, bool inRange)
    {
        if (!detected || chaseTimer >= chaseTimeout) { BackToPatrol(); return; }
        if (inRange && attackTimer <= 0) { Enter(State.Attack); return; }

        if (player) LookAt(player.position.x);
        if (isAtEdge) { ApplyVelocity(0f); return; }

        float dir = player.position.x > transform.position.x ? 1f : -1f;
        ApplyVelocity(dir * chaseSpeed);
        chaseTimer += Time.deltaTime;
    }

    IEnumerator DoAttack()
    {
        isAttacking = true;
        ApplyVelocity(0f);
        if (anim) anim.SetTrigger(animAttackTrigger);
        if (sr) sr.color = attackColor;

        yield return new WaitForSeconds(attackDuration * 0.5f);

        if (attackPoint)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, playerLayer);
            foreach (var h in hits)
                if (h.CompareTag("Player")) h.GetComponent<PlayerCore>()?.TakeDamage(attackDamage);
        }

        yield return new WaitForSeconds(attackDuration * 0.5f);

        if (sr) sr.color = originalColor;
        isAttacking = false;
        attackTimer = attackCooldown;
        Enter(State.Guard);
    }

    // ─────────────────────────────────────────────────────────
    // DAÑO / MUERTE
    // ─────────────────────────────────────────────────────────
    public void TakeDamage(int damage, int knockDir)
    {
        if (isInvincible) return;

        currentHealth -= damage;
        healthBar?.UpdateHealth(currentHealth, maxHealth);
        rb.linearVelocity = new Vector2(knockDir * knockbackForce.x, knockbackForce.y);

        if (currentHealth <= 0) Die();
        else StartCoroutine(Invincibility());
    }

    IEnumerator Invincibility()
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

    void Die()
    {
        healthBar?.gameObject.SetActive(false);
        if (EnemyManager.Instance) EnemyManager.Instance.OnEnemyDeath(gameObject);
        else Destroy(gameObject);
    }

    // ─────────────────────────────────────────────────────────
    // IResettable
    // ─────────────────────────────────────────────────────────
    public void ResetState()
    {
        StopAllCoroutines();
        gameObject.SetActive(true);

        currentHealth = maxHealth;
        transform.position = startPos;
        state = shouldPatrol ? State.Patrol : State.Idle;

        isInvincible = false;
        isAttacking = false;
        hasSeenPlayer = false;
        velX = targetVelX = 0f;

        if (sr) sr.color = originalColor;
        rb.linearVelocity = Vector2.zero;

        healthBar?.gameObject.SetActive(true);
        healthBar?.UpdateHealth(currentHealth, maxHealth);

        guardTimer = attackTimer = waitTimer = chaseTimer = 0f;
    }

    // ─────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────
    void TickTimers()
    {
        if (attackTimer > 0) attackTimer -= Time.deltaTime;
        if (waitTimer > 0) waitTimer -= Time.deltaTime;
        if (chaseTimer > 0 && state == State.Chase) chaseTimer += Time.deltaTime;
    }

    void Enter(State next)
    {
        state = next;
        guardTimer = chaseTimer = 0f;
        if (sr && !isInvincible)
            sr.color = next == State.Guard ? guardColor : originalColor;
    }

    void BackToPatrol() => Enter(shouldPatrol ? State.Patrol : State.Idle);

    void ApplyVelocity(float x) => targetVelX = x;

    void LookAt(float targetX)
    {
        bool right = targetX > transform.position.x;
        if (right != isFacingRight) Flip();
    }

    void AlignFlip(float dir)
    {
        if (dir > 0 && !isFacingRight) Flip();
        if (dir < 0 && isFacingRight) Flip();
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, 1f);
    }

    bool CanSeePlayer(float dist)
    {
        if (!player || dist > detectionRange) return false;
        Vector2 dir = (player.position - detectionPoint.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(detectionPoint.position, dir, detectionRange, wallLayer | playerLayer);
        return hit.collider != null && hit.collider.CompareTag("Player");
    }

    void CheckEdge()
    {
        if (state != State.Patrol && state != State.Chase) return;
        float side = isFacingRight ? 1f : -1f;
        Vector2 origin = new Vector2(transform.position.x + side * 0.6f, transform.position.y - 0.1f);
        isAtEdge = !Physics2D.Raycast(origin, Vector2.down, 1f, groundLayer);
    }

    void EnsureEdgeCheck()
    {
        // No necesita Transform extra — CheckEdge calcula el punto en runtime
    }

    void UpdateAnim()
    {
        if (!anim) return;
        anim.SetFloat(animSpeed, Mathf.Abs(rb.linearVelocity.x));
        anim.SetBool(animIsGuarding, state == State.Guard);
        anim.SetBool(animIsAttacking, isAttacking);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, attackRange);
        if (attackPoint) Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
    }
}