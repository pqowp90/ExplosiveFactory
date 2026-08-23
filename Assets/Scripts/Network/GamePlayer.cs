using Mirror;
using TMPro;
using UnityEngine;

namespace ExplosiveFactory.Network
{
    public class GamePlayer : NetworkBehaviour
    {
        [Header("Sync Variables")]
        [SyncVar(hook = nameof(OnNameChanged))]
        public string playerName = "Player";

        [SyncVar]
        public ulong steamId;

        [Header("References")]
        [SerializeField] private TextMeshPro nameText;
        [SerializeField] private Camera playerCamera;

        public static GamePlayer LocalPlayer { get; private set; }

        private PlayerMove _playerMove;

        private void Awake()
        {
            _playerMove = GetComponent<PlayerMove>();
            if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>(true);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (isLocalPlayer || isOwned)
            {
                LocalPlayer = this;
                if (nameText != null) nameText.gameObject.SetActive(false);
            }
            else
            {
                if (nameText != null) nameText.gameObject.SetActive(true);
            }

            UpdateNameVisual(playerName);
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            LocalPlayer = this;
            if (nameText != null) nameText.gameObject.SetActive(false);

            string myName = Steamworks.SteamClient.IsValid ? Steamworks.SteamClient.Name : "Player";
            ulong myId = Steamworks.SteamClient.IsValid ? Steamworks.SteamClient.SteamId.Value : 0;
            CmdSetPlayerInfo(myName, myId);
        }

        [Command]
        private void CmdSetPlayerInfo(string pName, ulong sId)
        {
            playerName = pName;
            steamId = sId;
            gameObject.name = $"GamePlayer_{pName} [connId={connectionToClient.connectionId}]";
            RpcUpdatePlayerInfo(pName, sId);
        }

        [ClientRpc]
        private void RpcUpdatePlayerInfo(string pName, ulong sId)
        {
            playerName = pName;
            steamId = sId;
            UpdateNameVisual(pName);
        }

        public override void OnStopClient()
        {
            base.OnStopClient();
            if (isLocalPlayer || isOwned)
            {
                LocalPlayer = null!;
            }
        }

        private void OnNameChanged(string oldName, string newName)
        {
            UpdateNameVisual(newName);
        }

        private void UpdateNameVisual(string nameStr)
        {
            if (nameText != null)
            {
                nameText.text = nameStr;
            }
        }

        private void Update()
        {
            if (!isLocalPlayer && !isOwned && nameText != null && Camera.main != null)
            {
                nameText.transform.rotation = Camera.main.transform.rotation;
            }
        }
    }
}
