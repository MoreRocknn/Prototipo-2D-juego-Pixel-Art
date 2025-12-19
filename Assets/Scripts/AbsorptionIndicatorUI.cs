using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Indicador de absorcion - 100% Personalizable desde Inspector
/// </summary>
public class AbsorptionIndicatorUI : MonoBehaviour
{
    [Header("=== COLORES ===")]
    public Color mainColor = new Color(0.9f, 0.7f, 0.3f);
    public Color glowColor = new Color(1f, 0.5f, 0.2f);
    public Color keyBgColor = new Color(0.1f, 0.08f, 0.05f);

    [Header("=== POSICION ===")]
    public float height = 2f;
    [Range(0.005f, 0.02f)]
    public float scale = 0.01f;

    [Header("=== TECLA ===")]
    [Range(20, 50)]
    public float keyBoxSize = 28f;
    [Range(10, 28)]
    public int keyFontSize = 16;

    [Header("=== TEXTO ACCION ===")]
    public string actionText = "Absorber";
    public bool showText = true;
    [Range(6, 20)]
    public int actionFontSize = 10;

    [Header("=== EFECTOS ===")]
    public bool showGlow = true;
    [Range(30, 80)]
    public float glowSize = 55f;
    public bool showRing = true;
    [Range(30, 70)]
    public float ringSize = 45f;

    [Header("=== ANIMACION ===")]
    public float pulseSpeed = 3f;
    public float floatSpeed = 1.5f;
    [Range(0, 10)]
    public float floatAmount = 5f;
    public float ringRotateSpeed = 20f;

    private Canvas canvas;
    private CanvasGroup cg;
    private RectTransform container;
    private Image glow, ring, keyBg;
    private Text keyTxt, actionTxt;
    private Transform target;
    private bool visible = false;
    private float time = 0;

    void Start()
    {
        Build();
        SetVisible(false);
    }

    void Build()
    {
        GameObject go = new GameObject("_AbsorbCanvas");
        go.transform.SetParent(transform);
        canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 150;

        cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 0;

        RectTransform crt = go.GetComponent<RectTransform>();
        crt.sizeDelta = new Vector2(100, 70);
        crt.localScale = Vector3.one * scale;

        // Container
        GameObject cont = new GameObject("Container");
        cont.transform.SetParent(go.transform, false);
        container = cont.AddComponent<RectTransform>();
        container.sizeDelta = new Vector2(100, 70);

        // Glow
        if (showGlow)
        {
            GameObject glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(cont.transform, false);
            glow = glowGo.AddComponent<Image>();
            glow.sprite = MakeGlow(64);
            glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0.3f);
            glow.raycastTarget = false;
            glowGo.GetComponent<RectTransform>().sizeDelta = new Vector2(glowSize, glowSize);
            glowGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 8);
        }

        // Ring
        if (showRing)
        {
            GameObject ringGo = new GameObject("Ring");
            ringGo.transform.SetParent(cont.transform, false);
            ring = ringGo.AddComponent<Image>();
            ring.sprite = MakeRing(64);
            ring.color = new Color(mainColor.r, mainColor.g, mainColor.b, 0.5f);
            ring.raycastTarget = false;
            ringGo.GetComponent<RectTransform>().sizeDelta = new Vector2(ringSize, ringSize);
            ringGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 8);
        }

        // Key BG
        GameObject keyBgGo = new GameObject("KeyBG");
        keyBgGo.transform.SetParent(cont.transform, false);
        keyBg = keyBgGo.AddComponent<Image>();
        keyBg.color = keyBgColor;
        keyBg.raycastTarget = false;
        keyBgGo.GetComponent<RectTransform>().sizeDelta = new Vector2(keyBoxSize, keyBoxSize);
        keyBgGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 8);

        Outline ko = keyBgGo.AddComponent<Outline>();
        ko.effectColor = mainColor;
        ko.effectDistance = new Vector2(1.5f, -1.5f);

        // Key Text
        GameObject keyTxtGo = new GameObject("KeyTxt");
        keyTxtGo.transform.SetParent(keyBgGo.transform, false);
        keyTxt = keyTxtGo.AddComponent<Text>();
        keyTxt.text = "E";
        keyTxt.fontSize = keyFontSize;
        keyTxt.color = mainColor;
        keyTxt.alignment = TextAnchor.MiddleCenter;
        keyTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        keyTxt.raycastTarget = false;
        RectTransform ktrt = keyTxtGo.GetComponent<RectTransform>();
        ktrt.anchorMin = Vector2.zero;
        ktrt.anchorMax = Vector2.one;
        ktrt.sizeDelta = Vector2.zero;

        // Action Text
        if (showText)
        {
            GameObject actGo = new GameObject("ActionTxt");
            actGo.transform.SetParent(cont.transform, false);
            actionTxt = actGo.AddComponent<Text>();
            actionTxt.text = actionText;
            actionTxt.fontSize = actionFontSize;
            actionTxt.color = mainColor;
            actionTxt.alignment = TextAnchor.MiddleCenter;
            actionTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            actionTxt.raycastTarget = false;

            Shadow s = actGo.AddComponent<Shadow>();
            s.effectColor = Color.black;
            s.effectDistance = new Vector2(1, -1);

            actGo.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 18);
            actGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -18);
        }
    }

    Sprite MakeGlow(int res)
    {
        Texture2D t = new Texture2D(res, res);
        float c = res / 2f;
        for (int x = 0; x < res; x++)
            for (int y = 0; y < res; y++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(1 - Mathf.Clamp01(d), 2)));
            }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
    }

    Sprite MakeRing(int res)
    {
        Texture2D t = new Texture2D(res, res);
        float c = res / 2f;
        for (int x = 0; x < res; x++)
            for (int y = 0; y < res; y++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                float a = (d > 0.7f && d < 0.95f) ? 1 : 0;
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
    }

    void Update()
    {
        if (!visible) return;
        time += Time.deltaTime;

        // Float
        if (container != null)
            container.anchoredPosition = new Vector2(0, Mathf.Sin(time * floatSpeed) * floatAmount);

        // Pulse glow
        if (glow != null)
        {
            float p = 0.25f + Mathf.Sin(time * pulseSpeed) * 0.15f;
            glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, p);
        }

        // Rotate ring
        if (ring != null)
            ring.rectTransform.Rotate(0, 0, ringRotateSpeed * Time.deltaTime);

        // Follow
        if (target != null && canvas != null)
        {
            canvas.transform.position = target.position + Vector3.up * height;
            if (Camera.main != null)
                canvas.transform.rotation = Camera.main.transform.rotation;
        }
    }

    public void Show(Transform t, KeyCode key = KeyCode.E)
    {
        target = t;
        if (keyTxt != null) keyTxt.text = key.ToString();
        SetVisible(true);
        StopAllCoroutines();
        StartCoroutine(Fade(true));
    }

    public void Hide()
    {
        StopAllCoroutines();
        StartCoroutine(Fade(false));
    }

    public void SetUrgent(bool u) => pulseSpeed = u ? 5f : 3f;
    public void SetHeightOffset(float h) => height = h;
    public void SetColors(Color m, Color g) { mainColor = m; glowColor = g; }
    public void SetActionText(string t) { actionText = t; if (actionTxt != null) actionTxt.text = t; }

    void SetVisible(bool v)
    {
        visible = v;
        if (canvas != null) canvas.gameObject.SetActive(v);
    }

    IEnumerator Fade(bool show)
    {
        if (show) { SetVisible(true); time = 0; }
        float dur = show ? 0.3f : 0.2f;
        float t = 0;
        float start = cg.alpha;
        float end = show ? 1 : 0;
        while (t < dur)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, t / dur);
            yield return null;
        }
        cg.alpha = end;
        if (!show) SetVisible(false);
    }
}