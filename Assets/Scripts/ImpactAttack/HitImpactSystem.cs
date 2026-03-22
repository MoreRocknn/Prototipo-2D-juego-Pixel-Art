// ============================================================
// HitImpactSystem.cs — Sistema de Game Feel estilo Dark Souls
// Compatible con Cinemachine 3.x (Unity 6)
//
// SETUP:
//   1. Crea un GameObject "GameFeel" y añade este script
//   2. Arrastra el CinemachineCamera al campo Cinemachine Camera
//   3. En el CinemachineCamera añade el componente:
//      "Cinemachine Basic Multi Channel Perlin"
//      y asigna un Noise Profile (ej: "Basic Multi Channel Perlin")
// ============================================================
using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class HitImpactSystem : MonoBehaviour
{
    public static HitImpactSystem Instance { get; private set; }

    public enum HitType { Light, Heavy, Critical, PlayerHit }

    [Header("=== HITSTOP ===")]
    public float lightHitstopDuration = 0.04f;
    public float heavyHitstopDuration = 0.08f;
    public float critHitstopDuration = 0.12f;
    public float playerHitHitstopDuration = 0.06f;

    [Header("=== CAMERA SHAKE ===")]
    [Tooltip("Arrastra aqui el CinemachineCamera")]
    public CinemachineCamera cinemachineCamera;

    public float lightShakeAmplitude = 0.5f;
    public float lightShakeFrequency = 10f;
    public float lightShakeDuration = 0.1f;

    [Space]
    public float heavyShakeAmplitude = 1.2f;
    public float heavyShakeFrequency = 12f;
    public float heavyShakeDuration = 0.2f;

    [Space]
    public float critShakeAmplitude = 2.5f;
    public float critShakeFrequency = 15f;
    public float critShakeDuration = 0.3f;

    [Space]
    public float playerHitAmplitude = 1.8f;
    public float playerHitFrequency = 8f;
    public float playerHitDuration = 0.25f;

    [Header("=== HIT FLASH ===")]
    public Color hitFlashColor = Color.white;
    public float hitFlashDuration = 0.06f;

    // ── Privadas ──────────────────────────────────────────────
    private CinemachineBasicMultiChannelPerlin noise;
    private Coroutine shakeCoroutine;
    private bool isHitstop = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    void Start()
    {
        if (cinemachineCamera != null)
            noise = cinemachineCamera.GetComponent<CinemachineBasicMultiChannelPerlin>();

        if (noise != null) { noise.AmplitudeGain = 0f; noise.FrequencyGain = 0f; }
    }

    // ── API Publica ───────────────────────────────────────────
    public void OnHit(HitType type, SpriteRenderer targetSprite = null)
    {
        float hitstop, amp, freq, dur;
        switch (type)
        {
            case HitType.Heavy:
                hitstop = heavyHitstopDuration; amp = heavyShakeAmplitude; freq = heavyShakeFrequency; dur = heavyShakeDuration; break;
            case HitType.Critical:
                hitstop = critHitstopDuration; amp = critShakeAmplitude; freq = critShakeFrequency; dur = critShakeDuration; break;
            case HitType.PlayerHit:
                hitstop = playerHitHitstopDuration; amp = playerHitAmplitude; freq = playerHitFrequency; dur = playerHitDuration; break;
            default:
                hitstop = lightHitstopDuration; amp = lightShakeAmplitude; freq = lightShakeFrequency; dur = lightShakeDuration; break;
        }

        if (!isHitstop && hitstop > 0f) StartCoroutine(DoHitstop(hitstop));
        if (amp > 0f)
        {
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(DoShake(amp, freq, dur));
        }
        if (targetSprite != null) StartCoroutine(DoHitFlash(targetSprite));
    }

    public void OnLightHit(SpriteRenderer sr = null) => OnHit(HitType.Light, sr);
    public void OnHeavyHit(SpriteRenderer sr = null) => OnHit(HitType.Heavy, sr);
    public void OnCritHit(SpriteRenderer sr = null) => OnHit(HitType.Critical, sr);
    public void OnPlayerHit(SpriteRenderer sr = null) => OnHit(HitType.PlayerHit, sr);

    // ── Hitstop ───────────────────────────────────────────────
    IEnumerator DoHitstop(float duration)
    {
        isHitstop = true;
        float saved = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = saved;
        isHitstop = false;
    }

    // ── Camera Shake ──────────────────────────────────────────
    IEnumerator DoShake(float amplitude, float frequency, float duration)
    {
        if (noise == null) yield break;
        noise.AmplitudeGain = amplitude;
        noise.FrequencyGain = frequency;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            noise.AmplitudeGain = Mathf.Lerp(amplitude, 0f, (elapsed / duration) * (elapsed / duration));
            yield return null;
        }
        noise.AmplitudeGain = 0f;
        noise.FrequencyGain = 0f;
    }

    // ── Hit Flash ─────────────────────────────────────────────
    IEnumerator DoHitFlash(SpriteRenderer sr)
    {
        if (sr == null) yield break;
        Color original = sr.color;
        sr.color = hitFlashColor;
        yield return new WaitForSecondsRealtime(hitFlashDuration);
        sr.color = original;
    }
}