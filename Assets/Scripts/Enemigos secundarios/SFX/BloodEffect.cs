using UnityEngine;

/// <summary>
/// Efecto de sangre usando el spritesheet.
/// - Shader Additive: el negro del PNG se vuelve transparente automáticamente.
/// - La sangre sale disparada en la dirección del knockback (hacia atrás del enemigo).
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class BloodEffect : MonoBehaviour
{
    [Header("=== SPRITESHEET ===")]
    [Tooltip("Arrastra aquí el spritesheet de sangre")]
    public Texture2D bloodSheet;
    public int sheetColumns = 14;
    public int sheetRows = 9;

    [Header("=== TAMAÑO & CANTIDAD ===")]
    public float particleSize = 1.2f;
    public int hitCount = 15;
    public int deathCount = 30;

    [Header("=== VELOCIDAD ===")]
    public float minSpeed = 3f;
    public float maxSpeed = 8f;
    public float lifetime = 0.6f;

    private ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    // ── API pública ────────────────────────────────────────────

    public void Play(int knockDir)
    {
        Setup(hitCount, knockDir);
        ps.Play();
        Destroy(gameObject, lifetime + 0.5f);
    }

    public void PlayDeath()
    {
        Setup(deathCount, 0);
        ps.Play();
        Destroy(gameObject, lifetime + 0.8f);
    }

    // ── Configuración del ParticleSystem ──────────────────────

    void Setup(int count, int knockDir)
    {
        // ── Material con shader Additive (negro = transparente) ──
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sortingOrder = 10;

        if (bloodSheet != null)
        {
            Shader additive = Shader.Find("Particles/Additive");
            if (additive == null) additive = Shader.Find("Legacy Shaders/Particles/Additive");
            if (additive == null) additive = Shader.Find("Sprites/Default");

            Material mat = new Material(additive);
            mat.mainTexture = bloodSheet;
            r.material = mat;
        }

        // ── Main ──────────────────────────────────────────────
        var main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = Color.white;
        main.startSize = new ParticleSystem.MinMaxCurve(particleSize * 0.7f, particleSize * 1.3f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.6f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.gravityModifier = 2.5f;
        main.maxParticles = 60;

        // ── Emission: un solo burst ───────────────────────────
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] {
            new ParticleSystem.Burst(0f, count)
        });

        // ── Shape ─────────────────────────────────────────────
        var shape = ps.shape;
        shape.enabled = true;
        shape.radius = 0.15f;

        if (knockDir == 0)
        {
            // Muerte: dispersión circular
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.angle = 0f;
        }
        else
        {
            // Golpe: cono apuntando en la dirección del knockback
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 40f;
            float zAngle = knockDir > 0 ? -90f : 90f;
            shape.rotation = new Vector3(0f, 0f, zAngle);
        }

        // ── Texture Sheet Animation ───────────────────────────
        if (bloodSheet != null)
        {
            var tsa = ps.textureSheetAnimation;
            tsa.enabled = true;
            tsa.mode = ParticleSystemAnimationMode.Grid;
            tsa.numTilesX = sheetColumns;
            tsa.numTilesY = sheetRows;
            tsa.animation = ParticleSystemAnimationType.SingleRow;
            tsa.rowMode = ParticleSystemAnimationRowMode.Random;
            tsa.frameOverTime = new ParticleSystem.MinMaxCurve(0f, 1f);
        }

        // ── Size over lifetime ────────────────────────────────
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f,
            AnimationCurve.EaseInOut(0f, 1f, 1f, 0.1f));

        // ── Color over lifetime: fade out ─────────────────────
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = new ParticleSystem.MinMaxGradient(g);
    }
}