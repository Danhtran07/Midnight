using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviourPun
{
    [Header("Joystick")]
    public Joystick joystick;

    [Header("Movement")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float rotationSpeed = 12f; // smooth hơn (không dùng RotateTowards kiểu cũ)

    [Header("Smoothing")]
    public float inputSmooth = 8f;

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

    private Transform cam;

    private Vector2 smoothedInput;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.Play("Locomotion", 0, 0f);

        if (!photonView.IsMine) return;

        StartCoroutine(InitLocal());
    }

    System.Collections.IEnumerator InitLocal()
    {
        yield return new WaitForSeconds(0.15f);

        joystick = FindObjectOfType<Joystick>();

        GameObject runBtnObj = GameObject.Find("RunButton");
        if (runBtnObj != null)
        {
            runButton = runBtnObj.GetComponent<Button>();
            runButton.onClick.AddListener(ToggleRun);
            runButtonImage = runButton.GetComponent<Image>();
        }

        if (Camera.main != null)
            cam = Camera.main.transform;
    }

    void Update()
    {
        if (!photonView.IsMine) return;
        if (joystick == null || cam == null) return;

        Move();
        HandleGravity();
        HandleAnimation();
    }

    void ToggleRun()
    {
        isRunning = !isRunning;

        if (runButtonImage != null)
            runButtonImage.color = isRunning ? runColor : walkColor;
    }

    void Move()
    {
        // 🔥 Smooth input (giảm giật joystick)
        Vector2 rawInput = new Vector2(joystick.Horizontal, joystick.Vertical);
        smoothedInput = Vector2.Lerp(smoothedInput, rawInput, Time.deltaTime * inputSmooth);

        Vector3 inputDir = new Vector3(smoothedInput.x, 0, smoothedInput.y);
        if (inputDir.magnitude < 0.1f) return;

        // camera-relative movement
        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;

        camForward.y = 0;
        camRight.y = 0;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = camForward * inputDir.z + camRight * inputDir.x;
        moveDir.Normalize();

        float speed = isRunning ? runSpeed : walkSpeed;

        controller.Move(moveDir * speed * Time.deltaTime);

        // 🔥 smooth rotation
        Quaternion targetRot = Quaternion.LookRotation(moveDir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            Time.deltaTime * rotationSpeed
        );
    }

    void HandleGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleAnimation()
    {
        if (animator == null) return;

        float move = new Vector2(joystick.Horizontal, joystick.Vertical).magnitude;
        float blend = move < 0.1f ? 0f : (isRunning ? 1f : 0.5f);

        animator.SetFloat("Blend", blend);
    }
}