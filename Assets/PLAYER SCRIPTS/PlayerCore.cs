using UnityEngine;

/// <summary>
/// Orquestador principal del jugador.
/// Solo coordina los módulos y gestiona la inicialización global.
/// No contiene lógica de juego directa.
/// </summary>
[RequireComponent(typeof(PlayerState))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerJump))]
[RequireComponent(typeof(PlayerWall))]
[RequireComponent(typeof(PlayerGravity))]
[RequireComponent(typeof(PlayerCombat))]
[RequireComponent(typeof(PlayerDash))]
[RequireComponent(typeof(PlayerHealth))]
public class PlayerCore : MonoBehaviour
{
    // Referencias a módulos
    private PlayerState state;
    private PlayerMovement movement;
    private PlayerJump jump;
    private PlayerWall wall;
    private PlayerGravity gravity;
    private PlayerCombat combat;
    private PlayerDash dash;
    private PlayerHealth health;

    // Componentes Unity compartidos
    [HideInInspector] public Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        state    = GetComponent<PlayerState>();
        movement = GetComponent<PlayerMovement>();
        jump     = GetComponent<PlayerJump>();
        wall     = GetComponent<PlayerWall>();
        gravity  = GetComponent<PlayerGravity>();
        combat   = GetComponent<PlayerCombat>();
        dash     = GetComponent<PlayerDash>();
        health   = GetComponent<PlayerHealth>();
    }

    void Start()
    {
        // Configurar Rigidbody2D
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Inicializar sistemas adicionales
        var abilityHolder = GetComponent<AbilityHolder>();
        if (abilityHolder == null)
            gameObject.AddComponent<AbilityHolder>();

        var healingSystem = GetComponent<HealingSystem>();
        if (healingSystem == null)
            gameObject.AddComponent<HealingSystem>();

        // Checkpoint
        if (GameManager.Instance != null && GameManager.Instance.hasCheckpoint)
            transform.position = GameManager.Instance.GetRespawnPosition();
    }

    void Update()
    {
        if (state.isDashing) return;
        if (state.isInputLocked) return;

        movement.HandleInput();
        wall.UpdateWallChecks();
        gravity.HandleGravity();
        wall.HandleWallMechanics();
        movement.HandleFlip();
        combat.HandleBounceReset();
        combat.HandleAbilityInput();
        combat.HandleComboReset();

        // Debug: info de viales
        if (Input.GetKeyDown(KeyCode.V))
        {
            var healingSystem = GetComponent<HealingSystem>();
            if (healingSystem != null)
                Debug.Log(healingSystem.GetVialsInfo());
        }
    }

    void FixedUpdate()
    {
        if (state.isDashing) return;

        if (state.isInputLocked)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        if (state.isWallGrabbing)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        movement.HandleMovement();
        jump.HandleJump();
        jump.LimitFallSpeed();
    }

    // Proxy público para que otros sistemas puedan aplicar daño
    public void TakeDamage(int damage) => health.TakeDamage(damage);

    // Proxy para el sistema de dash (IDashExecutor)
    public void PerformDash(float force, float duration) => dash.PerformDash(force, duration);

    // Bloqueo de input (para cinemáticas, etc.)
    public void SetInputLock(bool locked)
    {
        state.isInputLocked = locked;
        if (locked) state.moveInput = 0;
    }

    public void StopPhysics()
    {
        if (rb != null) rb.linearVelocity = Vector2.zero;
    }
}
