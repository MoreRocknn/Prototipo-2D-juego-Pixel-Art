// ============================================================
// BossMovementAI.cs
// FIXES:
//   - Ya no huye del jugador tras teletransporte (postTeleportGrace)
//   - ClampToArena cancela targetVelocity para no quedarse pegado
//   - Hooks de animación listos (Animator opcional en Inspector)
// ============================================================

using UnityEngine;

public class BossMovementAI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // INSPECTOR
    // ─────────────────────────────────────────────────────────
    [Header("=== VELOCIDADES ===")]
    public float moveSpeed = 8f;
    public float repositionSpeed = 12f;

    [Header("=== DISTANCIAS DE COMPORTAMIENTO ===")]
    public float optimalDistance = 7f;
    public float retreatDistance = 3f;

    [Header("=== SUAVIZADO DE MOVIMIENTO ===")]
    [Range(1f, 20f)]
    [Tooltip("1 = movimiento brusco | 20 = movimiento muy suave")]
    public float movementSmoothing = 10f;

    [Header("=== POST-TELEPORT ===")]
    [Tooltip("Segundos tras teleport en los que NO retrocede del jugador")]
    public float postTeleportGraceDuration = 1.2f;

    // ─────────────────────────────────────────────────────────
    // ANIMACION — Asigna el Animator en el Inspector cuando
    // tengas animaciones. Los parametros ya estan definidos.
    // Parametros esperados en el Animator:
    //   bool  isWalking
    //   bool  isRetreating
    //   float speedX
    // ─────────────────────────────────────────────────────────
    [Header("=== ANIMACION (opcional) ===")]
    [Tooltip("Asigna el Animator del boss cuando tengas animaciones")]
    public Animator animator;

    private static readonly int AnimIsWalking = Animator.StringToHash("isWalking");
    private static readonly int AnimIsRetreating = Animator.StringToHash("isRetreating");
    private static readonly int AnimSpeedX = Animator.StringToHash("speedX");

    // ─────────────────────────────────────────────────────────
    // ESTADO INTERNO
    // ─────────────────────────────────────────────────────────
    private Vector2 targetVelocity = Vector2.zero;
    private Vector2 currentVelocity = Vector2.zero;

    private float repositionTimer = 0f;
    private float circleDirection = 1f;

    // FIX: bloquea el retreat durante N segundos tras teleport
    private float postTeleportGraceTimer = 0f;

    public enum MoveState { Idle, Walking, Retreating, Circling }
    private MoveState currentMoveState = MoveState.Idle;

    // Referencias
    private BossData data;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D bossCollider;

    // =========================================================
    // INITIALIZE
    // =========================================================
    public void Initialize(BossData data, Rigidbody2D rb,
                           SpriteRenderer sr, Collider2D col)
    {
        this.data = data;
        this.rb = rb;
        this.spriteRenderer = sr;
        this.bossCollider = col;
    }

    // =========================================================
    // APPLY MOVEMENT — llamado desde BossController.FixedUpdate
    // =========================================================
    public void ApplyMovement()
    {
        // Tick del timer de gracia en FixedUpdate para consistencia
        if (postTeleportGraceTimer > 0f)
            postTeleportGraceTimer -= Time.fixedDeltaTime;

        if (targetVelocity == Vector2.zero) return;

        currentVelocity = Vector2.Lerp(
            currentVelocity,
            targetVelocity,
            movementSmoothing * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector2(currentVelocity.x, rb.linearVelocity.y);
    }

    // =========================================================
    // HANDLE MOVEMENT — 4 comportamientos segun distancia
    // =========================================================
    public void HandleMovement(float dist)
    {
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        float dirX = Mathf.Sign(data.player.position.x - transform.position.x);

        // FIX: durante el periodo de gracia post-teleport ignoramos retreat
        bool graceActive = postTeleportGraceTimer > 0f;

        if (dist < retreatDistance && !graceActive)
        {
            // RETROCEDER
            SetMoveState(MoveState.Retreating);
            targetVelocity = new Vector2(-dirX * repositionSpeed * 1.2f, 0);
        }
        else if (dist > optimalDistance + 3f)
        {
            // ACERCARSE
            SetMoveState(MoveState.Walking);
            bool esFase3 = data.currentPhase == BossData.BossPhase.Phase3;
            float dashMod = (esFase3 && Random.value < 0.3f) ? 1.8f : 1f;
            targetVelocity = new Vector2(dirX * moveSpeed * dashMod, 0);
        }
        else if (dist > GetDetectionRange())
        {
            // QUIETO
            SetMoveState(MoveState.Idle);
            targetVelocity = Vector2.zero;
        }
        else
        {
            // CIRCULAR
            SetMoveState(MoveState.Circling);
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

    // =========================================================
    // NOTIFY TELEPORT COMPLETED
    // Llamar desde BossTeleport al finalizar la coroutine.
    // Activa el periodo de gracia y resetea la inercia.
    // =========================================================
    public void NotifyTeleportCompleted()
    {
        postTeleportGraceTimer = postTeleportGraceDuration;
        currentVelocity = Vector2.zero;
        targetVelocity = Vector2.zero;
    }

    // =========================================================
    // STOP MOVEMENT
    // =========================================================
    public void StopMovement()
    {
        targetVelocity = Vector2.zero;
        currentVelocity = Vector2.zero;
        SetMoveState(MoveState.Idle);
    }

    // =========================================================
    // FLIP
    // =========================================================
    public void FlipTowardsPlayer()
    {
        if (data.player == null) return;
        float absX = Mathf.Abs(transform.localScale.x);
        transform.localScale = data.player.position.x < transform.position.x
            ? new Vector3(-absX, transform.localScale.y, 1)
            : new Vector3(absX, transform.localScale.y, 1);
    }

    // =========================================================
    // CLAMP TO ARENA
    // FIX: cancela la inercia al tocar el borde para evitar
    // que el boss quede "empujado" e inmovil en la esquina.
    // =========================================================
    public void ClampToArena()
    {
        float clampedX = Mathf.Clamp(transform.position.x, data.minArenaX, data.maxArenaX);
        if (Mathf.Abs(transform.position.x - clampedX) > 0.1f)
        {
            transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            // FIX clave: limpiar la velocidad objetivo para que deje de empujar
            targetVelocity = Vector2.zero;
            currentVelocity = Vector2.zero;
        }
    }

    // =========================================================
    // ENSURE VISIBILITY
    // =========================================================
    public void EnsureVisibility()
    {
        if (spriteRenderer != null && !spriteRenderer.enabled)
            spriteRenderer.enabled = true;
        if (bossCollider != null && !bossCollider.enabled)
            bossCollider.enabled = true;
        if (rb.gravityScale == 0 && !data.isInvulnerable)
            rb.gravityScale = data.defaultGravity;
    }

    // =========================================================
    // RESET
    // =========================================================
    public void ResetMovement()
    {
        targetVelocity = Vector2.zero;
        currentVelocity = Vector2.zero;
        repositionTimer = 0f;
        circleDirection = 1f;
        postTeleportGraceTimer = 0f;
        SetMoveState(MoveState.Idle);
    }

    // =========================================================
    // ANIMACION — actualiza parametros del Animator
    // =========================================================
    void SetMoveState(MoveState newState)
    {
        if (newState == currentMoveState) return;
        currentMoveState = newState;

        if (animator == null) return;

        animator.SetBool(AnimIsWalking, newState == MoveState.Walking);
        animator.SetBool(AnimIsRetreating, newState == MoveState.Retreating);
    }

    void LateUpdate()
    {
        if (animator == null) return;
        animator.SetFloat(AnimSpeedX, Mathf.Abs(rb != null ? rb.linearVelocity.x : 0f));
    }

    // Helper
    float GetDetectionRange()
    {
        BossController bc = GetComponent<BossController>();
        return bc != null ? bc.detectionRange : 30f;
    }
}