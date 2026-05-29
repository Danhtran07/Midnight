using System.Collections;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Spawn player prefab khi vào NightMap (mỗi client spawn player của mình).
/// </summary>
public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance { get; private set; }

    [Header("Spawn")]
    public string playerPrefabName = "player";
    public Transform[] spawnPoints;

    [Header("Retry")]
    public float spawnRetrySeconds = 8f;

    bool playerSpawned;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    void Start()
    {
        LogStartContext();
        StartCoroutine(SpawnWhenReady());
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[GameManager] OnJoinedRoom — Actor=" + PhotonNetwork.LocalPlayer.ActorNumber);
        StartCoroutine(SpawnWhenReady());
    }

    public override void OnLeftRoom()
    {
        playerSpawned = false;
        Debug.Log("[GameManager] OnLeftRoom — reset spawn flag.");
    }

    void LogStartContext()
    {
        Debug.Log("[GameManager] Start — InRoom=" + PhotonNetwork.InRoom +
                  " Scene=" + SceneManager.GetActiveScene().name);
    }

    IEnumerator SpawnWhenReady()
    {
        float elapsed = 0f;

        while (elapsed < spawnRetrySeconds)
        {
            if (TrySpawnPlayer())
                yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.LogError("[GameManager] Failed to spawn local player after " + spawnRetrySeconds + "s.");
    }

    bool TrySpawnPlayer()
    {
        if (playerSpawned)
            return true;

        if (!CanSpawnNow())
            return false;

        if (HasLocalPlayerAlready())
        {
            playerSpawned = true;
            Debug.Log("[GameManager] Local player already exists — skip spawn.");
            return true;
        }

        Transform spawn = PickRandomSpawnPoint();
        if (spawn == null)
            return false;

        return InstantiateLocalPlayer(spawn);
    }

    bool CanSpawnNow()
    {
        return PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom;
    }

    Transform PickRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[GameManager] No spawn points assigned!");
            return null;
        }

        int index = Random.Range(0, spawnPoints.Length);
        return spawnPoints[index];
    }

    bool InstantiateLocalPlayer(Transform spawn)
    {
        GameObject player = PhotonNetwork.Instantiate(
            playerPrefabName,
            spawn.position,
            spawn.rotation);

        if (player == null)
        {
            Debug.LogError("[GameManager] PhotonNetwork.Instantiate returned null.");
            return false;
        }

        playerSpawned = true;
        Debug.Log("[GameManager] Local player spawned: " + player.name +
                  " Actor=" + PhotonNetwork.LocalPlayer.ActorNumber);
        return true;
    }

    bool HasLocalPlayerAlready()
    {
        PhotonView[] views = FindObjectsByType<PhotonView>(FindObjectsSortMode.None);
        int localActor = PhotonNetwork.LocalPlayer.ActorNumber;

        foreach (PhotonView view in views)
        {
            if (view == null || !view.IsMine)
                continue;

            if (view.OwnerActorNr == localActor)
                return true;
        }

        return false;
    }
}
