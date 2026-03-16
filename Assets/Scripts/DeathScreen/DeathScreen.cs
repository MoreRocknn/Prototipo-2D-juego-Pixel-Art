// ============================================================
// DeathScreen.cs — Pantalla de muerte estilo Dark Souls
//
// SETUP EN UNITY:
//   1. Crea un Canvas (Screen Space - Overlay, Sort Order alto ej: 100)
//   2. Añade este script al Canvas o a un GameObject vacío hijo
//   3. Crea la jerarquía de UI:
//
//      Canvas
//       └─ DeathScreen (este script aquí)
//           ├─ BlackOverlay       (Image, color negro, stretch completo)
//           ├─ DeathText          (TextMeshProUGUI, centrado)
//           └─ ContinueButton     (Button con TextMeshProUGUI hijo)
//
//   4. Conecta los campos en el Inspector
//   5. Llama DeathScreen.Instance.Show() cuando el jugador muera
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class DeathScreen : MonoBehaviour
{
    public static DeathScreen Instance { get; private set; }

    [Header("=== TEXTO ===")]
    [Tooltip("El texto que aparece en pantalla al morir")]
    public string deathText = "YOU DIED";

    [Tooltip("Color del texto de muerte")]
    public Color textColor = new Color(0.7f, 0.05f, 0.05f, 1f);

    [Header("=== TIMING ===")]
    [Tooltip("Segundos que tarda en aparecer el fade negro")]
    public float fadeDuration = 1.5f;

    [Tooltip("Segundos que tarda en aparecer el texto tras el fade")]
    public float textDelay = 0.5f;

    [Tooltip("Segundos que tarda en aparecer el botón tras el texto")]
    public float buttonDelay = 1.2f;

    [Header("=== BOTÓN ===")]
    [Tooltip("Texto del botón de continuar")]
    public string continueText = "Continuar";

    [Header("=== REFERENCIAS UI ===")]
    [Tooltip("Image negra que cubre toda la pantalla")]
    public Image blackOverlay;

    [Tooltip("TextMeshProUGUI del texto de muerte")]
    public TextMeshProUGUI deathLabel;

    [Tooltip("Botón de continuar")]
    public Button continueButton;

    [Tooltip("TextMeshProUGUI del botón")]
    public TextMeshProUGUI continueLabel;

    // ── Estado ────────────────────────────────────────────────
    private bool isShowing = false;
    public bool IsShowing => isShowing;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Empezar oculto
        SetAlpha(blackOverlay, 0f);
        if (deathLabel) { deathLabel.alpha = 0f; deathLabel.text = deathText; deathLabel.color = textColor; }
        if (continueLabel) continueLabel.text = continueText;
        if (continueButton)
        {
            SetAlpha(continueButton.GetComponent<CanvasGroup>(), 0f);
            continueButton.onClick.AddListener(OnContinue);
            EnsureCanvasGroup(continueButton.gameObject);
        }

        gameObject.SetActive(false);
    }

    // ── API pública ───────────────────────────────────────────
    public void Show()
    {
        if (isShowing) return;
        gameObject.SetActive(true);
        StartCoroutine(ShowSequence());
    }

    public void Hide()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
        isShowing = false;
    }

    // ── Secuencia ─────────────────────────────────────────────
    IEnumerator ShowSequence()
    {
        isShowing = true;

        // Pausar el juego
        Time.timeScale = 0f;

        // Reset visual
        SetAlpha(blackOverlay, 0f);
        if (deathLabel) deathLabel.alpha = 0f;
        EnsureCanvasGroup(continueButton?.gameObject);
        SetCanvasGroupAlpha(continueButton?.gameObject, 0f);

        // 1. Fade negro
        yield return StartCoroutine(FadeImage(blackOverlay, 0f, 1f, fadeDuration));

        // 2. Espera antes del texto
        yield return new WaitForSecondsRealtime(textDelay);

        // 3. Texto aparece lentamente
        if (deathLabel)
        {
            deathLabel.text = deathText;
            deathLabel.color = textColor;
            yield return StartCoroutine(FadeText(deathLabel, 0f, 1f, 0.8f));
        }

        // 4. Espera antes del botón
        yield return new WaitForSecondsRealtime(buttonDelay);

        // 5. Botón aparece
        if (continueButton)
            yield return StartCoroutine(FadeCanvasGroup(continueButton.gameObject, 0f, 1f, 0.5f));
    }

    // ── Botón continuar ───────────────────────────────────────
    void OnContinue()
    {
        Time.timeScale = 1f;
        isShowing = false; // DeathZone usa WaitUntil(!IsShowing)
        Hide();
        // El respawn lo gestiona quien llamó a Show():
        // - PlayerHealth.Die() -> hace su propio RespawnAfterDeath
        // - DeathZone -> espera IsShowing=false y hace el respawn
    }

    // ── Helpers de animación ──────────────────────────────────
    IEnumerator FadeImage(Image img, float from, float to, float duration)
    {
        if (img == null) yield break;
        float t = 0f;
        Color c = img.color;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(from, to, t / duration);
            img.color = c;
            yield return null;
        }
        c.a = to; img.color = c;
    }

    IEnumerator FadeText(TextMeshProUGUI tmp, float from, float to, float duration)
    {
        if (tmp == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            tmp.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        tmp.alpha = to;
    }

    IEnumerator FadeCanvasGroup(GameObject go, float from, float to, float duration)
    {
        if (go == null) yield break;
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        cg.alpha = to;
        cg.interactable = (to >= 1f);
        cg.blocksRaycasts = (to >= 1f);
    }

    void SetAlpha(Image img, float a)
    {
        if (img == null) return;
        Color c = img.color; c.a = a; img.color = c;
    }

    void SetAlpha(CanvasGroup cg, float a)
    {
        if (cg == null) return;
        cg.alpha = a;
    }

    void SetCanvasGroupAlpha(GameObject go, float a)
    {
        if (go == null) return;
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = a;
    }

    CanvasGroup EnsureCanvasGroup(GameObject go)
    {
        if (go == null) return null;
        CanvasGroup cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        cg.interactable = false;
        cg.blocksRaycasts = false;
        return cg;
    }
}