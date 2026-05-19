using UnityEngine;

public class MobileCameraFollow : MonoBehaviour
{
    public Transform target;

    [Header("Camera Settings")]
    public Vector3 offset = new Vector3(0, 3, -5);

    public float followSpeed = 10f;
    public float rotationSpeed = 0.2f;

    [Header("Vertical Rotation")]
    public float minYAngle = -30f;
    public float maxYAngle = 60f;

    private float currentX;
    private float currentY;

    private Vector2 lastTouchPos;
    private bool isRotating = false;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;

        currentX = angles.y;
        currentY = angles.x;
    }

    void LateUpdate()
    {
        if (target == null) return;

        HandleTouchRotation();

        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

        Vector3 desiredPosition =
            target.position + rotation * offset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );

        transform.LookAt(target.position + Vector3.up * 1.5f);
    }

    void HandleTouchRotation()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            // Chỉ xoay khi chạm bên phải màn hình
            if (touch.position.x > Screen.width * 0.3f)
            {
                if (touch.phase == TouchPhase.Began)
                {
                    isRotating = true;
                    lastTouchPos = touch.position;
                }
                else if (touch.phase == TouchPhase.Moved && isRotating)
                {
                    Vector2 delta = touch.position - lastTouchPos;

                    currentX += delta.x * rotationSpeed;
                    currentY -= delta.y * rotationSpeed;

                    currentY = Mathf.Clamp(
                        currentY,
                        minYAngle,
                        maxYAngle
                    );

                    lastTouchPos = touch.position;
                }
                else if (
                    touch.phase == TouchPhase.Ended ||
                    touch.phase == TouchPhase.Canceled
                )
                {
                    isRotating = false;
                }
            }
        }

#if UNITY_EDITOR

        // Test bằng chuột trong editor
        if (Input.GetMouseButton(1))
        {
            currentX += Input.GetAxis("Mouse X") * 5f;
            currentY -= Input.GetAxis("Mouse Y") * 5f;

            currentY = Mathf.Clamp(
                currentY,
                minYAngle,
                maxYAngle
            );
        }

#endif
    }
}