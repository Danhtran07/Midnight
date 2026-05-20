using System.Collections;
using Photon.Pun;
using Photon.Voice;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
using POpusCodec.Enums;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

/// <summary>
/// Voice per-player (PUN 2 + Photon Voice 2).
/// Works with PhotonVoiceView for stream routing (ViewID = Recorder.UserData).
/// </summary>
[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(PhotonVoiceView))]
[DefaultExecutionOrder(-50)]
public class PlayerVoice : MonoBehaviourPun
{
    public const string PrefsMicDevice = "Voice_InputDevice";

    const string VoiceRecorderChild = "VoiceRecorder";
    const string VoiceAudioChild = "VoiceAudio";

    [Header("Spatial Audio")]
    public float minDistance = 1.5f;
    public float maxDistance = 22f;

    [Header("Voice Output (optional)")]
    public AudioSource voiceAudioSource;

    [Header("Debug")]
    public bool debugVoiceLogs = true;
    [Tooltip("Log mic state every N seconds (0 = off)")]
    public float debugLogInterval = 3f;

    Recorder recorder;
    Speaker speaker;
    PhotonVoiceView photonVoiceView;

    bool micTransmitting;
    bool setupDone;
    bool micActivationStarted;
    float nextDebugLogTime;

    public bool IsMicTransmitting => micTransmitting;
    public Recorder Recorder => GetActiveRecorder();

    /// <summary>Recorder Photon Voice đang dùng để transmit (ưu tiên PhotonVoiceView).</summary>
    public Recorder GetActiveRecorder()
    {
        if (photonVoiceView != null && photonVoiceView.RecorderInUse != null)
            return photonVoiceView.RecorderInUse;
        return recorder;
    }

    /// <summary>Áp dụng thiết bị micro Unity cho Photon Recorder.</summary>
    public void ApplyMicrophoneDevice(string deviceName)
    {
        if (!photonView.IsMine || string.IsNullOrEmpty(deviceName)) return;

        Recorder r = GetActiveRecorder();
        if (r == null) return;

        recorder = r;
        AssignPrimaryRecorder();

        r.SourceType = Recorder.InputSourceType.Microphone;
        r.MicrophoneType = Recorder.MicType.Unity;
        r.UseMicrophoneTypeFallback = true;
        r.MicrophoneDevice = new DeviceInfo(deviceName);

        if (debugVoiceLogs)
            Debug.Log($"[PlayerVoice] MicrophoneDevice = {deviceName}");
    }

    public void ApplySavedMicrophoneFromPrefs()
    {
        string saved = PlayerPrefs.GetString(PrefsMicDevice, "");
        if (string.IsNullOrEmpty(saved)) return;
        ApplyMicrophoneDevice(saved);
    }

    void Awake()
    {
        photonVoiceView = GetComponent<PhotonVoiceView>();
        CleanupDuplicateVoiceComponents();
        EnsureVoiceAudioSource();

        if (!setupDone)
            SetupVoice();
    }

    void Start()
    {
        if (photonView.IsMine)
            StartCoroutine(ActivateLocalMicWhenReady());
        else
            StartCoroutine(LinkRemoteSpeakerWhenReady());
    }

    void Update()
    {
        if (!debugVoiceLogs || !photonView.IsMine || recorder == null) return;
        if (debugLogInterval <= 0f) return;
        if (Time.unscaledTime < nextDebugLogTime) return;
        nextDebugLogTime = Time.unscaledTime + debugLogInterval;
        LogVoiceState("periodic");
    }

    void OnDestroy()
    {
        if (VoiceManager.Instance != null)
            VoiceManager.Instance.Unregister(this);
    }

    public void SetupVoice()
    {
        if (setupDone) return;
        setupDone = true;

        if (photonView.IsMine)
            SetupLocalPlayer();
        else
            SetupRemotePlayer();

        VoiceManager.Instance?.Register(this);
    }

    /// <summary>Called from VoiceManager after local player registers.</summary>
    public void OnRegisteredAsLocal()
    {
        if (!photonView.IsMine || micActivationStarted) return;
        StartCoroutine(ActivateLocalMicWhenReady());
    }

    void CleanupDuplicateVoiceComponents()
    {
        Transform audioRoot = transform.Find(VoiceAudioChild);

        Recorder[] recorders = GetComponentsInChildren<Recorder>(true);
        Recorder keepRecorder = null;
        Transform recRoot = transform.Find(VoiceRecorderChild);
        if (recRoot != null)
            keepRecorder = recRoot.GetComponent<Recorder>();

        for (int i = 0; i < recorders.Length; i++)
        {
            Recorder r = recorders[i];
            if (r == null) continue;
            if (keepRecorder == null && r.transform.parent == transform)
            {
                r.transform.SetParent(transform, false);
                if (r.gameObject.name != VoiceRecorderChild)
                    r.gameObject.name = VoiceRecorderChild;
                keepRecorder = r;
                continue;
            }
            if (r != keepRecorder)
                Destroy(r);
        }

        Speaker[] speakers = GetComponentsInChildren<Speaker>(true);
        for (int i = 0; i < speakers.Length; i++)
        {
            Speaker s = speakers[i];
            if (s == null) continue;
            bool onVoiceAudio = audioRoot != null && s.transform == audioRoot;
            bool onRecorder = keepRecorder != null && s.transform == keepRecorder.transform;
            if (photonView != null && photonView.IsMine)
            {
                DisableLocalSpeaker(s);
                continue;
            }
            if (!onVoiceAudio && !onRecorder)
                Destroy(s);
        }
    }

    void EnsureVoiceAudioSource()
    {
        if (voiceAudioSource != null) return;

        Transform voiceRoot = transform.Find(VoiceAudioChild);
        if (voiceRoot == null)
        {
            GameObject go = new GameObject(VoiceAudioChild);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            voiceRoot = go.transform;
        }

        voiceAudioSource = voiceRoot.GetComponent<AudioSource>();
        if (voiceAudioSource == null)
            voiceAudioSource = voiceRoot.gameObject.AddComponent<AudioSource>();
    }

    void SetupLocalPlayer()
    {
        recorder = EnsureRecorder();
        DisableAllLocalSpeakers();

        ConfigureRecorderForLocal();
        AssignPrimaryRecorder();
        BindRecorderUserData();
    }

    void SetupRemotePlayer()
    {
        if (recorder != null)
        {
            recorder.enabled = false;
            recorder.RecordingEnabled = false;
            recorder.TransmitEnabled = false;
        }

        speaker = EnsureRemoteSpeaker();
        ConfigureSpatialSpeaker();
        ApplyMuteState();
    }

    Recorder EnsureRecorder()
    {
        Transform recRoot = transform.Find(VoiceRecorderChild);
        if (recRoot == null)
        {
            GameObject go = new GameObject(VoiceRecorderChild);
            go.transform.SetParent(transform, false);
            recRoot = go.transform;
        }

        Recorder r = recRoot.GetComponent<Recorder>();
        if (r == null)
            r = recRoot.gameObject.AddComponent<Recorder>();

        recorder = r;
        return recorder;
    }

    Speaker EnsureRemoteSpeaker()
    {
        EnsureVoiceAudioSource();
        GameObject target = voiceAudioSource.gameObject;

        Speaker sp = target.GetComponent<Speaker>();
        if (sp == null)
            sp = target.AddComponent<Speaker>();

        speaker = sp;
        return speaker;
    }

    void DisableAllLocalSpeakers()
    {
        Speaker[] speakers = GetComponentsInChildren<Speaker>(true);
        for (int i = 0; i < speakers.Length; i++)
            DisableLocalSpeaker(speakers[i]);
    }

    static void DisableLocalSpeaker(Speaker sp)
    {
        if (sp == null) return;
        sp.enabled = false;
        AudioSource src = sp.GetComponent<AudioSource>();
        if (src != null)
        {
            src.mute = true;
            src.volume = 0f;
            src.spatialBlend = 0f;
        }
    }

    void ConfigureRecorderForLocal()
    {
        if (recorder == null) return;

        recorder.enabled = true;
        recorder.SourceType = Recorder.InputSourceType.Microphone;
        recorder.MicrophoneType = Recorder.MicType.Unity;
        recorder.UseMicrophoneTypeFallback = true;
        recorder.RecordWhenJoined = true;
        recorder.StopRecordingWhenPaused = false;
        recorder.DebugEchoMode = false;
        recorder.ReliableMode = false;
        recorder.VoiceDetection = false;
        recorder.RecordingEnabled = false;
        recorder.TransmitEnabled = false;

        if (VoiceManager.Instance != null)
        {
            ApplyRecorderSettings(
                VoiceManager.Instance.samplingRate,
                VoiceManager.Instance.frameDurationMs,
                VoiceManager.Instance.bitrate
            );
        }
    }

    void AssignPrimaryRecorder()
    {
        if (recorder == null) return;

        PunVoiceClient client = PunVoiceClient.Instance;
        if (client == null) return;

        client.PrimaryRecorder = recorder;

        if (debugVoiceLogs)
            Debug.Log($"[PlayerVoice] PrimaryRecorder assigned on PunVoiceClient ({recorder.name})");
    }

    void BindRecorderUserData()
    {
        if (recorder == null || photonView == null) return;
        recorder.UserData = photonView.ViewID;
    }

    IEnumerator ActivateLocalMicWhenReady()
    {
        if (!photonView.IsMine || micActivationStarted) yield break;
        micActivationStarted = true;

        yield return RequestMicrophonePermission();

        PunVoiceClient client = null;
        float timeout = 30f;
        while (timeout > 0f)
        {
            client = PunVoiceClient.Instance;
            if (client != null && client.Client != null && client.Client.InRoom)
                break;
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (client == null || client.Client == null || !client.Client.InRoom)
        {
            Debug.LogWarning("[PlayerVoice] Voice client chưa vào room — mic sẽ không hoạt động.");
            yield break;
        }

        yield return null;
        yield return null;

        if (recorder == null)
            recorder = EnsureRecorder();

        AssignPrimaryRecorder();
        BindRecorderUserData();

        if (photonVoiceView != null && photonVoiceView.RecorderInUse != null)
            recorder = photonVoiceView.RecorderInUse;

        ApplySavedMicrophoneFromPrefs();

        bool shouldTransmit = ShouldAutoEnableMicOnSpawn();
        SetMicTransmitting(shouldTransmit, forceRestart: true);

        if (debugVoiceLogs)
            LogVoiceState("mic-activated");
    }

    IEnumerator LinkRemoteSpeakerWhenReady()
    {
        if (photonView.IsMine) yield break;

        PunVoiceClient client = null;
        float timeout = 30f;
        while (timeout > 0f)
        {
            client = PunVoiceClient.Instance;
            if (client != null && client.Client != null && client.Client.InRoom)
                break;
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        yield return null;

        speaker = EnsureRemoteSpeaker();
        ConfigureSpatialSpeaker();
        ApplyMuteState();

        if (client != null && speaker != null && !speaker.IsLinked)
        {
            bool linked = client.AddSpeaker(speaker, photonView.ViewID);
            if (debugVoiceLogs)
                Debug.Log($"[PlayerVoice] Remote AddSpeaker view={photonView.ViewID} linked={linked} owner={photonView.Owner?.NickName}");
        }
    }

    bool ShouldAutoEnableMicOnSpawn()
    {
        if (VoiceManager.Instance == null)
            return true;

        return VoiceManager.Instance.CurrentMicMode != VoiceManager.MicMode.PushToTalk;
    }

#if UNITY_ANDROID
    static IEnumerator RequestMicrophonePermission()
    {
        if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
            yield break;

        Permission.RequestUserPermission(Permission.Microphone);

        float timeout = 10f;
        while (timeout > 0f && !Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Debug.LogWarning("[PlayerVoice] Microphone permission not granted on Android.");
    }
#else
    static IEnumerator RequestMicrophonePermission()
    {
        yield break;
    }
#endif

    public void ApplyRecorderSettings(int sampleRate, int frameMs, int bitRate)
    {
        if (recorder == null) return;
        recorder.SamplingRate = MapSamplingRate(sampleRate);
        recorder.FrameDuration = MapFrameDuration(frameMs);
        recorder.Bitrate = bitRate;
    }

    static SamplingRate MapSamplingRate(int sampleRate)
    {
        switch (sampleRate)
        {
            case 8000: return SamplingRate.Sampling08000;
            case 12000: return SamplingRate.Sampling12000;
            case 16000: return SamplingRate.Sampling16000;
            case 24000: return SamplingRate.Sampling24000;
            case 48000: return SamplingRate.Sampling48000;
            default: return SamplingRate.Sampling24000;
        }
    }

    static OpusCodec.FrameDuration MapFrameDuration(int frameMs)
    {
        switch (frameMs)
        {
            case 20: return OpusCodec.FrameDuration.Frame20ms;
            case 40: return OpusCodec.FrameDuration.Frame40ms;
            case 60: return OpusCodec.FrameDuration.Frame60ms;
            default: return OpusCodec.FrameDuration.Frame20ms;
        }
    }

    void ConfigureSpatialSpeaker()
    {
        if (speaker == null) return;
        speaker.enabled = true;

        AudioSource src = speaker.GetComponent<AudioSource>();
        if (src == null) src = voiceAudioSource;
        if (src == null) return;

        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = minDistance;
        src.maxDistance = maxDistance;
        src.dopplerLevel = 0f;
        src.playOnAwake = false;
        src.loop = false;
        src.mute = false;
    }

    public void ApplyMuteState()
    {
        if (photonView.IsMine || speaker == null) return;

        AudioSource src = speaker.GetComponent<AudioSource>();
        if (src == null) return;

        bool muted = VoiceManager.Instance != null &&
                     (VoiceManager.Instance.MuteAll || VoiceManager.Instance.IsPlayerMuted(photonView.OwnerActorNr));
        float volume = VoiceManager.Instance != null ? VoiceManager.Instance.defaultVoiceVolume : 1f;

        src.mute = muted;
        src.volume = muted ? 0f : volume;
    }

    public void OnRemoteLeft()
    {
        if (speaker != null) speaker.enabled = false;
    }

    public void OnMicModeChanged()
    {
        if (!photonView.IsMine) return;

        if (VoiceManager.Instance != null &&
            VoiceManager.Instance.CurrentMicMode == VoiceManager.MicMode.PushToTalk)
        {
            SetMicTransmitting(false);
        }
        else
        {
            SetMicTransmitting(true, forceRestart: true);
        }
    }

    public void ToggleMic()
    {
        if (!photonView.IsMine) return;
        SetMicTransmitting(!micTransmitting, forceRestart: true);
    }

    public void SetMicPressed(bool pressed)
    {
        if (!photonView.IsMine) return;

        if (VoiceManager.Instance != null &&
            VoiceManager.Instance.CurrentMicMode == VoiceManager.MicMode.Toggle)
            return;

        SetMicTransmitting(pressed, forceRestart: true);
    }

    public void SetMicTransmitting(bool transmit)
    {
        SetMicTransmitting(transmit, forceRestart: false);
    }

    public void SetMicTransmitting(bool transmit, bool forceRestart)
    {
        Recorder r = GetActiveRecorder();
        if (!photonView.IsMine || r == null) return;

        recorder = r;
        AssignPrimaryRecorder();

        micTransmitting = transmit;
        r.TransmitEnabled = transmit;
        r.RecordingEnabled = transmit;

        if (transmit || forceRestart)
            r.RestartRecording();

        if (debugVoiceLogs)
            LogVoiceState(transmit ? "mic-ON" : "mic-OFF");
    }

    void LogVoiceState(string tag)
    {
        Recorder r = GetActiveRecorder();
        if (r == null) return;

        float amp = r.LevelMeter != null ? r.LevelMeter.CurrentAvgAmp : 0f;
        float peak = r.LevelMeter != null ? r.LevelMeter.CurrentPeakAmp : 0f;
        string micName = r.MicrophoneDevice.Name;

        Debug.Log(
            $"[PlayerVoice:{tag}] user={photonView.Owner?.NickName} view={photonView.ViewID} " +
            $"RecordingEnabled={r.RecordingEnabled} TransmitEnabled={r.TransmitEnabled} " +
            $"IsCurrentlyTransmitting={r.IsCurrentlyTransmitting} amp={amp:F4} peak={peak:F4} " +
            $"micType={r.MicrophoneType} mic={micName} micTX={micTransmitting}");
    }
}
