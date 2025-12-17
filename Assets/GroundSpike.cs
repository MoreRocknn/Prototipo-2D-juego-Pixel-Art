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

        // Rotación correcta para que el spike apunte hacia arriba
        transform.rotation = Quaternion.identity;

        StartCoroutine(SpikeLifecycle());
    }

    IEnumerator SpikeLifecycle()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        // Efecto de advertencia parpadeante
        if (sr != null)
        {
            Color originalColor = sr.color;
            Color warningColor = new Color(1f, 0f, 0f, 0.5f); // Rojo semi-transparente

            for (int i = 0; i < 6; i++)
            {
                sr.color = warningColor;
                yield return new WaitForSeconds(0.1f);
                sr.color = originalColor;
                yield return new WaitForSeconds(0.1f);
            }

            // Restaurar color original
            sr.color = originalColor;
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