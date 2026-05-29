using UnityEngine;

/// <summary>
/// Tìm transform theo tên trong hierarchy.
/// </summary>
public static class TransformHierarchyUtility
{
    public static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrEmpty(childName))
            return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
                return children[i];
        }

        return null;
    }
}
