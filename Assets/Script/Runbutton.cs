using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI nút chạy — đổi màu theo trạng thái run/walk.
/// </summary>
public class RunButtonController : MonoBehaviour
{
    public RunButtonController playerController;
    public Image buttonImage;
    public Color runColor = Color.green;
    public Color walkColor = Color.white;

    bool isRunning;

    public void OnClickToggleRun()
    {
        isRunning = !isRunning;
        playerController.ToggleRun();
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
