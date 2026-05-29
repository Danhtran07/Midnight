using UnityEngine;

public class AIBrain : MonoBehaviour
{
    static readonly int SpeedParamId = Animator.StringToHash("speed");

    [Header("Animation")]
    public Animator animator;

    [Header("Movement Sync")]
    public float moveSpeed = 3f;

    [Tooltip("Animation smoothing")]
    public float speedDampTime = 0.1f;

    [Tooltip("Minimum movement before considered idle")]
    public float idleThreshold = 0.05f;

    Vector3 lastPosition;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        lastPosition = transform.position;

        if (animator == null)
        {
            Debug.LogError("Khong tim thay Animator.", this);
            enabled = false;
            return;
        }

        // Force locomotion state nếu có
        if (animator.HasState(0, Animator.StringToHash("Locomotion")))
        {
            animator.Play("Locomotion", 0, 0f);
        }

        animator.SetFloat(SpeedParamId, 0f);
    }

    void Update()
    {
        UpdateSpeedFromMovement();
    }

    void UpdateSpeedFromMovement()
    {
        float delta = Time.deltaTime;

        if (delta <= 0f)
            return;

        // Tính speed thực tế
        float currentSpeed =
            (transform.position - lastPosition).magnitude / delta;

        // Chống rung speed nhỏ
        if (currentSpeed < idleThreshold)
            currentSpeed = 0f;

        // Normalize speed
        float referenceSpeed = Mathf.Max(moveSpeed, 0.01f);

        float normalizedSpeed =
            Mathf.Clamp01(currentSpeed / referenceSpeed);

        // Chống blend tree bị kẹt 0.1
        if (normalizedSpeed < idleThreshold)
            normalizedSpeed = 0f;

        // Idle thì set cứng về 0
        if (normalizedSpeed == 0f)
        {
            animator.SetFloat(SpeedParamId, 0f);
        }
        else
        {
            // Moving thì smooth
            animator.SetFloat(
                SpeedParamId,
                normalizedSpeed,
                speedDampTime,
                delta
            );
        }

        lastPosition = transform.position;
    }

    // Optional manual control
    public void SetSpeed(float normalizedSpeed)
    {
        normalizedSpeed = Mathf.Clamp01(normalizedSpeed);

        if (normalizedSpeed < idleThreshold)
            normalizedSpeed = 0f;

        animator.SetFloat(SpeedParamId, normalizedSpeed);
    }
}