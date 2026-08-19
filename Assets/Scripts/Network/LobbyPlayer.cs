using System;
using Mirror;
using Steamworks;
using UnityEngine;

namespace ExplosiveFactory.Network
{
    public class LobbyPlayer : NetworkBehaviour
    {
        [Header("Sync Variables")]
        [SyncVar(hook = nameof(OnPlayerNameChanged))]
        public string playerName = "Player";

        [SyncVar(hook = nameof(OnSteamIdChanged))]
        public ulong steamId;

        [SyncVar(hook = nameof(OnReadyStatusChanged))]
        public bool isReady;

        [SyncVar(hook = nameof(OnHostStatusChanged))]
        public bool isHost;

        public static LobbyPlayer LocalPlayer { get; private set; }

        public event Action<string>? OnNameUpdated;
        public event Action<ulong>? OnSteamIdUpdated;
        public event Action<bool>? OnReadyUpdated;
        public event Action<bool>? OnHostUpdated;

        public override void OnStartClient()
        {
            base.OnStartClient();
            CustomNetworkManager.singleton?.RegisterRoomPlayer(this);

            if (isLocalPlayer)
            {
                LocalPlayer = this;
                string localName = SteamManager.Instance.IsInitialized ? SteamClient.Name : $"Player_{netId}";
                ulong localSteamId = SteamManager.Instance.IsInitialized ? SteamClient.SteamId.Value : 0;
                
                CmdSetPlayerInfo(localName, localSteamId);
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            // First player on the server is the host
            isHost = (connectionToClient.connectionId == 0);
            if (isHost)
            {
                isReady = true; // Host is always ready
            }
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            CustomNetworkManager.singleton?.UnregisterRoomPlayer(this);

            if (isLocalPlayer)
            {
                LocalPlayer = null!;
            }
        }

        #region Commands

        [Command]
        private void CmdSetPlayerInfo(string newName, ulong newSteamId)
        {
            playerName = newName;
            steamId = newSteamId;
        }

        [Command]
        public void CmdToggleReady()
        {
            if (isHost) return; // Host is always ready
            isReady = !isReady;
            Debug.Log($"[LobbyPlayer] Player {playerName} ready toggled to: {isReady}");
        }

        [Command]
        public void CmdSetReady(bool readyState)
        {
            if (isHost) return;
            isReady = readyState;
        }

        [Command]
        public void CmdStartGame()
        {
            if (!isHost)
            {
                Debug.LogWarning("[LobbyPlayer] Only host can start the game.");
                return;
            }

            CustomNetworkManager.singleton?.StartGame();
        }

        #endregion

        #region Hooks

        private void OnPlayerNameChanged(string oldName, string newName)
        {
            OnNameUpdated?.Invoke(newName);
            CustomNetworkManager.singleton?.NotifyPlayerListUpdated();
        }

        private void OnSteamIdChanged(ulong oldId, ulong newId)
        {
            OnSteamIdUpdated?.Invoke(newId);
        }

        private void OnReadyStatusChanged(bool oldReady, bool newReady)
        {
            OnReadyUpdated?.Invoke(newReady);
            CustomNetworkManager.singleton?.NotifyPlayerListUpdated();
        }

        private void OnHostStatusChanged(bool oldHost, bool newHost)
        {
            OnHostUpdated?.Invoke(newHost);
            CustomNetworkManager.singleton?.NotifyPlayerListUpdated();
        }

        #endregion
    }
}
