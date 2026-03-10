using UnityEngine;

/// <summary>
/// Gestiona la detección de paredes, wall slide y wall grab.
/// </summary>
[RequireComponent(typeof(PlayerState))]
public class PlayerWall : MonoBehaviour
{
    [Header("Detección")]
    public Transform wallCheck;
    public float checkRadius = 0.2f;
    public LayerMask wallLayer;

    [Header("Wall Slide")]
    public float wallSlideSpeed = 2.5f;

    [Header("Wall Grab")]
    public bool canWallGrab = true;
    public KeyCode wallGrabKey = KeyCode.LeftShift;
    public float wallGrabStaminaMax = 3f;
    private float wallGrabStamina;

    [Header("Detección de suelo")]
    public GroundCheck groundCheck;

    private PlayerState state;

    void Awake()
    {
        state = GetComponent<PlayerState>();
        wallGrabStamina = wallGrabStaminaMax;
    }

    /// <summary>Llamado desde PlayerCore.Update() antes de HandleWallMechanics</summary>
    public void UpdateWallChecks()
    {
        state.isTouchingWall = Physics2D.OverlapCircle(wallCheck.position, checkRadius, wallLayer);

        if (groundCheck.isGrounded)
        {
            state.wasWallJumping    = false;
            wallGrabStamina         = wallGrabStaminaMax;
            state.coyoteTimeCounter = GetComponent<PlayerJump>().coyoteTime;
        }

        if (state.wallJumpCounter > 0f)
            state.wallJumpCounter -= Time.deltaTime;
    }

    /// <summary>Llamado desde PlayerCore.Update()</summary>
    public void HandleWallMechanics()
    {
        bool isPushingWall = (state.moveInput * state.wallSide > 0);
        bool wantsToGrab   = canWallGrab && Input.GetKey(wallGrabKey);

        if (state.isTouchingWall && !groundCheck.isGrounded && wantsToGrab)
        {
            state.isWallGrabbing = true;
            state.isWallSliding  = false;

            if (wallGrabStaminaMax > 0)
            {
                wallGrabStamina -= Time.deltaTime;
                if (wallGrabStamina <= 0)
                    state.isWallGrabbing = false;
            }
        }
        else if (state.isTouchingWall && !groundCheck.isGrounded
                 && GetComponent<Rigidbody2D>().linearVelocity.y < 0f && isPushingWall)
        {
            state.isWallGrabbing = false;
            state.isWallSliding  = !Input.GetKey(KeyCode.DownArrow);
        }
        else
        {
            state.isWallGrabbing = false;
            state.isWallSliding  = false;
        }
    }

    void OnDrawGizmos()
    {
        if (wallCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(wallCheck.position, checkRadius);
        }
    }
}
