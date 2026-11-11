using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonMover : MonoBehaviour
{
    public float moveSpeed = 4f;      // 초당 이동 거리 (m)
    public float lookSpeed = 0.1f;    // 마우스 1픽셀당 회전 각도
    public Transform cam;             // Main Camera
    public bool canMove = true;

    private CharacterController cc;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float pitch;              // 위아래 각도

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }
    private void Update()
    {
        if (!canMove) return;

        transform.Rotate(0f, lookInput.x * lookSpeed, 0f);
        pitch = Mathf.Clamp(pitch - lookInput.y * lookSpeed, -60f, 60f);
        cam.localEulerAngles = new Vector3(pitch, 0f, 0f);

        Vector3 dir = transform.forward * moveInput.y
                    + transform.right * moveInput.x;
        cc.SimpleMove(dir * moveSpeed);
    }

    public void OnMove(InputValue value)
    { moveInput = value.Get<Vector2>(); }

    public void OnLook(InputValue value)
    { lookInput = value.Get<Vector2>(); }

    public void OnAttack(InputValue value)
    { Cursor.lockState = CursorLockMode.Locked; }
}
