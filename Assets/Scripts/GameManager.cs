using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Sistema de Respawn")]
    public Vector2 lastCheckpoint;
    public bool hasCheckpoint = false;
    public int playerHealth = 3;
    public int maxHealth = 3;

    [Header("Configuración")]
    public Vector2 spawnInicial = new Vector2(0, 0);

    // ========================================
    // SISTEMA DE HABILIDADES PERMANENTES
    // ========================================
    [Header("Habilidades Permanentes")]
    [Tooltip("Si es true, el jugador ya tiene el Dash de forma permanente")]
    public bool hasPermanentDash = false;

    public void TakeDamage(int damage)
    {
        playerHealth -= damage;
        if (playerHealth <= 0)
        {
            // Respawn
        }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (!hasCheckpoint)
        {
            lastCheckpoint = spawnInicial;
        }
    }

    public void SetCheckpoint(Vector2 position)
    {
        lastCheckpoint = position;
        hasCheckpoint = true;
        Debug.Log($"Checkpoint guardado en: {position}");
    }

    public Vector2 GetRespawnPosition()
    {
        return lastCheckpoint;
    }

    // ========================================
    // MÉTODOS PARA HABILIDADES PERMANENTES
    // ========================================

    /// <summary>
    /// Llama a este método cuando el jugador absorbe el Dash por primera vez.
    /// El Dash quedará guardado permanentemente.
    /// </summary>
    public void UnlockPermanentDash()
    {
        if (!hasPermanentDash)
        {
            hasPermanentDash = true;
            Debug.Log("¡DASH DESBLOQUEADO PERMANENTEMENTE!");
        }
    }

    /// <summary>
    /// Verifica si el jugador tiene el Dash permanente.
    /// </summary>
    public bool HasPermanentDash()
    {
        return hasPermanentDash;
    }

    /// <summary>
    /// Resetea el progreso del jugador (para nuevo juego).
    /// </summary>
    public void ResetProgress()
    {
        hasPermanentDash = false;
        hasCheckpoint = false;
        lastCheckpoint = spawnInicial;
        Debug.Log("Progreso reseteado");
    }
    public void RespawnPlayer()
    {
        // Resetear enemigos normales
        EnemyManager.Instance?.RespawnAllEnemies();

        // Resetear el boss si existe
        BossController boss = FindFirstObjectByType<BossController>();
        if (boss != null) boss.ResetState();

        // Resetear cámara
        CameraManager.instance?.RespawnToCamera(lastCheckpoint);
    }
}