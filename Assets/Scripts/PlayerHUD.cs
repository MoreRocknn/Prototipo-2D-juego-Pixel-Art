// ============================================================
// PlayerHUD.cs — HUD del jugador
// Pon este script en el GameObject "Display" del Canvas.
// Conecta los campos en el Inspector.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHUD : MonoBehaviour
{
    [Header("=== VIDA ===")]
    [Tooltip("Arrastra aquí el Slider de vida del Canvas")]
    public Slider healthSlider;

    [Header("=== VIALES ===")]
    [Tooltip("Arrastra aquí el Text (TMP) que muestra los viales")]
    public TextMeshProUGUI vialsText;

    [Tooltip("Formato del texto. {0} = actuales, {1} = máximo")]
    public string vialsFormat = "Viales: {0}/{1}";

    [Header("=== DASH ===")]
    [Tooltip("Arrastra aquí la Image Fill de la barra del Dash (DashBar → Fill)")]
    public Image dashFillImage;

    // Referencias cacheadas
    private PlayerHealth playerHealth;
    private HealingSystem healingSystem;
    private AbilityHolder abilityHolder;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) { Debug.LogWarning("[PlayerHUD] No se encontró jugador con tag Player."); return; }

        playerHealth = player.GetComponent<PlayerHealth>();
        healingSystem = player.GetComponent<HealingSystem>();
        abilityHolder = player.GetComponent<AbilityHolder>();

        // Inicializar valores al arrancar
        UpdateAll();
    }

    void Update()
    {
        UpdateAll();
    }

    void UpdateAll()
    {
        UpdateHealth();
        UpdateVials();
        UpdateDash();
    }

    // ── Barra de vida ─────────────────────────────────────────
    void UpdateHealth()
    {
        if (healthSlider == null || playerHealth == null) return;
        healthSlider.value = playerHealth.maxHealth > 0
            ? (float)playerHealth.currentHealth / playerHealth.maxHealth
            : 0f;
    }

    // ── Viales ───────────────────────────────────────────────
    void UpdateVials()
    {
        if (vialsText == null || healingSystem == null) return;
        vialsText.text = string.Format(vialsFormat, healingSystem.currentHealingVials, healingSystem.maxHealingVials);
        vialsText.color = healingSystem.currentHealingVials > 0 ? Color.white : Color.red;
    }

    // ── Barra de Dash ────────────────────────────────────────
    void UpdateDash()
    {
        if (dashFillImage == null || abilityHolder == null) return;

        Ability current = abilityHolder.GetAbility();
        if (current == null || current.abilityType != AbilityType.Dash)
        {
            dashFillImage.fillAmount = 0f;
            return;
        }

        DashAbility dash = current as DashAbility;
        if (dash == null) { dashFillImage.fillAmount = 0f; return; }

        float cooldown = dash.GetCooldownRemaining();
        float maxCooldown = dash.dashCooldown;
        dashFillImage.fillAmount = maxCooldown <= 0f ? 1f
            : cooldown <= 0f ? 1f
            : 1f - (cooldown / maxCooldown);
    }
}