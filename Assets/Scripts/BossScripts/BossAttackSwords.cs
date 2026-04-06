using UnityEngine;
using System.Collections;

public class BossAttackSwords : MonoBehaviour
{
    public GameObject fallingSwordPrefab;
    public int swordsCount = 10;
    public float swordSpeed = 22f;
    public float spawnSkyY = 20f;
    public LayerMask groundLayer;

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
        yield return new WaitForSeconds(0.5f);

        if (fallingSwordPrefab == null)
        {
            Debug.LogError("¡NO HAY PREFAB DE ESPADA ASIGNADO!");
            yield break;
        }

        for (int i = 0; i < swordsCount; i++)
        {
            if (data.player == null) break;

            float x = data.player.position.x + Random.Range(-5f, 5f);
            x = Mathf.Clamp(x, data.minArenaX, data.maxArenaX);

            // EJECUCIÓN DIRECTA: Si el raycast falla, la espada sale igual
            StartCoroutine(SpawnSwordSequence(x));
            yield return new WaitForSeconds(0.15f);
        }

        if (sr) sr.color = Color.white;
    }

    IEnumerator SpawnSwordSequence(float x)
    {
        // Forzamos un valor por defecto para que el código NO se detenga
        float groundY = data.player.position.y;

        // Raycast de depuración para ver qué está tocando el jefe
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, data.player.position.y + 5f), Vector2.down, 20f, groundLayer);

        if (hit.collider != null)
        {
            groundY = hit.point.y;
        }
        else
        {
            Debug.LogWarning("Raycast de espada no tocó suelo en X: " + x + ". Revisa el LayerMask.");
        }

        // Crear aviso
        GameObject warning = CreateWarning(new Vector3(x, groundY, 0));
        yield return new WaitForSeconds(0.6f);

        // INSTANCIAR: Asegúrate de que la Z sea 0
        Vector3 spawnPos = new Vector3(x, spawnSkyY, 0f);
        GameObject sword = Instantiate(fallingSwordPrefab, spawnPos, Quaternion.identity);

        FallingSword fs = sword.GetComponent<FallingSword>();
        if (fs != null)
        {
            fs.Initialize(swordSpeed, 1, groundY);
        }

        if (warning) Destroy(warning);
    }

    GameObject CreateWarning(Vector3 pos)
    {
        GameObject w = new GameObject("SwordWarning");
        w.transform.position = pos;
        SpriteRenderer wr = w.AddComponent<SpriteRenderer>();
        wr.sprite = MakeCircleSprite();
        wr.color = new Color(1f, 0f, 0f, 0.6f);
        return w;
    }

    Sprite MakeCircleSprite()
    {
        int res = 32;
        Texture2D tex = new Texture2D(res, res);
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
                tex.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), new Vector2(16, 16)) < 16 ? Color.red : Color.clear);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
    }
}