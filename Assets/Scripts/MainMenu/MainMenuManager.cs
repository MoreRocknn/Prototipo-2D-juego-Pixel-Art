using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

// ============================================================
//  MainMenuManager.cs — adaptado a jerarquía actual
//
//  JERARQUÍA ESPERADA:
//  Canvas
//  └── Panel
//      └── ButtonList
//          ├── BtnNewGame   (Button + Text TMP)
//          ├── BtnSettings  (Button + Text TMP)
//          └── BtnQuit      (Button + Text TMP)
//  GameObject               (título del juego)
//      └── Text (TMP)
// ============================================================

public class MainMenuManager : MonoBehaviour
{
    [Header("─── Botones principales ───────────────────")]
    [SerializeField] private Button btnNewGame;
    [SerializeField] private Button btnSettings;
    [SerializeField] private Button btnQuit;

    [Header("─── Paneles opcionales ────────────────────")]
    [SerializeField] private GameObject panelSettings;  // null si no existe aún

    [Header("─── Escenas ────────────────────────────────")]
    [SerializeField] private string newGameScene = "GameScene";

    [Header("─── Animación de entrada ────────────────────")]
    [SerializeField] private float introDelay = 0.4f;
    [SerializeField] private float introStagger = 0.15f;

    private bool _inputLocked = false;

    private void Start()
    {
        // Oculta paneles secundarios si existen
        if (panelSettings != null) panelSettings.SetActive(false);

        // Listeners
        btnNewGame?.onClick.AddListener(OnNewGame);
        btnSettings?.onClick.AddListener(OnOpenSettings);
        btnQuit?.onClick.AddListener(OnQuit);

        // Animación de entrada
        StartCoroutine(IntroAnimation());
    }

    // ── Acciones ───────────────────────────────────────────────

    private void OnNewGame()
    {
        if (_inputLocked) return;
        _inputLocked = true;
        SceneManager.LoadScene(newGameScene);
    }

    private void OnOpenSettings()
    {
        if (_inputLocked) return;
        if (panelSettings != null)
            panelSettings.SetActive(true);
        else
            Debug.Log("Panel Settings no asignado en el Inspector.");
    }

    private void OnQuit()
    {
        if (_inputLocked) return;
        _inputLocked = true;
        StartCoroutine(QuitRoutine());
    }

    // ── Animación de entrada escalonada ───────────────────────

    private IEnumerator IntroAnimation()
    {
        Button[] buttons = { btnNewGame, btnSettings, btnQuit };

        // Oculta todos al inicio
        foreach (var btn in buttons)
        {
            if (btn == null) continue;
            var cg = btn.GetComponent<CanvasGroup>()
                     ?? btn.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
        }

        yield return new WaitForSeconds(introDelay);

        // Aparecen uno a uno con fade
        foreach (var btn in buttons)
        {
            if (btn == null) continue;
            var cg = btn.GetComponent<CanvasGroup>();
            StartCoroutine(FadeIn(cg, 0.35f));
            yield return new WaitForSeconds(introStagger);
        }
    }

    private IEnumerator FadeIn(CanvasGroup cg, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        cg.alpha = 1f;
    }

    private IEnumerator QuitRoutine()
    {
        yield return new WaitForSeconds(0.4f);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}