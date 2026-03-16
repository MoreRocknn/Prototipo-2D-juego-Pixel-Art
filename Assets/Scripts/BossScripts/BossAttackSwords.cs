// ============================================================
// BossAttackSwords.cs — ATAQUE 1: Lluvia de espadas
// Marcador en el suelo → espada cae desde arriba
// ============================================================

using UnityEngine;
using System.Collections;

public class BossAttackSwords : MonoBehaviour
{
    [Header("=== ESPADAS ===")]
    public GameObject fallingSwordPrefab;
    public int swordsCount = 10;
    public float swordSpeed = 22f;

    [Header("=== POSICIÓN ===")]
    [Tooltip("Y del suelo donde aparece el marcador. Mira la Y del suelo en tu escena y ponla aquí.")]
    public float groundY = -3f;

    [Tooltip("Altura desde la que caen las espadas (por encima de groundY).")]
    public float spawnHeight = 10f;

    private BossData data;
    private SpriteRenderer sr;

    void Awake()
    {
        data = GetComponent<BossData>();
        sr = GetComponent<SpriteRenderer>();
    }

    public IEnumerator Execute()
    {
        if (sr) sr.color = Color.cyan;
        yield return new WaitForSeconds(0.3f);
        if (fallingSwordPrefab == null) yield break;

        int total = data.currentPhase == BossData.BossPhase.Phase3 ? swordsCount + 5 : swordsCount;
        for (int i = 0; i < total; i++)
        {
            if (data.player == null) break;
            float x = data.player.position.x + (i > 0 ? Random.Range(-4f, 4f) : 0f);
            x = Mathf.Clamp(x, data.minArenaX, data.maxArenaX);
            StartCoroutine(SpawnSword(x));
            yield return new WaitForSeconds(0.12f);
        }
        if (sr) sr.color = Color.white;
    }

    IEnumerator SpawnSword(float x)
    {
        // Raycast desde muy arriba solo contra el layer Ground
        float realGroundY = groundY;
        int groundMask = LayerMask.GetMask("Ground");
        if (groundMask != 0)
        {
            RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, 50f), Vector2.down, 100f, groundMask);
            if (hit.collider != null)
            {
                realGroundY = hit.point.y;
                Debug.Log("[Sword] Suelo detectado en Y=" + realGroundY + " objeto=" + hit.collider.name);
            }
            else
                Debug.LogWarning("[Sword] Raycast no encontró suelo en X=" + x + " usando groundY=" + groundY);
        }
        else
            Debug.LogWarning("[Sword] Layer Ground no encontrado, usando groundY=" + groundY);

        // Marcador en el suelo real
        Vector3 groundPos = new Vector3(x, realGroundY, 0f);
        GameObject warning = CreateWarning(groundPos);

        yield return new WaitForSeconds(0.6f);

        // Espada cae desde arriba
        Vector3 spawnPos = new Vector3(x, realGroundY + spawnHeight, 0f);
        GameObject sword = Instantiate(fallingSwordPrefab, spawnPos, Quaternion.identity);
        FallingSword fs = sword.GetComponent<FallingSword>() ?? sword.AddComponent<FallingSword>();
        fs.Initialize(swordSpeed, 1, realGroundY);

        if (warning) Destroy(warning);
    }

    GameObject CreateWarning(Vector3 pos)
    {
        GameObject w = new GameObject("SwordWarning");
        w.transform.position = pos;
        SpriteRenderer sr = w.AddComponent<SpriteRenderer>();
        sr.sprite = MakeCircleSprite();
        sr.color = new Color(1f, 0f, 0f, 0.6f);
        sr.sortingOrder = 5;
        StartCoroutine(Blink(sr));
        return w;
    }

    IEnumerator Blink(SpriteRenderer sr)
    {
        for (int i = 0; i < 6; i++)
        {
            if (sr == null) yield break;
            sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(0.1f);
        }
        if (sr) sr.enabled = true;
    }

    Sprite MakeCircleSprite()
    {
        int res = 64; float r = res / 2f;
        Texture2D tex = new Texture2D(res, res);
        Vector2 c = new Vector2(r, r);
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c);
                tex.SetPixel(x, y, (d < r && d > r - 4f) ? Color.red : Color.clear);
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
    }
}