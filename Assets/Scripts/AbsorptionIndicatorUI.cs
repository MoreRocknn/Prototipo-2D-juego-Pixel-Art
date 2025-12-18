using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Indicador visual estilo Dark Souls para absorción de enemigos
/// Temática medieval oscura con runas y llamas
/// </summary>
public class AbsorptionIndicatorUI : MonoBehaviour
{
    [Header("=== COLORES MEDIEVAL ===")]
    public Color soulColor = new Color(0.9f, 0.75f, 0.4f);        // Dorado antiguo
    public Color flameColor = new Color(1f, 0.5f, 0.1f);          // Naranja fuego
    public Color darkColor = new Color(0.15f, 0.1f, 0.08f);       // Marrón oscuro

    [Header("=== ANIMACIÓN ===")]
    public float flickerSpeed = 8f;
    public float floatSpeed = 1.2f;
    public float floatAmount = 0.08f;

    // Referencias internas
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform mainContainer;

    // Elementos visuales
    private Image[] flameParticles;
    private Image runeCircle;
    private Image soulOrb;
    private TextMeshProUGUI keyText;
    private TextMeshProUGUI actionText;

    private Transform targetEnemy;
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
        GameObject canvasObj = new GameObject("AbsorptionCanvas_Medieval");
        canvasObj.transform.SetParent(transform);
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 150;

        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(200, 150);
        canvasRect.localScale = Vector3.one * 0.01f;

        // Contenedor principal
        GameObject containerObj = new GameObject("Container");
        containerObj.transform.SetParent(canvasObj.transform, false);
        mainContainer = containerObj.AddComponent<RectTransform>();
        mainContainer.sizeDelta = new Vector2(200, 150);

        // Crear elementos medievales
        CreateFlameParticles();
        CreateRuneCircle();
        CreateSoulOrb();
        CreateKeyPrompt();
        CreateActionText();
    }

    void CreateFlameParticles()
    {
        flameParticles = new Image[12];

        for (int i = 0; i < 12; i++)
        {
            GameObject particleObj = new GameObject($"Flame_{i}");
            particleObj.transform.SetParent(mainContainer, false);

            flameParticles[i] = particleObj.AddComponent<Image>();
            flameParticles[i].sprite = CreateFlameSprite(32);
            flameParticles[i].color = flameColor;

            RectTransform rect = particleObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(8 + Random.Range(0, 6), 12 + Random.Range(0, 8));
        }
    }

    void CreateRuneCircle()
    {
        GameObject runeObj = new GameObject("RuneCircle");
        runeObj.transform.SetParent(mainContainer, false);

        runeCircle = runeObj.AddComponent<Image>();
        runeCircle.sprite = CreateRuneSprite(128);
        runeCircle.color = new Color(soulColor.r, soulColor.g, soulColor.b, 0.6f);

        RectTransform rect = runeObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(70, 70);
        rect.anchoredPosition = new Vector2(0, 25);
    }

    void CreateSoulOrb()
    {
        // Resplandor exterior
        GameObject glowObj = new GameObject("SoulGlow");
        glowObj.transform.SetParent(mainContainer, false);

        Image glow = glowObj.AddComponent<Image>();
        glow.sprite = CreateGlowSprite(64);
        glow.color = new Color(soulColor.r, soulColor.g, soulColor.b, 0.3f);

        RectTransform glowRect = glowObj.GetComponent<RectTransform>();
        glowRect.sizeDelta = new Vector2(50, 50);
        glowRect.anchoredPosition = new Vector2(0, 25);

        // Orbe central
        GameObject orbObj = new GameObject("SoulOrb");
        orbObj.transform.SetParent(mainContainer, false);

        soulOrb = orbObj.AddComponent<Image>();
        soulOrb.sprite = CreateOrbSprite(64);
        soulOrb.color = soulColor;

        RectTransform rect = orbObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(30, 30);
        rect.anchoredPosition = new Vector2(0, 25);
    }

    void CreateKeyPrompt()
    {
        // Fondo estilo pergamino/piedra
        GameObject bgObj = new GameObject("KeyBackground");
        bgObj.transform.SetParent(mainContainer, false);

        Image keyBg = bgObj.AddComponent<Image>();
        keyBg.sprite = CreateStoneSprite(64);
        keyBg.color = darkColor;

        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(36, 36);
        bgRect.anchoredPosition = new Vector2(0, 25);

        // Borde dorado
        GameObject borderObj = new GameObject("KeyBorder");
        borderObj.transform.SetParent(bgObj.transform, false);

        Image border = borderObj.AddComponent<Image>();
        border.sprite = CreateStoneBorderSprite(64);
        border.color = soulColor;

        RectTransform borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.sizeDelta = new Vector2(40, 40);

        // Texto de la tecla
        GameObject textObj = new GameObject("KeyText");
        textObj.transform.SetParent(bgObj.transform, false);

        keyText = textObj.AddComponent<TextMeshProUGUI>();
        keyText.text = "E";
        keyText.fontSize = 22;
        keyText.fontStyle = FontStyles.Bold;
        keyText.color = soulColor;
        keyText.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(36, 36);
    }

    void CreateActionText()
    {
        GameObject textObj = new GameObject("ActionText");
        textObj.transform.SetParent(mainContainer, false);

        actionText = textObj.AddComponent<TextMeshProUGUI>();
        actionText.text = "ABSORBER ALMA";
        actionText.fontSize = 12;
        actionText.fontStyle = FontStyles.SmallCaps;
        actionText.color = soulColor;
        actionText.alignment = TextAlignmentOptions.Center;

        // Sombra
        Shadow shadow = textObj.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.8f);
        shadow.effectDistance = new Vector2(1, -1);

        RectTransform rect = textObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(150, 25);
        rect.anchoredPosition = new Vector2(0, -15);
    }

    // ========== SPRITES MEDIEVALES ==========

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

                float width = 1f - dy * 0.8f;

                if (dx < width * (1f - dy * 0.5f))
                {
                    float alpha = (1f - dy) * (1f - dx / width);
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha * alpha));
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

    Sprite CreateRuneSprite(int res)
    {
        Texture2D tex = new Texture2D(res, res);
        tex.filterMode = FilterMode.Bilinear;

        float center = res / 2f;
        float radius = res / 2f - 4;

        for (int x = 0; x < res; x++)
        {
            for (int y = 0; y < res; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));

                float outerRing = radius;
                float innerRing = radius * 0.85f;

                if (dist <= outerRing && dist >= innerRing)
                {
                    float angle = Mathf.Atan2(y - center, x - center) * Mathf.Rad2Deg;
                    float runePattern = Mathf.Abs(Mathf.Sin(angle * 0.1f * Mathf.Deg2Rad));

                    if (runePattern > 0.3f || (int)(angle + 180) % 30 < 5)
                    {
                        tex.SetPixel(x, y, new Color(1, 1, 1, 0.8f));
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color(1, 1, 1, 0.2f));
                    }
                }
                else if (dist <= radius * 0.6f && dist >= radius * 0.55f)
                {
                    float angle = Mathf.Atan2(y - center, x - center) * Mathf.Rad2Deg;
                    if ((int)(angle + 180) % 60 < 15)
                    {
                        tex.SetPixel(x, y, new Color(1, 1, 1, 0.5f));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
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

    Sprite CreateOrbSprite(int res)
    {
        Texture2D tex = new Texture2D(res, res);
        tex.filterMode = FilterMode.Bilinear;

        float center = res / 2f;
        float radius = res / 2f - 2;

        for (int x = 0; x < res; x++)
        {
            for (int y = 0; y < res; y++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));

                if (dist <= radius)
                {
                    float normalDist = dist / radius;
                    float highlight = 1f - Vector2.Distance(new Vector2(x, y), new Vector2(center * 0.7f, center * 1.3f)) / (radius * 1.5f);
                    highlight = Mathf.Clamp01(highlight);

                    float alpha = 1f - normalDist * 0.3f;
                    float brightness = 0.7f + highlight * 0.3f;

                    tex.SetPixel(x, y, new Color(brightness, brightness, brightness, alpha));
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
                alpha = Mathf.Pow(Mathf.Clamp01(alpha), 2f);
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
                float cornerDist = Mathf.Max(dx, dy);

                if (cornerDist < 0.85f)
                {
                    float noise = ((x * 13 + y * 7) % 10) / 30f;
                    float alpha = 0.9f + noise;
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
                float cornerDist = Mathf.Max(dx, dy);

                if (cornerDist >= 0.75f && cornerDist < 0.95f)
                {
                    tex.SetPixel(x, y, new Color(1, 1, 1, 0.8f));
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

        // Parpadeo de llamas
        if (flameParticles != null)
        {
            for (int i = 0; i < flameParticles.Length; i++)
            {
                if (flameParticles[i] == null) continue;

                float baseAngle = (i * 30f) + animationTime * 20f;
                float radius = 28f + Mathf.Sin(animationTime * flickerSpeed + i) * 5f;

                float x = Mathf.Cos(baseAngle * Mathf.Deg2Rad) * radius;
                float y = 25f + Mathf.Sin(baseAngle * Mathf.Deg2Rad) * radius * 0.3f;
                y += Mathf.Abs(Mathf.Sin(animationTime * flickerSpeed * 2f + i * 0.5f)) * 15f;

                flameParticles[i].rectTransform.anchoredPosition = new Vector2(x, y);

                float flicker = 0.4f + Mathf.PerlinNoise(animationTime * flickerSpeed + i, i * 0.1f) * 0.6f;
                Color c = (i % 3 == 0) ? flameColor : soulColor;
                flameParticles[i].color = new Color(c.r, c.g, c.b, flicker);

                float scale = 0.7f + Mathf.Sin(animationTime * flickerSpeed * 1.5f + i) * 0.3f;
                flameParticles[i].rectTransform.localScale = new Vector3(scale, scale * 1.2f, 1f);
            }
        }

        // Rotación del círculo de runas
        if (runeCircle != null)
        {
            runeCircle.rectTransform.Rotate(0, 0, 15f * Time.deltaTime);
            float runeAlpha = 0.4f + Mathf.Sin(animationTime * 2f) * 0.2f;
            runeCircle.color = new Color(soulColor.r, soulColor.g, soulColor.b, runeAlpha);
        }

        // Pulso del orbe
        if (soulOrb != null)
        {
            float pulse = 1f + Mathf.Sin(animationTime * 3f) * 0.1f;
            soulOrb.rectTransform.localScale = Vector3.one * pulse;

            float brightness = 0.8f + Mathf.Sin(animationTime * 4f) * 0.2f;
            soulOrb.color = new Color(soulColor.r * brightness, soulColor.g * brightness, soulColor.b, 1f);
        }

        // Texto con parpadeo
        if (actionText != null)
        {
            float textAlpha = 0.7f + Mathf.Sin(animationTime * 2f) * 0.3f;
            actionText.color = new Color(soulColor.r, soulColor.g, soulColor.b, textAlpha);
        }

        // Seguir al enemigo
        if (targetEnemy != null && canvas != null)
        {
            canvas.transform.position = targetEnemy.position + Vector3.up * 2f;

            if (Camera.main != null)
            {
                canvas.transform.rotation = Camera.main.transform.rotation;
            }
        }
    }

    // ========== MÉTODOS PÚBLICOS ==========

    public void Show(Transform enemy, KeyCode key = KeyCode.E)
    {
        targetEnemy = enemy;

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

    public void SetUrgent(bool urgent)
    {
        flickerSpeed = urgent ? 12f : 8f;
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

        float duration = 0.5f;
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
        float duration = 0.3f;
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
}