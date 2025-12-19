using UnityEngine;

/// <summary>
/// Indicador de absorcion para enemigos - 100% Personalizable
/// </summary>
public class EnemyAbsorptionVisual : MonoBehaviour
{
    [Header("=== GENERAL ===")]
    public KeyCode key = KeyCode.E;
    public float urgentDist = 3f;

    [Header("=== POSICION ===")]
    [Range(0.5f, 4f)]
    public float indicatorHeight = 1.8f;
    [Range(0.005f, 0.02f)]
    public float indicatorScale = 0.01f;

    [Header("=== COLORES ===")]
    public Color mainColor = new Color(0.9f, 0.7f, 0.3f);
    public Color glowColor = new Color(1f, 0.5f, 0.2f);
    public Color keyBgColor = new Color(0.1f, 0.08f, 0.05f);

    [Header("=== TECLA ===")]
    [Range(20, 50)]
    public float keyBoxSize = 28f;
    [Range(10, 28)]
    public int keyFontSize = 16;

    [Header("=== TEXTO ===")]
    public string actionText = "Absorber";
    public bool showText = true;
    [Range(6, 20)]
    public int actionFontSize = 10;

    [Header("=== EFECTOS ===")]
    public bool showGlow = true;
    [Range(30, 80)]
    public float glowSize = 55f;
    public bool showRing = true;
    [Range(30, 70)]
    public float ringSize = 45f;

    [Header("=== ANIMACION ===")]
    public float pulseSpeed = 3f;
    public float floatSpeed = 1.5f;
    [Range(0, 10)]
    public float floatAmount = 5f;
    public float ringRotateSpeed = 20f;

    private Enemigo enemigo;
    private AbilityHolder abilityHolder;
    private AbsorptionIndicatorUI indicator;
    private bool shown = false;
    private Transform player;

    void Start()
    {
        enemigo = GetComponent<Enemigo>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        GameObject go = new GameObject("_AbsorbIndicator_" + name);
        indicator = go.AddComponent<AbsorptionIndicatorUI>();

        // Pasar configuracion
        indicator.height = indicatorHeight;
        indicator.scale = indicatorScale;
        indicator.mainColor = mainColor;
        indicator.glowColor = glowColor;
        indicator.keyBgColor = keyBgColor;
        indicator.keyBoxSize = keyBoxSize;
        indicator.keyFontSize = keyFontSize;
        indicator.actionText = actionText;
        indicator.showText = showText;
        indicator.actionFontSize = actionFontSize;
        indicator.showGlow = showGlow;
        indicator.glowSize = glowSize;
        indicator.showRing = showRing;
        indicator.ringSize = ringSize;
        indicator.pulseSpeed = pulseSpeed;
        indicator.floatSpeed = floatSpeed;
        indicator.floatAmount = floatAmount;
        indicator.ringRotateSpeed = ringRotateSpeed;
    }

    void Update()
    {
        if (enemigo == null) return;

        if (abilityHolder == null)
        {
            abilityHolder = GetComponent<AbilityHolder>();
            if (abilityHolder == null) return;
        }

        bool canAbsorb = enemigo.CanBeAbsorbed();

        if (canAbsorb && !shown)
        {
            indicator.Show(transform, key);
            shown = true;
        }
        else if (!canAbsorb && shown)
        {
            indicator.Hide();
            shown = false;
        }

        if (shown && player != null)
        {
            float d = Vector2.Distance(transform.position, player.position);
            indicator.SetUrgent(d <= urgentDist);
        }
    }

    public void OnAbsorbed()
    {
        if (shown && indicator != null)
        {
            indicator.Hide();
            shown = false;
        }
    }

    void OnDisable()
    {
        if (shown && indicator != null)
        {
            indicator.Hide();
            shown = false;
        }
    }

    void OnDestroy()
    {
        if (indicator != null) Destroy(indicator.gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * indicatorHeight, 0.2f);
        Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, urgentDist);
    }
}