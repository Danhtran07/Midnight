using UnityEngine;

/// <summary>
/// Nhận diện collider thuộc player (tag trên object hoặc root).
/// </summary>
public static class PlayerIdentityUtility
{
    public static bool IsPlayer(Collider other)
    {
        if (other == null)
            return false;

        if (other.CompareTag(GameTags.Player))
            return true;

        return other.transform.root.CompareTag(GameTags.Player);
    }
}
