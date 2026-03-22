// ============================================================
// CameraLookDown.cs — Mirar hacia abajo manteniendo tecla
// Pon este script en el mismo GameObject que CinemachineFollow
// o en un GameObject vacío en la escena.
// ============================================================
using UnityEngine;
using Unity.Cinemachine;
using System.Collections;

public class CameraLookDown : MonoBehaviour
{
    [Header("=== CONFIGURACIÓN ===")]
    [Tooltip("Tecla para mirar hacia abajo")]
    public KeyCode lookDownKey = KeyCode.DownArrow;

    [Tooltip("Tecla para mirar hacia arriba")]
    public KeyCode lookUpKey = KeyCode.UpArrow;

    [Tooltip("Cuánto baja la cámara al mirar abajo")]
    public float lookDownOffset = -4f;

    [Tooltip("Cuánto sube la cámara al mirar arriba")]
    public float lookUpOffset = 4f;

    [Tooltip("Velocidad de transición suave")]
    public float smoothSpeed = 3f;

    [Tooltip("Segundos que hay que mantener la tecla antes de que baje")]
    public float holdDelay = 0.3f;

    [Header("=== REFERENCIAS ===")]
    public CinemachineCamera cinemachineCamera;

    // ── Privadas ──────────────────────────────────────────────
    private CinemachineFollow followComponent;
    private float baseOffsetY;
    private float targetOffsetY;
    private float holdTimer = 0f;
    private bool isLooking = false;

    void Start()
    {
        if (cinemachineCamera == null)
            cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();

        if (cinemachineCamera != null)
            followComponent = cinemachineCamera.GetComponent<CinemachineFollow>();

        if (followComponent != null)
            baseOffsetY = followComponent.FollowOffset.y;

        targetOffsetY = baseOffsetY;
    }

    void Update()
    {
        bool lookingDown = Input.GetKey(lookDownKey);
        bool lookingUp = Input.GetKey(lookUpKey);

        if (lookingDown || lookingUp)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= holdDelay)
            {
                isLooking = true;
                targetOffsetY = lookingDown
                    ? baseOffsetY + lookDownOffset
                    : baseOffsetY + lookUpOffset;
            }
        }
        else
        {
            holdTimer = 0f;
            if (isLooking)
            {
                isLooking = false;
                targetOffsetY = baseOffsetY;
            }
        }

        // Aplicar suavemente
        if (followComponent != null)
        {
            Vector3 offset = followComponent.FollowOffset;
            offset.y = Mathf.Lerp(offset.y, targetOffsetY, Time.deltaTime * smoothSpeed);
            followComponent.FollowOffset = offset;
        }
    }
}