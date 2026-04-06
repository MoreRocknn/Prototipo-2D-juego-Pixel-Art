using UnityEngine;

/// <summary>
/// Gestiona toda la lógica de salto:
/// coyote time, jump buffer, variable jump height y límite de caída.
/// </summary>
[RequireComponent(typeof(PlayerState))]
public class PlayerJump : MonoBehaviour
{
    [Header("Animator")]
    private Animator anim;

    [Header("Salto")]
    public float jumpForce = 14f;
    public float jumpCutMultiplier = 0.5f;
    public float maxFallSpeed = 22f;

    [Header("Coyote Time y Jump Buffer")]
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.15f;

    [Header("Wall Jump")]
    public Vector2 wallJumpForce = new Vector2(13f, 17f);
    public float wallJumpControlTime = 0.25f;

    [Header("Detección de suelo")]
    public GroundCheck groundCheck;

    private PlayerState state;
    private Rigidbody2D rb;

    void Awake()
    {
        anim = GetComponent<Animator>();
        state = GetComponent<PlayerState>();
        rb    = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        HandleJumpInput();
    }

    private void HandleJumpInput()
    {
        // Coyote time
        if (groundCheck.isGrounded)
            state.coyoteTimeCounter = coyoteTime;
        else
            state.coyoteTimeCounter -= Time.deltaTime;

        // Jump buffer
        if (Input.GetKeyDown(KeyCode.Space))
        {
            state.jumpBufferCounter = jumpBufferTime;
            state.jumpReleased      = false;
        }
        else
        {
            state.jumpBufferCounter -= Time.deltaTime;
        }

        // Variable jump height: soltar Space corta el salto
        if (Input.GetKeyUp(KeyCode.Space))
        {
            state.jumpReleased = true;
            if (rb.linearVelocity.y > 0f)
                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x,
                    rb.linearVelocity.y * jumpCutMultiplier
                );

        }
        bool isGrounded = groundCheck.isGrounded;
        bool rising = rb.linearVelocity.y > 0.1f;
        bool falling = rb.linearVelocity.y < -0.1f && !isGrounded;

        // ✅ isJumping = true mientras esté en el aire (subiendo O bajando)
        bool onWall = state.isTouchingWall && !groundCheck.isGrounded;

        anim.SetBool("isWallSliding", onWall);
        anim.SetBool("isJumping", !isGrounded && !onWall);


    }

    /// <summary>Llamado desde PlayerCore.FixedUpdate()</summary>
    public void HandleJump()
    {
        if (state.jumpBufferCounter <= 0f || state.jumpReleased) return;

        var wall = GetComponent<PlayerWall>();

        // Wall jump
        if (state.isTouchingWall && !groundCheck.isGrounded && state.wallJumpCounter <= 0f)
        {
            bool pushingWall = (state.moveInput * state.wallSide > 0);
            float xForce = -state.wallSide * wallJumpForce.x
                           * (pushingWall || Mathf.Abs(state.moveInput) < 0.1f ? 0.7f : 1f);
            float yForce = wallJumpForce.y
                           * (pushingWall || Mathf.Abs(state.moveInput) < 0.1f ? 1f : 0.95f);

            rb.linearVelocity = new Vector2(xForce, yForce);

            state.wallJumpCounter    = wallJumpControlTime;
            state.wasWallJumping     = true;
            state.jumpBufferCounter  = 0f;
            state.coyoteTimeCounter  = 0f;
            state.isWallSliding      = false;
            state.isWallGrabbing     = false;
        }
        // Salto normal con coyote time
        else if (state.coyoteTimeCounter > 0f && !state.wasWallJumping)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            state.jumpBufferCounter = 0f;
            state.coyoteTimeCounter = 0f;
        }
        else
        {
            state.jumpBufferCounter = 0f;
        }
    }

    /// <summary>Llamado desde PlayerCore.FixedUpdate()</summary>
    public void LimitFallSpeed()
    {
        if (rb.linearVelocity.y < -maxFallSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
    }
}
