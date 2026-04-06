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

    [Header("=== RETRASO Y SUAVIZADO ===")]
    public float reactionDelay = 0.5f;
    [Range(1f, 20f)]
    public float movementSmoothing = 5f;
    private float reactionTimer = 0f;
    private float targetVelocityX = 0f;
    private float currentVelocityX = 0f;

    [Header("=== COMPORTAMIENTO ===")]
    public float moveSpeed = 3f;
    public float chaseSpeed = 5f;
    public float guardTime = 0.8f;
    public float attackCooldown = 1.5f;
    public float chaseTimeout = 8f;
    public float extendedChaseRange = 15f;

    [Header("=== DETECCIÓN & HABILIDAD ===")]
    public float detectionRange = 8f;
    public float attackRange = 2f;
    public LayerMask PlayerLayer, wallLayer, groundLayer;
    public Transform detectionPoint, edgeCheckPoint;

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
    public string animIsGuarding = "isGuarding";
    public string animIsAttacking = "isAttacking";
    public string animAttackTrigger = "attack";

    private EnemyState currentState = EnemyState.Idle;
    private enum EnemyState { Idle, Patrol, Guard, Chase, Attack, Stunned, Dashing, WaitingAbsorption }

    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Animator anim;
    private Transform player;
    private HealthBarUI healthBar;

    private Color originalColor;
    private Vector2 startPos;
    private bool isFacingRight = true, movingRight = true, isAtEdge;
    private bool isInvincible, isAttacking;
    private float attackTimer, waitTimer, chaseTimer, playerIgnoreTimer;
    private bool hasSeenPlayer = false;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        if (sr) originalColor = sr.color;
        startPos = transform.position;
        health = maxHealth;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (!detectionPoint) detectionPoint = transform;

        if (AbilityAbsorptionManager.Instance != null)
            AbilityAbsorptionManager.Instance.RegisterResettable(this);

        SetupHealthBar();

        if (GetComponent<Collider2D>() != null)
        {
            PhysicsMaterial2D mat = new PhysicsMaterial2D("EnemyFrictionless");
            mat.friction = 0f;
            mat.bounciness = 0f;
            GetComponent<Collider2D>().sharedMaterial = mat;
        }

        currentState = shouldPatrol ? EnemyState.Patrol : EnemyState.Idle;
    }

    void Update()
    {
        if (isInvincible || currentState == EnemyState.Stunned || isAttacking)
        {
            UpdateAnimations();
            return;
        }

        UpdateTimers();
        CheckEdge();

        bool seePlayer = CanSeePlayer();
        float distToPlayer = player != null ? Vector2.Distance(detectionPoint.position, player.position) : Mathf.Infinity;
        if (seePlayer) hasSeenPlayer = true;

        bool detected = (seePlayer && playerIgnoreTimer <= 0) || (hasSeenPlayer && distToPlayer <= extendedChaseRange);
        bool inRange = seePlayer && distToPlayer <= attackRange;

        switch (currentState)
        {
            case EnemyState.Idle:
                targetVelocityX = 0;
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

        if (!isAttacking) ApplyDelayedMovement();
        UpdateAnimations();
    }

    void ApplyDelayedMovement()
    {
        currentVelocityX = Mathf.Lerp(currentVelocityX, targetVelocityX, movementSmoothing * Time.deltaTime);
        rb.linearVelocity = new Vector2(currentVelocityX, rb.linearVelocity.y);
    }

    void HandleChase(bool detected, bool inRange)
    {
        if (!detected || chaseTimer >= chaseTimeout) { ReturnToPatrol(); return; }
        if (inRange && attackTimer <= 0) { EnterState(EnemyState.Attack); return; }

        if (player) LookAtPlayer();
        if (isAtEdge) { targetVelocityX = 0; return; }

        reactionTimer += Time.deltaTime;
        if (reactionTimer >= reactionDelay)
        {
            float dir = player.position.x > transform.position.x ? 1 : -1;
            targetVelocityX = dir * chaseSpeed;
        }
        else
        {
            targetVelocityX = 0;
        }

        chaseTimer += Time.deltaTime;
    }

    void HandlePatrol()
    {
        if (waitTimer > 0) { targetVelocityX = 0; return; }
        if (isAtEdge) { movingRight = !movingRight; waitTimer = waitTimeAtPatrolPoint; Flip(); return; }

        targetVelocityX = (movingRight ? 1 : -1) * moveSpeed;
    }

    void HandleGuard(bool detected, bool inRange)
    {
        targetVelocityX = 0;
        if (player) LookAtPlayer();
        reactionTimer += Time.deltaTime;

        if (reactionTimer >= guardTime)
        {
            if (detected && inRange) EnterState(EnemyState.Attack);
            else if (detected) EnterState(EnemyState.Chase);
            else ReturnToPatrol();
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y < -0.5f)
                {
                    Rigidbody2D pRb = collision.gameObject.GetComponent<Rigidbody2D>();
                    if (pRb != null)
                    {
                        float pushDir = (collision.transform.position.x > transform.position.x) ? 2f : -2f;
                        pRb.AddForce(new Vector2(pushDir, 0), ForceMode2D.Impulse);
                    }
                }
            }
        }
    }

    public void RestoreFullHealth()
    {
        health = maxHealth;
        if (healthBar != null) healthBar.UpdateHealth(health, maxHealth);
    }

    public void TakeDamage(int damage, int knockbackDirection)
    {
        if (isInvincible) return;
        health -= damage;
        if (healthBar != null) healthBar.UpdateHealth(health, maxHealth);

        currentVelocityX = knockbackDirection * knockbackForce.x;
        rb.linearVelocity = new Vector2(currentVelocityX, knockbackForce.y);

        if (health <= 0) Die();
        else StartCoroutine(InvincibilityCoroutine());
    }

    IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        if (sr) sr.color = Color.red;
        yield return new WaitForSeconds(invincibilityTime);
        if (sr) sr.color = originalColor;
        isInvincible = false;
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;
        targetVelocityX = 0;
        currentVelocityX = 0;
        if (anim) anim.SetTrigger(animAttackTrigger);

        yield return new WaitForSeconds(attackDuration * 0.5f);
        if (attackPoint != null)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius, PlayerLayer);
            foreach (var hit in hits)
                hit.GetComponent<PlayerCore>()?.TakeDamage(attackDamage);
        }
        yield return new WaitForSeconds(attackDuration * 0.5f);
        isAttacking = false;
        attackTimer = attackCooldown;
        EnterState(EnemyState.Chase);
    }

    void Die()
    {
        if (healthBar != null) healthBar.gameObject.SetActive(false);
        if (EnemyManager.Instance != null) EnemyManager.Instance.OnEnemyDeath(gameObject);
        else Destroy(gameObject);
    }

    void EnterState(EnemyState newState)
    {
        if (newState == EnemyState.Chase || newState == EnemyState.Guard) reactionTimer = 0f;
        currentState = newState;
    }

    void UpdateAnimations()
    {
        if (anim == null) return;
        anim.SetFloat(animSpeed, Mathf.Abs(currentVelocityX));
        anim.SetBool(animIsGuarding, currentState == EnemyState.Guard);
        anim.SetBool(animIsAttacking, isAttacking);
    }

    void UpdateTimers()
    {
        if (attackTimer > 0) attackTimer -= Time.deltaTime;
        if (waitTimer > 0) waitTimer -= Time.deltaTime;
        if (playerIgnoreTimer > 0) playerIgnoreTimer -= Time.deltaTime;
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }

    void LookAtPlayer()
    {
        if (!player) return;
        bool playerRight = player.position.x > transform.position.x;
        if ((playerRight && !isFacingRight) || (!playerRight && isFacingRight)) Flip();
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
        Vector2 origin = new Vector2(transform.position.x + (0.5f * (isFacingRight ? 1 : -1)), edgeCheckPoint.position.y);
        isAtEdge = !Physics2D.Raycast(origin, Vector2.down, 1f, groundLayer);
    }

    void SetupHealthBar()
    {
        if (HealthBarFactory.Instance)
            healthBar = HealthBarFactory.Instance.CreateHealthBar(transform, health, maxHealth, healthBarOffset);
    }

    public void ResetState()
    {
        gameObject.SetActive(true);
        RestoreFullHealth();
        transform.position = startPos;
        currentVelocityX = 0;
        targetVelocityX = 0;
        hasSeenPlayer = false;
        EnterState(shouldPatrol ? EnemyState.Patrol : EnemyState.Idle);
    }

    public void PerformDash(float f, float d) { }
    public bool CanBeAbsorbed() => health <= criticalHealthThreshold;
    public void OnAbsorbed() => Die();
    private void ReturnToPatrol() => EnterState(shouldPatrol ? EnemyState.Patrol : EnemyState.Idle);
}