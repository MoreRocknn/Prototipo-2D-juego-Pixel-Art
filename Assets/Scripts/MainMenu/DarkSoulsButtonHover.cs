using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;

// ============================================================
//  DarkSoulsButtonHover.cs
//  Estilo Dark Souls 3 / tu menú de referencia:
//  - Texto crema en reposo
//  - Texto dorado brillante en hover con transición suave
//  - Barra izquierda dorada que aparece en hover
//  - Sin fondo sólido en el botón
// ============================================================
[RequireComponent(typeof(Button))]
public class DarkSoulsButtonHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("── Colores del texto ──────────────────────")]
    [SerializeField] private Color colorNormal = new Color(0.78f, 0.75f, 0.67f, 1f); // crema
    [SerializeField] private Color colorHover = new Color(0.91f, 0.78f, 0.42f, 1f); // dorado
    [SerializeField] private Color colorPressed = Color.white;

    [Header("── Barra lateral izquierda ─────────────────")]
    [SerializeField] private Image leftBar;          // Image hijo opcional
    [SerializeField] private Color barColor = new Color(0.91f, 0.78f, 0.42f, 1f);
    [SerializeField] private float barFadeSpeed = 10f;

    [Header("── Transición ──────────────────────────────")]
    [SerializeField] private float transitionSpeed = 8f;

    // ── Privados ──────────────────────────────────────────────
    private TextMeshProUGUI _tmp;
    private Button _btn;
    private Color _targetColor;
    private float _targetBarAlpha = 0f;

    private void Awake()
    {
        _tmp = GetComponentInChildren<TextMeshProUGUI>();
        _btn = GetComponent<Button>();
        _btn.transition = Selectable.Transition.None;

        _targetColor = colorNormal;
        if (_tmp != null) _tmp.color = colorNormal;

        // Barra: empieza invisible
        if (leftBar != null)
        {
            var c = barColor;
            c.a = 0f;
            leftBar.color = c;
        }
    }

    private void Update()
    {
        // Texto
        if (_tmp != null)
            _tmp.color = Color.Lerp(_tmp.color, _targetColor, Time.deltaTime * transitionSpeed);

        // Barra lateral
        if (leftBar != null)
        {
            var c = leftBar.color;
            c.a = Mathf.Lerp(c.a, _targetBarAlpha, Time.deltaTime * barFadeSpeed);
            leftBar.color = c;
        }
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (!_btn.interactable) return;
        _targetColor = colorHover;
        _targetBarAlpha = 1f;
    }

    public void OnPointerExit(PointerEventData e)
    {
        _targetColor = colorNormal;
        _targetBarAlpha = 0f;
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (!_btn.interactable) return;
        if (_tmp != null) _tmp.color = colorPressed;
        _targetColor = colorNormal;
    }

    private void OnDisable()
    {
        _targetColor = colorNormal;
        _targetBarAlpha = 0f;
        if (_tmp != null)
        {
            var c = colorNormal;
            c.a = 0.35f;
            _tmp.color = c;
        }
        if (leftBar != null)
        {
            var c = leftBar.color;
            c.a = 0f;
            leftBar.color = c;
        }
    }
}