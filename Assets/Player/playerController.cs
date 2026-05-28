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
    public float rotationSpeed = 12f;

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

    public bool IsRunning => isRunning;
    public float MoveInputAmount => smoothedInput.magnitude;
    public float HorizontalSpeed { get; private set; }

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
        if (cam == null) return;

        Move();
        HandleAnimation();
    }

    void ToggleRun()
    {
        isRunning = !isRunning;

        if (runButtonImage != null)
        {
            runButtonImage.color =
                isRunning ? runColor : walkColor;
        }
    }

    void Move()
    {
        Vector2 rawInput = Vector2.zero;

        // =====================================
        // MOBILE INPUT
        // =====================================
        if (joystick != null)
        {
            rawInput = new Vector2(
                joystick.Horizontal,
                joystick.Vertical
            );
        }

        // =====================================
        // PC TEST INPUT
        // =====================================
#if UNITY_EDITOR || UNITY_STANDALONE

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // chi override khi co bam phim
        if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
        {
            rawInput.x = h;
            rawInput.y = v;
        }

        isRunning = Input.GetKey(KeyCode.LeftShift);

#endif

        // =====================================
        // SMOOTH INPUT
        // =====================================
        smoothedInput = Vector2.Lerp(
            smoothedInput,
            rawInput,
            Time.deltaTime * inputSmooth
        );

        Vector3 inputDir = new Vector3(
            smoothedInput.x,
            0,
            smoothedInput.y
        );

        // =====================================
        // CAMERA RELATIVE
        // =====================================
        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;

        camForward.y = 0;
        camRight.y = 0;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir =
            camForward * inputDir.z +
            camRight * inputDir.x;

        Vector3 finalMove = Vector3.zero;
        HorizontalSpeed = 0f;

        // =====================================
        // MOVE
        // =====================================
        if (moveDir.magnitude > 0.1f)
        {
            moveDir.Normalize();

            float speed =
                isRunning ? runSpeed : walkSpeed;

            finalMove = moveDir * speed;
            HorizontalSpeed = speed;

            Quaternion targetRot =
                Quaternion.LookRotation(moveDir);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                Time.deltaTime * rotationSpeed
            );
        }

        // =====================================
        // GRAVITY
        // =====================================
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;

        finalMove.y = velocity.y;

        controller.Move(finalMove * Time.deltaTime);
    }

    void HandleAnimation()
    {
        if (animator == null) return;

        float moveAmount = smoothedInput.magnitude;

        float blend = 0f;

        if (moveAmount > 0.1f)
        {
            blend = isRunning ? 1f : 0.5f;
        }

        animator.SetFloat(
            "Blend",
            blend,
            0.15f,
            Time.deltaTime
        );
    }
}