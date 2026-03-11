// ============================================================
// BossAttackMelee.cs — ATAQUE 0: Embestida simple
// Telegrafía amarilla → dash → comprueba golpe
// ============================================================

using UnityEngine;
using System.Collections;

public class BossAttackMelee : MonoBehaviour
{
    [Header("=== MELEE ===")]
    public Transform meleeAttackPoint;
    public Vector2   meleeAttackBoxSize = new Vector2(6f, 4f);
    public int       meleeAttackDamage  = 2;
    public float     meleeWindup        = 0.4f;
    public float     meleeDashForce     = 50f;
    public float     meleeKnockback     = 20f;

    private BossData       data;
    private Rigidbody2D    rb;
    private SpriteRenderer sr;
    private BossMovementAI movement;

    void Start()
    {
        data     = GetComponent<BossData>();
        rb       = GetComponent<Rigidbody2D>();
        sr       = GetComponent<SpriteRenderer>();
        movement = GetComponent<BossMovementAI>();

        if (meleeAttackPoint == null)
        {
            GameObject pt = new GameObject("MeleePt");
            pt.transform.SetParent(transform);
            pt.transform.localPosition = new Vector3(2f, 0, 0);
            meleeAttackPoint = pt.transform;
        }
    }

    public IEnumerator Execute()
    {
        if (sr) sr.color = Color.yellow;
        movement.FlipTowardsPlayer();
        yield return new WaitForSeconds(meleeWindup);

        if (sr) sr.color = Color.red;
        Vector2 dir = (data.player.position - transform.position).normalized;
        dir.y = 0;
        rb.AddForce(dir * meleeDashForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(0.25f);
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        int hits = Physics2D.OverlapBoxNonAlloc(meleeAttackPoint.position, meleeAttackBoxSize, 0f, data.hitBuffer);
        for (int i = 0; i < hits; i++)
        {
            if (data.hitBuffer[i].CompareTag("Player"))
            {
                data.playerMainChar?.TakeDamage(meleeAttackDamage);
                if (data.playerRb) data.playerRb.AddForce(dir * meleeKnockback, ForceMode2D.Impulse);
            }
        }
        yield return new WaitForSeconds(0.2f);
    }
}
