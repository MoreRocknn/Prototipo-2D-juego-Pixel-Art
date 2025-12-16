using UnityEngine;
using System.Collections;
using UnityEngine.UI; // Necesario para manejar UI

public class BossController : MonoBehaviour, IAbsorbable, IResettable
{
    [Header("=== EL REY INFECTADO (AGRESIVO) ===")]
    public int maxHealth = 50;
    private int currentHealth;
    public float moveSpeed = 8f;
    public float detectionRange = 30f;

    [Header("=== IA DEPREDADORA ===")]
    public float repositionSpeed = 12f;

    [Header("=== MOVIMIENTO: PASO VIRAL ===")]
    public int hitsToTriggerTeleport = 3;
    public float teleportDelay = 0.5f;
    private int currentHitCounter = 0;
    private bool isTeleporting = false;

    [Header("=== SISTEMA DE COMBATE ===")]
    public float minAttackCooldown = 0.6f;
    public float maxAttackCooldown = 1.2f;
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

    [Header("UI BOSS (Importante)")]
    public GameObject bossHealthBarPrefab; // Arrastra el PREFAB de la barra
    public string bossName = "EL REY PACIENTE";
    private BossHealthBar bossHealthBarUI;
    private bool arenaSealed = false;
    private bool healthBarActivated = false;

    [Header("PREFABS DE ATAQUE")]
    public GameObject FallingSwords;
    public GameObject GroundSpikes;

    // Límites
    private float minArenaX;
    private float maxArenaX;

    // Internas
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

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bossCollider = GetComponent<Collider2D>();
        initialPosition = transform.position;
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
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
            // Fallback si no hay puertas asignadas
            minArenaX = initialPosition.x - 12f;
            maxArenaX = initialPosition.x + 12f;
        }
    }

    void GenerateBloodSprite()
    {
        int res = 32;
        Texture2D tex = new Texture2D(res, res);
        for (int y = 0; y < res; y++) for (int x = 0; x < res; x++) tex.SetPixel(x, y, Color.red);
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

        // --- SEGURIDAD ANTI-DESPAWN VISUAL ---
        // Si no estamos teletransportándonos, asegurar que el sprite es visible
        if (!isTeleporting && spriteRenderer != null && !spriteRenderer.enabled)
        {
            spriteRenderer.enabled = true;
            if (bossCollider) bossCollider.enabled = true;
            rb.gravityScale = defaultGravity;
        }

        // Clamp de posición (Evitar que salga de la arena)
        float clampedX = Mathf.Clamp(transform.position.x, minArenaX, maxArenaX);
        if (Mathf.Abs(transform.position.x - clampedX) > 0.5f) // Margen de error
        {
            transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }

        float dist = Vector2.Distance(transform.position, player.position);

        // Activar barra de vida si estamos cerca
        if (!healthBarActivated && dist <= 20f) ActivateBossHealthBar();
        if (!arenaSealed && dist <= doorCloseDistance) SealArena();

        if (dist <= detectionRange)
        {
            HandleCombat(dist);
            if (!isAttacking && !isTeleporting) FlipTowardsPlayer();
        }

        UpdatePhase();
    }

    // --- CORRECCIÓN BARRA DE VIDA ---
    void ActivateBossHealthBar()
    {
        if (!healthBarActivated && bossHealthBarPrefab)
        {
            // 1. Instanciamos la barra
            GameObject barObj = Instantiate(bossHealthBarPrefab);

            // 2. Buscamos el Canvas en la escena
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                // 3. Hacemos que la barra sea hija del Canvas para que se vea
                barObj.transform.SetParent(canvas.transform, false);
            }
            else
            {
                Debug.LogError("¡NO HAY CANVAS EN LA ESCENA! La barra de vida no se puede mostrar.");
            }

            bossHealthBarUI = barObj.GetComponent<BossHealthBar>();
            if (bossHealthBarUI)
            {
                bossHealthBarUI.Initialize(bossName, maxHealth);
                healthBarActivated = true;
            }
        }
    }

    // --- DAÑO POR CONTACTO ---
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead || isTeleporting) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time > lastBodyDamageTime + bodyDamageCooldown)
            {
                collision.gameObject.GetComponent<MainChar>()?.TakeDamage(bodyContactDamage);
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
            StartCoroutine(PerformAggressiveAttack());
            float mod = (currentPhase == BossPhase.Phase3) ? 0.5f : 1f;
            attackCooldownTimer = Random.Range(minAttackCooldown, maxAttackCooldown) * mod;
        }
        else
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            Vector2 dir = (player.position - transform.position).normalized;

            if (dist > 5f)
                rb.linearVelocity = new Vector2(dir.x * moveSpeed, rb.linearVelocity.y);
            else
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    IEnumerator PerformAggressiveAttack()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero;

        float roll = Random.Range(0f, 100f);
        int attackType = 0;

        if (currentPhase == BossPhase.Phase1) attackType = (roll < 50) ? 0 : 1;
        else if (currentPhase == BossPhase.Phase2)
        {
            if (roll < 40) attackType = 0;
            else if (roll < 70) attackType = 1;
            else attackType = 2;
        }
        else
        {
            if (roll < 25) attackType = 3;
            else if (roll < 55) attackType = 0;
            else if (roll < 80) attackType = 1;
            else attackType = 2;
        }

        switch (attackType)
        {
            case 0: yield return StartCoroutine(MeleeDashAttack()); break;
            case 1: yield return StartCoroutine(SwordBarrage()); break;
            case 2: yield return StartCoroutine(GroundSpikesAttack()); break;
            case 3: yield return StartCoroutine(UltimateAttack()); break;
        }

        isAttacking = false;
        if (spriteRenderer) spriteRenderer.color = Color.white;
    }

    // --- ATAQUES ---
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
        rb.linearVelocity = Vector2.zero;

        Collider2D[] hits = Physics2D.OverlapBoxAll(meleeAttackPoint.position, meleeAttackBoxSize, 0f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                hit.GetComponent<MainChar>()?.TakeDamage(meleeAttackDamage);
                Rigidbody2D prb = hit.GetComponent<Rigidbody2D>();
                if (prb) prb.AddForce(dir * meleeKnockback, ForceMode2D.Impulse);
            }
        }
        yield return new WaitForSeconds(0.2f);
    }

    IEnumerator SwordBarrage()
    {
        if (spriteRenderer) spriteRenderer.color = Color.cyan;
        yield return new WaitForSeconds(0.3f);
        if (spriteRenderer) spriteRenderer.color = Color.white;

        if (FallingSwords != null)
        {
            for (int i = 0; i < swordsCount; i++)
            {
                if (player == null) break;
                Vector3 spawnPos = transform.position + new Vector3(Random.Range(-2f, 2f), 4f, 0);
                Vector2 dir = (player.position - spawnPos).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                Quaternion rotation = Quaternion.AngleAxis(angle - 90, Vector3.forward);

                GameObject sword = Instantiate(FallingSwords, spawnPos, rotation);
                BossProjectile bp = sword.GetComponent<BossProjectile>();
                if (bp == null) bp = sword.AddComponent<BossProjectile>();
                bp.Initialize(dir, swordSpeed, 1, false);

                yield return new WaitForSeconds(0.08f);
            }
        }
    }

    IEnumerator GroundSpikesAttack()
    {
        if (spriteRenderer) spriteRenderer.color = new Color(0.5f, 0f, 0f);
        yield return new WaitForSeconds(0.5f);

        if (GroundSpikes != null)
        {
            for (int i = 0; i < geysersCount; i++)
            {
                if (player == null) break;
                Vector3 target = player.position;
                if (player.GetComponent<Rigidbody2D>().linearVelocity.x != 0)
                    target += new Vector3(player.GetComponent<Rigidbody2D>().linearVelocity.x * 0.5f, 0, 0);

                target.y = initialPosition.y - 1.5f;
                target.x = Mathf.Clamp(target.x, minArenaX, maxArenaX);

                StartCoroutine(SpawnSpike(target));
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    IEnumerator SpawnSpike(Vector3 pos)
    {
        GameObject spike = Instantiate(GroundSpikes, pos, Quaternion.identity);
        yield return new WaitForSeconds(geyserWarningTime);
        Collider2D[] hits = Physics2D.OverlapBoxAll(pos + Vector3.up * 1f, new Vector2(1.5f, 2f), 0f);
        foreach (var h in hits)
        {
            if (h.CompareTag("Player")) h.GetComponent<MainChar>()?.TakeDamage(geyserDamage);
        }
        yield return new WaitForSeconds(0.5f);
        Destroy(spike);
    }

    IEnumerator UltimateAttack()
    {
        isInvulnerable = true;
        float t = 0;
        Vector3 startPos = transform.position;
        Vector3 centerPos = new Vector3((minArenaX + maxArenaX) / 2, initialPosition.y + 3f, 0);

        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero; // Frenar al flotar

        while (t < 1f)
        {
            transform.position = Vector3.Lerp(startPos, centerPos, t);
            t += Time.deltaTime * 2f;
            yield return null;
        }

        int waves = 3;
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

        while (transform.position.y > initialPosition.y)
        {
            transform.position += Vector3.down * Time.deltaTime * 5f;
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
        float x = transform.localScale.x;
        if (player.position.x < transform.position.x && x > 0) transform.localScale = new Vector3(-x, transform.localScale.y, 1);
        else if (player.position.x > transform.position.x && x < 0) transform.localScale = new Vector3(-x, transform.localScale.y, 1);
    }

    // --- TELETRANSPORTE SEGURO ---
    IEnumerator DefensiveTeleport()
    {
        isTeleporting = true;
        isAttacking = false;
        StopAllCoroutines(); // Cancela ataques en curso

        // 1. Apagar visuales y gravedad (para que no caiga al infinito)
        if (bossCollider) bossCollider.enabled = false;
        if (spriteRenderer) spriteRenderer.enabled = false;
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;

        yield return new WaitForSeconds(teleportDelay);

        // 2. Calcular nueva posición
        float randomX = Random.Range(minArenaX + 2f, maxArenaX - 2f);

        // Evitar aparecer encima del jugador
        if (Vector2.Distance(player.position, new Vector2(randomX, initialPosition.y)) < 5f)
            randomX = (player.position.x > (minArenaX + maxArenaX) / 2) ? minArenaX + 3f : maxArenaX - 3f;

        // 3. Set Position con Z=0 estricto para evitar problemas de visualización
        transform.position = new Vector3(randomX, initialPosition.y, 0);

        // 4. Reactivar
        if (spriteRenderer) spriteRenderer.enabled = true;
        if (bossCollider) bossCollider.enabled = true;
        rb.gravityScale = defaultGravity;

        isTeleporting = false;
        currentHitCounter = 0;
        attackCooldownTimer = 0.5f;
    }

    public void TakeDamage(int dmg, int dir)
    {
        if (isDead || isTeleporting || isInvulnerable) return;

        currentHealth -= dmg;
        currentHitCounter++;
        if (bossHealthBarUI) bossHealthBarUI.UpdateHealth(currentHealth);

        if (currentHitCounter >= hitsToTriggerTeleport && !isAttacking)
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
        if (!isDead && !isAttacking) spriteRenderer.color = Color.white;
    }

    void Die()
    {
        isDead = true;
        UnsealArena();
        if (bossHealthBarUI) bossHealthBarUI.Hide();
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
        StopAllCoroutines();
        currentHealth = maxHealth;
        transform.position = initialPosition;
        gameObject.SetActive(true);
        isDead = false;
        isAttacking = false;
        isTeleporting = false;

        // Reset vitales
        if (spriteRenderer) spriteRenderer.enabled = true;
        if (bossCollider) bossCollider.enabled = true;
        if (rb) rb.gravityScale = defaultGravity;

        SetDoorsState(false);
    }
    public bool CanBeAbsorbed() => isDead;
    public void OnAbsorbed() { Destroy(gameObject); }
    public bool IsBoss => true;
    void SetDoorsState(bool a) { if (leftDoor) leftDoor.SetActive(a); if (rightDoor) rightDoor.SetActive(a); }
    void SealArena() { arenaSealed = true; SetDoorsState(true); }
    void UnsealArena() { arenaSealed = false; SetDoorsState(false); }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.DrawWireCube(meleeAttackPoint != null ? meleeAttackPoint.position : transform.position, meleeAttackBoxSize);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(transform.position + Vector3.down * 1.5f, new Vector2(1.5f, 2f));
    }
}