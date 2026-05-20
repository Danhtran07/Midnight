using System.Collections.Generic;
using System.Text;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
using UnityEngine;

/// <summary>
/// Gắn vào scene (kéo lên VoiceManager hoặc GameObject trống).
/// Bật overlay: phím F9 hoặc tick "Show On Screen HUD".
/// In báo cáo đầy đủ: Context Menu → "Dump Voice Report" hoặc phím F10.
/// </summary>
public class VoiceDebugMonitor : MonoBehaviour
{
    [Header("Display")]
    public bool showOnScreenHud = true;
    public KeyCode toggleHudKey = KeyCode.F9;
    public KeyCode dumpReportKey = KeyCode.F10;

    [Header("Logging")]
    public bool logToConsole = true;
    [Tooltip("0 = chỉ khi bấm F10 / context menu")]
    public float autoLogIntervalSeconds = 5f;

    [Header("Fix / test")]
    public bool increasePhotonVoiceLogLevel;
    public bool tryRelinkUnlinkedSpeakers = true;

    float nextAutoLogTime;
    readonly List<string> warnings = new List<string>();
    string lastReport = "";

    GUIStyle boxStyle;
    GUIStyle labelStyle;

    void Update()
    {
        if (Input.GetKeyDown(toggleHudKey))
            showOnScreenHud = !showOnScreenHud;

        if (Input.GetKeyDown(dumpReportKey))
            DumpVoiceReport();

        if (increasePhotonVoiceLogLevel)
            SetVoiceLogLevel(LogLevel.Trace);

        if (autoLogIntervalSeconds > 0f && Time.unscaledTime >= nextAutoLogTime)
        {
            nextAutoLogTime = Time.unscaledTime + autoLogIntervalSeconds;
            if (logToConsole)
                Debug.Log(BuildReport());
        }

        if (tryRelinkUnlinkedSpeakers)
            TryRelinkRemoteSpeakers();
    }

    void OnGUI()
    {
        if (!showOnScreenHud) return;

        InitGuiStyles();

        float w = Mathf.Min(520f, Screen.width - 20f);
        GUILayout.BeginArea(new Rect(10f, 10f, w, Screen.height - 20f), boxStyle);
        GUILayout.Label("<b>Voice Debug (F9 ẩn/hiện, F10 log)</b>", labelStyle);
        GUILayout.Label(GetHudSummary(), labelStyle);
        GUILayout.Space(6f);
        GUILayout.Label("<size=11>" + EscapeRichText(lastReport) + "</size>", labelStyle);
        GUILayout.EndArea();
    }

    void InitGuiStyles()
    {
        if (boxStyle != null) return;

        boxStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            padding = new RectOffset(8, 8, 8, 8)
        };
        boxStyle.normal.background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.75f));

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            richText = true,
            wordWrap = true,
            fontSize = 13
        };
        labelStyle.normal.textColor = Color.white;
    }

    static Texture2D MakeTex(int w, int h, Color col)
    {
        Color[] pix = new Color[w * h];
        for (int i = 0; i < pix.Length; i++) pix[i] = col;
        Texture2D tex = new Texture2D(w, h);
        tex.SetPixels(pix);
        tex.Apply();
        return tex;
    }

    static string EscapeRichText(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("<", "\\<");
    }

    string GetHudSummary()
    {
        warnings.Clear();
        var sb = new StringBuilder();

        AppendGlobalState(sb);

        PlayerVoice local = VoiceManager.Instance != null
            ? VoiceManager.Instance.LocalPlayerVoice
            : null;

        if (local != null)
            AppendPlayerLine(sb, local, isLocal: true);
        else
            warnings.Add("Chưa có LocalPlayerVoice (player local chưa spawn / chưa Register).");

        foreach (PhotonView pv in PhotonNetwork.PhotonViewCollection)
        {
            if (pv == null || pv.IsMine) continue;
            PlayerVoice pvVoice = pv.GetComponent<PlayerVoice>();
            if (pvVoice != null)
                AppendPlayerLine(sb, pvVoice, isLocal: false);
        }

        if (warnings.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("<color=#ffaa00><b>Cảnh báo:</b></color>");
            foreach (string w in warnings)
                sb.AppendLine("• " + w);
        }

        lastReport = sb.ToString();
        return lastReport;
    }

    void AppendGlobalState(StringBuilder sb)
    {
        sb.AppendLine($"PUN: {(PhotonNetwork.InRoom ? "InRoom" : PhotonNetwork.NetworkClientState.ToString())}");
        if (PhotonNetwork.InRoom)
            sb.AppendLine($"  Room: {PhotonNetwork.CurrentRoom.Name} | Players: {PhotonNetwork.CurrentRoom.PlayerCount}");

        PunVoiceClient pvc = PunVoiceClient.Instance;
        if (pvc == null)
        {
            warnings.Add("PunVoiceClient = null. Thêm Pun Voice Client vào scene.");
            sb.AppendLine("<color=#ff6666>Voice: NO PunVoiceClient</color>");
            return;
        }

        var client = pvc.Client;
        bool voiceInRoom = client != null && client.InRoom;
        string voiceRoom = voiceInRoom ? client.CurrentRoom.Name : "(chưa vào)";
        sb.AppendLine($"Voice client: {pvc.ClientState} | InRoom: {voiceInRoom}");
        sb.AppendLine($"  Voice room: {voiceRoom}");

        if (PhotonNetwork.InRoom && !voiceInRoom)
            warnings.Add("Đã vào PUN room nhưng chưa vào voice room → không nghe được ai.");

        string appIdVoice = PhotonNetwork.PhotonServerSettings?.AppSettings?.AppIdVoice;
        if (string.IsNullOrEmpty(appIdVoice))
            warnings.Add("AppIdVoice trống trong PhotonServerSettings.");

        if (VoiceManager.Instance != null)
        {
            var vm = VoiceManager.Instance;
            sb.AppendLine($"VoiceManager: active={vm.VoiceSystemActive} muteAll={vm.MuteAll} vol={vm.VoiceVolume:F2} micMode={vm.CurrentMicMode}");
        }
        else
        {
            warnings.Add("VoiceManager.Instance = null.");
        }

        AudioListener listener = FindObjectOfType<AudioListener>();
        if (listener == null)
            warnings.Add("Không có AudioListener trong scene.");
    }

    void AppendPlayerLine(StringBuilder sb, PlayerVoice voice, bool isLocal)
    {
        if (voice == null || voice.photonView == null) return;

        PhotonView pv = voice.photonView;
        string nick = pv.Owner != null ? pv.Owner.NickName : "?";
        int actor = pv.OwnerActorNr;
        float dist = -1f;
        if (!isLocal && Camera.main != null)
            dist = Vector3.Distance(Camera.main.transform.position, voice.transform.position);

        Recorder rec = voice.GetComponentInChildren<Recorder>(true);
        Speaker spk = voice.GetComponentInChildren<Speaker>(true);
        PhotonVoiceView pvv = voice.GetComponent<PhotonVoiceView>();

        if (isLocal)
        {
            bool tx = voice.IsMicTransmitting;
            bool recOn = rec != null && rec.RecordingEnabled;
            bool txOn = rec != null && rec.TransmitEnabled;
            float amp = 0f;
            if (rec?.LevelMeter != null)
                amp = rec.LevelMeter.CurrentAvgAmp;

            sb.AppendLine($"<b>[LOCAL] {nick}</b> micTX={tx} rec={recOn} transmit={txOn} amp={amp:F4}");

            if (!tx || !recOn || !txOn)
                warnings.Add("Mic local đang TẮT (Toggle/PushToTalk). Bật mic thì người khác mới nghe bạn.");

            if (rec != null && rec.UserData is int uid && uid != pv.ViewID)
                warnings.Add($"Recorder.UserData ({uid}) ≠ ViewID ({pv.ViewID}) → remote không link được speaker.");

            if (rec != null && !rec.IsCurrentlyTransmitting && txOn)
                warnings.Add("Transmit bật nhưng không có tín hiệu mic (quyền mic / thiết bị?).");
        }
        else
        {
            bool linked = spk != null && spk.IsLinked;
            bool playing = spk != null && spk.IsPlaying;
            AudioSource src = spk != null ? spk.GetComponent<AudioSource>() : null;
            bool muted = src != null && src.mute;
            float vol = src != null ? src.volume : -1f;
            float spatial = src != null ? src.spatialBlend : -1f;

            sb.AppendLine($"<b>[REMOTE] {nick}</b> #{actor} dist={dist:F1}m linked={linked} playing={playing} mute={muted} vol={vol:F2} 3D={spatial:F1}");

            if (!linked)
                warnings.Add($"Speaker chưa link với {nick} — stream voice chưa gắn vào AudioSource.");

            if (linked && !playing)
                warnings.Add($"{nick}: đã link nhưng không phát — có thể họ chưa bật mic hoặc im lặng.");

            if (dist >= 0f && dist > voice.maxDistance)
                warnings.Add($"{nick} quá xa ({dist:F0}m > max {voice.maxDistance}m) — spatial audio = 0.");

            if (VoiceManager.Instance != null && VoiceManager.Instance.MuteAll)
                warnings.Add("MuteAll đang bật — tất cả remote bị mute.");

            if (VoiceManager.Instance != null && VoiceManager.Instance.IsPlayerMuted(actor))
                warnings.Add($"{nick} bị mute trong VoiceManager.");

            if (pvv != null && spk != null && pvv.SpeakerInUse != spk)
                warnings.Add($"{nick}: Speaker PlayerVoice khác PhotonVoiceView.SpeakerInUse.");
        }
    }

    void TryRelinkRemoteSpeakers()
    {
        if (!PhotonNetwork.InRoom) return;

        PunVoiceClient pvc = PunVoiceClient.Instance;
        if (pvc == null) return;

        foreach (PhotonView pv in PhotonNetwork.PhotonViewCollection)
        {
            if (pv == null || pv.IsMine) continue;

            Speaker spk = pv.GetComponentInChildren<Speaker>(true);
            if (spk == null || spk.IsLinked) continue;

            bool linked = pvc.AddSpeaker(spk, pv.ViewID);
            if (linked && logToConsole)
                Debug.Log($"[VoiceDebug] Đã relink speaker cho ViewID {pv.ViewID} ({pv.Owner?.NickName})");
        }
    }

    [ContextMenu("Dump Voice Report")]
    public void DumpVoiceReport()
    {
        string report = BuildReport();
        Debug.Log(report);
        lastReport = GetHudSummary();
    }

    string BuildReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("========== VOICE DEBUG REPORT ==========");
        sb.Append(GetHudSummary().Replace("<b>", "").Replace("</b>", "")
            .Replace("<color=#ffaa00>", "").Replace("</color>", "")
            .Replace("<color=#ff6666>", "").Replace("</color>", ""));
        sb.AppendLine("========================================");
        return sb.ToString();
    }

    static void SetVoiceLogLevel(LogLevel level)
    {
#if UNITY_6000_0_OR_NEWER
        VoiceLogger[] loggers = FindObjectsByType<VoiceLogger>(FindObjectsSortMode.InstanceID);
#else
        VoiceLogger[] loggers = FindObjectsOfType<VoiceLogger>();
#endif
        foreach (VoiceLogger vl in loggers)
            vl.LogLevel = level;
    }
}
