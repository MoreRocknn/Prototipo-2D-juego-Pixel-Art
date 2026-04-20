using UnityEngine;

/// <summary>
/// Proyectil disparado por EnemigoRanged.
/// Pon este componente en el prefab de bala junto a Rigidbody2D y Collider2D (Is Trigger = true).
/// </summary>
public class EnemyBullet : MonoBehaviour
{
    [Header("=== CONFIGURACIÓN ===")]
    public int damage = 1;
    public float lifetime = 4f;        // segundos antes de autodestruirse
    public LayerMask wallLayer;        // capas que destruyen la bala

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Impacta al jugador
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerCore>()?.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Impacta contra una pared o suelo
        if (((1 << other.gameObject.layer) & wallLayer) != 0)
        {
            Destroy(gameObject);
        }
    }
}