// ============================================================
// BossController.cs
// RESPONSABILIDAD: Coordinador principal. Es el "director de
//                  orquesta": inicializa todo, ejecuta el
//                  bucle principal (Update) y delega cada
//                  tarea al componente especializado.
//
// Este script es intencionalmente PEQUEÑO. No hace nada por
// sí mismo — llama a los otros componentes para que actúen.
// ============================================================

using UnityEngine;
using System.Collections;

public class BossController : MonoBehaviour, IAbsorbable, IResettable
{
    // ─────────────────────────────────────────────────────────
    // INSPECTOR — configuración general del boss
    // ─────────────────────────────────────────────────────────
    [Header("=== DETECCIÓN ===")]
    public float detectionRange = 30f;
    public float doorCloseDistance = 15f;

    [Header("=== REFERENCIAS DE ESCENA ===")]
    public GameObject leftDoor;
    public GameObject rightDoor;

    [Header("=== UI BOSS ===")]
    public GameObject bossHealthBarPrefab;
    public string bossName = "EL REY PACIENTE";

    // ─────────────────────────────────────────────────────────
    // REFERENCIAS A LOS COMPONENTES HERMANOS
    // ─────────────────────────────────────────────────────────
    [HideInInspector] public BossData data;
    [HideInInspector] public BossHealth health;
    [HideInInspector] public BossMovementAI movement;
    [HideInInspector] public BossAttackSystem attacks;
    [HideInInspector] public BossTeleport teleport;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D bossCollider;
    private float _detectionCooldown = 0f;

    // =========================================================
    // AWAKE
    // =========================================================
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bossCollider = GetComponent<Collider2D>();

        data = GetComponent<BossData>();
        health = GetComponent<BossHealth>();
        movement = GetComponent<BossMovementAI>();
        attacks = GetComponent<BossAttackSystem>();
        teleport = GetComponent<BossTeleport>();

        data.initialPosition = transform.position;
        ConfigurePhysics();
    }

    // =========================================================
    // START
    // =========================================================
    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            data.player = playerObj.transform;
            data.playerMainChar = playerObj.GetComponent<PlayerCore>();
            data.playerRb = playerObj.GetComponent<Rigidbody2D>();
        }

        CalculateArenaBounds();

        health.Initialize(data, rb, spriteRenderer, bossCollider, this);
        movement.Initialize(data, rb, spriteRenderer, bossCollider);
        attacks.Initialize(data, rb, spriteRenderer);
        teleport.Initialize(data, rb, spriteRenderer, bossCollider);

        if (AbilityAbsorptionManager.Instance != null)
            AbilityAbsorptionManager.Instance.RegisterResettable(this);

        SetDoorsState(false);
    }

    // =========================================================
    // UPDATE
    // =========================================================
    void Update()
    {
        if (_detectionCooldown > 0f)
        {
            _detectionCooldown -= Time.deltaTime;
            return;
        }

        if (data.isDead || data.player == null) return;

        if (!data.isTeleporting && !data.isAttacking)
            movement.EnsureVisibility();

        movement.ClampToArena();

        float dist = Vector2.Distance(transform.position, data.player.position);

        if (!data.healthBarActivated && dist <= detectionRange)
            ActivateBossHealthBar();

        if (!data.arenaSealed && dist <= doorCloseDistance)
            SealArena();

        if (dist <= detectionRange)
        {
            attacks.HandleCombat(dist);

            if (!data.isAttacking && !data.isTeleporting)
                movement.FlipTowardsPlayer();
        }

        UpdatePhase();
    }

    // =========================================================
    // FIXEDUPDATE
    // =========================================================
    void FixedUpdate()
    {
        if (data.isDead || data.player == null) return;
        if (data.isAttacking || data.isTeleporting) return;
        movement.ApplyMovement();
    }

    // =========================================================
    // FASE
    // =========================================================
    void UpdatePhase()
    {
        float hp = (float)health.currentHealth / health.maxHealth;

        if (hp > 0.60f) data.currentPhase = BossData.BossPhase.Phase1;
        else if (hp > 0.30f) data.currentPhase = BossData.BossPhase.Phase2;
        else data.currentPhase = BossData.BossPhase.Phase3;
    }

    // =========================================================
    // FÍSICA
    // =========================================================
    void ConfigurePhysics()
    {
        if (rb == null) return;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = 5000f;
        rb.linearDamping = 2f;
        rb.gravityScale = 3f;
        data.defaultGravity = rb.gravityScale;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    // =========================================================
    // ARENA
    // =========================================================
    void CalculateArenaBounds()
    {
        if (leftDoor != null && rightDoor != null)
        {
            data.minArenaX = leftDoor.transform.position.x + 2f;
            data.maxArenaX = rightDoor.transform.position.x - 2f;
        }
        else
        {
            data.minArenaX = data.initialPosition.x - 12f;
            data.maxArenaX = data.initialPosition.x + 12f;
        }
    }

    public void SealArena()
    {
        data.arenaSealed = true;
        SetDoorsState(true);
    }

    public void UnsealArena()
    {
        data.arenaSealed = false;
        SetDoorsState(false);
    }

    void SetDoorsState(bool active)
    {
        if (leftDoor) leftDoor.SetActive(active);
        if (rightDoor) rightDoor.SetActive(active);
    }

    // =========================================================
    // UI — Barra de vida
    // =========================================================
    void ActivateBossHealthBar()
    {
        if (data.healthBarActivated || bossHealthBarPrefab == null) return;

        GameObject barObj = Instantiate(bossHealthBarPrefab);
        health.bossHealthBarUI = barObj.GetComponent<BossHealthBar>();

        if (health.bossHealthBarUI != null)
        {
            health.bossHealthBarUI.Initialize(bossName, health.maxHealth);
            data.healthBarActivated = true;

            CamaraScript camara = Camera.main.GetComponent<CamaraScript>();
            if (camara != null) { camara.enModoBoss = true; camara.SnapToPlayer(); }
        }
        else
        {
            Debug.LogError("El prefab no tiene el componente BossHealthBar");
            Destroy(barObj);
        }
    }

    // =========================================================
    // COMPATIBILIDAD CON SCRIPTS EXTERNOS
    // =========================================================
    public void TakeDamage(int dmg, int dir)
    {
        health.TakeDamage(dmg, dir);
        teleport.RegisterHit(data.isAttacking);
    }

    public BossHealthBar bossHealthBarUI
    {
        get => health != null ? health.bossHealthBarUI : null;
        set { if (health != null) health.bossHealthBarUI = value; }
    }

    // =========================================================
    // IAbsorbable
    // =========================================================
    public bool CanBeAbsorbed() => data.isDead;
    public void OnAbsorbed() { Destroy(gameObject); }
    public bool IsBoss => true;

    // =========================================================
    // IResettable — Llamado por PlayerHealth.ResetearEnemigos()
    //               mientras la pantalla está en negro.
    //               BossHealth.ResetOnPlayerDeath() llama a este
    //               método para resetear la IA y la arena.
    // =========================================================
    public void ResetOnPlayerDeath()
    {
        StopAllCoroutines();

        // Destruir barra de vida anterior (se recreará al entrar en rango)
        if (health.bossHealthBarUI != null)
        {
            Destroy(health.bossHealthBarUI.gameObject);
            health.bossHealthBarUI = null;
        }

        // Restaurar cámara
        CamaraScript camara = Camera.main.GetComponent<CamaraScript>();
        if (camara != null) camara.enModoBoss = false;

        // Resetear todo el estado compartido
        data.isDead = false;
        data.isAttacking = false;
        data.isTeleporting = false;
        data.isInvulnerable = false;
        data.arenaSealed = false;
        data.healthBarActivated = false;
        data.currentPhase = BossData.BossPhase.Phase1;

        // Resetear cada subsistema
        health.ResetHealth();
        movement.ResetMovement();
        attacks.ResetAttacks();
        teleport.ResetTeleport();

        // Restaurar posición y escala originales
        transform.position = data.initialPosition;
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x),
            transform.localScale.y,
            1f
        );

        // Restaurar componentes de Unity
        if (spriteRenderer) { spriteRenderer.enabled = true; spriteRenderer.color = Color.white; }
        if (bossCollider) bossCollider.enabled = true;
        if (rb) { rb.gravityScale = data.defaultGravity; rb.linearVelocity = Vector2.zero; }

        // Cooldown para que el boss no reaccione instantáneamente al respawn
        _detectionCooldown = 2f;

        SetDoorsState(false);
        gameObject.SetActive(true);

        Debug.Log("[BossController] Boss reseteado correctamente.");
    }

    // Alias por retrocompatibilidad (AbilityAbsorptionManager u otros
    // scripts que puedan estar llamando a ResetState todavía)
    public void ResetState() => ResetOnPlayerDeath();

    // =========================================================
    // CONTACTO FÍSICO
    // =========================================================
    void OnCollisionStay2D(Collision2D collision)
    {
        if (data.isDead || data.isTeleporting) return;
        health.OnBodyContact(collision);
    }

    // =========================================================
    // GIZMOS
    // =========================================================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.yellow;
        if (movement != null)
        {
            Gizmos.DrawWireSphere(transform.position, movement.optimalDistance);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, movement.retreatDistance);
        }
    }
}