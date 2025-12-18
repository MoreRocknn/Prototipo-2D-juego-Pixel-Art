using UnityEngine;

/// <summary>
/// Añade este componente a tus enemigos para mostrar el indicador
/// visual cuando pueden ser absorbidos (estado crítico + tienen habilidad)
/// Se oculta automáticamente cuando el enemigo es absorbido
/// </summary>
public class EnemyAbsorptionVisual : MonoBehaviour
{
    [Header("Configuración")]
    public float urgentDistance = 3f;
    public KeyCode absorptionKey = KeyCode.E;

    private Enemigo enemigo;
    private AbilityHolder abilityHolder;
    private AbsorptionIndicatorUI indicator;
    private bool indicatorShown = false;
    private Transform player;

    void Start()
    {
        enemigo = GetComponent<Enemigo>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Crear el indicador visual
        GameObject indicatorObj = new GameObject("AbsorptionIndicatorUI_" + gameObject.name);
        indicator = indicatorObj.AddComponent<AbsorptionIndicatorUI>();

        Debug.Log($"[AbsorptionVisual] Iniciado para {gameObject.name}");
    }

    void Update()
    {
        if (enemigo == null) return;

        // Buscar AbilityHolder cada frame hasta encontrarlo
        if (abilityHolder == null)
        {
            abilityHolder = GetComponent<AbilityHolder>();
            if (abilityHolder == null) return;
            Debug.Log($"[AbsorptionVisual] AbilityHolder encontrado en {gameObject.name}");
        }

        // Verificar si puede ser absorbido usando el método del Enemigo
        bool canBeAbsorbed = enemigo.CanBeAbsorbed();

        // Mostrar indicador cuando puede ser absorbido
        if (canBeAbsorbed && !indicatorShown)
        {
            Debug.Log($"[AbsorptionVisual] MOSTRANDO indicador - {gameObject.name}");
            indicator.Show(transform, absorptionKey);
            indicatorShown = true;
        }
        // Ocultar cuando ya no puede
        else if (!canBeAbsorbed && indicatorShown)
        {
            Debug.Log($"[AbsorptionVisual] OCULTANDO indicador - {gameObject.name}");
            indicator.Hide();
            indicatorShown = false;
        }

        // Modo urgente cuando el jugador está cerca
        if (indicatorShown && player != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            indicator.SetUrgent(distance <= urgentDistance);
        }
    }

    public void OnAbsorbed()
    {
        Debug.Log($"[AbsorptionVisual] OnAbsorbed() llamado - {gameObject.name}");
        if (indicatorShown && indicator != null)
        {
            indicator.Hide();
            indicatorShown = false;
        }
    }

    void OnDisable()
    {
        if (indicatorShown && indicator != null)
        {
            indicator.Hide();
            indicatorShown = false;
        }
    }

    void OnDestroy()
    {
        if (indicator != null)
        {
            Destroy(indicator.gameObject);
        }
    }
}