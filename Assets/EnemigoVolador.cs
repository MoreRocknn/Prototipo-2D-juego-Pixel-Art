using System.Collections;
using UnityEngine;

public class EnemigoVolador : MonoBehaviour
{
    [Header("=== SALUD ===")]
    public int vidaMaxima = 3;
    public float tiempoInvencibilidad = 0.25f;
    public Vector2 fuerzaKnockback = new Vector2(4f, 3f);

    [Header("=== BARRA DE VIDA ===")]
    public Vector3 offsetBarraVida = new Vector3(0, 2f, 0);
    public bool ocultarBarraVidaLlena = true;
    private HealthBarUI healthBar;

    [Header("=== DETECCIÓN ===")]
    public float rangoDeteccion = 10f;
    public float rangoAtaque = 2.5f;
    public LayerMask capaJugador;
    public LayerMask capaPared;
    public Transform puntoDeteccion;

    [Header("=== MOVIMIENTO ===")]
    public float velocidadMovimiento = 2f;
    public float velocidadPersecucion = 3.5f;
    public float velocidadHuida = 5f;
    public float velocidadVertical = 2.5f;
    public float tiempoSuavizado = 0.3f;

    [Header("=== PATRULLA ===")]
    public Vector2 tamanoAreaPatrulla = new Vector2(8f, 4f);
    public float tiempoEsperaPatrulla = 2f;

    [Header("=== ATAQUE ===")]
    public Transform puntoAtaque;
    public float radioAtaque = 1f;
    public int danoAtaque = 1;
    public float velocidadAtaque = 12f;
    public float duracionAtaque = 0.25f;
    public float tiempoRecargaAtaque = 1.5f;
    public float tiempoGuardia = 0.8f;
    [Range(0.1f, 1f)] public float toleranciaAlineacion = 0.5f;

    [Header("=== VISUAL ===")]
    public Color colorGuardia = Color.yellow;
    public Color colorAtaque = Color.red;
    public Color colorDanado = Color.white;

    [Header("=== ANIMACIONES ===")]
    public string parametroVelocidad = "speed";
    public string parametroEstado = "state";

    private enum Estado { Inactivo, Patrulla, Alerta, Guardia, Perseguir, Atacar, Huir, Aturdido }
    private Estado estadoActual = Estado.Inactivo;

    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Transform jugador;
    private Animator anim;
    private Collider2D col;
    private Color colorOriginal;

    private Vector2 posicionInicial, objetivoPatrulla, velocidad;
    private float temporizadorAtaque, temporizadorGuardia, temporizadorEspera, limiteInferiorY, limiteSuperiorY;
    private float rangoDeteccionCuadrado, rangoAtaqueCuadrado;
    private bool mirandoDerecha = true, esInvencible, estaAtacando;
    private int vidaActual;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        if (spriteRenderer) colorOriginal = spriteRenderer.color;

        if (rb)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.mass = 1000f;
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    void Start()
    {
        vidaActual = vidaMaxima;
        jugador = GameObject.FindGameObjectWithTag("Player")?.transform;
        posicionInicial = transform.position;
        rangoDeteccionCuadrado = rangoDeteccion * rangoDeteccion;
        rangoAtaqueCuadrado = rangoAtaque * rangoAtaque;
        limiteInferiorY = posicionInicial.y - tamanoAreaPatrulla.y * 0.5f;
        limiteSuperiorY = posicionInicial.y + tamanoAreaPatrulla.y * 0.5f;

        if (!puntoDeteccion) puntoDeteccion = transform;
        if (!puntoAtaque)
        {
            GameObject pt = new GameObject("AttackPoint");
            pt.transform.SetParent(transform);
            pt.transform.localPosition = Vector3.right;
            puntoAtaque = pt.transform;
        }

        if (HealthBarFactory.Instance)
            healthBar = HealthBarFactory.Instance.CreateHealthBar(transform, vidaActual, vidaMaxima, offsetBarraVida);
        else
        {
            GameObject hbObj = new GameObject($"HealthBar_{name}");
            healthBar = hbObj.AddComponent<HealthBarUI>();
            healthBar.offset = offsetBarraVida;
            healthBar.Initialize(transform, vidaActual, vidaMaxima);
        }
        if (healthBar) healthBar.alwaysShow = !ocultarBarraVidaLlena;

        GenerarPuntoPatrulla();
        estadoActual = Estado.Patrulla;
    }

    void Update()
    {
        temporizadorAtaque -= Time.deltaTime;
        if (esInvencible || estadoActual == Estado.Aturdido || estaAtacando)
        {
            ActualizarAnimacion();
            return;
        }

        bool puedeVer = PuedeVerJugador();
        float distanciaCuadrada = jugador ? (transform.position - jugador.position).sqrMagnitude : float.MaxValue;
        bool enRango = puedeVer && distanciaCuadrada <= rangoAtaqueCuadrado;
        bool alineado = jugador && Mathf.Abs(transform.position.y - jugador.position.y) < toleranciaAlineacion;

        if (puedeVer && estadoActual != Estado.Atacar && estadoActual != Estado.Huir)
            MirarAlJugador();

        switch (estadoActual)
        {
            case Estado.Inactivo:
            case Estado.Patrulla:
                if (puedeVer) estadoActual = Estado.Alerta;
                else Patrullar();
                break;
            case Estado.Alerta:
                if (!puedeVer) { estadoActual = Estado.Patrulla; break; }
                MoverHacia(new Vector2(jugador.position.x, LimitarY(jugador.position.y)), velocidadPersecucion * 0.8f);
                if (distanciaCuadrada <= rangoDeteccionCuadrado * 0.36f) estadoActual = Estado.Guardia;
                break;
            case Estado.Guardia:
                Guardia(puedeVer, enRango, alineado);
                break;
            case Estado.Perseguir:
                if (!puedeVer) { estadoActual = Estado.Patrulla; break; }
                MoverHacia(new Vector2(jugador.position.x, LimitarY(jugador.position.y)), velocidadPersecucion);
                if (enRango && temporizadorAtaque <= 0) estadoActual = Estado.Atacar;
                break;
            case Estado.Atacar:
                if (!estaAtacando) StartCoroutine(RealizarAtaque());
                break;
            case Estado.Huir:
                Vector2 direccion = ((Vector2)transform.position - (Vector2)jugador.position).normalized;
                if (transform.position.y < limiteInferiorY) direccion.y = 1;
                else if (transform.position.y > limiteSuperiorY) direccion.y = -1;
                rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, direccion * velocidadHuida, ref velocidad, tiempoSuavizado * 0.5f);
                if (distanciaCuadrada >= rangoDeteccionCuadrado * 0.64f) estadoActual = PuedeVerJugador() ? Estado.Alerta : Estado.Patrulla;
                break;
        }
        ActualizarAnimacion();
    }

    void Patrullar()
    {
        if (temporizadorEspera > 0)
        {
            temporizadorEspera -= Time.deltaTime;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (transform.position.y < limiteInferiorY)
            rb.linearVelocity = Vector2.up * velocidadMovimiento;
        else if (transform.position.y > limiteSuperiorY)
            rb.linearVelocity = Vector2.down * velocidadMovimiento;

        MoverHacia(objetivoPatrulla, velocidadMovimiento);

        if ((transform.position - (Vector3)objetivoPatrulla).sqrMagnitude < 0.25f)
        {
            temporizadorEspera = tiempoEsperaPatrulla;
            GenerarPuntoPatrulla();
        }
    }

    void Guardia(bool puedeVer, bool enRango, bool alineado)
    {
        if (!puedeVer) { estadoActual = Estado.Patrulla; return; }

        float yObjetivo = LimitarY(jugador.position.y);
        float diferencia = yObjetivo - transform.position.y;
        rb.linearVelocity = Mathf.Abs(diferencia) > 0.1f ? new Vector2(0, Mathf.Sign(diferencia) * velocidadVertical * 0.5f) : Vector2.zero;

        temporizadorGuardia += Time.deltaTime;
        if (temporizadorGuardia >= tiempoGuardia)
        {
            temporizadorGuardia = 0;
            if (enRango && temporizadorAtaque <= 0)
                estadoActual = Estado.Atacar;
            else if (!enRango)
                estadoActual = Estado.Perseguir;
        }

        if (spriteRenderer && !esInvencible)
            spriteRenderer.color = colorGuardia;
    }

    IEnumerator RealizarAtaque()
    {
        estaAtacando = true;
        if (!jugador) { estaAtacando = false; estadoActual = Estado.Guardia; yield break; }

        Vector2 direccionDash = (jugador.position - transform.position).normalized;
        if ((direccionDash.x > 0) != mirandoDerecha) Voltear();

        rb.linearVelocity = Vector2.zero;
        if (spriteRenderer) spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.12f);
        if (spriteRenderer) spriteRenderer.color = colorAtaque;

        float tiempo = 0;
        while (tiempo < duracionAtaque)
        {
            rb.linearVelocity = direccionDash * velocidadAtaque;
            tiempo += Time.deltaTime;

            if (puntoAtaque)
            {
                Collider2D golpe = Physics2D.OverlapCircle(puntoAtaque.position, radioAtaque, capaJugador);
                if (golpe && golpe.CompareTag("Player"))
                {
                    golpe.GetComponent<MainChar>()?.TakeDamage(danoAtaque);
                    break;
                }
            }
            yield return null;
        }

        tiempo = 0;
        while (tiempo < 0.3f)
        {
            rb.linearVelocity = -direccionDash * velocidadAtaque * 0.5f;
            tiempo += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        estaAtacando = false;
        temporizadorAtaque = tiempoRecargaAtaque;
        if (spriteRenderer) spriteRenderer.color = colorOriginal;
        estadoActual = Estado.Guardia;
    }

    public void TakeDamage(int dano)
    {
        float direccionKnockback = jugador ? Mathf.Sign(transform.position.x - jugador.position.x) : 1f;
        TakeDamage(dano, direccionKnockback);
    }

    public void TakeDamage(int dano, float direccionKnockback)
    {
        if (esInvencible) return;

        vidaActual -= dano;
        if (healthBar) healthBar.UpdateHealth(vidaActual, vidaMaxima);
        if (spriteRenderer) spriteRenderer.color = colorDanado;
        rb.linearVelocity = new Vector2(fuerzaKnockback.x * direccionKnockback, fuerzaKnockback.y);

        if (vidaActual <= 0)
            Morir();
        else
            StartCoroutine(Aturdir());
    }

    IEnumerator Aturdir()
    {
        esInvencible = true;
        estadoActual = Estado.Aturdido;
        float tiempoParpadeo = tiempoInvencibilidad / 8f;

        for (int i = 0; i < 4; i++)
        {
            if (spriteRenderer) spriteRenderer.color = colorDanado;
            yield return new WaitForSeconds(tiempoParpadeo);
            if (spriteRenderer) spriteRenderer.color = colorOriginal;
            yield return new WaitForSeconds(tiempoParpadeo);
        }

        esInvencible = false;
        estadoActual = PuedeVerJugador() ? Estado.Alerta : Estado.Patrulla;
    }

    void Morir()
    {
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        if (col) col.enabled = false;
        if (healthBar) healthBar.gameObject.SetActive(false);
        StartCoroutine(AnimacionMuerte());
    }

    IEnumerator AnimacionMuerte()
    {
        if (anim) anim.SetInteger(parametroEstado, 6);

        float tiempo = 0;
        Color c = spriteRenderer ? spriteRenderer.color : Color.white;
        while (tiempo < 0.5f)
        {
            tiempo += Time.deltaTime;
            if (spriteRenderer) spriteRenderer.color = new Color(c.r, c.g, c.b, 1 - tiempo * 2);
            yield return null;
        }

        if (EnemyManager.Instance != null)
            EnemyManager.Instance.OnEnemyDeath(gameObject);
        else
            gameObject.SetActive(false);
    }

    public void RestoreFullHealth()
    {
        vidaActual = vidaMaxima;

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

        jugador = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (spriteRenderer)
        {
            Color c = colorOriginal;
            c.a = 1f;
            spriteRenderer.color = c;
        }

        estadoActual = Estado.Patrulla;
        esInvencible = false;
        estaAtacando = false;
        temporizadorAtaque = 0;
        temporizadorGuardia = 0;
        temporizadorEspera = 0;
        velocidad = Vector2.zero;

        if (healthBar)
        {
            healthBar.gameObject.SetActive(true);
            healthBar.UpdateHealth(vidaActual, vidaMaxima);
            if (ocultarBarraVidaLlena)
                healthBar.alwaysShow = false;
        }

        GenerarPuntoPatrulla();
    }

    void MoverHacia(Vector2 objetivo, float velocidadParam)
    {
        Vector2 direccion = (objetivo - (Vector2)transform.position).normalized;
        rb.linearVelocity = Vector2.SmoothDamp(rb.linearVelocity, direccion * velocidadParam, ref velocidad, tiempoSuavizado);
        if ((direccion.x > 0) != mirandoDerecha) Voltear();
    }

    float LimitarY(float y) => Mathf.Clamp(y, limiteInferiorY, limiteSuperiorY);

    void GenerarPuntoPatrulla()
    {
        objetivoPatrulla = new Vector2(
            posicionInicial.x + Random.Range(-tamanoAreaPatrulla.x * 0.5f, tamanoAreaPatrulla.x * 0.5f),
            Random.Range(limiteInferiorY, limiteSuperiorY)
        );
    }

    bool PuedeVerJugador()
    {
        if (!jugador || (transform.position - jugador.position).sqrMagnitude > rangoDeteccionCuadrado)
            return false;

        RaycastHit2D golpe = Physics2D.Raycast(
            puntoDeteccion.position,
            (jugador.position - puntoDeteccion.position).normalized,
            rangoDeteccion,
            capaPared | capaJugador
        );

        return golpe.collider && golpe.collider.CompareTag("Player");
    }

    void MirarAlJugador()
    {
        if (!jugador) return;
        if ((jugador.position.x > transform.position.x) != mirandoDerecha)
            Voltear();
    }

    void Voltear()
    {
        mirandoDerecha = !mirandoDerecha;
        Vector3 escala = transform.localScale;
        escala.x *= -1f;
        transform.localScale = escala;
    }

    void ActualizarAnimacion()
    {
        if (!anim) return;

        anim.SetFloat(parametroVelocidad, rb.linearVelocity.magnitude);

        int valor = estadoActual switch
        {
            Estado.Inactivo => 0,
            Estado.Patrulla => 1,
            Estado.Alerta => 2,
            Estado.Perseguir => 2,
            Estado.Guardia => 3,
            Estado.Atacar => 4,
            Estado.Aturdido => 5,
            _ => 0
        };

        anim.SetInteger(parametroEstado, valor);
    }

    void OnDrawGizmosSelected()
    {
        Vector2 centro = Application.isPlaying ? posicionInicial : (Vector2)transform.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(centro, rangoDeteccion);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(centro, rangoAtaque);

        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireCube(centro, tamanoAreaPatrulla);

        if (puntoAtaque)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(puntoAtaque.position, radioAtaque);
        }
    }
}