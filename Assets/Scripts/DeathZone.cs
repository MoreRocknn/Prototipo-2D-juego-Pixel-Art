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
            if (showDebugMessages) Debug.Log("¡Jugador cayó en DeadZone!");
            StartCoroutine(RespawnPlayer(other.gameObject));
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && !isRespawning)
        {
            if (showDebugMessages) Debug.Log("¡Jugador cayó en DeadZone (Collision)!");
            StartCoroutine(RespawnPlayer(collision.gameObject));
        }
    }

    IEnumerator RespawnPlayer(GameObject player)
    {
        isRespawning = true;

        AbilityAbsorptionManager.Instance?.OnPlayerDeath();

        PlayerCore playerCore = player.GetComponent<PlayerCore>();
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        SpriteRenderer sr = player.GetComponent<SpriteRenderer>();
        Collider2D col = player.GetComponent<Collider2D>();

        // Desactivar jugador
        if (playerCore) playerCore.enabled = false;
        if (col) col.enabled = false;
        if (rb != null) { rb.linearVelocity = Vector2.zero; rb.gravityScale = 0; }
        if (deathEffect) Instantiate(deathEffect, player.transform.position, Quaternion.identity);
        if (audioSource && deathSound) audioSource.PlayOneShot(deathSound);
        if (sr) sr.enabled = false;

        // Mostrar pantalla de muerte — el jugador pulsa continuar para seguir
        if (DeathScreen.Instance != null)
        {
            DeathScreen.Instance.Show();

            // Esperar a que el jugador pulse continuar (DeathScreen desaparece)
            yield return new WaitUntil(() => !DeathScreen.Instance.IsShowing);
        }
        else
        {
            // Sin DeathScreen: esperar el delay normal
            yield return new WaitForSeconds(respawnDelay);
        }

        // Resetear mundo
        if (EnemyManager.Instance != null)
            EnemyManager.Instance.RespawnAllEnemies();
        else
            Debug.LogWarning("EnemyManager no encontrado.");

        // Mover al checkpoint
        if (GameManager.Instance != null)
            player.transform.position = GameManager.Instance.GetRespawnPosition();

        // Restaurar vida y viales
        playerHealth?.Heal(playerHealth.maxHealth);
        player.GetComponent<HealingSystem>()?.RefillVials();

        // Reactivar jugador
        if (rb != null) { rb.gravityScale = 3.5f; rb.linearVelocity = Vector2.zero; }
        if (col) col.enabled = true;
        if (sr) sr.enabled = true;
        if (playerCore) playerCore.enabled = true;

        CameraManager.instance?.ResetToDefaultCamera();


        isRespawning = false;
    }
}