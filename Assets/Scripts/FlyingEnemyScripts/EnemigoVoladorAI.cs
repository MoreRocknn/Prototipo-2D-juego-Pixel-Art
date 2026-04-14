// ============================================================
// EnemigoVoladorAI.cs — Máquina de estados y movimiento
// ============================================================
using UnityEngine;

public class EnemigoVoladorAI : MonoBehaviour
{
    private EnemigoVoladorData d;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    void Awake()
    {
        d = GetComponent<EnemigoVoladorData>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        d.jugador = GameObject.FindGameObjectWithTag("Player")?.transform;
        d.posicionInicial = transform.position;
        d.rangoDeteccionCuadrado = d.rangoDeteccion * d.rangoDeteccion;
        d.rangoAtaqueCuadrado = d.rangoAtaque * d.rangoAtaque;
        d.limiteInferiorY = d.posicionInicial.y - d.tamanoAreaPatrulla.y * 0.5f;
        d.limiteSuperiorY = d.posicionInicial.y + d.tamanoAreaPatrulla.y * 0.5f;

        if (!d.puntoDeteccion) d.puntoDeteccion = transform;
        if (!d.puntoAtaque)
        {
            GameObject pt = new GameObject("AttackPoint");
            pt.transform.SetParent(transform);
            pt.transform.localPosition = Vector3.right;
            d.puntoAtaque = pt.transform;
        }

        // Barra de vida
        if (HealthBarFactory.Instance)
            d.healthBar = HealthBarFactory.Instance.CreateHealthBar(transform, d.vidaActual, d.vidaMaxima, d.offsetBarraVida);
        else
        {
            GameObject hbObj = new GameObject($"HealthBar_{name}");
            d.healthBar = hbObj.AddComponent<HealthBarUI>();
            d.healthBar.offset = d.offsetBarraVida;
            d.healthBar.Initialize(transform, d.vidaActual, d.vidaMaxima);
        }
        if (d.healthBar) d.healthBar.alwaysShow = !d.ocultarBarraVidaLlena;

        GenerarPuntoPatrulla();
        d.estadoActual = EnemigoVoladorData.Estado.Patrulla;
    }

    void Update()
    {
        d.temporizadorAtaque -= Time.deltaTime;
        if (d.esInvencible || d.estadoActual == EnemigoVoladorData.Estado.Aturdido || d.estaAtacando)
        {
            ActualizarAnimacion();
            return;
        }

        bool puedeVer = PuedeVerJugador();
        float distCuad = d.jugador ? (transform.position - d.jugador.position).sqrMagnitude : float.MaxValue;
        bool enRango = puedeVer && distCuad <= d.rangoAtaqueCuadrado;
        bool alineado = d.jugador && Mathf.Abs(transform.position.y - d.jugador.position.y) < d.toleranciaAlineacion;

        if (puedeVer && d.estadoActual != EnemigoVoladorData.Estado.Atacar && d.estadoActual != EnemigoVoladorData.Estado.Huir)
            MirarAlJugador();

        switch (d.estadoActual)
        {
            case EnemigoVoladorData.Estado.Inactivo:
            case EnemigoVoladorData.Estado.Patrulla:
                if (puedeVer) d.estadoActual = EnemigoVoladorData.Estado.Alerta;
                else Patrullar();
                break;
            case EnemigoVoladorData.Estado.Alerta:
                if (!puedeVer) { d.estadoActual = EnemigoVoladorData.Estado.Patrulla; break; }
                MoverHacia(new Vector2(d.jugador.position.x, LimitarY(d.jugador.position.y)), d.velocidadPersecucion * 0.8f);
                if (distCuad <= d.rangoDeteccionCuadrado * 0.36f) d.estadoActual = EnemigoVoladorData.Estado.Guardia;
                break;
            case EnemigoVoladorData.Estado.Guardia:
                Guardia(puedeVer, enRango);
                break;
            case EnemigoVoladorData.Estado.Perseguir:
                if (!puedeVer) { d.estadoActual = EnemigoVoladorData.Estado.Patrulla; break; }
                MoverHacia(new Vector2(d.jugador.position.x, LimitarY(d.jugador.position.y)), d.velocidadPersecucion);
                if (enRango && d.temporizadorAtaque <= 0) d.estadoActual = EnemigoVoladorData.Estado.Atacar;
                break;
            case EnemigoVoladorData.Estado.Atacar:
                if (!d.estaAtacando) StartCoroutine(GetComponent<EnemigoVoladorAttack>().RealizarAtaque());
                break;
            case EnemigoVoladorData.Estado.Huir:
                Vector2 dir = ((Vector2)transform.position - (Vector2)d.jugador.position).normalized;
                if (transform.position.y < d.limiteInferiorY) dir.y = 1;
                else if (transform.position.y > d.limiteSuperiorY) dir.y = -1;
                rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, dir * d.velocidadHuida, ref d.velocidadSuavizado, d.tiempoSuavizado * 0.5f);
                if (distCuad >= d.rangoDeteccionCuadrado * 0.64f)
                    d.estadoActual = PuedeVerJugador() ? EnemigoVoladorData.Estado.Alerta : EnemigoVoladorData.Estado.Patrulla;
                break;
        }
        ActualizarAnimacion();
    }

    void Patrullar()
    {
        if (d.temporizadorEspera > 0) { d.temporizadorEspera -= Time.deltaTime; rb.linearVelocity = Vector2.zero; return; }
        if (transform.position.y < d.limiteInferiorY) rb.linearVelocity = Vector2.up * d.velocidadMovimiento;
        else if (transform.position.y > d.limiteSuperiorY) rb.linearVelocity = Vector2.down * d.velocidadMovimiento;
        MoverHacia(d.objetivoPatrulla, d.velocidadMovimiento);
        if ((transform.position - (Vector3)d.objetivoPatrulla).sqrMagnitude < 0.25f)
        { d.temporizadorEspera = d.tiempoEsperaPatrulla; GenerarPuntoPatrulla(); }
    }

    void Guardia(bool puedeVer, bool enRango)
    {
        if (!puedeVer) { d.estadoActual = EnemigoVoladorData.Estado.Patrulla; return; }
        float yObj = LimitarY(d.jugador.position.y);
        float dif = yObj - transform.position.y;
        rb.linearVelocity = Mathf.Abs(dif) > 0.1f ? new Vector2(0, Mathf.Sign(dif) * d.velocidadVertical * 0.5f) : Vector2.zero;
        d.temporizadorGuardia += Time.deltaTime;
        if (d.temporizadorGuardia >= d.tiempoGuardia)
        {
            d.temporizadorGuardia = 0;
            if (enRango && d.temporizadorAtaque <= 0) d.estadoActual = EnemigoVoladorData.Estado.Atacar;
            else if (!enRango) d.estadoActual = EnemigoVoladorData.Estado.Perseguir;
        }
        if (sr && !d.esInvencible) sr.color = d.colorGuardia;
    }

    public void MoverHacia(Vector2 objetivo, float vel)
    {
        Vector2 dir = (objetivo - (Vector2)transform.position).normalized;
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, dir * vel, ref d.velocidadSuavizado, d.tiempoSuavizado);
        if ((dir.x > 0) != d.mirandoDerecha) Voltear();
    }

    public bool PuedeVerJugador()
    {
        if (!d.jugador || (transform.position - d.jugador.position).sqrMagnitude > d.rangoDeteccionCuadrado) return false;
        RaycastHit2D hit = Physics2D.Raycast(d.puntoDeteccion.position,
            (d.jugador.position - d.puntoDeteccion.position).normalized,
            d.rangoDeteccion, d.capaPared | d.capaJugador);
        return hit.collider && hit.collider.CompareTag("Player");
    }

    void MirarAlJugador() { if (d.jugador && (d.jugador.position.x > transform.position.x) != d.mirandoDerecha) Voltear(); }

    public void Voltear()
    {
        d.mirandoDerecha = !d.mirandoDerecha;
        Vector3 e = transform.localScale; e.x *= -1f; transform.localScale = e;
    }

    public float LimitarY(float y) => Mathf.Clamp(y, d.limiteInferiorY, d.limiteSuperiorY);

    public void GenerarPuntoPatrulla()
    {
        d.objetivoPatrulla = new Vector2(
            d.posicionInicial.x + Random.Range(-d.tamanoAreaPatrulla.x * 0.5f, d.tamanoAreaPatrulla.x * 0.5f),
            Random.Range(d.limiteInferiorY, d.limiteSuperiorY)
        );
    }

    void ActualizarAnimacion()
    {
        GetComponent<EnemigoVoladorAnimator>()?.Actualizar();
    }

    // ── FIX 1: Gizmos siguen al enemigo en runtime ────────────────────────
    void OnDrawGizmosSelected()
    {
        if (d == null) d = GetComponent<EnemigoVoladorData>();
        if (d == null) return;

        Vector2 pos = (Vector2)transform.position;                         // posición real del enemigo — SIEMPRE se mueve con él
        Vector2 origen = Application.isPlaying ? d.posicionInicial : pos;    // área de patrulla anclada al punto de spawn

        // Rango de detección — círculo amarillo, sigue al enemigo
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, d.rangoDeteccion);

        // Rango de ataque — círculo rojo, sigue al enemigo
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, d.rangoAtaque);

        // Área de patrulla — anclada al spawn (correcto, no cambia)
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireCube(origen, d.tamanoAreaPatrulla);

        // Punto exacto de golpe del ataque — magenta, sigue al puntoAtaque hijo
        if (d.puntoAtaque)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(d.puntoAtaque.position, d.radioAtaque);
        }
    }
}