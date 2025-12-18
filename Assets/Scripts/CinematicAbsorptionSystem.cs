using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// ============================================
// SISTEMA CINEMATOGRÁFICO DE ABSORCIÓN
// Versión con zoom ajustable desde Inspector
// ============================================
public class CinematicAbsorptionSystem : MonoBehaviour
{
    public static CinematicAbsorptionSystem Instance { get; private set; }

    [Header("Referencias")]
    public Camera mainCamera;
    public Canvas cinematicCanvas;

    [Header("⚙️ Configuración de Zoom")]
    [Tooltip("Tamaño de cámara al hacer zoom (MAYOR = menos zoom, MENOR = más zoom)")]
    [Range(2f, 8f)]
    public float zoomedOrthographicSize = 4.5f; // CAMBIADO: Valor por defecto más lejano
    [Tooltip("Velocidad del zoom")]
    [Range(0.1f, 2f)]
    public float zoomDuration = 0.5f;
    public AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("📏 Distancia de la Cámara")]
    [Tooltip("Distancia horizontal desde el objetivo (0 = centrado)")]
    [Range(-5f, 5f)]
    public float cameraOffsetX = 0f;
    [Tooltip("Distancia vertical desde el objetivo (positivo = arriba)")]
    [Range(-3f, 3f)]
    public float cameraOffsetY = 0.5f;

    [Header("Bordes Negros (Letterbox)")]
    public GameObject topLetterbox;
    public GameObject bottomLetterbox;
    [Range(50f, 200f)]
    public float letterboxHeight = 100f;
    [Range(0.1f, 1f)]
    public float letterboxFadeDuration = 0.3f;

    [Header("UI de Absorción")]
    public GameObject absorptionPromptPanel;
    public Text absorptionPromptText;
    public Image absorptionKeyIcon;
    public string absorptionKeyText = "E";
    public Color promptColor = Color.cyan;

    [Header("Efectos Visuales")]
    public bool enableVignette = true;
    [Range(0f, 1f)]
    public float vignetteIntensity = 0.4f;
    public bool enableSlowMotion = true;
    [Range(0.1f, 1f)]
    public float slowMotionScale = 0.5f;

    [Header("Audio (Opcional)")]
    public AudioSource cinematicAudioSource;
    public AudioClip absorptionStartSound;
    public AudioClip absorptionCompleteSound;

    [Header("🔧 Debug")]
    [Tooltip("Mostrar información de zoom en pantalla")]
    public bool showDebugInfo = true;

    private float originalOrthographicSize;
    private Vector3 originalCameraPosition;
    private bool isInCinematicMode = false;
    private CanvasGroup topLetterboxCanvasGroup;
    private CanvasGroup bottomLetterboxCanvasGroup;
    private CanvasGroup promptCanvasGroup;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        SetupCinematicElements();
    }

    void Start()
    {
        if (mainCamera != null)
        {
            originalOrthographicSize = mainCamera.orthographicSize;
            originalCameraPosition = mainCamera.transform.position;

            Debug.Log($"📹 Cámara Original: Size={originalOrthographicSize}, Zoom Target={zoomedOrthographicSize}");
        }
    }

    void SetupCinematicElements()
    {
        // Crear Canvas si no existe
        if (cinematicCanvas == null)
        {
            GameObject canvasObj = new GameObject("CinematicCanvas");
            cinematicCanvas = canvasObj.AddComponent<Canvas>();
            cinematicCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cinematicCanvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Crear bordes negros (Letterbox)
        CreateLetterbox();

        // Crear prompt de absorción
        CreateAbsorptionPrompt();
    }

    void CreateLetterbox()
    {
        // Top Letterbox
        GameObject topObj = new GameObject("TopLetterbox");
        topObj.transform.SetParent(cinematicCanvas.transform, false);
        topLetterbox = topObj;

        RectTransform topRect = topObj.AddComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0, 1);
        topRect.anchorMax = new Vector2(1, 1);
        topRect.pivot = new Vector2(0.5f, 1);
        topRect.sizeDelta = new Vector2(0, letterboxHeight);
        topRect.anchoredPosition = Vector2.zero;

        Image topImage = topObj.AddComponent<Image>();
        topImage.color = Color.black;

        topLetterboxCanvasGroup = topObj.AddComponent<CanvasGroup>();
        topLetterboxCanvasGroup.alpha = 0;

        // Bottom Letterbox
        GameObject bottomObj = new GameObject("BottomLetterbox");
        bottomObj.transform.SetParent(cinematicCanvas.transform, false);
        bottomLetterbox = bottomObj;

        RectTransform bottomRect = bottomObj.AddComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0, 0);
        bottomRect.anchorMax = new Vector2(1, 0);
        bottomRect.pivot = new Vector2(0.5f, 0);
        bottomRect.sizeDelta = new Vector2(0, letterboxHeight);
        bottomRect.anchoredPosition = Vector2.zero;

        Image bottomImage = bottomObj.AddComponent<Image>();
        bottomImage.color = Color.black;

        bottomLetterboxCanvasGroup = bottomObj.AddComponent<CanvasGroup>();
        bottomLetterboxCanvasGroup.alpha = 0;

        topLetterbox.SetActive(false);
        bottomLetterbox.SetActive(false);
    }

    void CreateAbsorptionPrompt()
    {
        // Panel principal
        GameObject promptObj = new GameObject("AbsorptionPromptPanel");
        promptObj.transform.SetParent(cinematicCanvas.transform, false);
        absorptionPromptPanel = promptObj;

        RectTransform promptRect = promptObj.AddComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.5f, 0.5f);
        promptRect.anchorMax = new Vector2(0.5f, 0.5f);
        promptRect.pivot = new Vector2(0.5f, 0.5f);
        promptRect.sizeDelta = new Vector2(400, 100);
        promptRect.anchoredPosition = new Vector2(0, -150);

        promptCanvasGroup = promptObj.AddComponent<CanvasGroup>();
        promptCanvasGroup.alpha = 0;

        // Background del prompt
        Image bgImage = promptObj.AddComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f);

        // Borde decorativo
        Outline bgOutline = promptObj.AddComponent<Outline>();
        bgOutline.effectColor = promptColor;
        bgOutline.effectDistance = new Vector2(2, -2);

        // Texto del prompt
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(promptObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = new Vector2(-20, -20);
        textRect.anchoredPosition = Vector2.zero;

        absorptionPromptText = textObj.AddComponent<Text>();
        absorptionPromptText.text = $"Presiona [{absorptionKeyText}] para Absorber";
        absorptionPromptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        absorptionPromptText.fontSize = 28;
        absorptionPromptText.fontStyle = FontStyle.Bold;
        absorptionPromptText.alignment = TextAnchor.MiddleCenter;
        absorptionPromptText.color = promptColor;

        // Efecto de outline
        Outline outline = textObj.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);

        absorptionPromptPanel.SetActive(false);

        // Animación de pulso
        StartCoroutine(PulsePrompt());
    }

    IEnumerator PulsePrompt()
    {
        while (true)
        {
            if (absorptionPromptPanel != null && absorptionPromptPanel.activeSelf)
            {
                float scale = 1f + Mathf.Sin(Time.unscaledTime * 3f) * 0.1f;
                absorptionPromptPanel.transform.localScale = Vector3.one * scale;
            }
            yield return null;
        }
    }

    // ============================================
    // MÉTODOS PÚBLICOS
    // ============================================

    public void StartCinematicMode(Transform target, System.Action onAbsorptionComplete)
    {
        if (isInCinematicMode) return;

        StartCoroutine(CinematicModeSequence(target, onAbsorptionComplete));
    }

    IEnumerator CinematicModeSequence(Transform target, System.Action onComplete)
    {
        isInCinematicMode = true;

        // Guardar posición original de la cámara
        originalCameraPosition = mainCamera.transform.position;

        // Reproducir sonido de inicio
        if (cinematicAudioSource != null && absorptionStartSound != null)
        {
            cinematicAudioSource.PlayOneShot(absorptionStartSound);
        }

        // Activar slow motion
        if (enableSlowMotion)
        {
            Time.timeScale = slowMotionScale;
        }

        // Lockear input del jugador
        MainChar player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<MainChar>();
        if (player != null)
        {
            player.SetInputLock(true);
            player.StopPhysics(); // Detener movimiento
        }

        // Mostrar bordes negros
        topLetterbox.SetActive(true);
        bottomLetterbox.SetActive(true);
        yield return StartCoroutine(FadeLetterbox(true));

        // Hacer zoom hacia el objetivo
        yield return StartCoroutine(ZoomToTarget(target.position));

        // Mostrar prompt de absorción
        absorptionPromptPanel.SetActive(true);
        yield return StartCoroutine(FadePrompt(true));

        // Esperar a que el jugador presione E
        bool absorbed = false;
        while (!absorbed)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                absorbed = true;

                // Reproducir sonido de absorción
                if (cinematicAudioSource != null && absorptionCompleteSound != null)
                {
                    cinematicAudioSource.PlayOneShot(absorptionCompleteSound);
                }

                // Ocultar prompt
                yield return StartCoroutine(FadePrompt(false));

                // Ejecutar la absorción
                onComplete?.Invoke();

                // Esperar un momento para efecto dramático
                yield return new WaitForSecondsRealtime(0.5f);
            }
            yield return null;
        }

        // Salir del modo cinemático
        yield return StartCoroutine(ExitCinematicMode());
    }

    IEnumerator ExitCinematicMode()
    {
        // Zoom out
        yield return StartCoroutine(ZoomOut());

        // Ocultar bordes
        yield return StartCoroutine(FadeLetterbox(false));
        topLetterbox.SetActive(false);
        bottomLetterbox.SetActive(false);

        absorptionPromptPanel.SetActive(false);

        // Restaurar velocidad normal
        Time.timeScale = 1f;

        // Desbloquear input del jugador
        MainChar player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<MainChar>();
        if (player != null)
        {
            player.SetInputLock(false);
        }

        isInCinematicMode = false;
    }

    IEnumerator ZoomToTarget(Vector3 targetPosition)
    {
        if (mainCamera == null) yield break;

        Vector3 startPos = mainCamera.transform.position;

        // Aplicar offset configurable
        Vector3 endPos = new Vector3(
            targetPosition.x + cameraOffsetX,
            targetPosition.y + cameraOffsetY,
            startPos.z
        );

        float startSize = mainCamera.orthographicSize;
        float elapsed = 0f;

        if (showDebugInfo)
        {
            Debug.Log($"📹 Zoom Start: Size {startSize} → {zoomedOrthographicSize}");
        }

        while (elapsed < zoomDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = zoomCurve.Evaluate(elapsed / zoomDuration);

            mainCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
            mainCamera.orthographicSize = Mathf.Lerp(startSize, zoomedOrthographicSize, t);

            yield return null;
        }

        mainCamera.transform.position = endPos;
        mainCamera.orthographicSize = zoomedOrthographicSize;

        if (showDebugInfo)
        {
            Debug.Log($"📹 Zoom Complete: Size={mainCamera.orthographicSize}, Pos={mainCamera.transform.position}");
        }
    }

    IEnumerator ZoomOut()
    {
        if (mainCamera == null) yield break;

        Vector3 startPos = mainCamera.transform.position;
        float startSize = mainCamera.orthographicSize;
        float elapsed = 0f;

        while (elapsed < zoomDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = zoomCurve.Evaluate(elapsed / zoomDuration);

            mainCamera.transform.position = Vector3.Lerp(startPos, originalCameraPosition, t);
            mainCamera.orthographicSize = Mathf.Lerp(startSize, originalOrthographicSize, t);

            yield return null;
        }

        mainCamera.transform.position = originalCameraPosition;
        mainCamera.orthographicSize = originalOrthographicSize;
    }

    IEnumerator FadeLetterbox(bool fadeIn)
    {
        float startAlpha = fadeIn ? 0f : 1f;
        float endAlpha = fadeIn ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < letterboxFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / letterboxFadeDuration;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, t);

            if (topLetterboxCanvasGroup != null)
                topLetterboxCanvasGroup.alpha = alpha;
            if (bottomLetterboxCanvasGroup != null)
                bottomLetterboxCanvasGroup.alpha = alpha;

            yield return null;
        }

        if (topLetterboxCanvasGroup != null)
            topLetterboxCanvasGroup.alpha = endAlpha;
        if (bottomLetterboxCanvasGroup != null)
            bottomLetterboxCanvasGroup.alpha = endAlpha;
    }

    IEnumerator FadePrompt(bool fadeIn)
    {
        float startAlpha = fadeIn ? 0f : 1f;
        float endAlpha = fadeIn ? 1f : 0f;
        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, t);

            if (promptCanvasGroup != null)
                promptCanvasGroup.alpha = alpha;

            yield return null;
        }

        if (promptCanvasGroup != null)
            promptCanvasGroup.alpha = endAlpha;
    }

    public bool IsInCinematicMode()
    {
        return isInCinematicMode;
    }

    // Debug visual en el Inspector
    void OnGUI()
    {
        if (!showDebugInfo || mainCamera == null) return;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 14;
        style.normal.textColor = Color.white;

        string debugText = $"📹 Camera Size: {mainCamera.orthographicSize:F2}\n";
        debugText += $"🎯 Zoom Target: {zoomedOrthographicSize:F2}\n";
        debugText += $"📊 Ratio: {(mainCamera.orthographicSize / originalOrthographicSize):P0}";

        if (isInCinematicMode)
        {
            style.normal.textColor = Color.cyan;
            debugText += "\n🎬 MODO CINEMATICO ACTIVO";
        }

        GUI.Label(new Rect(Screen.width - 250, 10, 240, 80), debugText, style);
    }

    // Métodos para probar en el Editor
    [ContextMenu("Test Zoom In")]
    void TestZoomIn()
    {
        if (mainCamera != null && !isInCinematicMode)
        {
            StartCoroutine(ZoomToTarget(mainCamera.transform.position));
        }
    }

    [ContextMenu("Test Zoom Out")]
    void TestZoomOut()
    {
        if (mainCamera != null)
        {
            StartCoroutine(ZoomOut());
        }
    }
}