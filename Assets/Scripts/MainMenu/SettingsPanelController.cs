using UnityEngine;
using UnityEngine.UIElements;

// ============================================================
//  SettingsPanelController.cs
//  Controla el panel de ajustes: audio, gráficos y controles.
//  Lee y guarda los valores usando PlayerPrefs (o un
//  SettingsData ScriptableObject si prefieres).
// ============================================================

public class SettingsPanelController : MonoBehaviour
{
    // ── Claves de PlayerPrefs ──────────────────────────────────
    private const string KEY_MUSIC_VOL  = "MusicVolume";
    private const string KEY_SFX_VOL    = "SfxVolume";
    private const string KEY_FULLSCREEN = "Fullscreen";
    private const string KEY_RESOLUTION = "ResolutionIndex";

    // ── Elementos de UI ────────────────────────────────────────
    private Slider         _sliderMusic;
    private Slider         _sliderSfx;
    private Toggle         _toggleFullscreen;
    private DropdownField  _dropdownResolution;
    private Button         _btnApply;

    // ── Dependencias ──────────────────────────────────────────
    private MenuAudioController _audio;

    // ── Resoluciones disponibles ───────────────────────────────
    private static readonly string[] ResolutionLabels =
        { "1280 × 720", "1920 × 1080", "2560 × 1440", "3840 × 2160" };
    private static readonly (int w, int h)[] Resolutions =
        { (1280,720), (1920,1080), (2560,1440), (3840,2160) };

    // ── Ciclo de vida ─────────────────────────────────────────

    private void Awake()
    {
        var uiDoc = GetComponent<UIDocument>();
        var root  = uiDoc.rootVisualElement;

        _sliderMusic        = root.Q<Slider>("SliderMusic");
        _sliderSfx          = root.Q<Slider>("SliderSfx");
        _toggleFullscreen   = root.Q<Toggle>("ToggleFullscreen");
        _dropdownResolution = root.Q<DropdownField>("DropdownResolution");
        _btnApply           = root.Q<Button>("BtnApply");

        _audio = FindObjectOfType<MenuAudioController>();
    }

    private void Start()
    {
        PopulateResolutions();
        LoadSettings();
        RegisterCallbacks();
    }

    // ── Inicialización ─────────────────────────────────────────

    private void PopulateResolutions()
    {
        if (_dropdownResolution == null) return;
        _dropdownResolution.choices.Clear();
        foreach (var label in ResolutionLabels)
            _dropdownResolution.choices.Add(label);
    }

    private void LoadSettings()
    {
        if (_sliderMusic != null)
            _sliderMusic.value = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 0.4f);

        if (_sliderSfx != null)
            _sliderSfx.value = PlayerPrefs.GetFloat(KEY_SFX_VOL, 1.0f);

        if (_toggleFullscreen != null)
            _toggleFullscreen.value = PlayerPrefs.GetInt(KEY_FULLSCREEN, 1) == 1;

        if (_dropdownResolution != null)
            _dropdownResolution.index = PlayerPrefs.GetInt(KEY_RESOLUTION, 1);
    }

    // ── Registro de eventos ────────────────────────────────────

    private void RegisterCallbacks()
    {
        _sliderMusic?.RegisterValueChangedCallback(evt =>
        {
            // Ajuste en tiempo real del volumen de música
            AudioListener.volume = evt.newValue;  // simplificado
        });

        _btnApply?.RegisterCallback<ClickEvent>(_ => ApplySettings());
    }

    // ── Aplicar ajustes ────────────────────────────────────────

    private void ApplySettings()
    {
        float musicVol = _sliderMusic?.value ?? 0.4f;
        float sfxVol   = _sliderSfx?.value   ?? 1.0f;
        bool  fullscr  = _toggleFullscreen?.value ?? true;
        int   resIdx   = _dropdownResolution?.index ?? 1;

        // Guardar
        PlayerPrefs.SetFloat(KEY_MUSIC_VOL,  musicVol);
        PlayerPrefs.SetFloat(KEY_SFX_VOL,    sfxVol);
        PlayerPrefs.SetInt(KEY_FULLSCREEN,   fullscr ? 1 : 0);
        PlayerPrefs.SetInt(KEY_RESOLUTION,   resIdx);
        PlayerPrefs.Save();

        // Aplicar resolución y modo
        var (w, h) = Resolutions[Mathf.Clamp(resIdx, 0, Resolutions.Length - 1)];
        Screen.SetResolution(w, h, fullscr ? FullScreenMode.FullScreenWindow
                                           : FullScreenMode.Windowed);
    }
}
