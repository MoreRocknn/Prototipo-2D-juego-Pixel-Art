using UnityEngine;
using System.Collections;

public class GroundSpike : MonoBehaviour
{
    [Header("=== ANIMACIONES ===")]
    public string warningAnimName = "SpikeWarning";
    public string emergeAnimName = "SpikeEmerge";
    public string sinkAnimName = "SpikeSink";

    [Header("=== TIEMPOS ===")]
    public float warningDuration = 1.0f;
    public float emergeDuration = 0.4f;
    public float stayDuration = 1.5f;
    public float sinkDuration = 0.3f;

    private int damage = 1;
    private bool hasDealtDamage = false;
    private Animator anim;
    private Collider2D col;
    private SpriteRenderer sr;

    public void Initialize(int dmg)
    {
        damage = dmg;
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        StartCoroutine(SpikeLifecycle());
    }

    IEnumerator SpikeLifecycle()
    {
        // ── 1. AJUSTE DE POSICIÓN LOCAL ──
        int groundMask = LayerMask.GetMask("Ground");
        if (groundMask == 0) groundMask = ~0;

        
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(transform.position.x, transform.position.y + 1f), Vector2.down, 5f, groundMask);
        float groundY = hit.collider != null ? hit.point.y : transform.position.y;
// Iniciamos enterrado (-2 unidades bajo el suelo) [cite: 18]
        transform.position = new Vector3(transform.position.x, groundY - 2f, 0f);

        if (col) col.enabled = false;
        if (sr) sr.enabled = false;

        // ── 2. AVISO ──
        if (sr) sr.enabled = true;
        if (HasAnimation(warningAnimName)) anim.Play(warningAnimName);
        else yield return StartCoroutine(FallbackWarning());

        yield return new WaitForSeconds(warningDuration);

        // ── 3. EMERGER (DAÑO ACTIVO) ──
        transform.position = new Vector3(transform.position.x, groundY, 0f);
        if (HasAnimation(emergeAnimName)) anim.Play(emergeAnimName);
        if (col) col.enabled = true;

        yield return new WaitForSeconds(emergeDuration + stayDuration);

        // ── 4. HUNDIRSE ──
        if (col) col.enabled = false;
        if (HasAnimation(sinkAnimName)) anim.Play(sinkAnimName);
        else yield return StartCoroutine(FallbackSink(groundY));

        yield return new WaitForSeconds(sinkDuration);
        Destroy(gameObject);
    }
// CORRECCIÓN: Uso de PlayerCore para aplicar daño [cite: 14, 15]
    void OnTriggerEnter2D(Collider2D c) => DealDamage(c);
    void OnTriggerStay2D(Collider2D c) => DealDamage(c);

    void DealDamage(Collider2D c)
    {
        if (hasDealtDamage || !c.CompareTag("Player")) return;

        PlayerCore player = c.GetComponent<PlayerCore>();
        if (player != null)
        {
            player.TakeDamage(damage);
            hasDealtDamage = true;
        }
    }

    IEnumerator FallbackWarning()
    {
        if (sr == null) yield break;
        for (int i = 0; i < 5; i++)
        {
            sr.color = new Color(1f, 0f, 0f, 0.5f);
            yield return new WaitForSeconds(0.1f);
            sr.color = Color.white;
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator FallbackSink(float groundY)
    {
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Vector3 hiddenPos = new Vector3(transform.position.x, groundY - 2f, 0f);
        while (elapsed < sinkDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, hiddenPos, elapsed / sinkDuration);
            yield return null;
        }
    }

    bool HasAnimation(string clipName)
    {
        if (anim == null || anim.runtimeAnimatorController == null) return false;
        foreach (var clip in anim.runtimeAnimatorController.animationClips)
            if (clip.name == clipName) return true;
        return false;
    }
}