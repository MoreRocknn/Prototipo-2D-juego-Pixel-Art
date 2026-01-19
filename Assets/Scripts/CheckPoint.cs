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

    [Header("=== PROMPT ===")]
    public bool showPrompt = true;
    [Range(0.5f, 5f)]
    public float promptHeight = 1.8f;

    [Header("=== PROMPT - TAMAÑO FONDO ===")]
    [Range(0.5f, 4f)]
    public float promptWidth = 1.6f;
    [Range(0.5f, 3f)]
    public float promptBgHeight = 1f;

    [Header("=== PROMPT - CAJA TECLA ===")]
    [Range(0.2f, 1.5f)]
    public float keyBoxSize = 0.55f;
    [Range(-0.5f, 0.5f)]
    public float keyBoxOffsetY = 0.12f;

    [Header("=== PROMPT - TEXTO TECLA ===")]
    [Range(0.05f, 0.4f)]
    public float keyTextSize = 0.15f;
    [Range(20, 80)]
    public int keyFontSize = 48;

    [Header("=== PROMPT - TEXTO ACCION ===")]
    [Range(0.03f, 0.2f)]
    public float actionTextSize = 0.08f;
    [Range(20, 80)]
    public int actionFontSize = 48;
    [Range(-0.8f, 0f)]
    public float actionTextOffsetY = -0.32f;
    public string actionText = "Interactuar";

    [Header("=== PROMPT - COLORES ===")]
    public Color promptKeyColor = new Color(0.76f, 0.6f, 0.23f);
    public Color promptKeyBgColor = new Color(0.15f, 0.12f, 0.08f);
    public Color promptTextColor = new Color(0.9f, 0.85f, 0.7f);
    public Color promptBgColor = new Color(0.05f, 0.04f, 0.03f, 0.95f);
    public Color promptBorderColor = new Color(0.5f, 0.4f, 0.2f);

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
    private bool menuLocked = false;

    private GameObject player;
    private MainChar playerCtrl;
    private Rigidbody2D playerRb;
    private HealingSystem healingSys;
    private Vector3 savedPlayerPos;
    private float savedTimeScale = 1f;

    // UI
    private GameObject promptObject;
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
                    n.Contains("ui") || n.Contains("tmp") || n.Contains("_prompt"))
                {
                    DestroyImmediate(c.gameObject);
                }
            }
        }
    }

    void Update()
    {
        if (menuLocked)
        {
            if (player != null)
            {
                player.transform.position = savedPlayerPos;
                if (playerRb != null)
                {
                    playerRb.linearVelocity = Vector2.zero;
                    playerRb.angularVelocity = 0f;
                }
            }

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

            return;
        }

        if (playerNearby && isActivated && canRestHere)
        {
            if (Input.GetKeyDown(interactKey))
                OpenMenu();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (menuLocked) return;
        if (!other.CompareTag("Player")) return;

        playerNearby = true;
        CachePlayer(other.gameObject);

        if (!isActivated) Activate();
        if (isActivated && canRestHere) ShowPrompt();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (menuLocked) return;
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

    // ===== PROMPT CON SPRITES (CONTROL TOTAL) =====
    private SpriteRenderer promptBgSprite;
    private SpriteRenderer promptKeyBgSprite;
    private TextMesh promptKeyText;
    private TextMesh promptActionTextMesh;
    private float promptAlpha = 0f;
    private bool promptFading = false;
    private bool promptFadeIn = false;

    void BuildPrompt()
    {
        if (promptObject != null)
        {
            DestroyImmediate(promptObject);
        }

        promptObject = new GameObject("_Prompt");
        promptObject.transform.SetParent(transform);
        promptObject.transform.localPosition = Vector3.up * promptHeight;
        promptObject.transform.localScale = Vector3.one;

        // === BORDE (detrás del fondo) ===
        GameObject border = new GameObject("Border");
        border.transform.SetParent(promptObject.transform);
        border.transform.localPosition = Vector3.zero;
        border.transform.localScale = new Vector3(promptWidth + 0.1f, promptBgHeight + 0.1f, 1f);

        SpriteRenderer borderSprite = border.AddComponent<SpriteRenderer>();
        borderSprite.sprite = CreateSquareSprite();
        borderSprite.color = promptBorderColor;
        borderSprite.sortingOrder = 99;

        // === FONDO PRINCIPAL ===
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(promptObject.transform);
        bg.transform.localPosition = Vector3.zero;
        bg.transform.localScale = new Vector3(promptWidth, promptBgHeight, 1f);

        promptBgSprite = bg.AddComponent<SpriteRenderer>();
        promptBgSprite.sprite = CreateSquareSprite();
        promptBgSprite.color = promptBgColor;
        promptBgSprite.sortingOrder = 100;

        // === BORDE DE LA TECLA ===
        GameObject keyBorder = new GameObject("KeyBorder");
        keyBorder.transform.SetParent(promptObject.transform);
        keyBorder.transform.localPosition = new Vector3(0, keyBoxOffsetY, 0);
        keyBorder.transform.localScale = new Vector3(keyBoxSize + 0.07f, keyBoxSize + 0.07f, 1f);

        SpriteRenderer keyBorderSprite = keyBorder.AddComponent<SpriteRenderer>();
        keyBorderSprite.sprite = CreateSquareSprite();
        keyBorderSprite.color = promptKeyColor;
        keyBorderSprite.sortingOrder = 100;

        // === CAJA DE LA TECLA ===
        GameObject keyBox = new GameObject("KeyBox");
        keyBox.transform.SetParent(promptObject.transform);
        keyBox.transform.localPosition = new Vector3(0, keyBoxOffsetY, 0);
        keyBox.transform.localScale = new Vector3(keyBoxSize, keyBoxSize, 1f);

        promptKeyBgSprite = keyBox.AddComponent<SpriteRenderer>();
        promptKeyBgSprite.sprite = CreateSquareSprite();
        promptKeyBgSprite.color = promptKeyBgColor;
        promptKeyBgSprite.sortingOrder = 101;

        // === TEXTO DE LA TECLA ===
        GameObject keyTextObj = new GameObject("KeyText");
        keyTextObj.transform.SetParent(promptObject.transform);
        keyTextObj.transform.localPosition = new Vector3(0, keyBoxOffsetY - 0.02f, -0.1f);
        keyTextObj.transform.localScale = Vector3.one * keyTextSize;

        promptKeyText = keyTextObj.AddComponent<TextMesh>();
        promptKeyText.text = interactKey.ToString();
        promptKeyText.fontSize = keyFontSize;
        promptKeyText.fontStyle = FontStyle.Bold;
        promptKeyText.color = promptKeyColor;
        promptKeyText.anchor = TextAnchor.MiddleCenter;
        promptKeyText.alignment = TextAlignment.Center;

        MeshRenderer keyTextRenderer = keyTextObj.GetComponent<MeshRenderer>();
        keyTextRenderer.sortingOrder = 102;

        // === TEXTO DE ACCIÓN ===
        GameObject actionTextObj = new GameObject("ActionText");
        actionTextObj.transform.SetParent(promptObject.transform);
        actionTextObj.transform.localPosition = new Vector3(0, actionTextOffsetY, -0.1f);
        actionTextObj.transform.localScale = Vector3.one * actionTextSize;

        promptActionTextMesh = actionTextObj.AddComponent<TextMesh>();
        promptActionTextMesh.text = actionText;
        promptActionTextMesh.fontSize = actionFontSize;
        promptActionTextMesh.fontStyle = FontStyle.Bold;
        promptActionTextMesh.color = promptTextColor;
        promptActionTextMesh.anchor = TextAnchor.MiddleCenter;
        promptActionTextMesh.alignment = TextAlignment.Center;

        MeshRenderer actionTextRenderer = actionTextObj.GetComponent<MeshRenderer>();
        actionTextRenderer.sortingOrder = 102;

        promptObject.SetActive(false);
        promptAlpha = 0f;
    }

    Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(4, 4);
        Color[] colors = new Color[16];
        for (int i = 0; i < 16; i++) colors[i] = Color.white;
        tex.SetPixels(colors);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
    }

    void UpdatePromptAlpha()
    {
        if (promptObject == null) return;

        SpriteRenderer[] sprites = promptObject.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in sprites)
        {
            Color c = sr.color;
            c.a = (sr == promptBgSprite) ? promptBgColor.a * promptAlpha : promptAlpha;
            sr.color = c;
        }

        if (promptKeyText != null)
        {
            Color c = promptKeyText.color;
            c.a = promptAlpha;
            promptKeyText.color = c;
        }

        if (promptActionTextMesh != null)
        {
            Color c = promptActionTextMesh.color;
            c.a = promptAlpha;
            promptActionTextMesh.color = c;
        }
    }

    void ShowPrompt()
    {
        if (!showPrompt || promptObject == null) return;
        promptObject.SetActive(true);
        promptFading = true;
        promptFadeIn = true;
    }

    void HidePrompt()
    {
        if (promptObject == null) return;
        promptFading = true;
        promptFadeIn = false;
    }

    void UpdatePromptFade()
    {
        if (!promptFading || promptObject == null) return;

        float target = promptFadeIn ? 1f : 0f;
        promptAlpha = Mathf.MoveTowards(promptAlpha, target, Time.unscaledDeltaTime * 5f);
        UpdatePromptAlpha();

        if (Mathf.Approximately(promptAlpha, target))
        {
            promptFading = false;
            if (!promptFadeIn) promptObject.SetActive(false);
        }
    }

    void LateUpdate()
    {
        // Actualizar fade del prompt
        UpdatePromptFade();

        // Billboard - que siempre mire a la cámara
        if (promptObject != null && promptObject.activeSelf && cam)
        {
            promptObject.transform.rotation = cam.transform.rotation;
        }
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

        if (showArrows && arrowsL != null && arrowsL[selectedBtn])
        {
            arrowsL[selectedBtn].SetActive(false);
            arrowsR[selectedBtn].SetActive(false);
        }
        if (btnTexts[selectedBtn]) btnTexts[selectedBtn].color = buttonTextColor;

        selectedBtn = idx;

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

        menuLocked = true;
        menuActive = true;

        if (player) savedPlayerPos = player.transform.position;
        if (cam)
        {
            savedCamSize = cam.orthographicSize;
            savedCamPos = cam.transform.position;
        }

        if (playerRb)
        {
            playerRb.linearVelocity = Vector2.zero;
            playerRb.angularVelocity = 0;
            playerRb.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        if (playerCtrl) playerCtrl.enabled = false;

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

            if (freezeWorld) Time.timeScale = savedTimeScale > 0 ? savedTimeScale : 1;

            if (playerRb) playerRb.constraints = RigidbodyConstraints2D.FreezeRotation;
            if (playerCtrl) playerCtrl.enabled = true;

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

        float t = 0;
        while (t < 0.2f)
        {
            t += Time.unscaledDeltaTime;
            menuCG.alpha = 1 - t / 0.2f;
            yield return null;
        }

        if (restEffect) restEffect.Play();
        if (audioSource && restSound) audioSource.PlayOneShot(restSound);
        RestUIManager.Instance?.ShowRestPanel(restDuration);

        yield return new WaitForSecondsRealtime(restDuration);

        GameManager.Instance?.SetCheckpoint(transform.position);
        if (healOnRest && playerCtrl) playerCtrl.currentHealth = playerCtrl.maxHealth;
        if (refillVialsOnRest && healingSys) healingSys.RefillVials();
        if (resetEnemiesOnRest) EnemyManager.Instance?.RespawnAllEnemies();

        if (audioSource && refillSound) audioSource.PlayOneShot(refillSound);

        resting = false;
        CloseMenu();
    }

    // ===== REBUILD EN EDITOR =====
#if UNITY_EDITOR
    private float lastPromptHeight, lastPromptWidth, lastPromptBgHeight, lastKeyBoxSize;
    private float lastKeyBoxOffsetY, lastKeyTextSize, lastActionTextSize, lastActionTextOffsetY;
    private int lastKeyFontSize, lastActionFontSize;

    void OnValidate()
    {
        if (Application.isPlaying && promptObject != null)
        {
            // Si cambió algo importante, reconstruir
            bool needsRebuild =
                lastPromptWidth != promptWidth ||
                lastPromptBgHeight != promptBgHeight ||
                lastKeyBoxSize != keyBoxSize ||
                lastKeyBoxOffsetY != keyBoxOffsetY ||
                lastKeyTextSize != keyTextSize ||
                lastKeyFontSize != keyFontSize ||
                lastActionTextSize != actionTextSize ||
                lastActionFontSize != actionFontSize ||
                lastActionTextOffsetY != actionTextOffsetY;

            if (needsRebuild)
            {
                BuildPrompt();
                if (playerNearby && isActivated)
                {
                    promptObject.SetActive(true);
                    promptAlpha = 1f;
                    UpdatePromptAlpha();
                }
            }

            // Solo actualizar altura (no requiere rebuild)
            promptObject.transform.localPosition = Vector3.up * promptHeight;

            // Guardar valores actuales
            lastPromptHeight = promptHeight;
            lastPromptWidth = promptWidth;
            lastPromptBgHeight = promptBgHeight;
            lastKeyBoxSize = keyBoxSize;
            lastKeyBoxOffsetY = keyBoxOffsetY;
            lastKeyTextSize = keyTextSize;
            lastKeyFontSize = keyFontSize;
            lastActionTextSize = actionTextSize;
            lastActionFontSize = actionFontSize;
            lastActionTextOffsetY = actionTextOffsetY;
        }
    }
#endif

    void OnDrawGizmos()
    {
        Gizmos.color = isActivated ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}