using UnityEngine;

public class CH1_Movement : MonoBehaviour
{
    public float speed = 5f;
    private Vector2 moveInput;
    private InputSystem_Actions input;
    void Start()
    {
        input = new InputSystem_Actions();
        input.Enable();
    }

    void Awake()
    {
    }

    void Update()
    {
        Vector2 direction = input.Player.Move.ReadValue<Vector2>();
        direction.y = 0;
        transform.Translate(direction * speed * Time.deltaTime);
    }
}
