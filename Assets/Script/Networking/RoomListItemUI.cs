using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One row in the room list. Assign to room item prefab.
/// </summary>
public class RoomListItemUI : MonoBehaviour
{
    public TMP_Text roomNameText;
    public TMP_Text playersText;
    public Button joinButton;

    string roomName;

    public void Setup(string name, int playerCount, int maxPlayers)
    {
        roomName = name;

        if (roomNameText != null)
            roomNameText.text = name;

        if (playersText != null)
            playersText.text = playerCount + " / " + maxPlayers;

        if (joinButton != null)
        {
            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(OnJoinClicked);
        }
    }

    void OnJoinClicked()
    {
        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogWarning("[RoomListItemUI] roomName empty — Setup() chưa được gọi?");
            return;
        }

        if (Launcher.Instance == null)
        {
            Debug.LogError("[RoomListItemUI] Launcher.Instance is null.");
            return;
        }

        Debug.Log("[RoomListItemUI] Join: " + roomName);
        Launcher.Instance.JoinRoom(roomName);
    }
}
