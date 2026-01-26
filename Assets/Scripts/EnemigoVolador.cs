using System.Collections;
using UnityEngine;

public class EnemigoVolador : MonoBehaviour
{
    [Header("=== SALUD ===")]
    public int health = 3;
    public int maxHealth = 3;
    public float invincibilityTime = 0.25f;
    public Vector2 knockbackForce = new Vector2(4f, 3f);

    [Header("=== BARRA DE VIDA ===")]
    public Vector3 healthBarOffset = new Vector3(0, 2f, 0);  // MÁS ALTO
    public bool hideHealthBarWhenFull = true;
    private HealthBarUI healthBar;

    [Header("=== DETECCIÓN ===")]
    public float detectionRange = 10f;
    public float attackRange = 2.5f;
    public LayerMask PlayerLayer;
    public LayerMask wallLayer;
    public Transform detectionPoint;

    [Header("=== MOVIMIENTO ===")]
    public float moveSpeed = 2f;
    public float chaseSpeed = 3.5f;
    public float fleeSpeed = 5f;
    public float verticalSpeed = 2.5f;
    public float smoothTime = 0.3f;

    [Header("=== PATRULLA ===")]
    public Vector2 patrolAreaSize = new Vector2(8f, 4f);
    public float waitTimeAtPatrolPoint = 2f;

    [Header("=== ATAQUE HORIZONTAL ===")]
    public Transform attackPoint;
    public float attackRadius = 1f;
    public int attackDamage = 1;
    public float attackSpeed = 12f;
    public float attackDuration = 0.25f;
    public float attackCooldown = 1.5f;
    public float guardTime = 0.8f;
    [Range(0.1f, 1f)] public float alignmentTolerance = 0.5f;

    [Header("=== COLORES ===")]
    public Color guardColor = Color.yellow;
    public Color attackColor = Color.red;
    public Color hurtColor = Color.white;

    [Header("=== ANIMACIONES ===")]
    public string animSpeed = "speed";
    public string animState = "state"; // 0=idle, 1=move, 2=chase, 3=guard, 4=attack, 5=hurt, 6=dead

    // Estado
    private enum State { Idle, Patrol, Alert, Guard, Chase, Attack, Flee, Stunned }
    private State state = State.Idle;

    // Referencias
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Transform player;
    private Animator anim;
    private Color originalColor;

    // Variables internas
    private Vector2 startPos, patrolTarget, velocity;
    private float attackTimer, guardTimer, waitTimer, minY, maxY;
    private bool isFacingRight = true, isInvincible, isAttacking;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        if (sr) originalColor = sr.color;
        if (rb) rb.gravityScale = 0f;

        // Asegurar que la vida empieza al máximo
        health = maxHealth;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        startPos = transform.position;

        minY = startPos.y - patrolAreaSize.y / 2f;
        maxY = startPos.y + patrolAreaSize.y / 2f;

        if (!detectionPoint) detectionPoint = transform;
        if (!attackPoint) CreateAttackPoint();

        SetupHealthBar();
        GeneratePatrolPoint();
        state = State.Patrol;
    }

    void SetupHealthBar()
    {
        if (HealthBarFactory.Instance)
        {
            healthBar = HealthBarFactory.Instance.CreateHealthBar(transform, health, maxHealth, healthBarOffset);
        }
        else
        {
            // Fallback sin Factory
            GameObject hbObj = new GameObject($"HealthBar_{name}");
            healthBar = hbObj.AddComponent<HealthBarUI>();
            healthBar.offset = healthBarOffset;
            healthBar.Initialize(transform, health, maxHealth);
        }

        // IMPORTANTE: La barra empieza OCULTA
        if (healthBar)
        {
            healthBar.alwaysShow = !hideHealthBarWhenFull;
        }
    }

    void CreateAttackPoint()
    {
        GameObject pt = new GameObject("AttackPoint");
        pt.transform.SetParent(transform);
        pt.transform.localPosition = new Vector3(1f, 0f, 0f);
        attackPoint = pt.transform;
    }

    void Update()
    {
        attackTimer -= Time.deltaTime;

        if (isInvincible || state == State.Stunned) { UpdateAnim(); return; }
        if (isAttacking) return;

        bool canSee = CanSeePlayer();
        bool inRange = canSee && Vector2.Distance(transform.position, player.position) <= attackRange;
        bool aligned = player && Mathf.Abs(transform.position.y - player.position.y) < alignmentTolerance;

        if (canSee && state != State.Attack && state != State.Flee)
            LookAtPlayer();

        switch (state)
        {
            case State.Idle:
            case State.Patrol:
                if (canSee) { state = State.Alert; break; }
                Patrol();
                break;
            case State.Alert:
                if (!canSee) { state = State.Patrol; break; }
                MoveToward(new Vector2(player.position.x, ClampY(player.position.y)), chaseSpeed * 0.8f);
                if (Vector2.Distance(transform.position, player.position) <= detectionRange * 0.6f)
                    state = State.Guard;
                break;
            case State.Guard:
                Guard(canSee, inRange, aligned);
                break;
            case State.Chase:
                Chase(canSee, inRange, aligned);
                break;
            case State.Attack:
                if (!isAttacking) StartCoroutine(DoAttack());
                break;
            case State.Flee:
                Flee();
                break;
        }

        UpdateAnim();
    }

    void Patrol()
    {
        if (waitTimer > 0) { waitTimer -= Time.deltaTime; rb.linearVelocity = Vector2.zero; return; }

        ClampPosition();
        MoveToward(patrolTarget, moveSpeed);

        if (Vector2.Distance(transform.position, patrolTarget) < 0.5f)
        {
            waitTimer = waitTimeAtPatrolPoint;
            GeneratePatrolPoint();
        }
    }

    void Guard(bool canSee, bool inRange, bool aligned)
    {
        if (!canSee) { state = State.Patrol; return; }

        // Alinearse verticalmente
        float targetY = ClampY(player.position.y);
        float dirY = Mathf.Sign(targetY - transform.position.y);
        rb.linearVelocity = new Vector2(0, dirY * verticalSpeed * 0.5f);

        if (Mathf.Abs(transform.position.y - targetY) < 0.1f)
            rb.linearVelocity = Vector2.zero;

        guardTimer += Time.deltaTime;
        if (guardTimer >= guardTime)
        {
            if (inRange && aligned && attackTimer <= 0) { state = State.Attack; guardTimer = 0; }
            else if (!inRange) { state = State.Chase; guardTimer = 0; }
        }

        if (sr && !isInvincible) sr.color = guardColor;
    }

    void Chase(bool canSee, bool inRange, bool aligned)
    {
        if (!canSee) { state = State.Patrol; return; }

        Vector2 target = new Vector2(player.position.x, ClampY(player.position.y));
        MoveToward(target, chaseSpeed);

        if (inRange && aligned && attackTimer <= 0)
            state = State.Attack;
    }

    void Flee()
    {
        Vector2 dir = ((Vector2)transform.position - (Vector2)player.position).normalized;
        if (transform.position.y < minY) dir.y = 1;
        else if (transform.position.y > maxY) dir.y = -1;

        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, dir * fleeSpeed, ref velocity, smoothTime * 0.5f);

        if (Vector2.Distance(transform.position, player.position) >= detectionRange * 0.8f)
            state = CanSeePlayer() ? State.Alert : State.Patrol;
    }

    IEnumerator DoAttack()
    {
        isAttacking = true;
        if (player == null) { isAttacking = false; state = State.Guard; yield break; }

        float dirX = player.position.x > transform.position.x ? 1f : -1f;
        if ((dirX > 0 && !isFacingRight) || (dirX < 0 && isFacingRight)) Flip();

        // Carga
        rb.linearVelocity = Vector2.zero;
        if (sr) sr.color = Color.white;
        yield return new WaitForSeconds(0.12f);
        if (sr) sr.color = attackColor;

        // Embestida
        float t = 0;
        while (t < attackDuration)
        {
            rb.linearVelocity = new Vector2(dirX * attackSpeed, 0);
            t += Time.deltaTime;

            // Detectar golpe durante la embestida
            if (attackPoint)
            {
                Collider2D hit = Physics2D.OverlapCircle(attackPoint.position, attackRadius, PlayerLayer);
                if (hit && hit.CompareTag("Player"))
                {
                    hit.GetComponent<MainChar>()?.TakeDamage(attackDamage);
                    break;
                }
            }
            yield return null;
        }

        // Retroceso
        t = 0;
        while (t < 0.3f)
        {
            rb.linearVelocity = new Vector2(-dirX * attackSpeed * 0.5f, 0);
            t += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        isAttacking = false;
        attackTimer = attackCooldown;
        if (sr) sr.color = originalColor;
        state = State.Guard;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"{name}: Trigger con {other.name} (tag: {other.tag})");
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        Debug.Log($"{name}: Colisión con {other.gameObject.name} (tag: {other.gameObject.tag})");
    }

    // Sobrecarga para compatibilidad
    public void TakeDamage(int damage)
    {
        float knockbackDir = player ? Mathf.Sign(transform.position.x - player.position.x) : 1f;
        TakeDamage(damage, knockbackDir);
    }

    public void TakeDamage(int damage, float knockbackDir)
    {
        if (isInvincible)
        {
            Debug.Log($"{name}: Invencible, daño ignorado");
            return;
        }

        health -= damage;
        Debug.Log($"{name}: Recibió {damage} daño. Vida: {health}/{maxHealth}");

        // MOSTRAR LA BARRA DE VIDA
        if (healthBar)
        {
            healthBar.UpdateHealth(health, maxHealth);
        }

        if (sr) sr.color = hurtColor;
        rb.linearVelocity = new Vector2(knockbackForce.x * knockbackDir, knockbackForce.y);

        if (health <= 0) Die();
        else StartCoroutine(Stun());
    }

    IEnumerator Stun()
    {
        isInvincible = true;
        state = State.Stunned;

        for (int i = 0; i < 4; i++)
        {
            if (sr) sr.color = hurtColor;
            yield return new WaitForSeconds(invincibilityTime / 8f);
            if (sr) sr.color = originalColor;
            yield return new WaitForSeconds(invincibilityTime / 8f);
        }

        isInvincible = false;
        state = CanSeePlayer() ? State.Alert : State.Patrol;
    }

    void Die()
    {
        if (healthBar) healthBar.gameObject.SetActive(false);
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        GetComponent<Collider2D>().enabled = false;
        StartCoroutine(DoDeath());
    }

    IEnumerator DoDeath()
    {
        if (anim) anim.SetInteger(animState, 6);

        float t = 0;
        Color c = sr ? sr.color : Color.white;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            if (sr) sr.color = new Color(c.r, c.g, c.b, 1 - t * 2);
            yield return null;
        }
        Destroy(gameObject);
    }

    // === UTILIDADES ===
    void MoveToward(Vector2 target, float speed)
    {
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, dir * speed, ref velocity, smoothTime);

        if ((dir.x > 0 && !isFacingRight) || (dir.x < 0 && isFacingRight)) Flip();
    }

    void ClampPosition()
    {
        if (transform.position.y < minY) rb.linearVelocity = Vector2.up * moveSpeed;
        else if (transform.position.y > maxY) rb.linearVelocity = Vector2.down * moveSpeed;
    }

    float ClampY(float y) => Mathf.Clamp(y, minY, maxY);

    void GeneratePatrolPoint()
    {
        patrolTarget = new Vector2(
            startPos.x + Random.Range(-patrolAreaSize.x / 2f, patrolAreaSize.x / 2f),
            Random.Range(minY, maxY)
        );
    }

    bool CanSeePlayer()
    {
        if (!player || Vector2.Distance(transform.position, player.position) > detectionRange) return false;
        Vector2 dir = (player.position - detectionPoint.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(detectionPoint.position, dir, detectionRange, wallLayer | PlayerLayer);
        return hit.collider && hit.collider.CompareTag("Player");
    }

    void LookAtPlayer()
    {
        if (!player) return;
        bool shouldFaceRight = player.position.x > transform.position.x;
        if (shouldFaceRight != isFacingRight) Flip();
    }

    void Flip()
    {
        isFacingRight = !isFacingRight;
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
    }

    void UpdateAnim()
    {
        if (!anim) return;
        anim.SetFloat(animSpeed, rb.linearVelocity.magnitude);

        int s = state switch
        {
            State.Idle => 0,
            State.Patrol => 1,
            State.Alert or State.Chase => 2,
            State.Guard => 3,
            State.Attack => 4,
            State.Stunned => 5,
            _ => 0
        };
        anim.SetInteger(animState, s);
    }

    void OnDrawGizmosSelected()
    {
        Vector2 center = Application.isPlaying ? startPos : (Vector2)transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, attackRange);
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireCube(center, patrolAreaSize);

        if (attackPoint)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
    }
}