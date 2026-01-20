using UnityEngine;
using System.Collections.Generic;

// ============================================
// ENUM DE HABILIDADES
// ============================================
public enum AbilityType
{
    None,
    Dash,
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

    public int maxUses;
    public int currentUses;
    public bool limitedUses = false;

    public float postAbsorptionCooldown = 0f;
    public float timeAbsorbed = -999f;

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
        abilityColor = new Color(0.3f, 0.8f, 1f);

        maxUses = 999;
        currentUses = 999;
        limitedUses = false;

        dashCooldown = 1f;
        postAbsorptionCooldown = 0f;
    }

    public override bool CanUse(GameObject owner)
    {
        bool cooldownOK = Time.time - lastDashTime >= dashCooldown;
        bool usesOK = !limitedUses || currentUses > 0;
        return cooldownOK && usesOK;
    }

    public override void Execute(GameObject owner)
    {
        if (!CanUse(owner)) return;

        lastDashTime = Time.time;

        if (limitedUses)
        {
            currentUses--;
            Debug.Log($"Dash usado. Restantes: {currentUses}/{maxUses}");
        }
        else
        {
            Debug.Log($"Dash usado. Próximo disponible en {dashCooldown}s");
        }

        var dashExecutor = owner.GetComponent<IDashExecutor>();
        if (dashExecutor != null)
        {
            dashExecutor.PerformDash(dashForce, dashDuration);
        }
    }

    public override Ability Clone()
    {
        return new DashAbility
        {
            dashForce = this.dashForce,
            dashDuration = this.dashDuration,
            dashCooldown = this.dashCooldown,
            abilityColor = this.abilityColor,
            maxUses = this.maxUses,
            currentUses = this.currentUses,
            limitedUses = this.limitedUses,
            postAbsorptionCooldown = this.postAbsorptionCooldown
        };
    }

    public float GetCooldownRemaining()
    {
        float remaining = dashCooldown - (Time.time - lastDashTime);
        return Mathf.Max(0f, remaining);
    }
}

// ============================================
// INTERFACES
// ============================================
public interface IDashExecutor
{
    void PerformDash(float force, float duration);
}

public interface IAbsorbable
{
    bool CanBeAbsorbed();
    void OnAbsorbed();
}

public interface IResettable
{
    void ResetState();
    bool IsBoss { get; }
}

// ============================================
// COMPONENTE DE HABILIDAD (Holder)
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
        bool canUse = currentAbility != null && (!currentAbility.limitedUses || currentAbility.currentUses > 0);

        if (hasAbilityVisualActive && spriteRenderer != null && currentAbility != null && canUse)
        {
            float pulse = Mathf.PingPong(Time.time * 2f, 0.3f);
            spriteRenderer.color = Color.Lerp(originalColor, currentAbility.abilityColor, pulse);
        }
        else if (spriteRenderer != null && hasAbilityVisualActive && !canUse)
        {
            spriteRenderer.color = originalColor;
        }
    }

    public void SetAbility(Ability newAbility)
    {
        currentAbility = newAbility?.Clone();
        UpdateVisuals();
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

            if (abilityParticles != null)
            {
                abilityParticles.Play();
            }
        }
    }

    private void UpdateVisuals()
    {
        bool canUse = currentAbility != null && (!currentAbility.limitedUses || currentAbility.currentUses > 0);
        hasAbilityVisualActive = canUse;

        if (abilityAuraEffect != null)
        {
            abilityAuraEffect.SetActive(hasAbilityVisualActive);
        }

        if (spriteRenderer != null && !hasAbilityVisualActive)
        {
            spriteRenderer.color = originalColor;
        }
    }

    public float GetAbsorptionCooldownRemaining()
    {
        if (currentAbility == null) return 0f;

        float remaining = currentAbility.postAbsorptionCooldown - (Time.time - currentAbility.timeAbsorbed);
        return Mathf.Max(0f, remaining);
    }
}

// ============================================
// MANAGER PRINCIPAL CON SISTEMA DE DASH PERMANENTE
// ============================================
public class AbilityAbsorptionManager : MonoBehaviour
{
    public static AbilityAbsorptionManager Instance { get; private set; }

    [Header("Configuración de Absorción")]
    public KeyCode absorbKey = KeyCode.E;
    public float absorptionRange = 2f;
    public LayerMask absorptionTargetLayer;

    [Header("Sistema Cinematográfico")]
    public bool useCinematicMode = true;
    public CinematicAbsorptionSystem cinematicSystem;

    [Header("Efectos Visuales (No Cinemáticos)")]
    public GameObject absorptionEffectPrefab;
    public Color absorptionBeamColor = Color.cyan;

    [Header("UI Simple (Si no hay cinemática)")]
    public GameObject absorptionPromptUI;
    public UnityEngine.UI.Text absorptionText;

    [Header("Indicador Visual")]
    public GameObject absorptionIndicatorPrefab;
    private GameObject currentIndicator;

    private Transform player;
    private AbilityHolder playerAbilityHolder;
    private IAbsorbable nearbyAbsorbableTarget;
    private Transform nearbyAbsorbableTransform;

    private List<IResettable> resettableEnemies = new List<IResettable>();

    private float checkTimer = 0f;
    private float checkInterval = 0.2f;
    private Collider2D[] hitCollidersBuffer = new Collider2D[10];
    private Font uiFont;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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

            // ========================================
            // RESTAURAR DASH PERMANENTE AL INICIAR
            // ========================================
            RestorePermanentAbilities();
        }

        if (useCinematicMode && cinematicSystem == null)
        {
            cinematicSystem = FindFirstObjectByType<CinematicAbsorptionSystem>();

            if (cinematicSystem == null)
            {
                GameObject cinematicObj = new GameObject("CinematicAbsorptionSystem");
                cinematicSystem = cinematicObj.AddComponent<CinematicAbsorptionSystem>();
            }
        }

        if (absorptionPromptUI != null)
        {
            absorptionPromptUI.SetActive(false);
        }

        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    /// <summary>
    /// Restaura las habilidades permanentes del jugador al iniciar el juego
    /// </summary>
    void RestorePermanentAbilities()
    {
        if (GameManager.Instance != null && GameManager.Instance.HasPermanentDash())
        {
            // Crear y asignar el Dash permanente
            DashAbility permanentDash = new DashAbility();
            permanentDash.limitedUses = false;
            permanentDash.dashCooldown = 3f;

            playerAbilityHolder.SetAbility(permanentDash);
            Debug.Log("Dash permanente restaurado al jugador");
        }
    }

    public void RegisterResettable(IResettable enemy)
    {
        if (!resettableEnemies.Contains(enemy)) resettableEnemies.Add(enemy);
    }

    public void UnregisterResettable(IResettable enemy)
    {
        if (resettableEnemies.Contains(enemy)) resettableEnemies.Remove(enemy);
    }

    // ========================================
    // MODIFICADO: Ya NO quita el dash si es permanente
    // ========================================
    public void OnPlayerDeath()
    {
        Debug.Log("Resetting Game State...");

        // SOLO quitar habilidad si NO es permanente
        if (playerAbilityHolder != null)
        {
            bool hasPermanentDash = GameManager.Instance != null && GameManager.Instance.HasPermanentDash();

            if (!hasPermanentDash)
            {
                // No tiene dash permanente, se lo quitamos
                playerAbilityHolder.RemoveAbility();
            }
            else
            {
                // Tiene dash permanente, lo mantenemos (solo reseteamos cooldown si es necesario)
                Debug.Log("El jugador mantiene el Dash permanente después de morir");
            }
        }

        // Resetear enemigos (excepto bosses)
        for (int i = resettableEnemies.Count - 1; i >= 0; i--)
        {
            var enemy = resettableEnemies[i];
            if (enemy != null && !enemy.ToString().Equals("null"))
            {
                if (!enemy.IsBoss) enemy.ResetState();
            }
        }
    }

    void Update()
    {
        if (player == null) return;

        if (useCinematicMode && cinematicSystem != null && cinematicSystem.IsInCinematicMode())
        {
            return;
        }

        checkTimer -= Time.deltaTime;
        if (checkTimer <= 0)
        {
            CheckForAbsorbableTargets();
            checkTimer = checkInterval;
        }

        if (currentIndicator != null && nearbyAbsorbableTarget != null)
        {
            MonoBehaviour targetMono = nearbyAbsorbableTarget as MonoBehaviour;
            if (targetMono != null)
            {
                currentIndicator.transform.position = targetMono.transform.position + new Vector3(0, 1.5f, 0);
            }
        }

        if (Input.GetKeyDown(absorbKey) && nearbyAbsorbableTarget != null)
        {
            if (useCinematicMode && cinematicSystem != null)
            {
                cinematicSystem.StartCinematicMode(nearbyAbsorbableTransform, () =>
                {
                    PerformAbsorption();
                });
            }
            else
            {
                PerformAbsorption();
            }
        }
    }

    void CheckForAbsorbableTargets()
    {
        IAbsorbable newTarget = null;
        Transform newTargetTransform = null;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(absorptionTargetLayer);
        filter.useTriggers = Physics2D.queriesHitTriggers;

        int hitCount = Physics2D.OverlapCircle(player.position, absorptionRange, filter, hitCollidersBuffer);

        float closestDistance = Mathf.Infinity;

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = hitCollidersBuffer[i];
            if (col == null) continue;

            IAbsorbable absorbable = col.GetComponent<IAbsorbable>();
            if (absorbable != null && absorbable.CanBeAbsorbed())
            {
                float distance = Vector2.Distance(player.position, col.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    newTarget = absorbable;
                    newTargetTransform = col.transform;
                }
            }
        }

        if (newTarget != nearbyAbsorbableTarget)
        {
            nearbyAbsorbableTarget = newTarget;
            nearbyAbsorbableTransform = newTargetTransform;

            if (currentIndicator != null) Destroy(currentIndicator);

            if (nearbyAbsorbableTarget != null)
            {
                MonoBehaviour targetMono = nearbyAbsorbableTarget as MonoBehaviour;
                if (targetMono != null)
                {
                    CreateAbsorptionIndicator(targetMono.transform);
                }
            }
        }

        if (nearbyAbsorbableTarget == null && currentIndicator != null)
        {
            Destroy(currentIndicator);
        }

        UpdateUI();
    }

    void CreateAbsorptionIndicator(Transform target)
    {
        if (useCinematicMode && cinematicSystem != null && cinematicSystem.IsInCinematicMode())
        {
            return;
        }

        if (absorptionIndicatorPrefab != null)
        {
            currentIndicator = Instantiate(absorptionIndicatorPrefab, target.position + new Vector3(0, 1.5f, 0), Quaternion.identity);
        }
        else
        {
            currentIndicator = new GameObject("AbsorptionIndicator");
            currentIndicator.transform.position = target.position + new Vector3(0, 1.5f, 0);

            Canvas canvas = currentIndicator.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform rect = currentIndicator.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one * 0.01f;

            GameObject textObj = new GameObject("E_Text");
            textObj.transform.SetParent(currentIndicator.transform, false);

            UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();
            text.text = "E";
            text.font = uiFont;
            text.fontSize = 100;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.cyan;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(100, 100);
            textRect.anchoredPosition = Vector2.zero;

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

        // Asignar habilidad al jugador
        playerAbilityHolder.SetAbility(targetAbility);

        if (playerAbilityHolder.currentAbility != null)
        {
            playerAbilityHolder.currentAbility.limitedUses = false;
            playerAbilityHolder.currentAbility.maxUses = 999;
            playerAbilityHolder.currentAbility.currentUses = 999;

            if (playerAbilityHolder.currentAbility is DashAbility dashAbility)
            {
                dashAbility.dashCooldown = 3f;

                // ========================================
                // GUARDAR DASH COMO PERMANENTE
                // ========================================
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.UnlockPermanentDash();
                }
            }

            Debug.Log("Habilidad absorbida: Dash PERMANENTE con cooldown de 3 segundos.");
        }

        targetAbilityHolder.SetAbility(playerAbility);

        if (currentIndicator != null)
        {
            Destroy(currentIndicator);
        }

        if (!useCinematicMode || cinematicSystem == null)
        {
            if (absorptionEffectPrefab != null)
            {
                GameObject effect = Instantiate(absorptionEffectPrefab,
                    (player.position + targetObject.transform.position) / 2f,
                    Quaternion.identity);
                Destroy(effect, 2f);
            }

            StartCoroutine(DrawAbsorptionBeam(player.position, targetObject.transform.position));
        }

        nearbyAbsorbableTarget.OnAbsorbed();

        string message = targetAbility != null
            ? $"¡Absorbiste {targetAbility.abilityName} PERMANENTEMENTE!"
            : "¡Transferiste tu habilidad!";
        Debug.Log(message);

        nearbyAbsorbableTarget = null;
        nearbyAbsorbableTransform = null;
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

        if (useCinematicMode && cinematicSystem != null)
        {
            absorptionPromptUI.SetActive(false);
            return;
        }

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