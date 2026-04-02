using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections;

// ============================================================
//  MenuAnimationController.cs
//  Controla todas las animaciones del menú usando USS transitions
//  y coroutines para secuencias de entrada/salida.
// ============================================================

public class MenuAnimationController : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────
    [Header("Tiempos (segundos)")]
    [SerializeField] private float introDelay      = 0.5f;
    [SerializeField] private float introStagger    = 0.12f;  // retardo entre cada botón
    [SerializeField] private float outroDuration   = 0.8f;
    [SerializeField] private float panelFadeDuration = 0.3f;

    // ── Clases USS ─────────────────────────────────────────────
    // Define estas clases en tu hoja de estilos MainMenu.uss
    private const string CLASS_HIDDEN      = "hidden";
    private const string CLASS_VISIBLE     = "visible";
    private const string CLASS_SLIDE_IN    = "slide-in";
    private const string CLASS_FADE_OUT    = "fade-out";
    private const string CLASS_FLICKER     = "flicker";

    // ── Referencias ───────────────────────────────────────────
    private UIDocument    _uiDoc;
    private VisualElement _root;
    private VisualElement _overlay;       // overlay negro para fade total
    private VisualElement _logo;
    private VisualElement _buttonList;

    // ── Ciclo de vida ─────────────────────────────────────────

    private void Awake()
    {
        _uiDoc      = GetComponent<UIDocument>();
        _root       = _uiDoc.rootVisualElement;
        _overlay    = _root.Q<VisualElement>("Overlay");
        _logo       = _root.Q<VisualElement>("Logo");
        _buttonList = _root.Q<VisualElement>("ButtonList");
    }

    // ── API pública ────────────────────────────────────────────

    /// <summary>Animación de entrada al abrir el juego.</summary>
    public void PlayIntro()
    {
        StartCoroutine(IntroRoutine());
    }

    /// <summary>Animación de salida antes de cambiar de escena.</summary>
    public void PlayOutro(Action onComplete)
    {
        StartCoroutine(OutroRoutine(onComplete));
    }

    /// <summary>Transición entre dos paneles (fade cruzado).</summary>
    public void TransitionToPanel(VisualElement from, VisualElement to)
    {
        StartCoroutine(PanelTransitionRoutine(from, to));
    }

    // ── Coroutines ─────────────────────────────────────────────

    private IEnumerator IntroRoutine()
    {
        // 1. Todo invisible al inicio
        _root.AddToClassList(CLASS_HIDDEN);
        yield return new WaitForSeconds(introDelay);

        // 2. Fade-in del overlay negro → desaparece
        _overlay?.AddToClassList("fade-in-overlay");
        yield return new WaitForSeconds(0.6f);
        _overlay?.RemoveFromClassList("fade-in-overlay");

        // 3. Logo aparece con efecto de parpadeo (flicker)
        _logo?.AddToClassList(CLASS_FLICKER);
        yield return new WaitForSeconds(0.8f);
        _logo?.RemoveFromClassList(CLASS_FLICKER);
        _logo?.AddToClassList(CLASS_VISIBLE);

        yield return new WaitForSeconds(0.4f);

        // 4. Botones aparecen escalonados de arriba a abajo
        var buttons = _buttonList?.Query<Button>().ToList();
        if (buttons != null)
        {
            foreach (var btn in buttons)
            {
                btn.AddToClassList(CLASS_SLIDE_IN);
                yield return new WaitForSeconds(introStagger);
            }
        }

        _root.RemoveFromClassList(CLASS_HIDDEN);
    }

    private IEnumerator OutroRoutine(Action onComplete)
    {
        // Fade a negro total
        _overlay?.AddToClassList("full-fade-out");
        yield return new WaitForSeconds(outroDuration);
        onComplete?.Invoke();
    }

    private IEnumerator PanelTransitionRoutine(VisualElement from, VisualElement to)
    {
        // Fade-out del panel actual
        from.AddToClassList(CLASS_FADE_OUT);
        yield return new WaitForSeconds(panelFadeDuration);
        from.style.display = DisplayStyle.None;
        from.RemoveFromClassList(CLASS_FADE_OUT);

        // Fade-in del nuevo panel
        to.style.display = DisplayStyle.Flex;
        to.AddToClassList(CLASS_SLIDE_IN);
        yield return new WaitForSeconds(panelFadeDuration);
        to.RemoveFromClassList(CLASS_SLIDE_IN);
    }

    // ── Efectos puntuales ──────────────────────────────────────

    /// <summary>
    /// Añade el efecto flicker a cualquier elemento (parpadeo tipo tubo fluorescente).
    /// Útil para textos de "PRESIONA CUALQUIER TECLA".
    /// </summary>
    public void ApplyFlicker(VisualElement element)
    {
        element.AddToClassList(CLASS_FLICKER);
    }
}
