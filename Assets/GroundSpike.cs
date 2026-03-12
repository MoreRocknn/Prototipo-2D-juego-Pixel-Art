// ============================================================
// GroundSpike.cs — con soporte para Animator
//
// ANIMACIONES QUE NECESITA EL PREFAB (créalas en Unity):
//
//   "SpikeWarning"  → marca roja parpadeando en el suelo
//                     Anima: SpriteRenderer/Color (alpha 0↔1)
//                     Loop Time: ON
//
//   "SpikeEmerge"   → el pincho sube del suelo
//                     Anima: Transform/Position Y (groundY-2 → groundY)
//                     Loop Time: OFF
//
//   "SpikeSink"     → el pincho se hunde de vuelta
//                     Anima: Transform/Position Y (groundY → groundY-2)
//                     Loop Time: OFF
//
// PASOS EN UNITY:
//   1. Abre el prefab GroundSpikes (doble clic)
//   2. Window → Animation → Animation → Create
//   3. Guarda el .anim con el nombre "SpikeWarning"
//   4. Add Property → SpriteRenderer → Color
//   5. En frame 0: alpha=0 | frame 5: alpha=1 | frame 10: alpha=0
//   6. Repite para crear "SpikeEmerge" y "SpikeSink"
//   7. En el Animator (Window → Animation → Animator):
//      - Conecta los estados pero SIN transiciones automáticas
//      - El código los llama con anim.Play() directamente
// ============================================================

using UnityEngine;
using System.Collections;

public class GroundSpike : MonoBehaviour
{
    [Header("=== NOMBRES DE ANIMACIONES ===")]
    [Tooltip("Debe coincidir EXACTAMENTE con el nombre del clip en el Animator")]
    public string warningAnimName = "SpikeWarning";
    public string emergeAnimName  = "SpikeEmerge";
    public string sinkAnimName    = "SpikeSink";

    [Header("=== TIEMPOS ===")]
    [Tooltip("Duración del aviso antes de emerger")]
    public float warningDuration = 1.0f;

    [Tooltip("Duración del clip SpikeEmerge — debe coincidir con el clip")]
    public float emergeDuration  = 0.4f;

    [Tooltip("Tiempo visible tras emerger")]
    public float stayDuration    = 1.5f;

    [Tooltip("Duración del clip SpikeSink — debe coincidir con el clip")]
    public float sinkDuration    = 0.3f;

    // ─────────────────────────────────────────────────────────
    // PRIVADAS
    // ─────────────────────────────────────────────────────────
    private int   damage         = 1;
    private bool  hasDealtDamage = false;

    private Animator       anim;
    private Collider2D     col;
    private SpriteRenderer sr;

    // =========================================================
    // INITIALIZE — llamado por BossAttackSystem
    // =========================================================
    public void Initialize(int dmg)
    {
        damage = dmg;
        anim   = GetComponent<Animator>();
        col    = GetComponent<Collider2D>();
        sr     = GetComponent<SpriteRenderer>();

        StartCoroutine(SpikeLifecycle());
    }

    // =========================================================
    // CICLO DE VIDA COMPLETO
    // =========================================================
    IEnumerator SpikeLifecycle()
    {
        // ── 1. Detectar suelo real con Raycast ─────────────────
        // Así el pincho siempre aparece en el suelo correcto
        // sin importar la Y desde donde fue spawneado.
        RaycastHit2D hit = Physics2D.Raycast(
            transform.position + Vector3.up * 5f,  // origen: 5u arriba
            Vector2.down,                           // dirección: abajo
            20f,                                    // distancia max
            LayerMask.GetMask("ground", "Ground", "Suelo")
        );

        float groundY = hit.collider != null
            ? hit.point.y
            : transform.position.y;

        // Colocar en el suelo (enterrado al inicio)
        transform.position = new Vector3(transform.position.x, groundY - 2f, 0f);

        // Invisible y sin colisión hasta que emerja
        if (col) col.enabled = false;
        if (sr)  sr.enabled  = false;

        // ── 2. AVISO — animación o fallback por código ─────────
        if (sr) sr.enabled = true;

        if (HasAnimation(warningAnimName))
            anim.Play(warningAnimName);  // reproduce el clip del Animator
        else
            yield return StartCoroutine(FallbackWarning()); // parpadeo por código

        yield return new WaitForSeconds(warningDuration);

        // ── 3. EMERGER ─────────────────────────────────────────
        // Mover al suelo primero (la animación lo sube desde aquí)
        transform.position = new Vector3(transform.position.x, groundY - 2f, 0f);

        if (HasAnimation(emergeAnimName))
            anim.Play(emergeAnimName);

        // Activar colisión al empezar a emerger
        if (col) col.enabled = true;

        yield return new WaitForSeconds(emergeDuration);

        // ── 4. ESPERAR ─────────────────────────────────────────
        yield return new WaitForSeconds(stayDuration);

        // ── 5. HUNDIRSE ────────────────────────────────────────
        if (col) col.enabled = false; // sin daño al retirarse

        if (HasAnimation(sinkAnimName))
            anim.Play(sinkAnimName);
        else
            yield return StartCoroutine(FallbackSink(groundY));

        yield return new WaitForSeconds(sinkDuration);

        Destroy(gameObject);
    }

    // =========================================================
    // FALLBACKS — se usan si el prefab aún no tiene animaciones
    // Puedes borrarlos cuando tengas los clips listos en Unity.
    // =========================================================

    IEnumerator FallbackWarning()
    {
        if (sr == null) yield break;
        Color original = sr.color;
        Color flash    = new Color(1f, 0.1f, 0.1f, 0.8f);
        for (int i = 0; i < 5; i++)
        {
            sr.color = flash;
            yield return new WaitForSeconds(0.1f);
            sr.color = original;
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator FallbackSink(float groundY)
    {
        float   elapsed   = 0f;
        Vector3 startPos  = transform.position;
        Vector3 hiddenPos = new Vector3(transform.position.x, groundY - 2f, 0f);

        while (elapsed < sinkDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, hiddenPos, elapsed / sinkDuration);
            yield return null;
        }
    }

    // =========================================================
    // UTILIDAD — comprueba si el Animator tiene ese clip
    // =========================================================
    bool HasAnimation(string clipName)
    {
        if (anim == null || anim.runtimeAnimatorController == null) return false;
        foreach (var clip in anim.runtimeAnimatorController.animationClips)
            if (clip.name == clipName) return true;
        return false;
    }

    // =========================================================
    // DAÑO
    // =========================================================
    void OnTriggerEnter2D(Collider2D c) => DealDamage(c);
    void OnTriggerStay2D (Collider2D c) => DealDamage(c);

    void DealDamage(Collider2D c)
    {
        if (hasDealtDamage || !c.CompareTag("Player")) return;
        PlayerCore player = c.GetComponent<PlayerCore>();
        if (player == null) return;
        player.TakeDamage(damage);
        hasDealtDamage = true;
    }
}