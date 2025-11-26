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
    private MainChar playerController;
    private bool isHealing = false;

    // VARIABLE NUEVA
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

        playerController = GetComponent<MainChar>();

        // GUARDAR COLOR ORIGINAL
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
        else baseColor = Color.white;
    }

    void Update()
    {
        if (Input.GetKeyDown(healKey))
        {
            TryUseHealingVial();
        }
    }

    public void TryUseHealingVial()
    {
        if (isHealing) return;
        if (currentHealingVials <= 0)
        {
            Debug.Log("¡No tienes viales de curación!");
            return;
        }

        if (playerController.currentHealth >= playerController.maxHealth)
        {
            Debug.Log("¡Vida completa!");
            return;
        }

        StartCoroutine(HealSequence());
    }

    IEnumerator HealSequence()
    {
        isHealing = true;

        if (playerController != null)
        {
            playerController.SetInputLock(true);
            playerController.StopPhysics();
        }

        if (audioSource != null && healSound != null)
        {
            audioSource.PlayOneShot(healSound);
        }

        Debug.Log("Usando vial...");

        yield return new WaitForSeconds(healAnimationDuration);

        currentHealingVials--;

        int healAmount = Mathf.CeilToInt(playerController.maxHealth * healPercentage);
        if (healAmount < 1) healAmount = 1;

        playerController.currentHealth += healAmount;
        if (playerController.currentHealth > playerController.maxHealth)
        {
            playerController.currentHealth = playerController.maxHealth;
        }

        Debug.Log($"¡Curado! (+{healAmount} HP). Vida: {playerController.currentHealth}/{playerController.maxHealth}");

        if (healEffect != null) healEffect.Play();
        StartCoroutine(FlashSprite());

        if (playerController != null)
        {
            playerController.SetInputLock(false);
        }

        isHealing = false;
    }

    public void RefillVials()
    {
        currentHealingVials = maxHealingVials;
        isHealing = false;
        Debug.Log($"¡Viales recargados! {currentHealingVials}/{maxHealingVials}");
    }

    public void OnPlayerDeath()
    {
        RefillVials();
    }

    IEnumerator FlashSprite()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            float elapsed = 0f;

            while (elapsed < healFlashDuration)
            {
                float t = elapsed / healFlashDuration;
                // Lerp hacia baseColor (Blanco) en lugar del color actual
                sr.color = Color.Lerp(healFlashColor, baseColor, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            sr.color = baseColor; // Asegurar blanco al final
        }
    }

    public string GetVialsInfo()
    {
        return $"Viales: {currentHealingVials}/{maxHealingVials}";
    }
}