// ============================================================
// BossAttackSystem.cs — SELECTOR DE ATAQUES
// Solo decide QUÉ ataque usar y lo lanza.
// La lógica de cada ataque está en su propio archivo:
//   BossAttackMelee.cs       (ataque 0)
//   BossAttackTripleSlash.cs (ataque 4)
//   BossAttackSwords.cs      (ataque 1)
//   BossAttackSpikes.cs      (ataque 2 y 5)
//   BossAttackUltimate.cs    (ataque 3)
// ============================================================

using UnityEngine;
using System.Collections;

public class BossAttackSystem : MonoBehaviour
{
    [Header("=== TIMING ===")]
    public float minAttackCooldown = 1.2f;
    public float maxAttackCooldown = 2.0f;

    // Estado interno
    [HideInInspector] public int consecutiveMeleeAttacks = 0;
    [HideInInspector] public int consecutiveRangedAttacks = 0;

    private float attackCooldownTimer = 1f;

    // Referencias compartidas
    [HideInInspector] public BossData data;
    [HideInInspector] public Rigidbody2D rb;
    [HideInInspector] public SpriteRenderer spriteRenderer;
    [HideInInspector] public BossMovementAI movement;

    // Componentes de ataque (en el mismo GameObject)
    private BossAttackMelee atk_melee;
    private BossAttackTripleSlash atk_triple;
    private BossAttackSwords atk_swords;
    private BossAttackSpikes atk_spikes;
    private BossAttackUltimate atk_ultimate;

    // =========================================================
    // INITIALIZE
    // =========================================================
    public void Initialize(BossData data, Rigidbody2D rb, SpriteRenderer sr)
    {
        this.data = data;
        this.rb = rb;
        this.spriteRenderer = sr;
        this.movement = GetComponent<BossMovementAI>();

        atk_melee = GetComponent<BossAttackMelee>();
        atk_triple = GetComponent<BossAttackTripleSlash>();
        atk_swords = GetComponent<BossAttackSwords>();
        atk_spikes = GetComponent<BossAttackSpikes>();
        atk_ultimate = GetComponent<BossAttackUltimate>();
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
            float mod = data.currentPhase == BossData.BossPhase.Phase3 ? 0.5f :
                        data.currentPhase == BossData.BossPhase.Phase2 ? 0.7f : 1.0f;
            attackCooldownTimer = Random.Range(minAttackCooldown, maxAttackCooldown) * mod;
        }
        else
        {
            movement.HandleMovement(dist);
        }
    }

    // =========================================================
    // DISPATCHER — lanza el ataque elegido
    // =========================================================
    IEnumerator PerformAttack(float dist)
    {
        data.isAttacking = true;
        movement.StopMovement();
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

        switch (ChooseAttack(dist))
        {
            case 0: consecutiveMeleeAttacks++; consecutiveRangedAttacks = 0; yield return StartCoroutine(atk_melee.Execute()); break;
            case 1: consecutiveMeleeAttacks = 0; consecutiveRangedAttacks++; yield return StartCoroutine(atk_swords.Execute()); break;
            case 2: consecutiveMeleeAttacks = 0; consecutiveRangedAttacks++; yield return StartCoroutine(atk_spikes.ExecuteGround()); break;
            case 3: consecutiveMeleeAttacks = 0; consecutiveRangedAttacks = 0; yield return StartCoroutine(atk_ultimate.Execute()); break;
            case 4: consecutiveMeleeAttacks++; consecutiveRangedAttacks = 0; yield return StartCoroutine(atk_triple.Execute()); break;
            case 5: consecutiveMeleeAttacks = 0; consecutiveRangedAttacks++; yield return StartCoroutine(atk_spikes.ExecuteCross()); break;
        }

        data.isAttacking = false;
        if (spriteRenderer) spriteRenderer.color = Color.white;
    }

    // =========================================================
    // SELECTOR — decide qué ataque según fase y distancia
    // =========================================================
    int ChooseAttack(float dist)
    {
        // Anti-repetición melee
        if (consecutiveMeleeAttacks >= 2)
        {
            consecutiveMeleeAttacks = 0;
            float r = Random.Range(0f, 100f);
            if (r < 35) return 2;
            else if (r < 70) return 1;
            else return 5;
        }

        // Anti-repetición a distancia
        if (consecutiveRangedAttacks >= 3)
        {
            consecutiveRangedAttacks = 0;
            return Random.value < 0.6f ? 0 : 4;
        }

        switch (data.currentPhase)
        {
            case BossData.BossPhase.Phase3:
                if (dist < 5f) { float r = Random.Range(0f, 100f); return r < 40 ? 4 : r < 70 ? 0 : 3; }
                else { float r = Random.Range(0f, 100f); return r < 25 ? 5 : r < 50 ? 2 : r < 75 ? 1 : 3; }

            case BossData.BossPhase.Phase2:
                if (dist < 6f) { float r = Random.Range(0f, 100f); return r < 50 ? 0 : r < 75 ? 4 : 2; }
                else { float r = Random.Range(0f, 100f); return r < 40 ? 1 : r < 70 ? 2 : 5; }

            default: // Phase1
                return dist < 7f ? (Random.value < 0.7f ? 0 : 2)
                                 : (Random.value < 0.6f ? 1 : 2);
        }
    }

    public void ResetAttacks()
    {
        attackCooldownTimer = 1f;
        consecutiveMeleeAttacks = 0;
        consecutiveRangedAttacks = 0;
    }
}