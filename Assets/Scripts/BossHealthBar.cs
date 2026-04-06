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
    public Color midHealthColor = new Color(1f, 0.5f, 0f);
    public Color lowHealthColor = new Color(0.5f, 0f, 0f);

    public int maxHealth;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Ocultar visualmente sin desactivar el GameObject
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (healthSlider == null)
            healthSlider = GetComponentInChildren<Slider>();

        if (bossNameText == null)
            bossNameText = GetComponentInChildren<TextMeshProUGUI>();

        if (fillImage == null && healthSlider != null)
            fillImage = healthSlider.fillRect.GetComponent<Image>();

        if (bossNameText == null || healthSlider == null)
            CreateHealthBarUI();
    }

    void CreateHealthBarUI()
    {
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

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("BossHealthPanel");
        panel.transform.SetParent(transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0, -50);
        panelRect.sizeDelta = new Vector2(800, 100);

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

        GameObject sliderObj = new GameObject("HealthSlider");
        sliderObj.transform.SetParent(bgObj.transform, false);

        healthSlider = sliderObj.AddComponent<Slider>();

        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.sizeDelta = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;

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

        healthSlider.fillRect = fillRect;
        healthSlider.minValue = 0;
        healthSlider.maxValue = 100;
        healthSlider.value = 100;
        healthSlider.interactable = false;
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
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        Destroy(gameObject, 0.5f);
    }
}