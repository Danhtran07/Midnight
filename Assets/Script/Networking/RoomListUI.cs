using System.Collections.Generic;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Room list on RoomListScene. Subscribes to Launcher.OnRoomListUpdated.
/// </summary>
public class RoomListUI : MonoBehaviour
{
    [Header("List")]
    public Transform contentParent;
    public GameObject roomItemPrefab;

    [Header("Empty state (optional)")]
    public GameObject emptyLabel;

    readonly List<GameObject> spawnedItems = new List<GameObject>();

    void Awake()
    {
        EnsureContentParent();
        HideTemplateInScene();
    }

    void OnEnable()
    {
        if (Launcher.Instance != null)
        {
            Launcher.Instance.OnRoomListUpdated += RefreshUI;
            Launcher.Instance.OnLobbyReady += OnLobbyReady;
        }
    }

    void OnDisable()
    {
        if (Launcher.Instance != null)
        {
            Launcher.Instance.OnRoomListUpdated -= RefreshUI;
            Launcher.Instance.OnLobbyReady -= OnLobbyReady;
        }
    }

    void Start()
    {
        if (Launcher.Instance == null)
        {
            Debug.LogError("[RoomListUI] Launcher.Instance is null. Play from MainMenu.");
            return;
        }

        Launcher.Instance.EnsureInLobby();

        List<RoomInfo> cached = Launcher.Instance.GetCachedRoomList();
        if (cached != null && cached.Count > 0)
            RefreshUI(cached);
    }

    void EnsureContentParent()
    {
        if (contentParent != null && contentParent.name != "Handle")
            return;

        GameObject rawImage = GameObject.Find("RawImage");
        if (rawImage != null)
        {
            Transform content = rawImage.transform.Find("RoomListContent");
            if (content == null)
            {
                GameObject go = new GameObject("RoomListContent", typeof(RectTransform));
                go.transform.SetParent(rawImage.transform, false);
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                content = go.transform;
            }

            contentParent = content;
            Debug.Log("[RoomListUI] contentParent set to RoomListContent under RawImage.");
        }
    }

    void HideTemplateInScene()
    {
        if (roomItemPrefab != null && roomItemPrefab.scene.IsValid())
            roomItemPrefab.SetActive(false);
    }

    void OnLobbyReady()
    {
        Debug.Log("[RoomListUI] Lobby ready — waiting for room list update...");
        Launcher.Instance?.EnsureInLobby();
    }

    void RefreshUI(List<RoomInfo> roomList)
    {
        ClearList();

        if (roomList == null)
            return;

        int count = 0;

        foreach (RoomInfo room in roomList)
        {
            if (room == null || room.RemovedFromList)
                continue;

            count++;
            SpawnRoomItem(room);
        }

        if (emptyLabel != null)
            emptyLabel.SetActive(count == 0);

        Debug.Log("[RoomListUI] Displaying " + count + " room(s).");
    }

    void SpawnRoomItem(RoomInfo room)
    {
        if (roomItemPrefab == null || contentParent == null)
        {
            Debug.LogWarning("[RoomListUI] Assign contentParent and roomItemPrefab.");
            return;
        }

        GameObject item = Instantiate(roomItemPrefab, contentParent);
        item.SetActive(true);
        spawnedItems.Add(item);

        RoomListItemUI itemUI = item.GetComponent<RoomListItemUI>();
        if (itemUI != null)
        {
            itemUI.Setup(room.Name, room.PlayerCount, room.MaxPlayers);
            return;
        }

        Text label = item.GetComponentInChildren<Text>();
        if (label != null)
            label.text = room.Name + "  (" + room.PlayerCount + "/" + room.MaxPlayers + ")";

        Button btn = item.GetComponentInChildren<Button>();
        if (btn != null)
        {
            string roomName = room.Name;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => JoinRoom(roomName));
        }
    }

    void JoinRoom(string roomName)
    {
        if (Launcher.Instance == null)
        {
            Debug.LogError("[RoomListUI] Launcher.Instance is null.");
            return;
        }

        Debug.Log("[RoomListUI] JoinRoom clicked: " + roomName);
        Launcher.Instance.JoinRoom(roomName);
    }

    void ClearList()
    {
        foreach (GameObject go in spawnedItems)
        {
            if (go != null)
                Destroy(go);
        }
        spawnedItems.Clear();
    }

    public void OnBackButtonClicked()
    {
        Launcher.Instance?.BackToMainMenu();
    }

    public void OnRefreshButtonClicked()
    {
        Launcher.Instance?.EnsureInLobby();
    }
}
