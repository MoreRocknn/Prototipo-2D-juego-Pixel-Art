using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossController : MonoBehaviour, IAbsorbable, IResettable
{
    [Header("Estadísticas del Boss")]
    public int maxHealth = 20;
    private int currentHealth;
    public float moveSpeed = 3f;
    public float detectionRange = 15f;
    public float attackRange = 8f;

    [Header("Sistema de Combate")]
    public float minAttackCooldown = 3f;
    public float maxAttackCooldown = 5f;
    private float attackCooldownTimer;
    public float meleeAttackRange = 2f;
    public int meleeDamage = 1;
    public float meleeKnockback = 8f;

    [Header("Ataque Cuerpo a Cuerpo")]
    public Transform meleeAttackPoint;
    public float meleeAttackRadius = 2f;
    public int meleeAttackDamage = 2;
    public float meleeAttackDuration = 0.5f;
    public float meleeAttackWindup = 0.3f;
    public Color meleeAttackColor = new Color(1f, 0.3f, 0f);

    [Header("Ataque: Espadas del Cielo")]
    public GameObject swordPrefab;
    public int swordsPerAttack = 5;
    public float swordWarningTime = 1f;
    public float swordDamage = 1;
    public float swordFallSpeed = 15f;
    public float swordSpacing = 2f;

    [Header("Ataque: Pinchos del Suelo")]
    public GameObject spikePrefab;
    public int spikesPerWave = 3;
    public float spikeWarningTime = 0.8f;
    public int spikeDamage = 1;
    public float spikeSpacing = 2.5f;

    [Header("Sistema de Daño Visual")]
    public Color normalColor = Color.white;
    public Color damageColor = Color.red;
    public float damageFeedbackDuration = 0.15f;

    [Header("Arena del Boss")]
    public GameObject leftDoor;
    public GameObject rightDoor;
    public float doorCloseDistance = 2f;
    private bool arenaSealed = false;

    [Header("Barra de Vida (Dark Souls Style)")]
    public GameObject bossHealthBarPrefab;
    private BossHealthBar bossHealthBarUI;
    public string bossName = "Guardian Corrupto";
    public float healthBarActivationDistance = 12f;
    private bool healthBarActivated = false;

    [Header("Habilidad Absorbible")]
    public BloodPoolAbility bloodPoolAbility;

    [Header("Daño por Contacto")]
    public int contactDamage = 1;
    public float contactDamageCooldown = 1f;
    private float lastContactDamageTime = -999f;

    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private bool isAttacking = false;
    private bool isDead = false;
    private Vector3 initialPosition;
    private int initialHealth;

    // Fases del boss
    private enum BossPhase { Phase1, Phase2, Phase3 }
    private BossPhase currentPhase = BossPhase.Phase1;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        currentHealth = maxHealth;
        initialHealth = maxHealth;
        initialPosition = transform.position;
        attackCooldownTimer = minAttackCooldown;

        // CORREGIDO: Boss es Dynamic pero con masa muy alta para no ser empujado fácilmente
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.mass = 1000f; // Masa muy alta = difícil de empujar
            rb.linearDamping = 10f; // Alta resistencia al movimiento
            rb.gravityScale = 1f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        // Crear punto de ataque cuerpo a cuerpo si no existe
        if (meleeAttackPoint == null)
        {
            GameObject attackPt = new GameObject("MeleeAttackPoint");
            attackPt.transform.SetParent(transform);
            attackPt.transform.localPosition = Vector3.zero; // Centro del boss
            meleeAttackPoint = attackPt.transform;
        }

        // Registrar en el sistema de reset
        if (AbilityAbsorptionManager.Instance != null)
        {
            AbilityAbsorptionManager.Instance.RegisterResettable(this);
        }

        // Mantener puertas abiertas al inicio
        if (leftDoor != null) leftDoor.SetActive(false);
        if (rightDoor != null) rightDoor.SetActive(false);
    }

    void Update()
    {
        if (isDead || player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        // Activar barra de vida
        if (!healthBarActivated && distanceToPlayer <= healthBarActivationDistance)
        {
            ActivateBossHealthBar();
        }

        // Sellar arena cuando el jugador entra
        if (!arenaSealed && distanceToPlayer <= doorCloseDistance)
        {
            SealArena();
        }

        // Comportamiento del boss
        if (distanceToPlayer <= detectionRange)
        {
            if (!isAttacking)
            {
                // Movimiento hacia el jugador (pero mantiene distancia)
                if (distanceToPlayer > meleeAttackRange * 1.5f)
                {
                    MoveTowardsPlayer();
                }

                // Sistema de ataques
                attackCooldownTimer -= Time.deltaTime;
                if (attackCooldownTimer <= 0f && distanceToPlayer <= attackRange)
                {
                    StartCoroutine(PerformRandomAttack());
                    attackCooldownTimer = Random.Range(minAttackCooldown, maxAttackCooldown);
                }
            }

            // Voltear hacia el jugador
            if (player.position.x < transform.position.x && transform.localScale.x > 0)
            {
                Flip();
            }
            else if (player.position.x > transform.position.x && transform.localScale.x < 0)
            {
                Flip();
            }
        }

        // Actualizar fase según vida
        UpdatePhase();
    }

    void MoveTowardsPlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        // CORREGIDO: Usar velocity normal ya que es Dynamic
        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
    }

    void UpdatePhase()
    {
        float healthPercent = (float)currentHealth / maxHealth;

        if (healthPercent > 0.66f)
            currentPhase = BossPhase.Phase1;
        else if (healthPercent > 0.33f)
            currentPhase = BossPhase.Phase2;
        else
            currentPhase = BossPhase.Phase3;
    }

    IEnumerator DamageFlash()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = damageColor;
            yield return new WaitForSeconds(damageFeedbackDuration);

            // Solo restaurar color si no está muerto
            if (!isDead && spriteRenderer != null)
            {
                spriteRenderer.color = normalColor;
            }
        }
    }

    IEnumerator PerformRandomAttack()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero; // Detener movimiento durante ataque

        // El boss NO puede ser atacado cuerpo a cuerpo durante habilidades
        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

        // Decidir tipo de ataque según distancia
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        int attackType;

        if (distanceToPlayer <= meleeAttackRange * 1.5f)
        {
            // Si está cerca, 70% probabilidad de ataque cuerpo a cuerpo
            attackType = Random.Range(0, 10) < 7 ? 0 : Random.Range(1, 3);
        }
        else
        {
            // Si está lejos, solo ataques a distancia
            attackType = Random.Range(1, 3);
        }

        switch (attackType)
        {
            case 0:
                yield return StartCoroutine(MeleeAttack());
                break;
            case 1:
                yield return StartCoroutine(SwordRainAttack());
                break;
            case 2:
                yield return StartCoroutine(SpikesAttack());
                break;
        }

        // Restaurar capa normal
        gameObject.layer = LayerMask.NameToLayer("Enemy");

        isAttacking = false;

        // Reducir cooldown en fases avanzadas
        if (currentPhase == BossPhase.Phase2)
            attackCooldownTimer *= 0.8f;
        else if (currentPhase == BossPhase.Phase3)
            attackCooldownTimer *= 0.6f;
    }

    IEnumerator MeleeAttack()
    {
        Debug.Log("Boss: Ataque Cuerpo a Cuerpo");

        // Fase de preparación (windup)
        if (spriteRenderer != null)
        {
            spriteRenderer.color = meleeAttackColor;
        }

        // Crear indicador circular de ataque
        GameObject warningCircle = CreateWarningIndicator(transform.position, new Color(1f, 0.5f, 0f));
        if (warningCircle != null)
        {
            warningCircle.transform.localScale = Vector3.one * meleeAttackRadius * 2f;
        }

        yield return new WaitForSeconds(meleeAttackWindup);

        if (warningCircle != null)
        {
            Destroy(warningCircle);
        }

        // Ejecutar el golpe
        if (meleeAttackPoint != null)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(meleeAttackPoint.position, meleeAttackRadius);

            foreach (Collider2D hit in hits)
            {
                if (hit.CompareTag("Player"))
                {
                    MainChar playerScript = hit.GetComponent<MainChar>();
                    if (playerScript != null)
                    {
                        playerScript.TakeDamage(meleeAttackDamage);
                        Debug.Log($"Boss golpeó al jugador con ataque cuerpo a cuerpo por {meleeAttackDamage} de daño");
                    }
                }
            }
        }

        // Efecto visual del golpe
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
        }

        yield return new WaitForSeconds(meleeAttackDuration);

        // Restaurar color
        if (spriteRenderer != null && !isDead)
        {
            spriteRenderer.color = normalColor;
        }
    }

    IEnumerator SwordRainAttack()
    {
        Debug.Log("Boss: Ataque de Lluvia de Espadas");

        int swordCount = swordsPerAttack;
        if (currentPhase == BossPhase.Phase3) swordCount += 2;

        List<Vector3> swordPositions = new List<Vector3>();
        List<GameObject> warnings = new List<GameObject>();

        // Generar posiciones de espadas centradas en el jugador
        for (int i = 0; i < swordCount; i++)
        {
            float offsetX = (i - swordCount / 2f) * swordSpacing;
            Vector3 targetPos = player.position + new Vector3(offsetX, 0, 0);
            swordPositions.Add(targetPos);

            // Crear indicador de advertencia
            GameObject warning = CreateWarningIndicator(targetPos, Color.yellow);
            warnings.Add(warning);
        }

        // Esperar tiempo de advertencia
        yield return new WaitForSeconds(swordWarningTime);

        // Destruir indicadores y lanzar espadas
        foreach (GameObject warning in warnings)
        {
            if (warning != null) Destroy(warning);
        }

        foreach (Vector3 pos in swordPositions)
        {
            if (swordPrefab != null)
            {
                Vector3 spawnPos = pos + new Vector3(0, 10f, 0);
                GameObject sword = Instantiate(swordPrefab, spawnPos, Quaternion.Euler(0, 0, 180));

                FallingSword swordScript = sword.GetComponent<FallingSword>();
                if (swordScript == null)
                {
                    swordScript = sword.AddComponent<FallingSword>();
                }
                swordScript.Initialize(swordFallSpeed, swordDamage);
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator SpikesAttack()
    {
        Debug.Log("Boss: Ataque de Pinchos");

        int waveCount = spikesPerWave;
        if (currentPhase == BossPhase.Phase3) waveCount += 1;

        for (int wave = 0; wave < waveCount; wave++)
        {
            List<Vector3> spikePositions = new List<Vector3>();
            List<GameObject> warnings = new List<GameObject>();

            // CORREGIDO: Línea recta a la ALTURA DEL BOSS (no del suelo)
            Vector3 startPos = transform.position; // Posición del boss
            Vector3 direction = (player.position - transform.position).normalized;

            // Crear línea de pinchos a la misma altura que el boss
            int spikeCount = 4;
            if (currentPhase == BossPhase.Phase3) spikeCount = 5;

            for (int i = 1; i <= spikeCount; i++)
            {
                // Mantener la Y del boss, solo avanzar en X hacia el jugador
                Vector3 spikePos = startPos + direction * (i * spikeSpacing);
                // CLAVE: Mantener altura del boss
                spikePos.y = startPos.y;

                spikePositions.Add(spikePos);

                GameObject warning = CreateWarningIndicator(spikePos, Color.red);
                warnings.Add(warning);
            }

            yield return new WaitForSeconds(spikeWarningTime);

            foreach (GameObject warning in warnings)
            {
                if (warning != null) Destroy(warning);
            }

            // Spawnar pinchos uno tras otro en línea recta horizontal
            foreach (Vector3 pos in spikePositions)
            {
                if (spikePrefab != null)
                {
                    GameObject spike = Instantiate(spikePrefab, pos, Quaternion.identity);

                    GroundSpike spikeScript = spike.GetComponent<GroundSpike>();
                    if (spikeScript == null)
                    {
                        spikeScript = spike.AddComponent<GroundSpike>();
                    }
                    spikeScript.Initialize(spikeDamage);
                }
                yield return new WaitForSeconds(0.15f);
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    GameObject CreateWarningIndicator(Vector3 position, Color color)
    {
        GameObject indicator = new GameObject("WarningIndicator");
        indicator.transform.position = position;

        SpriteRenderer sr = indicator.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = new Color(color.r, color.g, color.b, 0.5f);
        sr.sortingOrder = 10;

        indicator.transform.localScale = Vector3.one * 1.5f;

        // Animación de pulso
        StartCoroutine(PulseWarning(indicator));

        return indicator;
    }

    Sprite CreateCircleSprite()
    {
        Texture2D tex = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];

        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(32, 32));
                pixels[y * 64 + x] = dist < 30 ? Color.white : Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
    }

    IEnumerator PulseWarning(GameObject indicator)
    {
        Vector3 baseScale = indicator.transform.localScale;
        float elapsed = 0f;

        while (indicator != null)
        {
            elapsed += Time.deltaTime * 3f;
            float scale = 1f + Mathf.Sin(elapsed) * 0.2f;
            indicator.transform.localScale = baseScale * scale;
            yield return null;
        }
    }

    void SealArena()
    {
        arenaSealed = true;
        if (leftDoor != null) leftDoor.SetActive(true);
        if (rightDoor != null) rightDoor.SetActive(true);
        Debug.Log("¡Arena sellada! No hay escapatoria.");
    }

    void UnsealArena()
    {
        arenaSealed = false;
        if (leftDoor != null) leftDoor.SetActive(false);
        if (rightDoor != null) rightDoor.SetActive(false);
        Debug.Log("Arena desbloqueada.");
    }

    void ActivateBossHealthBar()
    {
        healthBarActivated = true;

        if (bossHealthBarPrefab != null)
        {
            GameObject barObj = Instantiate(bossHealthBarPrefab);
            bossHealthBarUI = barObj.GetComponent<BossHealthBar>();

            if (bossHealthBarUI != null)
            {
                bossHealthBarUI.Initialize(bossName, maxHealth);
            }
        }
        else
        {
            Debug.LogWarning("No hay prefab de barra de vida asignado");
        }
    }

    public void TakeDamage(int damage, int knockbackDir)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log($"Boss recibió {damage} daño. Vida: {currentHealth}/{maxHealth}");

        // Feedback visual de daño (NO detener otras corrutinas, solo la de flash)
        StopCoroutine("DamageFlash");
        StartCoroutine(DamageFlash());

        // Actualizar barra de vida
        if (bossHealthBarUI != null)
        {
            bossHealthBarUI.UpdateHealth(currentHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("¡Boss derrotado!");

        // Ocultar barra de vida
        if (bossHealthBarUI != null)
        {
            bossHealthBarUI.Hide();
        }

        // IMPORTANTE: Desbloquear arena inmediatamente
        UnsealArena();

        // Detener movimiento
        if (rb != null) rb.linearVelocity = Vector2.zero;

        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        // Animación de muerte
        for (int i = 0; i < 5; i++)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.red;
                yield return new WaitForSeconds(0.1f);
                spriteRenderer.color = Color.white;
                yield return new WaitForSeconds(0.1f);
            }
        }

        // El boss se vuelve absorbible
        gameObject.layer = LayerMask.NameToLayer("Default");

        Debug.Log("Boss ahora puede ser absorbido (presiona E cerca)");
    }

    void Flip()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    // Implementación de IAbsorbable
    public bool CanBeAbsorbed()
    {
        return isDead;
    }

    public void OnAbsorbed()
    {
        Debug.Log("Habilidad de Charco de Sangre absorbida");

        // Dar la habilidad al jugador
        if (player != null)
        {
            MainChar mainChar = player.GetComponent<MainChar>();
            if (mainChar != null)
            {
                // Crear nueva instancia de la habilidad
                BloodPoolAbility newAbility = new BloodPoolAbility();

                // Dar la habilidad al jugador directamente
                BloodPoolTransform poolTransform = mainChar.GetComponent<BloodPoolTransform>();
                if (poolTransform == null)
                {
                    poolTransform = mainChar.gameObject.AddComponent<BloodPoolTransform>();
                }

                Debug.Log("¡Habilidad de Charco de Sangre otorgada al jugador!");
            }
        }

        Destroy(gameObject);
    }

    // Implementación de IResettable
    public void ResetState()
    {
        currentHealth = initialHealth;
        isDead = false;
        isAttacking = false;
        arenaSealed = false;
        healthBarActivated = false;
        transform.position = initialPosition;

        if (spriteRenderer != null)
            spriteRenderer.color = normalColor;

        // CORREGIDO: Desbloquear puertas al resetear
        UnsealArena();

        if (bossHealthBarUI != null)
        {
            Destroy(bossHealthBarUI.gameObject);
            bossHealthBarUI = null;
        }

        attackCooldownTimer = minAttackCooldown;
        currentPhase = BossPhase.Phase1;

        // Hacer visible el boss de nuevo
        gameObject.SetActive(true);

        // Restaurar física
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        Debug.Log("Boss reseteado al estado inicial - Arena desbloqueada");
    }

    public bool IsBoss => true;

    // Método público para resetear desde EnemyManager
    public void ForceReset()
    {
        ResetState();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time - lastContactDamageTime >= contactDamageCooldown)
            {
                MainChar playerScript = collision.gameObject.GetComponent<MainChar>();
                if (playerScript != null)
                {
                    playerScript.TakeDamage(contactDamage);
                    lastContactDamageTime = Time.time;
                    Debug.Log("Boss hizo daño por contacto al jugador");
                }
            }
        }
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time - lastContactDamageTime >= contactDamageCooldown)
            {
                MainChar playerScript = collision.gameObject.GetComponent<MainChar>();
                if (playerScript != null)
                {
                    playerScript.TakeDamage(contactDamage);
                    lastContactDamageTime = Time.time;
                    Debug.Log("Boss hizo daño por contacto al jugador");
                }
            }
        }
    }

    void OnDestroy()
    {
        if (AbilityAbsorptionManager.Instance != null)
        {
            AbilityAbsorptionManager.Instance.UnregisterResettable(this);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, meleeAttackRange);
    }
}