using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

/// <summary>
/// Điều khiển di chuyển third-person cho player local (Photon PUN).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviourPun
{
    const string LocomotionStateName = "Locomotion";
    const string BlendParameterName = "Blend";
    const float MoveInputDeadzone = 0.1f;
    const float GroundedVelocityY = -2f;
    const float BlendDampTime = 0.15f;
    const float WalkBlendValue = 0.5f;
    const float RunBlendValue = 1f;
    const float LocalInitDelay = 0.15f;
    const string RunButtonObjectName = "RunButton";

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

    CharacterController controller;
    Animator animator;
    Image runButtonImage;
    Transform cam;

    Vector3 velocity;
    Vector2 smoothedInput;
    float gravity = -9.81f;
    bool isRunning;

    public bool IsRunning => isRunning;
    public float MoveInputAmount => smoothedInput.magnitude;
    public float HorizontalSpeed { get; private set; }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (animator != null)
            animator.Play(LocomotionStateName, 0, 0f);

        if (!photonView.IsMine)
            return;

        StartCoroutine(InitLocal());
    }

    void Update()
    {
        if (!photonView.IsMine || cam == null)
            return;

        Move();
        HandleAnimation();
    }

    IEnumerator InitLocal()
    {
        yield return new WaitForSeconds(LocalInitDelay);

        joystick = FindObjectOfType<Joystick>();
        BindRunButton();
        cam = Camera.main != null ? Camera.main.transform : null;
    }

    void BindRunButton()
    {
        GameObject runBtnObj = GameObject.Find(RunButtonObjectName);
        if (runBtnObj == null)
            return;

        runButton = runBtnObj.GetComponent<Button>();
        if (runButton == null)
            return;

        runButton.onClick.AddListener(ToggleRun);
        runButtonImage = runButton.GetComponent<Image>();
    }

    void ToggleRun()
    {
        isRunning = !isRunning;
        UpdateRunButtonVisual();
    }

    void UpdateRunButtonVisual()
    {
        if (runButtonImage == null)
            return;

        runButtonImage.color = isRunning ? runColor : walkColor;
    }

    void Move()
    {
        Vector2 rawInput = ReadRawInput();
        smoothedInput = SmoothInput(rawInput);

        Vector3 moveDir = GetCameraRelativeDirection(smoothedInput);
        Vector3 finalMove = BuildHorizontalMove(moveDir, out float speed);
        HorizontalSpeed = speed;

        ApplyGravity(ref finalMove);
        controller.Move(finalMove * Time.deltaTime);
    }

    Vector2 ReadRawInput()
    {
        Vector2 rawInput = Vector2.zero;

        if (joystick != null)
            rawInput = new Vector2(joystick.Horizontal, joystick.Vertical);

#if UNITY_EDITOR || UNITY_STANDALONE
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
        {
            rawInput.x = h;
            rawInput.y = v;
        }

        isRunning = Input.GetKey(KeyCode.LeftShift);
#endif

        return rawInput;
    }

    Vector2 SmoothInput(Vector2 rawInput)
    {
        return Vector2.Lerp(
            smoothedInput,
            rawInput,
            Time.deltaTime * inputSmooth);
    }

    Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        Vector3 inputDir = new Vector3(input.x, 0f, input.y);

        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        return camForward * inputDir.z + camRight * inputDir.x;
    }

    Vector3 BuildHorizontalMove(Vector3 moveDir, out float speed)
    {
        speed = 0f;

        if (moveDir.magnitude <= MoveInputDeadzone)
            return Vector3.zero;

        moveDir.Normalize();
        speed = isRunning ? runSpeed : walkSpeed;

        Quaternion targetRot = Quaternion.LookRotation(moveDir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            Time.deltaTime * rotationSpeed);

        return moveDir * speed;
    }

    void ApplyGravity(ref Vector3 finalMove)
    {
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = GroundedVelocityY;

        velocity.y += gravity * Time.deltaTime;
        finalMove.y = velocity.y;
    }

    void HandleAnimation()
    {
        if (animator == null)
            return;

        float blend = 0f;
        if (smoothedInput.magnitude > MoveInputDeadzone)
            blend = isRunning ? RunBlendValue : WalkBlendValue;

        animator.SetFloat(BlendParameterName, blend, BlendDampTime, Time.deltaTime);
    }
}
