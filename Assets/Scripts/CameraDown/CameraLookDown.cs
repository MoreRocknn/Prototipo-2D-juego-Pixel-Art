// ============================================================
// CameraLookDown.cs — Look down combinando ScreenPosition + FollowOffset
// Unity 6 + Cinemachine 3.x — Cámara Ortográfica 2D
// ============================================================
using UnityEngine;
using Unity.Cinemachine;

public class CameraLookDown : MonoBehaviour
{
    [Header("=== TECLAS ===")]
    public KeyCode lookDownKey = KeyCode.DownArrow;
    public KeyCode lookUpKey = KeyCode.UpArrow;

    [Header("=== SCREEN POSITION (0.5 max por Cinemachine) ===")]
    [Tooltip("Screen Y al mirar abajo — el jugador sube en pantalla")]
    [Range(-0.49f, 0f)]
    public float lookDownScreenY = -0.49f;

    [Tooltip("Screen Y al mirar arriba")]
    [Range(0f, 0.49f)]
    public float lookUpScreenY = 0.49f;

    [Header("=== FOLLOW OFFSET EXTRA (para bajar más) ===")]
    [Tooltip("Unidades extra que baja la cámara al mirar abajo (suma al Screen Position)")]
    public float lookDownOffsetExtra = -4f;

    [Tooltip("Unidades extra que sube la cámara al mirar arriba")]
    public float lookUpOffsetExtra = 4f;

    [Header("=== TIMING ===")]
    public float holdDelay = 0.2f;
    public float smoothSpeed = 3f;

    [Header("=== REFERENCIAS ===")]
    public CinemachineCamera cinemachineCamera;

    // ── Privadas ──────────────────────────────────────────────
    private CinemachinePositionComposer composer;
    private CinemachineFollow follow;
    private float baseScreenY;
    private float baseOffsetY;
    private float currentScreenY;
    private float currentOffsetY;
    private float targetScreenY;
    private float targetOffsetY;
    private float holdTimer;
    private bool isLooking;

    void Start()
    {
        if (cinemachineCamera == null)
            cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();

        if (cinemachineCamera != null)
        {
            composer = cinemachineCamera.GetComponent<CinemachinePositionComposer>();
            follow = cinemachineCamera.GetComponent<CinemachineFollow>();
        }

        if (composer != null) baseScreenY = composer.Composition.ScreenPosition.y;
        if (follow != null) baseOffsetY = follow.FollowOffset.y;

        currentScreenY = baseScreenY;
        currentOffsetY = baseOffsetY;
        targetScreenY = baseScreenY;
        targetOffsetY = baseOffsetY;
    }

    void Update()
    {
        bool pressingDown = Input.GetKey(lookDownKey);
        bool pressingUp = Input.GetKey(lookUpKey);

        if (pressingDown || pressingUp)
        {
            holdTimer += Time.deltaTime;
            if (holdTimer >= holdDelay)
            {
                isLooking = true;
                targetScreenY = pressingDown ? lookDownScreenY : lookUpScreenY;
                targetOffsetY = pressingDown
                    ? baseOffsetY + lookDownOffsetExtra
                    : baseOffsetY + lookUpOffsetExtra;
            }
        }
        else
        {
            holdTimer = 0f;
            if (isLooking)
            {
                isLooking = false;
                targetScreenY = baseScreenY;
                targetOffsetY = baseOffsetY;
            }
        }

        float t = Time.deltaTime * smoothSpeed;
        currentScreenY = Mathf.Lerp(currentScreenY, targetScreenY, t);
        currentOffsetY = Mathf.Lerp(currentOffsetY, targetOffsetY, t);

        if (composer != null)
        {
            var comp = composer.Composition;
            comp.ScreenPosition.y = currentScreenY;
            composer.Composition = comp;
        }

        if (follow != null)
        {
            Vector3 offset = follow.FollowOffset;
            offset.y = currentOffsetY;
            follow.FollowOffset = offset;
        }
    }
}