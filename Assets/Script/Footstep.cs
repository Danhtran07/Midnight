using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class MobileFootstepDebug : MonoBehaviour
{
    [Header("Joystick")]
    public Joystick joystick;

    [Header("Footstep Clips")]
    public AudioClip[] walkClips;
    public AudioClip[] runClips;

    [Header("Step Settings")]
    public float walkStepDelay = 0.5f;
    public float runStepDelay = 0.3f;
    public float moveThreshold = 0.1f;

    [Header("Audio")]
    public float volume = 1f;

    [Header("Run")]
    public bool isRunning = false;

    private CharacterController controller;
    private AudioSource audioSource;

    private float stepTimer;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();

        // Setup AudioSource
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 1f;

        Debug.Log("FOOTSTEP SYSTEM START");
    }

    void Update()
    {
        if (joystick == null)
        {
            Debug.LogWarning("Joystick NULL");
            return;
        }

        // Input joystick
        Vector2 moveInput = new Vector2(
            joystick.Horizontal,
            joystick.Vertical
        );

        // Debug joystick
        Debug.Log("Joystick Magnitude: " + moveInput.magnitude);

        // Auto run nếu kéo mạnh
        isRunning = moveInput.magnitude > 0.8f;

        // Kiểm tra grounded
        Debug.Log("Grounded: " + controller.isGrounded);

        // Nếu đang di chuyển
        if (controller.isGrounded &&
            moveInput.magnitude > moveThreshold)
        {
            stepTimer += Time.deltaTime;

            float currentDelay =
                isRunning ? runStepDelay : walkStepDelay;

            Debug.Log("Moving");

            if (stepTimer >= currentDelay)
            {
                Debug.Log("PLAY FOOTSTEP");

                PlayFootstep();

                stepTimer = 0f;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    void PlayFootstep()
    {
        AudioClip[] clips =
            isRunning ? runClips : walkClips;

        // Debug clip array
        Debug.Log("Clip Count: " + clips.Length);

        if (clips == null || clips.Length == 0)
        {
            Debug.LogWarning("NO FOOTSTEP CLIPS");
            return;
        }

        int randomIndex = Random.Range(0, clips.Length);

        AudioClip clip = clips[randomIndex];

        if (clip == null)
        {
            Debug.LogWarning("CLIP NULL");
            return;
        }

        Debug.Log("Playing Clip: " + clip.name);

        audioSource.pitch = Random.Range(0.95f, 1.05f);

        audioSource.PlayOneShot(clip, volume);
    }
}