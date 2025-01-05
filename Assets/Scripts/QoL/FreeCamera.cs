using UnityEngine;

public class FreeCamera : MonoBehaviour
{
    [SerializeField]
    private float _MovementSpeed = 10f;

    [SerializeField]
    private float _RotationSpeed = 2f;

    [SerializeField]
    private float _SmoothTime = 0.1f;

    private Vector3 _CurrentVelocity;
    private float _CurrentRotationX;
    private float _CurrentRotationY;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        _CurrentRotationX = transform.eulerAngles.y;
        _CurrentRotationY = transform.eulerAngles.x;
    }

    private void Update()
    {
        // If clicked on the game view, the cursor will be locked
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        HandleMovement();
        HandleRotation();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursor();
        }
    }

    private void HandleMovement()
    {
        Vector3 targetVelocity = new Vector3(
            Input.GetAxisRaw("Horizontal"),
            Input.GetKey(KeyCode.Space) ? 1f
                : Input.GetKey(KeyCode.LeftShift) ? -1f
                : 0f,
            Input.GetAxisRaw("Vertical")
        );

        targetVelocity = transform.TransformDirection(targetVelocity) * _MovementSpeed;
        _CurrentVelocity = Vector3.Lerp(
            _CurrentVelocity,
            targetVelocity,
            1f / _SmoothTime * Time.deltaTime
        );
        transform.position += _CurrentVelocity * Time.deltaTime;
    }

    private void HandleRotation()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        _CurrentRotationX += Input.GetAxis("Mouse X") * _RotationSpeed;
        _CurrentRotationY -= Input.GetAxis("Mouse Y") * _RotationSpeed;
        _CurrentRotationY = Mathf.Clamp(_CurrentRotationY, -90f, 90f);

        transform.rotation = Quaternion.Euler(_CurrentRotationY, _CurrentRotationX, 0f);
    }

    private void ToggleCursor()
    {
        Cursor.lockState =
            Cursor.lockState == CursorLockMode.Locked ? CursorLockMode.None : CursorLockMode.Locked;
    }
}
