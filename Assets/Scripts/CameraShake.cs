
using UnityEngine;
using System.Collections;

// ============================================
// SCRIPT DE CAMERA SHAKE
// ============================================
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private Vector3 originalPosition;
    private bool isShaking = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        originalPosition = transform.localPosition;
    }

    public void Shake(float duration, float magnitude, float frequency = 25f)
    {
        if (!isShaking)
        {
            StartCoroutine(ShakeCoroutine(duration, magnitude, frequency));
        }
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude, float frequency)
    {
        isShaking = true;
        float elapsed = 0f;
        Vector3 startPosition = transform.localPosition;

        while (elapsed < duration)
        {
            // Usar Perlin Noise para un shake más suave y natural
            float x = (Mathf.PerlinNoise(Time.time * frequency, 0f) - 0.5f) * 2f * magnitude;
            float y = (Mathf.PerlinNoise(0f, Time.time * frequency) - 0.5f) * 2f * magnitude;

            // Aplicar decay (reducción gradual del shake)
            float decay = 1f - (elapsed / duration);
            x *= decay;
            y *= decay;

            transform.localPosition = startPosition + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Volver suavemente a la posición original
        transform.localPosition = startPosition;
        isShaking = false;
    }

    // Método para shake instantáneo (útil para impactos fuertes)
    public void ShakeImpact(float magnitude)
    {
        if (!isShaking)
        {
            StartCoroutine(ShakeImpactCoroutine(magnitude));
        }
    }

    private IEnumerator ShakeImpactCoroutine(float magnitude)
    {
        isShaking = true;
        Vector3 startPosition = transform.localPosition;

        // Shake rápido e intenso
        float duration = 0.15f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            float decay = 1f - (elapsed / duration);
            transform.localPosition = startPosition + new Vector3(x * decay, y * decay, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = startPosition;
        isShaking = false;
    }
}
