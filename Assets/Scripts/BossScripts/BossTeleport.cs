// ============================================================
// BossTeleport.cs
// RESPONSABILIDAD: Teletransporte defensivo del boss.
//                  Se activa cuando recibe demasiados golpes
//                  seguidos, para escapar del jugador.
//
// Para usarlo: añádelo al mismo GameObject que BossController.
// ============================================================

using UnityEngine;
using System.Collections;

public class BossTeleport : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // INSPECTOR
    // ─────────────────────────────────────────────────────────
    [Header("=== TELETRANSPORTE DEFENSIVO ===")]
    // Cuántos golpes recibidos antes de teletransportarse
    public int   hitsToTriggerTeleport = 4;

    // Segundos de "carga" antes de reaparecer (tiempo para el jugador)
    public float teleportDelay         = 0.5f;

    // ─────────────────────────────────────────────────────────
    // ESTADO INTERNO
    // ─────────────────────────────────────────────────────────
    private int   currentHitCounter   = 0;
    private float lastTeleportTime    = -999f; // -999 = nunca ocurrió
    private float minTeleportInterval = 3f;    // segundos mínimos entre teleports

    // Referencias
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
    // REGISTER HIT — Llamado por BossHealth cada vez que el
    // boss recibe un golpe. Decide si debe teletransportarse.
    // =========================================================
    public void RegisterHit(bool isCurrentlyAttacking)
    {
        currentHitCounter++;

        // Condiciones para teletransportarse:
        // 1. No está en mitad de un ataque
        // 2. Han pasado al menos 3 segundos desde el último teleport
        // 3. Ha recibido suficientes golpes seguidos
        bool canTeleport = !isCurrentlyAttacking &&
                           (Time.time - lastTeleportTime) > minTeleportInterval &&
                           currentHitCounter >= hitsToTriggerTeleport;

        if (canTeleport)
            StartCoroutine(DoTeleport());
    }

    // =========================================================
    // DO TELEPORT — Coroutine principal del teletransporte
    //
    // Una Coroutine puede PAUSARSE con "yield return" y
    // reanudar más tarde. Aquí usamos eso para:
    //   1. Hacerse invisible (intangible)
    //   2. Esperar teleportDelay segundos
    //   3. Reaparecer en otro lugar
    // =========================================================
    IEnumerator DoTeleport()
    {
        data.isTeleporting = true;
        rb.linearVelocity  = Vector2.zero;

        // Hacerse intangible e invisible
        if (bossCollider)   bossCollider.enabled   = false;
        if (spriteRenderer) spriteRenderer.enabled = false;
        rb.gravityScale = 0; // suspender en el aire

        // Pausar aquí → el jugador ve que "desaparece"
        yield return new WaitForSeconds(teleportDelay);

        // Elegir posición aleatoria dentro de la arena
        float randomX = Random.Range(data.minArenaX + 2f, data.maxArenaX - 2f);

        // Si cae demasiado cerca del jugador → ir al extremo opuesto
        if (data.player != null && Mathf.Abs(randomX - data.player.position.x) < 5f)
        {
            float centro = (data.minArenaX + data.maxArenaX) / 2f;
            randomX = data.player.position.x > centro
                ? data.minArenaX + 3f   // jugador en la derecha → aparecer en la izquierda
                : data.maxArenaX - 3f;  // jugador en la izquierda → aparecer en la derecha
        }

        // ¡TELETRANSPORTE! Mover instantáneamente
        transform.position = new Vector3(randomX, data.initialPosition.y, 0);

        // Restaurar visibilidad y física
        if (spriteRenderer) spriteRenderer.enabled = true;
        if (bossCollider)   bossCollider.enabled   = true;
        rb.gravityScale = data.defaultGravity;

        // Limpiar estado
        data.isTeleporting  = false;
        currentHitCounter   = 0;          // resetear contador de golpes
        lastTeleportTime    = Time.time;  // guardar cuándo ocurrió
    }

    // =========================================================
    // RESET
    // =========================================================
    public void ResetTeleport()
    {
        currentHitCounter = 0;
        lastTeleportTime  = -999f;
    }

    // =========================================================
    // GETTERS — Para que otros componentes puedan consultar
    // =========================================================
    public float GetLastTeleportTime()    => lastTeleportTime;
    public float GetMinTeleportInterval() => minTeleportInterval;
    public int   GetHitCounter()          => currentHitCounter;
}
