using UnityEngine;
using System.Collections;

public class FallingSword : MonoBehaviour
{
    [Header("=== ANIMACIONES ===")]
    public string fallAnimName = "SwordFall";
    public string impactAnimName = "SwordImpact";

    [Header("=== PARÁMETROS ===")]
    public float fallSpeed = 22f;
    public int damage = 1;
    public float stickTime = 1.5f;
    public float impactDuration = 0.4f;

    private bool hasHit = false;
    private bool falling = false;
    private float targetGroundY = -999f;
    private Animator anim;
    private Rigidbody2D rb;
    private Collider2D col;

    // ── Initialize ────────────────────────────────────────────
    // groundY: Y real del suelo donde debe clavarse
    public void Initialize(float speed, int dmg, float groundY = -999f)
    {
        fallSpeed = speed;
        damage = dmg;
        hasHit = false;
        falling = true;
        targetGroundY = groundY;

        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        // Movimiento 100% manual
        if (rb != null) { rb.bodyType = RigidbodyType2D.Kinematic; rb.linearVelocity = Vector2.zero; }

        // Desactivar colider al inicio — se activa al llegar cerca del suelo
        // Evita que choque con el boss/jugador al spawnear
        if (col != null) col.enabled = false;

        if (anim != null && HasAnimation(fallAnimName)) anim.Play(fallAnimName);
    }

    // ── Caída ─────────────────────────────────────────────────
    void Update()
    {
        if (!falling || hasHit) return;

        transform.position += Vector3.down * fallSpeed * Time.deltaTime;

        // Activar colider cuando está a 3 unidades del suelo
        if (col != null && !col.enabled && targetGroundY > -999f)
            if (transform.position.y < targetGroundY + 3f)
                col.enabled = true;

        // Clavar por posición si llega al groundY (fallback sin colider)
        if (targetGroundY > -999f && transform.position.y <= targetGroundY)
        {
            transform.position = new Vector3(transform.position.x, targetGroundY, transform.position.z);
            hasHit = true; falling = false;
            StartCoroutine(StickToGround());
            return;
        }

        if (transform.position.y < -60f) Destroy(gameObject);
    }

    // ── Colisiones ────────────────────────────────────────────
    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        if (other.CompareTag("Player"))
        {
            hasHit = true; falling = false;
            other.GetComponent<PlayerCore>()?.TakeDamage(damage);
            StartCoroutine(QuickDestroy());
        }
        else if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            hasHit = true; falling = false;
            StartCoroutine(StickToGround());
        }
    }

    // ── Clavarse ──────────────────────────────────────────────
    IEnumerator StickToGround()
    {
        if (col != null) col.enabled = false;
        if (anim != null && HasAnimation(impactAnimName)) anim.Play(impactAnimName);
        else yield return StartCoroutine(FallbackImpact());
        yield return new WaitForSeconds(impactDuration);
        yield return new WaitForSeconds(stickTime);
        yield return StartCoroutine(FadeOut());
        Destroy(gameObject);
    }

    IEnumerator QuickDestroy()
    {
        if (anim != null && HasAnimation(impactAnimName)) anim.Play(impactAnimName);
        yield return new WaitForSeconds(0.2f);
        Destroy(gameObject);
    }

    IEnumerator FallbackImpact()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        for (int i = 0; i < 3; i++)
        {
            sr.color = Color.white; yield return new WaitForSeconds(0.08f);
            sr.color = Color.gray; yield return new WaitForSeconds(0.08f);
        }
        sr.color = Color.white;
    }

    IEnumerator FadeOut()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;
        float t = 0f; Color c = sr.color;
        while (t < 0.4f)
        {
            t += Time.deltaTime;
            sr.color = new Color(c.r, c.g, c.b, Mathf.Lerp(1f, 0f, t / 0.4f));
            yield return null;
        }
        Destroy(gameObject);
    }

    bool HasAnimation(string clipName)
    {
        if (anim == null || anim.runtimeAnimatorController == null) return false;
        foreach (var clip in anim.runtimeAnimatorController.animationClips)
            if (clip.name == clipName) return true;
        return false;
    }
}