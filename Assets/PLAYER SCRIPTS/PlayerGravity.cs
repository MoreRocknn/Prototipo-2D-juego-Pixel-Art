using UnityEngine;

/// <summary>
/// Controla la escala de gravedad del jugador según su estado actual,
/// simulando el feel de Hollow Knight (caída pesada, salto variable).
/// </summary>
[RequireComponent(typeof(PlayerState))]
public class PlayerGravity : MonoBehaviour
{
    [Header("Multiplicadores de gravedad")]
    public float fallGravityMultiplier     = 2.5f;
    public float lowJumpMultiplier         = 2.5f;
    public float wallSlideGravityMultiplier = 0.3f;

    private PlayerState state;
    private Rigidbody2D rb;
    private float defaultGravityScale;

    void Awake()
    {
        state = GetComponent<PlayerState>();
        rb    = GetComponent<Rigidbody2D>();
    }

    void Start()
    {
        defaultGravityScale = rb.gravityScale;
    }

    /// <summary>Llamado desde PlayerCore.Update()</summary>
    public void HandleGravity()
    {
        if (state.isDashing)
        {
            rb.gravityScale = 0f;
            return;
        }

        if (state.isWallGrabbing)
            rb.gravityScale = 0f;
        else if (state.isWallSliding)
            rb.gravityScale = defaultGravityScale * wallSlideGravityMultiplier;
        else if (rb.linearVelocity.y < -0.5f)
            rb.gravityScale = defaultGravityScale * fallGravityMultiplier;
        else if (rb.linearVelocity.y > 0.5f && !Input.GetKey(KeyCode.Space))
            rb.gravityScale = defaultGravityScale * lowJumpMultiplier;
        else
            rb.gravityScale = defaultGravityScale;
    }

    public float GetDefaultGravityScale() => defaultGravityScale;
}
