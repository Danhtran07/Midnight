using UnityEngine;
using TMPro;

/// <summary>
/// Cửa tương tác: hiện text khi player vào trigger, mở/đóng bằng phím E.
/// </summary>
public class DoorTrigger : MonoBehaviour
{
    const string OpenText = "Press To Open";
    const string CloseText = "Press To Close";
    const KeyCode InteractKey = KeyCode.E;
    const string OpenAnimatorBool = "Open";

    [Header("Animator")]
    public Animator animator;

    [Header("Text tren cua")]
    public TextMeshPro interactText;

    bool isPlayerNear;
    bool isOpen;

    void Start()
    {
        HideInteractText();
    }

    void Update()
    {
        if (!isPlayerNear || !Input.GetKeyDown(InteractKey))
            return;

        ToggleDoor();
    }

    void ToggleDoor()
    {
        isOpen = !isOpen;

        if (animator != null)
            animator.SetBool(OpenAnimatorBool, isOpen);

        Debug.Log("Trang thai cua: " + isOpen);
        RefreshInteractText();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!PlayerIdentityUtility.IsPlayer(other))
            return;

        Debug.Log("Player vao trigger");
        isPlayerNear = true;
        ShowInteractText();
    }

    void OnTriggerExit(Collider other)
    {
        if (!PlayerIdentityUtility.IsPlayer(other))
            return;

        Debug.Log("Player roi trigger");
        isPlayerNear = false;
        HideInteractText();
    }

    void ShowInteractText()
    {
        if (interactText == null)
            return;

        interactText.gameObject.SetActive(true);
        RefreshInteractText();
    }

    void HideInteractText()
    {
        if (interactText == null)
            return;

        interactText.gameObject.SetActive(false);
    }

    void RefreshInteractText()
    {
        if (interactText == null)
            return;

        interactText.text = isOpen ? CloseText : OpenText;
    }
}
