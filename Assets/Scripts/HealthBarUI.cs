using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("=== REFERENCIAS UI ===")]
    public Image borderImage;
    public Image backgroundImage;
    public Image damageFillImage;
    public Image healthFillImage;
    public Image shineImage;

    [Header("=== POSICIÓN ===")]
    public Vector3 offset = new Vector3(0, 2f, 0);

    [Header("=== VISIBILIDAD ===")]
    [Tooltip("Si es TRUE, siempre visible. Si es FALSE, solo aparece al recibir daño")]
    public bool alwaysShow = false;
    public float hideDelay = 4f;

    [Header("=== COLORES DARK SOULS ===")]
    public Color healthColor = new Color(0.75f, 0.12f, 0.12f, 1f);
    public Color damageColor = new Color(0.95f, 0.55f, 0.1f, 1f);
    public Color criticalColor = new Color(0.5f, 0.08f, 0.08f, 1f);

    [Header("=== ANIMACIÓN ===")]
    public float damageAnimSpeed = 0.8f;
    public float criticalThreshold = 0.25f;
    public float pulseSpeed = 5f;

    // Referencias internas
    private Transform target;
    private int currentHealth;
    private int maxHealth;
    private float displayedHealth = 1f;
    private float displayedDamage = 1f;
    private float hideTimer;
    private bool isVisible = false;
    private Vector3 originalScale;

    public void Initialize(Transform targetTransform, int health, int max)
    {
        target = targetTransform;
        maxHealth = max;
        currentHealth = health;
        displayedHealth = 1f;
        displayedDamage = 1f;

        originalScale = transform.localScale;

        // Configurar colores iniciales
        if (healthFillImage) healthFillImage.color = healthColor;
        if (damageFillImage) damageFillImage.color = damageColor;

        // IMPORTANTE: Empezar OCULTA
        SetVisible(false);
    }

    void LateUpdate()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Seguir al objetivo
        transform.position = target.position + offset;
        transform.rotation = Quaternion.identity;

        // Mantener escala (ignorar flip del sprite enemigo)
        transform.localScale = new Vector3(
            Mathf.Abs(originalScale.x),
            Mathf.Abs(originalScale.y),
            Mathf.Abs(originalScale.z)
        );

        // Solo actualizar si está visible
        if (!isVisible) return;

        // Animación estilo Dark Souls: la barra naranja baja lentamente
        if (displayedDamage > displayedHealth)
        {
            displayedDamage = Mathf.MoveTowards(displayedDamage, displayedHealth, damageAnimSpeed * Time.deltaTime);
            if (damageFillImage) damageFillImage.fillAmount = displayedDamage;
        }

        // Pulso cuando está crítico
        if (displayedHealth <= criticalThreshold && displayedHealth > 0)
        {
            float pulse = 0.7f + Mathf.Sin(Time.time * pulseSpeed) * 0.3f;
            if (healthFillImage)
            {
                Color c = criticalColor;
                c.a = pulse;
                healthFillImage.color = c;
            }
        }

        // Auto-ocultar después de un tiempo (solo si no es alwaysShow)
        if (!alwaysShow)
        {
            hideTimer -= Time.deltaTime;
            if (hideTimer <= 0)
            {
                SetVisible(false);
            }
        }
    }

    public void UpdateHealth(int health, int max)
    {
        int previousHealth = currentHealth;
        currentHealth = health;
        maxHealth = max;

        float newPercent = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;

        // ¿Recibió daño?
        if (health < previousHealth)
        {
            // MOSTRAR la barra
            SetVisible(true);
            hideTimer = hideDelay;

            // La barra roja baja inmediatamente
            displayedHealth = newPercent;
            if (healthFillImage) healthFillImage.fillAmount = displayedHealth;

            // La barra naranja se queda arriba y bajará lentamente en LateUpdate
            // (displayedDamage mantiene su valor anterior)
        }
        else if (health > previousHealth)
        {
            // Curación: ambas suben
            displayedHealth = newPercent;
            displayedDamage = newPercent;
            if (healthFillImage) healthFillImage.fillAmount = displayedHealth;
            if (damageFillImage) damageFillImage.fillAmount = displayedDamage;
        }

        // Actualizar color según vida
        UpdateColor();
    }

    void UpdateColor()
    {
        if (healthFillImage == null) return;

        if (displayedHealth <= criticalThreshold)
        {
            healthFillImage.color = criticalColor;
        }
        else
        {
            healthFillImage.color = healthColor;
        }
    }

    void SetVisible(bool visible)
    {
        isVisible = visible;

        if (borderImage) borderImage.enabled = visible;
        if (backgroundImage) backgroundImage.enabled = visible;
        if (damageFillImage) damageFillImage.enabled = visible;
        if (healthFillImage) healthFillImage.enabled = visible;
        if (shineImage) shineImage.enabled = visible;
    }

    /// <summary>
    /// Forzar que se muestre la barra (útil para jefes)
    /// </summary>
    public void ForceShow()
    {
        SetVisible(true);
        hideTimer = hideDelay;
    }

    /// <summary>
    /// Forzar ocultar
    /// </summary>
    public void ForceHide()
    {
        SetVisible(false);
    }

    /// <summary>
    /// Para jefes: mantener siempre visible
    /// </summary>
    public void SetAlwaysVisible(bool always)
    {
        alwaysShow = always;
        if (always) SetVisible(true);
    }
}