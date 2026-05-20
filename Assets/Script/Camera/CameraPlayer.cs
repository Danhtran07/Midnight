using UnityEngine;
using UnityEngine.EventSystems;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow")]
    public float smooth = 10f;
    public Vector3 offset = new Vector3(0, 2, -4);

    [Header("Rotation")]
    public float rotateSpeed = 5f;
    private float yaw;
    private float pitch = 15f;

    [Header("Zoom")]
    public float minZoom = 2f;
    public float maxZoom = 8f;
    public float zoomSpeed = 0.1f;

    private float currentZoom = 4f;

    void LateUpdate()
    {
        if (target == null) return;

        HandleTouchInput();
        UpdateCamera();
    }

    static bool IsTouchOverUI(Touch touch)
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject(touch.fingerId);
    }

    void HandleTouchInput()
    {
        // ===== MOBILE ROTATION + ZOOM =====
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);

            // Joystick / UI touches must not rotate the camera
            if (IsTouchOverUI(t)) return;

            if (t.phase == TouchPhase.Moved)
            {
                yaw += t.deltaPosition.x * rotateSpeed * Time.deltaTime;
                pitch -= t.deltaPosition.y * rotateSpeed * Time.deltaTime;
                pitch = Mathf.Clamp(pitch, -20f, 60f);
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch t1 = Input.GetTouch(0);
            Touch t2 = Input.GetTouch(1);

            if (IsTouchOverUI(t1) || IsTouchOverUI(t2)) return;

            Vector2 prev1 = t1.position - t1.deltaPosition;
            Vector2 prev2 = t2.position - t2.deltaPosition;

            float prevDist = Vector2.Distance(prev1, prev2);
            float currentDist = Vector2.Distance(t1.position, t2.position);

            float delta = currentDist - prevDist;

            currentZoom -= delta * zoomSpeed * Time.deltaTime;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
        }
    }

    void UpdateCamera()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

        Vector3 direction = new Vector3(0, 0, -currentZoom);
        Vector3 position = target.position + rotation * direction;

        transform.position = Vector3.Lerp(
            transform.position,
            position,
            smooth * Time.deltaTime
        );

        transform.LookAt(target.position + Vector3.up * 1.5f);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}