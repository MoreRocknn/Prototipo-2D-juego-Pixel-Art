using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class RestUIManager : MonoBehaviour
{
    public static RestUIManager Instance;

    [Header("UI General")]
    public Slider healthBarSlider;
    public TextMeshProUGUI vialsText;

    [Header("Panel de Descanso (RestPanel)")]
    public GameObject restPanel;
    public TextMeshProUGUI restMessageText;
    public Image restProgressBar;

    [Header("Prompt de Interacción")]
    public GameObject promptPanel;
    public TextMeshProUGUI promptText;

    [Header("Configuración Visual")]
    public Color restingColor = new Color(0.3f, 1f, 0.3f);
    public float fadeSpeed = 2f;

    private CanvasGroup restCanvasGroup;
    private CanvasGroup promptCanvasGroup;

    // FIX: PlayerHealth en vez de MainChar para la vida
    private PlayerHealth playerHealth;
    private HealingSystem healingSystem;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        SetupCanvasGroup(restPanel, ref restCanvasGroup);
        SetupCanvasGroup(promptPanel, ref promptCanvasGroup);

        // FIX: buscar PlayerHealth en vez de MainChar
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            healingSystem = player.GetComponent<HealingSystem>();
        }

        UpdatePlayerStats();
    }

    void Update()
    {
        if (playerHealth != null)
            UpdatePlayerStats();
    }

    public void UpdatePlayerStats()
    {
        if (playerHealth == null || healingSystem == null) return;

        // Barra de vida
        if (healthBarSlider != null)
        {
            float pct = playerHealth.maxHealth > 0
                ? (float)playerHealth.currentHealth / playerHealth.maxHealth
                : 0f;
            healthBarSlider.value = pct;
        }

        // Viales
        if (vialsText != null)
        {
            vialsText.text = $"Viales: {healingSystem.currentHealingVials}";
            vialsText.color = healingSystem.currentHealingVials > 0 ? Color.white : Color.red;
        }
    }

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
            if (restProgressBar != null) restProgressBar.fillAmount = elapsed / duration;
            yield return null;
        }
        if (restProgressBar != null) restProgressBar.fillAmount = 1f;
        if (restMessageText != null) restMessageText.text = "¡Recuperado!";
        yield return new WaitForSeconds(0.5f);
        HideRestPanel();
    }

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
        while (cg.alpha < 1f) { cg.alpha += Time.deltaTime * fadeSpeed; yield return null; }
        cg.alpha = 1f;
    }

    IEnumerator FadeOut(CanvasGroup cg, System.Action onComplete)
    {
        if (cg == null) yield break;
        while (cg.alpha > 0f) { cg.alpha -= Time.deltaTime * fadeSpeed; yield return null; }
        cg.alpha = 0f;
        onComplete?.Invoke();
    }
}