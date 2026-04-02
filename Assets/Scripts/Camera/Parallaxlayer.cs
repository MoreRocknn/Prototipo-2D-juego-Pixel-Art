using UnityEngine;

// ============================================================
//  ParallaxLayer.cs  —  v2
//
//  FIXES:
//  1. Loop correcto: duplica el sprite y hace swap cuando sale
//  2. Factor 0 = fondo completamente estático
//  3. Compatible con Cinemachine — lee posición de la cámara
//     directamente, no depende de sorting layers de Cinemachine
//
//  SETUP por capa:
//  · Añade este script al GameObject que tiene el SpriteRenderer
//  · Asigna el mismo sprite en "spriteRenderer" del Inspector
//  · Ajusta parallaxFactorX (0 = estático, 1 = se mueve mucho)
// ============================================================

[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxLayer : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("0 = completamente estático.\n0.1-0.3 = movimiento sutil (capas lejanas).\n0.5-0.8 = movimiento notable (capas cercanas).")]
    [Range(0f, 1f)]
    public float parallaxFactorX = 0f;

    [Range(0f, 1f)]
    public float parallaxFactorY = 0f;

    [Header("Loop")]
    [Tooltip("Activa para que el sprite se repita en bucle horizontalmente.")]
    public bool loopHorizontal = true;

    // ── Privados ──────────────────────────────────────────────
    private Transform _cam;
    private Vector3 _startPos;       // posición inicial de esta capa
    private float _spriteWidth;    // ancho del sprite en unidades mundo
    private SpriteRenderer _sr;

    // ── Ciclo de vida ─────────────────────────────────────────

    private void Awake()
    {
        // Usa Camera.main — funciona con y sin Cinemachine
        // Cinemachine mueve la cámara real, Camera.main siempre apunta a ella
        _cam = Camera.main.transform;
        _sr = GetComponent<SpriteRenderer>();

        _startPos = transform.position;
        _spriteWidth = _sr.bounds.size.x;
    }

    private void LateUpdate()
    {
        // ── Parallax ───────────────────────────────────────────
        // Desplazamiento relativo a la posición inicial de la cámara
        // parallaxFactor 0 → posición fija (estático)
        // parallaxFactor 1 → se mueve igual que la cámara
        float camOffsetX = _cam.position.x * parallaxFactorX;
        float camOffsetY = _cam.position.y * parallaxFactorY;

        transform.position = new Vector3(
            _startPos.x + camOffsetX,
            _startPos.y + camOffsetY,
            transform.position.z      // mantiene Z para el sorting
        );

        // ── Loop horizontal ────────────────────────────────────
        if (!loopHorizontal || _spriteWidth <= 0f) return;

        // Distancia entre la cámara y el centro de este sprite
        float dist = _cam.position.x - transform.position.x;

        // Si la cámara se alejó más de un ancho de sprite, hacemos jump
        if (dist > _spriteWidth)
            _startPos.x += _spriteWidth;
        else if (dist < -_spriteWidth)
            _startPos.x -= _spriteWidth;
    }
}