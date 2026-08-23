using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전체 플레이어 스킨 ScriptableObject 데이터베이스.
/// - Resources/SkinData/PlayerSkinDatabase 경로에서 자동 로드됩니다.
/// </summary>
[CreateAssetMenu(fileName = "PlayerSkinDatabase", menuName = "ExplosiveFactory/PlayerSkinDatabase", order = 0)]
public class PlayerSkinDatabase : ScriptableObject
{
    private static PlayerSkinDatabase _instance;
    public static PlayerSkinDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<PlayerSkinDatabase>("SkinData/PlayerSkinDatabase");
            }
            return _instance;
        }
    }

    [Header("Registered Skins")]
    [SerializeField]
    private List<PlayerSkinData> _skins = new List<PlayerSkinData>();
    public IReadOnlyList<PlayerSkinData> Skins => _skins;
    public int SkinCount => _skins.Count;

    public PlayerSkinData GetSkin(int index)
    {
        if (index >= 0 && index < _skins.Count)
        {
            return _skins[index];
        }
        return _skins.Count > 0 ? _skins[0] : null;
    }

    public PlayerSkinData GetSkin(string skinId)
    {
        for (int i = 0; i < _skins.Count; i++)
        {
            if (_skins[i] != null && _skins[i].skinId == skinId)
            {
                return _skins[i];
            }
        }
        return null;
    }

    public int GetSkinIndex(string skinId)
    {
        for (int i = 0; i < _skins.Count; i++)
        {
            if (_skins[i] != null && _skins[i].skinId == skinId)
            {
                return i;
            }
        }
        return 0;
    }
}
