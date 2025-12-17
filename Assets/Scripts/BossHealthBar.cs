using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthBar : MonoBehaviour
{
    [Header("Referencias UI")]
    public Slider healthSlider;
    public TextMeshProUGUI bossNameText;
    public Image fillImage;

    [Header("Colores")]
    public Color highHealthColor = Color.red;
    public Color midHealthColor = new Color(1f, 0.5f, 0f); // Naranja
    public Color lowHealthColor = new Color(0.5f, 0f, 0f); // Rojo oscuro

    private int maxHealth;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Buscar referencias automáticamente si no están asignadas
        if (healthSlider == null)
            healthSlider = GetComponentInChildren<Slider>();

        if (bossNameText == null)
            bossNameText = GetComponentInChildren<TextMeshProUGUI>();

        if (fillImage == null && healthSlider != null)
            fillImage = healthSlider.fillRect.GetComponent<Image>();

        // Si NO encuentra nada, crear UI dinámicamente
        if (bossNameText == null || healthSlider == null)
        {
            CreateHealthBarUI();
        }
    }

    void CreateHealthBarUI()
    {
        // Crear Canvas si no existe
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
        }

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        // Panel contenedor
        GameObject panel = new GameObject("BossHealthPanel");
        panel.transform.SetParent(transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0, -50);
        panelRect.sizeDelta = new Vector2(800, 100);

        // Nombre del Boss (TextMeshProUGUI)
        GameObject nameObj = new GameObject("BossName");
        nameObj.transform.SetParent(panel.transform, false);

        bossNameText = nameObj.AddComponent<TextMeshProUGUI>();
        bossNameText.text = "BOSS";
        bossNameText.fontSize = 36;
        bossNameText.fontStyle = FontStyles.Bold;
        bossNameText.alignment = TextAlignmentOptions.Center;
        bossNameText.color = Color.white;

        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0f, 0.5f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.anchoredPosition = Vector2.zero;
        nameRect.sizeDelta = new Vector2(0, 50);

        // Fondo de la barra
        GameObject bgObj = new GameObject("HealthBarBackground");
        bgObj.transform.SetParent(panel.transform, false);

        Image healthBarBackground = bgObj.AddComponent<Image>();
        healthBarBackground.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0f);
        bgRect.anchorMax = new Vector2(1f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0);
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = new Vector2(-40, -10);

        // Crear Slider
        GameObject sliderObj = new GameObject("HealthSlider");
        sliderObj.transform.SetParent(bgObj.transform, false);

        healthSlider = sliderObj.AddComponent<Slider>();

        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.sizeDelta = Vector2.zero;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

        // Barra de vida (Fill)
        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(fillArea.transform, false);

        fillImage = fillObj.AddComponent<Image>();
        fillImage.color = highHealthColor;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;

        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.pivot = new Vector2(0.5f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = Vector2.zero;

        // Configurar slider
        healthSlider.fillRect = fillRect;
        healthSlider.minValue = 0;
        healthSlider.maxValue = 100;
        healthSlider.value = 100;
        healthSlider.interactable = false;

        Debug.Log("✅ UI de BossHealthBar creada dinámicamente con TextMeshProUGUI");
    }

    public void Initialize(string name, int maxHp)
    {
        maxHealth = maxHp;

        if (bossNameText != null)
            bossNameText.text = name;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHp;
            healthSlider.value = maxHp;
        }

        UpdateHealthColor(maxHp);
        Show();

        Debug.Log($"✅ BossHealthBar inicializada: {name} ({maxHp} HP)");
    }

    public void UpdateHealth(int currentHp)
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHp;
            UpdateHealthColor(currentHp);
        }
    }

    void UpdateHealthColor(int currentHp)
    {
        if (fillImage == null || maxHealth == 0) return;

        float healthPercent = (float)currentHp / maxHealth;

        if (healthPercent > 0.6f)
            fillImage.color = highHealthColor;
        else if (healthPercent > 0.3f)
            fillImage.color = midHealthColor;
        else
            fillImage.color = lowHealthColor;
    }

    public void Show()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        // Destruir después de un delay
        Destroy(gameObject, 0.5f);
    }
}