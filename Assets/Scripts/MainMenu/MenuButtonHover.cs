using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// ============================================================
//  MenuButtonHover.cs
//  Añade este script a cada botón del menú.
//  Cambia el color del texto al hacer hover, como en Dark Souls.
//  No usa sprites de transición — solo cambia el color del TMP.
// ============================================================

[RequireComponent(typeof(Button))]
public class MenuButtonHover : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Colores")]
    [SerializeField] private Color colorNormal   = new Color(0.769f, 0.706f, 0.604f); // #C4B49A
    [SerializeField] private Color colorHover    = new Color(0.910f, 0.788f, 0.416f); // #E8C96A
    [SerializeField] private Color colorPressed  = Color.white;

    [Header("Velocidad de transición")]
    [SerializeField] private float transitionSpeed = 12f;

    // ── Privados ──────────────────────────────────────────────
    private TextMeshProUGUI _tmp;
    private Color           _targetColor;
    private Button          _button;

    private void Awake()
    {
        _tmp    = GetComponentInChildren<TextMeshProUGUI>();
        _button = GetComponent<Button>();

        // Desactiva la transición visual de Unity (no queremos el color azul por defecto)
        _button.transition = Selectable.Transition.None;

        _targetColor = colorNormal;

        if (_tmp != null)
            _tmp.color = colorNormal;
    }

    private void Update()
    {
        // Transición suave de color
        if (_tmp != null)
            _tmp.color = Color.Lerp(_tmp.color, _targetColor, Time.deltaTime * transitionSpeed);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_button.interactable) return;
        _targetColor = colorHover;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _targetColor = colorNormal;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_button.interactable) return;
        _tmp.color   = colorPressed;
        _targetColor = colorNormal;
    }

    // Llamado externamente si el botón se deshabilita
    private void OnDisable()
    {
        _targetColor = colorNormal;
        if (_tmp != null)
            _tmp.color = new Color(colorNormal.r, colorNormal.g, colorNormal.b, 0.3f);
    }
}
