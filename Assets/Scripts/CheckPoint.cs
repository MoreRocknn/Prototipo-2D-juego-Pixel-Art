using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CheckPoint : MonoBehaviour
{
    public bool isActivated = false;

    [Header("=== VISUALES ===")]
    public GameObject inactiveVisual;
    public GameObject activeVisual;
    public ParticleSystem activationEffect;
    public ParticleSystem restEffect;

    [Header("=== AUDIO ===")]
    public AudioClip activationSound;
    public AudioClip restSound;
    public AudioClip refillSound;
    public AudioClip menuOpenSound;
    public AudioClip menuSelectSound;
    private AudioSource audioSource;

    [Header("=== INTERACCION ===")]
    public KeyCode interactKey = KeyCode.E;
    public bool canRestHere = true;

    [Header("=== DESCANSO ===")]
    public float restDuration = 2f;
    public bool resetEnemiesOnRest = true;
    public bool healOnRest = true;
    public bool refillVialsOnRest = true;

    [Header("=== PROMPT UI ===")]
    public Canvas promptCanvas;
    public GameObject promptPanel;
    public TextMeshProUGUI promptKeyText;
    public TextMeshProUGUI promptActionText;

    [Header("=== CINEMATICA ===")]
    public bool useCinematic = true;
    [Range(2f, 8f)] public float zoomSize = 4.5f;
    [Range(0.1f, 1f)] public float zoomDuration = 0.35f;

    [Header("=== PAUSA ===")]
    public bool freezeWorld = true;

    [Header("=== MENU UI ===")]
    public Canvas menuCanvas;
    public Image overlayImage;
    public GameObject menuPanel;
    public TextMeshProUGUI menuTitle;
    public Button restButton;
    public Button exitButton;
    public TextMeshProUGUI restButtonText;
    public TextMeshProUGUI exitButtonText;

    [Header("=== COLORES ===")]
    public Color normalButtonColor = new Color(0.8f, 0.75f, 0.65f);
    public Color selectedButtonColor = new Color(0.76f, 0.6f, 0.23f);

    private bool playerNearby = false;
    private bool menuActive = false;
    private bool resting = false;
    private bool menuLocked = false;

    private GameObject player;
    // FIX: PlayerCore + PlayerHealth en vez de MainChar
    private PlayerCore playerCore;
    private PlayerHealth playerHealth;
    private Rigidbody2D playerRb;
    private HealingSystem healingSys;
    private Vector3 savedPlayerPos;
    private float savedTimeScale = 1f;

    private Camera cam;
    private float savedCamSize;
    private Vector3 savedCamPos;
    private int selectedBtn = 0;

    void Start()
    {
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        cam = Camera.main;
        UpdateVisuals();
        SetupUI();
    }

    void SetupUI()
    {
        if (promptCanvas)
        {
            promptCanvas.gameObject.SetActive(false);
            if (promptKeyText) promptKeyText.text = interactKey.ToString();
            if (promptActionText) promptActionText.text = "Interactuar";
        }
        if (menuCanvas)
        {
            menuCanvas.gameObject.SetActive(false);
            if (restButton) { restButton.onClick.RemoveAllListeners(); restButton.onClick.AddListener(DoRest); }
            if (exitButton) { exitButton.onClick.RemoveAllListeners(); exitButton.onClick.AddListener(DoExit); }
            if (menuPanel && !menuPanel.GetComponent<CanvasGroup>()) menuPanel.AddComponent<CanvasGroup>();
        }
    }

    void Update()
    {
        if (menuLocked)
        {
            if (player != null)
            {
                player.transform.position = savedPlayerPos;
                if (playerRb != null) { playerRb.linearVelocity = Vector2.zero; playerRb.angularVelocity = 0f; }
            }

            if (menuActive && !resting)
            {
                if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) SelectBtn(0);
                if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) SelectBtn(1);
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)) PressBtn(selectedBtn);
                if (Input.GetKeyDown(KeyCode.Escape)) DoExit();
            }
            return;
        }

        if (playerNearby && isActivated && canRestHere)
            if (Input.GetKeyDown(interactKey)) OpenMenu();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (menuLocked || !other.CompareTag("Player")) return;
        playerNearby = true;
        CachePlayer(other.gameObject);
        if (!isActivated) Activate();
        if (isActivated && canRestHere) ShowPrompt();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (menuLocked || !other.CompareTag("Player")) return;
        playerNearby = false;
        HidePrompt();
    }

    void CachePlayer(GameObject p)
    {
        player = p;
        // FIX: PlayerCore + PlayerHealth en vez de MainChar
        playerCore = p.GetComponent<PlayerCore>();
        playerHealth = p.GetComponent<PlayerHealth>();
        playerRb = p.GetComponent<Rigidbody2D>();
        healingSys = p.GetComponent<HealingSystem>();
    }

    void Activate()
    {
        isActivated = true;
        GameManager.Instance?.SetCheckpoint(transform.position);
        UpdateVisuals();
        if (activationEffect) activationEffect.Play();
        if (audioSource && activationSound) audioSource.PlayOneShot(activationSound);
    }

    void UpdateVisuals()
    {
        if (inactiveVisual) inactiveVisual.SetActive(!isActivated);
        if (activeVisual) activeVisual.SetActive(isActivated);
    }

    void ShowPrompt() { if (promptCanvas) promptCanvas.gameObject.SetActive(true); }
    void HidePrompt() { if (promptCanvas) promptCanvas.gameObject.SetActive(false); }

    void SelectBtn(int idx)
    {
        selectedBtn = idx;
        if (restButtonText && exitButtonText)
        {
            restButtonText.color = (idx == 0) ? selectedButtonColor : normalButtonColor;
            exitButtonText.color = (idx == 1) ? selectedButtonColor : normalButtonColor;
        }
        if (restButton && exitButton)
        {
            restButton.transform.localScale = (idx == 0) ? Vector3.one * 1.1f : Vector3.one;
            exitButton.transform.localScale = (idx == 1) ? Vector3.one * 1.1f : Vector3.one;
        }
    }

    void PressBtn(int idx)
    {
        if (audioSource && menuSelectSound) audioSource.PlayOneShot(menuSelectSound);
        if (idx == 0) DoRest(); else DoExit();
    }

    void OpenMenu()
    {
        if (menuLocked) return;
        menuLocked = true;
        menuActive = true;

        if (player) savedPlayerPos = player.transform.position;
        if (cam) { savedCamSize = cam.orthographicSize; savedCamPos = cam.transform.position; }

        if (playerRb) { playerRb.linearVelocity = Vector2.zero; playerRb.angularVelocity = 0; playerRb.constraints = RigidbodyConstraints2D.FreezeAll; }
        // FIX: desactivar PlayerCore en vez de MainChar
        if (playerCore) playerCore.enabled = false;

        if (freezeWorld) { savedTimeScale = Time.timeScale; Time.timeScale = 0; }

        HidePrompt();
        if (audioSource && menuOpenSound) audioSource.PlayOneShot(menuOpenSound);
        if (menuCanvas) menuCanvas.gameObject.SetActive(true);
        selectedBtn = 0;
        SelectBtn(0);
        StartCoroutine(AnimMenu(true));
    }

    void CloseMenu() => StartCoroutine(AnimMenu(false));
    void DoRest() => StartCoroutine(RestSequence());
    void DoExit() => CloseMenu();

    IEnumerator AnimMenu(bool open)
    {
        float dur = open ? zoomDuration : zoomDuration * 0.6f;
        float t = 0;
        Color overlayColor = overlayImage ? overlayImage.color : Color.black;
        float targetAlpha = open ? 0.9f : 0f;
        CanvasGroup menuCG = menuPanel ? menuPanel.GetComponent<CanvasGroup>() : null;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = t / dur;
            if (!open) p = 1 - p;

            if (overlayImage) { Color c = overlayColor; c.a = targetAlpha * p; overlayImage.color = c; }
            if (menuCG) menuCG.alpha = p;
            if (useCinematic && cam)
            {
                cam.orthographicSize = Mathf.Lerp(savedCamSize, zoomSize, p);
                Vector3 tgt = new Vector3(transform.position.x, transform.position.y + 0.5f, savedCamPos.z);
                cam.transform.position = Vector3.Lerp(savedCamPos, tgt, p);
            }
            yield return null;
        }

        if (!open)
        {
            if (menuCanvas) menuCanvas.gameObject.SetActive(false);
            if (freezeWorld) Time.timeScale = savedTimeScale > 0 ? savedTimeScale : 1;
            if (playerRb) playerRb.constraints = RigidbodyConstraints2D.FreezeRotation;
            // FIX: reactivar PlayerCore
            if (playerCore) playerCore.enabled = true;
            menuActive = false;
            menuLocked = false;
            playerNearby = true;
            ShowPrompt();
        }
    }

    IEnumerator RestSequence()
    {
        resting = true;

        CanvasGroup menuCG = menuPanel ? menuPanel.GetComponent<CanvasGroup>() : null;
        if (menuCG)
        {
            float t = 0;
            while (t < 0.2f) { t += Time.unscaledDeltaTime; menuCG.alpha = 1 - t / 0.2f; yield return null; }
        }

        if (restEffect) restEffect.Play();
        if (audioSource && restSound) audioSource.PlayOneShot(restSound);
        RestUIManager.Instance?.ShowRestPanel(restDuration);

        yield return new WaitForSecondsRealtime(restDuration);

        GameManager.Instance?.SetCheckpoint(transform.position);

        // FIX: curar via PlayerHealth en vez de asignar directamente
        if (healOnRest && playerHealth != null)
            playerHealth.Heal(playerHealth.maxHealth); // cura vida completa

        if (refillVialsOnRest && healingSys) healingSys.RefillVials();
        if (resetEnemiesOnRest) EnemyManager.Instance?.RespawnAllEnemies();
        if (audioSource && refillSound) audioSource.PlayOneShot(refillSound);

        resting = false;
        CloseMenu();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = isActivated ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}