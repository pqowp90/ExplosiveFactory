#nullable enable
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Mirror;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ExplosiveFactory.Network
{
    public class CustomNetworkManager : NetworkManager
    {
        public static new CustomNetworkManager singleton => (NetworkManager.singleton as CustomNetworkManager)!;

        [Header("Scene Configuration")]
        [Scene] [SerializeField] private string mainMenuSceneName = "MainMenuScene";
        [Scene] [SerializeField] private string lobbySceneName = "LobbyScene";
        [Scene] [SerializeField] private string gameSceneName = "GameScene";

        public string MainMenuSceneName => mainMenuSceneName;
        public string LobbySceneName => lobbySceneName;
        public string GameSceneName => gameSceneName;

        [Header("Distinct Player Prefabs")]
        [SerializeField] private GameObject lobbyPlayerPrefab = null!;
        [SerializeField] private GameObject gamePlayerPrefab = null!;

        public GameObject LobbyPlayerPrefab => lobbyPlayerPrefab;
        public GameObject GamePlayerPrefab => gamePlayerPrefab;

        public readonly List<LobbyPlayer> RoomPlayers = new();
        private readonly Dictionary<int, (string Name, ulong SteamId)> _playerInfoCache = new();

        public event Action<LobbyPlayer>? OnPlayerJoined;
        public event Action<LobbyPlayer>? OnPlayerLeft;
        public event Action? OnPlayerListUpdated;

        public void NotifyPlayerListUpdated()
        {
            OnPlayerListUpdated?.Invoke();
        }

        public void CachePlayerInfo(int connectionId, string pName, ulong sId)
        {
            _playerInfoCache[connectionId] = (pName, sId);
            Debug.Log($"[CustomNetworkManager] Cached player info: connId={connectionId}, name={pName}, steamId={sId}");
        }

        public override void Awake()
        {
            base.Awake();
            autoCreatePlayer = false;
            EnsurePrefabsLoaded();
        }

        public void EnsurePrefabsLoaded()
        {
            // 1. Auto-scan all network prefabs in Resources/Network via NetworkPoolManager
            var networkPrefabs = Resources.LoadAll<GameObject>("Network");
            foreach (var p in networkPrefabs)
            {
                if (p != null && p.TryGetComponent<NetworkIdentity>(out _))
                {
                    if (!spawnPrefabs.Contains(p))
                    {
                        spawnPrefabs.Add(p);
                    }
                }
            }
            NetworkPoolManager.RegisterNetworkPrefabs();

            // 2. Resolve LobbyPlayer & GamePlayer prefabs
            if (lobbyPlayerPrefab == null)
            {
                lobbyPlayerPrefab = Resources.Load<GameObject>("Network/LobbyPlayer")
                                 ?? Resources.Load<GameObject>("Prefabs/LobbyPlayer") 
                                 ?? Resources.Load<GameObject>("LobbyPlayer");
#if UNITY_EDITOR
                if (lobbyPlayerPrefab == null)
                {
                    lobbyPlayerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Network/LobbyPlayer.prefab")
                                     ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/LobbyPlayer.prefab")
                                     ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/LobbyPlayer.prefab");
                }
#endif
            }

            if (gamePlayerPrefab == null)
            {
                gamePlayerPrefab = Resources.Load<GameObject>("Network/GamePlayer")
                                ?? Resources.Load<GameObject>("Prefabs/GamePlayer") 
                                ?? Resources.Load<GameObject>("GamePlayer");
#if UNITY_EDITOR
                if (gamePlayerPrefab == null)
                {
                    gamePlayerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Network/GamePlayer.prefab")
                                    ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Prefabs/GamePlayer.prefab")
                                    ?? UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GamePlayer.prefab");
                }
#endif
            }

            if (playerPrefab == null) playerPrefab = lobbyPlayerPrefab;

            if (gamePlayerPrefab != null && !spawnPrefabs.Contains(gamePlayerPrefab))
            {
                spawnPrefabs.Add(gamePlayerPrefab);
            }
            if (lobbyPlayerPrefab != null && !spawnPrefabs.Contains(lobbyPlayerPrefab))
            {
                spawnPrefabs.Add(lobbyPlayerPrefab);
            }
        }

        public override void Start()
        {
            base.Start();

            // Check if there is a pending lobby connection from launch arguments
            var pendingLobbyId = SteamManager.ConsumePendingConnectLobbyId();
            if (pendingLobbyId.HasValue)
            {
                JoinLobby(pendingLobbyId.Value);
            }
        }

        #region Lobby Creation & Joining

        public async void HostLobby(int maxPlayers = 4, bool isFriendsOnly = true)
        {
            if (LobbyService.Instance == null)
            {
                Debug.LogError("[CustomNetworkManager] LobbyService is missing!");
                return;
            }

            bool success = await LobbyService.Instance.CreateLobbyAsync(maxPlayers, isFriendsOnly);
            if (success)
            {
                StartHost();
                Debug.Log("[CustomNetworkManager] Host started. Changing scene to LobbyScene.");
                ServerChangeScene(lobbySceneName);
            }
        }

        public async void JoinLobby(SteamId lobbyId)
        {
            if (LobbyService.Instance == null)
            {
                Debug.LogError("[CustomNetworkManager] LobbyService is missing!");
                return;
            }

            bool success = await LobbyService.Instance.JoinLobbyAsync(lobbyId);
            if (success && LobbyService.Instance.CurrentLobby.HasValue)
            {
                var lobby = LobbyService.Instance.CurrentLobby.Value;
                string hostSteamId = lobby.GetData(LobbyService.HostSteamIdKey);

                if (string.IsNullOrEmpty(hostSteamId))
                {
                    // Fallback to lobby owner ID if metadata hasn't synced yet
                    var ownerId = lobby.Owner.Id;
                    if (ownerId.Value != 0)
                    {
                        hostSteamId = ownerId.ToString();
                        Debug.Log($"[CustomNetworkManager] HostSteamId not in metadata, using Owner.Id fallback: {hostSteamId}");
                    }
                    else
                    {
                        Debug.LogError("[CustomNetworkManager] HostSteamId not found in lobby metadata or owner.");
                        return;
                    }
                }

                if (SteamClient.IsValid && SteamClient.SteamId.ToString() == hostSteamId)
                {
                    Debug.LogWarning("[CustomNetworkManager] You are the lobby host. Cannot join self as client.");
                    return;
                }

                networkAddress = hostSteamId;
                StartClient();
                Debug.Log($"[CustomNetworkManager] Connecting to host: {hostSteamId}");
            }
        }

        public void JoinLobbyByIdString(string lobbyIdString)
        {
            if (ulong.TryParse(lobbyIdString.Trim(), out ulong id))
            {
                JoinLobby((SteamId)id);
            }
            else
            {
                Debug.LogError($"[CustomNetworkManager] Invalid lobby ID format: {lobbyIdString}");
            }
        }

        public void LeaveCurrentGame()
        {
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                StopHost();
            }
            else if (NetworkClient.isConnected)
            {
                StopClient();
            }

            LobbyService.Instance?.LeaveLobby();

            if (SceneManager.GetActiveScene().name != mainMenuSceneName)
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }

        #endregion

        #region Room Player Management

        public void RegisterRoomPlayer(LobbyPlayer player)
        {
            if (!RoomPlayers.Contains(player))
            {
                RoomPlayers.Add(player);
                Debug.Log($"[CustomNetworkManager] RoomPlayer registered: {player.playerName} (Total: {RoomPlayers.Count})");
                OnPlayerJoined?.Invoke(player);
                OnPlayerListUpdated?.Invoke();
            }
        }

        public void UnregisterRoomPlayer(LobbyPlayer player)
        {
            if (RoomPlayers.Remove(player))
            {
                Debug.Log($"[CustomNetworkManager] RoomPlayer unregistered: {player.playerName} (Remaining: {RoomPlayers.Count})");
                OnPlayerLeft?.Invoke(player);
                OnPlayerListUpdated?.Invoke();
            }
        }

        public bool AreAllPlayersReady()
        {
            if (RoomPlayers.Count == 0) return true;

            foreach (var player in RoomPlayers)
            {
                if (player == null) continue;
                // Host is always ready, other clients must be ready
                if (!player.isHost && !player.isReady)
                {
                    return false;
                }
            }
            return true;
        }

        public void StartGame()
        {
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[CustomNetworkManager] Only the server/host can start the game.");
                return;
            }

            if (!AreAllPlayersReady())
            {
                Debug.LogWarning("[CustomNetworkManager] Cannot start game: Not all players are ready!");
                return;
            }

            Debug.Log($"[CustomNetworkManager] Starting game scene: {gameSceneName}");
            ServerChangeScene(gameSceneName);
        }

        #endregion

        #region Mirror Server Callbacks & Player Spawning

        public override void OnServerReady(NetworkConnectionToClient conn)
        {
            base.OnServerReady(conn);
            EnsurePrefabsLoaded();

            string currentScene = SceneManager.GetActiveScene().name;

            if (currentScene == lobbySceneName || currentScene == mainMenuSceneName)
            {
                SpawnLobbyPlayer(conn);
            }
            else if (currentScene == gameSceneName)
            {
                SpawnGamePlayer(conn);
            }
        }

        public override void OnServerSceneChanged(string sceneName)
        {
            base.OnServerSceneChanged(sceneName);
            EnsurePrefabsLoaded();

            if (sceneName == gameSceneName)
            {
                SpawnSceneVendingMachine();
            }
        }

        private void SpawnSceneVendingMachine()
        {
            if (!NetworkServer.active) return;

            var existing = FindFirstObjectByType<ItemVendingMachine>();
            if (existing != null) return;

            var vmPrefab = Resources.Load<GameObject>("Network/ItemVendingMachine");
            if (vmPrefab != null)
            {
                var vmObj = NetworkPoolManager.Get(vmPrefab, new Vector3(0, 0, 3.5f), Quaternion.Euler(0, 180, 0));
                Debug.Log("[CustomNetworkManager] Spawned network ItemVendingMachine via NetworkPoolManager in scene.");
            }
        }

        private void SpawnLobbyPlayer(NetworkConnectionToClient conn)
        {
            EnsurePrefabsLoaded();

            if (conn.identity != null && conn.identity.GetComponent<LobbyPlayer>() != null)
            {
                return;
            }

            if (lobbyPlayerPrefab == null)
            {
                Debug.LogError("[CustomNetworkManager] lobbyPlayerPrefab is null! Cannot spawn LobbyPlayer.");
                return;
            }

            GameObject lobbyObj = Instantiate(lobbyPlayerPrefab);
            lobbyObj.name = $"LobbyPlayer [connId={conn.connectionId}]";

            if (conn.identity == null)
            {
                NetworkServer.AddPlayerForConnection(conn, lobbyObj);
            }
            else
            {
                NetworkServer.ReplacePlayerForConnection(conn, lobbyObj, true);
            }
        }

        private void SpawnGamePlayer(NetworkConnectionToClient conn)
        {
            EnsurePrefabsLoaded();

            if (conn.identity != null && conn.identity.GetComponent<GamePlayer>() != null)
            {
                Transform startPos = GetStartPosition();
                if (startPos != null)
                {
                    conn.identity.transform.position = startPos.position;
                    conn.identity.transform.rotation = startPos.rotation;
                }
                return;
            }

            if (gamePlayerPrefab == null)
            {
                Debug.LogError("[CustomNetworkManager] gamePlayerPrefab is null! Cannot spawn GamePlayer.");
                return;
            }

            string pName = "Player";
            ulong sId = 0;

            if (_playerInfoCache.TryGetValue(conn.connectionId, out var cachedInfo))
            {
                pName = cachedInfo.Name;
                sId = cachedInfo.SteamId;
            }
            else if (conn.identity != null && conn.identity.TryGetComponent<LobbyPlayer>(out var lobbyPlayer))
            {
                pName = lobbyPlayer.playerName;
                sId = lobbyPlayer.steamId;
            }

            Transform spawnPos = GetStartPosition();
            GameObject gamePlayer = spawnPos != null
                ? Instantiate(gamePlayerPrefab, spawnPos.position, spawnPos.rotation)
                : Instantiate(gamePlayerPrefab);

            gamePlayer.name = $"GamePlayer_{pName} [connId={conn.connectionId}]";

            if (gamePlayer.TryGetComponent<GamePlayer>(out var gp))
            {
                gp.playerName = pName;
                gp.steamId = sId;
            }

            if (conn.identity == null)
            {
                NetworkServer.AddPlayerForConnection(conn, gamePlayer);
            }
            else
            {
                NetworkServer.ReplacePlayerForConnection(conn, gamePlayer, true);
            }
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            _playerInfoCache.Remove(conn.connectionId);
            base.OnServerDisconnect(conn);
            OnPlayerListUpdated?.Invoke();
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            RoomPlayers.Clear();
            _playerInfoCache.Clear();
            OnPlayerListUpdated?.Invoke();

            if (SceneManager.GetActiveScene().name != mainMenuSceneName)
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }

        public override void OnStopHost()
        {
            base.OnStopHost();
            RoomPlayers.Clear();
            _playerInfoCache.Clear();

            if (SceneManager.GetActiveScene().name != mainMenuSceneName)
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }

        #endregion
    }
}
