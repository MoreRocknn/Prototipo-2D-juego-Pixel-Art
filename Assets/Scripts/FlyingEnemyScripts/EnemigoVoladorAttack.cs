// ============================================================
// EnemigoVoladorAttack.cs — Lógica de ataque
// Usa PlayerCore.TakeDamage() — NO depende de MainChar
// ============================================================
using UnityEngine;
using System.Collections;

public class EnemigoVoladorAttack : MonoBehaviour
{
    private EnemigoVoladorData d;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    void Awake()
    {
        d  = GetComponent<EnemigoVoladorData>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    public IEnumerator RealizarAtaque()
    {
        d.estaAtacando = true;
        if (!d.jugador) { d.estaAtacando = false; d.estadoActual = EnemigoVoladorData.Estado.Guardia; yield break; }

        Vector2 dir = (d.jugador.position - transform.position).normalized;
        var ai = GetComponent<EnemigoVoladorAI>();
        if ((dir.x > 0) != d.mirandoDerecha) ai.Voltear();

        rb.linearVelocity = Vector2.zero;
        if (sr) sr.color = Color.white;
        yield return new WaitForSeconds(0.12f);
        if (sr) sr.color = d.colorAtaque;

        // Dash hacia el jugador
        float tiempo = 0;
        while (tiempo < d.duracionAtaque)
        {
            rb.linearVelocity = dir * d.velocidadAtaque;
            tiempo += Time.deltaTime;

            if (d.puntoAtaque)
            {
                Collider2D golpe = Physics2D.OverlapCircle(d.puntoAtaque.position, d.radioAtaque, d.capaJugador);
                if (golpe && golpe.CompareTag("Player"))
                {
                    // FIX: usar PlayerCore en vez de MainChar
                    golpe.GetComponent<PlayerCore>()?.TakeDamage(d.danoAtaque);
                    break;
                }
            }
            yield return null;
        }

        // Retroceder
        tiempo = 0;
        while (tiempo < 0.3f)
        {
            rb.linearVelocity = -dir * d.velocidadAtaque * 0.5f;
            tiempo += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        d.estaAtacando = false;
        d.temporizadorAtaque = d.tiempoRecargaAtaque;
        if (sr) sr.color = d.colorOriginal;
        d.estadoActual = EnemigoVoladorData.Estado.Guardia;
    }
}
