using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.PUN;
using UnityEngine;

public class VoiceManager : MonoBehaviourPunCallbacks
{
    public static VoiceManager Instance { get; private set; }

    public enum MicMode
    {
        Toggle,
        PushToTalk
    }

    [Header("Mic")]
    public MicMode defaultMicMode = MicMode.Toggle;

    [Header("Voice Volume")]
    [Range(0f, 2f)]
    public float defaultVoiceVolume = 1f;

    [Header("Recorder Settings")]
    public int samplingRate = 24000;
    public int frameDurationMs = 20;
    public int bitrate = 28000;

    public bool VoiceSystemActive { get; private set; }
    public bool MuteAll { get; private set; }
    public float VoiceVolume { get; private set; }
    public MicMode CurrentMicMode { get; private set; }

    public PlayerVoice LocalPlayerVoice { get; private set; }

    readonly Dictionary<int, PlayerVoice> remoteVoices = new Dictionary<int, PlayerVoice>();
    readonly HashSet<int> mutedPlayers = new HashSet<int>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        VoiceVolume = defaultVoiceVolume;
        CurrentMicMode = defaultMicMode;

        ConfigurePunVoiceClient();
    }

    [Header("Photon Voice (fallback Speaker prefab)")]
    public GameObject speakerPrefab;

    public void ConfigurePunVoiceClient()
    {
        PunVoiceClient client = PunVoiceClient.Instance;
        if (client == null)
        {
            Debug.LogWarning("[VoiceManager] PunVoiceClient chưa có trong scene.");
            return;
        }

        client.UsePunAppSettings = true;
        client.UsePunAuthValues = true;
        client.AutoConnectAndJoin = true;

        if (client.UsePrimaryRecorder)
            Debug.LogWarning("[VoiceManager] PunVoiceClient 'Use Primary Recorder' nên TẮT trong Inspector (dùng Recorder trên player prefab).");

        if (client.SpeakerPrefab == null)
        {
            if (speakerPrefab == null)
                speakerPrefab = Resources.Load<GameObject>("VoiceSpeaker");

            if (speakerPrefab != null)
            {
                client.SpeakerPrefab = speakerPrefab;
                Debug.Log("[VoiceManager] SpeakerPrefab assigned: " + speakerPrefab.name);
            }
            else
            {
                Debug.LogError("[VoiceManager] SpeakerPrefab missing! Add Assets/Resources/VoiceSpeaker.prefab");
            }
        }
    }

    public override void OnJoinedRoom()
    {
        VoiceSystemActive = true;
        MuteAll = false;
        mutedPlayers.Clear();
    }

    public override void OnLeftRoom()
    {
        VoiceSystemActive = false;
        LocalPlayerVoice = null;
        remoteVoices.Clear();
        mutedPlayers.Clear();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (remoteVoices.TryGetValue(otherPlayer.ActorNumber, out PlayerVoice voice))
        {
            remoteVoices.Remove(otherPlayer.ActorNumber);
            mutedPlayers.Remove(otherPlayer.ActorNumber);
            if (voice != null) voice.OnRemoteLeft();
        }
    }

    public void Register(PlayerVoice voice)
    {
        if (voice == null || voice.photonView == null) return;

        if (voice.photonView.IsMine)
        {
            LocalPlayerVoice = voice;
            voice.ApplyRecorderSettings(samplingRate, frameDurationMs, bitrate);

            PunVoiceClient client = PunVoiceClient.Instance;
            if (client != null && voice.Recorder != null)
                client.PrimaryRecorder = voice.Recorder;

            voice.OnRegisteredAsLocal();
            return;
        }

        int actorNumber = voice.photonView.OwnerActorNr;
        remoteVoices[actorNumber] = voice;
        voice.ApplyMuteState();
    }

    public void Unregister(PlayerVoice voice)
    {
        if (voice == null || voice.photonView == null) return;

        if (voice.photonView.IsMine)
        {
            if (LocalPlayerVoice == voice) LocalPlayerVoice = null;
            return;
        }

        remoteVoices.Remove(voice.photonView.OwnerActorNr);
    }

    public void SetMicMode(MicMode mode)
    {
        CurrentMicMode = mode;
        LocalPlayerVoice?.OnMicModeChanged();
    }

    public void SetVoiceVolume(float volume)
    {
        VoiceVolume = Mathf.Clamp(volume, 0f, 2f);
        foreach (PlayerVoice v in remoteVoices.Values)
            v.ApplyMuteState();
    }

    public void SetMuteAll(bool mute)
    {
        MuteAll = mute;
        foreach (PlayerVoice v in remoteVoices.Values)
            v.ApplyMuteState();
    }

    public void SetPlayerMuted(int actorNumber, bool muted)
    {
        if (muted) mutedPlayers.Add(actorNumber);
        else mutedPlayers.Remove(actorNumber);

        if (remoteVoices.TryGetValue(actorNumber, out PlayerVoice voice))
            voice.ApplyMuteState();
    }

    public void TogglePlayerMute(int actorNumber)
    {
        SetPlayerMuted(actorNumber, !IsPlayerMuted(actorNumber));
    }

    public bool IsPlayerMuted(int actorNumber)
    {
        return mutedPlayers.Contains(actorNumber);
    }
}
