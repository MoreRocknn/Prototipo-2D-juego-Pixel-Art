using UnityEngine;
using UnityEngine.UI;
using TMPro; // Necesario para TextMeshPro

public class BossHealthBar : MonoBehaviour
{
    [Header("Referencias UI")]
    public TextMeshProUGUI bossNameText; // Cambiado de Text a TextMeshProUGUI
    public Image healthBarFill;
    public Image healthBarBackground;
    public CanvasGroup canvasGroup;

    [Header("Configuración")]
    public float fadeInDuration = 1f;
    public float fadeOutDuration = 0.5f;
    public Color healthBarColor = Color.red;
    public Color lowHealthColor = new Color(0.8f, 0.1f, 0.1f);

    // Asigna aquí tu fuente SDF (ej. LiberationSans SDF) si se crea por código
    public TMP_FontAsset fontAsset;

    private int maxHealth;
    private int currentHealth;
    private bool isVisible = false;

    void Awake()
    {
        // Si no hay referencias asignadas, crear la UI dinámicamente
        if (bossNameText == null || healthBarFill == null)
        {
            CreateHealthBarUI();
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    void CreateHealthBarUI()
    {
        // Crear Canvas
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

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
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

        // Nombre del Boss (TMP)
        GameObject nameObj = new GameObject("BossName");
        nameObj.transform.SetParent(panel.transform, false);

        bossNameText = nameObj.AddComponent<TextMeshProUGUI>();

        // Configuración específica de TMP
        if (fontAsset != null) bossNameText.font = fontAsset;
        bossNameText.text = "BOSS";
        bossNameText.fontSize = 36;
        bossNameText.fontStyle = FontStyles.Bold; // Enum de TMP
        bossNameText.alignment = TextAlignmentOptions.Center; // Enum de TMP
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

        healthBarBackground = bgObj.AddComponent<Image>();
        healthBarBackground.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0f);
        bgRect.anchorMax = new Vector2(1f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0);
        bgRect.anchoredPosition = Vector2.zero;
        bgRect.sizeDelta = new Vector2(-40, -10);

        // Barra de vida
        GameObject fillObj = new GameObject("HealthBarFill");
        fillObj.transform.SetParent(bgObj.transform, false);

        healthBarFill = fillObj.AddComponent<Image>();
        healthBarFill.color = healthBarColor;
        healthBarFill.type = Image.Type.Filled;
        healthBarFill.fillMethod = Image.FillMethod.Horizontal;
        healthBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;

        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.pivot = new Vector2(0.5f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = Vector2.zero;
    }

    public void Initialize(string bossName, int maxHp)
    {
        maxHealth = maxHp;
        currentHealth = maxHp;

        if (bossNameText != null)
        {
            bossNameText.text = bossName.ToUpper();
        }

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = 1f;
        }

        Show();
    }

    public void UpdateHealth(int newHealth)
    {
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);

        if (healthBarFill != null)
        {
            float targetFill = (float)currentHealth / maxHealth;
            StopAllCoroutines();
            StartCoroutine(SmoothUpdateHealthBar(targetFill));
        }

        // Cambiar color en vida baja
        if (healthBarFill != null && currentHealth <= maxHealth * 0.3f)
        {
            healthBarFill.color = lowHealthColor;
        }
    }

    System.Collections.IEnumerator SmoothUpdateHealthBar(float targetFill)
    {
        float currentFill = healthBarFill.fillAmount;
        float elapsed = 0f;
        float duration = 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            healthBarFill.fillAmount = Mathf.Lerp(currentFill, targetFill, elapsed / duration);
            yield return null;
        }

        healthBarFill.fillAmount = targetFill;
    }

    public void Show()
    {
        if (isVisible) return;
        isVisible = true;
        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }

    public void Hide()
    {
        if (!isVisible) return;
        isVisible = false;
        StopAllCoroutines();
        StartCoroutine(FadeOut());
    }

    System.Collections.IEnumerator FadeIn()
    {
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            }
            yield return null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }
    }

    System.Collections.IEnumerator FadeOut()
    {
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeOutDuration);
            }
            yield return null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        Destroy(gameObject);
    }
}