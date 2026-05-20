using System;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton Photon launcher — only ONE instance across all scenes (MainMenu only).
/// Handles connect, lobby, create/join room, scene sync.
/// </summary>
public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher Instance { get; private set; }

    [Header("Scene Names (Build Settings)")]
    public string mainMenuScene = "MainMenu";
    public string createRoomScene = "CreateRoomScene";
    public string roomListScene = "RoomListScene";
    public string gameScene = "NightMap";

    [Header("Room")]
    public byte defaultMaxPlayers = 10;

    [Header("Connection")]
    public bool connectOnStart = true;

    public bool IsLobbyReady => PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InLobby;

    /// <summary>Fired when client is in lobby and can create/join/list rooms.</summary>
    public event Action OnLobbyReady;

    /// <summary>Fired when Photon sends an updated room list (only in lobby).</summary>
    public event Action<List<RoomInfo>> OnRoomListUpdated;

    bool connectRequested;
    string pendingMenuScene;
    List<RoomInfo> cachedRoomList = new List<RoomInfo>();

    // ========================= AWAKE / SINGLETON =========================

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[Launcher] Duplicate Launcher destroyed: " + gameObject.name);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        PhotonNetwork.AutomaticallySyncScene = true;
        PlayerNameService.LoadSavedName();
        Debug.Log("[Launcher] Singleton initialized. AutomaticallySyncScene = true");
    }

    void Start()
    {
        if (connectOnStart)
            EnsureConnected();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // ========================= CONNECTION =========================

    /// <summary>Connect only if disconnected. Safe to call multiple times.</summary>
    public void EnsureConnected()
    {
        if (!connectOnStart && !connectRequested)
            connectRequested = true;

        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("[Launcher] Already connected. State=" + PhotonNetwork.NetworkClientState);
            HandleAlreadyConnected();
            return;
        }

        if (connectRequested)
        {
            Debug.Log("[Launcher] Connection already in progress...");
            return;
        }

        connectRequested = true;
        Debug.Log("[Launcher] ConnectUsingSettings()...");
        PhotonNetwork.ConnectUsingSettings();
    }

    void HandleAlreadyConnected()
    {
        connectRequested = false;

        if (PhotonNetwork.InLobby)
        {
            Debug.Log("[Launcher] Already in lobby.");
            OnLobbyReady?.Invoke();
            return;
        }

        if (PhotonNetwork.InRoom)
        {
            Debug.Log("[Launcher] Already in room: " + PhotonNetwork.CurrentRoom.Name);
            return;
        }

        if (PhotonNetwork.IsConnectedAndReady)
        {
            Debug.Log("[Launcher] Connected — joining lobby...");
            PhotonNetwork.JoinLobby();
        }
    }

    public void EnsureInLobby()
    {
        if (!PhotonNetwork.IsConnected)
        {
            EnsureConnected();
            return;
        }

        if (PhotonNetwork.InRoom)
        {
            Debug.Log("[Launcher] In room — leave room first to see room list.");
            return;
        }

        if (!PhotonNetwork.InLobby && PhotonNetwork.IsConnectedAndReady)
        {
            Debug.Log("[Launcher] JoinLobby()...");
            PhotonNetwork.JoinLobby();
        }
    }

    // ========================= PHOTON CALLBACKS =========================

    public override void OnConnectedToMaster()
    {
        connectRequested = false;
        Debug.Log("[Launcher] OnConnectedToMaster");

        PlayerNameService.ApplyToPhoton();

        if (!PhotonNetwork.InLobby && !PhotonNetwork.InRoom)
        {
            Debug.Log("[Launcher] JoinLobby()...");
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[Launcher] OnJoinedLobby — ready for create/join/list");
        OnLobbyReady?.Invoke();

        if (cachedRoomList.Count > 0)
            OnRoomListUpdated?.Invoke(cachedRoomList);

        TryLoadPendingMenuScene();
    }

    void TryLoadPendingMenuScene()
    {
        if (string.IsNullOrEmpty(pendingMenuScene))
            return;

        string scene = pendingMenuScene;
        pendingMenuScene = null;
        Debug.Log("[Launcher] Loading pending scene: " + scene);
        SceneManager.LoadScene(scene);
    }

    public override void OnLeftLobby()
    {
        Debug.Log("[Launcher] OnLeftLobby");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        cachedRoomList = roomList ?? new List<RoomInfo>();

        int visible = 0;
        foreach (RoomInfo room in cachedRoomList)
        {
            if (room == null || room.RemovedFromList)
                continue;
            visible++;
            Debug.Log("[Launcher] Room: " + room.Name + " | Players: " + room.PlayerCount + "/" + room.MaxPlayers);
        }

        Debug.Log("[Launcher] OnRoomListUpdate — visible rooms: " + visible);
        OnRoomListUpdated?.Invoke(cachedRoomList);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("[Launcher] OnCreatedRoom: " + PhotonNetwork.CurrentRoom.Name);
    }

    public override void OnJoinedRoom()
    {
        PlayerNameService.ApplyToPhoton();

        Debug.Log("[Launcher] OnJoinedRoom: " + PhotonNetwork.CurrentRoom.Name +
                  " Players=" + PhotonNetwork.CurrentRoom.PlayerCount +
                  " Master=" + PhotonNetwork.IsMasterClient);

        // Load game scene for ALL clients (fixes player 2 joining late from room list)
        string activeScene = SceneManager.GetActiveScene().name;
        if (activeScene != gameScene)
        {
            Debug.Log("[Launcher] LoadLevel: " + gameScene + " (from " + activeScene + ")");
            PhotonNetwork.LoadLevel(gameScene);
        }
        else
        {
            Debug.Log("[Launcher] Already in game scene: " + gameScene);
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[Launcher] OnLeftRoom");

        if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby)
        {
            Debug.Log("[Launcher] Re-joining lobby after leave room...");
            PhotonNetwork.JoinLobby();
        }
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError("[Launcher] OnCreateRoomFailed (" + returnCode + "): " + message);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError("[Launcher] OnJoinRoomFailed (" + returnCode + "): " + message);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        connectRequested = false;
        cachedRoomList.Clear();
        Debug.Log("[Launcher] OnDisconnected: " + cause);
    }

    // ========================= ROOM ACTIONS =========================

    public void CreateRoom(string roomName)
    {
        if (!EnsurePlayerNameSet())
            return;

        if (!PrepareForRoomTransaction())
            return;

        if (string.IsNullOrWhiteSpace(roomName))
            roomName = "Room_" + UnityEngine.Random.Range(1000, 9999);

        roomName = roomName.Trim();

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = defaultMaxPlayers,
            IsVisible = true,
            IsOpen = true
        };

        Debug.Log("[Launcher] CreateRoom: " + roomName);
        PhotonNetwork.CreateRoom(roomName, options);
    }

    public void JoinRoom(string roomName)
    {
        if (string.IsNullOrWhiteSpace(roomName))
        {
            Debug.LogWarning("[Launcher] JoinRoom — empty room name.");
            return;
        }

        if (!EnsurePlayerNameSet())
            return;

        roomName = roomName.Trim();

        if (!PrepareForRoomTransaction())
        {
            Debug.LogWarning("[Launcher] JoinRoom delayed — not in lobby yet. Room: " + roomName);
            return;
        }

        Debug.Log("[Launcher] JoinRoom: " + roomName);
        PhotonNetwork.JoinRoom(roomName);
    }

    bool PrepareForRoomTransaction()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning("[Launcher] Not connected — connecting...");
            EnsureConnected();
            return false;
        }

        if (PhotonNetwork.InRoom)
        {
            Debug.LogWarning("[Launcher] Already in a room.");
            return false;
        }

        if (!PhotonNetwork.InLobby)
        {
            Debug.Log("[Launcher] Not in lobby — joining lobby first...");
            PhotonNetwork.JoinLobby();
            return false;
        }

        return true;
    }

    public List<RoomInfo> GetCachedRoomList()
    {
        return cachedRoomList;
    }

    // ========================= SCENE NAVIGATION (menu — local LoadScene) =========================

    public void OpenCreateRoomScene()
    {
        if (!EnsurePlayerNameSet())
            return;

        OpenMenuScene(createRoomScene);
    }

    public void OpenRoomListScene()
    {
        if (!EnsurePlayerNameSet())
            return;

        OpenMenuScene(roomListScene);
    }

    void OpenMenuScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[Launcher] Scene name is empty.");
            return;
        }

        if (!IsLobbyReady)
        {
            pendingMenuScene = sceneName;
            Debug.Log("[Launcher] Waiting for lobby, then load: " + sceneName);
            EnsureConnected();
            EnsureInLobby();
            return;
        }

        pendingMenuScene = null;
        Debug.Log("[Launcher] LoadScene: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>Alias for MainMenu button (legacy name).</summary>
    public void OpenJoinRoomScene()
    {
        OpenRoomListScene();
    }

    bool EnsurePlayerNameSet()
    {
        PlayerNameService.LoadSavedName();

        if (PlayerNameService.HasValidName)
        {
            PlayerNameService.ApplyToPhoton();
            return true;
        }

        Debug.LogWarning("[Launcher] Cần nhập tên trước khi vào phòng.");
        return false;
    }

    public void BackToMainMenu()
    {
        Debug.Log("[Launcher] BackToMainMenu");

        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();

        SceneManager.LoadScene(mainMenuScene);
    }

    public void LeaveRoomAndReturnToMenu()
    {
        BackToMainMenu();
    }

    // ========================= EXIT =========================

    public void ExitGame()
    {
        Debug.Log("[Launcher] ExitGame");

        if (PhotonNetwork.IsConnected)
            PhotonNetwork.Disconnect();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
