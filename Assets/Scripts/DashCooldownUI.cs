using UnityEngine;
using UnityEngine.UI;

public class DashCooldownUI : MonoBehaviour
{
    [Header("Arrastra aquí la imagen Fill de tu barra")]
    public Image fillImage;

    [Header("Jugador (opcional, busca automáticamente)")]
    public GameObject playerObject;

    private AbilityHolder abilityHolder;
    private DashAbility dashAbility;

    void Start()
    {
        if (playerObject == null)
            playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
            abilityHolder = playerObject.GetComponent<AbilityHolder>();
    }

    void Update()
    {
        if (fillImage == null) return;

        // Buscar AbilityHolder si no lo tenemos
        if (abilityHolder == null && playerObject != null)
            abilityHolder = playerObject.GetComponent<AbilityHolder>();

        // Sin jugador o sin habilidad = barra vacía
        if (abilityHolder == null || abilityHolder.currentAbility == null)
        {
            fillImage.fillAmount = 0f;
            return;
        }

        // Verificar que sea DashAbility
        dashAbility = abilityHolder.currentAbility as DashAbility;
        if (dashAbility == null)
        {
            fillImage.fillAmount = 0f;
            return;
        }

        // Calcular fill
        float cooldown = dashAbility.GetCooldownRemaining();
        float maxCooldown = dashAbility.dashCooldown;

        // Fill: 0 = vacío (en cooldown), 1 = lleno (listo)
        if (cooldown <= 0f)
            fillImage.fillAmount = 1f;
        else
            fillImage.fillAmount = 1f - (cooldown / maxCooldown);
    }
}