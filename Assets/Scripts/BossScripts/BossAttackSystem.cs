// ============================================================
// BossAttackSystem.cs
// RESPONSABILIDAD: Los 6 ataques del boss y la lógica de
//                  decisión de cuál usar según fase y distancia.
//
// Para usarlo: añádelo al mismo GameObject que BossController.
// ============================================================

using UnityEngine;
using System.Collections;

public class BossAttackSystem : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // INSPECTOR — parámetros de cada ataque
    // ─────────────────────────────────────────────────────────
    [Header("=== TIMING DE ATAQUES ===")]
    public float minAttackCooldown = 1.2f;
    public float maxAttackCooldown = 2.0f;

    [Header("=== ESPADAS: POSICIÓN ===")]
    [Tooltip("Y del suelo donde aparece el marcador y aterrizan las espadas. Ajusta hasta que quede en el suelo de tu escena.")]
    public float swordGroundY = -3f;

    [Tooltip("Desde qué altura caen las espadas (relativo al suelo). Más alto = más tiempo de caída.")]
    public float swordSpawnHeight = 10f;

    [Header("=== ATAQUE MELEE ===")]
    public Transform meleeAttackPoint;
    public Vector2 meleeAttackBoxSize = new Vector2(6f, 4f);
    public int meleeAttackDamage = 2;
    public float meleeWindup = 0.4f;  // tiempo de "carga" antes del golpe
    public float meleeDashForce = 50f;
    public float meleeKnockback = 20f;

    [Header("=== LLUVIA DE ESPADAS ===")]
    public GameObject FallingSwords;
    public int swordsCount = 10;
    public float swordSpeed = 22f;

    [Header("=== PINCHOS DEL SUELO ===")]
    public GameObject GroundSpikes;
    public int geysersCount = 6;
    public float geyserWarningTime = 0.6f;
    public int geyserDamage = 1;

    [Header("=== ATAQUE DEFINITIVO ===")]
    public int bloodProjectiles = 20;
    public float bloodSpeed = 11f;

    // ─────────────────────────────────────────────────────────
    // ESTADO INTERNO
    // ─────────────────────────────────────────────────────────
    private float attackCooldownTimer = 1f;
    private int consecutiveMeleeAttacks = 0;
    private int consecutiveRangedAttacks = 0;
    private int lastAttackType = -1;

    // Sprite rojo generado por código para los proyectiles
    private Sprite bloodSprite;

    // Referencias
    private BossData data;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private BossMovementAI movement;

    // =========================================================
    // INITIALIZE
    // =========================================================
    public void Initialize(BossData data, Rigidbody2D rb, SpriteRenderer sr)
    {
        this.data = data;
        this.rb = rb;
        this.spriteRenderer = sr;
        movement = GetComponent<BossMovementAI>();

        GenerateBloodSprite();
        SetupMeleeAttackPoint();
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

    void SetupMeleeAttackPoint()
    {
        if (meleeAttackPoint != null) return;
        GameObject pt = new GameObject("MeleePt");
        pt.transform.SetParent(transform);
        pt.transform.localPosition = new Vector3(2f, 0, 0);
        meleeAttackPoint = pt.transform;
    }

    // =========================================================
    // HANDLE COMBAT — Gestiona el timing de ataques
    // Llamado cada frame desde BossController.Update()
    // =========================================================
    public void HandleCombat(float dist)
    {
        if (data.isAttacking || data.isTeleporting) return;

        attackCooldownTimer -= Time.deltaTime;

        if (attackCooldownTimer <= 0f)
        {
            // ¡Hora de atacar!
            StartCoroutine(PerformAttack(dist));

            // Cooldown más corto en fases avanzadas → ataca más rápido
            float phaseMod = data.currentPhase == BossData.BossPhase.Phase3 ? 0.5f :
                             data.currentPhase == BossData.BossPhase.Phase2 ? 0.7f : 1.0f;
            attackCooldownTimer = Random.Range(minAttackCooldown, maxAttackCooldown) * phaseMod;
        }
        else
        {
            // Entre ataques: moverse inteligentemente
            movement.HandleMovement(dist);
        }
    }

    // =========================================================
    // PERFORM ATTACK — Dispatcher: elige y ejecuta el ataque
    // =========================================================
    IEnumerator PerformAttack(float dist)
    {
        data.isAttacking = true;
        movement.StopMovement();
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        int attackType = ChooseAttack(dist);

        switch (attackType)
        {
            case 0: consecutiveMeleeAttacks++; consecutiveRangedAttacks = 0; yield return StartCoroutine(MeleeDashAttack()); break;
            case 1: consecutiveMeleeAttacks = 0; consecutiveRangedAttacks++; yield return StartCoroutine(SwordBarrage()); break;
            case 2: consecutiveMeleeAttacks = 0; consecutiveRangedAttacks++; yield return StartCoroutine(GroundSpikesAttack()); break;
            case 3: consecutiveMeleeAttacks = 0; consecutiveRangedAttacks = 0; yield return StartCoroutine(UltimateAttack()); break;
            case 4: consecutiveMeleeAttacks++; consecutiveRangedAttacks = 0; yield return StartCoroutine(TripleSlashCombo()); break;
            case 5: consecutiveMeleeAttacks = 0; consecutiveRangedAttacks++; yield return StartCoroutine(CrossPatternAttack()); break;
        }

        lastAttackType = attackType;
        data.isAttacking = false;
        if (spriteRenderer) spriteRenderer.color = Color.white;
    }

    // =========================================================
    // CHOOSE ATTACK — Decide qué ataque usar
    // Tiene en cuenta: anti-repetición, fase y distancia
    // =========================================================
    int ChooseAttack(float dist)
    {
        // Anti-repetición: 2 melees seguidos → forzar a distancia
        if (consecutiveMeleeAttacks >= 2)
        {
            consecutiveMeleeAttacks = 0;
            float r = Random.Range(0f, 100f);
            if (r < 35) return 2;
            else if (r < 70) return 1;
            else return 5;
        }

        // Anti-repetición: 3 a distancia seguidos → forzar melee
        if (consecutiveRangedAttacks >= 3)
        {
            consecutiveRangedAttacks = 0;
            return Random.value < 0.6f ? 0 : 4;
        }

        // Decisión por fase y distancia
        switch (data.currentPhase)
        {
            case BossData.BossPhase.Phase3:
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

            case BossData.BossPhase.Phase2:
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

            default: // Phase1
                if (dist < 7f) return Random.value < 0.7f ? 0 : 2;
                else return Random.value < 0.6f ? 1 : 2;
        }
    }

    // =========================================================
    // ATAQUE 0: MELEE DASH — Embestida simple
    // Telegrafía amarilla → dash → comprueba colisión
    // =========================================================
    IEnumerator MeleeDashAttack()
    {
        if (spriteRenderer) spriteRenderer.color = Color.yellow;
        movement.FlipTowardsPlayer();
        yield return new WaitForSeconds(meleeWindup); // tiempo para esquivar

        if (spriteRenderer) spriteRenderer.color = Color.red;
        Vector2 dir = (data.player.position - transform.position).normalized;
        dir.y = 0; // solo moverse en horizontal
        rb.AddForce(dir * meleeDashForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(0.25f);
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        // OverlapBoxNonAlloc: reutiliza data.hitBuffer → sin garbage
        int hitCount = Physics2D.OverlapBoxNonAlloc(
            meleeAttackPoint.position, meleeAttackBoxSize, 0f, data.hitBuffer
        );
        for (int i = 0; i < hitCount; i++)
        {
            if (data.hitBuffer[i].CompareTag("Player"))
            {
                data.playerMainChar?.TakeDamage(meleeAttackDamage);
                if (data.playerRb) data.playerRb.AddForce(dir * meleeKnockback, ForceMode2D.Impulse);
            }
        }
        yield return new WaitForSeconds(0.2f);
    }

    // =========================================================
    // ATAQUE 4: TRIPLE SLASH — 3 dashes rápidos seguidos
    // =========================================================
    IEnumerator TripleSlashCombo()
    {
        for (int i = 0; i < 3; i++)
        {
            if (spriteRenderer) spriteRenderer.color = new Color(1f, 0.5f, 0f);
            movement.FlipTowardsPlayer();
            yield return new WaitForSeconds(0.2f);

            if (spriteRenderer) spriteRenderer.color = Color.red;
            Vector2 dir = (data.player.position - transform.position).normalized;
            dir.y = 0;
            rb.AddForce(dir * (meleeDashForce * 0.7f), ForceMode2D.Impulse);

            yield return new WaitForSeconds(0.15f);
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            int hitCount = Physics2D.OverlapBoxNonAlloc(
                meleeAttackPoint.position, meleeAttackBoxSize, 0f, data.hitBuffer
            );
            for (int h = 0; h < hitCount; h++)
            {
                if (data.hitBuffer[h].CompareTag("Player"))
                {
                    data.playerMainChar?.TakeDamage(1);
                    if (data.playerRb) data.playerRb.AddForce(dir * meleeKnockback * 0.5f, ForceMode2D.Impulse);
                }
            }
            yield return new WaitForSeconds(0.15f);
        }
    }

    // =========================================================
    // ATAQUE 1: SWORD BARRAGE — Lluvia de espadas del cielo
    // =========================================================
    IEnumerator SwordBarrage()
    {
        if (spriteRenderer) spriteRenderer.color = Color.cyan;
        yield return new WaitForSeconds(0.3f);
        if (FallingSwords == null) yield break;

        int total = data.currentPhase == BossData.BossPhase.Phase3 ? swordsCount + 5 : swordsCount;
        for (int i = 0; i < total; i++)
        {
            if (data.player == null) break;
            Vector3 targetPos = data.player.position;
            if (i > 0) targetPos += new Vector3(Random.Range(-4f, 4f), 0, 0);
            targetPos.y = swordGroundY;
            targetPos.x = Mathf.Clamp(targetPos.x, data.minArenaX, data.maxArenaX);
            StartCoroutine(SpawnSwordWithWarning(targetPos));
            yield return new WaitForSeconds(0.12f);
        }
        if (spriteRenderer) spriteRenderer.color = Color.white;
    }

    IEnumerator SpawnSwordWithWarning(Vector3 groundPos)
    {
        GameObject warning = CreateWarningIndicator(groundPos);
        yield return new WaitForSeconds(0.6f);

        Vector3 spawnPos = new Vector3(groundPos.x, swordGroundY + swordSpawnHeight, groundPos.z);
        GameObject sword = Instantiate(FallingSwords, spawnPos, Quaternion.identity);
        FallingSword fs = sword.GetComponent<FallingSword>() ?? sword.AddComponent<FallingSword>();
        fs.Initialize(swordSpeed, 1);
        if (warning != null) Destroy(warning);
    }

    // =========================================================
    // ATAQUE 2: GROUND SPIKES — Pinchos con predicción
    // =========================================================
    IEnumerator GroundSpikesAttack()
    {
        if (spriteRenderer) spriteRenderer.color = new Color(0.5f, 0f, 0f);
        yield return new WaitForSeconds(0.5f);
        if (GroundSpikes == null) yield break;

        int total = data.currentPhase == BossData.BossPhase.Phase3 ? geysersCount + 3 : geysersCount;
        for (int i = 0; i < total; i++)
        {
            if (data.player == null) break;

            // Calcular X objetivo con predicción de movimiento
            float targetX = data.player.position.x;
            if (data.playerRb != null && data.playerRb.linearVelocity.x != 0)
            {
                float pred = data.currentPhase == BossData.BossPhase.Phase3 ? 0.7f : 0.5f;
                targetX += data.playerRb.linearVelocity.x * pred;
            }
            targetX = Mathf.Clamp(targetX, data.minArenaX, data.maxArenaX);

            // Spawnear alto: GroundSpike hace su propio raycast al suelo
            Vector3 spawnPos = new Vector3(targetX, data.player.position.y + 10f, 0f);
            SpawnSpike(spawnPos);

            yield return new WaitForSeconds(0.15f);
        }
    }

    // No es Coroutine — GroundSpike gestiona su propio ciclo (aviso→emerge→daña→desaparece)
    void SpawnSpike(Vector3 pos)
    {
        GameObject spike = Instantiate(GroundSpikes, pos, Quaternion.identity);
        GroundSpike gs = spike.GetComponent<GroundSpike>() ?? spike.AddComponent<GroundSpike>();
        gs.Initialize(geyserDamage);
    }

    // =========================================================
    // ATAQUE 5: CROSS PATTERN — Línea horizontal de pinchos
    // =========================================================
    IEnumerator CrossPatternAttack()
    {
        if (spriteRenderer) spriteRenderer.color = new Color(1f, 0f, 1f);
        yield return new WaitForSeconds(0.5f);
        if (GroundSpikes == null) yield break;

        float centerX = data.player.position.x;
        float spawnY = data.player.position.y + 10f; // alto: GroundSpike detecta el suelo

        for (int i = -4; i <= 4; i++)
        {
            float x = Mathf.Clamp(centerX + i * 2f, data.minArenaX, data.maxArenaX);
            SpawnSpike(new Vector3(x, spawnY, 0f));
            yield return new WaitForSeconds(0.08f);
        }
    }

    // =========================================================
    // ATAQUE 3: ULTIMATE — Sube al centro, dispara 360°, baja
    // =========================================================
    IEnumerator UltimateAttack()
    {
        data.isInvulnerable = true;

        // Subir al centro interpolando la posición manualmente
        float t = 0f;
        Vector3 startPos = transform.position;
        Vector3 centerPos = new Vector3(
            (data.minArenaX + data.maxArenaX) / 2f,
            data.initialPosition.y + 3f, 0
        );
        rb.gravityScale = 0; // desactivar gravedad para volar
        rb.linearVelocity = Vector2.zero;

        while (t < 1f)
        {
            transform.position = Vector3.Lerp(startPos, centerPos, t);
            t += Time.deltaTime * 2f; // llega en ~0.5 segundos
            yield return null;        // pausar hasta el próximo frame
        }

        // Oleadas de proyectiles en 360°
        int waves = data.currentPhase == BossData.BossPhase.Phase3 ? 4 : 3;
        for (int w = 0; w < waves; w++)
        {
            for (int i = 0; i < bloodProjectiles; i++)
            {
                // Distribuir uniformemente en círculo, rotando 15° por oleada
                float angle = i * (360f / bloodProjectiles) + (w * 15f);
                // Cos y Sin convierten el ángulo en una dirección 2D
                Vector2 dir = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad)
                );
                GameObject p = CreateBloodProjectile(transform.position);
                BossProjectile bp = p.AddComponent<BossProjectile>();
                bp.Initialize(dir, bloodSpeed, 1, true);
            }
            yield return new WaitForSeconds(0.4f);
        }

        // Bajar al suelo restaurando la gravedad
        rb.gravityScale = data.defaultGravity;
        while (transform.position.y > data.initialPosition.y + 0.5f)
        {
            transform.position += Vector3.down * Time.deltaTime * 6f;
            yield return null;
        }

        data.isInvulnerable = false;
    }

    // =========================================================
    // HELPERS — Indicadores visuales
    // =========================================================

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
        Vector2 center = new Vector2(res / 2f, res / 2f);
        float radius = res / 2f;
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                tex.SetPixel(x, y, (d < radius && d > radius - 4f) ? Color.red : Color.clear);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
    }

    GameObject CreateBloodProjectile(Vector3 pos)
    {
        GameObject go = new GameObject("BloodProjectile");
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = bloodSprite; sr.color = Color.red; sr.sortingOrder = 10;
        return go;
    }

    // =========================================================
    // RESET
    // =========================================================
    public void ResetAttacks()
    {
        attackCooldownTimer = 1f;
        consecutiveMeleeAttacks = 0;
        consecutiveRangedAttacks = 0;
        lastAttackType = -1;
    }
}