#nullable enable
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplosiveFactory.Network.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button hostButton = null!;
        [SerializeField] private Button joinButton = null!;
        [SerializeField] private Button quitButton = null!;

        [Header("Join Modal Popup (Scene UI)")]
        [SerializeField] private GameObject? joinPopupPanel;
        [SerializeField] private TMP_InputField? lobbyIdInputField;
        [SerializeField] private Button? pasteButton;
        [SerializeField] private Button? confirmJoinButton;
        [SerializeField] private Button? cancelButton;
        [SerializeField] private Button? closeJoinPopupButton;

        [Header("Texts")]
        [SerializeField] private TextMeshProUGUI versionText = null!;
        [SerializeField] private TextMeshProUGUI? statusText;

        private void Awake()
        {
            EnsureBindings();
        }

        private void Start()
        {
            EnsureBindings();

            if (versionText != null)
            {
                versionText.text = $"v{Application.version}";
            }

            UpdateStatusText();
        }

        private void EnsureBindings()
        {
            var canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
            var rootTransform = canvas != null ? canvas.transform : transform;

            // 1. Main Buttons
            if (hostButton == null) hostButton = rootTransform.Find("Buttons/HostButton")?.GetComponent<Button>() ?? rootTransform.Find("HostButton")?.GetComponent<Button>()!;
            if (joinButton == null) joinButton = rootTransform.Find("Buttons/JoinButton")?.GetComponent<Button>() ?? rootTransform.Find("JoinButton")?.GetComponent<Button>()!;
            if (quitButton == null) quitButton = rootTransform.Find("Buttons/QuitButton")?.GetComponent<Button>() ?? rootTransform.Find("QuitButton")?.GetComponent<Button>()!;

            if (hostButton != null) { hostButton.onClick.RemoveAllListeners(); hostButton.onClick.AddListener(OnClickHost); }
            if (joinButton != null) { joinButton.onClick.RemoveAllListeners(); joinButton.onClick.AddListener(OpenJoinPopup); }
            if (quitButton != null) { quitButton.onClick.RemoveAllListeners(); quitButton.onClick.AddListener(OnClickQuit); }

            // 2. Texts
            if (versionText == null) versionText = rootTransform.Find("Buttons/VersionText")?.GetComponent<TextMeshProUGUI>() ?? rootTransform.Find("VersionText")?.GetComponent<TextMeshProUGUI>()!;
            if (statusText == null) statusText = rootTransform.Find("Buttons/SteamStatusText")?.GetComponent<TextMeshProUGUI>() ?? rootTransform.Find("SteamStatusText")?.GetComponent<TextMeshProUGUI>();

            // 3. Join Modal Popup
            EnsureJoinPopup();
        }

        private void UpdateStatusText()
        {
            if (statusText != null)
            {
                if (SteamManager.Instance != null && SteamManager.Instance.IsInitialized)
                {
                    statusText.text = $"Steam: <color=#00FF88>{SteamClient.Name}</color> ({SteamClient.SteamId})";
                }
                else
                {
                    statusText.text = "<color=#FF4444>Steam 미연결 (스팀 클라이언트를 켜주세요)</color>";
                }
            }
        }

        private void OnClickHost()
        {
            if (CustomNetworkManager.singleton != null)
            {
                CustomNetworkManager.singleton.HostLobby(maxPlayers: 4, isFriendsOnly: false);
            }
            else
            {
                Debug.LogError("[MainMenuUI] CustomNetworkManager singleton is missing!");
            }
        }

        private void EnsureJoinPopup()
        {
            var canvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
            var rootTransform = canvas != null ? canvas.transform : transform;

            if (joinPopupPanel == null)
            {
                var popupTrans = rootTransform.Find("JoinPopup");
                if (popupTrans != null)
                {
                    joinPopupPanel = popupTrans.gameObject;
                }
            }

            if (joinPopupPanel != null)
            {
                var pt = joinPopupPanel.transform;
                if (lobbyIdInputField == null) lobbyIdInputField = pt.Find("WindowPanel/InputRow/LobbyIdInputField")?.GetComponent<TMP_InputField>();
                if (pasteButton == null) pasteButton = pt.Find("WindowPanel/InputRow/PasteButton")?.GetComponent<Button>();
                if (confirmJoinButton == null) confirmJoinButton = pt.Find("WindowPanel/ButtonRow/ConfirmJoinButton")?.GetComponent<Button>();
                if (cancelButton == null) cancelButton = pt.Find("WindowPanel/ButtonRow/CancelButton")?.GetComponent<Button>();
                if (closeJoinPopupButton == null) closeJoinPopupButton = pt.Find("WindowPanel/Header/CloseButton")?.GetComponent<Button>();
            }

            if (pasteButton != null) { pasteButton.onClick.RemoveAllListeners(); pasteButton.onClick.AddListener(OnClickPaste); }
            if (confirmJoinButton != null) { confirmJoinButton.onClick.RemoveAllListeners(); confirmJoinButton.onClick.AddListener(OnClickConfirmJoin); }
            if (cancelButton != null) { cancelButton.onClick.RemoveAllListeners(); cancelButton.onClick.AddListener(CloseJoinPopup); }
            if (closeJoinPopupButton != null) { closeJoinPopupButton.onClick.RemoveAllListeners(); closeJoinPopupButton.onClick.AddListener(CloseJoinPopup); }
        }

        public void OpenJoinPopup()
        {
            EnsureJoinPopup();

            if (joinPopupPanel != null)
            {
                joinPopupPanel.transform.SetAsLastSibling();
                joinPopupPanel.SetActive(true);

                // If clipboard contains a valid numeric lobby ID, auto-fill it
                string clipboard = GUIUtility.systemCopyBuffer;
                if (!string.IsNullOrWhiteSpace(clipboard))
                {
                    clipboard = clipboard.Trim();
                    if (ulong.TryParse(clipboard, out _))
                    {
                        if (lobbyIdInputField != null)
                        {
                            lobbyIdInputField.text = clipboard;
                        }
                    }
                }

                if (lobbyIdInputField != null)
                {
                    lobbyIdInputField.Select();
                    lobbyIdInputField.ActivateInputField();
                }
            }
            else
            {
                Debug.LogError("[MainMenuUI] Cannot open JoinPopup: joinPopupPanel is null!");
            }
        }

        public void CloseJoinPopup()
        {
            if (joinPopupPanel != null)
            {
                joinPopupPanel.SetActive(false);
            }
        }

        private void OnClickPaste()
        {
            string clipboard = GUIUtility.systemCopyBuffer;
            if (!string.IsNullOrWhiteSpace(clipboard))
            {
                clipboard = clipboard.Trim();
                if (lobbyIdInputField != null)
                {
                    lobbyIdInputField.text = clipboard;
                }
            }
        }

        private void OnClickConfirmJoin()
        {
            if (lobbyIdInputField != null && !string.IsNullOrWhiteSpace(lobbyIdInputField.text))
            {
                string input = lobbyIdInputField.text.Trim();
                if (CustomNetworkManager.singleton != null)
                {
                    Debug.Log($"[MainMenuUI] Joining lobby from input ID: {input}");
                    CustomNetworkManager.singleton.JoinLobbyByIdString(input);
                }
            }
            else
            {
                Debug.LogWarning("[MainMenuUI] Please enter a lobby ID!");
            }
        }

        private void OnClickPasteAndJoin()
        {
            string clipboard = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrWhiteSpace(clipboard))
            {
                Debug.LogWarning("[MainMenuUI] Clipboard is empty!");
                return;
            }

            clipboard = clipboard.Trim();
            if (ulong.TryParse(clipboard, out _))
            {
                Debug.Log($"[MainMenuUI] Joining lobby from clipboard ID: {clipboard}");
                CustomNetworkManager.singleton?.JoinLobbyByIdString(clipboard);
            }
            else
            {
                Debug.LogWarning($"[MainMenuUI] Clipboard content is not a valid Steam Lobby ID: '{clipboard}'");
            }
        }

        private void OnClickQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
