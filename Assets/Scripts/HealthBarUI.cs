using UnityEngine;

public class HealthBarUI : MonoBehaviour
{
    public SpriteRenderer backgroundSr;
    public SpriteRenderer fillSr;
    public Vector3 offset = new Vector3(0, 1f, 0);
    public bool alwaysShow = false;

    private Transform target;
    private float originalFillScaleX;
    private int maxHealth;
    private int currentHealth;

    // ── Initialize ─────────────────────────────────────────────
    // FIX: Recibe fillScaleX directamente desde HealthBarFactory
    // en vez de intentar leerlo de localScale (que aún no está
    // aplicado por Unity en el mismo frame).
    public void Initialize(Transform targetTransform, int health, int max, float fillScaleX)
    {
        target = targetTransform;
        maxHealth = max;
        currentHealth = health;
        originalFillScaleX = fillScaleX; // valor real pasado desde la Factory

        UpdateFillBar();

        // Solo ocultar si no es "always show"
        if (!alwaysShow)
            SetVisible(false);
    }

    // Sobrecarga de compatibilidad con código antiguo (sin fillScaleX)
    // Lee la escala del fill en el momento de llamar — funciona si
    // se llama desde un Start() o con yield return null antes.
    public void Initialize(Transform targetTransform, int health, int max)
    {
        float scaleX = fillSr != null ? fillSr.transform.localScale.x : 1f;
        Initialize(targetTransform, health, max, scaleX);
    }

    void LateUpdate()
    {
        if (target == null) { Destroy(gameObject); return; }

        transform.position = target.position + offset;
        transform.rotation = Quaternion.identity;

        // Mantener escala positiva (el jugador puede flipear)
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    public void UpdateHealth(int health, int max)
    {
        currentHealth = health;
        maxHealth = max;
        UpdateFillBar();
        SetVisible(true);
    }

    void UpdateFillBar()
    {
        if (fillSr == null) return;

        float percent = maxHealth > 0 ? Mathf.Clamp01((float)currentHealth / maxHealth) : 0f;

        Vector3 fillScale = fillSr.transform.localScale;
        fillScale.x = originalFillScaleX * percent;
        fillSr.transform.localScale = fillScale;

        // Alinear a la izquierda: mover el fill para que se reduzca por la derecha
        Vector3 fillPos = fillSr.transform.localPosition;
        fillPos.x = -(originalFillScaleX - fillScale.x) / 2f;
        fillSr.transform.localPosition = fillPos;
    }

    void SetVisible(bool visible)
    {
        if (backgroundSr != null) backgroundSr.enabled = visible;
        if (fillSr != null) fillSr.enabled = visible;
    }

    public void ForceShow() => SetVisible(true);
    public void ForceHide() => SetVisible(false);

    public void ResetVisibility()
    {
        currentHealth = maxHealth;
        UpdateFillBar();
        SetVisible(alwaysShow);
    }
}