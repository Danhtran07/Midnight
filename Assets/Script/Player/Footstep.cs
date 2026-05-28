using UnityEngine;
using Photon.Pun;

/// <summary>
/// Phát tiếng bước chân khi chân chạm đất (theo animation), có fallback theo quãng đường.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerFootstep : MonoBehaviourPun
{
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
    Animator animator;
    AudioSource footstepSource;

    FootCycleTracker leftFootTracker = new FootCycleTracker();
    FootCycleTracker rightFootTracker = new FootCycleTracker();

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
        animator = GetComponentInChildren<Animator>();
        footstepSource = EnsureFootstepAudioSource();
        lastBodyPosition = transform.position;
    }

    void Start()
    {
        CacheFootBones();
        useBoneDetection = leftFoot != null && rightFoot != null && hips != null;
    }

    void Update()
    {
        if (photonView != null && !photonView.IsMine)
            return;

        if (!CanPlayFootsteps(out bool running))
        {
            ResetTrackers();
            fallbackDistance = 0f;
            lastBodyPosition = transform.position;
            return;
        }

        if (useBoneDetection)
        {
            UpdateFootStrike(leftFoot, leftFootTracker, running);
            UpdateFootStrike(rightFoot, rightFootTracker, running);
        }
        else
        {
            UpdateDistanceFallback(running);
        }

        lastBodyPosition = transform.position;
    }

    void CacheFootBones()
    {
        if (hips == null)
            hips = FindBone("mixamorig:Hips") ?? FindBone("Hips");

        if (animator != null)
        {
            if (leftFoot == null && animator.isHuman)
                leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            if (rightFoot == null && animator.isHuman)
                rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (hips == null && animator.isHuman)
                hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        }

        if (leftFoot == null)
            leftFoot = FindBone("mixamorig:LeftFoot") ?? FindBone("LeftFoot");
        if (rightFoot == null)
            rightFoot = FindBone("mixamorig:RightFoot") ?? FindBone("RightFoot");
    }

    Transform FindBone(string boneName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == boneName)
                return children[i];
        }
        return null;
    }

    bool CanPlayFootsteps(out bool running)
    {
        running = movement != null && movement.IsRunning;

        if (!IsGrounded())
            return false;

        float speed = GetHorizontalSpeed();
        if (speed < minMoveSpeed)
            return false;

        if (movement != null && movement.MoveInputAmount < minMoveInput)
            return false;

        return true;
    }

    bool IsGrounded()
    {
        if (controller.isGrounded)
            return true;

        return Physics.Raycast(
            transform.position + Vector3.up * 0.1f,
            Vector3.down,
            controller.height * 0.55f + 0.2f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);
    }

    float GetHorizontalSpeed()
    {
        if (movement != null && movement.HorizontalSpeed > 0.01f)
            return movement.HorizontalSpeed;

        Vector3 vel = controller.velocity;
        return new Vector3(vel.x, 0f, vel.z).magnitude;
    }

    void UpdateFootStrike(Transform foot, FootCycleTracker tracker, bool running)
    {
        float height = foot.position.y - hips.position.y;
        bool goingDown = height < tracker.PrevHeight - 0.0008f;

        if (!tracker.Initialized)
        {
            tracker.PrevHeight = height;
            tracker.WasGoingDown = goingDown;
            tracker.Initialized = true;
            return;
        }

        // Chân vừa chạm đất: đang hạ xuống → bắt đầu nhấc lên (điểm cực tiểu theo trục Y)
        if (tracker.WasGoingDown && !goingDown)
            TryPlayStep(running);

        tracker.PrevHeight = height;
        tracker.WasGoingDown = goingDown;
    }

    void UpdateDistanceFallback(bool running)
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
        if (photonView != null && !photonView.IsMine)
            return;

        bool running = movement != null && movement.IsRunning;
        if (!CanPlayFootsteps(out _))
            return;

        TryPlayStep(running);
    }

    AudioSource EnsureFootstepAudioSource()
    {
        Transform child = transform.Find("FootstepAudio");
        if (child != null)
        {
            AudioSource existing = child.GetComponent<AudioSource>();
            if (existing != null)
                return Configure(existing);
        }

        var go = new GameObject("FootstepAudio");
        go.transform.SetParent(transform, false);
        return Configure(go.AddComponent<AudioSource>());
    }

    static AudioSource Configure(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.mute = false;
        return source;
    }

    void PlayStep(bool running)
    {
        AudioClip[] clips = running ? runClips : walkClips;
        if (clips == null || clips.Length == 0)
        {
            clips = walkClips;
            if (clips == null || clips.Length == 0)
            {
                if (!warnedNoClips)
                {
                    Debug.LogWarning("[PlayerFootstep] Gán AudioClip vào Walk Clips / Run Clips.", this);
                    warnedNoClips = true;
                }
                return;
            }
        }

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null)
            return;

        footstepSource.volume = volume;
        footstepSource.pitch = Random.Range(0.94f, 1.06f);
        footstepSource.PlayOneShot(clip);
    }
}
