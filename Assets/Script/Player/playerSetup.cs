using System.Collections;
using UnityEngine;
using Photon.Pun;

/// <summary>
/// Khởi tạo player local: camera follow, UI, movement.
/// </summary>
public class PlayerSetup : MonoBehaviourPun
{
    const float SetupDelay = 0.1f;
    const string RunButtonObjectName = "RunButton";

    [Header("Scripts")]
    [Tooltip("Gán ThirdPersonController — tắt trên player remote.")]
    public ThirdPersonController movementScript;

    void Start()
    {
        if (!photonView.IsMine)
        {
            SetupRemotePlayer();
            return;
        }

        Debug.Log("LOCAL PLAYER SETUP");
        StartCoroutine(SetupLocalPlayer());
    }

    void SetupRemotePlayer()
    {
        Debug.Log("REMOTE PLAYER SETUP");

        if (movementScript != null)
            movementScript.enabled = false;
    }

    IEnumerator SetupLocalPlayer()
    {
        yield return new WaitForSeconds(SetupDelay);

        SetupCamera();
        EnableGameplayUI();
        EnableMovement();
    }

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("Main Camera not found!");
            return;
        }

        cam.gameObject.SetActive(true);

        CameraFollow follow = cam.GetComponent<CameraFollow>();
        if (follow == null)
        {
            Debug.LogError("CameraFollow missing!");
            return;
        }

        follow.SetTarget(transform);
        Debug.Log("Camera locked to player");
    }

    void EnableGameplayUI()
    {
        Joystick joy = FindObjectOfType<Joystick>();
        if (joy != null)
            joy.gameObject.SetActive(true);

        GameObject runBtn = GameObject.Find(RunButtonObjectName);
        if (runBtn != null)
            runBtn.SetActive(true);
    }

    void EnableMovement()
    {
        if (movementScript != null)
            movementScript.enabled = true;
    }
}
