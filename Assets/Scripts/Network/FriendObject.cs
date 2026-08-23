#nullable enable
using ExplosiveFactory.Network;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplosiveFactory.Network
{
    public enum FriendObjectMode
    {
        Invite,
        Join
    }

    public class FriendObject : MonoBehaviour
    {
        public SteamId steamId;
        public string friendName = "";
        public SteamId? targetLobbyId;
        public FriendObjectMode mode = FriendObjectMode.Invite;

        [SerializeField] private Button? inviteBtn;
        [SerializeField] private TextMeshProUGUI? inviteBtnText;

        private bool _isInvited;

        private void Awake()
        {
            if (inviteBtn == null) inviteBtn = GetComponentInChildren<Button>();
            if (inviteBtnText == null && inviteBtn != null) inviteBtnText = inviteBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (inviteBtn != null) inviteBtn.onClick.AddListener(OnClickAction);
        }

        public void Setup(SteamId id, string name, FriendObjectMode objectMode = FriendObjectMode.Invite, SteamId? lobbyId = null)
        {
            steamId = id;
            friendName = name;
            mode = objectMode;
            targetLobbyId = lobbyId;
            _isInvited = false;

            if (mode == FriendObjectMode.Invite)
            {
                if (inviteBtnText != null)
                {
                    inviteBtnText.text = "초대";
                }
                if (inviteBtn != null)
                {
                    inviteBtn.interactable = true;
                    var img = inviteBtn.GetComponent<Image>();
                    if (img != null) img.color = new Color(0.2f, 0.5f, 0.9f, 1f);
                }
            }
            else // Join Mode
            {
                bool canJoin = targetLobbyId.HasValue && targetLobbyId.Value.IsValid;
                if (inviteBtnText != null)
                {
                    inviteBtnText.text = canJoin ? "참가" : "대기 중";
                }
                if (inviteBtn != null)
                {
                    inviteBtn.interactable = canJoin;
                    var img = inviteBtn.GetComponent<Image>();
                    if (img != null)
                    {
                        img.color = canJoin ? new Color(0.1f, 0.7f, 0.35f, 1f) : new Color(0.35f, 0.38f, 0.45f, 0.6f);
                    }
                }
            }
        }

        private void OnClickAction()
        {
            if (mode == FriendObjectMode.Invite)
            {
                Invite();
            }
            else
            {
                JoinFriendLobby();
            }
        }

        public void JoinFriendLobby()
        {
            if (targetLobbyId.HasValue && targetLobbyId.Value.IsValid)
            {
                Debug.Log($"[FriendObject] Joining friend {friendName}'s lobby: {targetLobbyId.Value}");
                CustomNetworkManager.singleton?.JoinLobbyByIdString(targetLobbyId.Value.ToString());
            }
            else
            {
                Debug.LogWarning($"[FriendObject] Cannot join {friendName}: no valid lobby ID found.");
            }
        }

        public void Invite()
        {
            if (_isInvited) return;

            if (LobbyService.Instance != null && LobbyService.Instance.IsInLobby && LobbyService.Instance.CurrentLobby.HasValue)
            {
                LobbyService.Instance.CurrentLobby.Value.InviteFriend(steamId);
                Debug.Log($"[FriendObject] Invited {friendName} ({steamId}) to lobby.");
                SetInvitedState();
            }
            else if (SteamClient.IsValid)
            {
                new Friend(steamId).InviteToGame("ExplosiveFactory");
                Debug.Log($"[FriendObject] Invited {friendName} ({steamId}) to game.");
                SetInvitedState();
            }
            else
            {
                Debug.LogWarning("[FriendObject] Steam is not initialized");
            }
        }

        private void SetInvitedState()
        {
            _isInvited = true;
            if (inviteBtnText != null)
            {
                inviteBtnText.text = "초대 완료";
            }
            if (inviteBtn != null)
            {
                inviteBtn.interactable = false;
                var img = inviteBtn.GetComponent<Image>();
                if (img != null) img.color = new Color(0.3f, 0.7f, 0.4f, 1f);
            }
        }
    }
}
