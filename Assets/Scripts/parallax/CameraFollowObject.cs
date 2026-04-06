using UnityEngine;

public class CameraFollowObject : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _playerTransform;

    [Header("Flip Rotation Stats")]
    [SerializeField] private float _flipYrotationTime = 0.5f;

    private PlayerMovement _player;  // ← tu clase
    private PlayerState _state;      // ← para leer isFacingRight
    private bool _isFacingRight;

    public void Awake()
    {
        _player = _playerTransform.GetComponent<PlayerMovement>();
        _state = _playerTransform.GetComponent<PlayerState>();
        _isFacingRight = _state.isFacingRight;
    }

    private void Update()
    {
        transform.position = _playerTransform.position;
    }

    public void CallTurn()
    {
        LeanTween.rotateY(gameObject, DetermineEndRotation(), _flipYrotationTime).setEaseInOutSine();
    }

    private float DetermineEndRotation()
    {
        _isFacingRight = !_isFacingRight;
        return _isFacingRight ? 0f : 180f;  // ← tenías 180f en los dos casos, bug corregido
    }
}