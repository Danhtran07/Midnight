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
    bool keyboardRunHeld;

    public bool IsRunning => isRunning || keyboardRunHeld;
    public float MoveInputAmount => smoothedInput.magnitude;
    public float HorizontalSpeed { get; private set; }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        if (animator != null)
            animator.Play(LocomotionStateName, 0, 0f);

        if (!IsLocalPlayer())
            return;

        StartCoroutine(InitLocal());
    }

    void Update()
    {
        if (!IsLocalPlayer() || cam == null)
            return;

        Move();
        HandleAnimation();
    }

    IEnumerator InitLocal()
    {
        yield return new WaitForSeconds(LocalInitDelay);

        joystick = FindObjectOfType<Joystick>();
        CacheCamera();
        BindRunButtonIfNeeded();
    }

    bool IsLocalPlayer()
    {
        return photonView == null || photonView.IsMine;
    }

    void CacheCamera()
    {
        cam = Camera.main != null ? Camera.main.transform : null;
        if (cam == null)
            Debug.LogWarning("[ThirdPersonController] Main Camera not found.", this);
    }

    void BindRunButtonIfNeeded()
    {
        if (runButton == null)
            runButton = RunButtonUtility.FindButton(RunButtonObjectName);

        runButtonImage = RunButtonUtility.Bind(runButton, ToggleRun);
        UpdateRunButtonVisual();
    }

    public void ToggleRun()
    {
        SetRunning(!isRunning);
    }

    void SetRunning(bool running)
    {
        if (isRunning == running)
            return;

        isRunning = running;
        UpdateRunButtonVisual();
    }

    void UpdateRunButtonVisual()
    {
        RunButtonUtility.ApplyColor(runButtonImage, IsRunning, runColor, walkColor);
    }

    void Move()
    {
        Vector2 rawInput = PlayerInputReader.ReadMoveInput(joystick);
        bool hadKeyboardRun = keyboardRunHeld;
        keyboardRunHeld = PlayerInputReader.TryReadKeyboardRun(out bool isKeyboardRunHeld) && isKeyboardRunHeld;

        if (hadKeyboardRun != keyboardRunHeld)
            UpdateRunButtonVisual();

        smoothedInput = SmoothInput(rawInput);

        Vector3 moveDir = GetCameraRelativeDirection(smoothedInput);
        Vector3 finalMove = BuildHorizontalMove(moveDir, out float speed);
        HorizontalSpeed = speed;

        ApplyGravity(ref finalMove);
        controller.Move(finalMove * Time.deltaTime);
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
        speed = IsRunning ? runSpeed : walkSpeed;

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
            blend = IsRunning ? RunBlendValue : WalkBlendValue;

        animator.SetFloat(BlendParameterName, blend, BlendDampTime, Time.deltaTime);
    }
}
