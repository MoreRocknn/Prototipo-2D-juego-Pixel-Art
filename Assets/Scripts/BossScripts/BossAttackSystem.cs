// ============================================================
// BossAttackSystem.cs — SELECTOR DE ATAQUES
// Delega en los componentes separados:
//   BossAttackMelee, BossAttackTripleSlash,
//   BossAttackSwords, BossAttackSpikes, BossAttackUltimate
// ============================================================
using UnityEngine;
using System.Collections;

public class BossAttackSystem : MonoBehaviour
{
    [Header("=== TIMING DE ATAQUES ===")]
    public float minAttackCooldown = 1.2f;
    public float maxAttackCooldown = 2.0f;

    // ─────────────────────────────────────────────────────────
    // ESTADO INTERNO
    // ─────────────────────────────────────────────────────────
    private float attackCooldownTimer = 1f;
    private int consecutiveMelee = 0;
    private int consecutiveRanged = 0;
    private int lastAttackType = -1;

    // Referencias
    private BossData data;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private BossMovementAI movement;

    // Componentes de ataque
    private BossAttackMelee atk_melee;
    private BossAttackTripleSlash atk_triple;
    private BossAttackSwords atk_swords;
    private BossAttackSpikes atk_spikes;
    private BossAttackUltimate atk_ultimate;

    // =========================================================
    // INITIALIZE — llamado por BossController.Awake()
    // =========================================================
    public void Initialize(BossData data, Rigidbody2D rb, SpriteRenderer sr)
    {
        this.data = data;
        this.rb = rb;
        this.sr = sr;
        movement = GetComponent<BossMovementAI>();
        atk_melee = GetComponent<BossAttackMelee>();
        atk_triple = GetComponent<BossAttackTripleSlash>();
        atk_swords = GetComponent<BossAttackSwords>();
        atk_spikes = GetComponent<BossAttackSpikes>();
        atk_ultimate = GetComponent<BossAttackUltimate>();

        // Avisos si faltan componentes
        if (atk_swords == null) Debug.LogWarning("[BossAttackSystem] Falta BossAttackSwords en el GameObject.");
        if (atk_spikes == null) Debug.LogWarning("[BossAttackSystem] Falta BossAttackSpikes en el GameObject.");
        if (atk_melee == null) Debug.LogWarning("[BossAttackSystem] Falta BossAttackMelee en el GameObject.");
        if (atk_triple == null) Debug.LogWarning("[BossAttackSystem] Falta BossAttackTripleSlash en el GameObject.");
        if (atk_ultimate == null) Debug.LogWarning("[BossAttackSystem] Falta BossAttackUltimate en el GameObject.");
    }

    // =========================================================
    // HANDLE COMBAT — llamado cada frame desde BossController
    // =========================================================
    public void HandleCombat(float dist)
    {
        if (data.isAttacking || data.isTeleporting) return;

        attackCooldownTimer -= Time.deltaTime;

        if (attackCooldownTimer <= 0f)
        {
            StartCoroutine(PerformAttack(dist));

            float phaseMod = data.currentPhase == BossData.BossPhase.Phase3 ? 0.5f :
                             data.currentPhase == BossData.BossPhase.Phase2 ? 0.7f : 1.0f;
            attackCooldownTimer = Random.Range(minAttackCooldown, maxAttackCooldown) * phaseMod;
        }
        else
        {
            movement.HandleMovement(dist);
        }
    }

    // =========================================================
    // PERFORM ATTACK — dispatcher
    // =========================================================
    IEnumerator PerformAttack(float dist)
    {
        data.isAttacking = true;
        movement.StopMovement();
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        int attackType = ChooseAttack(dist);

        switch (attackType)
        {
            case 0:
                consecutiveMelee++; consecutiveRanged = 0;
                if (atk_melee != null) yield return StartCoroutine(atk_melee.Execute());
                break;
            case 1:
                consecutiveMelee = 0; consecutiveRanged++;
                if (atk_swords != null) yield return StartCoroutine(atk_swords.Execute());
                else Debug.LogWarning("[Boss] BossAttackSwords no encontrado — asigna el componente.");
                break;
            case 2:
                consecutiveMelee = 0; consecutiveRanged++;
                if (atk_spikes != null) yield return StartCoroutine(atk_spikes.ExecuteGround());
                else Debug.LogWarning("[Boss] BossAttackSpikes no encontrado — asigna el componente.");
                break;
            case 3:
                consecutiveMelee = 0; consecutiveRanged = 0;
                if (atk_ultimate != null) yield return StartCoroutine(atk_ultimate.Execute());
                break;
            case 4:
                consecutiveMelee++; consecutiveRanged = 0;
                if (atk_triple != null) yield return StartCoroutine(atk_triple.Execute());
                break;
            case 5:
                consecutiveMelee = 0; consecutiveRanged++;
                if (atk_spikes != null) yield return StartCoroutine(atk_spikes.ExecuteCross());
                else Debug.LogWarning("[Boss] BossAttackSpikes no encontrado — asigna el componente.");
                break;
        }

        lastAttackType = attackType;
        data.isAttacking = false;
        if (sr) sr.color = Color.white;
    }

    // =========================================================
    // CHOOSE ATTACK
    // =========================================================
    int ChooseAttack(float dist)
    {
        if (consecutiveMelee >= 2)
        {
            consecutiveMelee = 0;
            float r = Random.Range(0f, 100f);
            return r < 35 ? 2 : r < 70 ? 1 : 5;
        }
        if (consecutiveRanged >= 3)
        {
            consecutiveRanged = 0;
            return Random.value < 0.6f ? 0 : 4;
        }

        switch (data.currentPhase)
        {
            case BossData.BossPhase.Phase3:
                if (dist < 5f)
                {
                    float r = Random.Range(0f, 100f);
                    return r < 40 ? 4 : r < 70 ? 0 : 3;
                }
                else
                {
                    float r = Random.Range(0f, 100f);
                    return r < 25 ? 5 : r < 50 ? 2 : r < 75 ? 1 : 3;
                }
            case BossData.BossPhase.Phase2:
                if (dist < 6f)
                {
                    float r = Random.Range(0f, 100f);
                    return r < 50 ? 0 : r < 75 ? 4 : 2;
                }
                else
                {
                    float r = Random.Range(0f, 100f);
                    return r < 40 ? 1 : r < 70 ? 2 : 5;
                }
            default: // Phase1
                if (dist < 7f) return Random.value < 0.7f ? 0 : 2;
                else return Random.value < 0.6f ? 1 : 2;
        }
    }

    // =========================================================
    // RESET
    // =========================================================
    public void ResetAttacks()
    {
        attackCooldownTimer = 1f;
        consecutiveMelee = 0;
        consecutiveRanged = 0;
        lastAttackType = -1;
    }
}