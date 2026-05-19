using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    public Joystick joystick;

    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float rotationSpeed = 720f;

    public Button runButton;
    public Color runColor = Color.green;
    public Color walkColor = Color.white;

    private CharacterController controller;
    private Animator animator;
    private Image runButtonImage;

    private float gravity = -9.81f;
    private Vector3 velocity;

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
                runButtonImage.color = walkColor;
        }
    }

    void ToggleRun()
    {
        isRunning = !isRunning;

        if (runButtonImage != null)
            runButtonImage.color = isRunning ? runColor : walkColor;

    }

    void Update()
    {
        float horizontal = joystick.Horizontal;
        float vertical = joystick.Vertical;

        Vector3 direction = new Vector3(horizontal, 0f, vertical);

        float moveAmount = Mathf.Clamp01(direction.magnitude);

        if (moveAmount <= 0.1f)
        {
            animator.Play("idle");
        }
        else
        {
            if (isRunning)
                animator.Play("run");
            else
                animator.Play("walk");
        }

        // Movement
        if (moveAmount >= 0.1f)
        {
            Vector3 moveDir = direction.normalized;

            float speed = isRunning ? runSpeed : walkSpeed;

            controller.Move(moveDir * speed * Time.deltaTime);

            Quaternion lookRotation = Quaternion.LookRotation(moveDir);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                lookRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        // Gravity
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);

       
    }
}