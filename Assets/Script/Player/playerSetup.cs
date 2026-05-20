using UnityEngine;
using Photon.Pun;

public class PlayerSetup : MonoBehaviourPun
{
    [Header("Scripts")]
    [Tooltip("Gán ThirdPersonController — tắt trên player remote.")]
    public ThirdPersonController movementScript;

    void Start()
    {
        if (!photonView.IsMine)
        {
            Debug.Log("REMOTE PLAYER SETUP");

            if (movementScript != null)
                movementScript.enabled = false;

            return;
        }

        Debug.Log("LOCAL PLAYER SETUP");

        StartCoroutine(SetupLocalPlayer());
    }

    System.Collections.IEnumerator SetupLocalPlayer()
    {
        yield return new WaitForSeconds(0.1f);

        // ===== CAMERA SETUP =====
        Camera cam = Camera.main;

        if (cam != null)
        {
            cam.gameObject.SetActive(true);

            CameraFollow follow = cam.GetComponent<CameraFollow>();

            if (follow != null)
            {
                follow.SetTarget(transform);
                Debug.Log("Camera locked to player");
            }
            else
            {
                Debug.LogError("CameraFollow missing!");
            }
        }
        else
        {
            Debug.LogError("Main Camera not found!");
        }

        // ===== UI =====
        Joystick joy = FindObjectOfType<Joystick>();
        if (joy != null)
            joy.gameObject.SetActive(true);

        GameObject runBtn = GameObject.Find("RunButton");
        if (runBtn != null)
            runBtn.SetActive(true);

        // ===== MOVEMENT =====
        if (movementScript != null)
            movementScript.enabled = true;
    }
}