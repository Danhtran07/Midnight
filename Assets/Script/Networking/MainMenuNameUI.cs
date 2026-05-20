using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main Menu Name UI
/// - Nhập tên người chơi
/// - Apply vào Photon
/// - Khóa menu nếu chưa có tên hợp lệ
/// - Auto reset tên khi thoát game
/// </summary>
public class MainMenuNameUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField nameInput;

    public TextMeshProUGUI warningText;

    public Button confirmButton;

    [Header("Buttons Lock")]
    public Button[] menuButtonsToLock;

    // =========================
    // INIT
    // =========================

    void Awake()
    {
        PlayerNameService.LoadSavedName();
    }

    void Start()
    {
        if (nameInput != null)
        {
            nameInput.text = PlayerNameService.DisplayName;

            nameInput.characterLimit =
                PlayerNameService.MaxLength;

            nameInput.onValueChanged.AddListener(
                OnNameInputChanged
            );

            nameInput.onEndEdit.AddListener(
                OnNameEndEdit
            );
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(
                OnConfirmClicked
            );
        }

        // Nếu tên hợp lệ -> apply Photon
        if (PlayerNameService.HasValidName)
        {
            PlayerNameService.ApplyToPhoton();
        }

        RefreshMenuButtons();
    }

    void OnDestroy()
    {
        if (nameInput != null)
        {
            nameInput.onValueChanged.RemoveListener(
                OnNameInputChanged
            );

            nameInput.onEndEdit.RemoveListener(
                OnNameEndEdit
            );
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(
                OnConfirmClicked
            );
        }
    }

    // =========================
    // AUTO RESET NAME
    // =========================

    void OnApplicationQuit()
    {
        PlayerNameService.ClearSavedName();
    }

    // =========================
    // INPUT
    // =========================

    void OnNameInputChanged(string _)
    {
        ClearWarning();
    }

    void OnNameEndEdit(string value)
    {
        TryApplyName(value);
    }

    public void OnConfirmClicked()
    {
        string raw =
            nameInput != null
            ? nameInput.text
            : string.Empty;

        TryApplyName(raw);
    }

    // =========================
    // APPLY NAME
    // =========================

    void TryApplyName(string raw)
    {
        if (PlayerNameService.TrySetName(
            raw,
            out string error))
        {
            ClearWarning();

            if (nameInput != null)
            {
                nameInput.text =
                    PlayerNameService.DisplayName;
            }
        }
        else
        {
            if (warningText != null)
            {
                warningText.text = error;
            }
        }

        RefreshMenuButtons();
    }

    // =========================
    // WARNING
    // =========================

    void ClearWarning()
    {
        if (warningText != null)
        {
            warningText.text = string.Empty;
        }
    }

    // =========================
    // LOCK MENU BUTTONS
    // =========================

    void RefreshMenuButtons()
    {
        bool ok =
            PlayerNameService.HasValidName;

        if (menuButtonsToLock == null)
            return;

        foreach (Button btn in menuButtonsToLock)
        {
            if (btn != null)
            {
                btn.interactable = ok;
            }
        }
    }
}