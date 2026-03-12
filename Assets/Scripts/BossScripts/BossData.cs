// ============================================================
// BossData.cs
// RESPONSABILIDAD: Estado compartido entre todos los componentes.
//
// ¿Por qué existe este archivo?
// Cuando tienes 5 scripts separados en el mismo GameObject,
// necesitan una forma de compartir información entre ellos.
// Por ejemplo: BossMovementAI necesita saber si el boss
// está atacando (dato de BossAttackSystem) para dejar de moverse.
//
// BossData actúa como "pizarra compartida": cualquier
// componente puede leer o escribir en ella.
// ============================================================

using UnityEngine;

public class BossData : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    // ESTADO DEL BOSS
    // Estos bools son leídos por TODOS los componentes del boss.
    // ─────────────────────────────────────────────────────────

    // ¿Está muerto? → todos los componentes paran de funcionar
    [HideInInspector] public bool isDead = false;

    // ¿Está ejecutando un ataque? → el movimiento se detiene
    [HideInInspector] public bool isAttacking = false;

    // ¿Está teletransportándose? → nada más puede ocurrir
    [HideInInspector] public bool isTeleporting = false;

    // ¿Es invulnerable? → ocurre durante el ataque definitivo
    [HideInInspector] public bool isInvulnerable = false;

    // ¿Están las puertas cerradas?
    [HideInInspector] public bool arenaSealed = false;

    // ¿Ya se activó la barra de vida en la UI?
    [HideInInspector] public bool healthBarActivated = false;

    // ─────────────────────────────────────────────────────────
    // REFERENCIAS AL JUGADOR
    // Se cachean UNA vez en BossController.Start() y se
    // comparten con todos los demás componentes desde aquí.
    // ─────────────────────────────────────────────────────────
    [HideInInspector] public Transform player;
    [HideInInspector] public PlayerCore playerMainChar;
    [HideInInspector] public Rigidbody2D playerRb;

    // ─────────────────────────────────────────────────────────
    // LÍMITES DEL ESCENARIO
    // Calculados una vez en BossController y usados por
    // MovementAI (para circular) y AttackSystem (para spawnear
    // ataques dentro de la arena).
    // ─────────────────────────────────────────────────────────
    [HideInInspector] public float minArenaX;
    [HideInInspector] public float maxArenaX;
    [HideInInspector] public Vector3 initialPosition;
    [HideInInspector] public float defaultGravity;

    // ─────────────────────────────────────────────────────────
    // FASE ACTUAL
    // Actualizada cada frame por BossController según la vida.
    // Leída por AttackSystem para elegir qué ataque usar.
    // ─────────────────────────────────────────────────────────
    public enum BossPhase { Phase1, Phase2, Phase3 }
    [HideInInspector] public BossPhase currentPhase = BossPhase.Phase1;

    // ─────────────────────────────────────────────────────────
    // BUFFER DE COLISIONES REUTILIZABLE
    // OPTIMIZACIÓN: En vez de crear un array nuevo cada vez que
    // se detectan colisiones (costoso para el Garbage Collector),
    // todos los componentes comparten y reusan este mismo array.
    // ─────────────────────────────────────────────────────────
    [HideInInspector] public Collider2D[] hitBuffer = new Collider2D[10];
}