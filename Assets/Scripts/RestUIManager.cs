using UnityEngine;
using UnityEngine.UI; // Necesario para Slider e Image
using TMPro;
using System.Collections;

public class RestUIManager : MonoBehaviour
{
    public static RestUIManager Instance;

    [Header("UI General")]
    public Slider healthBarSlider; // <--- NUEVO: Arrastra tu Slider de Vida aquí
    public TextMeshProUGUI vialsText; // Mantenemos viales en texto o puedes cambiarlo luego

    [Header("Panel de Descanso (RestPanel)")]
    public GameObject restPanel;
    public TextMeshProUGUI restMessageText;
    public Image restProgressBar; // La barra que se llena al descansar

    [Header("Prompt de Interacción")]
    public GameObject promptPanel;
    public TextMeshProUGUI promptText;

    [Header("Configuración Visual")]
    public Color restingColor = new Color(0.3f, 1f, 0.3f);
    public float fadeSpeed = 2f;

    private CanvasGroup restCanvasGroup;
    private CanvasGroup promptCanvasGroup;

    // Referencias cacheadas del jugador
    private MainChar playerScript;
    private HealingSystem healingSystem;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Configuramos CanvasGroups
        SetupCanvasGroup(restPanel, ref restCanvasGroup);
        SetupCanvasGroup(promptPanel, ref promptCanvasGroup);

        // Encontrar al jugador una sola vez
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerScript = player.GetComponent<MainChar>();
            healingSystem = player.GetComponent<HealingSystem>();
        }

        UpdatePlayerStats(); // Actualizar UI al inicio
    }

    void Update()
    {
        // Actualizamos siempre para ver la vida bajar/subir en tiempo real
        if (playerScript != null)
        {
            UpdatePlayerStats();
        }
    }

    // Método simplificado para actualizar la UI
    public void UpdatePlayerStats()
    {
        if (playerScript == null || healingSystem == null) return;

        // --- ACTUALIZAR BARRA DE VIDA (VISUAL) ---
        if (healthBarSlider != null)
        {
            // Calculamos el porcentaje de vida (0 a 1)
            float healthPercent = (float)playerScript.currentHealth / playerScript.maxHealth;
            healthBarSlider.value = healthPercent;
        }

        // --- ACTUALIZAR TEXTO DE VIALES ---
        if (vialsText != null)
        {
            vialsText.text = $"Viales: {healingSystem.currentHealingVials}";
            // Cambiar color si no quedan viales
            vialsText.color = (healingSystem.currentHealingVials > 0) ? Color.white : Color.red;
        }
    }

    // --- MÉTODOS DEL SISTEMA DE DESCANSO ---

    public void ShowRestPanel(float duration)
    {
        if (restPanel == null) return;
        restPanel.SetActive(true);
        if (restProgressBar != null) restProgressBar.fillAmount = 0f;

        StartCoroutine(FadeIn(restCanvasGroup));
        StartCoroutine(AnimateRestProgress(duration));
    }

    public void HideRestPanel()
    {
        if (restPanel == null) return;
        StartCoroutine(FadeOut(restCanvasGroup, () => restPanel.SetActive(false)));
    }

    IEnumerator AnimateRestProgress(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (restProgressBar != null)
                restProgressBar.fillAmount = elapsed / duration;
            yield return null;
        }

        if (restProgressBar != null) restProgressBar.fillAmount = 1f;
        if (restMessageText != null) restMessageText.text = "¡Recuperado!";

        yield return new WaitForSeconds(0.5f);
        HideRestPanel();
    }

    // --- UTILIDADES ---

    public void ShowPrompt(string message)
    {
        if (promptPanel == null) return;
        promptPanel.SetActive(true);
        if (promptText != null) promptText.text = message;
        StartCoroutine(FadeIn(promptCanvasGroup));
    }

    public void HidePrompt()
    {
        if (promptPanel == null) return;
        StartCoroutine(FadeOut(promptCanvasGroup, () => promptPanel.SetActive(false)));
    }

    void SetupCanvasGroup(GameObject panel, ref CanvasGroup group)
    {
        if (panel == null) return;
        group = panel.GetComponent<CanvasGroup>();
        if (group == null) group = panel.AddComponent<CanvasGroup>();
        panel.SetActive(false);
    }

    IEnumerator FadeIn(CanvasGroup cg)
    {
        if (cg == null) yield break;
        cg.alpha = 0f;
        while (cg.alpha < 1f)
        {
            cg.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }
        cg.alpha = 1f;
    }

    IEnumerator FadeOut(CanvasGroup cg, System.Action onComplete)
    {
        if (cg == null) yield break;
        while (cg.alpha > 0f)
        {
            cg.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
        cg.alpha = 0f;
        onComplete?.Invoke();
    }
}