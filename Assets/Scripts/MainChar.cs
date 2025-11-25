using UnityEngine;
using System.Collections;

public class MainChar : MonoBehaviour, IDashExecutor
{
    [Header("Movimiento")]
    public float moveSpeed = 8f;
    public float jumpForce = 14f;
    private Rigidbody2D rb;
    private float moveInput;

    [Header("Detección")]
    public Transform groundCheck;
    public Transform wallCheck;
    public float checkRadius = 0.2f;
    public LayerMask groundLayer;
    public LayerMask wallLayer;

    [Header("Pared y salto")]
    public float wallSlideSpeed = 2.5f;
    public Vector2 wallJumpForce = new Vector2(13f, 17f);
    public float wallJumpLockTime = 0.15f;
    public float wallJumpControlTime = 0.25f;
    private float wallJumpCounter = 0f;
    private bool wasWallJumping = false;

    [Header("Wall Grab")]
    public bool canWallGrab = true;
    public KeyCode wallGrabKey = KeyCode.LeftShift;
    public float wallGrabStaminaMax = 3f;
    private float wallGrabStamina;
    private bool isWallGrabbing = false;

    private bool isGrounded;
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool isFacingRight = true;
    private int wallSide = 1;

    [Header("Ataque")]
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public LayerMask enemyLayer;
    public int attackDamage = 1;
    public float playerKnockbackForce = 3f;
    public GameObject sideAttackEffect;

    [Header("Down Attack (DESHABILITADO)")]
    public bool enableDownAttack = false;
    public Transform downAttackPoint;
    public GameObject downAttackEffect;
    private bool isAttackingDown = false;

    [Header("Gravedad / Saltos tipo Hollow Knight")]
    public float fallGravityMultiplier = 2.5f;
    public float lowJumpMultiplier = 2.5f;
    public float wallSlideGravityMultiplier = 0.3f;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.15f;

    [Header("Afinación adicional")]
    public float jumpCutMultiplier = 0.5f;
    public float airControlMultiplier = 1f;
    public float maxFallSpeed = 22f;
    public float wallJumpAirDrag = 0.92f;

    [Header("Down Attack Bounce (Deshabilitado)")]
    public float downAttackBounceForce = 25f;
    public float downAttackSmallBounceForce = 12f;

    [Header("Límite de Rebotes")]
    public int maxConsecutiveBounces = 3;
    public float bounceResetTime = 0.5f;
    private int consecutiveBounces = 0;
    private float lastBounceTime = -1f;

    [Header("Sistema de Vida")]
    public int maxHealth = 3;
    public int currentHealth = 3;
    public float damageInvincibilityTime = 1f;
    public Vector2 damageKnockbackForce = new Vector2(5f, 5f);
    public Color damageColor = new Color(1f, 0.3f, 0.3f);
    private bool isDamageInvincible = false;

    [Header("Sistema de Habilidades")]
    public KeyCode abilityUseKey = KeyCode.Q;
    private AbilityHolder abilityHolder;
    private bool isDashing = false;

    [Header("Efectos de Dash")]
    public GameObject dashTrailEffect; // (Opcional) Prefab de partículas si quieres
    public Color dashColor = new Color(0.3f, 0.8f, 1f);
    public bool showGhostTrail = true; // NUEVO: Activar efecto fantasma
    public float ghostTrailFrequency = 0.05f; // Cada cuanto tiempo sale un fantasma

    private float defaultGravityScale;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private bool jumpReleased = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultGravityScale = rb.gravityScale;

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        currentHealth = maxHealth;
        wallGrabStamina = wallGrabStaminaMax;

        // Inicializar sistema de habilidades
        abilityHolder = GetComponent<AbilityHolder>();
        if (abilityHolder == null)
        {
            abilityHolder = gameObject.AddComponent<AbilityHolder>();
        }

        if (GameManager.Instance != null && GameManager.Instance.hasCheckpoint)
            transform.position = GameManager.Instance.GetRespawnPosition();
    }

    void Update()
    {
        if (isDashing) return; // No hacer nada mientras hace dash

        HandleInput();
        UpdatePhysicsChecks();
        HandleGravity();
        HandleWallMechanics();
        HandleFlip();
        HandleBounceReset();
        HandleAbilityInput();
    }

    void FixedUpdate()
    {
        if (isDashing) return; // No mover durante dash

        if (isWallGrabbing)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isWallSliding)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed));
        }

        if (wallJumpCounter > 0f)
        {
            if (wallJumpCounter > wallJumpLockTime) return;

            float controlAmount = 1f - (wallJumpCounter / wallJumpLockTime);
            float targetX = moveInput * moveSpeed * controlAmount;
            rb.linearVelocity = new Vector2(
                Mathf.Lerp(rb.linearVelocity.x, targetX, wallJumpAirDrag),
                rb.linearVelocity.y
            );
        }
        else if (!isWallSliding)
        {
            float targetX = moveInput * moveSpeed;
            float appliedX = isGrounded ? targetX : targetX * airControlMultiplier;
            rb.linearVelocity = new Vector2(appliedX, rb.linearVelocity.y);
        }

        HandleJump();
        LimitFallSpeed();
    }

    // --- Métodos privados para modularidad y legibilidad ---

    private void HandleInput()
    {
        // Movimiento horizontal
        moveInput = (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);

        // Movimiento vertical
        float verticalInput = (Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) - (Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);

        // Salto
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;
            jumpReleased = false;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            jumpReleased = true;
            if (rb.linearVelocity.y > 0f)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
        }

        // Ataque
        isAttackingDown = false;
        if (Input.GetKeyDown(KeyCode.X))
        {
            if (enableDownAttack && verticalInput < 0 && !isGrounded)
                isAttackingDown = true;
            Attack();
        }
    }

    private void HandleAbilityInput()
    {
        if (Input.GetKeyDown(abilityUseKey) && abilityHolder != null)
        {
            abilityHolder.UseAbility();
        }
    }

    private void UpdatePhysicsChecks()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        isTouchingWall = Physics2D.OverlapCircle(wallCheck.position, checkRadius, wallLayer);

        if (isGrounded)
        {
            wasWallJumping = false;
            consecutiveBounces = 0;
            wallGrabStamina = wallGrabStaminaMax;
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (wallJumpCounter > 0f)
            wallJumpCounter -= Time.deltaTime;
    }

    private void HandleGravity()
    {
        if (isDashing)
        {
            rb.gravityScale = 0f;
            return;
        }

        if (isWallGrabbing)
            rb.gravityScale = 0f;
        else if (isWallSliding)
            rb.gravityScale = defaultGravityScale * wallSlideGravityMultiplier;
        else if (rb.linearVelocity.y < -0.5f)
            rb.gravityScale = defaultGravityScale * fallGravityMultiplier;
        else if (rb.linearVelocity.y > 0.5f && !Input.GetKey(KeyCode.Space))
            rb.gravityScale = defaultGravityScale * lowJumpMultiplier;
        else
            rb.gravityScale = defaultGravityScale;
    }

    private void HandleWallMechanics()
    {
        bool isPushingWall = (moveInput * wallSide > 0);
        bool wantsToGrab = canWallGrab && Input.GetKey(wallGrabKey);

        if (isTouchingWall && !isGrounded && wantsToGrab)
        {
            isWallGrabbing = true;
            isWallSliding = false;

            if (wallGrabStaminaMax > 0)
            {
                wallGrabStamina -= Time.deltaTime;
                if (wallGrabStamina <= 0)
                    isWallGrabbing = false;
            }
        }
        else if (isTouchingWall && !isGrounded && rb.linearVelocity.y < 0f && isPushingWall)
        {
            isWallGrabbing = false;
            isWallSliding = (Input.GetKey(KeyCode.DownArrow)) ? false : true;
        }
        else
        {
            isWallGrabbing = false;
            isWallSliding = false;
        }
    }

    private void HandleFlip()
    {
        if (isDashing) return; // No voltear durante dash
        if (wallJumpCounter > 0.05f) return;

        if (moveInput < 0 && isFacingRight)
            Flip();
        else if (moveInput > 0 && !isFacingRight)
            Flip();
    }

    private void HandleBounceReset()
    {
        if (Time.time - lastBounceTime > bounceResetTime && !isGrounded)
            consecutiveBounces = 0;
    }

    private void HandleJump()
    {
        if (jumpBufferCounter > 0f && !jumpReleased)
        {
            if (isTouchingWall && !isGrounded && wallJumpCounter <= 0f)
            {
                bool isPushingTowardsWall = (moveInput * wallSide > 0);
                float xForce = -wallSide * wallJumpForce.x * (isPushingTowardsWall || Mathf.Abs(moveInput) < 0.1f ? 0.7f : 1f);
                float yForce = wallJumpForce.y * (isPushingTowardsWall || Mathf.Abs(moveInput) < 0.1f ? 1f : 0.95f);

                rb.linearVelocity = new Vector2(xForce, yForce);

                wallJumpCounter = wallJumpControlTime;
                wasWallJumping = true;
                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;
                isWallSliding = false;
                isWallGrabbing = false;
            }
            else if (coyoteTimeCounter > 0f && !wasWallJumping)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;
            }
            else
            {
                jumpBufferCounter = 0f;
            }
        }
    }

    private void LimitFallSpeed()
    {
        if (rb.linearVelocity.y < -maxFallSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        wallSide *= -1;
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;
    }

    // ============================================
    // IMPLEMENTACIÓN DE IDashExecutor
    // ============================================
    public void PerformDash(float force, float duration)
    {
        if (!isDashing)
        {
            StartCoroutine(DashCoroutine(force, duration));
        }
    }

    private IEnumerator DashCoroutine(float force, float duration)
    {
        isDashing = true;

        // Determinar dirección del dash
        float dashDirection = isFacingRight ? 1f : -1f;

        // Si hay input, usar esa dirección
        if (Mathf.Abs(moveInput) > 0.1f)
        {
            dashDirection = Mathf.Sign(moveInput);
        }

        // Aplicar velocidad de dash
        rb.linearVelocity = new Vector2(dashDirection * force, 0f);

        // Efecto visual
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color originalColor = sr != null ? sr.color : Color.white;
        if (sr != null) sr.color = dashColor;

        // Crear trail effect (Partículas originales)
        if (dashTrailEffect != null)
        {
            GameObject trail = Instantiate(dashTrailEffect, transform.position, Quaternion.identity);
            Destroy(trail, duration + 0.5f);
        }

        // NUEVO: EFECTO FANTASMA (PAPEL CEBOLLA)
        if (showGhostTrail)
        {
            StartCoroutine(SpawnGhostTrail(duration, sr));
        }

        Debug.Log($"¡DASH ejecutado! Dirección: {dashDirection}, Fuerza: {force}");

        yield return new WaitForSeconds(duration);

        // Restaurar gravedad y color
        isDashing = false;
        rb.gravityScale = defaultGravityScale;
        if (sr != null) sr.color = originalColor;

        // Reducir velocidad gradualmente
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, rb.linearVelocity.y);
    }

    // NUEVA CORRUTINA: Genera copias fantasmas del jugador
    private IEnumerator SpawnGhostTrail(float duration, SpriteRenderer originalSr)
    {
        float elapsed = 0f;
        while (elapsed < duration && isDashing)
        {
            // Crear un objeto fantasma
            GameObject ghost = new GameObject("GhostTrail_Player");
            ghost.transform.position = transform.position;
            ghost.transform.localScale = transform.localScale;
            ghost.transform.rotation = transform.rotation;

            // Copiar el sprite
            SpriteRenderer ghostSr = ghost.AddComponent<SpriteRenderer>();
            ghostSr.sprite = originalSr.sprite;
            ghostSr.color = new Color(dashColor.r, dashColor.g, dashColor.b, 0.5f); // Semi-transparente
            ghostSr.sortingOrder = originalSr.sortingOrder - 1;

            // Destruir el fantasma rápidamente
            Destroy(ghost, 0.3f);

            yield return new WaitForSeconds(ghostTrailFrequency);
            elapsed += ghostTrailFrequency;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDamageInvincible)
        {
            Debug.Log("Jugador invencible, daño ignorado");
            return;
        }

        currentHealth -= damage;
        Debug.Log($"Jugador recibió {damage} de daño. Vida: {currentHealth}/{maxHealth}");

        float knockbackDir = 1f;
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length > 0)
        {
            float closestDist = Mathf.Infinity;
            GameObject closestEnemy = null;
            foreach (GameObject enemy in enemies)
            {
                float dist = Vector2.Distance(transform.position, enemy.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestEnemy = enemy;
                }
            }
            if (closestEnemy != null)
                knockbackDir = transform.position.x > closestEnemy.transform.position.x ? 1f : -1f;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.linearVelocity = new Vector2(knockbackDir * damageKnockbackForce.x, damageKnockbackForce.y);
        }

        if (currentHealth <= 0)
            Die();
        else
            StartCoroutine(DamageInvincibility());
    }

    IEnumerator DamageInvincibility()
    {
        isDamageInvincible = true;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        Color originalColor = sr != null ? sr.color : Color.white;

        float flashInterval = damageInvincibilityTime / 10f;
        for (int i = 0; i < 5; i++)
        {
            if (sr != null) sr.color = damageColor;
            yield return new WaitForSeconds(flashInterval);
            if (sr != null) sr.color = originalColor;
            yield return new WaitForSeconds(flashInterval);
        }

        isDamageInvincible = false;
        Debug.Log("Invencibilidad terminada");
    }

    void Die()
    {
        Debug.Log("¡Jugador murió!");

        if (AbilityAbsorptionManager.Instance != null)
        {
            AbilityAbsorptionManager.Instance.OnPlayerDeath();
        }

        if (GameManager.Instance != null)
            StartCoroutine(RespawnAfterDeath());
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
    }

    IEnumerator RespawnAfterDeath()
    {
        enabled = false;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;

        yield return new WaitForSeconds(1f);

        currentHealth = maxHealth;
        if (GameManager.Instance != null)
            transform.position = GameManager.Instance.GetRespawnPosition();

        if (sr != null) sr.enabled = true;
        rb.linearVelocity = Vector2.zero;
        isDamageInvincible = false;
        enabled = true;

        Debug.Log("Jugador respawneado");
    }

    void Attack()
    {
        GameObject effectToShow = isAttackingDown ? downAttackEffect : sideAttackEffect;
        if (effectToShow != null)
        {
            ParticleSystem ps = effectToShow.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();
        }

        if (!isAttackingDown)
        {
            float knockbackDir = isFacingRight ? -1 : 1;
            rb.AddForce(new Vector2(knockbackDir * playerKnockbackForce, 0), ForceMode2D.Impulse);
        }

        if (isAttackingDown && enableDownAttack)
        {
            HandleDownAttack();
        }
        else
        {
            HandleSideAttack();
        }
    }

    private void HandleDownAttack()
    {
        Transform currentAttackPoint = downAttackPoint;
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(currentAttackPoint.position, attackRange, enemyLayer);
        Collider2D[] hitGround = Physics2D.OverlapCircleAll(currentAttackPoint.position, attackRange, groundLayer);

        bool hitSomething = false;

        foreach (Collider2D enemyCollider in hitEnemies)
        {
            if (TryDealDamage(enemyCollider, isFacingRight ? 1 : -1))
                hitSomething = true;
        }

        if (hitGround.Length > 0)
        {
            hitSomething = true;
            Debug.Log("¡Pegaste al suelo!");
        }

        if (hitSomething)
        {
            if (consecutiveBounces < maxConsecutiveBounces)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, downAttackBounceForce);
                consecutiveBounces++;
                lastBounceTime = Time.time;
                Debug.Log($"¡Rebote automático! ({consecutiveBounces}/{maxConsecutiveBounces})");
            }
            else
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, downAttackBounceForce * 0.3f);
                Debug.Log("¡Límite de rebotes alcanzado! Rebote reducido");
            }
        }
    }

    private void HandleSideAttack()
    {
        Transform currentAttackPoint = attackPoint;
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(currentAttackPoint.position, attackRange, enemyLayer);

        foreach (Collider2D enemyCollider in hitEnemies)
        {
            if (TryDealDamage(enemyCollider, isFacingRight ? 1 : -1))
                Debug.Log($"Golpeó a enemigo: {enemyCollider.name}");
        }
    }

    private bool TryDealDamage(Collider2D enemyCollider, int knockbackDir)
    {
        var enemy = enemyCollider.GetComponent<Enemigo>();
        if (enemy != null)
        {
            enemy.TakeDamage(attackDamage, knockbackDir);
            return true;
        }
        return false;
    }

    void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }
        if (wallCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(wallCheck.position, checkRadius);
        }
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
        if (enableDownAttack && downAttackPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(downAttackPoint.position, attackRange);
        }
    }
}