using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed;
    private int damage;
    private bool canBounce;
    private int bounces = 0;
    private int maxBounces = 2; // Límite de rebotes
    private float lifeTime = 6f; // Tiempo de vida un poco más largo

    // Variable para saber si ya golpeamos (evita golpear 2 veces en el mismo frame)
    private bool hasHit = false;

    public void Initialize(Vector2 dir, float spd, int dmg, bool bounce)
    {
        direction = dir.normalized; // Asegurar que la dirección tenga longitud 1
        speed = spd;
        damage = dmg;
        canBounce = bounce;

        // --- CORRECCIÓN FÍSICA ---

        // 1. Añadir Collider (Trigger)
        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<CircleCollider2D>();
        }
        col.isTrigger = true; // Importante: Atraviesa otros enemigos, solo choca con Jugador/Pared
        col.radius = 0.25f;   // Hitbox ajustado

        // 2. Añadir Rigidbody (Kinematic)
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        // CORREGIDO: Forma moderna de hacerlo en Unity 2020+
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Evita que atraviese paredes rápidas

        // Autodestrucción por tiempo
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        // Mover el proyectil
        transform.Translate(direction * speed * Time.deltaTime, Space.World);

        // Opcional: Rotar el proyectil hacia donde mira
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle - 90, Vector3.forward);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return; // Seguridad para no procesar doble colisión

        // 1. CHOQUE CON JUGADOR
        if (other.CompareTag("Player"))
        {
            MainChar playerScript = other.GetComponent<MainChar>();
            if (playerScript != null)
            {
                playerScript.TakeDamage(damage);
                hasHit = true;
                Destroy(gameObject); // Impacto directo mata el proyectil
            }
        }
        // 2. CHOQUE CON PAREDES O SUELO
        // IMPORTANTE: Esto verifica si el objeto NO es el Boss, ni otro proyectil, ni un trigger invisible
        else if (!other.CompareTag("Enemy") && !other.isTrigger)
        {
            // Intentamos rebotar si está habilitado
            if (canBounce && bounces < maxBounces)
            {
                // Obtenemos la normal de la superficie (dirección perpendicular)
                // Hacemos un pequeño Raycast hacia adelante para saber cómo rebotar
                RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, 1f, LayerMask.GetMask("Ground", "Default", "Wall"));

                if (hit.collider != null)
                {
                    // Rebote físico real
                    direction = Vector2.Reflect(direction, hit.normal);
                }
                else
                {
                    // Rebote de emergencia (invertir si falló el raycast)
                    direction = -direction;
                }

                bounces++;
                speed *= 0.8f; // Pierde un poco de velocidad al rebotar
            }
            else
            {
                // Si no rebota, se destruye contra la pared
                hasHit = true;
                Destroy(gameObject);
            }
        }
    }
}