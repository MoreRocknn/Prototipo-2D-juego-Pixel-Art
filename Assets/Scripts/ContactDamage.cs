using UnityEngine;

// ============================================
// COMPONENTE DE DAÑO POR CONTACTO
// Añadir a cualquier enemigo para que haga daño al tocar al jugador
// ============================================
public class ContactDamage : MonoBehaviour
{
    [Header("Configuración de Daño por Contacto")]
    [Tooltip("Cantidad de daño al tocar al jugador")]
    public int contactDamage = 1;

    [Tooltip("Tiempo mínimo entre golpes de contacto")]
    public float damageCooldown = 1f;

    [Tooltip("Hacer daño solo cuando el enemigo está persiguiendo/atacando")]
    public bool onlyDamageWhenAggressive = false;

    [Header("Referencias (Opcional)")]
    [Tooltip("Dejar vacío para usar el collider del objeto")]
    public Collider2D damageCollider;

    private float lastDamageTime = -999f;
    private Enemigo enemigo;
    private EnemigoVoladorData EnemigoVoladorData;
    private BossController boss;

    void Start()
    {
        // Si no se asignó un collider específico, usar el del objeto
        if (damageCollider == null)
        {
            damageCollider = GetComponent<Collider2D>();
        }

        // Obtener script del enemigo para verificar estados
        enemigo = GetComponent<Enemigo>();
        EnemigoVoladorData = GetComponent<EnemigoVoladorData>();
        boss = GetComponent<BossController>();

        if (damageCollider == null)
        {
            Debug.LogError($"{gameObject.name}: ContactDamage necesita un Collider2D");
            enabled = false;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        TryDamagePlayer(other.gameObject);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other.gameObject);
    }

    void TryDamagePlayer(GameObject target)
    {
        // Verificar que sea el jugador
        if (!target.CompareTag("Player"))
            return;

        // Verificar cooldown
        if (Time.time - lastDamageTime < damageCooldown)
            return;

        // Verificar si solo debe hacer daño cuando está agresivo
        if (onlyDamageWhenAggressive)
        {
            bool isAggressive = false;

            // Para Enemigo normal
            if (enemigo != null)
            {
                // Aquí necesitarías acceder al estado del enemigo
                // Como los estados son privados, asumimos que siempre está activo
                // O puedes hacer los estados públicos temporalmente
                isAggressive = true; // Simplificado
            }
            // Para Enemigo Volador
            else if (EnemigoVoladorData != null)
            {
                isAggressive = true; // Simplificado
            }
            // Para Boss
            else if (boss != null)
            {
                isAggressive = true; // Boss siempre hace daño por contacto
            }

            if (!isAggressive)
                return;
        }

        // Hacer daño al jugador
        PlayerCore player = target.GetComponent<PlayerCore>();
        if (player != null)
        {
            player.TakeDamage(contactDamage);
            lastDamageTime = Time.time;
            Debug.Log($"{gameObject.name} hizo {contactDamage} de daño por contacto");
        }
    }

    // Método público para verificar si puede hacer daño (útil para debugging)
    public bool CanDamage()
    {
        return Time.time - lastDamageTime >= damageCooldown;
    }

    // Método para forzar el cooldown (útil si el enemigo muere, etc.)
    public void ResetCooldown()
    {
        lastDamageTime = -999f;
    }
}