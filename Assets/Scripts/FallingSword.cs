using UnityEngine;
using System.Collections;

public class FallingSword : MonoBehaviour
{
    private float fallSpeed;
    private float damage;
    private bool hasHit = false;
    private Rigidbody2D rb;

    public void Initialize(float speed, float dmg)
    {
        fallSpeed = speed;
        damage = dmg;
        hasHit = false;
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) { rb.gravityScale = 0f; rb.linearVelocity = Vector2.zero; }
        // NO ROTAR - El prefab ya est� en la orientaci�n correcta
        // transform.rotation ya est� bien configurado en el prefab
    }

    void Update()
    {
        if (!hasHit)
        {
            // Caer hacia ABAJO
            transform.position += Vector3.down * fallSpeed * Time.deltaTime;

            // Auto-destruir si cae fuera del mundo
            if (transform.position.y < -50f)
            {
                Destroy(gameObject);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;

        if (collision.CompareTag("Player"))
        {
            PlayerCore player = collision.GetComponent<PlayerCore>();
            if (player != null)
            {
                player.TakeDamage((int)damage);
                Debug.Log("�Espada golpe� al jugador!");
            }
            hasHit = true;
            StartCoroutine(DestroyAfterDelay());
        }
        else if (collision.CompareTag("ground") || collision.gameObject.layer == LayerMask.NameToLayer("ground"))
        {
            hasHit = true;
            StartCoroutine(StickAndDestroy());
        }
    }

    IEnumerator StickAndDestroy()
    {
        fallSpeed = 0;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // Efecto de impacto
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