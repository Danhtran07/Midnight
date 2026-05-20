using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI chat phòng — kéo thả reference trong Inspector.
/// Gán OnClick thủ công: Send → SendCurrentInput, Toggle → TogglePanel.
/// </summary>
public class RoomChatUI : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("Panel chat (ẩn/hiện). Nút Toggle phải đặt NGOÀI object này.")]
    public GameObject panelRoot;

    [Tooltip("Ẩn panel lúc Start nếu bật.")]
    public bool hidePanelOnStart = true;

    [Header("Log")]
    public ScrollRect scrollRect;
    public TMP_Text logText;

    [Header("Nhập tin")]
    public TMP_InputField inputField;
    public Button sendButton;

    [Header("Mở/đóng (tùy chọn)")]
    public Button toggleButton;
    public TMP_Text toggleLabel;

    [Header("Sự kiện")]
    [Tooltip("Bật chỉ khi KHÔNG gán OnClick / OnSubmit trong Inspector.")]
    public bool autoWireEvents;

    readonly StringBuilder logBuilder = new StringBuilder(2048);
    bool panelVisible;

    void Start()
    {
        if (logText == null)
            Debug.LogWarning("[RoomChatUI] Chưa gán Log Text — chat sẽ không hiển thị.");

        if (toggleButton != null && panelRoot != null &&
            toggleButton.transform.IsChildOf(panelRoot.transform))
        {
            Debug.LogWarning(
                "[RoomChatUI] Toggle Button đang nằm TRONG Panel Root. " +
                "Khi ẩn panel, nút Toggle cũng bị tắt. Hãy đặt nút Toggle ra ngoài panel.");
        }

        SetupInputField();

        if (autoWireEvents)
            WireEvents();

        if (hidePanelOnStart && panelRoot != null)
            SetPanelVisible(false);
        else
            panelVisible = panelRoot == null || panelRoot.activeSelf;

        RefreshLogFromHistory();

        if (RoomChatManager.Instance != null)
            RoomChatManager.Instance.OnMessageReceived += OnChatMessage;
    }

    void OnDestroy()
    {
        if (RoomChatManager.Instance != null)
            RoomChatManager.Instance.OnMessageReceived -= OnChatMessage;
    }

    void SetupInputField()
    {
        if (inputField == null) return;

        inputField.characterLimit = RoomChatManager.MaxMessageLength;
        inputField.lineType = TMP_InputField.LineType.SingleLine;
    }

    void WireEvents()
    {
        if (sendButton != null)
        {
            sendButton.onClick.RemoveListener(SendCurrentInput);
            sendButton.onClick.AddListener(SendCurrentInput);
        }

        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(TogglePanel);
            toggleButton.onClick.AddListener(TogglePanel);
        }

        if (inputField != null)
        {
            inputField.onSubmit.RemoveListener(OnInputSubmit);
            inputField.onSubmit.AddListener(OnInputSubmit);
        }
    }

    void OnInputSubmit(string _)
    {
        SendCurrentInput();
    }

    void OnChatMessage(ChatMessage msg)
    {
        AppendLine(msg.FormattedLine);
    }

    void RefreshLogFromHistory()
    {
        logBuilder.Clear();

        if (RoomChatManager.Instance != null)
        {
            foreach (ChatMessage msg in RoomChatManager.Instance.GetMessageLog())
                logBuilder.AppendLine(msg.FormattedLine);
        }

        ApplyLogText();
    }

    void AppendLine(string richLine)
    {
        if (logBuilder.Length > 0)
            logBuilder.Append('\n');

        logBuilder.Append(richLine);
        ApplyLogText();
    }

    void ApplyLogText()
    {
        if (logText == null) return;

        logText.text = logBuilder.ToString();

        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    /// <summary>Gán cho nút Gửi (OnClick).</summary>
    public void SendCurrentInput()
    {
        if (inputField == null || RoomChatManager.Instance == null)
            return;

        string text = inputField.text;
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (!RoomChatManager.Instance.TrySendMessage(text, out string error))
        {
            if (!string.IsNullOrEmpty(error))
                AppendLine($"<color=#FF8888>{error}</color>");
            return;
        }

        inputField.text = string.Empty;
        inputField.ActivateInputField();
    }

    /// <summary>Gán cho nút mở/đóng chat (OnClick). Chỉ gán MỘT lần (Inspector HOẶC autoWireEvents).</summary>
    public void TogglePanel()
    {
        SetPanelVisible(!panelVisible);
    }

    public void SetPanelVisible(bool visible)
    {
        panelVisible = visible;

        if (panelRoot != null)
            panelRoot.SetActive(visible);

        if (toggleLabel != null)
            toggleLabel.text = visible ? "Ẩn chat" : "Chat";
    }
}
