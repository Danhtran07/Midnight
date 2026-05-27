using UnityEngine;
using UnityEngine.EventSystems;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Follow")]
    public float smooth = 15f;

    [Header("Rotation")]
    public float rotateSpeed = 0.2f;

    private float yaw;
    private float pitch = 15f;

    [Header("Zoom")]
    public float minZoom = 2f;
    public float maxZoom = 8f;
    public float zoomSpeed = 0.02f;

    private float currentZoom = 4f;

    private Vector3 currentVelocity;

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
        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);

            if (IsTouchOverUI(t)) return;

            if (t.phase == TouchPhase.Moved)
            {
                yaw += t.deltaPosition.x * rotateSpeed;
                pitch -= t.deltaPosition.y * rotateSpeed;

                pitch = Mathf.Clamp(pitch, -20f, 60f);
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

    void UpdateCamera()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

        Vector3 targetPosition =
            target.position +
            rotation * new Vector3(0, 0, -currentZoom);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref currentVelocity,
            0.08f
        );

        transform.LookAt(target.position + Vector3.up * 1.5f);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;

        yaw = target.eulerAngles.y;
    }
}