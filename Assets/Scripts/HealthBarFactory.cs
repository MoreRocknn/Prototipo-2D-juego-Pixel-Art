using UnityEngine;

public class HealthBarFactory : MonoBehaviour
{
    public static HealthBarFactory Instance { get; private set; }

    [Header("Tamaño de Barra")]
    public float barWidth = 0.8f;
    public float barHeight = 0.1f;

    [Header("Colores")]
    public Color backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    public Color fillColor = new Color(0.8f, 0.1f, 0.1f, 1f);

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public HealthBarUI CreateHealthBar(Transform target, int currentHealth, int maxHealth, Vector3 offset)
    {
        // ROOT
        GameObject root = new GameObject($"HealthBar_{target.name}");
        root.transform.position = target.position + offset;

        // BACKGROUND
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(root.transform, false);
        bgObj.transform.localPosition = Vector3.zero;
        bgObj.transform.localScale = new Vector3(barWidth, barHeight, 1f);

        SpriteRenderer bgSr = bgObj.AddComponent<SpriteRenderer>();
        bgSr.sprite = CreateSquareSprite();
        bgSr.color = backgroundColor;
        bgSr.sortingOrder = 99;

        // FILL
        float fillScaleX = barWidth * 0.95f; // guardamos este valor explícitamente

        GameObject fillObj = new GameObject("Fill");
        fillObj.transform.SetParent(root.transform, false);
        fillObj.transform.localPosition = Vector3.zero;
        fillObj.transform.localScale = new Vector3(fillScaleX, barHeight * 0.7f, 1f);

        SpriteRenderer fillSr = fillObj.AddComponent<SpriteRenderer>();
        fillSr.sprite = CreateSquareSprite();
        fillSr.color = fillColor;
        fillSr.sortingOrder = 100;

        // COMPONENTE — pasamos fillScaleX directamente para evitar
        // el problema de leerlo desde localScale en el mismo frame
        HealthBarUI hb = root.AddComponent<HealthBarUI>();
        hb.backgroundSr = bgSr;
        hb.fillSr = fillSr;
        hb.offset = offset;
        hb.Initialize(target, currentHealth, maxHealth, fillScaleX);

        return hb;
    }

    private Sprite CreateSquareSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1);
    }
}