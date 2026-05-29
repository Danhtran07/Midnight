using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Camera third-person: follow target, xoay/zoom touch + chuột, va chạm tường.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    const float PitchMin = -20f;
    const float PitchMax = 60f;
    const float LookAtHeightOffset = 1.5f;
    const float SmoothDampTime = 0.08f;
    const float TouchRotateScale = 0.1f;
    const float ScrollZoomScale = 5f;

    [Header("Target")]
    public Transform target;

    [Header("Follow")]
    public float smooth = 15f;

    [Header("Rotation")]
    public float rotateSpeed = 2f;

    [Header("Zoom")]
    public float minZoom = 2f;
    public float maxZoom = 8f;
    public float zoomSpeed = 0.02f;

    [Header("Collision")]
    public LayerMask wallLayer;

    [Tooltip("Khoang cach toi thieu voi vat can")]
    public float collisionOffset = 0.2f;

    float yaw;
    float pitch = 15f;
    float currentZoom = 4f;
    Vector3 currentVelocity;

    void LateUpdate()
    {
        if (target == null)
            return;

        ProcessTouchInput();
        ProcessMouseInput();
        ApplyCameraTransform();
    }

    void ProcessTouchInput()
    {
        if (Input.touchCount == 1)
            HandleSingleTouchRotate();
        else if (Input.touchCount == 2)
            HandlePinchZoom();
    }

    void HandleSingleTouchRotate()
    {
        Touch t = Input.GetTouch(0);
        if (IsTouchOverUI(t))
            return;

        if (t.phase != TouchPhase.Moved)
            return;

        yaw += t.deltaPosition.x * rotateSpeed * TouchRotateScale;
        pitch -= t.deltaPosition.y * rotateSpeed * TouchRotateScale;
        pitch = Mathf.Clamp(pitch, PitchMin, PitchMax);
        target.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    void HandlePinchZoom()
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

        SetZoom(currentZoom - delta * zoomSpeed);
    }

    void ProcessMouseInput()
    {
        if (Input.GetMouseButton(1))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            yaw += Input.GetAxis("Mouse X") * rotateSpeed;
            pitch -= Input.GetAxis("Mouse Y") * rotateSpeed;
            pitch = Mathf.Clamp(pitch, PitchMin, PitchMax);
            target.rotation = Quaternion.Euler(0f, yaw, 0f);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        SetZoom(currentZoom - scroll * ScrollZoomScale);
    }

    void SetZoom(float zoom)
    {
        currentZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
    }

    void ApplyCameraTransform()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 targetLookPos = target.position + Vector3.up * LookAtHeightOffset;

        Vector3 desiredPosition = targetLookPos + rotation * new Vector3(0f, 0f, -currentZoom);
        Vector3 direction = desiredPosition - targetLookPos;
        float distance = currentZoom;

        if (Physics.Raycast(
            targetLookPos,
            direction.normalized,
            out RaycastHit hit,
            distance,
            wallLayer))
        {
            desiredPosition = hit.point + hit.normal * collisionOffset;
        }

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            SmoothDampTime);

        transform.LookAt(targetLookPos);
    }

    static bool IsTouchOverUI(Touch touch)
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.IsPointerOverGameObject(touch.fingerId);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
            yaw = target.eulerAngles.y;
    }
}
