#nullable enable
using System;
using Cysharp.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

namespace ExplosiveFactory.Network
{
    public class LobbyService : MonoBehaviour
    {
        public static LobbyService Instance { get; private set; } = null!;

        public const string HostSteamIdKey = "HostSteamId";
        public const string GameVersionKey = "GameVersion";

        public Lobby? CurrentLobby { get; private set; }
        public bool IsInLobby => CurrentLobby.HasValue;
        public bool IsLobbyOwner => CurrentLobby.HasValue && CurrentLobby.Value.Owner.Id == SteamClient.SteamId;

        public event Action<Lobby>? OnLobbyCreatedEvent;
        public event Action<Lobby>? OnLobbyEnteredEvent;
        public event Action<Lobby, Friend>? OnLobbyMemberJoinedEvent;
        public event Action<Lobby, Friend>? OnLobbyMemberLeaveEvent;
        public event Action<Lobby>? OnLobbyDataChangedEvent;
        public event Action? OnLobbyLeftEvent;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            RegisterSteamEvents();
        }

        private void RegisterSteamEvents()
        {
            SteamMatchmaking.OnLobbyCreated += OnSteamLobbyCreated;
            SteamMatchmaking.OnLobbyEntered += OnSteamLobbyEntered;
            SteamMatchmaking.OnLobbyMemberJoined += OnSteamLobbyMemberJoined;
            SteamMatchmaking.OnLobbyMemberLeave += OnSteamLobbyMemberLeave;
            SteamMatchmaking.OnLobbyMemberDisconnected += OnSteamLobbyMemberDisconnected;
            SteamMatchmaking.OnLobbyDataChanged += OnSteamLobbyDataChanged;
            SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
        }

        private void UnregisterSteamEvents()
        {
            SteamMatchmaking.OnLobbyCreated -= OnSteamLobbyCreated;
            SteamMatchmaking.OnLobbyEntered -= OnSteamLobbyEntered;
            SteamMatchmaking.OnLobbyMemberJoined -= OnSteamLobbyMemberJoined;
            SteamMatchmaking.OnLobbyMemberLeave -= OnSteamLobbyMemberLeave;
            SteamMatchmaking.OnLobbyMemberDisconnected -= OnSteamLobbyMemberDisconnected;
            SteamMatchmaking.OnLobbyDataChanged -= OnSteamLobbyDataChanged;
            SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
        }

        private void OnDestroy()
        {
            UnregisterSteamEvents();
            LeaveLobby();
        }

        public async UniTask<bool> CreateLobbyAsync(int maxMembers = 4, bool isFriendsOnly = true)
        {
            if (!SteamManager.Instance.IsInitialized)
            {
                Debug.LogError("[LobbyService] Steam is not initialized.");
                return false;
            }

            LeaveLobby();

            try
            {
                var lobbyResult = await SteamMatchmaking.CreateLobbyAsync(maxMembers);
                if (!lobbyResult.HasValue)
                {
                    Debug.LogError("[LobbyService] Failed to create lobby.");
                    return false;
                }

                Lobby lobby = lobbyResult.Value;
                if (isFriendsOnly)
                {
                    lobby.SetFriendsOnly();
                }
                else
                {
                    lobby.SetPublic();
                }

                lobby.SetJoinable(true);
                lobby.SetData(HostSteamIdKey, SteamClient.SteamId.ToString());
                lobby.SetData(GameVersionKey, Application.version);

                CurrentLobby = lobby;
                Debug.Log($"[LobbyService] Lobby created successfully. ID: {lobby.Id}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LobbyService] Exception while creating lobby: {ex.Message}");
                return false;
            }
        }

        public async UniTask<bool> JoinLobbyAsync(SteamId lobbyId)
        {
            if (!SteamManager.Instance.IsInitialized)
            {
                Debug.LogError("[LobbyService] Steam is not initialized.");
                return false;
            }

            LeaveLobby();

            try
            {
                var room = await SteamMatchmaking.JoinLobbyAsync(lobbyId);
                if (!room.HasValue)
                {
                    Debug.LogError($"[LobbyService] Failed to join lobby: {lobbyId}");
                    return false;
                }

                CurrentLobby = room.Value;
                Debug.Log($"[LobbyService] Joined lobby: {lobbyId}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LobbyService] Exception while joining lobby: {ex.Message}");
                return false;
            }
        }

        public void LeaveLobby()
        {
            if (CurrentLobby.HasValue)
            {
                try
                {
                    if (SteamClient.IsValid)
                    {
                        Debug.Log($"[LobbyService] Leaving lobby: {CurrentLobby.Value.Id}");
                        CurrentLobby.Value.Leave();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[LobbyService] LeaveLobby exception ignored: {ex.Message}");
                }
                finally
                {
                    CurrentLobby = null;
                    OnLobbyLeftEvent?.Invoke();
                }
            }
        }

        public event Action<string>? OnLobbyIdCopiedEvent;

        public void OpenInviteOverlay()
        {
            if (!CurrentLobby.HasValue)
            {
                Debug.LogWarning("[LobbyService] Cannot open invite overlay: not in a lobby.");
                return;
            }

            string lobbyIdStr = CurrentLobby.Value.Id.ToString();
            GUIUtility.systemCopyBuffer = lobbyIdStr;
            Debug.Log($"[LobbyService] Lobby ID copied to clipboard: {lobbyIdStr}");
            OnLobbyIdCopiedEvent?.Invoke(lobbyIdStr);

            try
            {
                if (SteamClient.IsValid)
                {
                    SteamFriends.OpenGameInviteOverlay(CurrentLobby.Value.Id);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LobbyService] Failed to open Steam invite overlay: {ex.Message}");
            }
        }

        #region Steam Matchmaking Callbacks

        private void OnSteamLobbyCreated(Result result, Lobby lobby)
        {
            if (result == Result.OK)
            {
                CurrentLobby = lobby;
                OnLobbyCreatedEvent?.Invoke(lobby);
            }
            else
            {
                Debug.LogError($"[LobbyService] OnSteamLobbyCreated failed with result: {result}");
            }
        }

        private void OnSteamLobbyEntered(Lobby lobby)
        {
            CurrentLobby = lobby;
            Debug.Log($"[LobbyService] Entered lobby: {lobby.Id}, Member count: {lobby.MemberCount}");
            OnLobbyEnteredEvent?.Invoke(lobby);
        }

        private void OnSteamLobbyMemberJoined(Lobby lobby, Friend friend)
        {
            Debug.Log($"[LobbyService] Member joined: {friend.Name} ({friend.Id})");
            OnLobbyMemberJoinedEvent?.Invoke(lobby, friend);
        }

        private void OnSteamLobbyMemberLeave(Lobby lobby, Friend friend)
        {
            Debug.Log($"[LobbyService] Member left: {friend.Name} ({friend.Id})");
            OnLobbyMemberLeaveEvent?.Invoke(lobby, friend);
        }

        private void OnSteamLobbyMemberDisconnected(Lobby lobby, Friend friend)
        {
            Debug.Log($"[LobbyService] Member disconnected: {friend.Name} ({friend.Id})");
            OnLobbyMemberLeaveEvent?.Invoke(lobby, friend);
        }

        private void OnSteamLobbyDataChanged(Lobby lobby)
        {
            OnLobbyDataChangedEvent?.Invoke(lobby);
        }

        private void OnGameLobbyJoinRequested(Lobby lobby, SteamId friendId)
        {
            Debug.Log($"[LobbyService] Join requested from friend ({friendId}) for lobby: {lobby.Id}");
            if (CustomNetworkManager.singleton != null)
            {
                CustomNetworkManager.singleton.JoinLobby(lobby.Id);
            }
            else
            {
                JoinLobbyAsync(lobby.Id).Forget();
            }
        }

        #endregion
    }
}
