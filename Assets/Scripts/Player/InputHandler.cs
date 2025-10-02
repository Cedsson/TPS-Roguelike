using UnityEngine;

public class InputHandler : MonoBehaviour
{


    [Header("Action Keys")]
    public KeyCode attackKey = KeyCode.Mouse0;
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode dodgeKey = KeyCode.LeftShift;

    [Header("Camera Settings")]
    public float mouseSensitivity = 2f;


    public Vector2 MoveInput { get; private set; }
    public Vector2 LookInput { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool MeleePressed { get; private set; }
    public bool DodgePressed { get; private set; }
    public float ScrollInput { get; private set; }



    void Update()
    {
        // --- Rörelse ---
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        MoveInput = new Vector2(h, v);

        // --- Kamera ---
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        LookInput = new Vector2(mouseX, mouseY);

        // --- Jump ---
        JumpPressed = Input.GetKeyDown(jumpKey);

        // --- Scroll ---
        ScrollInput = Input.GetAxis("Mouse ScrollWheel");

        // --- Attacks ---
        MeleePressed = Input.GetKeyDown(attackKey);

        // --- Dodge ---
        DodgePressed = Input.GetKeyDown(dodgeKey);
    }
}
