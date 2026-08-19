using Steamworks;
using UnityEngine;
using UnityEngine.UI;

[SingletonLifeTime(LifeTime.Scene)]
public class SteamFriendsManager : MonoSingleton<SteamFriendsManager>
{
    public RawImage pp;
    public Text playerName;

    public Transform friendsContent;
    public GameObject friendObj;

    private void Start()
    {
        if (!SteamClient.IsValid) return;
        if (friendsContent != null)
            InitFriendsAsync(friendsContent, playerName, pp);
    }

    public static Texture2D GetTextureFromImage(Steamworks.Data.Image image)
    {
        Texture2D texture = new Texture2D((int)image.Width, (int)image.Height);

        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                var p = image.GetPixel(x, y);
                texture.SetPixel(x, (int)image.Height - y, new Color(p.r / 255.0f, p.g / 255.0f, p.b / 255.0f, p.a / 255.0f));
            }
        }
        texture.Apply();
        return texture;
    }

    public async void InitFriendsAsync(Transform friendsContent, Text playerName, RawImage pp)
    {
        if (!SteamClient.IsValid) return;

        try
        {
            var img = await SteamFriends.GetLargeAvatarAsync(SteamClient.SteamId);
            if (img.HasValue && pp != null)
                pp.texture = GetTextureFromImage(img.Value);

            if (playerName != null)
                playerName.text = SteamClient.Name;

            if (friendsContent != null && friendObj != null)
            {
                foreach (var friend in SteamFriends.GetFriends())
                {
                    GameObject f = PoolManager.Get(friendObj, friendsContent);
                    var txt = f.GetComponentInChildren<Text>();
                    if (txt != null) txt.text = friend.Name;

                    var fo = f.GetComponent<FriendObject>();
                    if (fo != null) fo.steamId = friend.Id;

                    AssignFriendImage(f, friend.Id);
                }
            }
            Debug.Log("Friends initialized");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SteamFriendsManager] InitFriendsAsync error: {ex.Message}");
        }
    }

    public async void AssignFriendImage(GameObject f, SteamId id)
    {
        try
        {
            var img = await SteamFriends.GetLargeAvatarAsync(id);
            if (img.HasValue && f != null)
            {
                var rawImg = f.GetComponentInChildren<RawImage>();
                if (rawImg != null)
                    rawImg.texture = GetTextureFromImage(img.Value);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[SteamFriendsManager] AssignFriendImage error: {ex.Message}");
        }
    }
}
