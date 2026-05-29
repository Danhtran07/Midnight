using UnityEngine;
using Photon.Pun;

/// <summary>
/// Phát tiếng bước chân khi chân chạm đất (theo animation), có fallback theo quãng đường.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerFootstep : MonoBehaviourPun
{
    const float FootHeightEpsilon = 0.0008f;
    const float PitchMin = 0.94f;
    const float PitchMax = 1.06f;

    [Header("Clips")]
    public AudioClip[] walkClips;
    public AudioClip[] runClips;

    [Header("Foot bones (tự tìm nếu để trống)")]
    public Transform leftFoot;
    public Transform rightFoot;
    public Transform hips;

    [Header("Movement")]
    public float minMoveSpeed = 0.2f;
    public float minMoveInput = 0.12f;

    [Header("Step spacing (fallback khi không có bone)")]
    public float walkStepDistance = 0.55f;
    public float runStepDistance = 0.72f;

    [Header("Anti spam")]
    [Tooltip("Khoảng cách tối thiểu giữa 2 tiếng bước (giây)")]
    public float minStepInterval = 0.16f;

    [Header("Audio")]
    [Range(0f, 1f)]
    public float volume = 0.85f;

    CharacterController controller;
    ThirdPersonController movement;
    AudioSource footstepSource;

    readonly FootCycleTracker leftFootTracker = new FootCycleTracker();
    readonly FootCycleTracker rightFootTracker = new FootCycleTracker();

    float lastStepTime = -999f;
    float fallbackDistance;
    Vector3 lastBodyPosition;
    bool useBoneDetection;
    bool warnedNoClips;

    sealed class FootCycleTracker
    {
        public float PrevHeight;
        public bool WasGoingDown;
        public bool Initialized;
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<ThirdPersonController>();
        footstepSource = FootstepAudioUtility.GetOrCreate(transform);
        lastBodyPosition = transform.position;
    }

    void Start()
    {
        CacheFootBones();
        useBoneDetection = leftFoot != null && rightFoot != null && hips != null;
    }

    void Update()
    {
        if (!IsLocalPlayer())
            return;

        if (!CanPlayFootsteps(out bool running))
        {
            OnMovementStopped();
            return;
        }

        if (useBoneDetection)
            UpdateBoneFootsteps(running);
        else
            UpdateDistanceFootsteps(running);

        lastBodyPosition = transform.position;
    }

    bool IsLocalPlayer()
    {
        return photonView == null || photonView.IsMine;
    }

    void OnMovementStopped()
    {
        ResetTrackers();
        fallbackDistance = 0f;
        lastBodyPosition = transform.position;
    }

    void CacheFootBones()
    {
        Animator animator = GetComponentInChildren<Animator>();
        HumanoidBoneUtility.FootBones bones = HumanoidBoneUtility.Resolve(
            animator, transform, leftFoot, rightFoot, hips);

        leftFoot = bones.LeftFoot;
        rightFoot = bones.RightFoot;
        hips = bones.Hips;
    }

    bool CanPlayFootsteps(out bool running)
    {
        running = movement != null && movement.IsRunning;

        if (!CharacterGroundCheck.IsGrounded(controller, transform))
            return false;

        if (GetHorizontalSpeed() < minMoveSpeed)
            return false;

        if (movement != null && movement.MoveInputAmount < minMoveInput)
            return false;

        return true;
    }

    float GetHorizontalSpeed()
    {
        if (movement != null && movement.HorizontalSpeed > 0.01f)
            return movement.HorizontalSpeed;

        Vector3 vel = controller.velocity;
        return new Vector3(vel.x, 0f, vel.z).magnitude;
    }

    void UpdateBoneFootsteps(bool running)
    {
        UpdateFootStrike(leftFoot, leftFootTracker, running);
        UpdateFootStrike(rightFoot, rightFootTracker, running);
    }

    void UpdateFootStrike(Transform foot, FootCycleTracker tracker, bool running)
    {
        float height = foot.position.y - hips.position.y;
        bool goingDown = height < tracker.PrevHeight - FootHeightEpsilon;

        if (!tracker.Initialized)
        {
            tracker.PrevHeight = height;
            tracker.WasGoingDown = goingDown;
            tracker.Initialized = true;
            return;
        }

        if (tracker.WasGoingDown && !goingDown)
            TryPlayStep(running);

        tracker.PrevHeight = height;
        tracker.WasGoingDown = goingDown;
    }

    void UpdateDistanceFootsteps(bool running)
    {
        Vector3 delta = transform.position - lastBodyPosition;
        delta.y = 0f;
        fallbackDistance += delta.magnitude;

        float stepDistance = running ? runStepDistance : walkStepDistance;
        if (fallbackDistance < stepDistance)
            return;

        TryPlayStep(running);
        fallbackDistance -= stepDistance;
    }

    void TryPlayStep(bool running)
    {
        if (Time.time - lastStepTime < minStepInterval)
            return;

        PlayStep(running);
        lastStepTime = Time.time;
    }

    void ResetTrackers()
    {
        leftFootTracker.Initialized = false;
        rightFootTracker.Initialized = false;
        leftFootTracker.WasGoingDown = false;
        rightFootTracker.WasGoingDown = false;
    }

    /// <summary>Gọi từ Animation Event trên clip walk/run nếu cần sync tuyệt đối.</summary>
    public void OnFootstepAnimationEvent()
    {
        if (!IsLocalPlayer())
            return;

        if (!CanPlayFootsteps(out bool running))
            return;

        TryPlayStep(running);
    }

    void PlayStep(bool running)
    {
        AudioClip[] clips = ResolveClips(running);
        if (clips == null || clips.Length == 0)
            return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null)
            return;

        footstepSource.volume = volume;
        footstepSource.pitch = Random.Range(PitchMin, PitchMax);
        footstepSource.PlayOneShot(clip);
    }

    AudioClip[] ResolveClips(bool running)
    {
        AudioClip[] clips = running ? runClips : walkClips;

        if (clips == null || clips.Length == 0)
            clips = walkClips;

        if (clips == null || clips.Length == 0)
        {
            if (!warnedNoClips)
            {
                Debug.LogWarning("[PlayerFootstep] Gán AudioClip vào Walk Clips / Run Clips.", this);
                warnedNoClips = true;
            }
            return null;
        }

        return clips;
    }
}
