using UnityEngine;

/// <summary>
/// Contenedor de estado compartido entre todos los módulos del jugador.
/// Ningún módulo debe tener lógica aquí, solo datos.
/// </summary>
public class PlayerState : MonoBehaviour
{
    [Header("Estado de movimiento")]
    public float moveInput;
    public bool isFacingRight = true;
    public int wallSide = 1;

    [Header("Estado de pared y suelo")]
    public bool isTouchingWall;
    public bool isWallSliding;
    public bool isWallGrabbing;

    [Header("Estado de salto")]
    public float coyoteTimeCounter;
    public float jumpBufferCounter;
    public bool jumpReleased = true;
    public bool wasWallJumping;
    public float wallJumpCounter;

    [Header("Estado de acción")]
    public bool isDashing;
    public bool isAttacking;
    public bool isAttackingDown;
    public bool isInputLocked;

    [Header("Estado de combo")]
    public int currentComboStep;
    public float lastAttackTime = -999f;

    [Header("Estado de rebotes")]
    public int consecutiveBounces;
    public float lastBounceTime = -1f;

    [Header("Estado de daño")]
    public bool isDamageInvincible;
}
