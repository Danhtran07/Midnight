using UnityEngine;
using TMPro;

public class DoorTrigger : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;

    [Header("Text tren cua")]
    public TextMeshPro interactText;

    private bool isPlayerNear;
    private bool isOpen;

    private void Start()
    {
        if (interactText != null)
        {
            interactText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
        {
            isOpen = !isOpen;

            if (animator != null)
            {
                animator.SetBool("Open", isOpen);
            }

            Debug.Log("Trang thai cua: " + isOpen);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
            return;

        Debug.Log("Player vao trigger");

        isPlayerNear = true;

        if (interactText != null)
        {
            interactText.gameObject.SetActive(true);

            interactText.text = isOpen
                ? "Press To Close"
                : "Press To Open";
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other))
            return;

        Debug.Log("Player roi trigger");

        isPlayerNear = false;

        if (interactText != null)
        {
            interactText.gameObject.SetActive(false);
        }
    }

    static bool IsPlayer(Collider other)
    {
        if (other.CompareTag("Player"))
            return true;

        Transform root = other.transform.root;

        return root.CompareTag("Player");
    }
}