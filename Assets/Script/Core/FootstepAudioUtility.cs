using UnityEngine;

/// <summary>
/// Tạo / cấu hình AudioSource riêng cho footstep (tách khỏi Photon Voice).
/// </summary>
public static class FootstepAudioUtility
{
    const string FootstepChildName = "FootstepAudio";

    public static AudioSource GetOrCreate(Transform playerRoot)
    {
        Transform child = playerRoot.Find(FootstepChildName);
        if (child != null)
        {
            AudioSource existing = child.GetComponent<AudioSource>();
            if (existing != null)
                return Configure(existing);
        }

        var go = new GameObject(FootstepChildName);
        go.transform.SetParent(playerRoot, false);
        return Configure(go.AddComponent<AudioSource>());
    }

    static AudioSource Configure(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.mute = false;
        return source;
    }
}
