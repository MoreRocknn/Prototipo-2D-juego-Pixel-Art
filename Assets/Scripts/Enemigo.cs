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

        if (startsWithAbility)
        {
            DashAbility enemyDash = new DashAbility();
            enemyDash.limitedUses = false;
            abilityHolder.SetAbility(enemyDash);
        }

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

        if (startsWithAbility && abilityHolder != null)
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

        if (currentState == EnemyState.Chase && detected && abilityHolder.HasAbility() && dashCooldownTimer <= 0)
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
                if (!isAttacking) StartCoroutine(PerformAttack());
                break;
        }

        UpdateAnimations();
    }

    void UpdateAnimations()
    {
        if (!anim || !anim.runtimeAnimatorController) return;

        anim.SetBool(animIsGrounded, IsGrounded());
        anim.SetBool(animIsGuarding, currentState == EnemyState.Guard);
        anim.SetBool(animIsAttacking, currentState == EnemyState.Attack);
        anim.SetBool(animIsDashing, isDashing);
        anim.SetBool(animIsCritical, isCritical);
        anim.SetBool(animIsStunned, currentState == EnemyState.Stunned || isCriticalStunned || currentState == EnemyState.WaitingAbsorption);
        anim.SetFloat(animSpeed, Mathf.Abs(rb.linearVelocity.x));
    }

    bool IsGrounded()
    {
        Vector2 origin = new Vector2(transform.position.x, transform.position.y - 0.5f);
        return Physics2D.Raycast(origin, Vector2.down, 0.2f, groundLayer);
    }

    Vector2 PredictPlayerPosition()
    {
        if (!player || !playerRb) return player ? (Vector2)player.position : transform.position;
        Vector2 playerVelocity = playerRb.linearVelocity;
        Vector2 currentPos = player.position;
        return currentPos + (playerVelocity * dashPredictionTime);
    }

    IEnumerator PerformAttack()
    {
        isAttacking = true;
        SetVelocity(0);
        if (anim) anim.SetTrigger(animAttackTrigger);

        if (attackEffect && attackPoint)
        {
            GameObject obj = Instantiate(attackEffect, attackPoint);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localScale = new Vector3((isFacingRight ? 1 : -1) * 0.7f, 0.7f, 0.7f);
            Destroy(obj, attackDuration + 0.5f);
        }
        if (sr) sr.color = attackColor;

        yield return null;
        yield return new WaitForSeconds(attackDuration * 0.5f);

        if (attackPoint != null)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackRadius);
            foreach (var hit in hits)
            {
                if (hit.CompareTag("Player")) hit.GetComponent<MainChar>()?.TakeDamage(attackDamage);
            }
        }

        yield return new WaitForSeconds(attackDuration * 0.5f);
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

    void HandleGuard(bool detected, bool inRange, bool seePlayer)
    {
        SetVelocity(0);
        if (!detected) { hasSeenPlayer = false; ReturnToPatrol(); return; }
        if (player) LookAtPlayer();

        if (isAtEdge)
        {
            totalEdgeTime += Time.deltaTime;
            if (totalEdgeTime >= maxEdgeWaitTime) { hasSeenPlayer = false; TurnAroundAndPatrol(); return; }
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
                    if (edgeWaitTimer >= edgeWaitTime) { hasSeenPlayer = false; TurnAroundAndPatrol(); }
                }
            }
        }
        if (!isAtEdge) edgeWaitTimer = 0;
    }

    void HandleChase(bool detected, bool inRange, bool seePlayer)
    {
        if (chaseTimer >= chaseTimeout || !detected) { playerIgnoreTimer = 3f; hasSeenPlayer = false; ReturnToPatrol(); return; }
        if (isAtEdge) { SetVelocity(0); LookAtPlayer(); EnterState(EnemyState.Guard); return; }

        LookAtPlayer();
        Vector2 targetPos = seePlayer ? (Vector2)player.position : lastKnownPlayerPosition;
        Vector2 dir = (targetPos - (Vector2)transform.position).normalized;
        SetVelocity(dir.x * chaseSpeed);

        if (inRange)
        {
            if (attackTimer <= 0) EnterState(EnemyState.Attack);
            else EnterState(EnemyState.Guard);
        }
    }

    // ========== FIX BUG 3: TakeDamage corregido ==========
    public void TakeDamage(int damage, float knockbackDir)
    {
        // Si está inmortal (esperando absorción), ignorar daño
        if (isImmortalForAbsorption)
        {
            Debug.Log("¡Enemigo inmortal! Solo puede ser absorbido");
            return;
        }

        if (isInvincible) return;

        // === FIX BUG 3: Prevenir muerte si tiene habilidad para absorber ===
        int newHealth = health - damage;

        // Si el daño lo mataría Y tiene dash para absorber, dejarlo en el umbral crítico
        if (newHealth <= 0 && immortalWhenCriticalWithDash && abilityHolder != null && abilityHolder.HasAbility())
        {
            health = criticalHealthThreshold; // Dejarlo en 1 HP (o el umbral crítico configurado)
            Debug.Log($"¡Daño letal prevenido! {gameObject.name} entra en estado crítico para absorción");
        }
        else
        {
            health = newHealth;
        }

        if (healthBar) healthBar.UpdateHealth(health, maxHealth);
        if (anim) anim.SetTrigger(animTakeDamage);
        rb.linearVelocity = new Vector2(knockbackForce.x * knockbackDir, knockbackForce.y);

        if (!isCritical) currentState = EnemyState.Stunned;
        ResetTimers();

        // Solo morir si realmente la vida llegó a 0 (sin protección de absorción)
        if (health <= 0) Die();
        else if (!isCritical) StartCoroutine(RecoverAndChase());
    }

    public bool CanBeAbsorbed()
    {
        return isCritical && abilityHolder != null && abilityHolder.HasAbility();
    }

    public void OnAbsorbed()
    {
        Debug.Log("Enemigo Absorbido");

        isImmortalForAbsorption = false;

        StartCoroutine(FlashRoutine(dashColor));
        if (player != null)
        {
            MainChar playerScript = player.GetComponent<MainChar>();
            SpriteRenderer playerSr = player.GetComponent<SpriteRenderer>();
            if (playerScript != null && playerSr != null)
                playerScript.StartCoroutine(PlayerAbsorptionFlash(playerSr));
        }

        if (abilityHolder && !abilityHolder.HasAbility())
            Die();
    }

    IEnumerator PlayerAbsorptionFlash(SpriteRenderer playerSr)
    {
        Color oldColor = playerSr.color;
        for (int i = 0; i < 4; i++)
        {
            playerSr.color = dashColor;
            yield return new WaitForSeconds(0.08f);
            playerSr.color = Color.white;
            yield return new WaitForSeconds(0.08f);
        }
        playerSr.color = oldColor;
    }

    public void PerformDash(float force, float duration) => StartCoroutine(DashRoutine(force, duration));

    IEnumerator DashRoutine(float force, float duration)
    {
        isDashing = true;
        dashCooldownTimer = dashCooldownTime;
        currentState = EnemyState.Dashing;

        float dir;
        if (isDashingToPlayer && player)
        {
            Vector2 predictedPos = PredictPlayerPosition();
            dir = (predictedPos.x > transform.position.x) ? 1 : -1;
            if ((dir > 0 && !isFacingRight) || (dir < 0 && isFacingRight)) Flip();
        }
        else dir = isFacingRight ? 1 : -1;

        SetVelocity(dir * dashForce);
        if (sr) sr.color = dashColor;
        if (showDashTrail) StartCoroutine(SpawnDashTrail(duration));

        float elapsed = 0f;
        while (elapsed < duration && isDashing)
        {
            if (IsGoingToHitWall(dir)) break;
            if (!canDashThroughPlayer && player)
            {
                float distToPlayer = Vector2.Distance(transform.position, player.position);
                if (distToPlayer <= attackRange)
                {
                    if (attackTimer <= 0)
                    {
                        EnterState(EnemyState.Attack);
                        break;
                    }
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        SetVelocity(rb.linearVelocity.x * 0.3f);
        isDashing = false;
        isDashingToPlayer = false;

        if (sr && currentState == EnemyState.Guard)
            sr.color = guardColor;
        else if (sr && !isInvincible && !isCritical)
            sr.color = originalColor;
        else if (sr && isImmortalForAbsorption)
            sr.color = immortalColor;

        if (currentState == EnemyState.WaitingAbsorption)
        {
            yield break;
        }

        if (player && Vector2.Distance(transform.position, player.position) <= detectionRange)
            EnterState(EnemyState.Chase);
        else
            EnterState(EnemyState.Guard);
    }

    bool IsGoingToHitWall(float dir)
    {
        Vector2 origin = transform.position;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * dir, 1f, wallLayer);
        return hit.collider != null;
    }

    IEnumerator SpawnDashTrail(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration && isDashing)
        {
            GameObject ghost = new GameObject("Ghost");
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

    void CheckCriticalHealth()
    {
        bool wasCritical = isCritical;
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
            Debug.Log("Enemigo esperando absorción - NO puede atacar");
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

    IEnumerator RecoverAndChase()
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
        EnterState(player ? EnemyState.Chase : EnemyState.Idle);
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
        Debug.Log($"{gameObject.name} vida restaurada: {health}/{maxHealth}");
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

    IEnumerator DeathSequence()
    {
        rb.linearVelocity = Vector2.zero;
        enabled = false;
        yield return new WaitForSeconds(0.5f);
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