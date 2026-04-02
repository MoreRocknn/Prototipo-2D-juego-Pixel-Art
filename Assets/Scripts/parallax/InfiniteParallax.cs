using UnityEngine;

public class InfiniteParallax : MonoBehaviour
{
    [Header("Ajustes de Movimiento")]
    [SerializeField] private Vector2 parallaxEffect; // 0 = no se mueve, 1 = se mueve con la cámara

    private Transform cameraTransform;
    private Vector3 lastCameraPosition;
    private float textureUnitSizeX;

    void Start()
    {
        cameraTransform = Camera.main.transform;
        lastCameraPosition = cameraTransform.position;

        // Medimos el tamaño de la textura para saber cuándo saltar
        Sprite sprite = GetComponent<SpriteRenderer>().sprite;
        Texture2D texture = sprite.texture;
        textureUnitSizeX = texture.width / sprite.pixelsPerUnit;
    }

    void LateUpdate()
    {
        // Calculamos cuánto se ha movido la cámara
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;

        // Movemos el objeto según el factor de parallax
        transform.position += new Vector3(deltaMovement.x * parallaxEffect.x, deltaMovement.y * parallaxEffect.y, 0);

        lastCameraPosition = cameraTransform.position;

        // EFECTO INFINITO (Loop)
        // Si la cámara se mueve más allá del tamaño de la textura, saltamos la posición del fondo
        if (Mathf.Abs(cameraTransform.position.x - transform.position.x) >= textureUnitSizeX)
        {
            float offsetPositionX = (cameraTransform.position.x - transform.position.x) % textureUnitSizeX;
            transform.position = new Vector3(cameraTransform.position.x + offsetPositionX, transform.position.y);
        }
    }
}