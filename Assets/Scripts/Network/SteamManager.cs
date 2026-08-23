using System;
using Steamworks;
using UnityEngine;

namespace ExplosiveFactory.Network
{
    [DefaultExecutionOrder(-1000)]
    public class SteamManager : MonoBehaviour
    {
        public static SteamManager Instance { get; private set; }

        [Header("Steam Configuration")]
        [Tooltip("Your Steam App ID (480 is Spacewar for testing)")]
        [SerializeField] private uint appId = 480;

        public uint AppId => appId;
        public bool IsInitialized { get; private set; }
        public string PlayerName => IsInitialized ? SteamClient.Name : "LocalPlayer";
        public SteamId PlayerSteamId => IsInitialized ? SteamClient.SteamId : (SteamId)0;

        public static SteamId? PendingConnectLobbyId { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeSteam();
            CheckCommandLineArgs();
        }

        private void InitializeSteam()
        {
            if (IsInitialized) return;

            try
            {
                SteamClient.Init(appId, true);
                if (SteamClient.IsValid)
                {
                    IsInitialized = true;
                    Debug.Log($"[SteamManager] Steam Initialized successfully. User: {SteamClient.Name} ({SteamClient.SteamId})");
                }
                else
                {
                    Debug.LogError("[SteamManager] SteamClient.IsValid returned false after Init.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SteamManager] Failed to initialize Steam: {ex.Message}\nMake sure Steam client is running!");
                IsInitialized = false;
            }
        }

        private void CheckCommandLineArgs()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals("+connect_lobby", StringComparison.OrdinalIgnoreCase))
                {
                    if (ulong.TryParse(args[i + 1], out ulong lobbyId))
                    {
                        PendingConnectLobbyId = (SteamId)lobbyId;
                        Debug.Log($"[SteamManager] Found +connect_lobby argument: {lobbyId}");
                    }
                    break;
                }
            }
        }

        public static SteamId? ConsumePendingConnectLobbyId()
        {
            var id = PendingConnectLobbyId;
            PendingConnectLobbyId = null;
            return id;
        }

        private void Update()
        {
            if (IsInitialized)
            {
                SteamClient.RunCallbacks();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                ShutdownSteam();
                Instance = null;
            }
        }

        private void OnApplicationQuit()
        {
            StopMirrorNetwork();
        }

        private void StopMirrorNetwork()
        {
            try
            {
                if (Mirror.NetworkManager.singleton != null && Mirror.NetworkManager.singleton.isNetworkActive)
                {
                    Debug.Log("[SteamManager] Stopping Mirror NetworkManager before Steam Shutdown...");
                    if (Mirror.NetworkManager.singleton.mode == Mirror.NetworkManagerMode.Host)
                    {
                        Mirror.NetworkManager.singleton.StopHost();
                    }
                    else if (Mirror.NetworkManager.singleton.mode == Mirror.NetworkManagerMode.ClientOnly)
                    {
                        Mirror.NetworkManager.singleton.StopClient();
                    }
                    else if (Mirror.NetworkManager.singleton.mode == Mirror.NetworkManagerMode.ServerOnly)
                    {
                        Mirror.NetworkManager.singleton.StopServer();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SteamManager] Exception while stopping NetworkManager: {ex.Message}");
            }
        }

        private void ShutdownSteam()
        {
            if (IsInitialized)
            {
                StopMirrorNetwork();

                try
                {
                    SteamClient.Shutdown();
                    Debug.Log("[SteamManager] Steam shutdown complete.");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SteamManager] Exception during SteamClient.Shutdown: {ex.Message}");
                }
                finally
                {
                    IsInitialized = false;
                }
            }
        }
    }
}
