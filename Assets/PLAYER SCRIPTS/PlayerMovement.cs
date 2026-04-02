using UnityEngine;

/// <summary>
/// Gestiona el movimiento horizontal del jugador, lectura de input
/// y el flip de sprite según la dirección.
/// </summary>
[RequireComponent(typeof(PlayerState))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Animator")]
    private Animator anim;



    [Header("Movimiento")]
    public float moveSpeed = 8f;
    public float airControlMultiplier = 1f;
    public float wallJumpAirDrag = 0.92f;
    public float wallJumpLockTime = 0.15f;

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

    /// <summary>Llamado desde PlayerCore.Update()</summary>
    public void HandleInput()
    {
        if (state.isAttacking)
        {
            state.moveInput = 0;
        }
        else
        {
            state.moveInput = (Input.GetKey(KeyCode.RightArrow) ? 1f : 0f)
                            - (Input.GetKey(KeyCode.LeftArrow)  ? 1f : 0f);


            if (state.moveInput != 0)
            {
                anim.SetBool("isWalking", true);
            }
            else
            {
                anim.SetBool("isWalking", false);
            }
        }
    }

    /// <summary>Llamado desde PlayerCore.FixedUpdate()</summary>
    public void HandleMovement()
    {
        if (state.isWallSliding)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                Mathf.Max(rb.linearVelocity.y, -GetComponent<PlayerWall>().wallSlideSpeed)
            );
        }

        if (state.wallJumpCounter > 0f)
        {
            if (state.wallJumpCounter > wallJumpLockTime) return;

            float controlAmount = 1f - (state.wallJumpCounter / wallJumpLockTime);
            float targetX = state.moveInput * moveSpeed * controlAmount;
            rb.linearVelocity = new Vector2(
                Mathf.Lerp(rb.linearVelocity.x, targetX, wallJumpAirDrag),
                rb.linearVelocity.y
            );
        }
        else if (!state.isWallSliding && !state.isAttacking)
        {
            float targetX  = state.moveInput * moveSpeed;
            float appliedX = groundCheck.isGrounded ? targetX : targetX * airControlMultiplier;
            rb.linearVelocity = new Vector2(appliedX, rb.linearVelocity.y);
        }
        else if (state.isAttacking && groundCheck.isGrounded)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    /// <summary>Llamado desde PlayerCore.Update()</summary>
    public void HandleFlip()
    {
        if (state.isDashing)              return;
        if (state.isAttacking)            return;
        if (state.wallJumpCounter > 0.05f) return;

        if (state.moveInput < 0 && state.isFacingRight)
            Flip();
        else if (state.moveInput > 0 && !state.isFacingRight)
            Flip();
    }

    private void Flip()
    {
        state.isFacingRight = !state.isFacingRight;
        state.wallSide      *= -1;
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;
    }
}
