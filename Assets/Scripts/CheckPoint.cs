using UnityEngine;
using UnityEngine.UI;
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

    [Header("=== PROMPT - GENERAL ===")]
    public bool showPrompt = true;
    [Range(0.5f, 4f)]
    public float promptHeight = 1.8f;
    [Range(0.004f, 0.02f)]
    public float promptScale = 0.008f;

    [Header("=== PROMPT - TAMAÑOS ===")]
    [Range(10, 40)]
    public int promptKeyFontSize = 22;
    [Range(6, 24)]
    public int promptActionFontSize = 12;
    [Range(20, 60)]
    public float promptKeyBoxSize = 34f;

    [Header("=== PROMPT - TEXTOS ===")]
    public string promptActionText = "Interactuar";

    [Header("=== PROMPT - COLORES ===")]
    public Color promptKeyColor = new Color(0.76f, 0.6f, 0.23f);
    public Color promptKeyBgColor = new Color(0.12f, 0.1f, 0.06f);
    public Color promptTextColor = new Color(0.76f, 0.6f, 0.23f);
    public Color promptBgColor = new Color(0, 0, 0, 0.85f);
    public Color promptBorderColor = new Color(0.4f, 0.32f, 0.12f);

    [Header("=== CINEMATICA ===")]
    public bool useCinematic = true;
    [Range(2f, 8f)]
    public float zoomSize = 4.5f;
    [Range(0.1f, 1f)]
    public float zoomDuration = 0.35f;

    [Header("=== PAUSA ===")]
    public bool freezeWorld = true;

    [Header("=== MENU - FONDO ===")]
    [Range(0f, 1f)]
    public float overlayDarkness = 0.9f;
    public Color overlayColor = new Color(0, 0, 0, 0.9f);
    public bool showVignette = true;
    [Range(0f, 1f)]
    public float vignetteIntensity = 0.6f;

    [Header("=== MENU - PANEL ===")]
    [Range(200, 500)]
    public float menuWidth = 320f;
    [Range(150, 400)]
    public float menuHeight = 200f;

    [Header("=== MENU - TITULO ===")]
    public string menuTitleText = "ALTAR";
    [Range(20, 50)]
    public int menuTitleFontSize = 36;
    public Color menuTitleColor = new Color(0.76f, 0.6f, 0.23f);

    [Header("=== MENU - LINEAS DECORATIVAS ===")]
    public bool showDecoLines = true;
    public Color decoLineColor = new Color(0.76f, 0.6f, 0.23f);
    [Range(100, 250)]
    public float decoLineWidth = 150f;

    [Header("=== MENU - BOTONES ===")]
    public string restButtonText = "Descansar";
    public string exitButtonText = "Salir";
    [Range(14, 40)]
    public int buttonFontSize = 26;
    public Color buttonTextColor = new Color(0.8f, 0.75f, 0.65f);
    public Color buttonTextSelectedColor = new Color(0.76f, 0.6f, 0.23f);
    [Range(30, 80)]
    public float buttonSpacing = 50f;

    [Header("=== MENU - FLECHAS ===")]
    public bool showArrows = true;
    [Range(14, 40)]
    public int arrowFontSize = 26;
    public Color arrowColor = new Color(0.76f, 0.6f, 0.23f);
    [Range(50, 150)]
    public float arrowOffset = 100f;

    // === ESTADO INTERNO ===
    private bool playerNearby = false;
    private bool menuActive = false;
    private bool resting = false;
    private bool menuLocked = false; // BLOQUEO TOTAL

    private GameObject player;
    private MainChar playerCtrl;
    private Rigidbody2D playerRb;
    private HealingSystem healingSys;
    private Vector3 savedPlayerPos;
    private float savedTimeScale = 1f;

    // UI
    private Canvas promptCanvas;
    private CanvasGroup promptCG;
    private Canvas menuCanvas;
    private Image overlay;
    private CanvasGroup menuCG;
    private int selectedBtn = 0;
    private Text[] btnTexts;
    private GameObject[] arrowsL, arrowsR;

    // Camara
    private Camera cam;
    private float savedCamSize;
    private Vector3 savedCamPos;

    private Font gameFont;

    void Start()
    {
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        cam = Camera.main;
        gameFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        CleanOld();
        UpdateVisuals();

        if (showPrompt) BuildPrompt();
        BuildMenu();
    }

    void CleanOld()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform c = transform.GetChild(i);
            if (c.gameObject != inactiveVisual && c.gameObject != activeVisual &&
                c.GetComponent<ParticleSystem>() == null)
            {
                string n = c.name.ToLower();
                if (n.Contains("text") || n.Contains("prompt") || n.Contains("canvas") ||
                    n.Contains("ui") || n.Contains("tmp"))
                {
                    DestroyImmediate(c.gameObject);
                }
            }
        }
    }

    void Update()
    {
        // === BLOQUEO TOTAL: Si menu activo, NADA puede interrumpir ===
        if (menuLocked)
        {
            // Forzar posicion del jugador
            if (player != null)
            {
                player.transform.position = savedPlayerPos;
                if (playerRb != null)
                {
                    playerRb.linearVelocity = Vector2.zero;
                    playerRb.angularVelocity = 0f;
                }
            }

            // Solo procesar input del menu si no estamos en animacion de descanso
            if (menuActive && !resting)
            {
                if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.LeftArrow))
                    SelectBtn(selectedBtn - 1);
                if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.RightArrow))
                    SelectBtn(selectedBtn + 1);
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space))
                    PressBtn(selectedBtn);
                if (Input.GetKeyDown(KeyCode.Escape))
                    DoExit();
            }

            return; // NO PROCESAR NADA MAS
        }

        // Menu no activo - permitir abrir
        if (playerNearby && isActivated && canRestHere)
        {
            if (Input.GetKeyDown(interactKey))
                OpenMenu();
        }
    }

    // === TRIGGERS - Solo funcionan si menu NO esta bloqueado ===
    void OnTriggerEnter2D(Collider2D other)
    {
        if (menuLocked) return; // IGNORAR
        if (!other.CompareTag("Player")) return;

        playerNearby = true;
        CachePlayer(other.gameObject);

        if (!isActivated) Activate();
        if (isActivated && canRestHere) ShowPrompt();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (menuLocked) return; // IGNORAR
        if (!other.CompareTag("Player")) return;

        playerNearby = false;
        HidePrompt();
    }

    void CachePlayer(GameObject p)
    {
        player = p;
        playerCtrl = p.GetComponent<MainChar>();
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

    // ===== PROMPT =====
    void BuildPrompt()
    {
        var go = new GameObject("_Prompt");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.up * promptHeight;

        promptCanvas = go.AddComponent<Canvas>();
        promptCanvas.renderMode = RenderMode.WorldSpace;
        promptCanvas.sortingOrder = 100;

        promptCG = go.AddComponent<CanvasGroup>();
        promptCG.alpha = 0;

        var crt = go.GetComponent<RectTransform>();
        crt.sizeDelta = new Vector2(120, 75);
        crt.localScale = Vector3.one * promptScale;

        // Fondo
        var bg = MakeImg(go.transform, "bg", new Vector2(120, 75), Vector2.zero, promptBgColor);
        bg.AddComponent<Outline>().effectColor = promptBorderColor;

        // Caja tecla
        var key = MakeImg(go.transform, "key", new Vector2(promptKeyBoxSize, promptKeyBoxSize), new Vector2(0, 12), promptKeyBgColor);
        var ko = key.AddComponent<Outline>();
        ko.effectColor = promptKeyColor;
        ko.effectDistance = new Vector2(2, -2);

        // Texto tecla
        var kt = MakeTxt(key.transform, "kt", interactKey.ToString(), promptKeyFontSize, promptKeyColor);
        var ktr = kt.GetComponent<RectTransform>();
        ktr.anchorMin = Vector2.zero;
        ktr.anchorMax = Vector2.one;
        ktr.offsetMin = ktr.offsetMax = Vector2.zero;

        // Texto accion
        var at = MakeTxt(go.transform, "at", promptActionText, promptActionFontSize, promptTextColor);
        at.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -24);
        at.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 25);

        promptCanvas.gameObject.SetActive(false);
    }

    void ShowPrompt()
    {
        if (!showPrompt || !promptCanvas) return;
        promptCanvas.gameObject.SetActive(true);
        StopCoroutine("FadePrompt");
        StartCoroutine(FadePrompt(true));
    }

    void HidePrompt()
    {
        if (!promptCanvas) return;
        StopCoroutine("FadePrompt");
        StartCoroutine(FadePrompt(false));
    }

    IEnumerator FadePrompt(bool show)
    {
        float target = show ? 1 : 0;
        while (Mathf.Abs(promptCG.alpha - target) > 0.01f)
        {
            promptCG.alpha = Mathf.MoveTowards(promptCG.alpha, target, Time.unscaledDeltaTime * 5);
            yield return null;
        }
        promptCG.alpha = target;
        if (!show) promptCanvas.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (promptCanvas && promptCanvas.gameObject.activeSelf && cam)
            promptCanvas.transform.rotation = cam.transform.rotation;
    }

    // ===== MENU =====
    void BuildMenu()
    {
        var go = new GameObject("_Menu");
        menuCanvas = go.AddComponent<Canvas>();
        menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        menuCanvas.sortingOrder = 500;

        var cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        go.AddComponent<GraphicRaycaster>();

        // Overlay
        var ov = MakeImg(go.transform, "ov", Vector2.zero, Vector2.zero, Color.clear);
        overlay = ov.GetComponent<Image>();
        var ovr = ov.GetComponent<RectTransform>();
        ovr.anchorMin = Vector2.zero;
        ovr.anchorMax = Vector2.one;
        ovr.offsetMin = ovr.offsetMax = Vector2.zero;

        if (showVignette)
        {
            var vig = MakeImg(ov.transform, "vig", Vector2.zero, Vector2.zero, new Color(0, 0, 0, vignetteIntensity));
            vig.GetComponent<Image>().sprite = MakeVignette();
            var vr = vig.GetComponent<RectTransform>();
            vr.anchorMin = Vector2.zero;
            vr.anchorMax = Vector2.one;
            vr.offsetMin = vr.offsetMax = Vector2.zero;
        }

        // Container
        var cont = new GameObject("cont");
        cont.transform.SetParent(go.transform, false);
        var cr = cont.AddComponent<RectTransform>();
        cr.sizeDelta = new Vector2(menuWidth, menuHeight);

        menuCG = cont.AddComponent<CanvasGroup>();
        menuCG.alpha = 0;

        // Deco lines
        if (showDecoLines)
        {
            MakeDecoLine(cont.transform, menuHeight * 0.32f);
            MakeDecoLine(cont.transform, menuHeight * 0.15f);
        }

        // Titulo
        var title = MakeTxt(cont.transform, "title", menuTitleText, menuTitleFontSize, menuTitleColor);
        title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, menuHeight * 0.24f);
        title.GetComponent<RectTransform>().sizeDelta = new Vector2(menuWidth, 50);

        // Botones
        btnTexts = new Text[2];
        arrowsL = new GameObject[2];
        arrowsR = new GameObject[2];

        MakeBtn(cont.transform, 0, restButtonText, 0);
        MakeBtn(cont.transform, 1, exitButtonText, -buttonSpacing);

        menuCanvas.gameObject.SetActive(false);
    }

    void MakeDecoLine(Transform p, float y)
    {
        var line = new GameObject("line");
        line.transform.SetParent(p, false);
        var lr = line.AddComponent<RectTransform>();
        lr.anchoredPosition = new Vector2(0, y);
        lr.sizeDelta = new Vector2(decoLineWidth + 30, 12);

        MakeImg(line.transform, "c", new Vector2(decoLineWidth, 2), Vector2.zero, decoLineColor);

        var dl = MakeImg(line.transform, "dl", new Vector2(8, 8), new Vector2(-decoLineWidth / 2 - 10, 0), decoLineColor);
        dl.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0, 0, 45);

        var dr = MakeImg(line.transform, "dr", new Vector2(8, 8), new Vector2(decoLineWidth / 2 + 10, 0), decoLineColor);
        dr.GetComponent<RectTransform>().localRotation = Quaternion.Euler(0, 0, 45);
    }

    void MakeBtn(Transform p, int idx, string txt, float y)
    {
        var btn = new GameObject("btn" + idx);
        btn.transform.SetParent(p, false);
        var br = btn.AddComponent<RectTransform>();
        br.sizeDelta = new Vector2(250, 45);
        br.anchoredPosition = new Vector2(0, y - 25);

        if (showArrows)
        {
            arrowsL[idx] = MakeTxt(btn.transform, "al", ">", arrowFontSize, arrowColor);
            arrowsL[idx].GetComponent<RectTransform>().anchoredPosition = new Vector2(-arrowOffset, 0);
            arrowsL[idx].SetActive(false);

            arrowsR[idx] = MakeTxt(btn.transform, "ar", "<", arrowFontSize, arrowColor);
            arrowsR[idx].GetComponent<RectTransform>().anchoredPosition = new Vector2(arrowOffset, 0);
            arrowsR[idx].SetActive(false);
        }

        var t = MakeTxt(btn.transform, "t", txt, buttonFontSize, buttonTextColor);
        var tr = t.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        btnTexts[idx] = t.GetComponent<Text>();
    }

    void SelectBtn(int idx)
    {
        if (btnTexts == null) return;

        idx = (idx < 0) ? 1 : (idx > 1) ? 0 : idx;

        // Deselect old
        if (showArrows && arrowsL != null && arrowsL[selectedBtn])
        {
            arrowsL[selectedBtn].SetActive(false);
            arrowsR[selectedBtn].SetActive(false);
        }
        if (btnTexts[selectedBtn]) btnTexts[selectedBtn].color = buttonTextColor;

        selectedBtn = idx;

        // Select new
        if (showArrows && arrowsL != null && arrowsL[selectedBtn])
        {
            arrowsL[selectedBtn].SetActive(true);
            arrowsR[selectedBtn].SetActive(true);
        }
        if (btnTexts[selectedBtn]) btnTexts[selectedBtn].color = buttonTextSelectedColor;
    }

    void PressBtn(int idx)
    {
        if (audioSource && menuSelectSound) audioSource.PlayOneShot(menuSelectSound);
        if (idx == 0) DoRest();
        else DoExit();
    }

    // ===== UI HELPERS =====
    GameObject MakeImg(Transform p, string n, Vector2 sz, Vector2 pos, Color c)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);
        var img = go.AddComponent<Image>();
        img.color = c;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = sz;
        rt.anchoredPosition = pos;
        return go;
    }

    GameObject MakeTxt(Transform p, string n, string txt, int sz, Color c)
    {
        var go = new GameObject(n);
        go.transform.SetParent(p, false);

        var t = go.AddComponent<Text>();
        t.text = txt;
        t.fontSize = sz;
        t.color = c;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = gameFont;
        t.fontStyle = FontStyle.Bold;
        t.raycastTarget = false;

        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.9f);
        shadow.effectDistance = new Vector2(2, -2);

        go.GetComponent<RectTransform>().sizeDelta = new Vector2(150, sz + 15);
        return go;
    }

    Sprite MakeVignette()
    {
        int r = 128;
        var tex = new Texture2D(r, r);
        float c = r / 2f;
        for (int x = 0; x < r; x++)
            for (int y = 0; y < r; y++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                tex.SetPixel(x, y, new Color(0, 0, 0, Mathf.Pow(Mathf.Clamp01(d), 1.3f)));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, r, r), new Vector2(0.5f, 0.5f));
    }

    // ===== MENU CONTROL =====
    void OpenMenu()
    {
        if (menuLocked) return;

        // ACTIVAR BLOQUEO TOTAL
        menuLocked = true;
        menuActive = true;

        // Guardar estado
        if (player) savedPlayerPos = player.transform.position;
        if (cam)
        {
            savedCamSize = cam.orthographicSize;
            savedCamPos = cam.transform.position;
        }

        // Congelar jugador
        if (playerRb)
        {
            playerRb.linearVelocity = Vector2.zero;
            playerRb.angularVelocity = 0;
            playerRb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        if (playerCtrl) playerCtrl.enabled = false;

        // Congelar tiempo
        if (freezeWorld)
        {
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0;
        }

        HidePrompt();
        if (audioSource && menuOpenSound) audioSource.PlayOneShot(menuOpenSound);

        menuCanvas.gameObject.SetActive(true);
        selectedBtn = 0;
        SelectBtn(0);
        StartCoroutine(AnimMenu(true));
    }

    void CloseMenu()
    {
        StartCoroutine(AnimMenu(false));
    }

    IEnumerator AnimMenu(bool open)
    {
        float dur = open ? zoomDuration : zoomDuration * 0.6f;
        float t = 0;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float p = open ? (t / dur) : (1 - t / dur);

            overlay.color = new Color(overlayColor.r, overlayColor.g, overlayColor.b, overlayDarkness * p);
            menuCG.alpha = p;

            if (useCinematic && cam)
            {
                cam.orthographicSize = Mathf.Lerp(savedCamSize, zoomSize, p);
                var target = new Vector3(transform.position.x, transform.position.y + 0.5f, savedCamPos.z);
                cam.transform.position = Vector3.Lerp(savedCamPos, target, p);
            }
            yield return null;
        }

        if (!open)
        {
            menuCanvas.gameObject.SetActive(false);

            // Restaurar tiempo
            if (freezeWorld) Time.timeScale = savedTimeScale > 0 ? savedTimeScale : 1;

            // Descongelar jugador
            if (playerRb) playerRb.constraints = RigidbodyConstraints2D.FreezeRotation;
            if (playerCtrl) playerCtrl.enabled = true;

            // QUITAR BLOQUEO
            menuActive = false;
            menuLocked = false;
            playerNearby = true;

            ShowPrompt();
        }
    }

    // ===== ACCIONES =====
    void DoRest()
    {
        StartCoroutine(RestSequence());
    }

    void DoExit()
    {
        CloseMenu();
    }

    IEnumerator RestSequence()
    {
        resting = true;

        // Fade menu
        float t = 0;
        while (t < 0.2f)
        {
            t += Time.unscaledDeltaTime;
            menuCG.alpha = 1 - t / 0.2f;
            yield return null;
        }

        // Efectos
        if (restEffect) restEffect.Play();
        if (audioSource && restSound) audioSource.PlayOneShot(restSound);
        RestUIManager.Instance?.ShowRestPanel(restDuration);

        yield return new WaitForSecondsRealtime(restDuration);

        // Aplicar
        GameManager.Instance?.SetCheckpoint(transform.position);
        if (healOnRest && playerCtrl) playerCtrl.currentHealth = playerCtrl.maxHealth;
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
