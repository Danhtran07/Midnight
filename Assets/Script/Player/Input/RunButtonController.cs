using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// UI nút chạy — đổi màu theo trạng thái run/walk.
/// </summary>
public class RunButtonController : MonoBehaviour
{
    [FormerlySerializedAs("playerController")]
    public ThirdPersonController targetController;

    public Image buttonImage;
    public Color runColor = Color.green;
    public Color walkColor = Color.white;

    bool isRunning;

    public void OnClickToggleRun()
    {
        isRunning = !isRunning;

        if (targetController != null)
            targetController.ToggleRun();

        ApplyButtonColor();
    }

    public void ToggleRun()
    {
        isRunning = !isRunning;
        ApplyButtonColor();
    }

    void ApplyButtonColor()
    {
        if (buttonImage == null)
            return;

        buttonImage.color = isRunning ? runColor : walkColor;
    }
}
