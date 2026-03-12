// ============================================================
// FallingSword.cs — con soporte para Animator
//
// ANIMACIONES QUE NECESITA EL PREFAB (créalas en Unity):
//
//   "SwordFall"     → espada cayendo (puede ser solo idle)
//                     Anima: lo que quieras (rotación, escala...)
//                     Loop Time: ON
//
//   "SwordImpact"   → cuando clava en el suelo
//                     Anima: Transform/Scale Y (1 → 0.8 → 1)
//                             SpriteRenderer/Color (blanco → gris)
//                     Loop Time: OFF
//
//   "SwordAppear"   → cuando se instancia (opcional, efecto de entrada)
//                     Anima: Transform/Scale (0,0,1 → 1,1,1)
//                     Loop Time: OFF
//
// PASOS EN UNITY:
//   1. Abre el prefab FallingSwords (doble clic)
//   2. Window → Animation → Animation → Create
//   3. Guarda como "SwordAppear"
//   4. Add Property → Transform → Scale
//      Frame 0: (0, 0, 1)  |  Frame 8: (1.2, 1.2, 1)  |  Frame 12: (1, 1, 1)
//   5. Repite para "SwordFall" y "SwordImpact"
//   6. En el Animator, el estado por defecto puede ser "SwordAppear"
//      con transición automática a "SwordFall" al terminar
// ============================================================

using UnityEngine;
using System.Collections;

public class FallingSword : MonoBehaviour
{
    [Header("=== NOMBRES DE ANIMACIONES ===")]
    [Tooltip("Animación al aparecer (opcional)")]
    public string appearAnimName = "SwordAppear";

    [Tooltip("Animación mientras cae")]
    public string fallAnimName   = "SwordFall";

    [Tooltip("Animación al clavarse en el suelo o golpear")]
    public string impactAnimName = "SwordImpact";

    [Header("=== PARÁMETROS ===")]
    public float fallSpeed = 22f;
    public float damage    = 1f;

    [Tooltip("Tiempo que permanece clavada antes de destruirse")]
    public float stickTime = 1.5f;

    [Tooltip("Duración del clip SwordImpact — debe coincidir con el clip")]
    public float impactDuration = 0.4f;

    // ─────────────────────────────────────────────────────────
    // PRIVADAS
    // ─────────────────────────────────────────────────────────
    private bool      hasHit = false;
    private Animator  anim;

    // =========================================================
    // INITIALIZE — llamado por BossAttackSystem
    // =========================================================
    public void Initialize(float speed, float dmg)
    {
        fallSpeed = speed;
        damage    = dmg;
        anim      = GetComponent<Animator>();

        // Reproducir animación de aparición si existe,
        // si no, ir directamente a la de caída
        if (HasAnimation(appearAnimName))
            anim.Play(appearAnimName);
        else if (HasAnimation(fallAnimName))
            anim.Play(fallAnimName);
    }

    // =========================================================
    // UPDATE — caída continua hasta impacto
    // =========================================================
    void Update()
    {
        if (hasHit) return;

        // Mover hacia abajo cada frame
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;

        // Auto-destruir si sale del mundo
        if (transform.position.y < -50f)
            Destroy(gameObject);
    }

    // =========================================================
    // COLISIONES
    // =========================================================
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;

        if (collision.CompareTag("Player"))
        {
            // Golpeó al jugador
            PlayerCore player = collision.GetComponent<PlayerCore>();
            if (player != null) player.TakeDamage((int)damage);

            hasHit = true;
            StartCoroutine(ImpactAndDestroy());
        }
        else if (collision.CompareTag("ground") ||
                 collision.gameObject.layer == LayerMask.NameToLayer("ground") ||
                 collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            // Golpeó el suelo
            hasHit = true;
            StartCoroutine(StickToGround());
        }
    }

    // =========================================================
    // CLAVARSE EN EL SUELO
    // =========================================================
    IEnumerator StickToGround()
    {
        // Parar movimiento
        fallSpeed = 0f;

        // Reproducir animación de impacto
        if (HasAnimation(impactAnimName))
            anim.Play(impactAnimName);
        else
            yield return StartCoroutine(FallbackImpact());

        yield return new WaitForSeconds(impactDuration);

        // Esperar clavada
        yield return new WaitForSeconds(stickTime);

        // Fade out y destruir
        yield return StartCoroutine(FadeOut());
        Destroy(gameObject);
    }

    // =========================================================
    // DESTRUIR TRAS GOLPEAR AL JUGADOR
    // =========================================================
    IEnumerator ImpactAndDestroy()
    {
        fallSpeed = 0f;

        if (HasAnimation(impactAnimName))
            anim.Play(impactAnimName);

        yield return new WaitForSeconds(0.2f);
        Destroy(gameObject);
    }

    // =========================================================
    // FALLBACKS — se usan si no hay animaciones todavía
    // =========================================================

    IEnumerator FallbackImpact()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        for (int i = 0; i < 3; i++)
        {
            sr.color = Color.white;
            yield return new WaitForSeconds(0.08f);
            sr.color = Color.gray;
            yield return new WaitForSeconds(0.08f);
        }
        sr.color = Color.white;
    }

    IEnumerator FadeOut()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        float elapsed  = 0f;
        float fadeTime = 0.3f;
        Color start    = sr.color;

        while (elapsed < fadeTime)
        {
            elapsed   += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
            sr.color    = new Color(start.r, start.g, start.b, alpha);
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
}