using UnityEngine;
using System.Collections;

// ============================================
// ESPADA CAYENDO
// ============================================
public class FallingSword : MonoBehaviour
{
    private float fallSpeed;
    private float damage;
    private bool hasHit = false;

    public void Initialize(float speed, float dmg)
    {
        fallSpeed = speed;
        damage = dmg;
    }

    void Update()
    {
        if (!hasHit)
        {
            // CORREGIDO: Caer hacia ABAJO
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;

        if (collision.CompareTag("Player"))
        {
            MainChar player = collision.GetComponent<MainChar>();
            if (player != null)
            {
                player.TakeDamage((int)damage);
                Debug.Log("¡Espada golpeó al jugador!");
            }
            hasHit = true;
            StartCoroutine(DestroyAfterDelay());
        }
        else if (collision.CompareTag("ground") || collision.gameObject.layer == LayerMask.NameToLayer("ground"))
        {
            hasHit = true;
            // Clavar espada en el suelo
            StartCoroutine(StickAndDestroy());
        }
    }

    IEnumerator StickAndDestroy()
    {
        // Detener movimiento
        fallSpeed = 0;

        // Efecto visual de impacto
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            for (int i = 0; i < 3; i++)
            {
                sr.color = Color.white;
                yield return new WaitForSeconds(0.1f);
                sr.color = Color.gray;
                yield return new WaitForSeconds(0.1f);
            }
        }

        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }

    IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);
        Destroy(gameObject);
    }
}