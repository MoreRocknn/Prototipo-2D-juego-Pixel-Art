using UnityEngine;
using UnityEngine.UI;

// ============================================
// UI BARRA DE DASH - Debajo de la vida
// Versión PC - Barra horizontal elegante
// ============================================
public class DashCooldownUI : MonoBehaviour
{
    [Header("⚠️ IMPORTANTE: Asigna manualmente si no se detecta")]
    [Tooltip("Arrastra aquí el GameObject del jugador (el que tiene MainChar)")]
    public GameObject playerObject;

    [Header("Configuración Visual")]
    public Color readyColor = new Color(0.3f, 0.8f, 1f); // Cyan brillante
    public Color cooldownColor = new Color(0.3f, 0.3f, 0.4f, 0.9f); // Gris oscuro
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
    public bool showNumericCooldown = true;

    [Header("Posición (Debajo de la Vida)")]
    [Tooltip("Posición X desde la izquierda")]
    public float xPosition = 20f;
    [Tooltip("Posición Y desde arriba (justo debajo de la barra de vida)")]
    public float yPosition = -60f;
    [Tooltip("Ancho de la barra")]
    public float barWidth = 250f;
    [Tooltip("Alto de la barra")]
    public float barHeight = 20f;

    [Header("Estado (Solo lectura)")]
    [SerializeField] private bool isUICreated = false;
    [SerializeField] private bool hasDash = false;
    [SerializeField] private float currentCooldown = 0f;
    [SerializeField] private float maxCooldown = 3f;

    private Canvas canvas;
    private GameObject dashBarPanel;
    private Image barFillImage;
    private Image barBackgroundImage;
    private Text cooldownText;
    private Text dashLabelText;
    private AbilityHolder abilityHolder;
    private DashAbility dashAbility;

    void Start()
    {
        Debug.Log("🎨 DashCooldownUI: Iniciando sistema de barra...");

        // Intentar encontrar el jugador
        FindPlayer();

        // Crear la UI
        CreateDashBarUI();

        if (isUICreated)
        {
            Debug.Log("✅ Barra de Dash creada correctamente");
        }
        else
        {
            Debug.LogError("❌ Error al crear la barra de Dash");
        }
    }

    void FindPlayer()
    {
        // Si ya está asignado manualmente, usar ese
        if (playerObject != null)
        {
            abilityHolder = playerObject.GetComponent<AbilityHolder>();
            if (abilityHolder != null)
            {
                Debug.Log("✅ AbilityHolder encontrado en objeto asignado manualmente");
                return;
            }
        }

        // Si no, buscar por tag
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerObject = player;
            abilityHolder = player.GetComponent<AbilityHolder>();

            if (abilityHolder != null)
            {
                Debug.Log("✅ AbilityHolder encontrado automáticamente");
            }
            else
            {
                Debug.LogWarning("⚠️ Jugador encontrado pero sin AbilityHolder");
            }
        }
        else
        {
            Debug.LogError("❌ No se encontró jugador con tag 'Player'");
        }
    }

    void Update()
    {
        if (!isUICreated) return;

        // Re-buscar si se perdió la referencia
        if (abilityHolder == null && playerObject != null)
        {
            abilityHolder = playerObject.GetComponent<AbilityHolder>();
        }

        // Verificar estado
        if (abilityHolder == null)
        {
            hasDash = false;
            UpdateBarState(0f, "SIN JUGADOR", Color.red);
            return;
        }

        if (abilityHolder.currentAbility == null)
        {
            hasDash = false;
            UpdateBarState(0f, "SIN DASH", new Color(0.5f, 0.5f, 0.5f));
            return;
        }

        // Obtener el DashAbility
        dashAbility = abilityHolder.currentAbility as DashAbility;

        if (dashAbility == null)
        {
            hasDash = false;
            string abilityType = abilityHolder.currentAbility.GetType().Name;
            UpdateBarState(0f, $"HABILIDAD: {abilityType}", new Color(1f, 0.7f, 0f));
            return;
        }

        hasDash = true;
        maxCooldown = dashAbility.dashCooldown;

        // Actualizar barra con cooldown real
        UpdateDashBar();
    }

    void UpdateBarState(float fillAmount, string message, Color textColor)
    {
        if (dashBarPanel != null)
        {
            dashBarPanel.SetActive(true);
        }

        if (barFillImage != null)
        {
            barFillImage.fillAmount = fillAmount;
            barFillImage.color = cooldownColor;
        }

        if (dashLabelText != null)
        {
            dashLabelText.text = message;
            dashLabelText.color = textColor;
        }

        if (cooldownText != null)
        {
            cooldownText.text = "";
        }
    }

    void UpdateDashBar()
    {
        currentCooldown = dashAbility.GetCooldownRemaining();
        bool isReady = currentCooldown <= 0f;

        // Mostrar panel
        if (dashBarPanel != null)
        {
            dashBarPanel.SetActive(true);
        }

        // Actualizar relleno de la barra
        if (barFillImage != null)
        {
            float fillAmount = isReady ? 1f : 1f - (currentCooldown / maxCooldown);
            barFillImage.fillAmount = fillAmount;

            // Color animado
            if (isReady)
            {
                // Efecto de pulso cuando está listo
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 3f);
                barFillImage.color = Color.Lerp(readyColor, Color.white, pulse * 0.3f);
            }
            else
            {
                barFillImage.color = cooldownColor;
            }
        }

        // Actualizar texto del label
        if (dashLabelText != null)
        {
            dashLabelText.text = isReady ? "⚡ DASH" : "⏳ DASH";
            dashLabelText.color = isReady ? readyColor : new Color(0.6f, 0.6f, 0.7f);
        }

        // Actualizar texto numérico
        if (cooldownText != null && showNumericCooldown)
        {
            if (isReady)
            {
                cooldownText.text = "Q";
                cooldownText.color = readyColor;
                cooldownText.fontStyle = FontStyle.Bold;
            }
            else
            {
                cooldownText.text = $"{currentCooldown:F1}s";
                cooldownText.color = Color.white;
                cooldownText.fontStyle = FontStyle.Normal;
            }
        }
    }

    void CreateDashBarUI()
    {
        try
        {
            // Crear Canvas
            GameObject canvasObj = new GameObject("DashBarCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            // Panel principal (contenedor de la barra)
            GameObject panelObj = new GameObject("DashBarPanel");
            panelObj.transform.SetParent(canvas.transform, false);
            dashBarPanel = panelObj;

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            // Anclaje: esquina superior izquierda
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(xPosition, yPosition);
            panelRect.sizeDelta = new Vector2(barWidth, barHeight + 30); // Espacio para label

            Debug.Log($"✅ Panel creado en posición: ({xPosition}, {yPosition})");

            // ====================
            // LABEL "DASH" ARRIBA
            // ====================
            GameObject labelObj = new GameObject("DashLabel");
            labelObj.transform.SetParent(panelObj.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 1);
            labelRect.anchorMax = new Vector2(0, 1);
            labelRect.pivot = new Vector2(0, 1);
            labelRect.anchoredPosition = new Vector2(0, 0);
            labelRect.sizeDelta = new Vector2(100, 20);

            dashLabelText = labelObj.AddComponent<Text>();
            dashLabelText.text = "⚡ DASH";
            dashLabelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dashLabelText.fontSize = 14;
            dashLabelText.fontStyle = FontStyle.Bold;
            dashLabelText.alignment = TextAnchor.MiddleLeft;
            dashLabelText.color = readyColor;

            Outline labelOutline = labelObj.AddComponent<Outline>();
            labelOutline.effectColor = Color.black;
            labelOutline.effectDistance = new Vector2(1, -1);

            // ====================
            // FONDO DE LA BARRA
            // ====================
            GameObject bgObj = new GameObject("BarBackground");
            bgObj.transform.SetParent(panelObj.transform, false);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0);
            bgRect.anchorMax = new Vector2(0, 0);
            bgRect.pivot = new Vector2(0, 0);
            bgRect.anchoredPosition = new Vector2(0, 0);
            bgRect.sizeDelta = new Vector2(barWidth, barHeight);

            barBackgroundImage = bgObj.AddComponent<Image>();
            barBackgroundImage.color = backgroundColor;

            // Borde del fondo
            Outline bgOutline = bgObj.AddComponent<Outline>();
            bgOutline.effectColor = new Color(0.2f, 0.5f, 0.7f, 0.8f);
            bgOutline.effectDistance = new Vector2(1, -1);

            // ====================
            // BARRA DE RELLENO
            // ====================
            GameObject fillObj = new GameObject("BarFill");
            fillObj.transform.SetParent(bgObj.transform, false);

            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0, 0);
            fillRect.anchorMax = new Vector2(0, 1); // Se estira verticalmente
            fillRect.pivot = new Vector2(0, 0.5f);
            fillRect.anchoredPosition = new Vector2(2, 0);
            fillRect.sizeDelta = new Vector2(barWidth - 4, -4);

            barFillImage = fillObj.AddComponent<Image>();
            barFillImage.type = Image.Type.Filled;
            barFillImage.fillMethod = Image.FillMethod.Horizontal;
            barFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            barFillImage.fillAmount = 0f;
            barFillImage.color = readyColor;

            // ====================
            // TEXTO DE COOLDOWN (dentro de la barra)
            // ====================
            GameObject textObj = new GameObject("CooldownText");
            textObj.transform.SetParent(bgObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.sizeDelta = Vector2.zero;

            cooldownText = textObj.AddComponent<Text>();
            cooldownText.text = "";
            cooldownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cooldownText.fontSize = 14;
            cooldownText.fontStyle = FontStyle.Bold;
            cooldownText.alignment = TextAnchor.MiddleRight;
            cooldownText.color = Color.white;

            Outline textOutline = textObj.AddComponent<Outline>();
            textOutline.effectColor = Color.black;
            textOutline.effectDistance = new Vector2(1, -1);

            // Padding para el texto
            RectOffset padding = new RectOffset(5, 5, 0, 0);

            Debug.Log("✅ Barra de Dash creada completamente");

            isUICreated = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error creando barra: {e.Message}\n{e.StackTrace}");
            isUICreated = false;
        }
    }

    // Método para ajustar posición desde el Inspector
    public void SetPosition(float x, float y)
    {
        xPosition = x;
        yPosition = y;

        if (dashBarPanel != null)
        {
            RectTransform rect = dashBarPanel.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(x, y);
            }
        }
    }

    // Debug visual
    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 12;
        style.normal.textColor = Color.white;

        string debugMsg = "";

        if (!isUICreated)
        {
            style.normal.textColor = Color.red;
            debugMsg = "❌ Barra de Dash: UI no creada";
        }
        else if (abilityHolder == null)
        {
            style.normal.textColor = Color.red;
            debugMsg = $"❌ Dash UI: No se encuentra AbilityHolder";
            if (playerObject != null)
                debugMsg += $" (Objeto: {playerObject.name})";
        }
        else if (!hasDash)
        {
            style.normal.textColor = Color.yellow;
            string abilityName = abilityHolder.currentAbility != null ?
                abilityHolder.currentAbility.GetType().Name : "null";
            debugMsg = $"⚠️ Dash UI: Habilidad = {abilityName}";
        }
        else
        {
            style.normal.textColor = Color.green;
            float percentage = ((maxCooldown - currentCooldown) / maxCooldown) * 100f;
            debugMsg = $"✅ Dash UI: {percentage:F0}% cargado ({currentCooldown:F1}s)";
        }

        GUI.Label(new Rect(10, Screen.height - 30, 500, 25), debugMsg, style);
    }
}