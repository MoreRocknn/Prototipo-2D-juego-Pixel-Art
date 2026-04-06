// ParallaxLayer.cs — versión para Cinemachine (Unity 6)
// Usa CinemachineCore para ejecutarse DESPUÉS de que
// Cinemachine haya movido la cámara → sin jitter
using UnityEngine;
using Unity.Cinemachine;

public class ParallaxLayer : MonoBehaviour
{
    [Range(0f, 1f)]
    public float parallaxFactor = 0.5f;
    public bool loopHorizontal = true;

    private Transform cam;
    private Vector3 lastCamPos;
    private float spriteWidth;

    void Start()
    {
        cam = Camera.main.transform;
        lastCamPos = cam.position;
        spriteWidth = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void OnEnable()
    {
        // Suscribirse al evento que Cinemachine lanza
        // DESPUÉS de mover la cámara → orden garantizado
        CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
    }

    void OnDisable()
    {
        CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
    }

    // Este método se llama cuando Cinemachine YA terminó de mover
    void OnCameraUpdated(CinemachineBrain brain)
    {
        Vector3 delta = cam.position - lastCamPos;

        transform.position += new Vector3(
            delta.x * parallaxFactor, 0f, 0f
        );

        lastCamPos = cam.position;

        if (!loopHorizontal) return;

        float dist = cam.position.x - transform.position.x;
        if (Mathf.Abs(dist) > spriteWidth)
        {
            transform.position += new Vector3(
                spriteWidth * Mathf.Sign(dist), 0f, 0f
            );
        }
    }
}