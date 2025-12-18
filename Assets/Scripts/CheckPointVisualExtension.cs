using UnityEngine;

/// <summary>
/// Añade este componente a tu CheckPoint existente para mostrar
/// el indicador visual de descanso automáticamente
/// </summary>
public class CheckPointVisualExtension : MonoBehaviour
{
    private CheckPoint checkpoint;
    private RestPromptUI restPrompt;
    private bool promptShown = false;
    private bool playerInRange = false;

    void Start()
    {
        checkpoint = GetComponent<CheckPoint>();

        // Crear el prompt visual
        GameObject promptObj = new GameObject("RestPromptUI");
        restPrompt = promptObj.AddComponent<RestPromptUI>();
    }

    void Update()
    {
        // Mostrar prompt cuando el jugador está en rango y puede descansar
        if (playerInRange && checkpoint != null)
        {
            bool shouldShow = checkpoint.isActivated && checkpoint.canRestHere;

            if (shouldShow && !promptShown)
            {
                restPrompt.Show(transform, checkpoint.restKey);
                promptShown = true;
            }
            else if (!shouldShow && promptShown)
            {
                restPrompt.Hide();
                promptShown = false;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;

        if (promptShown)
        {
            restPrompt.Hide();
            promptShown = false;
        }
    }

    void OnDestroy()
    {
        if (restPrompt != null)
        {
            Destroy(restPrompt.gameObject);
        }
    }
}