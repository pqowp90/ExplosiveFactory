#nullable enable
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplosiveFactory.Network.UI
{
    public class LobbyUI : MonoBehaviour
    {
        [Header("Controls")]
        [SerializeField] private Button readyButton = null!;
        [SerializeField] private TextMeshProUGUI readyButtonText = null!;
        [SerializeField] private Button startButton = null!;
        [SerializeField] private Button leaveButton = null!;
        [SerializeField] private Button inviteButton = null!;
        [SerializeField] private TextMeshProUGUI lobbyTitleText = null!;

        [Header("Player List")]
        [SerializeField] private Transform playerListContainer = null!;
        [SerializeField] private GameObject playerEntryPrefab = null!;

        [Header("Friends Popup (Scene UI)")]
        [SerializeField] private GameObject? friendsPopupPanel;
        [SerializeField] private Transform? friendsContent;
        [SerializeField] private GameObject? friendItemPrefab;
        [SerializeField] private Button? closeFriendsPopupButton;
        [SerializeField] private Button? copyLobbyIdButton;
        [SerializeField] private Button? refreshFriendsButton;
        [Header("Toast / Feedback")]
        [SerializeField] private TextMeshProUGUI? inviteToastText;

        private readonly List<GameObject> _spawnedEntries = new();
        private int _lastPlayerCount = -1;
        private bool _lastIsHost;
        private bool _lastIsReady;
        private Coroutine? _toastCoroutine;

        private void Start()
        {
            if (inviteButton != null) inviteButton.onClick.AddListener(OnClickInvite);
            if (readyButton != null) readyButton.onClick.AddListener(OnClickReady);
            if (startButton != null) startButton.onClick.AddListener(OnClickStartGame);
            if (leaveButton != null) leaveButton.onClick.AddListener(OnClickLeave);

            if (closeFriendsPopupButton != null) closeFriendsPopupButton.onClick.AddListener(CloseFriendsPopup);
            if (copyLobbyIdButton != null) copyLobbyIdButton.onClick.AddListener(() => LobbyService.Instance?.OpenInviteOverlay());
            if (refreshFriendsButton != null) refreshFriendsButton.onClick.AddListener(RefreshFriendsList);

            if (friendsPopupPanel != null)
            {
                friendsPopupPanel.SetActive(false);
            }

            if (CustomNetworkManager.singleton != null)
            {
                CustomNetworkManager.singleton.OnPlayerListUpdated += RefreshLobbyUI;
            }

            if (LobbyService.Instance != null)
            {
                LobbyService.Instance.OnLobbyIdCopiedEvent += HandleLobbyIdCopied;
            }

            if (inviteToastText != null)
            {
                inviteToastText.gameObject.SetActive(false);
            }

            RefreshLobbyUI();
        }

        private void Update()
        {
            // Detect dynamic state changes and refresh
            if (CustomNetworkManager.singleton == null) return;

            var players = CustomNetworkManager.singleton.RoomPlayers;
            var localPlayer = LobbyPlayer.LocalPlayer;

            bool isHost = localPlayer != null && localPlayer.isHost;
            bool isReady = localPlayer != null && localPlayer.isReady;

            if (_lastPlayerCount != players.Count || _lastIsHost != isHost || _lastIsReady != isReady)
            {
                _lastPlayerCount = players.Count;
                _lastIsHost = isHost;
                _lastIsReady = isReady;
                RefreshLobbyUI();
            }
        }

        private void OnDestroy()
        {
            if (CustomNetworkManager.singleton != null)
            {
                CustomNetworkManager.singleton.OnPlayerListUpdated -= RefreshLobbyUI;
            }

            if (LobbyService.Instance != null)
            {
                LobbyService.Instance.OnLobbyIdCopiedEvent -= HandleLobbyIdCopied;
            }
        }

        private void OnClickInvite()
        {
            if (LobbyService.Instance != null)
            {
                LobbyService.Instance.OpenInviteOverlay();
            }

            if (friendsPopupPanel != null)
            {
                friendsPopupPanel.SetActive(true);
                RefreshFriendsList();
            }
        }

        public void RefreshFriendsList()
        {
            if (friendsContent != null && friendItemPrefab != null && SteamFriendsManager.Instance != null)
            {
                SteamFriendsManager.Instance.PopulateFriends(friendsContent, friendItemPrefab);
            }
        }

        public void CloseFriendsPopup()
        {
            if (friendsPopupPanel != null)
            {
                friendsPopupPanel.SetActive(false);
            }
        }

        private void HandleLobbyIdCopied(string lobbyId)
        {
            if (inviteToastText != null)
            {
                ShowToast($"로비 ID가 복사되었습니다!\n<color=#FFFF00>[{lobbyId}]</color> 친구에게 붙여넣기하세요!");
            }
        }

        private void ShowToast(string message)
        {
            if (inviteToastText == null) return;

            if (_toastCoroutine != null)
            {
                StopCoroutine(_toastCoroutine);
            }
            _toastCoroutine = StartCoroutine(ToastRoutine(message));
        }

        private System.Collections.IEnumerator ToastRoutine(string message)
        {
            if (inviteToastText != null)
            {
                inviteToastText.text = message;
                inviteToastText.gameObject.SetActive(true);
                yield return new WaitForSeconds(3.5f);
                inviteToastText.gameObject.SetActive(false);
            }
        }

        private void OnClickReady()
        {
            if (LobbyPlayer.LocalPlayer != null)
            {
                LobbyPlayer.LocalPlayer.CmdToggleReady();
            }
        }

        private void OnClickStartGame()
        {
            if (LobbyPlayer.LocalPlayer != null && LobbyPlayer.LocalPlayer.isHost)
            {
                LobbyPlayer.LocalPlayer.CmdStartGame();
            }
        }

        private void OnClickLeave()
        {
            CustomNetworkManager.singleton?.LeaveCurrentGame();
        }

        public void RefreshLobbyUI()
        {
            if (CustomNetworkManager.singleton == null) return;

            var players = CustomNetworkManager.singleton.RoomPlayers;
            var localPlayer = LobbyPlayer.LocalPlayer;

            // Update title
            if (lobbyTitleText != null)
            {
                lobbyTitleText.text = $"로비 대기실 ({players.Count}/4)";
            }

            // Update ready button & start button visibility
            if (localPlayer != null)
            {
                if (localPlayer.isHost)
                {
                    if (readyButton != null) readyButton.gameObject.SetActive(false);
                    if (startButton != null)
                    {
                        startButton.gameObject.SetActive(true);
                        startButton.interactable = CustomNetworkManager.singleton.AreAllPlayersReady();
                    }
                }
                else
                {
                    if (readyButton != null)
                    {
                        readyButton.gameObject.SetActive(true);
                        if (readyButtonText != null)
                        {
                            readyButtonText.text = localPlayer.isReady ? "준비 취소" : "준비 완료";
                        }
                    }
                    if (startButton != null) startButton.gameObject.SetActive(false);
                }
            }

            // Clear old entries
            foreach (var entry in _spawnedEntries)
            {
                Destroy(entry);
            }
            _spawnedEntries.Clear();

            // Populate player list
            if (playerEntryPrefab != null && playerListContainer != null)
            {
                foreach (var player in players)
                {
                    if (player == null) continue;

                    var entry = Instantiate(playerEntryPrefab, playerListContainer);
                    var text = entry.GetComponentInChildren<TextMeshProUGUI>();
                    if (text != null)
                    {
                        string status = player.isHost 
                            ? "<color=#FFD700>[방장]</color>" 
                            : (player.isReady ? "<color=#00FF00>[준비 완료]</color>" : "<color=#FF6666>[대기 중]</color>");
                        text.text = $"{player.playerName} {status}";
                    }
                    _spawnedEntries.Add(entry);
                }
            }
        }
    }
}
