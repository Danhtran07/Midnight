using UnityEngine;

/// <summary>
/// Mở/đóng panel voice settings. Kéo Popup Panel vào Inspector.
/// </summary>
public class VoicePopupAnimator : MonoBehaviour
{
    public GameObject popupPanel;

    void Awake()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);
    }

    public void Show()
    {
        if (popupPanel != null)
            popupPanel.SetActive(true);
    }

    public void Hide()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);
    }
}
