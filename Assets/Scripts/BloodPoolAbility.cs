using UnityEngine;
using System.Collections;

// ============================================
// HABILIDAD: CHARCO DE SANGRE (Simplificada)
// ============================================
[System.Serializable]
public class BloodPoolAbility
{
    public float poolDuration = 3f;
    public float poolCooldown = 5f;
    public float poolMoveSpeed = 4f;
    public Vector2 poolSize = new Vector2(0.5f, 0.2f);
    public string abilityName = "Charco de Sangre";
    public Color abilityColor = new Color(0.8f, 0.1f, 0.1f);

    public BloodPoolAbility()
    {
        // Constructor vacío para inicialización
    }
}

// ============================================
// COMPONENTE DE TRANSFORMACIÓN (Independiente)
// ============================================
public class BloodPoolTransform : MonoBehaviour
{
    [Header("Configuración")]
    public KeyCode activationKey = KeyCode.R; // Cambio de Q a R para evitar conflictos
    public int maxUses = 3;
    public int currentUses = 3;

    [Header("Propiedades")]
    public float poolDuration = 3f;
    public float poolCooldown = 5f;
    public float poolMoveSpeed = 4f;
    public Vector2 poolSize = new Vector2(0.5f, 0.2f);

    private bool isInPoolForm = false;
    private float poolTimer = 0f;
    private float lastUseTime = -999f;
    private Vector2 originalSize;
    private Vector2 originalOffset;

    private MainChar mainChar;
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Vector3 originalScale;

    private GameObject poolVisualEffect;

    void Awake()
    {
        mainChar = GetComponent<MainChar>();
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        if (boxCollider != null)
        {
            originalSize = boxCollider.size;
            originalOffset = boxCollider.offset;
        }

        originalScale = transform.localScale;

        // Restaurar usos al iniciar
        currentUses = maxUses;
    }

    void Update()
    {
        // Mostrar info de usos (debug)
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log($"Charco de Sangre: {currentUses}/{maxUses} usos disponibles");
        }

        if (isInPoolForm)
        {
            poolTimer -= Time.deltaTime;

            if (poolTimer <= 0f)
            {
                ExitPoolForm();
            }
            else
            {
                HandlePoolMovement();
            }
        }
        else
        {
            // Activar habilidad
            if (Input.GetKeyDown(activationKey) && CanUseAbility())
            {
                UseAbility();
            }
        }
    }

    bool CanUseAbility()
    {
        bool hasUses = currentUses > 0;
        bool cooldownReady = Time.time - lastUseTime >= poolCooldown;
        bool notInPool = !isInPoolForm;

        if (!hasUses)
        {
            Debug.Log("¡No tienes más usos de Charco de Sangre!");
            return false;
        }

        if (!cooldownReady)
        {
            float remaining = poolCooldown - (Time.time - lastUseTime);
            Debug.Log($"Cooldown: {remaining:F1}s restantes");
            return false;
        }

        return hasUses && cooldownReady && notInPool;
    }

    void UseAbility()
    {
        currentUses--;
        lastUseTime = Time.time;
        EnterPoolForm();

        Debug.Log($"¡Charco de Sangre activado! Usos restantes: {currentUses}/{maxUses}");
    }

    public void EnterPoolForm()
    {
        if (isInPoolForm) return;

        isInPoolForm = true;
        poolTimer = poolDuration;

        Debug.Log("¡Transformado en charco de sangre!");

        // Deshabilitar controles normales
        if (mainChar != null)
        {
            mainChar.SetInputLock(true);
            mainChar.StopPhysics();
        }

        // Cambiar tamaño del collider
        if (boxCollider != null)
        {
            boxCollider.size = poolSize;
            boxCollider.offset = new Vector2(0, poolSize.y / 2f);
        }

        // Cambiar apariencia
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.5f, 0f, 0f, 0.7f);
        }

        // Cambiar escala visual
        transform.localScale = new Vector3(originalScale.x * 1.5f, originalScale.y * 0.3f, originalScale.z);

        // Desactivar gravedad
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
        }

        // Crear efecto visual
        CreatePoolVisual();
    }

    public void ExitPoolForm()
    {
        if (!isInPoolForm) return;

        isInPoolForm = false;
        poolTimer = 0f;

        Debug.Log("Forma normal restaurada");

        // Restaurar controles
        if (mainChar != null)
        {
            mainChar.SetInputLock(false);
        }

        // Restaurar collider
        if (boxCollider != null)
        {
            boxCollider.size = originalSize;
            boxCollider.offset = originalOffset;
        }

        // Restaurar apariencia
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        // Restaurar escala
        transform.localScale = originalScale;

        // Restaurar física
        if (rb != null)
        {
            rb.gravityScale = 2f;
        }

        // Destruir efecto visual
        if (poolVisualEffect != null)
        {
            Destroy(poolVisualEffect);
        }
    }

    void HandlePoolMovement()
    {
        float moveInput = 0f;

        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            moveInput = 1f;
        else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            moveInput = -1f;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(moveInput * poolMoveSpeed, 0f);
        }

        // Salir anticipadamente si se presiona la tecla de nuevo
        if (Input.GetKeyDown(activationKey))
        {
            ExitPoolForm();
        }
    }

    void CreatePoolVisual()
    {
        poolVisualEffect = new GameObject("PoolEffect");
        poolVisualEffect.transform.SetParent(transform);
        poolVisualEffect.transform.localPosition = Vector3.zero;

        SpriteRenderer poolSr = poolVisualEffect.AddComponent<SpriteRenderer>();
        poolSr.sprite = CreatePoolSprite();
        poolSr.color = new Color(0.6f, 0f, 0f, 0.6f);
        poolSr.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder - 1 : -1;

        StartCoroutine(DripEffect());
    }

    Sprite CreatePoolSprite()
    {
        Texture2D tex = new Texture2D(64, 32);
        Color[] pixels = new Color[64 * 32];

        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float distX = (x - 32f) / 32f;
                float distY = (y - 16f) / 16f;
                float dist = distX * distX + distY * distY;

                if (dist < 1f)
                {
                    pixels[y * 64 + x] = new Color(0.5f, 0f, 0f, 1f - dist);
                }
                else
                {
                    pixels[y * 64 + x] = Color.clear;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, 64, 32), new Vector2(0.5f, 0.5f));
    }

    IEnumerator DripEffect()
    {
        while (isInPoolForm && poolVisualEffect != null)
        {
            float alpha = 0.4f + Mathf.PingPong(Time.time * 2f, 0.3f);
            SpriteRenderer sr = poolVisualEffect.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
            yield return null;
        }
    }

    public bool IsInPoolForm()
    {
        return isInPoolForm;
    }

    // Restaurar usos (cuando el jugador muere)
    public void ResetUses()
    {
        currentUses = maxUses;
        ExitPoolForm();
        Debug.Log("Usos de Charco de Sangre restaurados");
    }

    void OnDestroy()
    {
        if (poolVisualEffect != null)
        {
            Destroy(poolVisualEffect);
        }
    }
}