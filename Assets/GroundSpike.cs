using UnityEngine;
using System.Collections;

public class GroundSpike : MonoBehaviour
{
    private float damage;
    private bool hasDealtDamage = false;
    private float lifetime = 2f;

    public void Initialize(float dmg)
    {
        damage = dmg;

        // CORRECCIÓN DEFINITIVA: 
        // Tu sprite apunta HACIA ARRIBA (↑)
        // Para que quede HORIZONTAL (→), necesitamos rotar -90°
        transform.rotation = Quaternion.Euler(0, 0, 0);
        // Si apunta al lado contrario, cambia a 90f

        StartCoroutine(SpikeLifecycle());
    }

    IEnumerator SpikeLifecycle()
    {
        // Aparece instantáneamente (como las espadas)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            for (int i = 0; i < 3; i++)
            {
                sr.enabled = false;
                yield return new WaitForSeconds(0.05f);
                sr.enabled = true;
                yield return new WaitForSeconds(0.05f);
            }
        }

        // Esperar antes de destruir
        yield return new WaitForSeconds(lifetime);

        // Fade out rápido
        if (sr != null)
        {
            float elapsed = 0f;
            float fadeTime = 0.2f;
            Color startColor = sr.color;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
        }

        Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!hasDealtDamage && collision.CompareTag("Player"))
        {
            MainChar player = collision.GetComponent<MainChar>();
            if (player != null)
            {
                player.TakeDamage((int)damage);
                hasDealtDamage = true;
                Debug.Log("¡Pincho golpeó al jugador!");
            }
        }
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (!hasDealtDamage)
            {
                MainChar player = collision.GetComponent<MainChar>();
                if (player != null)
                {
                    player.TakeDamage((int)damage);
                    hasDealtDamage = true;
                }
            }
        }
    }
}