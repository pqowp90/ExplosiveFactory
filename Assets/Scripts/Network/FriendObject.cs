#nullable enable
using ExplosiveFactory.Network;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplosiveFactory.Network
{
    public class FriendObject : MonoBehaviour
    {
        public SteamId steamId;
        public string friendName = "";

        [SerializeField] private Button? inviteBtn;
        [SerializeField] private TextMeshProUGUI? inviteBtnText;

        private bool _isInvited;

        private void Awake()
        {
            if (inviteBtn == null) inviteBtn = GetComponentInChildren<Button>();
            if (inviteBtnText == null && inviteBtn != null) inviteBtnText = inviteBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (inviteBtn != null) inviteBtn.onClick.AddListener(Invite);
        }

        public void Setup(SteamId id, string name)
        {
            steamId = id;
            friendName = name;
            _isInvited = false;

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
