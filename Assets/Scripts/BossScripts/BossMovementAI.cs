// ============================================================
// BossMovementAI.cs
// RESPONSABILIDAD: Todo el movimiento del boss:
//                  - 4 comportamientos según distancia al jugador
//                  - Suavizado de movimiento (optimización Unity 6)
//                  - Voltear sprite
//                  - Mantener al boss dentro de la arena
//
// Para usarlo: añádelo al mismo GameObject que BossController.
// ============================================================

using UnityEngine;

public class BossMovementAI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // INSPECTOR
    // ─────────────────────────────────────────────────────────
    [Header("=== VELOCIDADES ===")]
    public float moveSpeed       = 8f;  // velocidad normal hacia el jugador
    public float repositionSpeed = 12f; // velocidad al esquivar / circular

    [Header("=== DISTANCIAS DE COMPORTAMIENTO ===")]
    public float optimalDistance = 7f;  // distancia ideal al jugador
    public float retreatDistance = 3f;  // si el jugador está MÁS cerca → retroceder

    [Header("=== SUAVIZADO DE MOVIMIENTO ===")]
    [Range(1f, 20f)]
    [Tooltip("1 = movimiento brusco | 20 = movimiento muy suave")]
    public float movementSmoothing = 10f;

    // ─────────────────────────────────────────────────────────
    // ESTADO INTERNO
    // ─────────────────────────────────────────────────────────

    // Velocidad objetivo: a dónde QUIERE ir el boss
    // Solo se calcula aquí, se aplica en BossController.FixedUpdate
    private Vector2 targetVelocity  = Vector2.zero;

    // Velocidad actual: interpolada suavemente hacia targetVelocity
    private Vector2 currentVelocity = Vector2.zero;

    // Para el movimiento circular: cambia dirección cada 1.5 segundos
    private float repositionTimer = 0f;
    private float circleDirection = 1f; // +1 = derecha, -1 = izquierda

    // Referencias (asignadas en Initialize)
    private BossData       data;
    private Rigidbody2D    rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D     bossCollider;

    // =========================================================
    // INITIALIZE — Llamado por BossController en Start()
    // =========================================================
    public void Initialize(BossData data, Rigidbody2D rb,
                           SpriteRenderer sr, Collider2D col)
    {
        this.data           = data;
        this.rb             = rb;
        this.spriteRenderer = sr;
        this.bossCollider   = col;
    }

    // =========================================================
    // APPLY MOVEMENT — Llamado desde BossController.FixedUpdate()
    //
    // REGLA DE ORO: rb.linearVelocity SIEMPRE en FixedUpdate,
    // nunca en Update(). De lo contrario el movimiento varía
    // según el FPS del jugador y se vuelve inconsistente.
    // =========================================================
    public void ApplyMovement()
    {
        if (targetVelocity == Vector2.zero) return;

        // Vector2.Lerp(a, b, t): interpola entre 'a' y 'b'
        // Con t pequeño (~0.2), el cambio es gradual → movimiento suave
        // Con t grande (~0.9), el cambio es brusco → movimiento instantáneo
        currentVelocity = Vector2.Lerp(
            currentVelocity,
            targetVelocity,
            movementSmoothing * Time.fixedDeltaTime
        );

        // Solo modificamos el eje X.
        // El eje Y lo gestiona la gravedad de Unity automáticamente.
        rb.linearVelocity = new Vector2(currentVelocity.x, rb.linearVelocity.y);
    }

    // =========================================================
    // HANDLE MOVEMENT — 4 comportamientos según la distancia
    // Llamado desde BossAttackSystem cuando no está atacando.
    // Solo CALCULA targetVelocity; ApplyMovement() la ejecuta.
    // =========================================================
    public void HandleMovement(float dist)
    {
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // Dirección horizontal hacia el jugador: +1 o -1
        float dirX = Mathf.Sign(data.player.position.x - transform.position.x);

        if (dist < retreatDistance)
        {
            // ── RETROCEDER ──
            // El jugador está demasiado cerca. Huir en dirección opuesta.
            targetVelocity = new Vector2(-dirX * repositionSpeed * 1.2f, 0);
        }
        else if (dist > optimalDistance + 3f)
        {
            // ── ACERCARSE ──
            // El jugador está lejos. En fase 3, posibilidad de dash.
            bool esFase3  = data.currentPhase == BossData.BossPhase.Phase3;
            float dashMod = (esFase3 && Random.value < 0.3f) ? 1.8f : 1f;
            targetVelocity = new Vector2(dirX * moveSpeed * dashMod, 0);
        }
        else if (dist > GetDetectionRange())
        {
            // ── QUIETO ──
            // El jugador salió del rango de detección.
            targetVelocity = Vector2.zero;
        }
        else
        {
            // ── CIRCULAR ──
            // Distancia ideal: moverse alrededor del jugador.
            // Cada 1.5 segundos cambia de dirección para ser impredecible.
            repositionTimer += Time.deltaTime;
            if (repositionTimer > 1.5f)
            {
                circleDirection *= -1f; // invertir dirección
                repositionTimer  = 0f;
            }
            float moveMod  = (circleDirection > 0) ? 0.5f : -0.3f;
            targetVelocity = new Vector2(dirX * repositionSpeed * moveMod, 0);
        }
    }

    // =========================================================
    // STOP MOVEMENT — Detener el boss (durante ataques)
    // =========================================================
    public void StopMovement()
    {
        targetVelocity  = Vector2.zero;
        currentVelocity = Vector2.zero;
    }

    // =========================================================
    // FLIP — Voltear el sprite para mirar hacia el jugador
    // =========================================================
    public void FlipTowardsPlayer()
    {
        if (data.player == null) return;
        float absX = Mathf.Abs(transform.localScale.x);
        transform.localScale = data.player.position.x < transform.position.x
            ? new Vector3(-absX, transform.localScale.y, 1) // mirar izquierda
            : new Vector3( absX, transform.localScale.y, 1); // mirar derecha
    }

    // =========================================================
    // CLAMP TO ARENA — Mantener al boss dentro del escenario
    // =========================================================
    public void ClampToArena()
    {
        float clampedX = Mathf.Clamp(transform.position.x, data.minArenaX, data.maxArenaX);
        if (Mathf.Abs(transform.position.x - clampedX) > 0.1f)
        {
            transform.position = new Vector3(clampedX, transform.position.y, transform.position.z);
            rb.linearVelocity  = new Vector2(0, rb.linearVelocity.y);
        }
    }

    // =========================================================
    // ENSURE VISIBILITY — Restaurar si quedó invisible por bug
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
        targetVelocity  = Vector2.zero;
        currentVelocity = Vector2.zero;
        repositionTimer = 0f;
        circleDirection = 1f;
    }

    // Helper: obtener el rango de detección desde BossController
    float GetDetectionRange()
    {
        BossController bc = GetComponent<BossController>();
        return bc != null ? bc.detectionRange : 30f;
    }
}
