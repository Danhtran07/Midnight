using UnityEngine;
using UnityEngine.UI;

public class RunButtonController : MonoBehaviour
{
    public RunButtonController playerController;
    public Image buttonImage;
    public Color runColor = Color.green;
    public Color walkColor = Color.white;

    private bool isRunning = false;

    public void OnClickToggleRun()
    {
        isRunning = !isRunning;
        playerController.ToggleRun();
        buttonImage.color = isRunning ? runColor : walkColor;
    }

    public void ToggleRun()
    {
        isRunning = !isRunning;
        buttonImage.color = isRunning ? runColor : walkColor;
    }
}
