using System.Collections;
using System.Collections.Generic;
using ExplosiveFactory.Network;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;

public class InviteUI : MonoBehaviour
{
    public RawImage pp;
    public Text playerName;
    public Transform friendsContent;

    public void InitFriendUI()
    {
        if (!SteamClient.IsValid) return;
        LobbyService.Instance?.OpenInviteOverlay();
    }

    private void Start()
    {
    }
}
