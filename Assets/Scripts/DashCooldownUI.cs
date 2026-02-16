using UnityEngine;
using UnityEngine.UI;

public class DashCooldownUI : MonoBehaviour
{
    [Header("Arrastra aquí la imagen Fill de tu barra")]
    public Image fillImage;

    private AbilityHolder abilityHolder;
    private MainChar playerChar;

    void Update()
    {
        if (fillImage == null) return;

        // 1. FORZAMOS la búsqueda del jugador REAL en la escena 
        // (Esto evita el 100% de los problemas de Tags o Prefabs desconectados)
        if (playerChar == null)
        {
            playerChar = FindObjectOfType<MainChar>();
            if (playerChar != null)
            {
                abilityHolder = playerChar.GetComponent<AbilityHolder>();
            }
        }

        // Si no detecta al jugador en el nivel, barra a 0
        if (playerChar == null || abilityHolder == null)
        {
            fillImage.fillAmount = 0f;
            return;
        }

        // 2. Obtenemos la habilidad actual del jugador
        Ability currentAbility = abilityHolder.GetAbility();

        // Si el jugador no tiene nada equipado, o lo que tiene NO es el Dash, barra a 0
        if (currentAbility == null || currentAbility.abilityType != AbilityType.Dash)
        {
            fillImage.fillAmount = 0f;
            return;
        }

        // 3. Confirmamos la habilidad como Dash
        DashAbility dashSkill = currentAbility as DashAbility;
        if (dashSkill == null)
        {
            fillImage.fillAmount = 0f;
            return;
        }

        // 4. Calculamos el tiempo de recarga
        float cooldown = dashSkill.GetCooldownRemaining();
        float maxCooldown = dashSkill.dashCooldown;

        // Protección matemática
        if (maxCooldown <= 0f)
        {
            fillImage.fillAmount = 1f;
            return;
        }

        // 5. Aplicar visualmente
        if (cooldown <= 0f)
        {
            fillImage.fillAmount = 1f;  // Listo para usar = Lleno (Amarillo)
        }
        else
        {
            // Barra recargándose progresivamente
            fillImage.fillAmount = 1f - (cooldown / maxCooldown);
        }
    }
}