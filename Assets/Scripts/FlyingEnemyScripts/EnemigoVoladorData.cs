// ============================================================
// EnemigoVoladorData.cs — Estado compartido del enemigo volador
// Todos los componentes leen y escriben aquí
// ============================================================
using UnityEngine;

public class EnemigoVoladorData : MonoBehaviour
{
    [Header("=== SALUD ===")]
    public int vidaMaxima = 3;
    public float tiempoInvencibilidad = 0.25f;
    public Vector2 fuerzaKnockback = new Vector2(4f, 3f);

    [Header("=== BARRA DE VIDA ===")]
    public Vector3 offsetBarraVida = new Vector3(0, 2f, 0);
    public bool ocultarBarraVidaLlena = true;

    [Header("=== DETECCIÓN ===")]
    public float rangoDeteccion = 10f;
    public float rangoAtaque = 2.5f;
    public LayerMask capaJugador;
    public LayerMask capaPared;
    public Transform puntoDeteccion;

    [Header("=== MOVIMIENTO ===")]
    public float velocidadMovimiento = 2f;
    public float velocidadPersecucion = 3.5f;
    public float velocidadHuida = 5f;
    public float velocidadVertical = 2.5f;
    public float tiempoSuavizado = 0.3f;

    [Header("=== PATRULLA ===")]
    public Vector2 tamanoAreaPatrulla = new Vector2(8f, 4f);
    public float tiempoEsperaPatrulla = 2f;

    [Header("=== ATAQUE ===")]
    public Transform puntoAtaque;
    public float radioAtaque = 1f;
    public int danoAtaque = 1;
    public float velocidadAtaque = 12f;
    public float duracionAtaque = 0.25f;
    public float tiempoRecargaAtaque = 1.5f;
    public float tiempoGuardia = 0.8f;
    [Range(0.1f, 1f)] public float toleranciaAlineacion = 0.5f;

    [Header("=== VISUAL ===")]
    public Color colorGuardia = Color.yellow;
    public Color colorAtaque = Color.red;
    public Color colorDanado = Color.white;

    [Header("=== ANIMACIONES ===")]
    public string parametroVelocidad = "speed";
    public string parametroEstado = "state";

    // ── Estado en tiempo de ejecución (compartido entre componentes) ──
    public enum Estado { Inactivo, Patrulla, Alerta, Guardia, Perseguir, Atacar, Huir, Aturdido }
    [HideInInspector] public Estado estadoActual = Estado.Inactivo;

    [HideInInspector] public Transform jugador;
    [HideInInspector] public int vidaActual;
    [HideInInspector] public bool esInvencible;
    [HideInInspector] public bool estaAtacando;
    [HideInInspector] public bool mirandoDerecha = true;
    [HideInInspector] public float temporizadorAtaque;
    [HideInInspector] public float temporizadorGuardia;
    [HideInInspector] public float temporizadorEspera;
    [HideInInspector] public float limiteInferiorY;
    [HideInInspector] public float limiteSuperiorY;
    [HideInInspector] public float rangoDeteccionCuadrado;
    [HideInInspector] public float rangoAtaqueCuadrado;
    [HideInInspector] public Vector2 posicionInicial;
    [HideInInspector] public Vector2 objetivoPatrulla;
    [HideInInspector] public Vector2 velocidadSuavizado;
    [HideInInspector] public HealthBarUI healthBar;
    [HideInInspector] public Color colorOriginal;
}
