using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class CheckPoint : MonoBehaviour
{
    public bool isActivated = false;

    [Header("Efectos visuales")]
    public GameObject inactiveVisual;
    public GameObject activeVisual;
    public ParticleSystem activationEffect;
    public ParticleSystem restEffect;

    [Header("Audio")]
    public AudioClip activationSound;
    public AudioClip restSound;
    public AudioClip refillSound;
    private AudioSource audioSource;

    [Header("Sistema de Descanso")]
    public KeyCode restKey = KeyCode.R;
    public float restDuration = 2f;
    public bool canRestHere = true;

    [Header("UI Prompt (opcional)")]
    public GameObject uiPrompt;
    public TextMeshPro promptText;

    private bool playerInRange = false;
    private bool isResting = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        UpdateVisuals();

        if (uiPrompt != null)
        {
            uiPrompt.SetActive(false);
        }
    }

    void Update()
    {
        if (playerInRange && isActivated && canRestHere && !isResting)
        {
            if (Input.GetKeyDown(restKey))
            {
                StartCoroutine(RestAtAltar());
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;

            if (!isActivated)
            {
                ActivateCheckpoint();
            }

            if (isActivated && canRestHere && uiPrompt != null)
            {
                uiPrompt.SetActive(true);
                if (promptText != null)
                {
                    promptText.text = $"Presiona [{restKey}] para descansar";
                }
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            if (uiPrompt != null)
            {
                uiPrompt.SetActive(false);
            }
        }
    }

    void ActivateCheckpoint()
    {
        isActivated = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCheckpoint(transform.position);
        }

        UpdateVisuals();
        if (activationEffect != null)
        {
            activationEffect.Play();
        }

        if (audioSource != null && activationSound != null)
        {
            audioSource.PlayOneShot(activationSound);
        }

        Debug.Log("¡Checkpoint activado!");
    }

    IEnumerator RestAtAltar()
    {
        isResting = true;

        if (uiPrompt != null)
        {
            uiPrompt.SetActive(false);
        }

        Debug.Log("Descansando en el altar...");

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        MainChar playerController = player?.GetComponent<MainChar>();
        Rigidbody2D playerRb = player?.GetComponent<Rigidbody2D>(); // Obtenemos el RB
        HealingSystem healingSystem = player?.GetComponent<HealingSystem>();

        // --- CORRECCIÓN: FRENAR EN SECO ---
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero; // Frenar movimiento lineal
            playerRb.angularVelocity = 0f;
        }
        // ----------------------------------

        if (playerController != null)
        {
            playerController.enabled = false;
        }

        if (restEffect != null)
        {
            restEffect.Play();
        }

        if (audioSource != null && restSound != null)
        {
            audioSource.PlayOneShot(restSound);
        }

        // Llamar a la UI si existe
        if (RestUIManager.Instance != null)
        {
            RestUIManager.Instance.ShowRestPanel(restDuration);
        }

        yield return new WaitForSeconds(restDuration);

        // 1. CURAR AL JUGADOR
        if (playerController != null)
        {
            playerController.currentHealth = playerController.maxHealth;
            Debug.Log("¡Vida restaurada completamente!");
        }

        // 2. RECARGAR VIALES
        if (healingSystem != null)
        {
            healingSystem.RefillVials();
        }

        // Sonido de recarga
        if (audioSource != null && refillSound != null)
        {
            audioSource.PlayOneShot(refillSound);
        }

        // 3. REAPARECEN LOS ENEMIGOS
        EnemyManager.Instance?.RespawnAllEnemies();

        Debug.Log("¡Descanso completado!");

        if (playerController != null)
        {
            playerController.enabled = true;
        }

        isResting = false;

        if (playerInRange && uiPrompt != null)
        {
            uiPrompt.SetActive(true);
        }
    }

    void UpdateVisuals()
    {
        if (inactiveVisual != null)
            inactiveVisual.SetActive(!isActivated);
        if (activeVisual != null)
            activeVisual.SetActive(isActivated);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = isActivated ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        Gizmos.color = new Color(0, 1, 0, 0.3f);
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.DrawWireCube(transform.position, col.size);
        }
    }
}