using System.Collections;
using UnityEngine;

/// <summary>
/// Enemigo a distancia: patrulla, detecta al jugador por visión y dispara
/// proyectiles horizontales. No persigue — mantiene distancia y dispara.
/// </summary>
public class EnemigoRanged : MonoBehaviour, IResettable
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
    public float detectionRange = 10f;
    public float stopShootingRange = 2f;   // deja de disparar si el jugador está muy cerca
    public LayerMask playerLayer, wallLayer, groundLayer;
    public Transform detectionPoint;

    [Header("=== PROYECTIL ===")]
    public GameObject bulletPrefab;        // prefab con Rigidbody2D + Collider2D
    public Transform firePoint;            // punto de disparo (assign en inspector)
    public float bulletSpeed = 8f;
    public int bulletDamage = 1;
    public float shootCooldown = 2f;
    public float windupTime = 0.4f;        // pausa antes de disparar (telegrafía)

    [Header("=== MOVIMIENTO ===")]
    public float moveSpeed = 2f;
    [Range(1f, 20f)]
    public float movementSmoothing = 6f;

    [Header("=== PATRULLA ===")]
    public bool shouldPatrol = true;
    public float patrolDistance = 4f;
    public float waitAtPatrolPoint = 1.5f;

    [Header("=== BARRA DE VIDA ===")]
    public Vector3 healthBarOffset = new Vector3(0f, 1.4f, 0f);

    [Header("=== ANIMACIONES ===")]
    public string animSpeed = "speed";
    public string animIsAiming = "isAiming";
    public string animShootTrigger = "shoot";

    [Header("=== VISUALES ===")]
    public Color aimColor = new Color(1f, 0.6f, 0f);   // color al apuntar

    // ─────────────────────────────────────────────────────────
    // PRIVADOS
    // ─────────────────────────────────────────────────────────
    private enum State { Idle, Patrol, Aim, Shoot, Retreat }
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
    private bool isShooting;
    private bool hasSeenPlayer;

    private float shootTimer, waitTimer, velX, targetVelX;

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
        if (!firePoint) firePoint = transform; // fallback

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

        bool detected = seePlayer || (hasSeenPlayer && dist <= detectionRange * 1.3f);
        bool tooClose = dist <= stopShootingRange;

        switch (state)
        {
            case State.Idle:
                ApplyVelocity(0f);
                if (detected) Enter(tooClose ? State.Retreat : State.Aim);
                break;

            case State.Patrol:
                if (detected) Enter(tooClose ? State.Retreat : State.Aim);
                else Patrol();
                break;

            case State.Aim:
                Aim(detected, tooClose);
                break;

            case State.Shoot:
                if (!isShooting) StartCoroutine(DoShoot());
                break;

            case State.Retreat:
                Retreat(detected, tooClose);
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

        if (Mathf.Abs(transform.position.x - startPos.x) >= patrolDistance)
        {
            movingRight = !movingRight;
            waitTimer = waitAtPatrolPoint;
        }
    }

    void Aim(bool detected, bool tooClose)
    {
        // Si el jugador salió de rango o hay pared, volver a patrulla
        if (!detected) { BackToPatrol(); return; }
        if (tooClose) { Enter(State.Retreat); return; }

        ApplyVelocity(0f);
        if (player) LookAt(player.position.x);

        if (shootTimer <= 0) Enter(State.Shoot);
    }

    IEnumerator DoShoot()
    {
        isShooting = true;
        if (sr) sr.color = aimColor;
        if (anim) anim.SetBool(animIsAiming, true);

        // Windup — telegrafía el disparo
        yield return new WaitForSeconds(windupTime);

        // Verificar que el jugador sigue en rango antes de disparar
        float dist = player ? Vector2.Distance(detectionPoint.position, player.position) : Mathf.Infinity;
        if (player && dist <= detectionRange && dist > stopShootingRange)
        {
            SpawnBullet();
            if (anim) anim.SetTrigger(animShootTrigger);
        }

        yield return new WaitForSeconds(0.1f);

        if (sr) sr.color = originalColor;
        if (anim) anim.SetBool(animIsAiming, false);

        isShooting = false;
        shootTimer = shootCooldown;

        // Volver a apuntar si el jugador sigue visible
        float distAfter = player ? Vector2.Distance(detectionPoint.position, player.position) : Mathf.Infinity;
        bool tooClose = distAfter <= stopShootingRange;
        bool detected = CanSeePlayer(distAfter) || (hasSeenPlayer && distAfter <= detectionRange * 1.3f);

        if (!detected) BackToPatrol();
        else if (tooClose) Enter(State.Retreat);
        else Enter(State.Aim);
    }

    void SpawnBullet()
    {
        if (!bulletPrefab) { Debug.LogWarning("[EnemigoRanged] bulletPrefab no asignado."); return; }

        GameObject b = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
        Rigidbody2D bRb = b.GetComponent<Rigidbody2D>();
        if (bRb)
        {
            // Disparo SIEMPRE horizontal en la dirección que mira el enemigo
            float dir = isFacingRight ? 1f : -1f;
            bRb.linearVelocity = new Vector2(dir * bulletSpeed, 0f);
        }

        // Pasar daño al componente del proyectil si lo tiene
        EnemyBullet bullet = b.GetComponent<EnemyBullet>();
        if (bullet) bullet.damage = bulletDamage;
    }

    void Retreat(bool detected, bool tooClose)
    {
        if (!detected) { BackToPatrol(); return; }
        if (!tooClose) { Enter(State.Aim); return; }

        // Alejarse del jugador
        if (player)
        {
            float dir = transform.position.x > player.position.x ? 1f : -1f;
            if (isAtEdge) dir = -dir;
            ApplyVelocity(dir * moveSpeed);
            AlignFlip(dir);
        }
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
        isShooting = false;
        hasSeenPlayer = false;
        velX = targetVelX = 0f;

        if (sr) sr.color = originalColor;
        if (anim) anim.SetBool(animIsAiming, false);
        rb.linearVelocity = Vector2.zero;

        healthBar?.gameObject.SetActive(true);
        healthBar?.UpdateHealth(currentHealth, maxHealth);

        shootTimer = waitTimer = 0f;
    }

    // ─────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────
    void TickTimers()
    {
        if (shootTimer > 0) shootTimer -= Time.deltaTime;
        if (waitTimer > 0) waitTimer -= Time.deltaTime;
    }

    void Enter(State next)
    {
        state = next;
        if (sr && !isInvincible)
            sr.color = next == State.Aim ? aimColor : originalColor;
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
        if (state != State.Patrol && state != State.Retreat) return;
        float side = isFacingRight ? 1f : -1f;
        Vector2 origin = new Vector2(transform.position.x + side * 0.6f, transform.position.y - 0.1f);
        isAtEdge = !Physics2D.Raycast(origin, Vector2.down, 1f, groundLayer);
    }

    void UpdateAnim()
    {
        if (!anim) return;
        anim.SetFloat(animSpeed, Mathf.Abs(rb.linearVelocity.x));
        anim.SetBool(animIsAiming, state == State.Aim || isShooting);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, stopShootingRange);
        if (firePoint) { Gizmos.color = Color.red; Gizmos.DrawRay(firePoint.position, Vector3.right * 2f); }
    }
}