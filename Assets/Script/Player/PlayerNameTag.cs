using Photon.Pun;
using TMPro;
using UnityEngine;

/// <summary>
/// Hiển thị nickname trên đầu player.
/// Kéo thả Name Tag Root + TextMeshPro vào Inspector (tạo sẵn trên prefab player).
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class PlayerNameTag : MonoBehaviourPun
{
    const string DefaultNick = "Player";
    const string NameTagChildName = "NameTag";
    const float BillboardMinSqrDist = 0.0001f;

    [Header("References — kéo thả từ prefab")]
    [Tooltip("Empty trên đầu model (vd. child NameTag).")]
    public Transform nameTagRoot;

    [Tooltip("TextMeshPro 3D trên NameTag (World Space).")]
    public TextMeshPro nameLabel;

    [Header("Colors")]
    public Color localColor = new Color(0.4f, 1f, 0.55f, 1f);
    public Color remoteColor = Color.white;

    [Header("Billboard")]
    [Tooltip("Chỉ xoay theo trục Y (ổn định, không lật ngược).")]
    public bool lockYawOnly = true;

    Camera targetCamera;

    void Awake()
    {
        ResolveReferences();
        RefreshName();
    }

    void Start()
    {
        RefreshName();
        targetCamera = Camera.main;
    }

    void LateUpdate()
    {
        FaceCamera();
    }

    void ResolveReferences()
    {
        if (nameTagRoot == null)
        {
            Transform found = transform.Find(NameTagChildName);
            if (found != null)
                nameTagRoot = found;
        }

        if (nameLabel == null && nameTagRoot != null)
            nameLabel = nameTagRoot.GetComponent<TextMeshPro>();

        if (nameTagRoot == null || nameLabel == null)
            Debug.LogWarning("[PlayerNameTag] Gán Name Tag Root và Name Label trên prefab player.", this);
    }

    void RefreshName()
    {
        if (nameLabel == null)
            return;

        string nick = ResolveNickname();
        nameLabel.text = nick;
        nameLabel.color = photonView.IsMine ? localColor : remoteColor;
    }

    string ResolveNickname()
    {
        string nick = photonView.Owner != null ? photonView.Owner.NickName : DefaultNick;
        return string.IsNullOrWhiteSpace(nick) ? DefaultNick : nick;
    }

    void FaceCamera()
    {
        if (nameTagRoot == null)
            return;

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        if (lockYawOnly)
            FaceCameraYawOnly();
        else
            nameTagRoot.rotation = targetCamera.transform.rotation;
    }

    void FaceCameraYawOnly()
    {
        Vector3 toCam = targetCamera.transform.position - nameTagRoot.position;
        toCam.y = 0f;

        if (toCam.sqrMagnitude < BillboardMinSqrDist)
            return;

        nameTagRoot.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
    }
}
