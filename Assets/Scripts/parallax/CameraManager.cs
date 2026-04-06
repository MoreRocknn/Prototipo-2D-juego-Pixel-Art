using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using static CameraControlTrigger;

public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;
    [Header("Cámara por defecto al respawn")]
    [SerializeField] private CinemachineCamera _defaultCamera;
    [SerializeField] private CinemachineCamera[] _allVirtualCameras;

    [Header("Controls for lerping the Y Damping during player jump/fall")]
    [SerializeField] private float _fallPanAmount = 0.25f;
    [SerializeField] private float _fallYPanTime = 0.35f;
    public float _fallSpeedYDampingChangeThreshold = -15f;

    public bool IsLerpingYDamping { get; private set; }
    public bool LerpedFromPlayerFalling { get; set; }

    private Coroutine _lerpYPanCoroutine;
    private Coroutine _panCameraCoroutine;

    private CinemachinePositionComposer _positionComposer;
    private CinemachineCamera _currentCamera;

    private CinemachineCamera _savedCheckpointCamera;

    private float _normYPanAmount;

    private Dictionary<CinemachineCamera, Vector3> _cameraStartingOffsets = new Dictionary<CinemachineCamera, Vector3>();
    private float _lastSwapTime = 0f;
    private float _swapCooldown = 0.5f;

    private void Awake()
    {
        if (instance == null)
            instance = this;

        // Solo guardamos los offsets, sin asumir cuál está activa
        for (int i = 0; i < _allVirtualCameras.Length; i++)
        {
            var cam = _allVirtualCameras[i];
            var composer = cam.GetComponent<CinemachinePositionComposer>();

            if (composer != null)
                _cameraStartingOffsets[cam] = composer.TargetOffset;
        }
    }

    private void Start()
    {
        // Forzamos siempre la cámara por defecto al iniciar la escena,
        // sin importar si venimos del menú principal o del Play directo.
        ActivateCamera(_defaultCamera);
    }

    #region Lerp the Y Damping
    public void LerpYDamping(bool isPlayerFalling)
    {
        if (_lerpYPanCoroutine != null)
            StopCoroutine(_lerpYPanCoroutine);

        _lerpYPanCoroutine = StartCoroutine(LerpYAction(isPlayerFalling));
    }

    private IEnumerator LerpYAction(bool isPlayerFalling)
    {
        IsLerpingYDamping = true;
        float startDampAmount = _positionComposer.Damping.y;
        float endDampAmount;

        if (isPlayerFalling)
        {
            endDampAmount = _fallPanAmount;
            LerpedFromPlayerFalling = true;
        }
        else
        {
            endDampAmount = _normYPanAmount;
        }

        float elapsedTime = 0f;
        while (elapsedTime < _fallYPanTime)
        {
            elapsedTime += Time.deltaTime;
            float lerpedPanAmount = Mathf.Lerp(startDampAmount, endDampAmount, elapsedTime / _fallYPanTime);
            Vector3 damping = _positionComposer.Damping;
            damping.y = lerpedPanAmount;
            _positionComposer.Damping = damping;
            yield return null;
        }

        IsLerpingYDamping = false;
    }
    #endregion

    #region Pan Camera
    public void PanCameraOnContact(float panDistance, float panTime, PanDirection panDirection, bool panToStartingPos)
    {
        _panCameraCoroutine = StartCoroutine(PanCamera(panDistance, panTime, panDirection, panToStartingPos));
    }

    private IEnumerator PanCamera(float panDistance, float panTime, PanDirection panDirection, bool panToStartingPos)
    {
        Vector3 endPos = Vector3.zero;
        Vector3 startingPos = Vector3.zero;

        if (!panToStartingPos)
        {
            switch (panDirection)
            {
                case PanDirection.Up: endPos = Vector3.up; break;
                case PanDirection.Down: endPos = Vector3.down; break;
                case PanDirection.Left: endPos = Vector3.left; break;
                case PanDirection.Right: endPos = Vector3.right; break;
            }

            endPos *= panDistance;
            startingPos = _cameraStartingOffsets[_currentCamera];
            endPos += startingPos;
        }
        else
        {
            startingPos = _positionComposer.TargetOffset;
            endPos = _cameraStartingOffsets[_currentCamera];
        }

        float elapsedTime = 0f;
        while (elapsedTime < panTime)
        {
            elapsedTime += Time.deltaTime;
            _positionComposer.TargetOffset = Vector3.Lerp(startingPos, endPos, elapsedTime / panTime);
            yield return null;
        }
    }
    #endregion

    #region Swap Cameras
    public void SwapCamera(CinemachineCamera cameraFromLeft, CinemachineCamera cameraFromRight, Vector2 triggerExitDirection)
    {
        if (Time.time < _lastSwapTime + _swapCooldown)
            return;

        if (_currentCamera == cameraFromLeft)
        {
            cameraFromRight.gameObject.SetActive(true);
            cameraFromLeft.gameObject.SetActive(false);
            _currentCamera = cameraFromRight;

            var composer = _currentCamera.GetComponent<CinemachinePositionComposer>();
            if (composer != null) _positionComposer = composer;

            _lastSwapTime = Time.time;
        }
        else if (_currentCamera == cameraFromRight)
        {
            cameraFromLeft.gameObject.SetActive(true);
            cameraFromRight.gameObject.SetActive(false);
            _currentCamera = cameraFromLeft;

            var composer = _currentCamera.GetComponent<CinemachinePositionComposer>();
            if (composer != null) _positionComposer = composer;

            _lastSwapTime = Time.time;
        }
    }
    #endregion

    #region Respawn Camera

    public CinemachineCamera GetCurrentCamera()
    {
        return _currentCamera;
    }

    public void SaveCheckpointCamera(CinemachineCamera cam)
    {
        _savedCheckpointCamera = cam;
    }

    public void RespawnToCamera(Vector2 spawnPos)
    {
        // 1. Si tenemos una cámara guardada del checkpoint, la usamos directamente.
        if (_savedCheckpointCamera != null)
        {
            ActivateCamera(_savedCheckpointCamera);
            return;
        }

        // 2. Fallback: busca si hay un collider que contenga el punto de spawn
        CinemachineCamera targetCamera = null;
        foreach (var cam in _allVirtualCameras)
        {
            Collider2D zone = cam.GetComponent<Collider2D>();
            if (zone != null && zone.OverlapPoint(spawnPos))
            {
                targetCamera = cam;
                break;
            }
        }

        // 3. Si no hay nada, usar la de por defecto
        if (targetCamera == null)
            targetCamera = _defaultCamera;

        ActivateCamera(targetCamera);
    }

    public void RespawnToSpecificCamera(CinemachineCamera targetCamera)
    {
        if (targetCamera == null) targetCamera = _defaultCamera;
        ActivateCamera(targetCamera);
    }

    private void ActivateCamera(CinemachineCamera targetCamera)
    {
        foreach (var cam in _allVirtualCameras)
            cam.gameObject.SetActive(cam == targetCamera);

        _currentCamera = targetCamera;
        _positionComposer = _currentCamera.GetComponent<CinemachinePositionComposer>();

        if (_positionComposer != null)
        {
            if (_cameraStartingOffsets.ContainsKey(_currentCamera))
                _positionComposer.TargetOffset = _cameraStartingOffsets[_currentCamera];

            _normYPanAmount = _positionComposer.Damping.y;
        }

        if (_panCameraCoroutine != null) StopCoroutine(_panCameraCoroutine);
        if (_lerpYPanCoroutine != null) StopCoroutine(_lerpYPanCoroutine);

        IsLerpingYDamping = false;
        LerpedFromPlayerFalling = false;

        _lastSwapTime = Time.time + 2f;
    }
    #endregion

    public void ResetToDefaultCamera()
    {
        ActivateCamera(_defaultCamera);
    }
}