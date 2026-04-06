using UnityEngine;
using System.Collections;

public class BossHealth : MonoBehaviour
{
    [Header("=== SALUD ===")]
    public int maxHealth = 50;

    [Header("=== DAÑO POR CONTACTO ===")]
    public int bodyContactDamage = 1;
    public float bodyDamageCooldown = 1.0f;

    [HideInInspector] public int currentHealth;
    [HideInInspector] public BossHealthBar bossHealthBarUI;

    private float lastBodyDamageTime;
    private BossData data;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D bossCollider;
    private BossController controller;

    public void Initialize(BossData data, Rigidbody2D rb,
                           SpriteRenderer sr, Collider2D col,
                           BossController controller)
    {
        this.data = data;
        this.rb = rb;
        this.spriteRenderer = sr;
        this.bossCollider = col;
        this.controller = controller;
        currentHealth = maxHealth;
    }

    public void TakeDamage(int dmg, int dir)
    {
        if (data.isDead || data.isInvulnerable || data.isTeleporting) return;

        currentHealth -= dmg;

        if (bossHealthBarUI != null)
            bossHealthBarUI.UpdateHealth(currentHealth);

        StartCoroutine(FlashDamage());

        if (currentHealth <= 0) Die();
    }

    public void OnBodyContact(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        if (Time.time > lastBodyDamageTime + bodyDamageCooldown)
        {
            data.playerMainChar?.TakeDamage(bodyContactDamage);
            lastBodyDamageTime = Time.time;
        }
    }

    IEnumerator FlashDamage()
    {
        if (spriteRenderer) spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        if (!data.isDead && !data.isAttacking && spriteRenderer)
            spriteRenderer.color = Color.white;
    }

    void Die()
    {
        data.isDead = true;

        controller.UnsealArena();

        if (bossHealthBarUI != null) bossHealthBarUI.Hide();

        CamaraScript camara = Camera.main.GetComponent<CamaraScript>();
        if (camara != null) camara.enModoBoss = false;

        foreach (MonoBehaviour comp in GetComponents<MonoBehaviour>())
            comp.StopAllCoroutines();

        // Ocultar sin desactivar el GameObject
        if (spriteRenderer) spriteRenderer.enabled = false;
        if (bossCollider) bossCollider.enabled = false;
        if (rb) rb.simulated = false;
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        lastBodyDamageTime = 0f;

        if (spriteRenderer) spriteRenderer.enabled = true;
        if (bossCollider) bossCollider.enabled = true;
        if (rb) rb.simulated = true;
    }
}