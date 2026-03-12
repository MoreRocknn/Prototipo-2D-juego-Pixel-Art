// ============================================================
// EnemigoVoladorHealth.cs — Salud, daño, muerte y respawn
// ============================================================
using UnityEngine;
using System.Collections;

public class EnemigoVoladorHealth : MonoBehaviour
{
    private EnemigoVoladorData d;
    private SpriteRenderer sr;
    private Rigidbody2D rb;
    private Collider2D col;

    void Awake()
    {
        d   = GetComponent<EnemigoVoladorData>();
        sr  = GetComponent<SpriteRenderer>();
        rb  = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
    }

    // ── Recibir daño (llamado desde PlayerCombat) ─────────────
    public void TakeDamage(int dano, float direccionKnockback)
    {
        if (d.esInvencible) return;

        d.vidaActual -= dano;
        if (d.healthBar) d.healthBar.UpdateHealth(d.vidaActual, d.vidaMaxima);
        if (sr) sr.color = d.colorDanado;
        rb.linearVelocity = new Vector2(d.fuerzaKnockback.x * direccionKnockback, d.fuerzaKnockback.y);

        if (d.vidaActual <= 0) Morir();
        else StartCoroutine(Aturdir());
    }

    // Sobrecarga sin dirección — calcula desde el jugador
    public void TakeDamage(int dano)
    {
        float dir = d.jugador ? Mathf.Sign(transform.position.x - d.jugador.position.x) : 1f;
        TakeDamage(dano, dir);
    }

    IEnumerator Aturdir()
    {
        d.esInvencible = true;
        d.estadoActual = EnemigoVoladorData.Estado.Aturdido;
        float t = d.tiempoInvencibilidad / 8f;

        for (int i = 0; i < 4; i++)
        {
            if (sr) sr.color = d.colorDanado;
            yield return new WaitForSeconds(t);
            if (sr) sr.color = d.colorOriginal;
            yield return new WaitForSeconds(t);
        }

        d.esInvencible = false;
        d.estadoActual = GetComponent<EnemigoVoladorAI>().PuedeVerJugador()
            ? EnemigoVoladorData.Estado.Alerta
            : EnemigoVoladorData.Estado.Patrulla;
    }

    void Morir()
    {
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        if (col) col.enabled = false;
        if (d.healthBar) d.healthBar.gameObject.SetActive(false);
        StartCoroutine(AnimacionMuerte());
    }

    IEnumerator AnimacionMuerte()
    {
        GetComponent<EnemigoVoladorAnimator>()?.PlayMuerte();

        float tiempo = 0;
        Color c = sr ? sr.color : Color.white;
        while (tiempo < 0.5f)
        {
            tiempo += Time.deltaTime;
            if (sr) sr.color = new Color(c.r, c.g, c.b, 1 - tiempo * 2);
            yield return null;
        }

        if (EnemyManager.Instance != null) EnemyManager.Instance.OnEnemyDeath(gameObject);
        else gameObject.SetActive(false);
    }

    // ── Restaurar vida completa (llamado por EnemyManager) ────
    public void RestoreFullHealth()
    {
        d.vidaActual = d.vidaMaxima;

        if (rb)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.simulated = true;
            rb.gravityScale = 0f;
            rb.mass = 1000f;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.linearVelocity = Vector2.zero;
        }

        if (col) col.enabled = true;
        d.jugador = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (sr) { Color c = d.colorOriginal; c.a = 1f; sr.color = c; }

        d.estadoActual      = EnemigoVoladorData.Estado.Patrulla;
        d.esInvencible      = false;
        d.estaAtacando      = false;
        d.temporizadorAtaque = 0;
        d.temporizadorGuardia = 0;
        d.temporizadorEspera = 0;
        d.velocidadSuavizado = Vector2.zero;

        if (d.healthBar)
        {
            d.healthBar.gameObject.SetActive(true);
            d.healthBar.UpdateHealth(d.vidaActual, d.vidaMaxima);
            if (d.ocultarBarraVidaLlena) d.healthBar.alwaysShow = false;
        }

        GetComponent<EnemigoVoladorAI>()?.GenerarPuntoPatrulla();
    }
}
