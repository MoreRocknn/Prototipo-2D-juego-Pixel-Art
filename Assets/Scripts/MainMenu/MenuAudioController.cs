using UnityEngine;

// ============================================================
//  MenuAudioController.cs
//  Gestiona todos los sonidos del menú:
//    · Música de fondo en loop
//    · SFX de hover y selección de botones
//    · Stinger de apertura (campana, ruido de pergamino, etc.)
// ============================================================

[RequireComponent(typeof(AudioSource))]
public class MenuAudioController : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────
    [Header("Música")]
    [SerializeField] private AudioClip  musicLoop;
    [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.4f;

    [Header("SFX de UI")]
    [SerializeField] private AudioClip sfxHover;
    [SerializeField] private AudioClip sfxSelect;
    [SerializeField] private AudioClip sfxBack;
    [SerializeField] private AudioClip sfxIntroStinger;   // sonido al abrir el menú

    [Header("Fade de música")]
    [SerializeField] private float fadeInDuration  = 2f;
    [SerializeField] private float fadeOutDuration = 1f;

    // ── Privados ──────────────────────────────────────────────
    private AudioSource _musicSource;
    private AudioSource _sfxSource;

    // ── Ciclo de vida ─────────────────────────────────────────

    private void Awake()
    {
        // Usamos dos AudioSources: uno para música (loop) y otro para SFX (one-shot)
        _musicSource = GetComponent<AudioSource>();
        _musicSource.loop        = true;
        _musicSource.playOnAwake = false;
        _musicSource.volume      = 0f;
        _musicSource.clip        = musicLoop;

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.loop        = false;
        _sfxSource.playOnAwake = false;
        _sfxSource.volume      = 1f;
    }

    private void Start()
    {
        PlayIntroStinger();
        StartMusicWithFade();
    }

    // ── API pública ────────────────────────────────────────────

    public void PlayHover()  => PlaySFX(sfxHover,  0.6f);
    public void PlaySelect() => PlaySFX(sfxSelect, 1.0f);
    public void PlayBack()   => PlaySFX(sfxBack,   0.9f);

    /// <summary>Fade-out de la música antes de cambiar de escena.</summary>
    public void FadeOutMusic()
    {
        StartCoroutine(FadeRoutine(_musicSource, 0f, fadeOutDuration));
    }

    // ── Helpers privados ───────────────────────────────────────

    private void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;
        _sfxSource.PlayOneShot(clip, volume);
    }

    private void PlayIntroStinger()
    {
        if (sfxIntroStinger != null)
            _sfxSource.PlayOneShot(sfxIntroStinger);
    }

    private void StartMusicWithFade()
    {
        if (musicLoop == null) return;
        _musicSource.Play();
        StartCoroutine(FadeRoutine(_musicSource, musicVolume, fadeInDuration));
    }

    private System.Collections.IEnumerator FadeRoutine(
        AudioSource source, float targetVolume, float duration)
    {
        float startVolume = source.volume;
        float elapsed     = 0f;

        while (elapsed < duration)
        {
            elapsed        += Time.deltaTime;
            source.volume   = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        source.volume = targetVolume;
        if (targetVolume == 0f) source.Stop();
    }
}
