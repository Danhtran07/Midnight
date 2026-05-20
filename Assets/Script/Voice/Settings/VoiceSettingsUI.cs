using System.Collections;
using System.Collections.Generic;
using Photon.Voice;
using Photon.Voice.PUN;
using Photon.Voice.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using DeviceEnumeratorUnity = Photon.Voice.Unity.AudioInEnumerator;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

/// <summary>
/// Voice: nhận TẤT CẢ microphone (đầu vào), đầu ra, bật/tắt mic, popup, về MainMenu.
/// Kéo thả UI vào Inspector.
/// </summary>
public class VoiceSettingsUI : MonoBehaviour
{
    const string PrefsMic = PlayerVoice.PrefsMicDevice;
    const string PrefsOutput = "Voice_OutputDevice";

    [Header("Microphone (đầu vào)")]
    [FormerlySerializedAs("inputDropdown")]
    public TMP_Dropdown micDropdown;
    public Button btnRefreshMic;
    public TMP_Text micStatusText;

    [Header("Loa / đầu ra")]
    public TMP_Dropdown outputDropdown;
    [Tooltip("Ẩn chọn loa trên mobile (Unity không liệt kê thiết bị loa).")]
    public bool hideOutputOnMobile = true;

    [Header("Popup")]
    public VoicePopupAnimator popup;
    public Button btnOpenPopup;
    public Button btnClosePopup;

    [Header("Mic bật/tắt")]
    public Button btnToggleMic;
    public TMP_Text btnMicLabel;

    [Header("Menu")]
    public Button btnBackMainMenu;

    readonly List<string> micDevices = new List<string>();
    readonly List<string> outputDevices = new List<string>();

    Recorder recorder;
    bool micOn = true;

    void Start()
    {
        FixDropdownLayout();
        ConfigureOutputDropdownVisibility();

        if (micDropdown != null)
            micDropdown.onValueChanged.AddListener(OnMicChanged);

        if (outputDropdown != null)
            outputDropdown.onValueChanged.AddListener(OnOutputChanged);

        if (btnOpenPopup != null) btnOpenPopup.onClick.AddListener(OpenPopup);
        if (btnClosePopup != null) btnClosePopup.onClick.AddListener(ClosePopup);
        if (btnToggleMic != null) btnToggleMic.onClick.AddListener(ToggleMic);
        if (btnBackMainMenu != null) btnBackMainMenu.onClick.AddListener(BackToMainMenu);
        if (btnRefreshMic != null) btnRefreshMic.onClick.AddListener(RefreshMicList);

        StartCoroutine(InitRoutine());
    }

    IEnumerator InitRoutine()
    {
        yield return WaitForMicPermission();
        yield return WarmupMicrophoneList();
        RefreshMicList();
        RefreshOutputList();

        if (VoiceManager.Instance?.LocalPlayerVoice != null)
            micOn = VoiceManager.Instance.LocalPlayerVoice.IsMicTransmitting;

        UpdateMicButtonLabel();
    }

    IEnumerator WaitForMicPermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);

        float t = 15f;
        while (t > 0f && !Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            t -= Time.unscaledDeltaTime;
            yield return null;
        }
#else
        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
#endif
        yield return null;
    }

    public void OpenPopup()
    {
        popup?.Show();
        StartCoroutine(OpenPopupRoutine());
    }

    IEnumerator OpenPopupRoutine()
    {
        yield return WaitForMicPermission();
        yield return WarmupMicrophoneList();
        GetRecorder(forceRefresh: true);
        RefreshMicList();
        RefreshOutputList();
    }

    void FixDropdownLayout()
    {
        if (micDropdown == null || outputDropdown == null) return;

        RectTransform micRt = micDropdown.GetComponent<RectTransform>();
        RectTransform outRt = outputDropdown.GetComponent<RectTransform>();
        if (micRt == null || outRt == null) return;

        if (Vector2.Distance(micRt.anchoredPosition, outRt.anchoredPosition) < 5f)
        {
            micRt.anchoredPosition = new Vector2(micRt.anchoredPosition.x, 235f);
            outRt.anchoredPosition = new Vector2(outRt.anchoredPosition.x, 55f);
        }

        micDropdown.transform.SetAsLastSibling();
    }

    void ConfigureOutputDropdownVisibility()
    {
        if (outputDropdown == null) return;

#if UNITY_ANDROID || UNITY_IOS
        if (hideOutputOnMobile)
            outputDropdown.gameObject.SetActive(false);
#endif
    }

    IEnumerator WarmupMicrophoneList()
    {
        if (Microphone.devices != null && Microphone.devices.Length > 0)
            yield break;

        string device = null;
        try
        {
            Microphone.Start(device, false, 1, 16000);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[VoiceSettingsUI] Warmup mic: " + ex.Message);
            yield break;
        }

        float timeout = 2f;
        while (timeout > 0f && Microphone.GetPosition(device) <= 0)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        Microphone.End(device);
        yield return null;
    }

    public void ClosePopup() => popup?.Hide();

    public void BackToMainMenu()
    {
        ClosePopup();
        if (Launcher.Instance != null)
            Launcher.Instance.BackToMainMenu();
        else
            Debug.LogWarning("[VoiceSettingsUI] Launcher.Instance null.");
    }

    // --- Nhận TẤT CẢ microphone ---

    public void RefreshMicList()
    {
        if (micDropdown == null) return;

        micDevices.Clear();

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            micDevices.Add("(Chưa có quyền micro)");
            FillDropdown(micDropdown, micDevices);
            SetMicStatus("Cần cấp quyền microphone");
            return;
        }
#endif

        CollectUnityMicrophones(micDevices);

        if (micDevices.Count == 0)
        {
            micDevices.Add("(Không tìm thấy micro)");
            SetMicStatus("Không có thiết bị micro");
        }
        else
        {
            SetMicStatus($"Đã nhận {micDevices.Count} micro");
            Debug.Log("[VoiceSettingsUI] Microphones: " + string.Join(", ", micDevices));
        }

        FillDropdown(micDropdown, micDevices);

        string saved = PlayerPrefs.GetString(PrefsMic, "");
        int idx = FindIndex(micDevices, saved);
        if (idx < 0) idx = 0;

        if (micDevices[0].StartsWith("("))
            return;

        micDropdown.SetValueWithoutNotify(idx);
        ApplyMicDevice(idx);
    }

    void OnMicChanged(int index) => ApplyMicDevice(index);

    void ApplyMicDevice(int index)
    {
        if (index < 0 || index >= micDevices.Count) return;

        string name = micDevices[index];
        if (name.StartsWith("(")) return;

        PlayerPrefs.SetString(PrefsMic, name);
        PlayerPrefs.Save();

        PlayerVoice voice = VoiceManager.Instance?.LocalPlayerVoice;
        if (voice != null)
        {
            voice.ApplyMicrophoneDevice(name);
            recorder = voice.Recorder;
            if (micOn)
                voice.SetMicTransmitting(true, forceRestart: true);
        }
        else if (GetRecorder(forceRefresh: true))
        {
            recorder.MicrophoneType = Recorder.MicType.Unity;
            recorder.UseMicrophoneTypeFallback = true;
            recorder.MicrophoneDevice = new DeviceInfo(name);
            SyncPrimaryRecorder(recorder);
            if (micOn)
                SetMicTransmit(true);
        }
        else
        {
            SetMicStatus("Chưa có Recorder Photon — vào phòng trước");
            return;
        }

        SetMicStatus("Đang dùng: " + name);
    }

    // --- Đầu ra ---

    static void CollectUnityMicrophones(List<string> target)
    {
        using (var enumerator = new DeviceEnumeratorUnity(null))
        {
            foreach (DeviceInfo device in enumerator)
            {
                if (!string.IsNullOrEmpty(device.Name) && !target.Contains(device.Name))
                    target.Add(device.Name);
            }
        }

        if (target.Count > 0) return;

        string[] devices = Microphone.devices;
        if (devices == null) return;

        for (int i = 0; i < devices.Length; i++)
        {
            if (!string.IsNullOrEmpty(devices[i]) && !target.Contains(devices[i]))
                target.Add(devices[i]);
        }
    }

    public void RefreshOutputList()
    {
        if (outputDropdown == null || !outputDropdown.gameObject.activeInHierarchy) return;

        outputDevices.Clear();
        outputDevices.Add("Loa mặc định (hệ thống)");

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        outputDevices.Add("Tai nghe / loa (Windows)");
#elif UNITY_STANDALONE_OSX && !UNITY_EDITOR
        outputDevices.Add("Tai nghe / loa (macOS)");
#endif

        FillDropdown(outputDropdown, outputDevices);

        int idx = FindIndex(outputDevices, PlayerPrefs.GetString(PrefsOutput, "Default"));
        if (idx < 0) idx = 0;
        outputDropdown.SetValueWithoutNotify(idx);
        SaveOutput(idx);
    }

    void OnOutputChanged(int index) => SaveOutput(index);

    void SaveOutput(int index)
    {
        if (index < 0 || index >= outputDevices.Count) return;
        PlayerPrefs.SetString(PrefsOutput, outputDevices[index]);
        PlayerPrefs.Save();
    }

    // --- Bật / tắt mic ---

    public void ToggleMic()
    {
        micOn = !micOn;
        SetMicTransmit(micOn);
        UpdateMicButtonLabel();
    }

    void SetMicTransmit(bool on)
    {
        micOn = on;
        GetRecorder();

        PlayerVoice voice = VoiceManager.Instance?.LocalPlayerVoice;
        if (voice != null)
        {
            voice.SetMicTransmitting(on, forceRestart: true);
            return;
        }

        if (recorder != null)
        {
            recorder.RecordingEnabled = on;
            recorder.TransmitEnabled = on;
            if (on) recorder.RestartRecording();
        }
    }

    void UpdateMicButtonLabel()
    {
        if (btnMicLabel != null)
            btnMicLabel.text = micOn ? "Mic: BẬT" : "Mic: TẮT";
    }

    void SetMicStatus(string msg)
    {
        if (micStatusText != null)
            micStatusText.text = msg;
    }

    bool GetRecorder(bool forceRefresh = false)
    {
        if (forceRefresh)
            recorder = null;

        if (recorder != null) return true;

        PlayerVoice voice = VoiceManager.Instance?.LocalPlayerVoice;
        if (voice != null)
            recorder = voice.Recorder;
        else if (PunVoiceClient.Instance?.PrimaryRecorder != null)
            recorder = PunVoiceClient.Instance.PrimaryRecorder;

        if (recorder == null) return false;

        recorder.SourceType = Recorder.InputSourceType.Microphone;
        recorder.MicrophoneType = Recorder.MicType.Unity;
        recorder.UseMicrophoneTypeFallback = true;
        recorder.VoiceDetection = false;
        SyncPrimaryRecorder(recorder);
        return true;
    }

    static void SyncPrimaryRecorder(Recorder r)
    {
        PunVoiceClient client = PunVoiceClient.Instance;
        if (client != null && r != null)
            client.PrimaryRecorder = r;
    }

    static void FillDropdown(TMP_Dropdown dropdown, List<string> options)
    {
        dropdown.ClearOptions();
        dropdown.AddOptions(options);
    }

    static int FindIndex(List<string> list, string value)
    {
        for (int i = 0; i < list.Count; i++)
            if (list[i] == value) return i;
        return -1;
    }
}
