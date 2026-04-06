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

// Sigue implementando las interfaces para el sistema de checkpoints
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
    // Todos viven en el mismo GameObject. Se obtienen en Awake.
    // ─────────────────────────────────────────────────────────
    [HideInInspector] public BossData data;       // estado compartido
    [HideInInspector] public BossHealth health;     // vida y daño
    [HideInInspector] public BossMovementAI movement;   // movimiento inteligente
    [HideInInspector] public BossAttackSystem attacks;    // todos los ataques
    [HideInInspector] public BossTeleport teleport;   // teletransporte defensivo

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D bossCollider;
    private float _detectionCooldown = 0f;
    // =========================================================
    // AWAKE — Obtener todos los componentes del mismo GameObject
    // =========================================================
    void Awake()
    {
        // Componentes de Unity
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bossCollider = GetComponent<Collider2D>();

        // Componentes del boss (deben estar en el mismo GameObject)
        data = GetComponent<BossData>();
        health = GetComponent<BossHealth>();
        movement = GetComponent<BossMovementAI>();
        attacks = GetComponent<BossAttackSystem>();
        teleport = GetComponent<BossTeleport>();

        // Guardar datos de posición y física en BossData
        data.initialPosition = transform.position;
        ConfigurePhysics();
    }

    // =========================================================
    // START — Inicializar referencias y calcular arena
    // =========================================================
    void Start()
    {
        // Buscar jugador y cachear sus referencias en BossData
        // para que todos los componentes puedan acceder a él
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            data.player = playerObj.transform;
            data.playerMainChar = playerObj.GetComponent<PlayerCore>();
            data.playerRb = playerObj.GetComponent<Rigidbody2D>();
        }

        CalculateArenaBounds();

        // Inicializar cada componente con la referencia a BossData
        health.Initialize(data, rb, spriteRenderer, bossCollider, this);
        movement.Initialize(data, rb, spriteRenderer, bossCollider);
        attacks.Initialize(data, rb, spriteRenderer);
        teleport.Initialize(data, rb, spriteRenderer, bossCollider);

        if (AbilityAbsorptionManager.Instance != null)
            AbilityAbsorptionManager.Instance.RegisterResettable(this);

        SetDoorsState(false);
    }

    // =========================================================
    // UPDATE — Bucle principal: decisiones cada frame
    // =========================================================
    void Update()
    {
        if (_detectionCooldown > 0f)
        {
            _detectionCooldown -= Time.deltaTime;
            return; // no hacer nada hasta que pase el cooldown
        }
        if (data.isDead || data.player == null) return;

        // Asegurar visibilidad si algo salió mal
        if (!data.isTeleporting && !data.isAttacking)
            movement.EnsureVisibility();

        // Mantener al boss dentro del escenario
        movement.ClampToArena();

        float dist = Vector2.Distance(transform.position, data.player.position);

        // Activar barra de vida al entrar en rango
        if (!data.healthBarActivated && dist <= detectionRange)
            ActivateBossHealthBar();

        // Cerrar puertas al acercarse
        if (!data.arenaSealed && dist <= doorCloseDistance)
            SealArena();

        if (dist <= detectionRange)
        {
            // Delegar combate al AttackSystem
            attacks.HandleCombat(dist);

            // Voltear sprite hacia el jugador
            if (!data.isAttacking && !data.isTeleporting)
                movement.FlipTowardsPlayer();
        }

        // Recalcular fase según vida restante
        UpdatePhase();
    }

    // =========================================================
    // FIXEDUPDATE — Física: aplicar movimiento suavizado
    // =========================================================
    void FixedUpdate()
    {
        if (data.isDead || data.player == null) return;
        if (data.isAttacking || data.isTeleporting) return;
        movement.ApplyMovement();
    }

    // =========================================================
    // FASE — Recalcular según porcentaje de vida
    // =========================================================
    void UpdatePhase()
    {
        // (float) convierte int a float para que no pierda decimales
        float hp = (float)health.currentHealth / health.maxHealth;

        if (hp > 0.60f) data.currentPhase = BossData.BossPhase.Phase1;
        else if (hp > 0.30f) data.currentPhase = BossData.BossPhase.Phase2;
        else data.currentPhase = BossData.BossPhase.Phase3;
    }

    // =========================================================
    // FÍSCA — Configurar Rigidbody2D
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
        // Interpolate: suaviza visualmente el movimiento entre frames de física
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    // =========================================================
    // ARENA — Límites y puertas
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
    // UI — Barra de vida del boss
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
    // PlayerCombat.cs llama bossController.TakeDamage()
    // Este método reenvía la llamada a BossHealth.
    // Así no hay que modificar ningún script externo.
    // =========================================================

    // TakeDamage: llamado por PlayerCombat al golpear al boss
    public void TakeDamage(int dmg, int dir)
    {
        health.TakeDamage(dmg, dir);
        // Registrar el golpe en el sistema de teletransporte
        teleport.RegisterHit(data.isAttacking);
    }

    // bossHealthBarUI: EnemyManager accede a esta propiedad directamente.
    // La exponemos aquí como propiedad pública que apunta a BossHealth.
    public BossHealthBar bossHealthBarUI
    {
        get => health != null ? health.bossHealthBarUI : null;
        set { if (health != null) health.bossHealthBarUI = value; }
    }

    // =========================================================
    // IAbsorbable — El boss puede ser absorbido al morir
    // =========================================================
    public bool CanBeAbsorbed() => data.isDead;
    public void OnAbsorbed() { Destroy(gameObject); }
    public bool IsBoss => true;

    // =========================================================
    // IResettable — Reiniciar todo al morir el jugador
    // =========================================================
    public void ResetState()
    {
        StopAllCoroutines();

        // Destruir barra de vida de la UI
        if (health.bossHealthBarUI != null)
        {
            Destroy(health.bossHealthBarUI.gameObject);
            health.bossHealthBarUI = null;
        }

        // Restaurar cámara
        CamaraScript camara = Camera.main.GetComponent<CamaraScript>();
        if (camara != null) camara.enModoBoss = false;

        // Resetear estado compartido
        data.isDead = false;
        data.isAttacking = false;
        data.isTeleporting = false;
        data.isInvulnerable = false;
        data.arenaSealed = false;
        data.healthBarActivated = false;
        data.currentPhase = BossData.BossPhase.Phase1;

        // Resetear cada componente
        health.ResetHealth();
        movement.ResetMovement();
        attacks.ResetAttacks();
        teleport.ResetTeleport();

        // Restaurar posición y escala
        transform.position = data.initialPosition;
        transform.localScale = new Vector3(
            Mathf.Abs(transform.localScale.x),
            transform.localScale.y, 1
        );

        // Restaurar componentes de Unity
        if (spriteRenderer) { spriteRenderer.enabled = true; spriteRenderer.color = Color.white; }
        if (bossCollider) bossCollider.enabled = true;
        if (rb) { rb.gravityScale = data.defaultGravity; rb.linearVelocity = Vector2.zero; }
        data.healthBarActivated = false;
        _detectionCooldown = 2f; // ← NUEVO
        SetDoorsState(false);
        gameObject.SetActive(true);
    }

    // =========================================================
    // CONTACTO FÍSICO — Daño al tocar al jugador
    // =========================================================
    void OnCollisionStay2D(Collision2D collision)
    {
        if (data.isDead || data.isTeleporting) return;
        health.OnBodyContact(collision);
    }

    // =========================================================
    // GIZMOS — Solo visibles en el Editor de Unity
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