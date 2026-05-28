using UnityEngine;
using UnityEngine.EventSystems;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow")]
    public float smooth = 15f;

    [Header("Rotation")]
    public float rotateSpeed = 2f;

    private float yaw;
    private float pitch = 15f;

    [Header("Zoom")]
    public float minZoom = 2f;
    public float maxZoom = 8f;
    public float zoomSpeed = 0.02f;

    private float currentZoom = 4f;

    [Header("Collision")]
    public LayerMask wallLayer;

    [Tooltip("Khoang cach toi thieu voi vat can")]
    public float collisionOffset = 0.2f;

    private Vector3 currentVelocity;

    void LateUpdate()
    {
        if (target == null)
            return;

        HandleTouchInput();
        HandleMouseInput();

        UpdateCamera();
    }

    static bool IsTouchOverUI(Touch touch)
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.IsPointerOverGameObject(touch.fingerId);
    }

    void HandleTouchInput()
    {
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);

            if (IsTouchOverUI(t))
                return;

            if (t.phase == TouchPhase.Moved)
            {
                yaw += t.deltaPosition.x * rotateSpeed * 0.1f;
                pitch -= t.deltaPosition.y * rotateSpeed * 0.1f;

                pitch = Mathf.Clamp(pitch, -20f, 60f);

                // Player xoay theo camera
                target.rotation = Quaternion.Euler(0, yaw, 0);
            }
        }

        else if (Input.touchCount == 2)
        {
            Touch t1 = Input.GetTouch(0);
            Touch t2 = Input.GetTouch(1);

            if (IsTouchOverUI(t1) || IsTouchOverUI(t2))
                return;

            Vector2 prev1 = t1.position - t1.deltaPosition;
            Vector2 prev2 = t2.position - t2.deltaPosition;

            float prevDist = Vector2.Distance(prev1, prev2);
            float currentDist = Vector2.Distance(t1.position, t2.position);

            float delta = currentDist - prevDist;

            currentZoom -= delta * zoomSpeed;

            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
        }
    }

    void HandleMouseInput()
    {
        if (Input.GetMouseButton(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            yaw += Input.GetAxis("Mouse X") * rotateSpeed;
            pitch -= Input.GetAxis("Mouse Y") * rotateSpeed;

            pitch = Mathf.Clamp(pitch, -20f, 60f);

            target.rotation = Quaternion.Euler(0, yaw, 0);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        currentZoom -= scroll * 5f;

        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
    }

    void UpdateCamera()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

        Vector3 targetLookPos = target.position + Vector3.up * 1.5f;

        Vector3 desiredPosition =
            targetLookPos +
            rotation * new Vector3(0, 0, -currentZoom);

        Vector3 direction = desiredPosition - targetLookPos;

        float distance = currentZoom;

        if (Physics.Raycast(
            targetLookPos,
            direction.normalized,
            out RaycastHit hit,
            distance,
            wallLayer))
        {
            desiredPosition =
                hit.point +
                hit.normal * collisionOffset;
        }

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            0.08f
        );

        transform.LookAt(targetLookPos);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        yaw = target.eulerAngles.y;
    }
}