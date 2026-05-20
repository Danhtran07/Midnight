using TMPro;
using UnityEngine;

/// <summary>
/// UI on CreateRoomScene — delegates room creation to Launcher singleton.
/// Do NOT add Photon callbacks here (only Launcher handles them).
/// </summary>
public class CreateRoomManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField roomInput;

    public void OnCreateButtonClicked()
    {
        if (Launcher.Instance == null)
        {
            Debug.LogError("[CreateRoomManager] Launcher.Instance is null. Play from MainMenu.");
            return;
        }

        if (!PlayerNameService.HasValidName)
        {
            Debug.LogWarning("[CreateRoomManager] Nhập tên ở MainMenu trước.");
            return;
        }

        string roomName = roomInput != null ? roomInput.text : string.Empty;
        Launcher.Instance.CreateRoom(roomName);
    }

    public void OnBackButtonClicked()
    {
        Launcher.Instance?.BackToMainMenu();
    }
}
