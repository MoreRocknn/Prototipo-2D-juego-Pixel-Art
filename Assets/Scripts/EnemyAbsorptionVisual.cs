using UnityEngine;
using System.Collections;

/// <summary>
/// Indicador de absorción - Versión con input único y animación por sprites
/// CORREGIDO: Delega la absorción al AbilityAbsorptionManager cuando el modo cinemático está activo.
/// No ejecuta la absorción ni OnAbsorbed por sí mismo en ese caso.
/// </summary>
public class EnemyAbsorptionVisual : MonoBehaviour
{
    [Header("=== GENERAL ===")]
    public KeyCode key = KeyCode.E;
    public float absorptionRange = 3f;

    [Header("=== ANIMACIÓN POR SPRITES ===")]
    [Tooltip("Frames de la animación del indicador")]
    public Sprite[] indicatorFrames;
    [Tooltip("Velocidad de la animación (frames por segundo)")]
    [Range(5f, 30f)]
    public float animationSpeed = 12f;
    [Tooltip("Si está vacío, usa un sprite circular generado")]
    public Sprite staticIndicatorSprite;

    [Header("=== POSICIÓN ===")]
    [Range(0.5f, 4f)]
    public float indicatorHeight = 1.8f;
    [Range(0.3f, 2f)]
    public float indicatorSize = 1f;

    [Header("=== COLORES ===")]
    public Color normalColor = new Color(0.9f, 0.7f, 0.3f);
    public Color readyColor = new Color(0.3f, 1f, 0.3f);
    public Color glowColor = new Color(1f, 0.5f, 0.2f);

    [Header("=== EFECTOS ===")]
    public bool showGlow = true;
    [Range(0.1f, 2f)]
    public float glowIntensity = 0.5f;
    public bool bobAnimation = true;
    [Range(0f, 0.3f)]
    public float bobAmount = 0.1f;
    [Range(1f, 5f)]
    public float bobSpeed = 2f;

    [Header("=== TECLA ===")]
    public bool showKeyPrompt = true;
    public Color keyTextColor = Color.white;
    public Color keyBgColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
    [Range(10, 40)]
    public int keyFontSize = 24;

    // Referencias
    private Enemigo enemigo;
    private AbilityHolder abilityHolder;
    private Transform player;

    // Indicador visual
    private GameObject indicatorObject;
    private SpriteRenderer indicatorSprite;
    private SpriteRenderer glowSprite;
    private GameObject keyPromptObject;

    // Estado
    private bool isShowing = false;
    private bool playerInRange = false;
    private bool canAbsorb = false;
    private int currentFrame = 0;
    private float animationTimer = 0f;

    void Start()
    {
        enemigo = GetComponent<Enemigo>();
        abilityHolder = GetComponent<AbilityHolder>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        CreateIndicator();
        HideIndicator();
    }

    void Update()
    {
        if (enemigo == null || player == null) return;

        // Si el modo cinemático está activo, este script NO debe procesar nada.
        // El AbilityAbsorptionManager ya maneja todo en ese caso.
        if (AbilityAbsorptionManager.Instance != null &&
            AbilityAbsorptionManager.Instance.useCinematicMode &&
            AbilityAbsorptionManager.Instance.cinematicSystem != null &&
            AbilityAbsorptionManager.Instance.cinematicSystem.IsInCinematicMode())
        {
            return;
        }

        // Verificar si el enemigo puede ser absorbido
        canAbsorb = enemigo.CanBeAbsorbed();

        // Verificar si el jugador está en rango
        float distance = Vector2.Distance(transform.position, player.position);
        playerInRange = distance <= absorptionRange;

        // Mostrar/ocultar indicador
        if (canAbsorb && playerInRange && !isShowing)
        {
            ShowIndicator();
        }
        else if ((!canAbsorb || !playerInRange) && isShowing)
        {
            HideIndicator();
        }

        // Actualizar indicador si está visible
        if (isShowing)
        {
            UpdateIndicator();

            // Detectar input de absorción SOLO si NO hay modo cinemático
            // (si hay modo cinemático, el manager captura el input)
            if (Input.GetKeyDown(key))
            {
                bool useCinematic = AbilityAbsorptionManager.Instance != null &&
                                    AbilityAbsorptionManager.Instance.useCinematicMode;

                if (!useCinematic)
                {
                    // Sin cinemático: este script hace la absorción directamente
                    TryAbsorb();
                }
                // Si hay cinemático, NO hacemos nada aquí.
                // El AbilityAbsorptionManager.Update() ya captura el mismo input
                // y lanza el modo cinemático, que al terminar llama a PerformAbsorption().
            }
        }
    }

    private void CreateIndicator()
    {
        // Crear objeto principal del indicador
        indicatorObject = new GameObject("AbsorptionIndicator");
        indicatorObject.transform.SetParent(transform);
        indicatorObject.transform.localPosition = Vector3.up * indicatorHeight;

        // Crear sprite del indicador
        GameObject spriteObj = new GameObject("Sprite");
        spriteObj.transform.SetParent(indicatorObject.transform);
        spriteObj.transform.localPosition = Vector3.zero;

        indicatorSprite = spriteObj.AddComponent<SpriteRenderer>();
        indicatorSprite.sortingOrder = 100;

        // Asignar sprite
        if (indicatorFrames != null && indicatorFrames.Length > 0)
        {
            indicatorSprite.sprite = indicatorFrames[0];
        }
        else if (staticIndicatorSprite != null)
        {
            indicatorSprite.sprite = staticIndicatorSprite;
        }
        else
        {
            // Generar sprite circular por defecto
            indicatorSprite.sprite = GenerateCircleSprite(64);
        }

        spriteObj.transform.localScale = Vector3.one * indicatorSize;

        // Crear glow (opcional)
        if (showGlow)
        {
            GameObject glowObj = new GameObject("Glow");
            glowObj.transform.SetParent(indicatorObject.transform);
            glowObj.transform.localPosition = Vector3.zero;

            glowSprite = glowObj.AddComponent<SpriteRenderer>();
            glowSprite.sprite = GenerateCircleSprite(64, true);
            glowSprite.color = glowColor;
            glowSprite.sortingOrder = 99;
            glowObj.transform.localScale = Vector3.one * indicatorSize * 1.5f;
        }

        // Crear prompt de tecla
        if (showKeyPrompt)
        {
            CreateKeyPrompt();
        }
    }

    private void CreateKeyPrompt()
    {
        keyPromptObject = new GameObject("KeyPrompt");
        keyPromptObject.transform.SetParent(indicatorObject.transform);
        keyPromptObject.transform.localPosition = Vector3.down * 0.5f;

        // Canvas
        Canvas canvas = keyPromptObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform rect = keyPromptObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1f, 0.5f);
        rect.localScale = Vector3.one * 0.01f;

        // Background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(keyPromptObject.transform, false);

        UnityEngine.UI.Image bg = bgObj.AddComponent<UnityEngine.UI.Image>();
        bg.color = keyBgColor;

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(80, 80);

        // Text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(bgObj.transform, false);

        UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();
        text.text = key.ToString();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = keyFontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = keyTextColor;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(80, 80);
    }

    private void UpdateIndicator()
    {
        // Animación por frames
        if (indicatorFrames != null && indicatorFrames.Length > 1)
        {
            animationTimer += Time.deltaTime * animationSpeed;

            if (animationTimer >= 1f)
            {
                animationTimer = 0f;
                currentFrame = (currentFrame + 1) % indicatorFrames.Length;
                indicatorSprite.sprite = indicatorFrames[currentFrame];
            }
        }

        // Efecto de bobbing (flotar arriba/abajo)
        if (bobAnimation)
        {
            float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            indicatorObject.transform.localPosition = Vector3.up * (indicatorHeight + bobOffset);
        }

        // Efecto de glow pulsante
        if (showGlow && glowSprite != null)
        {
            float pulse = 0.5f + Mathf.Sin(Time.time * 3f) * 0.5f;
            Color color = glowColor;
            color.a = pulse * glowIntensity;
            glowSprite.color = color;
        }

        // Cambiar color según si está listo para absorber
        if (playerInRange && canAbsorb)
        {
            indicatorSprite.color = readyColor;
        }
        else
        {
            indicatorSprite.color = normalColor;
        }
    }

    private void ShowIndicator()
    {
        if (indicatorObject != null)
        {
            indicatorObject.SetActive(true);
            isShowing = true;
        }
    }

    private void HideIndicator()
    {
        if (indicatorObject != null)
        {
            indicatorObject.SetActive(false);
            isShowing = false;
        }
    }

    /// <summary>
    /// Solo se ejecuta cuando NO hay modo cinemático.
    /// Cuando hay modo cinemático, el AbilityAbsorptionManager.PerformAbsorption() se encarga.
    /// </summary>
    private void TryAbsorb()
    {
        if (!canAbsorb || !playerInRange) return;

        // Obtener el AbilityAbsorptionManager
        var absorptionManager = AbilityAbsorptionManager.Instance;
        if (absorptionManager == null)
        {
            Debug.LogError("❌ No se encontró AbilityAbsorptionManager");
            return;
        }

        // Obtener el jugador
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        var playerAbilityHolder = playerObj.GetComponent<AbilityHolder>();
        if (playerAbilityHolder == null)
        {
            Debug.LogError("❌ Player no tiene AbilityHolder");
            return;
        }

        // Obtener la habilidad del enemigo
        if (abilityHolder == null)
        {
            Debug.LogError("❌ Enemigo no tiene AbilityHolder");
            return;
        }

        Ability enemyAbility = abilityHolder.GetAbility();
        Ability playerAbility = playerAbilityHolder.GetAbility();

        // ============================================
        // ABSORCIÓN DIRECTA - UN SOLO TOQUE
        // ============================================

        // Asignar habilidad al jugador
        playerAbilityHolder.SetAbility(enemyAbility);

        if (playerAbilityHolder.currentAbility != null)
        {
            playerAbilityHolder.currentAbility.limitedUses = false;
            playerAbilityHolder.currentAbility.maxUses = 999;
            playerAbilityHolder.currentAbility.currentUses = 999;

            // Si es Dash, configurar como permanente
            if (playerAbilityHolder.currentAbility is DashAbility dashAbility)
            {
                dashAbility.dashCooldown = 3f;

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.UnlockPermanentDash();
                }
            }

            Debug.Log($"✅ Habilidad absorbida: {playerAbilityHolder.currentAbility.abilityName}");
        }

        // Asignar la habilidad del jugador al enemigo (swap)
        abilityHolder.SetAbility(playerAbility);

        // Llamar al evento de absorción del enemigo
        if (enemigo != null)
        {
            enemigo.OnAbsorbed();
        }

        // Efecto visual de absorción
        StartCoroutine(AbsorptionEffect());

        // Mensaje de log
        string message = enemyAbility != null
            ? $"¡Absorbiste {enemyAbility.abilityName} PERMANENTEMENTE!"
            : "¡Transferiste tu habilidad!";
        Debug.Log(message);
    }

    private IEnumerator AbsorptionEffect()
    {
        // Animación de absorción
        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 startScale = indicatorObject.transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Escalar hacia arriba y luego desaparecer
            indicatorObject.transform.localScale = startScale * (1f + t * 0.5f);

            if (indicatorSprite != null)
            {
                Color color = indicatorSprite.color;
                color.a = 1f - t;
                indicatorSprite.color = color;
            }

            yield return null;
        }

        HideIndicator();

        // Resetear
        indicatorObject.transform.localScale = startScale;
        if (indicatorSprite != null)
        {
            Color color = indicatorSprite.color;
            color.a = 1f;
            indicatorSprite.color = color;
        }
    }

    /// <summary>
    /// Llamado externamente (por ejemplo desde el manager) cuando este enemigo fue absorbido.
    /// Solo oculta el indicador visual, NO ejecuta lógica de absorción.
    /// </summary>
    public void OnAbsorbed()
    {
        HideIndicator();
    }

    private Sprite GenerateCircleSprite(int resolution, bool soft = false)
    {
        Texture2D tex = new Texture2D(resolution, resolution);
        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float radius = resolution / 2f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);

                if (soft)
                {
                    // Gradiente suave para el glow
                    float alpha = 1f - (dist / radius);
                    alpha = Mathf.Clamp01(alpha);
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
                else
                {
                    // Círculo sólido con borde
                    if (dist < radius - 4)
                    {
                        tex.SetPixel(x, y, Color.white);
                    }
                    else if (dist < radius)
                    {
                        tex.SetPixel(x, y, new Color(1, 1, 1, 0.5f));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
    }

    void OnDisable()
    {
        HideIndicator();
    }

    void OnDestroy()
    {
        if (indicatorObject != null)
            Destroy(indicatorObject);
    }

    void OnDrawGizmosSelected()
    {
        // Visualizar rango de absorción
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, absorptionRange);

        // Visualizar posición del indicador
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * indicatorHeight, 0.2f);
    }
}