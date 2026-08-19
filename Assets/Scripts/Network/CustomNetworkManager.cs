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
        public static new CustomNetworkManager singleton => NetworkManager.singleton as CustomNetworkManager;

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

        public event Action<LobbyPlayer>? OnPlayerJoined;
        public event Action<LobbyPlayer>? OnPlayerLeft;
        public event Action? OnPlayerListUpdated;

        public void NotifyPlayerListUpdated()
        {
            OnPlayerListUpdated?.Invoke();
        }

        public override void Awake()
        {
            base.Awake();
            autoCreatePlayer = false;
            EnsurePrefabsLoaded();
        }

        public void EnsurePrefabsLoaded()
        {
            // 1. Auto-scan all network prefabs in Resources/Network
            var networkPrefabs = Resources.LoadAll<GameObject>("Network");
            foreach (var p in networkPrefabs)
            {
                if (p != null && p.TryGetComponent<NetworkIdentity>(out _))
                {
                    if (!spawnPrefabs.Contains(p))
                    {
                        spawnPrefabs.Add(p);
                    }
                    if (NetworkClient.active && !NetworkClient.prefabs.ContainsKey(p.GetComponent<NetworkIdentity>().assetId))
                    {
                        NetworkClient.RegisterPrefab(p);
                    }
                }
            }

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
                string hostSteamId = LobbyService.Instance.CurrentLobby.Value.GetData(LobbyService.HostSteamIdKey);
                if (string.IsNullOrEmpty(hostSteamId))
                {
                    Debug.LogError("[CustomNetworkManager] HostSteamId not found in lobby metadata.");
                    return;
                }

                networkAddress = hostSteamId;
                StartClient();
                Debug.Log($"[CustomNetworkManager] Connecting to host: {hostSteamId}");
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

            if (sceneName == lobbySceneName)
            {
                foreach (var conn in NetworkServer.connections.Values)
                {
                    if (conn != null && conn.isReady)
                    {
                        SpawnLobbyPlayer(conn);
                    }
                }
            }
            else if (sceneName == gameSceneName)
            {
                foreach (var conn in NetworkServer.connections.Values)
                {
                    if (conn != null && conn.isReady)
                    {
                        SpawnGamePlayer(conn);
                    }
                }
            }
        }

        private void SpawnLobbyPlayer(NetworkConnectionToClient conn)
        {
            EnsurePrefabsLoaded();

            if (conn.identity != null && conn.identity.GetComponent<LobbyPlayer>() != null)
            {
                return;
            }

            GameObject lobbyObj;
            if (lobbyPlayerPrefab != null)
            {
                lobbyObj = Instantiate(lobbyPlayerPrefab);
            }
            else
            {
                lobbyObj = new GameObject("LobbyPlayer");
                lobbyObj.AddComponent<NetworkIdentity>();
                lobbyObj.AddComponent<LobbyPlayer>();
            }

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

            string pName = "Player";
            ulong sId = 0;
            if (conn.identity != null && conn.identity.TryGetComponent<LobbyPlayer>(out var lobbyPlayer))
            {
                pName = lobbyPlayer.playerName;
                sId = lobbyPlayer.steamId;
            }

            Transform spawnPos = GetStartPosition();
            GameObject gamePlayer;

            if (gamePlayerPrefab != null)
            {
                gamePlayer = spawnPos != null
                    ? Instantiate(gamePlayerPrefab, spawnPos.position, spawnPos.rotation)
                    : Instantiate(gamePlayerPrefab);
            }
            else
            {
                // Fallback Dynamic Runtime GamePlayer
                gamePlayer = new GameObject($"GamePlayer_{pName}");
                if (spawnPos != null) gamePlayer.transform.position = spawnPos.position;

                var rb = gamePlayer.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;

                var col = gamePlayer.AddComponent<CircleCollider2D>();
                col.radius = 0.5f;

                gamePlayer.AddComponent<NetworkIdentity>();
                gamePlayer.AddComponent<NetworkTransformReliable>();
                gamePlayer.AddComponent<GamePlayer>();
            }

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
            base.OnServerDisconnect(conn);
            OnPlayerListUpdated?.Invoke();
        }

        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            RoomPlayers.Clear();
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

            if (SceneManager.GetActiveScene().name != mainMenuSceneName)
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }

        #endregion
    }
}
