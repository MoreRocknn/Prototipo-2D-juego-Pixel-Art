using UnityEngine;

public class CamaraScript : MonoBehaviour
{
    public Transform JUGADOR1111;
    public float VelocidadCamara = 5f;

    [Header("Zona Muerta Vertical")]
    public float verticalDeadzone = 1.5f;
    public Vector3 offset = new Vector3(0, 0, -10);

    [Header("Configuración Boss")]
    public bool enModoBoss = false;
    public float offsetBossY = 3f;

    public void SnapToPlayer()
    {
        if (JUGADOR1111 == null) return;

        // Aplicar offset extra si estamos en modo boss
        float offsetYFinal = offset.y + (enModoBoss ? offsetBossY : 0);

        // 1. Calculamos la 'Y' objetivo del jugador
        float playerY = JUGADOR1111.position.y + offsetYFinal;

        // 2. Calculamos dónde DEBERÍA estar la cámara
        float targetCamY = playerY + verticalDeadzone;

        // 3. Asignamos la posición de la cámara instantáneamente
        transform.position = new Vector3(
            JUGADOR1111.position.x + offset.x,
            targetCamY,
            offset.z
        );
    }

    void LateUpdate()
    {
        if (JUGADOR1111 == null) return;

        float posX = JUGADOR1111.position.x + offset.x;
        float posY = transform.position.y;

        // 🎯 Aplicar offset extra si estamos en modo boss
        float offsetYFinal = offset.y + (enModoBoss ? offsetBossY : 0);
        float playerY = JUGADOR1111.position.y + offsetYFinal;

        float topThreshold = transform.position.y + verticalDeadzone;
        float bottomThreshold = transform.position.y - verticalDeadzone;

        if (playerY > topThreshold)
        {
            posY = playerY - verticalDeadzone;
        }
        else if (playerY < bottomThreshold)
        {
            posY = playerY + verticalDeadzone;
        }

        Vector3 posDeseada = new Vector3(posX, posY, offset.z);
        transform.position = Vector3.Lerp(transform.position, posDeseada, VelocidadCamara * Time.deltaTime);
    }
}