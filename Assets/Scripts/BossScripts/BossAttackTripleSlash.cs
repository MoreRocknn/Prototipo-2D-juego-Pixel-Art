// ============================================================
// BossAttackTripleSlash.cs — ATAQUE 4: Triple slash
// 3 dashes rápidos seguidos, menos daño por golpe
// ============================================================

using UnityEngine;
using System.Collections;

public class BossAttackTripleSlash : MonoBehaviour
{
    [Header("=== TRIPLE SLASH ===")]
    public Transform meleeAttackPoint;
    public Vector2 meleeAttackBoxSize = new Vector2(6f, 4f);
    public float dashForce = 35f;
    public float knockback = 10f;

    private BossData data;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private BossMovementAI movement;

    void Start()
    {
        data = GetComponent<BossData>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        movement = GetComponent<BossMovementAI>();

        // Reutilizar el punto de melee si existe
        if (meleeAttackPoint == null)
            meleeAttackPoint = transform.Find("MeleePt");
    }

    public IEnumerator Execute()
    {
        for (int i = 0; i < 3; i++)
        {
            if (sr) sr.color = new Color(1f, 0.5f, 0f);
            movement.FlipTowardsPlayer();
            yield return new WaitForSeconds(0.2f);

            if (sr) sr.color = Color.red;
            Vector2 dir = (data.player.position - transform.position).normalized;
            dir.y = 0;
            rb.AddForce(dir * dashForce, ForceMode2D.Impulse);

            yield return new WaitForSeconds(0.15f);
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            if (meleeAttackPoint != null)
            {
                int hits = Physics2D.OverlapBoxNonAlloc(meleeAttackPoint.position, meleeAttackBoxSize, 0f, data.hitBuffer);
                for (int h = 0; h < hits; h++)
                {
                    if (data.hitBuffer[h].CompareTag("Player"))
                    {
                        data.playerMainChar?.TakeDamage(1);
                        if (data.playerRb) data.playerRb.AddForce(dir * knockback, ForceMode2D.Impulse);
                    }
                }
            }
            yield return new WaitForSeconds(0.15f);
        }
    }
}