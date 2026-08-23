#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using ExplosiveFactory.Network;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ExplosiveFactory.Network
{
    [SingletonLifeTime(LifeTime.Application)]
    public class SteamFriendsManager : MonoSingleton<SteamFriendsManager>
    {
        private GameObject? _popupRoot;
        private Transform? _contentContainer;
        private GameObject? _itemTemplate;
        private readonly List<GameObject> _spawnedItems = new();
        private readonly Dictionary<ulong, Texture2D> _avatarCache = new();

        protected override void Awake()
        {
            base.Awake();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            foreach (var kvp in _avatarCache)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value);
                }
            }
            _avatarCache.Clear();
        }

        public static Texture2D GetTextureFromImage(Steamworks.Data.Image image)
        {
            Texture2D texture = new Texture2D((int)image.Width, (int)image.Height, TextureFormat.RGBA32, false);

            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    var p = image.GetPixel(x, y);
                    texture.SetPixel(x, (int)image.Height - 1 - y, new Color(p.r / 255.0f, p.g / 255.0f, p.b / 255.0f, p.a / 255.0f));
                }
            }
            texture.Apply();
            return texture;
        }

        public void PopulateFriends(Transform container, GameObject templatePrefab, FriendObjectMode mode = FriendObjectMode.Invite)
        {
            if (!SteamClient.IsValid || container == null || templatePrefab == null) return;

            // Clear old children
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }

            try
            {
                var friends = SteamFriends.GetFriends().ToList();
                friends.Sort((a, b) =>
                {
                    if (mode == FriendObjectMode.Join)
                    {
                        bool aHasLobby = a.IsPlayingThisGame && a.GameInfo.HasValue && a.GameInfo.Value.Lobby.HasValue && a.GameInfo.Value.Lobby.Value.Id.IsValid;
                        bool bHasLobby = b.IsPlayingThisGame && b.GameInfo.HasValue && b.GameInfo.Value.Lobby.HasValue && b.GameInfo.Value.Lobby.Value.Id.IsValid;
                        if (aHasLobby != bHasLobby) return bHasLobby.CompareTo(aHasLobby);
                    }

                    int scoreA = a.IsPlayingThisGame ? 3 : (a.IsOnline ? 2 : 1);
                    int scoreB = b.IsPlayingThisGame ? 3 : (b.IsOnline ? 2 : 1);
                    return scoreB.CompareTo(scoreA);
                });

                foreach (var friend in friends)
                {
                    var item = Instantiate(templatePrefab, container);
                    item.SetActive(true);

                    SteamId? lobbyId = null;
                    if (friend.GameInfo.HasValue && friend.GameInfo.Value.Lobby.HasValue && friend.GameInfo.Value.Lobby.Value.Id.IsValid)
                    {
                        lobbyId = friend.GameInfo.Value.Lobby.Value.Id;
                    }

                    var fo = item.GetComponent<FriendObject>() ?? item.GetComponentInChildren<FriendObject>();
                    if (fo != null)
                    {
                        fo.Setup(friend.Id, friend.Name, mode, lobbyId);
                    }

                    var nameText = item.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>()
                                ?? item.GetComponentInChildren<TextMeshProUGUI>();
                    if (nameText != null)
                    {
                        string statusTag;
                        if (friend.IsPlayingThisGame)
                        {
                            statusTag = (lobbyId.HasValue && lobbyId.Value.IsValid)
                                ? "<color=#00FF88>[로비 접속 중]</color> "
                                : "<color=#00FFAA>[인게임]</color> ";
                        }
                        else
                        {
                            statusTag = friend.IsOnline ? "<color=#55AAFF>[온라인]</color> " : "<color=#888888>[오프라인]</color> ";
                        }
                        nameText.text = $"{statusTag}{friend.Name}";
                    }

                    AssignAvatarAsync(item, friend.Id).Forget();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SteamFriendsManager] PopulateFriends error: {ex.Message}");
            }
        }



        private async UniTaskVoid AssignAvatarAsync(GameObject item, SteamId id)
        {
            if (item == null) return;
            try
            {
                if (_avatarCache.TryGetValue(id.Value, out var cachedTex) && cachedTex != null)
                {
                    var raw = item.GetComponentInChildren<RawImage>();
                    if (raw != null)
                    {
                        raw.texture = cachedTex;
                    }
                    return;
                }

                var img = await SteamFriends.GetLargeAvatarAsync(id);
                if (img.HasValue && item != null)
                {
                    var tex = GetTextureFromImage(img.Value);
                    _avatarCache[id.Value] = tex;
                    var raw = item.GetComponentInChildren<RawImage>();
                    if (raw != null)
                    {
                        raw.texture = tex;
                    }
                }
            }
            catch (Exception)
            {
                // Ignored
            }
        }
    }
}
