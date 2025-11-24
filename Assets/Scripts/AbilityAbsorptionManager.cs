using UnityEngine;
using System.Collections.Generic;

// ============================================
// ENUM DE HABILIDADES
// ============================================
public enum AbilityType
{
    None,
    Dash,
    // Aquí puedes añadir más habilidades en el futuro
    // DoubleJump,
    // FireBall,
    // Shield,
    // etc.
}

// ============================================
// CLASE BASE PARA HABILIDADES
// ============================================
[System.Serializable]
public abstract class Ability
{
    public AbilityType abilityType;
    public string abilityName;
    public Sprite abilityIcon;
    public Color abilityColor = Color.cyan;

    public abstract void Execute(GameObject owner);
    public abstract bool CanUse(GameObject owner);
    public abstract Ability Clone();
}

// ============================================
// HABILIDAD: DASH
// ============================================
[System.Serializable]
public class DashAbility : Ability
{
    public float dashForce = 25f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    private float lastDashTime = -999f;

    public DashAbility()
    {
        abilityType = AbilityType.Dash;
        abilityName = "Dash";
        abilityColor = new Color(0.3f, 0.8f, 1f); // Azul claro
    }

    public override bool CanUse(GameObject owner)
    {
        return Time.time - lastDashTime >= dashCooldown;
    }

    public override void Execute(GameObject owner)
    {
        if (!CanUse(owner)) return;

        lastDashTime = Time.time;

        var dashExecutor = owner.GetComponent<IDashExecutor>();
        if (dashExecutor != null)
        {
            dashExecutor.PerformDash(dashForce, dashDuration);
            Debug.Log($"{owner.name} ejecutó DASH con fuerza {dashForce}");
        }
    }

    public override Ability Clone()
    {
        return new DashAbility
        {
            dashForce = this.dashForce,
            dashDuration = this.dashDuration,
            dashCooldown = this.dashCooldown,
            abilityColor = this.abilityColor
        };
    }
}

// ============================================
// INTERFAZ PARA EJECUTAR DASH
// ============================================
public interface IDashExecutor
{
    void PerformDash(float force, float duration);
}

// ============================================
// COMPONENTE DE HABILIDAD (para cada entidad)
// ============================================
public class AbilityHolder : MonoBehaviour
{
    [Header("Habilidad Actual")]
    public Ability currentAbility;

    [Header("Efectos Visuales")]
    public GameObject abilityAuraEffect;
    public ParticleSystem abilityParticles;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool hasAbilityVisualActive = false;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        UpdateVisuals();
    }

    void Update()
    {
        // Efecto de brillo si tiene habilidad
        if (hasAbilityVisualActive && spriteRenderer != null && currentAbility != null)
        {
            float pulse = Mathf.PingPong(Time.time * 2f, 0.3f);
            spriteRenderer.color = Color.Lerp(originalColor, currentAbility.abilityColor, pulse);
        }
    }

    public void SetAbility(Ability newAbility)
    {
        currentAbility = newAbility?.Clone();
        UpdateVisuals();
        Debug.Log($"{gameObject.name} ahora tiene la habilidad: {(currentAbility != null ? currentAbility.abilityName : "Ninguna")}");
    }

    public Ability GetAbility()
    {
        return currentAbility;
    }

    public void RemoveAbility()
    {
        currentAbility = null;
        UpdateVisuals();
    }

    public bool HasAbility()
    {
        return currentAbility != null;
    }

    public void UseAbility()
    {
        if (currentAbility != null && currentAbility.CanUse(gameObject))
        {
            currentAbility.Execute(gameObject);

            // Efecto visual al usar habilidad
            if (abilityParticles != null)
            {
                abilityParticles.Play();
            }
        }
    }

    private void UpdateVisuals()
    {
        hasAbilityVisualActive = (currentAbility != null);

        if (abilityAuraEffect != null)
        {
            abilityAuraEffect.SetActive(hasAbilityVisualActive);
        }

        if (spriteRenderer != null && !hasAbilityVisualActive)
        {
            spriteRenderer.color = originalColor;
        }
    }
}

// ============================================
// MANAGER DE ABSORCIÓN (Singleton)
// ============================================
public class AbilityAbsorptionManager : MonoBehaviour
{
    public static AbilityAbsorptionManager Instance { get; private set; }

    [Header("Configuración de Absorción")]
    public KeyCode absorbKey = KeyCode.E;
    public float absorptionRange = 2f;
    public LayerMask absorptionTargetLayer;

    [Header("Efectos Visuales")]
    public GameObject absorptionEffectPrefab;
    public Color absorptionBeamColor = Color.cyan;

    [Header("UI")]
    public GameObject absorptionPromptUI;
    public UnityEngine.UI.Text absorptionText;

    [Header("Indicador Visual sobre Enemigo")]
    public GameObject absorptionIndicatorPrefab;
    private GameObject currentIndicator;

    private Transform player;
    private AbilityHolder playerAbilityHolder;
    private IAbsorbable nearbyAbsorbableTarget;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerAbilityHolder = playerObj.GetComponent<AbilityHolder>();

            if (playerAbilityHolder == null)
            {
                playerAbilityHolder = playerObj.AddComponent<AbilityHolder>();
            }
        }

        if (absorptionPromptUI != null)
        {
            absorptionPromptUI.SetActive(false);
        }
    }

    void Update()
    {
        if (player == null) return;

        CheckForAbsorbableTargets();

        if (Input.GetKeyDown(absorbKey) && nearbyAbsorbableTarget != null)
        {
            PerformAbsorption();
        }
    }

    void CheckForAbsorbableTargets()
    {
        // Limpiar indicador anterior
        if (currentIndicator != null && nearbyAbsorbableTarget == null)
        {
            Destroy(currentIndicator);
        }

        nearbyAbsorbableTarget = null;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(player.position, absorptionRange, absorptionTargetLayer);

        float closestDistance = Mathf.Infinity;
        IAbsorbable closestTarget = null;

        foreach (Collider2D col in colliders)
        {
            IAbsorbable absorbable = col.GetComponent<IAbsorbable>();
            if (absorbable != null && absorbable.CanBeAbsorbed())
            {
                float distance = Vector2.Distance(player.position, col.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = absorbable;
                }
            }
        }

        nearbyAbsorbableTarget = closestTarget;

        // Crear indicador visual sobre el enemigo
        if (nearbyAbsorbableTarget != null)
        {
            MonoBehaviour targetMono = nearbyAbsorbableTarget as MonoBehaviour;
            if (targetMono != null && currentIndicator == null)
            {
                CreateAbsorptionIndicator(targetMono.transform);
            }
            else if (currentIndicator != null && targetMono != null)
            {
                // Actualizar posición del indicador
                currentIndicator.transform.position = targetMono.transform.position + new Vector3(0, 1.5f, 0);
            }
        }
        else if (currentIndicator != null)
        {
            Destroy(currentIndicator);
        }

        UpdateUI();
    }

    void CreateAbsorptionIndicator(Transform target)
    {
        if (absorptionIndicatorPrefab != null)
        {
            currentIndicator = Instantiate(absorptionIndicatorPrefab, target.position + new Vector3(0, 1.5f, 0), Quaternion.identity);
            currentIndicator.transform.SetParent(target);
        }
        else
        {
            // Crear indicador simple si no hay prefab
            currentIndicator = new GameObject("AbsorptionIndicator");
            currentIndicator.transform.position = target.position + new Vector3(0, 1.5f, 0);
            currentIndicator.transform.SetParent(target);

            // Crear Canvas para el texto "E"
            Canvas canvas = currentIndicator.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform rect = currentIndicator.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one * 0.01f;

            // Crear texto "E"
            GameObject textObj = new GameObject("E_Text");
            textObj.transform.SetParent(currentIndicator.transform, false);

            UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();
            text.text = "E";
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 100;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.cyan;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(100, 100);
            textRect.anchoredPosition = Vector2.zero;

            // Añadir efecto de pulsación
            StartCoroutine(PulseIndicator(currentIndicator.transform));
        }
    }

    System.Collections.IEnumerator PulseIndicator(Transform indicator)
    {
        Vector3 baseScale = indicator.localScale;
        while (indicator != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.2f;
            indicator.localScale = baseScale * pulse;
            yield return null;
        }
    }

    void PerformAbsorption()
    {
        if (nearbyAbsorbableTarget == null || playerAbilityHolder == null) return;

        GameObject targetObject = (nearbyAbsorbableTarget as MonoBehaviour)?.gameObject;
        if (targetObject == null) return;

        AbilityHolder targetAbilityHolder = targetObject.GetComponent<AbilityHolder>();
        if (targetAbilityHolder == null) return;

        Ability targetAbility = targetAbilityHolder.GetAbility();
        Ability playerAbility = playerAbilityHolder.GetAbility();

        // Intercambiar habilidades
        playerAbilityHolder.SetAbility(targetAbility);
        targetAbilityHolder.SetAbility(playerAbility);

        // Destruir indicador
        if (currentIndicator != null)
        {
            Destroy(currentIndicator);
        }

        // Efecto visual
        if (absorptionEffectPrefab != null)
        {
            GameObject effect = Instantiate(absorptionEffectPrefab,
                (player.position + targetObject.transform.position) / 2f,
                Quaternion.identity);
            Destroy(effect, 2f);
        }

        // Línea de conexión visual
        StartCoroutine(DrawAbsorptionBeam(player.position, targetObject.transform.position));

        nearbyAbsorbableTarget.OnAbsorbed();

        string message = targetAbility != null
            ? $"¡Absorbiste {targetAbility.abilityName}!"
            : "¡Transferiste tu habilidad!";
        Debug.Log(message);
    }

    System.Collections.IEnumerator DrawAbsorptionBeam(Vector3 from, Vector3 to)
    {
        LineRenderer line = new GameObject("AbsorptionBeam").AddComponent<LineRenderer>();
        line.startWidth = 0.1f;
        line.endWidth = 0.1f;
        line.positionCount = 2;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = absorptionBeamColor;
        line.endColor = absorptionBeamColor;

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / duration);
            Color color = absorptionBeamColor;
            color.a = alpha;
            line.startColor = color;
            line.endColor = color;

            line.SetPosition(0, from);
            line.SetPosition(1, to);

            yield return null;
        }

        Destroy(line.gameObject);
    }

    void UpdateUI()
    {
        if (absorptionPromptUI == null) return;

        bool showPrompt = nearbyAbsorbableTarget != null;
        absorptionPromptUI.SetActive(showPrompt);

        if (showPrompt && absorptionText != null)
        {
            GameObject targetObj = (nearbyAbsorbableTarget as MonoBehaviour)?.gameObject;
            AbilityHolder targetHolder = targetObj?.GetComponent<AbilityHolder>();

            string abilityName = targetHolder?.GetAbility()?.abilityName ?? "Nada";
            absorptionText.text = $"[E] Absorber: {abilityName}";
        }
    }

    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(player.position, absorptionRange);
        }
    }
}

// ============================================
// INTERFAZ PARA ENTIDADES ABSORBIBLES
// ============================================
public interface IAbsorbable
{
    bool CanBeAbsorbed();
    void OnAbsorbed();
}