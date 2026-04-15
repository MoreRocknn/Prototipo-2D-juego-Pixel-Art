// ============================================================
// BossTeleport.cs
// FIX: Al finalizar el teleport llama a
//      movement.NotifyTeleportCompleted() para que el boss
//      NO entre en modo retreat nada mas aparecer.
// ============================================================

using UnityEngine;
using System.Collections;

public class BossTeleport : MonoBehaviour
{
    [Header("=== TELETRANSPORTE DEFENSIVO ===")]
    public int hitsToTriggerTeleport = 4;
    public float teleportDelay = 0.5f;

    private int currentHitCounter = 0;
    private float lastTeleportTime = -999f;
    private float minTeleportInterval = 3f;

    private BossData data;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D bossCollider;
    private BossMovementAI movement; // FIX: referencia al AI de movimiento

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
        this.movement = GetComponent<BossMovementAI>(); // FIX
    }

    // =========================================================
    // REGISTER HIT
    // =========================================================
    public void RegisterHit(bool isCurrentlyAttacking)
    {
        currentHitCounter++;

        bool canTeleport = !isCurrentlyAttacking &&
                           (Time.time - lastTeleportTime) > minTeleportInterval &&
                           currentHitCounter >= hitsToTriggerTeleport;

        if (canTeleport)
            StartCoroutine(DoTeleport());
    }

    // =========================================================
    // DO TELEPORT
    // =========================================================
    IEnumerator DoTeleport()
    {
        data.isTeleporting = true;
        rb.linearVelocity = Vector2.zero;

        if (bossCollider) bossCollider.enabled = false;
        if (spriteRenderer) spriteRenderer.enabled = false;
        rb.gravityScale = 0;

        yield return new WaitForSeconds(teleportDelay);

        // Elegir posicion aleatoria dentro de la arena
        float randomX = Random.Range(data.minArenaX + 2f, data.maxArenaX - 2f);

        // Si cae demasiado cerca del jugador, ir al extremo opuesto
        if (data.player != null && Mathf.Abs(randomX - data.player.position.x) < 5f)
        {
            float centro = (data.minArenaX + data.maxArenaX) / 2f;
            randomX = data.player.position.x > centro
                ? data.minArenaX + 3f
                : data.maxArenaX - 3f;
        }

        transform.position = new Vector3(randomX, data.initialPosition.y, 0);

        if (spriteRenderer) spriteRenderer.enabled = true;
        if (bossCollider) bossCollider.enabled = true;
        rb.gravityScale = data.defaultGravity;

        // FIX: avisar al MovementAI para que active el periodo de gracia
        // y el boss vaya hacia el jugador en vez de huir
        if (movement != null)
            movement.NotifyTeleportCompleted();

        data.isTeleporting = false;
        currentHitCounter = 0;
        lastTeleportTime = Time.time;
    }

    // =========================================================
    // RESET
    // =========================================================
    public void ResetTeleport()
    {
        currentHitCounter = 0;
        lastTeleportTime = -999f;
    }

    // =========================================================
    // GETTERS
    // =========================================================
    public float GetLastTeleportTime() => lastTeleportTime;
    public float GetMinTeleportInterval() => minTeleportInterval;
    public int GetHitCounter() => currentHitCounter;
}