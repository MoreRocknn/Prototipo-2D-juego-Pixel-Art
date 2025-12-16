using UnityEngine;
using System.Collections;

public class DeathZone : MonoBehaviour
{
    public float respawnDelay = 1f;
    public bool destroyplayer = false;

    [Header("Efectos (opcional)")]
    public GameObject deathEffect;
    public AudioClip deathSound;

    [Header("Debug")]
    public bool showDebugMessages = true;

    private AudioSource audioSource;
    private bool isRespawning = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isRespawning)
        {
            if (showDebugMessages) Debug.Log("¡Jugador cayó en DeadZone! Iniciando respawn...");
            StartCoroutine(RespawnPlayer(other.gameObject));
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !isRespawning)
        {
            if (showDebugMessages) Debug.Log("¡Jugador cayó en DeadZone (via Collision)! Iniciando respawn...");
            StartCoroutine(RespawnPlayer(collision.gameObject));
        }
    }

    IEnumerator RespawnPlayer(GameObject player)
    {
        isRespawning = true;

        // Notificar al sistema de absorción
        if (AbilityAbsorptionManager.Instance != null)
        {
            AbilityAbsorptionManager.Instance.OnPlayerDeath();
        }

        MainChar playerController = player.GetComponent<MainChar>();
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        SpriteRenderer spriteRenderer = player.GetComponent<SpriteRenderer>();
        Collider2D playerCollider = player.GetComponent<Collider2D>();

        // Desactivar controles y físicas
        if (playerController) playerController.enabled = false;
        if (playerCollider) playerCollider.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0;
        }

        // Efectos de muerte
        if (deathEffect) Instantiate(deathEffect, player.transform.position, Quaternion.identity);
        if (audioSource && deathSound) audioSource.PlayOneShot(deathSound);
        if (spriteRenderer) spriteRenderer.enabled = false;

        // Esperar tiempo de respawn
        yield return new WaitForSeconds(respawnDelay);

        // 1. Mover al jugador a la posición de guardado
        Vector2 respawnPos = Vector2.zero;
        if (GameManager.Instance != null)
        {
            respawnPos = GameManager.Instance.GetRespawnPosition();
        }

        player.transform.position = respawnPos;

        // ====================================================================
        // --- CORRECCIÓN NUEVA: RESETEAR EL MUNDO (BOSSES Y ENEMIGOS) ---
        // Esto busca a todos los enemigos y Bosses y les dice que vuelvan a su estado inicial
        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.RespawnAllEnemies();
        }
        else
        {
            Debug.LogWarning("EnemyManager no encontrado en la escena. Los enemigos no se resetearán.");
        }
        // ====================================================================

        // 2. Restaurar Vida y Viales
        if (playerController != null)
        {
            playerController.currentHealth = playerController.maxHealth; // Vida a tope
        }

        HealingSystem healing = player.GetComponent<HealingSystem>();
        if (healing != null)
        {
            healing.RefillVials(); // Viales a tope
        }

        // 3. Reactivar físicas y controles
        if (rb != null)
        {
            rb.gravityScale = 3.5f;
            rb.linearVelocity = Vector2.zero;
        }

        if (playerCollider) playerCollider.enabled = true;
        if (spriteRenderer) spriteRenderer.enabled = true;
        if (playerController) playerController.enabled = true;

        isRespawning = false;
    }
}