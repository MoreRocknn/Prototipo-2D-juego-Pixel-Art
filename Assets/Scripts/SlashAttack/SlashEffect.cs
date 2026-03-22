// ============================================================
// SlashEffect.cs — Efecto de slash blanco estilo Hollow Knight
//
// SETUP:
//   1. Crea un GameObject vacío llamado "SlashEffect"
//   2. Añade este script
//   3. Añade un SpriteRenderer (el sprite puede ser un quad blanco
//      o un sprite de slash que tengas)
//   4. Convierte el GameObject en Prefab
//   5. Asigna el prefab en PlayerCombat → Slash Effect Prefab
//
// El efecto se instancia en el AttackPoint del jugador,
// escala rápido y se desvanece.
// ============================================================
using UnityEngine;
using System.Collections;

public class SlashEffect : MonoBehaviour
{
    [Header("=== APARIENCIA ===")]
    public Color slashColor = new Color(1f, 1f, 1f, 0.85f);
    public Color slashColorLight = new Color(0.8f, 0.95f, 1f, 0.6f);

    [Header("=== TAMAÑO ===")]
    [Tooltip("Escala final del slash")]
    public Vector3 targetScale = new Vector3(2.5f, 1.2f, 1f);
    [Tooltip("Escala inicial (pequeño y luego crece)")]
    public Vector3 startScale = new Vector3(0.2f, 0.8f, 1f);

    [Header("=== TIMING ===")]
    [Tooltip("Tiempo en crecer hasta el tamaño máximo")]
    public float growDuration = 0.06f;
    [Tooltip("Tiempo en desvanecerse")]
    public float fadeDuration = 0.08f;

    [Header("=== ROTACIÓN ===")]
    [Tooltip("Rotación aleatoria máxima en grados")]
    public float randomRotation = 20f;

    // ── Privadas ──────────────────────────────────────────────
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 20;
    }

    // ── API Pública ───────────────────────────────────────────
    // direction: 1 = mirando derecha, -1 = izquierda
    public void Play(int direction = 1, float sizeMultiplier = 1f)
    {
        // Flip según dirección del jugador
        transform.localScale = new Vector3(
            startScale.x * direction,
            startScale.y,
            startScale.z
        );

        // Rotación aleatoria leve para que no sea siempre igual
        float rot = Random.Range(-randomRotation, randomRotation);
        transform.rotation = Quaternion.Euler(0f, 0f, rot);

        sr.color = slashColor;
        StartCoroutine(SlashSequence(direction, sizeMultiplier));
    }

    IEnumerator SlashSequence(int direction, float sizeMultiplier)
    {
        Vector3 finalScale = new Vector3(
            targetScale.x * sizeMultiplier * direction,
            targetScale.y * sizeMultiplier,
            1f
        );

        // ── 1. Crecer rápido ───────────────────────────────────
        float t = 0f;
        while (t < growDuration)
        {
            t += Time.unscaledDeltaTime; // unscaled: funciona durante hitstop
            float p = t / growDuration;
            // Ease out: empieza rápido, termina suave
            float ease = 1f - (1f - p) * (1f - p);
            transform.localScale = Vector3.Lerp(
                new Vector3(startScale.x * direction, startScale.y, 1f),
                finalScale,
                ease
            );
            yield return null;
        }
        transform.localScale = finalScale;

        // ── 2. Desvanecer ──────────────────────────────────────
        t = 0f;
        Color startColor = sr.color;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            float p = t / fadeDuration;
            sr.color = new Color(
                startColor.r, startColor.g, startColor.b,
                Mathf.Lerp(startColor.a, 0f, p)
            );
            yield return null;
        }

        Destroy(gameObject);
    }
}