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

        if (AbilityAbsorptionManager.Instance != null)
            AbilityAbsorptionManager.Instance.OnPlayerDeath();

        PlayerCore playerCore = player.GetComponent<PlayerCore>();
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
        Collider2D col = player.GetComponent<Collider2D>();

        if (playerCore) playerCore.enabled = false;
        if (col) col.enabled = false;
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.gravityScale = 0; }

        if (deathEffect) Instantiate(deathEffect, player.transform.position, Quaternion.identity);
        if (audioSource && deathSound) audioSource.PlayOneShot(deathSound);
        if (sr) sr.enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        // Mover al checkpoint
        if (GameManager.Instance != null)
            player.transform.position = GameManager.Instance.GetRespawnPosition();

        // Resetear enemigos
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.RespawnAllEnemies();
        else
            Debug.LogWarning("EnemyManager no encontrado. Los enemigos no se resetearán.");

        // FIX: usar PlayerHealth para restaurar vida (actualiza la barra automáticamente)
        if (playerHealth != null)
            playerHealth.Heal(playerHealth.maxHealth);

        // Restaurar viales
        player.GetComponent<HealingSystem>()?.RefillVials();

        // Reactivar
        if (rb != null) { rb.gravityScale = 3.5f; rb.linearVelocity = Vector2.zero; }
        if (col) col.enabled = true;
        if (sr) sr.enabled = true;
        if (playerCore) playerCore.enabled = true;

        isRespawning = false;
    }
}