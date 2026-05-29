using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Kiểm tra touch/pointer có đang trên UI hay không.
/// </summary>
public class TouchManager : MonoBehaviour
{
    public static bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

#if UNITY_EDITOR
        return EventSystem.current.IsPointerOverGameObject();
#elif UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount > 0)
            return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        return false;
#else
        return false;
#endif
    }
}
