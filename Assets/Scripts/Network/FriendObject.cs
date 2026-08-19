using ExplosiveFactory.Network;
using Steamworks;
using UnityEngine;

public class FriendObject : MonoBehaviour
{
    public SteamId steamId;

    public void Invite()
    {
        if (LobbyService.Instance != null && LobbyService.Instance.IsInLobby && LobbyService.Instance.CurrentLobby.HasValue)
        {
            LobbyService.Instance.CurrentLobby.Value.InviteFriend(steamId);
            Debug.Log("[FriendObject] Invited to lobby: " + steamId);
        }
        else if (SteamClient.IsValid)
        {
            new Friend(steamId).InviteToGame("ExplosiveFactory");
            Debug.Log("[FriendObject] Invited to game: " + steamId);
        }
        else
        {
            Debug.Log("[FriendObject] Steam is not initialized");
        }
    }
}
