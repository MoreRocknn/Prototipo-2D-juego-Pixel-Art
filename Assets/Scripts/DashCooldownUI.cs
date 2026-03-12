using UnityEngine;
using UnityEngine.UI;

public class DashCooldownUI : MonoBehaviour
{
    [Header("Arrastra aquí la imagen Fill de tu barra")]
    public Image fillImage;

    private AbilityHolder abilityHolder;
    // FIX: PlayerCore en vez de MainChar
    private PlayerCore playerCore;

    void Update()
    {
        if (fillImage == null) return;

        // FIX: buscar PlayerCore en vez de MainChar
        if (playerCore == null)
        {
            playerCore = FindObjectOfType<PlayerCore>();
            if (playerCore != null)
                abilityHolder = playerCore.GetComponent<AbilityHolder>();
        }

        if (playerCore == null || abilityHolder == null)
        {
            fillImage.fillAmount = 0f;
            return;
        }

        Ability currentAbility = abilityHolder.GetAbility();
        if (currentAbility == null || currentAbility.abilityType != AbilityType.Dash)
        {
            fillImage.fillAmount = 0f;
            return;
        }

        DashAbility dashSkill = currentAbility as DashAbility;
        if (dashSkill == null) { fillImage.fillAmount = 0f; return; }

        float cooldown = dashSkill.GetCooldownRemaining();
        float maxCooldown = dashSkill.dashCooldown;

        if (maxCooldown <= 0f) { fillImage.fillAmount = 1f; return; }

        fillImage.fillAmount = cooldown <= 0f ? 1f : 1f - (cooldown / maxCooldown);
    }
}