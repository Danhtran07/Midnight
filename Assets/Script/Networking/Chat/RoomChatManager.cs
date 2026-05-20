using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// Chat text trong phòng Photon (PUN RaiseEvent). Không cần Photon Chat AppId.
/// </summary>
public class RoomChatManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    public static RoomChatManager Instance { get; private set; }

    public const byte ChatEventCode = 101;

    public const int MaxMessageLength = 200;
    public const int MaxLogMessages = 80;

    [Header("Rate limit")]
    public float minSecondsBetweenSends = 0.4f;

    public event Action<ChatMessage> OnMessageReceived;

    readonly List<ChatMessage> messageLog = new List<ChatMessage>();
    float lastSendTime = -999f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public override void OnJoinedRoom()
    {
        PlayerNameService.ApplyToPhoton();
        string name = GetDisplayName(PhotonNetwork.LocalPlayer);
        AddLocalSystemMessage($"Bạn đã vào phòng. Xin chào, {name}!");
    }

    public override void OnLeftRoom()
    {
        messageLog.Clear();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (newPlayer == null || newPlayer.IsLocal)
            return;

        AddLocalSystemMessage($"{GetDisplayName(newPlayer)} đã vào phòng.");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (otherPlayer == null)
            return;

        AddLocalSystemMessage($"{GetDisplayName(otherPlayer)} đã rời phòng.");
    }

    public IReadOnlyList<ChatMessage> GetMessageLog() => messageLog;

    public bool TrySendMessage(string rawText, out string error)
    {
        error = null;

        if (!PhotonNetwork.InRoom)
        {
            error = "Chưa vào phòng.";
            return false;
        }

        string text = SanitizeMessage(rawText);
        if (string.IsNullOrEmpty(text))
        {
            error = "Nội dung trống.";
            return false;
        }

        if (Time.unscaledTime - lastSendTime < minSecondsBetweenSends)
        {
            error = "Gửi quá nhanh.";
            return false;
        }

        lastSendTime = Time.unscaledTime;

        PlayerNameService.ApplyToPhoton();

        string senderName = GetDisplayName(PhotonNetwork.LocalPlayer);
        object[] payload = { PhotonNetwork.LocalPlayer.ActorNumber, senderName, text };

        var options = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        bool ok = PhotonNetwork.RaiseEvent(ChatEventCode, payload, options, SendOptions.SendReliable);

        if (!ok)
            error = "Không gửi được tin nhắn.";

        return ok;
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code != ChatEventCode)
            return;

        object[] data = photonEvent.CustomData as object[];
        if (data == null || data.Length < 2)
            return;

        if (!(data[0] is int actorNumber))
            return;

        string text;
        string senderName;

        // Payload mới: [actor, tên, nội dung] — luôn hiển thị đúng tên trên mọi máy
        if (data.Length >= 3)
        {
            senderName = data[1] as string;
            text = data[2] as string;
        }
        else
        {
            text = data[1] as string;
            senderName = null;
        }

        if (string.IsNullOrEmpty(text))
            return;

        Player sender = PhotonNetwork.CurrentRoom?.GetPlayer(actorNumber);
        senderName = ResolveSenderName(senderName, sender, actorNumber);

        bool isLocal = sender != null && sender.IsLocal;

        PublishMessage(new ChatMessage
        {
            SenderName = senderName,
            Text = text,
            IsSystem = false,
            IsLocal = isLocal
        });
    }

    void AddLocalSystemMessage(string text)
    {
        PublishMessage(new ChatMessage
        {
            SenderName = "Hệ thống",
            Text = text,
            IsSystem = true,
            IsLocal = false
        });
    }

    void PublishMessage(ChatMessage msg)
    {
        messageLog.Add(msg);

        while (messageLog.Count > MaxLogMessages)
            messageLog.RemoveAt(0);

        OnMessageReceived?.Invoke(msg);
    }

    public static string GetDisplayName(Player player)
    {
        if (player == null)
            return "???";

        if (player.IsLocal && PlayerNameService.HasValidName)
            return PlayerNameService.DisplayName;

        if (!string.IsNullOrWhiteSpace(player.NickName))
            return player.NickName.Trim();

        return "Player " + player.ActorNumber;
    }

    static string ResolveSenderName(string nameFromEvent, Player sender, int actorNumber)
    {
        if (!string.IsNullOrWhiteSpace(nameFromEvent))
            return SanitizeSenderName(nameFromEvent);

        return GetDisplayName(sender);
    }

    static string SanitizeSenderName(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        raw = raw.Replace("\r", "").Replace("\n", "").Trim();

        if (raw.Length > PlayerNameService.MaxLength)
            raw = raw.Substring(0, PlayerNameService.MaxLength);

        return raw;
    }

    public static string SanitizeMessage(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        raw = raw.Replace("\r", " ").Replace("\n", " ").Trim();

        if (raw.Length > MaxMessageLength)
            raw = raw.Substring(0, MaxMessageLength);

        return raw;
    }
}

public struct ChatMessage
{
    public string SenderName;
    public string Text;
    public bool IsSystem;
    public bool IsLocal;

    public string FormattedLine
    {
        get
        {
            if (IsSystem)
                return $"<color=#AAAAAA><i>{Text}</i></color>";

            string name = string.IsNullOrWhiteSpace(SenderName) ? "???" : SenderName.Trim();
            string color = IsLocal ? "#7FD4FF" : "#FFFFFF";
            return $"<color={color}><b>{name}:</b></color> {Text}";
        }
    }
}
