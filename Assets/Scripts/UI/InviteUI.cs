#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using ExplosiveFactory.Network;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InviteUI : MonoBehaviour
{
    [Header("Self Profile")]
    public RawImage? pp;
    public Text? playerName;
    [SerializeField] private TextMeshProUGUI? playerNameTmp;

    [Header("Friends List")]
    public Transform? friendsContent;
    [SerializeField] private GameObject? friendItemPrefab;

    [Header("Controls")]
    [SerializeField] private Button? refreshButton;
    [SerializeField] private Button? overlayInviteButton;
    [SerializeField] private Button? copyLobbyIdButton;
    [SerializeField] private TextMeshProUGUI? toastText;

    private Coroutine? _toastCoroutine;

    private void Awake()
    {
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(RefreshUI);
        }
        if (overlayInviteButton != null)
        {
            overlayInviteButton.onClick.AddListener(OpenInviteOverlay);
        }
        if (copyLobbyIdButton != null)
        {
            copyLobbyIdButton.onClick.AddListener(CopyLobbyId);
        }
    }

    private void OnEnable()
    {
        RefreshUI();
    }

    public void InitFriendUI()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        RefreshSelfProfileAsync().Forget();
        RefreshFriendsList();
    }

    private async UniTaskVoid RefreshSelfProfileAsync()
    {
        if (!SteamClient.IsValid) return;

        // Update Name
        string name = SteamClient.Name;
        if (playerName != null) playerName.text = name;
        if (playerNameTmp != null) playerNameTmp.text = name;

        // Update Avatar
        if (pp != null)
        {
            try
            {
                var img = await SteamFriends.GetLargeAvatarAsync(SteamClient.SteamId);
                if (img.HasValue && pp != null)
                {
                    var tex = SteamFriendsManager.GetTextureFromImage(img.Value);
                    pp.texture = tex;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[InviteUI] Failed to load avatar: {ex.Message}");
            }
        }
    }

    public void RefreshFriendsList()
    {
        if (friendsContent == null) return;

        var prefab = friendItemPrefab;
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("Prefabs/PhoneFriendItem") 
                  ?? Resources.Load<GameObject>("Prefabs/FriendItem");
        }

        if (prefab != null && SteamFriendsManager.Instance != null)
        {
            SteamFriendsManager.Instance.PopulateFriends(friendsContent, prefab);
        }
    }

    public void OpenInviteOverlay()
    {
        if (!SteamClient.IsValid) return;
        LobbyService.Instance?.OpenInviteOverlay();
    }

    public void CopyLobbyId()
    {
        if (LobbyService.Instance != null && LobbyService.Instance.IsInLobby && LobbyService.Instance.CurrentLobby.HasValue)
        {
            string id = LobbyService.Instance.CurrentLobby.Value.Id.ToString();
            GUIUtility.systemCopyBuffer = id;
            ShowToast("로비 ID가 복사되었습니다.");
        }
        else
        {
            ShowToast("현재 참여 중인 로비가 없습니다.");
        }
    }

    private void ShowToast(string message)
    {
        if (toastText == null) return;

        if (_toastCoroutine != null)
        {
            StopCoroutine(_toastCoroutine);
        }
        _toastCoroutine = StartCoroutine(ToastRoutine(message));
    }

    private IEnumerator ToastRoutine(string message)
    {
        if (toastText != null)
        {
            toastText.text = message;
            toastText.gameObject.SetActive(true);
            yield return new WaitForSeconds(2.5f);
            toastText.gameObject.SetActive(false);
        }
    }
}
