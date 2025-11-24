using UnityEngine;
using UnityEngine.UI;

// ============================================
// FACTORY PARA CREAR BARRAS DE VIDA
// ============================================
public class HealthBarFactory : MonoBehaviour
{
    public static HealthBarFactory Instance { get; private set; }

    [Header("Prefab de Barra de Vida")]
    [Tooltip("Arrastra aquí tu prefab de HealthBar. Si lo dejas vacío, se creará automáticamente.")]
    public GameObject healthBarPrefab;

    [Header("Configuración de Auto-Creación")]
    [Tooltip("Si está activado y no hay prefab, creará barras automáticamente")]
    public bool autoCreateIfNoPrefab = true;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Opcional: mantener entre escenas
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (healthBarPrefab != null)
        {
            Debug.Log("✓ HealthBarFactory: Prefab asignado correctamente - " + healthBarPrefab.name);
        }
        else if (autoCreateIfNoPrefab)
        {
            Debug.LogWarning("⚠ HealthBarFactory: No hay prefab asignado. Las barras se crearán automáticamente.");
        }
        else
        {
            Debug.LogError("✗ HealthBarFactory: ¡No hay prefab asignado y la auto-creación está desactivada!");
        }
    }

    public HealthBarUI CreateHealthBar(Transform target, int currentHealth, int maxHealth, Vector3 offset)
    {
        if (healthBarPrefab != null)
        {
            // Usar el prefab asignado
            GameObject barInstance = Instantiate(healthBarPrefab, target.position + offset, Quaternion.identity);
            barInstance.transform.SetParent(null); // No hijo del enemigo para que no rote con él

            HealthBarUI healthBar = barInstance.GetComponent<HealthBarUI>();
            if (healthBar == null)
            {
                Debug.LogError("El prefab no tiene el componente HealthBarUI!");
                healthBar = barInstance.AddComponent<HealthBarUI>();
            }

            healthBar.Initialize(target, currentHealth, maxHealth);
            healthBar.offset = offset;

            return healthBar;
        }
        else if (autoCreateIfNoPrefab)
        {
            // Crear barra programáticamente
            Debug.Log("Creando barra de vida automáticamente para: " + target.name);
            return CreateHealthBarProgrammatically(target, currentHealth, maxHealth, offset);
        }
        else
        {
            Debug.LogError("No se puede crear barra de vida - no hay prefab ni auto-creación");
            return null;
        }
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