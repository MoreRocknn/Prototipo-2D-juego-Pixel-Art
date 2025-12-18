using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Indicador visual estilo Dark Souls para descansar en hogueras/altares
/// Temática medieval con llamas y runas antiguas
/// </summary>
public class RestPromptUI : MonoBehaviour
{
    [Header("=== COLORES MEDIEVAL ===")]
    public Color flameColor = new Color(1f, 0.6f, 0.2f);          // Naranja fuego
    public Color emberColor = new Color(1f, 0.3f, 0.1f);          // Rojo brasa
    public Color goldColor = new Color(0.85f, 0.7f, 0.3f);        // Dorado antiguo

    [Header("=== ANIMACIÓN ===")]
    public float flickerSpeed = 6f;
    public float floatSpeed = 1f;
    public float floatAmount = 0.05f;

    // Referencias internas
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform mainContainer;

    // Elementos visuales
    private Image[] flames;
    private Image emberGlow;
    private TextMeshProUGUI keyText;
    private TextMeshProUGUI actionText;

    private Transform targetCheckpoint;
    private bool isVisible = false;
    private float animationTime = 0f;

    void Start()
    {
        CreateUI();
        SetVisible(false);
    }

    void CreateUI()
    {
        // Canvas en World Space
        GameObject canvasObj = new GameObject("RestPromptCanvas_Medieval");
        canvasObj.transform.SetParent(transform);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;

        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(180, 120);
        canvasRect.localScale = Vector3.one * 0.01f;

        // Contenedor principal
        GameObject containerObj = new GameObject("Container");
        containerObj.transform.SetParent(canvasObj.transform, false);
        mainContainer = containerObj.AddComponent<RectTransform>();
        mainContainer.sizeDelta = new Vector2(180, 120);

        // Crear elementos
        CreateEmberGlow();
        CreateFlames();
        CreateKeyPrompt();
        CreateActionText();
    }

    void CreateEmberGlow()
    {
        GameObject glowObj = new GameObject("EmberGlow");
        glowObj.transform.SetParent(mainContainer, false);

        emberGlow = glowObj.AddComponent<Image>();
        emberGlow.sprite = CreateGlowSprite(64);
        emberGlow.color = new Color(flameColor.r, flameColor.g, flameColor.b, 0.4f);

        RectTransform rect = glowObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(80, 80);
        rect.anchoredPosition = new Vector2(0, 15);
    }

    void CreateFlames()
    {
        flames = new Image[8];

        for (int i = 0; i < 8; i++)
        {
            GameObject flameObj = new GameObject($"Flame_{i}");
            flameObj.transform.SetParent(mainContainer, false);

            flames[i] = flameObj.AddComponent<Image>();
            flames[i].sprite = CreateFlameSprite(32);

            // Alternar colores
            flames[i].color = (i % 2 == 0) ? flameColor : emberColor;

            RectTransform rect = flameObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(10 + Random.Range(0, 5), 18 + Random.Range(0, 10));
        }
    }

    void CreateKeyPrompt()
    {
        // Fondo de piedra oscura
        GameObject bgObj = new GameObject("KeyBackground");
        bgObj.transform.SetParent(mainContainer, false);

        Image keyBg = bgObj.AddComponent<Image>();
        keyBg.sprite = CreateStoneSprite(64);
        keyBg.color = new Color(0.12f, 0.1f, 0.08f);

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(38, 38);
        bgRect.anchoredPosition = new Vector2(0, 15);

        // Borde dorado desgastado
        GameObject borderObj = new GameObject("KeyBorder");
        borderObj.transform.SetParent(bgObj.transform, false);

        Image border = borderObj.AddComponent<Image>();
        border.sprite = CreateStoneBorderSprite(64);
        border.color = goldColor;

        RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.sizeDelta = new Vector2(44, 44);

        // Texto de la tecla
        GameObject textObj = new GameObject("KeyText");
        textObj.transform.SetParent(bgObj.transform, false);

        keyText = textObj.AddComponent<TextMeshProUGUI>();
        keyText.text = "E";
        keyText.fontSize = 24;
        keyText.fontStyle = FontStyles.Bold;
        keyText.color = goldColor;
        keyText.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(38, 38);
    }

    void CreateActionText()
    {
        GameObject textObj = new GameObject("ActionText");
        textObj.transform.SetParent(mainContainer, false);

        actionText = textObj.AddComponent<TextMeshProUGUI>();
        actionText.text = "DESCANSAR";
        actionText.fontSize = 13;
        actionText.fontStyle = FontStyles.SmallCaps;
        actionText.color = goldColor;
        actionText.alignment = TextAlignmentOptions.Center;

        // Sombra oscura
        Shadow shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.9f);
        shadow.effectDistance = new Vector2(1, -1);

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(140, 25);
        rect.anchoredPosition = new Vector2(0, -22);
    }

    // ========== SPRITES ==========

    Sprite CreateFlameSprite(int res)
    {
        Texture2D tex = new Texture2D(res, res);
        tex.filterMode = FilterMode.Bilinear;

        float centerX = res / 2f;

        for (int x = 0; x < res; x++)
        {
            for (int y = 0; y < res; y++)
            {
                float dx = Mathf.Abs(x - centerX) / centerX;
                float dy = (float)y / res;

                float width = 1f - dy * 0.7f;

                if (dx < width * (1f - dy * 0.4f))
                {
                    float alpha = (1f - dy) * (1f - dx / width);
                    alpha = Mathf.Pow(alpha, 1.5f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0f));
    }

    Sprite CreateGlowSprite(int res)
    {
        Texture2D tex = new Texture2D(res, res);
        tex.filterMode = FilterMode.Bilinear;

        float center = res / 2f;

        for (int x = 0; x < res; x++)
        {
            for (int y = 0; y < res; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float alpha = 1f - (dist / center);
                alpha = Mathf.Pow(Mathf.Clamp01(alpha), 2.5f);
                tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
    }

    Sprite CreateStoneSprite(int res)
    {
        Texture2D tex = new Texture2D(res, res);
        tex.filterMode = FilterMode.Bilinear;

        float center = res / 2f;

        for (int x = 0; x < res; x++)
        {
            for (int y = 0; y < res; y++)
            {
                float dx = Mathf.Abs(x - center) / center;
                float dy = Mathf.Abs(y - center) / center;
                float dist = Mathf.Max(dx, dy);

                if (dist < 0.85f)
                {
                    // Textura rugosa
                    float noise = ((x * 17 + y * 11) % 13) / 40f;
                    float alpha = 0.85f + noise;
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(alpha)));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
    }

    Sprite CreateStoneBorderSprite(int res)
    {
        Texture2D tex = new Texture2D(res, res);
        tex.filterMode = FilterMode.Bilinear;

        float center = res / 2f;

        for (int x = 0; x < res; x++)
        {
            for (int y = 0; y < res; y++)
            {
                float dx = Mathf.Abs(x - center) / center;
                float dy = Mathf.Abs(y - center) / center;
                float dist = Mathf.Max(dx, dy);

                if (dist >= 0.7f && dist < 0.95f)
                {
                    // Borde irregular
                    float noise = ((x * 7 + y * 13) % 11) / 20f;
                    float alpha = 0.6f + noise;
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(alpha)));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f));
    }

    // ========== UPDATE ==========

    void Update()
    {
        if (!isVisible) return;

        animationTime += Time.deltaTime;

        // Flotación suave
        if (mainContainer != null)
        {
            float yOffset = Mathf.Sin(animationTime * floatSpeed) * floatAmount * 100f;
            mainContainer.anchoredPosition = new Vector2(0, yOffset);
        }

        // Animación de llamas
        if (flames != null)
        {
            for (int i = 0; i < flames.Length; i++)
            {
                if (flames[i] == null) continue;

                // Posición alrededor del centro
                float baseAngle = i * 45f;
                float wobble = Mathf.Sin(animationTime * flickerSpeed + i) * 8f;
                float radius = 22f + wobble * 0.3f;

                float x = Mathf.Cos(baseAngle * Mathf.Deg2Rad) * radius;
                float y = 15f + Mathf.Sin(baseAngle * Mathf.Deg2Rad) * radius * 0.2f;

                // Movimiento hacia arriba
                y += Mathf.Abs(Mathf.Sin(animationTime * flickerSpeed * 1.5f + i * 0.7f)) * 12f;

                flames[i].rectTransform.anchoredPosition = new Vector2(x, y);

                // Parpadeo
                float flicker = 0.5f + Mathf.PerlinNoise(animationTime * flickerSpeed + i * 0.5f, i) * 0.5f;
                Color baseColor = (i % 2 == 0) ? flameColor : emberColor;
                flames[i].color = new Color(baseColor.r, baseColor.g, baseColor.b, flicker);

                // Escala
                float scale = 0.8f + Mathf.Sin(animationTime * flickerSpeed + i * 0.3f) * 0.3f;
                flames[i].rectTransform.localScale = new Vector3(scale, scale * 1.3f, 1f);
            }
        }

        // Resplandor pulsante
        if (emberGlow != null)
        {
            float glowPulse = 0.3f + Mathf.Sin(animationTime * 2f) * 0.15f;
            emberGlow.color = new Color(flameColor.r, flameColor.g, flameColor.b, glowPulse);

            float glowScale = 1f + Mathf.Sin(animationTime * 1.5f) * 0.1f;
            emberGlow.rectTransform.localScale = Vector3.one * glowScale;
        }

        // Texto con brillo
        if (actionText != null)
        {
            float textGlow = 0.7f + Mathf.Sin(animationTime * 2.5f) * 0.3f;
            actionText.color = new Color(goldColor.r, goldColor.g, goldColor.b, textGlow);
        }

        // Tecla con pulso sutil
        if (keyText != null)
        {
            float keyPulse = 0.85f + Mathf.Sin(animationTime * 3f) * 0.15f;
            keyText.color = new Color(goldColor.r * keyPulse, goldColor.g * keyPulse, goldColor.b, 1f);
        }

        // Seguir al checkpoint
        if (targetCheckpoint != null && canvas != null)
        {
            canvas.transform.position = targetCheckpoint.position + Vector3.up * 1.8f;

            if (Camera.main != null)
            {
                canvas.transform.rotation = Camera.main.transform.rotation;
            }
        }
    }

    // ========== MÉTODOS PÚBLICOS ==========

    public void Show(Transform checkpoint, KeyCode key = KeyCode.E)
    {
        targetCheckpoint = checkpoint;

        if (keyText != null)
        {
            keyText.text = key.ToString();
        }

        SetVisible(true);
        StartCoroutine(FadeIn());
    }

    public void Hide()
    {
        StartCoroutine(FadeOut());
    }

    void SetVisible(bool visible)
    {
        isVisible = visible;
        if (canvas != null)
        {
            canvas.gameObject.SetActive(visible);
        }
    }

    IEnumerator FadeIn()
    {
        SetVisible(true);
        animationTime = 0f;

        float duration = 0.4f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            }
            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    IEnumerator FadeOut()
    {
        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            }
            yield return null;
        }

        SetVisible(false);
    }

    public void SetColors(Color primary, Color secondary)
    {
        flameColor = primary;
        emberColor = secondary;
    }
}