using UnityEngine;

public class HealthBarUI : MonoBehaviour
{
    public SpriteRenderer backgroundSr;
    public SpriteRenderer fillSr;
    public Vector3 offset = new Vector3(0, 1f, 0);
    public bool alwaysShow = false;

    private Transform target;
    private Vector3 originalScale;
    private float originalFillScaleX;
    private int maxHealth;
    private int currentHealth;

    public void Initialize(Transform targetTransform, int health, int max)
    {
        target = targetTransform;
        maxHealth = max;
        currentHealth = health;

        originalScale = transform.localScale;
        if (fillSr != null)
            originalFillScaleX = fillSr.transform.localScale.x;

        UpdateFillBar();
        SetVisible(false);
    }

    void LateUpdate()
    {
        if (target == null) { Destroy(gameObject); return; }

        // Seguir al target
        transform.position = target.position + offset;

        // Mantener rotación fija
        transform.rotation = Quaternion.identity;

        // Mantener escala positiva
        Vector3 scale = originalScale;
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

        float percent = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
        percent = Mathf.Clamp01(percent);

        // Escalar el fill en X según el porcentaje de vida
        Vector3 fillScale = fillSr.transform.localScale;
        fillScale.x = originalFillScaleX * percent;
        fillSr.transform.localScale = fillScale;

        // Mover el fill para que se reduzca desde la derecha
        Vector3 fillPos = fillSr.transform.localPosition;
        float fullWidth = originalFillScaleX;
        float currentWidth = fillScale.x;
        fillPos.x = -(fullWidth - currentWidth) / 2f;
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
        SetVisible(false);
    }
}