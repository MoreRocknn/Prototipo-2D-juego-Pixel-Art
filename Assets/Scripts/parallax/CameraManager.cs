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

    // Cámara que se usará al respawnear.
    // Se actualiza SOLO cuando el jugador activa un checkpoint.
    private CinemachineCamera _respawnCamera;

    private float _normYPanAmount;

    private Dictionary<CinemachineCamera, Vector3> _cameraStartingOffsets = new Dictionary<CinemachineCamera, Vector3>();
    private float _lastSwapTime = 0f;
    private float _swapCooldown = 0.1f;

    private void Awake()
    {
        if (instance == null)
            instance = this;

        foreach (var cam in _allVirtualCameras)
        {
            var composer = cam.GetComponent<CinemachinePositionComposer>();
            if (composer != null)
                _cameraStartingOffsets[cam] = composer.TargetOffset;
        }
    }

    private void Start()
    {
        ActivateCamera(_defaultCamera);
        _respawnCamera = _defaultCamera;
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

    #region Trigger / Zone Activation

    /// <summary>
    /// Llamado por CameraControlTrigger cuando el jugador entra en una zona.
    /// Cambia la cámara activa pero NO toca _respawnCamera (eso lo hace el checkpoint).
    /// </summary>
    public void ActivateCameraFromTrigger(CinemachineCamera targetCamera)
    {
        if (targetCamera == _currentCamera) return;
        ActivateCamera(targetCamera);
    }

    #endregion

    #region Pan Camera

    public void PanCameraOnContact(float panDistance, float panTime, PanDirection panDirection, bool panToStartingPos)
    {
        _panCameraCoroutine = StartCoroutine(PanCamera(panDistance, panTime, panDirection, panToStartingPos));
    }

    private IEnumerator PanCamera(float panDistance, float panTime, PanDirection panDirection, bool panToStartingPos)
    {
        if (_positionComposer == null) yield break;

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
            if (_cameraStartingOffsets.TryGetValue(_currentCamera, out Vector3 offset))
                startingPos = offset;
            endPos += startingPos;
        }
        else
        {
            startingPos = _positionComposer.TargetOffset;
            if (_cameraStartingOffsets.TryGetValue(_currentCamera, out Vector3 offset))
                endPos = offset;
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
        if (Time.time < _lastSwapTime + _swapCooldown) return;

        if (_currentCamera == cameraFromLeft)
        {
            ActivateCamera(cameraFromRight);
            _lastSwapTime = Time.time;
        }
        else if (_currentCamera == cameraFromRight)
        {
            ActivateCamera(cameraFromLeft);
            _lastSwapTime = Time.time;
        }
    }

    #endregion

    #region Checkpoint Camera

    public CinemachineCamera GetCurrentCamera() => _currentCamera;

    /// <summary>
    /// El CheckPoint llama esto al activarse para guardar qué cámara
    /// debe restaurarse cuando el jugador muera y respawnee aquí.
    /// </summary>
    public void SaveCheckpointCamera()
    {
        _respawnCamera = _currentCamera;
        Debug.Log($"[CameraManager] Cámara de respawn guardada: {_respawnCamera.name}");
    }

    /// <summary>
    /// Sobrecarga: permite forzar una cámara específica desde el inspector del checkpoint.
    /// Dejar en null para usar la cámara activa automáticamente.
    /// </summary>
    public void SaveCheckpointCamera(CinemachineCamera cam)
    {
        _respawnCamera = cam != null ? cam : _currentCamera;
        Debug.Log($"[CameraManager] Cámara de respawn forzada: {_respawnCamera.name}");
    }

    /// <summary>
    /// Llamado desde PlayerHealth al respawnear.
    /// Restaura la cámara del checkpoint y fuerza un cut instantáneo
    /// para que la cámara no haga un blend visible al reaparecer.
    ///
    /// IMPORTANTE: llama esto ANTES de mover al jugador al checkpoint.
    /// </summary>
    // ── FIX 2: cut instantáneo de cámara al respawnear ────────────────────
    public void RespawnToCamera(Vector2 spawnPos)
    {
        CinemachineCamera target = _respawnCamera != null ? _respawnCamera : _defaultCamera;
        ActivateCamera(target);

        // Fuerza un cut instantáneo: desactiva y reactiva el CinemachineBrain
        // para que no interpole desde la posición de muerte hasta el checkpoint.
        var brain = Camera.main?.GetComponent<CinemachineBrain>();
        if (brain != null)
        {
            brain.enabled = false;
            brain.enabled = true;
        }

        Debug.Log($"[CameraManager] Respawn → cámara activada con cut: {target.name}");
    }

    #endregion

    #region Core Activate

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

    public void ResetToDefaultCamera()
    {
        ActivateCamera(_defaultCamera);
        _respawnCamera = _defaultCamera;
    }

    #endregion
}