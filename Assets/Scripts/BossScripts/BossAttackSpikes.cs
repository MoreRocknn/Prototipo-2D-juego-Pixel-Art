// ============================================================
// BossAttackSpikes.cs — ATAQUES 2 y 5
//   ExecuteGround() → pinchos con predicción de movimiento
//   ExecuteCross()  → línea horizontal de 9 pinchos
// ============================================================

using UnityEngine;
using System.Collections;

public class BossAttackSpikes : MonoBehaviour
{
    [Header("=== PINCHOS ===")]
    public GameObject groundSpikePrefab;
    public int geysersCount = 6;
    public int spikeDamage = 1;

    [Header("=== POSICIÓN ===")]
    [Tooltip("Y del suelo donde aparecen los pinchos. Mira la Y del suelo en tu escena y ponla aquí.")]
    public float groundY = -3f;

    [Tooltip("Altura de spawn por encima del suelo (el GroundSpike hace el raycast desde aquí).")]
    public float spawnHeight = 10f;

    private BossData data;
    private SpriteRenderer sr;

    void Start()
    {
        data = GetComponent<BossData>();
        sr = GetComponent<SpriteRenderer>();
    }

    // ── ATAQUE 2: Pinchos con predicción ─────────────────────
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
            // Predicción: apuntar donde ESTARÁ el jugador
            if (data.playerRb != null && data.playerRb.linearVelocity.x != 0)
            {
                float pred = data.currentPhase == BossData.BossPhase.Phase3 ? 0.7f : 0.5f;
                x += data.playerRb.linearVelocity.x * pred;
            }
            x = Mathf.Clamp(x, data.minArenaX, data.maxArenaX);

            Spawn(new Vector3(x, groundY + spawnHeight, 0f));
            yield return new WaitForSeconds(0.15f);
        }
    }

    // ── ATAQUE 5: Línea horizontal de 9 pinchos ──────────────
    public IEnumerator ExecuteCross()
    {
        if (sr) sr.color = new Color(1f, 0f, 1f);
        yield return new WaitForSeconds(0.5f);
        if (groundSpikePrefab == null) yield break;

        float centerX = data.player.position.x;
        float spawnY = groundY + spawnHeight;

        for (int i = -4; i <= 4; i++)
        {
            float x = Mathf.Clamp(centerX + i * 2f, data.minArenaX, data.maxArenaX);
            Spawn(new Vector3(x, spawnY, 0f));
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