// ============================================================
// EnemigoVoladorAnimator.cs — Control del Animator
//
// ESTADOS (Integer "state"):
//   0 = Idle (sin animación asignada, puede dejarse vacío)
//   1 = Alerta / Perseguir
//   2 = Ataque (dash)
//   3 = Aturdido / Daño
//   4 = Muerte
//
// SETUP EN UNITY:
//   1. Añade un Animator al GameObject del enemigo volador
//   2. Crea un AnimatorController y asígnalo
//   3. Añade el parámetro Integer llamado "state" (o el nombre
//      que tengas en EnemigoVoladorData.parametroEstado)
//   4. Crea los estados en el Animator y conecta las transiciones:
//      - Any State → cada estado con condición "state == N"
//      - Desactiva "Can Transition To Self" para evitar reinicios
//   5. Asigna cada clip de animación a su estado
//
// CLIPS NECESARIOS:
//   - Alerta:    loop ON  — vuelo rápido hacia el jugador
//   - Ataque:    loop OFF — animación de dash/embestida
//   - Aturdido:  loop OFF — parpadeo o tambaleo al recibir daño
//   - Muerte:    loop OFF — caída o desintegración
// ============================================================
using UnityEngine;

public class EnemigoVoladorAnimator : MonoBehaviour
{
    // Nombres de los parámetros en el Animator (deben coincidir exactamente)
    // Si los cambias aquí, cámbialos también en el AnimatorController de Unity
    [Header("=== PARÁMETROS DEL ANIMATOR ===")]
    [Tooltip("Nombre del Integer que controla el estado. Debe existir en el AnimatorController.")]
    public string paramEstado    = "state";

    [Tooltip("Nombre del Float de velocidad (opcional, para blend trees).")]
    public string paramVelocidad = "speed";

    // Valores del Integer para cada estado
    // Cambia estos números si los tienes distintos en tu AnimatorController
    [Header("=== VALORES DE ESTADO ===")]
    public int valorIdle      = 0;
    public int valorAlerta    = 1;  // Alerta + Perseguir comparten animación
    public int valorAtaque    = 2;
    public int valorAturdido  = 3;
    public int valorMuerte    = 4;

    private EnemigoVoladorData d;
    private Rigidbody2D        rb;
    private Animator           anim;
    private int                estadoAnterior = -1;

    void Awake()
    {
        d    = GetComponent<EnemigoVoladorData>();
        rb   = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();

        if (anim == null)
            Debug.LogWarning("[EnemigoVoladorAnimator] No hay Animator en este GameObject. " +
                             "Añade un Animator y asígnale un AnimatorController.");
    }

    // Llamado cada frame desde EnemigoVoladorAI.ActualizarAnimacion()
    public void Actualizar()
    {
        if (anim == null) return;

        // Float de velocidad (útil para blend trees)
        anim.SetFloat(paramVelocidad, rb.linearVelocity.magnitude);

        // Calcular qué estado corresponde
        int nuevoEstado = EstadoActualAValor();

        // Solo cambiar si el estado es diferente (evita reiniciar animaciones)
        if (nuevoEstado != estadoAnterior)
        {
            anim.SetInteger(paramEstado, nuevoEstado);
            estadoAnterior = nuevoEstado;
        }
    }

    // Llamado desde EnemigoVoladorHealth al morir
    // (la muerte necesita forzarse aunque sea el mismo estado)
    public void PlayMuerte()
    {
        if (anim == null) return;
        anim.SetInteger(paramEstado, valorMuerte);
        estadoAnterior = valorMuerte;
    }

    int EstadoActualAValor()
    {
        return d.estadoActual switch
        {
            EnemigoVoladorData.Estado.Alerta    => valorAlerta,
            EnemigoVoladorData.Estado.Perseguir => valorAlerta,   // misma anim que alerta
            EnemigoVoladorData.Estado.Atacar    => valorAtaque,
            EnemigoVoladorData.Estado.Aturdido  => valorAturdido,
            _ => valorIdle
        };
    }
}
