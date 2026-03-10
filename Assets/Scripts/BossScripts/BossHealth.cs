// ============================================================
// BossHealth.cs
// RESPONSABILIDAD: Todo lo relacionado con la vida del boss:
//                  recibir daño, parpadeo, muerte y UI de vida.
//
// Para usarlo: añádelo al mismo GameObject que BossController.
// ============================================================

using UnityEngine;
using System.Collections;

public class BossHealth : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // INSPECTOR
    // ─────────────────────────────────────────────────────────
    [Header("=== SALUD ===")]
    public int maxHealth = 50;

    [Header("=== DAÑO POR CONTACTO ===")]
    // Daño que hace el boss simplemente tocando al jugador
    public int   bodyContactDamage  = 1;
    public float bodyDamageCooldown = 1.0f; // segundos entre daños por contacto

    // ─────────────────────────────────────────────────────────
    // ESTADO INTERNO
    // ─────────────────────────────────────────────────────────
    [HideInInspector] public int          currentHealth;
    [HideInInspector] public BossHealthBar bossHealthBarUI;

    private float lastBodyDamageTime;

    // Referencias a otros componentes (asignadas en Initialize)
    private BossData         data;
    private Rigidbody2D      rb;
    private SpriteRenderer   spriteRenderer;
    private Collider2D       bossCollider;
    private BossController   controller; // para llamar a UnsealArena y Die

    // =========================================================
    // INITIALIZE — Llamado por BossController en Start()
    // Recibe las referencias necesarias en vez de buscarlas
    // con GetComponent() (más eficiente).
    // =========================================================
    public void Initialize(BossData data, Rigidbody2D rb,
                           SpriteRenderer sr, Collider2D col,
                           BossController controller)
    {
        this.data           = data;
        this.rb             = rb;
        this.spriteRenderer = sr;
        this.bossCollider   = col;
        this.controller     = controller;
        currentHealth       = maxHealth;
    }

    // =========================================================
    // TAKE DAMAGE — Llamado por el sistema de combate del jugador
    // =========================================================
    public void TakeDamage(int dmg, int dir)
    {
        // No recibir daño en estos estados
        if (data.isDead || data.isInvulnerable || data.isTeleporting) return;

        currentHealth -= dmg;

        // Actualizar la barra de vida en la UI
        if (bossHealthBarUI != null)
            bossHealthBarUI.UpdateHealth(currentHealth);

        // Parpadear en rojo para dar feedback visual
        StartCoroutine(FlashDamage());

        // Comprobar si debe morir
        if (currentHealth <= 0) Die();
    }

    // =========================================================
    // DAÑO POR CONTACTO FÍSICO
    // Llamado desde BossController.OnCollisionStay2D()
    // =========================================================
    public void OnBodyContact(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        // Solo dañar si pasó suficiente tiempo desde el último daño
        if (Time.time > lastBodyDamageTime + bodyDamageCooldown)
        {
            // ?. = solo llama si playerMainChar no es null
            data.playerMainChar?.TakeDamage(bodyContactDamage);
            lastBodyDamageTime = Time.time;
        }
    }

    // =========================================================
    // FLASH DAMAGE — Parpadeo rojo al recibir daño
    // Es una Coroutine: se pausa 0.1 segundos con yield return
    // =========================================================
    IEnumerator FlashDamage()
    {
        if (spriteRenderer) spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        if (!data.isDead && !data.isAttacking && spriteRenderer)
            spriteRenderer.color = Color.white;
    }

    // =========================================================
    // DIE — Muerte del boss
    // =========================================================
    void Die()
    {
        data.isDead = true;

        // Abrir puertas
        controller.UnsealArena();

        // Ocultar barra de vida
        if (bossHealthBarUI != null) bossHealthBarUI.Hide();

        // Desactivar modo boss en la cámara
        CamaraScript camara = Camera.main.GetComponent<CamaraScript>();
        if (camara != null) camara.enModoBoss = false;

        // Detener todas las coroutines activas en TODOS los componentes
        // del GameObject (ataques en curso, teletransporte, etc.)
        foreach (MonoBehaviour comp in GetComponents<MonoBehaviour>())
            comp.StopAllCoroutines();

        gameObject.SetActive(false); // Ocultar (no destruir: permite reset)
    }

    // =========================================================
    // RESET — Restaurar al estado inicial
    // =========================================================
    public void ResetHealth()
    {
        currentHealth      = maxHealth;
        lastBodyDamageTime = 0f;
    }
}
