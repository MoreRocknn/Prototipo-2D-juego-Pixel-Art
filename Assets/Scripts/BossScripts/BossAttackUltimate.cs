// ============================================================
// BossAttackUltimate.cs — ATAQUE 3: Definitivo
// Sube al centro → dispara 360° → baja
// ============================================================

using UnityEngine;
using System.Collections;

public class BossAttackUltimate : MonoBehaviour
{
    [Header("=== DEFINITIVO ===")]
    public int   bloodProjectiles = 20;
    public float bloodSpeed       = 11f;

    private BossData       data;
    private Rigidbody2D    rb;
    private SpriteRenderer sr;
    private Sprite         bloodSprite;

    void Start()
    {
        data = GetComponent<BossData>();
        rb   = GetComponent<Rigidbody2D>();
        sr   = GetComponent<SpriteRenderer>();
        GenerateBloodSprite();
    }

    public IEnumerator Execute()
    {
        data.isInvulnerable = true;

        // Subir al centro de la arena
        float t = 0f;
        Vector3 start  = transform.position;
        Vector3 center = new Vector3((data.minArenaX + data.maxArenaX) / 2f, data.initialPosition.y + 3f, 0);
        rb.gravityScale   = 0;
        rb.linearVelocity = Vector2.zero;

        while (t < 1f)
        {
            transform.position = Vector3.Lerp(start, center, t);
            t += Time.deltaTime * 2f;
            yield return null;
        }

        // Oleadas en 360°
        int waves = data.currentPhase == BossData.BossPhase.Phase3 ? 4 : 3;
        for (int w = 0; w < waves; w++)
        {
            for (int i = 0; i < bloodProjectiles; i++)
            {
                float angle = i * (360f / bloodProjectiles) + (w * 15f);
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                GameObject p  = CreateProjectile(transform.position);
                BossProjectile bp = p.AddComponent<BossProjectile>();
                bp.Initialize(dir, bloodSpeed, 1, true);
            }
            yield return new WaitForSeconds(0.4f);
        }

        // Bajar al suelo
        rb.gravityScale = data.defaultGravity;
        while (transform.position.y > data.initialPosition.y + 0.5f)
        {
            transform.position += Vector3.down * Time.deltaTime * 6f;
            yield return null;
        }

        data.isInvulnerable = false;
    }

    GameObject CreateProjectile(Vector3 pos)
    {
        GameObject go = new GameObject("BloodProjectile");
        go.transform.position = pos;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = bloodSprite; sr.color = Color.red; sr.sortingOrder = 10;
        return go;
    }

    void GenerateBloodSprite()
    {
        int res = 32;
        Texture2D tex = new Texture2D(res, res);
        Color[] px = new Color[res * res];
        for (int i = 0; i < px.Length; i++) px[i] = Color.red;
        tex.SetPixels(px); tex.Apply();
        bloodSprite = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
    }
}
