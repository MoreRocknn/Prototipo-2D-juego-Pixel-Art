using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class BossHealth : MonoBehaviour, IResettable
{
    [Header("=== SALUD ===")]
    public int maxHealth = 50;

    [Header("=== DAÑO POR CONTACTO ===")]
    public int bodyContactDamage = 1;
    public float bodyDamageCooldown = 1.0f;

    [Header("=== MUERTE Y ESCENAS ===")]
    public float deathDelay = 5f;
    public string victorySceneName = "VictoryScene";
    public float victorySceneDuration = 5f;
    public string mainMenuSceneName = "MainMenu";

    [HideInInspector] public int currentHealth;
    [HideInInspector] public BossHealthBar bossHealthBarUI;

    private float lastBodyDamageTime;
    private BossData data;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D bossCollider;
    private BossController controller;

    // Flag para distinguir muerte real (victoria) de muerte del jugador
    private bool isRealDeath = false;

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

    // ── IResettable ──────────────────────────────────────────────────────────
    public bool IsBoss => true;

    // Delega en BossController para no duplicar lógica.
    public void ResetState()
    {
        if (isRealDeath) return; // el boss murió de verdad, no resetear
        controller?.ResetState();
    }

    // ── Daño ─────────────────────────────────────────────────────────────────
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

    // ── Muerte real (victoria) ────────────────────────────────────────────────
    void Die()
    {
        data.isDead = true;
        isRealDeath = true;

        controller.UnsealArena();

        if (bossHealthBarUI != null) bossHealthBarUI.Hide();

        CamaraScript camara = Camera.main.GetComponent<CamaraScript>();
        if (camara != null) camara.enModoBoss = false;

        foreach (MonoBehaviour comp in GetComponents<MonoBehaviour>())
            comp.StopAllCoroutines();

        if (spriteRenderer) spriteRenderer.enabled = false;
        if (bossCollider) bossCollider.enabled = false;
        if (rb) rb.simulated = false;

        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        yield return new WaitForSeconds(deathDelay);
        SceneManager.LoadScene(victorySceneName);

        GameObject timer = new GameObject("VictoryTimer");
        DontDestroyOnLoad(timer);
        timer.AddComponent<VictoryTimer>().Init(victorySceneDuration, mainMenuSceneName);
    }

    // ── Reset de salud y componentes ─────────────────────────────────────────
    // Llamado por BossController.ResetState()
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        lastBodyDamageTime = 0f;
        isRealDeath = false;

        if (spriteRenderer)
        {
            spriteRenderer.enabled = true;
            spriteRenderer.color = Color.white;
        }

        if (bossCollider) bossCollider.enabled = true;
        if (rb) rb.simulated = true;

        if (bossHealthBarUI != null)
        {
            bossHealthBarUI.UpdateHealth(currentHealth);
            bossHealthBarUI.Show();
        }
    }
}