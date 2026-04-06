using UnityEngine;

public class FallingSword : MonoBehaviour
{
    private float speed;
    private int damage;
    private float stopY;
    private bool hasHit = false;
    private Rigidbody2D rb;

    public void Initialize(float _speed, int _damage, float _stopY)
    {
        speed = _speed;
        damage = _damage;
        stopY = _stopY - 1f; // Margen para atravesar un poco el suelo

        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic; // Importante para que no lo mueva la gravedad de Unity

        // CORRECCIÓN: Forzamos la rotación para que apunte hacia abajo (en 2D suele ser -90 o 0 dependiendo de tu sprite)
        // Si tu espada se ve horizontal, cambia el -90 por 0 o 180.

        // Movimiento directo hacia abajo
        rb.linearVelocity = Vector2.down * speed;
    }

    void Update()
    {
        // Si la espada cae por debajo del suelo sin chocar, se destruye
        if (!hasHit && transform.position.y < stopY)
        {
            Destroy(gameObject);
        }
    }

    // EL DAÑO: Asegúrate de que el Collider de la espada tenga marcado "Is Trigger"
    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        if (other.CompareTag("Player"))
        {
            // Buscamos el PlayerCore (que es el que gestiona la vida en tu proyecto)
            PlayerCore player = other.GetComponent<PlayerCore>();
            if (player != null)
            {
                player.TakeDamage(damage);
                hasHit = true;
                Destroy(gameObject);
            }
        }
        // Si choca con el suelo, se destruye
        else if (((1 << other.gameObject.layer) & LayerMask.GetMask("Ground")) != 0)
        {
            hasHit = true;
            Destroy(gameObject);
        }
    }
}