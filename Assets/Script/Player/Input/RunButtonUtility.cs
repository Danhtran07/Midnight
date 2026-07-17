using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class RunButtonUtility
{
    public static Button FindButton(string objectName)
    {
        GameObject buttonObject = GameObject.Find(objectName);
        return buttonObject != null ? buttonObject.GetComponent<Button>() : null;
    }

    public static Image Bind(Button button, UnityAction onClick)
    {
        if (button == null)
            return null;

        button.onClick.RemoveListener(onClick);
        button.onClick.AddListener(onClick);
        return button.GetComponent<Image>();
    }

    public static void ApplyColor(Image buttonImage, bool isRunning, Color runColor, Color walkColor)
    {
        if (buttonImage != null)
            buttonImage.color = isRunning ? runColor : walkColor;
    }
}
