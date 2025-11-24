using UnityEngine;
using UnityEngine.UI;

// ============================================
// BARRA DE VIDA SOBRE ENEMIGOS
// ============================================
public class HealthBarUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public Slider healthSlider;
    public Image fillImage;
    public Image backgroundImage;

    [Header("Colores")]
    public Color fullHealthColor = Color.green;
    public Color midHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;
    public Color criticalHealthColor = new Color(0.5f, 0f, 0f);
    public Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

    [Header("Configuración")]
    public Vector3 offset = new Vector3(0, 1.5f, 0);
    public bool hideWhenFull = true;
    public bool alwaysShow = false;

    [Header("Animación")]
    public float smoothSpeed = 5f;

    private Transform targetTransform;
    private Canvas canvas;
    private float currentDisplayHealth;
    private float maxHealth;
    private Camera mainCamera;

    void Awake()
    {
        mainCamera = Camera.main;

        // Si no hay referencias asignadas, intentar encontrarlas
        if (healthSlider == null)
        {
            healthSlider = GetComponentInChildren<Slider>();
        }

        if (fillImage == null && healthSlider != null)
        {
            fillImage = healthSlider.fillRect?.GetComponent<Image>();
        }

        if (backgroundImage == null)
        {
            backgroundImage = GetComponentInChildren<Image>();
        }

        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.WorldSpace;

        // Configurar el RectTransform
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.sizeDelta = new Vector2(1f, 0.15f);
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = backgroundColor;
            transform.localScale = Vector3.one * 0.02f;
        }
    }

    void LateUpdate()
    {
        if (targetTransform != null)
        {
            // Seguir al objetivo
            transform.position = targetTransform.position + offset;

            // Hacer que la barra siempre mire a la cámara
            if (mainCamera != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.transform.position);
            }
        }

        // Animar el slider suavemente
        if (healthSlider != null)
        {
            healthSlider.value = Mathf.Lerp(healthSlider.value, currentDisplayHealth / maxHealth, Time.deltaTime * smoothSpeed);
        }
    }

    public void Initialize(Transform target, int startHealth, int maxHealthValue)
    {
        targetTransform = target;
        maxHealth = maxHealthValue;
        currentDisplayHealth = startHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = 1f;
            healthSlider.value = 1f;
        }

        UpdateVisibility();
    }

    public void UpdateHealth(int currentHealth, int maxHealthValue)
    {
        maxHealth = maxHealthValue;
        currentDisplayHealth = currentHealth;

        // Actualizar color según el porcentaje de vida
        float healthPercent = currentHealth / maxHealth;
        UpdateHealthColor(healthPercent);

        UpdateVisibility();
    }

    void UpdateHealthColor(float healthPercent)
    {
        if (fillImage == null) return;

        Color targetColor;

        if (healthPercent > 0.6f)
        {
            // Verde a amarillo
            float t = (healthPercent - 0.6f) / 0.4f;
            targetColor = Color.Lerp(midHealthColor, fullHealthColor, t);
        }
        else if (healthPercent > 0.3f)
        {
            // Amarillo a rojo
            float t = (healthPercent - 0.3f) / 0.3f;
            targetColor = Color.Lerp(lowHealthColor, midHealthColor, t);
        }
        else
        {
            // Rojo a rojo oscuro
            float t = healthPercent / 0.3f;
            targetColor = Color.Lerp(criticalHealthColor, lowHealthColor, t);
        }

        fillImage.color = targetColor;
    }

    void UpdateVisibility()
    {
        if (alwaysShow)
        {
            gameObject.SetActive(true);
            return;
        }

        if (hideWhenFull)
        {
            bool shouldShow = currentDisplayHealth < maxHealth;
            gameObject.SetActive(shouldShow);
        }
    }

    public void ForceShow()
    {
        gameObject.SetActive(true);
    }

    public void ForceHide()
    {
        gameObject.SetActive(false);
    }
}

// NOTA: La clase HealthBarFactory ahora está en un archivo separado
// llamado "HealthBarFactory.cs". Este archivo solo contiene HealthBarUI.