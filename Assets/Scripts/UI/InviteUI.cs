using System.Collections;
using System.Collections.Generic;
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
        foreach (Transform child in friendsContent)
        {
            PoolManager.Release(child.gameObject);
        }
        SteamFriendsManager.Instance.InitFriendsAsync(friendsContent, playerName, pp);
    }

    private void Start()
    {
    }
}
