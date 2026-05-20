using Photon.Pun;
using UnityEngine;

/// <summary>
/// Lưu và validate tên người chơi trước khi vào phòng Photon.
/// </summary>
public static class PlayerNameService
{
    public const int MinLength = 2;
    public const int MaxLength = 16;

    const string PrefsKey = "PlayerDisplayName";

    public static string DisplayName { get; private set; } = string.Empty;

    public static bool HasValidName => Validate(DisplayName, out _);

    public static void LoadSavedName()
    {
        DisplayName = PlayerPrefs.GetString(PrefsKey, string.Empty);

        DisplayName = Sanitize(DisplayName);
    }

    public static bool TrySetName(string rawName, out string error)
    {
        string cleaned = Sanitize(rawName);

        if (!Validate(cleaned, out error))
            return false;

        DisplayName = cleaned;

        PlayerPrefs.SetString(PrefsKey, DisplayName);

        PlayerPrefs.Save();

        ApplyToPhoton();

        return true;
    }

    public static bool Validate(string name, out string error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(name))
        {
            error = "Vui lòng nhập tên.";
            return false;
        }

        string trimmed = name.Trim();

        if (trimmed.Length < MinLength)
        {
            error = $"Tên tối thiểu {MinLength} ký tự.";
            return false;
        }

        if (trimmed.Length > MaxLength)
        {
            error = $"Tên tối đa {MaxLength} ký tự.";
            return false;
        }

        return true;
    }

    public static void ApplyToPhoton()
    {
        if (!HasValidName)
            return;

        if (PhotonNetwork.NickName != DisplayName)
            PhotonNetwork.NickName = DisplayName;
    }

    public static void ClearSavedName()
    {
        PlayerPrefs.DeleteKey(PrefsKey);

        PlayerPrefs.Save();

        DisplayName = string.Empty;

        PhotonNetwork.NickName = string.Empty;
    }

    static string Sanitize(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        raw = raw.Replace("\n", "")
                 .Replace("\r", "")
                 .Trim();

        if (raw.Length > MaxLength)
            raw = raw.Substring(0, MaxLength);

        return raw;
    }
}