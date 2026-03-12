using UnityEngine;
using System.Collections;

public class HealingSystem : MonoBehaviour
{
    public static HealingSystem Instance;

    [Header("Sistema de Viales")]
    public int maxHealingVials = 3;
    public int currentHealingVials = 3;
    public KeyCode healKey = KeyCode.E;

    [Header("Configuración Dark Souls")]
    [Range(0f, 1f)]
    public float healPercentage = 0.5f;
    public float healAnimationDuration = 1.35f;

    [Header("Efectos de Curación")]
    public ParticleSystem healEffect;
    public AudioClip healSound;
    public Color healFlashColor = new Color(0.3f, 1f, 0.3f);
    public float healFlashDuration = 0.5f;

    private AudioSource audioSource;
    private PlayerHealth playerHealth;   // FIX: usar PlayerHealth en vez de MainChar
    private PlayerCore playerCore;     // para SetInputLock y StopPhysics
    private bool isHealing = false;
    private Color baseColor;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // FIX: buscar PlayerHealth y PlayerCore en vez de MainChar
        playerHealth = GetComponent<PlayerHealth>();
        playerCore = GetComponent<PlayerCore>();

        if (playerHealth == null)
            Debug.LogError("[HealingSystem] No se encontró PlayerHealth en este GameObject.");

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        baseColor = sr != null ? sr.color : Color.white;
    }

    void Update()
    {
        if (Input.GetKeyDown(healKey))
            TryUseHealingVial();
    }

    public void TryUseHealingVial()
    {
        if (isHealing) return;

        if (playerHealth == null)
        {
            Debug.LogError("[HealingSystem] playerHealth es null.");
            return;
        }

        if (currentHealingVials <= 0)
        {
            Debug.Log("¡No tienes viales de curación!");
            return;
        }

        if (playerHealth.currentHealth >= playerHealth.maxHealth)
        {
            Debug.Log("¡Vida completa!");
            return;
        }

        StartCoroutine(HealSequence());
    }

    IEnumerator HealSequence()
    {
        isHealing = true;

        // Bloquear input durante la animación de curación
        playerCore?.SetInputLock(true);
        playerCore?.StopPhysics();

        if (audioSource != null && healSound != null)
            audioSource.PlayOneShot(healSound);

        Debug.Log("Usando vial...");
        yield return new WaitForSeconds(healAnimationDuration);

        currentHealingVials--;

        // FIX: usar PlayerHealth.Heal() para que actualice la barra automáticamente
        int healAmount = Mathf.Max(1, Mathf.CeilToInt(playerHealth.maxHealth * healPercentage));
        playerHealth.Heal(healAmount);

        Debug.Log($"¡Curado! (+{healAmount} HP). Vida: {playerHealth.currentHealth}/{playerHealth.maxHealth}");

        if (healEffect != null) healEffect.Play();
        StartCoroutine(FlashSprite());

        playerCore?.SetInputLock(false);
        isHealing = false;
    }

    public void RefillVials()
    {
        currentHealingVials = maxHealingVials;
        isHealing = false;
        Debug.Log($"¡Viales recargados! {currentHealingVials}/{maxHealingVials}");
    }

    public void OnPlayerDeath() => RefillVials();

    IEnumerator FlashSprite()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        float elapsed = 0f;
        while (elapsed < healFlashDuration)
        {
            sr.color = Color.Lerp(healFlashColor, baseColor, elapsed / healFlashDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        sr.color = baseColor;
    }

    public string GetVialsInfo() => $"Viales: {currentHealingVials}/{maxHealingVials}";
}