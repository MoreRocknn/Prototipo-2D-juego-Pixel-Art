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

// ============================================
// FACTORY PARA CREAR BARRAS DE VIDA
// ============================================
public class HealthBarFactory : MonoBehaviour
{
    public static HealthBarFactory Instance { get; private set; }

    [Header("Prefab")]
    public GameObject healthBarPrefab;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public HealthBarUI CreateHealthBar(Transform target, int currentHealth, int maxHealth, Vector3 offset)
    {
        if (healthBarPrefab == null)
        {
            Debug.LogError("HealthBarPrefab no está asignado en HealthBarFactory!");
            return CreateHealthBarProgrammatically(target, currentHealth, maxHealth, offset);
        }

        GameObject barInstance = Instantiate(healthBarPrefab, target.position + offset, Quaternion.identity);
        barInstance.transform.SetParent(null); // No hijo del enemigo para que no rote con él

        HealthBarUI healthBar = barInstance.GetComponent<HealthBarUI>();
        if (healthBar == null)
        {
            healthBar = barInstance.AddComponent<HealthBarUI>();
        }

        healthBar.Initialize(target, currentHealth, maxHealth);
        healthBar.offset = offset;

        return healthBar;
    }

    private HealthBarUI CreateHealthBarProgrammatically(Transform target, int currentHealth, int maxHealth, Vector3 offset)
    {
        // Crear Canvas para la barra
        GameObject canvasObj = new GameObject($"HealthBar_{target.name}");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1f, 0.15f);

        // Crear background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // Crear Slider
        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(canvasObj.transform, false);
        Slider slider = sliderObj.AddComponent<Slider>();

        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.05f, 0.2f);
        sliderRect.anchorMax = new Vector2(0.95f, 0.8f);
        sliderRect.sizeDelta = Vector2.zero;

        // Crear Fill Area
        GameObject fillAreaObj = new GameObject("Fill Area");
        fillAreaObj.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        // Crear Fill
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillAreaObj.transform, false);
        Image fillImage = fillObj.AddComponent<Image>();
        fillImage.color = Color.green;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;

        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        // Configurar Slider
        slider.fillRect = fillRect;
        slider.transition = Selectable.Transition.None;

        // Añadir componente HealthBarUI
        HealthBarUI healthBar = canvasObj.AddComponent<HealthBarUI>();
        healthBar.healthSlider = slider;
        healthBar.fillImage = fillImage;
        healthBar.backgroundImage = bgImage;
        healthBar.offset = offset;

        healthBar.Initialize(target, currentHealth, maxHealth);

        return healthBar;
    }
}