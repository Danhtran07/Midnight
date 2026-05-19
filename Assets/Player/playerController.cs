using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Joystick")]
    public Joystick joystick;

    [Header("Camera")]
    public Transform cameraTransform;

    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float rotationSpeed = 720f;

    [Header("Run Button")]
    public Button runButton;
    public Color runColor = Color.green;
    public Color walkColor = Color.white;

    private CharacterController controller;
    private Animator animator;
    private Image runButtonImage;

    private Vector3 velocity;
    private float gravity = -9.81f;

    private bool isRunning = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (runButton != null)
        {
            runButton.onClick.AddListener(ToggleRun);

            runButtonImage = runButton.GetComponent<Image>();

            if (runButtonImage != null)
            {
                runButtonImage.color = walkColor;
            }
        }
    }

    void ToggleRun()
    {
        isRunning = !isRunning;

        if (runButtonImage != null)
        {
            runButtonImage.color = isRunning ? runColor : walkColor;
        }
    }

    void Update()
    {
        Move();
        HandleGravity();
        HandleAnimation();
    }

    void Move()
    {
        float horizontal = joystick.Horizontal;
        float vertical = joystick.Vertical;

        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical);

        float moveAmount = Mathf.Clamp01(inputDirection.magnitude);

        if (moveAmount >= 0.1f)
        {
            // Camera direction
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            // Final move direction
            Vector3 moveDirection =
                camForward * vertical +
                camRight * horizontal;

            moveDirection.Normalize();

            // Speed
            float currentSpeed = isRunning ? runSpeed : walkSpeed;

            // Move player
            controller.Move(moveDirection * currentSpeed * Time.deltaTime);

            // Rotate player
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }

    void HandleGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);
    }

    void HandleAnimation()
    {
        float horizontal = joystick.Horizontal;
        float vertical = joystick.Vertical;

        float moveAmount = Mathf.Clamp01(
            new Vector2(horizontal, vertical).magnitude
        );

        if (moveAmount < 0.1f)
        {
            animator.Play("idle");
        }
        else
        {
            if (isRunning)
            {
                animator.Play("run");
            }
            else
            {
                animator.Play("walk");
            }
        }
    }
}