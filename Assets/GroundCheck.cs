using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float checkRadius = 0.2f;
    public bool isGrounded;

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    // Update is called once per frame
    void Update()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);

    }

    void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, checkRadius);
        }
    }
}
