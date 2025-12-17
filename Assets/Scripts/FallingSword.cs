using UnityEngine;
using System.Collections;

public class FallingSword : MonoBehaviour
{
    private float fallSpeed;
    private float damage;
    private bool hasHit = false;

    public void Initialize(float speed, float dmg)
    {
        fallSpeed = speed;
        damage = dmg;

        // NO ROTAR - El prefab ya está en la orientación correcta
        // transform.rotation ya está bien configurado en el prefab
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