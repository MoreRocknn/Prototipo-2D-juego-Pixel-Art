using UnityEngine;

public class Simple2DMover : MonoBehaviour
{
    public float speed;
    InputSystem_Actions input;
    void Start()
    {
        input = new InputSystem_Actions();
        input.Enable();
    }

    void Update()
    {
        if (input.Player.Jump.WasPressedThisFrame())
        {
            Debug.Log("Jump!");
        }

        Vector2 direction = input.Player.Move.ReadValue<Vector2>();
        transform.Translate(direction * speed * Time.deltaTime);
    }
}
