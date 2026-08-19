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
        [SerializeField] private TextMeshPro? nameText;
        [SerializeField] private Camera? playerCamera;

        public static GamePlayer LocalPlayer { get; private set; }

        private PlayerMove? _playerMove;

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
                if (_playerMove != null) _playerMove.enabled = true;
                if (playerCamera != null)
                {
                    playerCamera.gameObject.SetActive(true);
                    playerCamera.tag = "MainCamera";
                }
                if (nameText != null) nameText.gameObject.SetActive(false);

                // 씬 내 중복 기본 카메라 끄기
                var sceneCams = FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var c in sceneCams)
                {
                    if (c != playerCamera && c.gameObject.name == "Main Camera")
                    {
                        c.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                // 원격 플레이어: 로컬 조작 및 카메라 비활성화
                if (_playerMove != null) _playerMove.enabled = false;
                if (playerCamera != null) playerCamera.gameObject.SetActive(false);
                if (nameText != null) nameText.gameObject.SetActive(true);
            }

            UpdateNameVisual(playerName);
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            LocalPlayer = this;
            if (_playerMove != null) _playerMove.enabled = true;
            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(true);
                playerCamera.tag = "MainCamera";
            }
            if (nameText != null) nameText.gameObject.SetActive(false);
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
