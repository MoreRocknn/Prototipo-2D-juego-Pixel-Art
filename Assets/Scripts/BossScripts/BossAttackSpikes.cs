using UnityEngine;
using System.Collections;

public class BossAttackSpikes : MonoBehaviour
{
    [Header("=== PINCHOS ===")]
    public GameObject groundSpikePrefab;
    public int geysersCount = 6;
    public int spikeDamage = 1;

    [Header("=== DETECCIÓN DE SUELO ===")]
    public LayerMask groundLayer;
    public float raycastMaxDistance = 10f;

    private BossData data;
    private SpriteRenderer sr;

    void Awake()
    {
        data = GetComponent<BossData>();
        sr = GetComponent<SpriteRenderer>();
    }


    float? GetGroundY(float x)
    {

        Vector2 origin = new Vector2(x, data.player.position.y + 2f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, raycastMaxDistance, groundLayer);

        if (hit.collider != null)
        {
            return hit.point.y;
        }
        return null;
    }

    public IEnumerator ExecuteGround()
    {
        if (sr) sr.color = new Color(0.5f, 0f, 0f);
        yield return new WaitForSeconds(0.5f);
        if (groundSpikePrefab == null) yield break;

        int total = data.currentPhase == BossData.BossPhase.Phase3 ? geysersCount + 3 : geysersCount;
        for (int i = 0; i < total; i++)
        {
            if (data.player == null) break;

            float x = data.player.position.x;
            if (data.playerRb != null && data.playerRb.linearVelocity.x != 0)
            {
                float pred = data.currentPhase == BossData.BossPhase.Phase3 ? 0.7f : 0.5f;
                x += data.playerRb.linearVelocity.x * pred;
            }
            x = Mathf.Clamp(x, data.minArenaX, data.maxArenaX);

            float? groundY = GetGroundY(x);
            if (groundY.HasValue)
                Spawn(new Vector3(x, groundY.Value, 0f));

            yield return new WaitForSeconds(0.15f);
        }
    }

    public IEnumerator ExecuteCross()
    {
        if (sr) sr.color = new Color(1f, 0f, 1f);
        yield return new WaitForSeconds(0.5f);
        if (groundSpikePrefab == null) yield break;

        float centerX = data.player.position.x;

        for (int i = -4; i <= 4; i++)
        {
            float x = Mathf.Clamp(centerX + i * 2f, data.minArenaX, data.maxArenaX);
            float? groundY = GetGroundY(x);
            if (groundY.HasValue)
                Spawn(new Vector3(x, groundY.Value, 0f));

            yield return new WaitForSeconds(0.08f);
        }
    }

    void Spawn(Vector3 pos)
    {
        GameObject spike = Instantiate(groundSpikePrefab, pos, Quaternion.identity);
        GroundSpike gs = spike.GetComponent<GroundSpike>() ?? spike.AddComponent<GroundSpike>();
        gs.Initialize(spikeDamage);
    }
}