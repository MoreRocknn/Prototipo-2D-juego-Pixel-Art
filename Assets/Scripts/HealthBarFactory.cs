using UnityEngine;
using UnityEngine.UI;

public class HealthBarFactory : MonoBehaviour
{
    public static HealthBarFactory Instance { get; private set; }

    [Header("=== TAMAÑO ESTILO DARK SOULS ===")]
    public float barWidth = 200f;
    public float barHeight = 20f;
    public float worldScale = 0.02f;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public HealthBarUI CreateHealthBar(Transform target, int currentHealth, int maxHealth, Vector3 offset)
    {
        // ========================================
        // ROOT
        // ========================================
        GameObject barRoot = new GameObject($"HealthBar_{target.name}");
        barRoot.transform.position = target.position + offset;

        // ========================================
        // CANVAS
        // ========================================
        GameObject canvasObj = new GameObject("Canvas");
        canvasObj.transform.SetParent(barRoot.transform, false);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingLayerName = "UI";
        canvas.sortingOrder = 999;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 100;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(barWidth, barHeight);
        canvasRect.localScale = Vector3.one * worldScale;
        canvasRect.localPosition = Vector3.zero;

        // ========================================
        // BORDE EXTERIOR NEGRO (Marco grueso)
        // ========================================
        GameObject borderObj = new GameObject("Border");
        borderObj.transform.SetParent(canvasRect, false);
        Image borderImage = borderObj.AddComponent<Image>();
        borderImage.color = Color.black;
        borderImage.raycastTarget = false;

        RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.anchorMin = Vector2.zero;
        borderRect.anchorMax = Vector2.one;
        borderRect.offsetMin = new Vector2(-3f, -3f);
        borderRect.offsetMax = new Vector2(3f, 3f);

        // ========================================
        // FONDO ROJO OSCURO (vida perdida)
        // ========================================
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasRect, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.3f, 0.05f, 0.05f, 1f);
        bgImage.raycastTarget = false;

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // ========================================
        // BARRA DE DAÑO (naranja - baja lento)
        // ========================================
        GameObject damageObj = new GameObject("DamageFill");
        damageObj.transform.SetParent(canvasRect, false);
        Image damageImage = damageObj.AddComponent<Image>();
        damageImage.color = new Color(0.9f, 0.5f, 0.1f, 1f);
        damageImage.type = Image.Type.Filled;
        damageImage.fillMethod = Image.FillMethod.Horizontal;
        damageImage.fillOrigin = 0;
        damageImage.raycastTarget = false;

        RectTransform damageRect = damageObj.GetComponent<RectTransform>();
        damageRect.anchorMin = new Vector2(0.02f, 0.1f);
        damageRect.anchorMax = new Vector2(0.98f, 0.9f);
        damageRect.offsetMin = Vector2.zero;
        damageRect.offsetMax = Vector2.zero;

        // ========================================
        // BARRA DE VIDA PRINCIPAL (rojo sangre)
        // ========================================
        GameObject healthObj = new GameObject("HealthFill");
        healthObj.transform.SetParent(canvasRect, false);
        Image healthImage = healthObj.AddComponent<Image>();
        healthImage.color = new Color(0.8f, 0.15f, 0.1f, 1f);
        healthImage.type = Image.Type.Filled;
        healthImage.fillMethod = Image.FillMethod.Horizontal;
        healthImage.fillOrigin = 0;
        healthImage.raycastTarget = false;

        RectTransform healthRect = healthObj.GetComponent<RectTransform>();
        healthRect.anchorMin = new Vector2(0.02f, 0.1f);
        healthRect.anchorMax = new Vector2(0.98f, 0.9f);
        healthRect.offsetMin = Vector2.zero;
        healthRect.offsetMax = Vector2.zero;

        // ========================================
        // BRILLO SUPERIOR
        // ========================================
        GameObject shineObj = new GameObject("Shine");
        shineObj.transform.SetParent(canvasRect, false);
        Image shineImage = shineObj.AddComponent<Image>();
        shineImage.color = new Color(1f, 1f, 1f, 0.2f);
        shineImage.raycastTarget = false;

        RectTransform shineRect = shineObj.GetComponent<RectTransform>();
        shineRect.anchorMin = new Vector2(0.02f, 0.55f);
        shineRect.anchorMax = new Vector2(0.98f, 0.88f);
        shineRect.offsetMin = Vector2.zero;
        shineRect.offsetMax = Vector2.zero;

        // ========================================
        // COMPONENTE
        // ========================================
        HealthBarUI healthBar = barRoot.AddComponent<HealthBarUI>();
        healthBar.borderImage = borderImage;
        healthBar.backgroundImage = bgImage;
        healthBar.damageFillImage = damageImage;
        healthBar.healthFillImage = healthImage;
        healthBar.shineImage = shineImage;
        healthBar.offset = offset;

        // IMPORTANTE: Inicializar OCULTA hasta recibir daño
        healthBar.Initialize(target, currentHealth, maxHealth);

        return healthBar;
    }
}