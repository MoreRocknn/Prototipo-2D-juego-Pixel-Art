using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class BossController : MonoBehaviour, IAbsorbable, IResettable
{
    [Header("=== EL REY INFECTADO (AGRESIVO) ===")]
    public int maxHealth = 50;
    private int currentHealth;
    public float moveSpeed = 8f;
    public float detectionRange = 30f;

    [Header("=== IA DEPREDADORA ===")]
    public float repositionSpeed = 12f;
    public float optimalDistance = 7f;
    public float retreatDistance = 3f;

    [Header("=== OPTIMIZACIÓN DE MOVIMIENTO ===")]
    [Tooltip("Suavizado del movimiento para evitar teletransporte")]
    [Range(1f, 20f)]
    public float movementSmoothing = 10f;
    private Vector2 targetVelocity;
    private Vector2 currentVelocity;

    [Header("=== MOVIMIENTO: PASO VIRAL ===")]
    public int hitsToTriggerTeleport = 4;
    public float teleportDelay = 0.5f;
    private int currentHitCounter = 0;
    private bool isTeleporting = false;
    private float lastTeleportTime = -999f;
    private float minTeleportInterval = 3f;

    [Header("=== SISTEMA DE COMBATE ===")]
    public float minAttackCooldown = 1.2f;
    public float maxAttackCooldown = 2.0f;
    private float attackCooldownTimer;

    [Header("Daño por Contacto")]
    public int bodyContactDamage = 1;
    public float bodyDamageCooldown = 1.0f;
    private float lastBodyDamageTime;

    [Header("Ataques")]
    public Transform meleeAttackPoint;
    public Vector2 meleeAttackBoxSize = new Vector2(6f, 4f);
    public int meleeAttackDamage = 2;
    public float meleeWindup = 0.4f;
    public float meleeDashForce = 50f;
    public float meleeKnockback = 20f;

    public int swordsCount = 10;
    public float swordSpeed = 22f;

    public int geysersCount = 6;
    public float geyserWarningTime = 0.6f;
    public int geyserDamage = 1;

    public int bloodProjectiles = 20;
    public float bloodSpeed = 11f;

    [Header("Referencias")]
    public GameObject leftDoor;
    public GameObject rightDoor;
    public float doorCloseDistance = 15f;

    [Header("UI BOSS")]
    public GameObject bossHealthBarPrefab;
    public string bossName = "EL REY PACIENTE";
    public BossHealthBar bossHealthBarUI;
    private bool arenaSealed = false;
    private bool healthBarActivated = false;
    public float resetCooldown = 10f; 


    [Header("PREFABS DE ATAQUE")]
    public GameObject FallingSwords;
    public GameObject GroundSpikes;

    [Header("CONFIGURACIÓN DEL SUELO")]
[Tooltip("Altura Y donde está el suelo. Déjalo en 0 para detectar automáticamente.")]
public float groundYPosition = 0f;

    private float minArenaX;
    private float maxArenaX;

    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D bossCollider;
    private bool isAttacking = false;
    private bool isDead = false;
    private bool isInvulnerable = false;
    private Vector3 initialPosition;
    private float defaultGravity;

    private enum BossPhase { Phase1, Phase2, Phase3 }
    private BossPhase currentPhase = BossPhase.Phase1;
    private Sprite bloodSprite;
    private float lastResetTime = -999f;

    private int lastAttackType = -1;
    private int consecutiveMeleeAttacks = 0;
    private int consecutiveRangedAttacks = 0;
    private Vector2 preferredPosition;
    private float repositionTimer = 0f;
    private bool isCircling = false;
    private float circleDirection = 1f;

    // OPTIMIZACIÓN: Cache de componentes
    private MainChar playerMainChar;
    private Rigidbody2D playerRb;

    // OPTIMIZACIÓN: Pool de objetos reutilizables
    private Collider2D[] hitBuffer = new Collider2D[10];

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bossCollider = GetComponent<Collider2D>();
        initialPosition = transform.position;
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerMainChar = playerObj.GetComponent<MainChar>();
            playerRb = playerObj.GetComponent<Rigidbody2D>();
        }

        currentHealth = maxHealth;
        attackCooldownTimer = 1f;

        GenerateBloodSprite();
        ConfigurePhysics();
        SetupMeleeAttackPoint();
        CalculateArenaBounds();

        if (AbilityAbsorptionManager.Instance != null)
            AbilityAbsorptionManager.Instance.RegisterResettable(this);

        SetDoorsState(false);
    }

    void CalculateArenaBounds()
    {
        if (leftDoor != null && rightDoor != null)
        {
            minArenaX = leftDoor.transform.position.x + 2f;
            maxArenaX = rightDoor.transform.position.x - 2f;
        }
        else
        {
            minArenaX = initialPosition.x - 12f;
            maxArenaX = initialPosition.x + 12f;
        }
    }

    void GenerateBloodSprite()
    {
        int res = 32;
        Texture2D tex = new Texture2D(res, res);
        Color[] pixels = new Color[res * res];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.red;
        tex.SetPixels(pixels);
        tex.Apply();
        bloodSprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
    }

    void ConfigurePhysics()
    {
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.mass = 5000f;
            rb.linearDamping = 2f;
            rb.gravityScale = 3f;
            defaultGravity = rb.gravityScale;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            // OPTIMIZACIÓN: Interpolación para movimiento suave
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }
    }

    void SetupMeleeAttackPoint()
    {
        if (meleeAttackPoint == null)
        {
            GameObject attackPt = new GameObject("MeleePt");
            attackPt.transform.SetParent(transform);
            attackPt.transform.localPosition = new Vector3(2f, 0, 0);
            meleeAttackPoint = attackPt.transform;
        }
    }

    void Update()
{
    if (isDead || player == null) return;

    // ✅ NO HACER NADA durante el cooldown post-reset
    if (Time.time < lastResetTime + resetCooldown)
    {
        return;
    }

    if (!isTeleporting && !isAttacking)
    {
        EnsureVisibility();
    }

    ClampToArena();

    float dist = Vector2.Distance(transform.position, player.position);

    if (!healthBarActivated && dist <= detectionRange)
    {
        ActivateBossHealthBar();
    }

    if (!arenaSealed && dist <= doorCloseDistance) SealArena();

    if (dist <= detectionRange)
    {
        HandleCombat(dist);
        if (!isAttacking && !isTeleporting) FlipTowardsPlayer();
    }

    UpdatePhase();
}

    void EnsureVisibility()
    {
        if (spriteRenderer != null && !spriteRenderer.enabled)
        {
            spriteRenderer.enabled = true;
        }
        if (bossCollider != null && !bossCollider.enabled)
        {
            bossCollider.enabled = true;
        }
        if (rb.gravityScale == 0 && !isInvulnerable)
        {
            rb.gravityScale = defaultGravity;
        }
    }

    void ClampToArena()
    {
        float clampedX = Mathf.Clamp(transform.position.x, minArenaX, maxArenaX);
        if (Mathf.Abs(transform.position.x - clampedX) > 0.1f)
        {
            transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    void ActivateBossHealthBar()
{
    if (healthBarActivated || bossHealthBarPrefab == null) return;
    
    // ✅ Usar la variable configurable
    if (Time.time < lastResetTime + resetCooldown) 
    {
        return; // Salir silenciosamente
    }

    Debug.Log("Activando barra de vida del boss...");

    GameObject barObj = Instantiate(bossHealthBarPrefab);

    bossHealthBarUI = barObj.GetComponent<BossHealthBar>();
    if (bossHealthBarUI != null)
    {
        bossHealthBarUI.Initialize(bossName, maxHealth);
        healthBarActivated = true;
        Debug.Log("✅ Barra de vida inicializada correctamente");

        CamaraScript camara = Camera.main.GetComponent<CamaraScript>();
        if (camara != null)
        {
            camara.enModoBoss = true;
            camara.SnapToPlayer();
        }
    }
    else
    {
        Debug.LogError("❌ El prefab no tiene componente BossHealthBar");
        Destroy(barObj);
    }
}
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead || isTeleporting) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time > lastBodyDamageTime + bodyDamageCooldown)
            {
                // OPTIMIZACIÓN: Usar referencia cacheada
                playerMainChar?.TakeDamage(bodyContactDamage);
                lastBodyDamageTime = Time.time;
            }
        }
    }

    void HandleCombat(float dist)
    {
        if (isAttacking || isTeleporting) return;

        attackCooldownTimer -= Time.deltaTime;

        if (attackCooldownTimer <= 0f)
        {
            StartCoroutine(PerformDarkSoulsAttack(dist));

            float phaseMod = (currentPhase == BossPhase.Phase3) ? 0.5f :
                            (currentPhase == BossPhase.Phase2) ? 0.7f : 1f;
            attackCooldownTimer = Random.Range(minAttackCooldown, maxAttackCooldown) * phaseMod;
        }
        else
        {
            HandleAggressiveMovement(dist);
        }
    }

    // ========================================
    // OPTIMIZACIÓN: Movimiento suavizado en FixedUpdate
    // ========================================
    void FixedUpdate()
    {
        if (isDead || player == null || isAttacking || isTeleporting) return;

        // Aplicar velocidad suavizada
        if (targetVelocity != Vector2.zero)
        {
            currentVelocity = Vector2.Lerp(currentVelocity, targetVelocity, movementSmoothing * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(currentVelocity.x, rb.linearVelocity.y);
        }
    }

    void HandleAggressiveMovement(float dist)
    {
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        float dirX = Mathf.Sign(player.position.x - transform.position.x);

        // OPTIMIZACIÓN: Calcular velocidad objetivo en vez de aplicarla directamente
        if (dist < retreatDistance)
        {
            targetVelocity = new Vector2(-dirX * repositionSpeed * 1.2f, 0);
        }
        else if (dist > optimalDistance + 3f && dist <= detectionRange)
        {
            float dashMod = (currentPhase == BossPhase.Phase3 && Random.value < 0.3f) ? 1.8f : 1f;
            targetVelocity = new Vector2(dirX * moveSpeed * dashMod, 0);
        }
        else if (dist > detectionRange)
        {
            targetVelocity = Vector2.zero;
        }
        else
        {
            repositionTimer += Time.deltaTime;
            if (repositionTimer > 1.5f)
            {
                circleDirection *= -1f;
                repositionTimer = 0f;
            }

            float moveMod = (circleDirection > 0) ? 0.5f : -0.3f;
            targetVelocity = new Vector2(dirX * repositionSpeed * moveMod, 0);
        }
    }

    IEnumerator PerformDarkSoulsAttack(float dist)
    {
        isAttacking = true;
        targetVelocity = Vector2.zero;
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        int attackType = ChooseDarkSoulsAttack(dist);

        switch (attackType)
        {
            case 0:
                consecutiveMeleeAttacks++;
                consecutiveRangedAttacks = 0;
                yield return StartCoroutine(MeleeDashAttack());
                break;
            case 1:
                consecutiveMeleeAttacks = 0;
                consecutiveRangedAttacks++;
                yield return StartCoroutine(SwordBarrage());
                break;
            case 2:
                consecutiveMeleeAttacks = 0;
                consecutiveRangedAttacks++;
                yield return StartCoroutine(GroundSpikesAttack());
                break;
            case 3:
                consecutiveMeleeAttacks = 0;
                consecutiveRangedAttacks = 0;
                yield return StartCoroutine(UltimateAttack());
                break;
            case 4:
                consecutiveMeleeAttacks++;
                consecutiveRangedAttacks = 0;
                yield return StartCoroutine(TripleSlashCombo());
                break;
            case 5:
                consecutiveMeleeAttacks = 0;
                consecutiveRangedAttacks++;
                yield return StartCoroutine(CrossPatternAttack());
                break;
        }

        lastAttackType = attackType;
        isAttacking = false;
        if (spriteRenderer) spriteRenderer.color = Color.white;
    }

    int ChooseDarkSoulsAttack(float dist)
    {
        if (consecutiveMeleeAttacks >= 2)
        {
            consecutiveMeleeAttacks = 0;
            float r = Random.Range(0f, 100f);
            if (r < 35) return 2;
            else if (r < 70) return 1;
            else return 5;
        }

        if (consecutiveRangedAttacks >= 3)
        {
            consecutiveRangedAttacks = 0;
            return Random.Range(0f, 1f) < 0.6f ? 0 : 4;
        }

        if (currentPhase == BossPhase.Phase3)
        {
            if (dist < 5f)
            {
                float r = Random.Range(0f, 100f);
                if (r < 40) return 4;
                else if (r < 70) return 0;
                else return 3;
            }
            else
            {
                float r = Random.Range(0f, 100f);
                if (r < 25) return 5;
                else if (r < 50) return 2;
                else if (r < 75) return 1;
                else return 3;
            }
        }
        else if (currentPhase == BossPhase.Phase2)
        {
            if (dist < 6f)
            {
                float r = Random.Range(0f, 100f);
                if (r < 50) return 0;
                else if (r < 75) return 4;
                else return 2;
            }
            else
            {
                float r = Random.Range(0f, 100f);
                if (r < 40) return 1;
                else if (r < 70) return 2;
                else return 5;
            }
        }
        else
        {
            if (dist < 7f)
            {
                return Random.Range(0f, 1f) < 0.7f ? 0 : 2;
            }
            else
            {
                return Random.Range(0f, 1f) < 0.6f ? 1 : 2;
            }
        }
    }

    IEnumerator TripleSlashCombo()
    {
        for (int i = 0; i < 3; i++)
        {
            if (spriteRenderer) spriteRenderer.color = new Color(1f, 0.5f, 0f);
            FlipTowardsPlayer();
            yield return new WaitForSeconds(0.2f);

            if (spriteRenderer) spriteRenderer.color = Color.red;
            Vector2 dir = (player.position - transform.position).normalized;
            dir.y = 0;
            rb.AddForce(dir * (meleeDashForce * 0.7f), ForceMode2D.Impulse);

            yield return new WaitForSeconds(0.15f);
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            // OPTIMIZACIÓN: Usar buffer de colisiones
            int hitCount = Physics2D.OverlapBoxNonAlloc(meleeAttackPoint.position, meleeAttackBoxSize, 0f, hitBuffer);
            for (int h = 0; h < hitCount; h++)
            {
                if (hitBuffer[h].CompareTag("Player"))
                {
                    playerMainChar?.TakeDamage(1);
                    if (playerRb) playerRb.AddForce(dir * meleeKnockback * 0.5f, ForceMode2D.Impulse);
                }
            }
            yield return new WaitForSeconds(0.15f);
        }
    }

    IEnumerator CrossPatternAttack()
    {
        if (spriteRenderer) spriteRenderer.color = new Color(1f, 0f, 1f);
        yield return new WaitForSeconds(0.5f);

        if (GroundSpikes != null)
        {
            Vector3 center = player.position;
            center.y = initialPosition.y - 1.5f;

            for (int i = -4; i <= 4; i++)
            {
                Vector3 pos = center + new Vector3(i * 2f, 0, 0);
                pos.x = Mathf.Clamp(pos.x, minArenaX, maxArenaX);
                StartCoroutine(SpawnSpike(pos));
                yield return new WaitForSeconds(0.08f);
            }
        }
    }

    IEnumerator MeleeDashAttack()
    {
        if (spriteRenderer) spriteRenderer.color = Color.yellow;
        FlipTowardsPlayer();
        yield return new WaitForSeconds(meleeWindup);

        if (spriteRenderer) spriteRenderer.color = Color.red;
        Vector2 dir = (player.position - transform.position).normalized;
        dir.y = 0;
        rb.AddForce(dir * meleeDashForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(0.25f);
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        // OPTIMIZACIÓN: Usar buffer de colisiones
        int hitCount = Physics2D.OverlapBoxNonAlloc(meleeAttackPoint.position, meleeAttackBoxSize, 0f, hitBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            if (hitBuffer[i].CompareTag("Player"))
            {
                playerMainChar?.TakeDamage(meleeAttackDamage);
                if (playerRb) playerRb.AddForce(dir * meleeKnockback, ForceMode2D.Impulse);
            }
        }
        yield return new WaitForSeconds(0.2f);
    }

    IEnumerator SwordBarrage()
    {
        if (spriteRenderer) spriteRenderer.color = Color.cyan;
        yield return new WaitForSeconds(0.3f);

        if (FallingSwords != null)
        {
            int swordsMod = currentPhase == BossPhase.Phase3 ? swordsCount + 5 : swordsCount;

            for (int i = 0; i < swordsMod; i++)
            {
                if (player == null) break;

                Vector3 targetPos = player.position;
                if (i > 0) targetPos += new Vector3(Random.Range(-4f, 4f), 0, 0);
                targetPos.y = initialPosition.y - 1.5f;
                targetPos.x = Mathf.Clamp(targetPos.x, minArenaX, maxArenaX);

                StartCoroutine(SpawnSwordWithWarning(targetPos));
                yield return new WaitForSeconds(0.12f);
            }
        }

        if (spriteRenderer) spriteRenderer.color = Color.white;
    }

    IEnumerator SpawnSwordWithWarning(Vector3 groundPos)
    {
        GameObject warning = CreateWarningIndicator(groundPos);

        yield return new WaitForSeconds(0.6f);

        Vector3 spawnPos = groundPos + Vector3.up * 8f;
        GameObject sword = Instantiate(FallingSwords, spawnPos, Quaternion.identity);

        FallingSword fs = sword.GetComponent<FallingSword>();
        if (fs == null) fs = sword.AddComponent<FallingSword>();
        fs.Initialize(swordSpeed, 1);

        if (warning != null) Destroy(warning);
    }

    GameObject CreateWarningIndicator(Vector3 pos)
    {
        GameObject warning = new GameObject("SwordWarning");
        warning.transform.position = pos;

        SpriteRenderer sr = warning.AddComponent<SpriteRenderer>();
        sr.sprite = CreateCircleSprite();
        sr.color = new Color(1f, 0f, 0f, 0.5f);
        sr.sortingOrder = 5;

        StartCoroutine(BlinkWarning(sr));

        return warning;
    }

    IEnumerator BlinkWarning(SpriteRenderer sr)
    {
        for (int i = 0; i < 6; i++)
        {
            if (sr == null) yield break;
            sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(0.1f);
        }
    }

    Sprite CreateCircleSprite()
    {
        int res = 64;
        Texture2D tex = new Texture2D(res, res);
        Vector2 center = new Vector2(res / 2, res / 2);
        float radius = res / 2;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist < radius && dist > radius - 4)
                {
                    tex.SetPixel(x, y, Color.red);
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
    }

    IEnumerator GroundSpikesAttack()
    {
        if (spriteRenderer) spriteRenderer.color = new Color(0.5f, 0f, 0f);
        yield return new WaitForSeconds(0.5f);

        if (GroundSpikes != null)
        {
            int geysersMod = currentPhase == BossPhase.Phase3 ? geysersCount + 3 : geysersCount;

            for (int i = 0; i < geysersMod; i++)
            {
                if (player == null) break;
                Vector3 target = player.position;

                // OPTIMIZACIÓN: Usar referencia cacheada
                if (playerRb && playerRb.linearVelocity.x != 0)
                {
                    float prediction = currentPhase == BossPhase.Phase3 ? 0.7f : 0.5f;
                    target += new Vector3(playerRb.linearVelocity.x * prediction, 0, 0);
                }

                target.y = initialPosition.y - 1.5f;
                target.x = Mathf.Clamp(target.x, minArenaX, maxArenaX);

                StartCoroutine(SpawnSpike(target));
                yield return new WaitForSeconds(0.12f);
            }
        }
    }

    IEnumerator SpawnSpike(Vector3 pos)
    {
        GameObject spike = Instantiate(GroundSpikes, pos, Quaternion.identity);
        GroundSpike gs = spike.GetComponent<GroundSpike>();
        if (gs == null) gs = spike.AddComponent<GroundSpike>();
        gs.Initialize(geyserDamage);

        yield return new WaitForSeconds(geyserWarningTime);

        // OPTIMIZACIÓN: Usar buffer de colisiones
        int hitCount = Physics2D.OverlapBoxNonAlloc(pos + Vector3.up * 1f, new Vector2(1.5f, 2f), 0f, hitBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            if (hitBuffer[i].CompareTag("Player"))
            {
                playerMainChar?.TakeDamage(geyserDamage);
            }
        }
    }

    IEnumerator UltimateAttack()
    {
        isInvulnerable = true;
        float t = 0;
        Vector3 startPos = transform.position;
        Vector3 centerPos = new Vector3((minArenaX + maxArenaX) / 2, initialPosition.y + 3f, 0);

        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;
        targetVelocity = Vector2.zero;

        while (t < 1f)
        {
            transform.position = Vector3.Lerp(startPos, centerPos, t);
            t += Time.deltaTime * 2f;
            yield return null;
        }

        int waves = currentPhase == BossPhase.Phase3 ? 4 : 3;
        for (int w = 0; w < waves; w++)
        {
            for (int i = 0; i < bloodProjectiles; i++)
            {
                float angle = i * (360f / bloodProjectiles) + (w * 15f);
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                GameObject p = CreateBloodProjectile(transform.position);
                BossProjectile bp = p.AddComponent<BossProjectile>();
                bp.Initialize(dir, bloodSpeed, 1, true);
            }
            yield return new WaitForSeconds(0.4f);
        }

        rb.gravityScale = defaultGravity;

        while (transform.position.y > initialPosition.y + 0.5f)
        {
            transform.position += Vector3.down * Time.deltaTime * 6f;
            yield return null;
        }

        isInvulnerable = false;
    }

    GameObject CreateBloodProjectile(Vector3 pos)
    {
        GameObject go = new GameObject("BloodFX");
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = bloodSprite;
        sr.color = Color.red;
        sr.sortingOrder = 10;
        return go;
    }

    void FlipTowardsPlayer()
    {
        if (player == null) return;
        float x = Mathf.Abs(transform.localScale.x);
        if (player.position.x < transform.position.x)
            transform.localScale = new Vector3(-x, transform.localScale.y, 1);
        else
            transform.localScale = new Vector3(x, transform.localScale.y, 1);
    }

    IEnumerator DefensiveTeleport()
    {
        isTeleporting = true;
        targetVelocity = Vector2.zero;
        rb.linearVelocity = Vector2.zero;

        if (bossCollider) bossCollider.enabled = false;
        if (spriteRenderer) spriteRenderer.enabled = false;
        rb.gravityScale = 0;

        yield return new WaitForSeconds(teleportDelay);

        float randomX = Random.Range(minArenaX + 2f, maxArenaX - 2f);

        if (player != null)
        {
            float distToPlayer = Mathf.Abs(randomX - player.position.x);
            if (distToPlayer < 5f)
            {
                randomX = (player.position.x > (minArenaX + maxArenaX) / 2) ? minArenaX + 3f : maxArenaX - 3f;
            }
        }

        transform.position = new Vector3(randomX, initialPosition.y, 0);

        if (spriteRenderer) spriteRenderer.enabled = true;
        if (bossCollider) bossCollider.enabled = true;
        rb.gravityScale = defaultGravity;

        isTeleporting = false;
        currentHitCounter = 0;
        lastTeleportTime = Time.time;
        attackCooldownTimer = 0.8f;
    }

    public void TakeDamage(int dmg, int dir)
    {
        if (isDead || isInvulnerable || isTeleporting) return;

        currentHealth -= dmg;
        currentHitCounter++;

        if (bossHealthBarUI) bossHealthBarUI.UpdateHealth(currentHealth);

        bool canTeleport = !isAttacking &&
                          (Time.time - lastTeleportTime) > minTeleportInterval &&
                          currentHitCounter >= hitsToTriggerTeleport;

        if (canTeleport)
        {
            StartCoroutine(DefensiveTeleport());
        }
        else
        {
            StartCoroutine(FlashDamage());
        }

        if (currentHealth <= 0) Die();
    }

    IEnumerator FlashDamage()
    {
        if (spriteRenderer) spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        if (!isDead && !isAttacking && spriteRenderer)
            spriteRenderer.color = Color.white;
    }

    void Die()
    {
        isDead = true;
        UnsealArena();
        if (bossHealthBarUI) bossHealthBarUI.Hide();

        CamaraScript camara = Camera.main.GetComponent<CamaraScript>();
        if (camara != null)
        {
            camara.enModoBoss = false;
        }
        StopAllCoroutines();
        gameObject.SetActive(false);
    }

    void UpdatePhase()
    {
        float p = (float)currentHealth / maxHealth;
        if (p > 0.60f) currentPhase = BossPhase.Phase1;
        else if (p > 0.30f) currentPhase = BossPhase.Phase2;
        else currentPhase = BossPhase.Phase3;
    }

    public void ResetState()
{
    lastResetTime = Time.time;
    
    StopAllCoroutines();

    // 🎯 Destruir barra de vida
    if (bossHealthBarUI != null)
    {
        Destroy(bossHealthBarUI.gameObject);
        bossHealthBarUI = null;
    }

    // 🎯 Desactivar modo boss en cámara
    CamaraScript camara = Camera.main?.GetComponent<CamaraScript>();
    if (camara != null)
    {
        camara.enModoBoss = false;
    }

    // Resetear stats
    currentHealth = maxHealth;
    transform.position = initialPosition;
    transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
    gameObject.SetActive(true);

    isDead = false;
    isAttacking = false;
    isTeleporting = false;
    isInvulnerable = false;
    currentHitCounter = 0;
    consecutiveMeleeAttacks = 0;
    consecutiveRangedAttacks = 0;
    lastTeleportTime = -999f;
    arenaSealed = false;  // ✅ IMPORTANTE
    healthBarActivated = false;
    
    // ✅ FORZAR APERTURA DE PUERTAS
    UnsealArena();
    
    // Resetear física
    targetVelocity = Vector2.zero;
    currentVelocity = Vector2.zero;

    if (spriteRenderer)
    {
        spriteRenderer.enabled = true;
        spriteRenderer.color = Color.white;
    }
    
    if (bossCollider) bossCollider.enabled = true;
    
    if (rb)
    {
        rb.gravityScale = defaultGravity;
        rb.linearVelocity = Vector2.zero;
    }
    
    Debug.Log("✅ Boss reseteado - Puertas abiertas - Cooldown: " + resetCooldown + "s");
}

    public bool CanBeAbsorbed() => isDead;
    public void OnAbsorbed() { Destroy(gameObject); }
    public bool IsBoss => true;

    void SetDoorsState(bool active)
    {
        if (leftDoor) leftDoor.SetActive(active);
        if (rightDoor) rightDoor.SetActive(active);
    }

    void SealArena()
    {
        arenaSealed = true;
        SetDoorsState(true);
    }

    void UnsealArena()
    {
        arenaSealed = false;
        SetDoorsState(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        if (meleeAttackPoint != null)
            Gizmos.DrawWireCube(meleeAttackPoint.position, meleeAttackBoxSize);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, optimalDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, retreatDistance);
    }
   private float GetGroundY(float xPosition)
{
    // Punto de inicio MUY arriba
    Vector2 rayStart = new Vector2(xPosition, transform.position.y + 30f);
    
    // Raycast hacia abajo con TODOS los layers que podrían ser suelo
    RaycastHit2D hit = Physics2D.Raycast(
        rayStart,
        Vector2.down,
        100f, // Distancia muy larga
        ~0 // Detectar TODOS los layers
    );

    // Debug visual (línea verde si detecta, roja si no)
    if (hit.collider != null)
    {
        Debug.DrawLine(rayStart, hit.point, Color.green, 1f);
        return hit.point.y;
    }
    else
    {
        Debug.DrawLine(rayStart, rayStart + Vector2.down * 100f, Color.red, 1f);
        Debug.LogWarning($"⚠️ GetGroundY no detectó suelo en X={xPosition}");
        
        // Fallback: usar la Y inicial del boss
        return initialPosition.y;
    }
}
}