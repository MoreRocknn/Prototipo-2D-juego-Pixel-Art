using UnityEngine;
using Unity.Cinemachine;

public class CameraLookDown : MonoBehaviour
{
    [Header("=== TECLAS ===")]
    public KeyCode lookDownKey = KeyCode.DownArrow;
    public KeyCode lookUpKey = KeyCode.UpArrow;

    [Header("=== SCREEN POSITION ===")]
    [Range(-0.49f, 0f)]
    public float lookDownScreenY = -0.49f;

    [Range(0f, 0.49f)]
    public float lookUpScreenY = 0.49f;

    [Header("=== TIMING ===")]
    public float holdDelay = 0.2f;
    public float smoothSpeed = 3f;

    [Header("=== REFERENCIAS ===")]
    public CinemachineCamera cinemachineCamera;

    private CinemachinePositionComposer composer;
    private float baseScreenY;
    private float currentScreenY;
    private float targetScreenY;
    private float holdTimer;
    private bool isLooking;

    void Start()
    {
        if (cinemachineCamera == null)
            cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();

        if (cinemachineCamera != null)
            composer = cinemachineCamera.GetComponent<CinemachinePositionComposer>();

        if (composer != null)
            baseScreenY = composer.Composition.ScreenPosition.y;

        currentScreenY = baseScreenY;
        targetScreenY = baseScreenY;
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
            }
        }
        else
        {
            holdTimer = 0f;
            if (isLooking)
            {
                isLooking = false;
                targetScreenY = baseScreenY;
            }
        }

        currentScreenY = Mathf.Lerp(currentScreenY, targetScreenY, Time.deltaTime * smoothSpeed);

        if (composer != null)
        {
            var comp = composer.Composition;
            comp.ScreenPosition.y = currentScreenY;
            composer.Composition = comp;
        }
    }
}