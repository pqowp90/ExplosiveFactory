using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 핸드폰 UI 내 3D 캐릭터 모델링(스킨) 선택 화면 컨트롤러.
/// </summary>
public class ModelSelectUI : MonoBehaviour
{
    [System.Serializable]
    public class SkinSlotUI
    {
        public Button button;
        public Image iconImage;
        public TextMeshProUGUI nameText;
        public GameObject selectedHighlight;
    }

    [Header("Slot UI Elements (Static Inspector References)")]
    [Tooltip("에디터에 정적 배치된 스킨 슬롯 UI 목록")]
    [SerializeField] private List<SkinSlotUI> skinSlots = new List<SkinSlotUI>();

    [Header("Info Display")]
    [SerializeField] private TextMeshProUGUI currentSkinNameText;
    [SerializeField] private TextMeshProUGUI currentSkinDescText;

    private Player _localPlayer;

    private void OnEnable()
    {
        RefreshUI();
    }

    public void SetLocalPlayer(Player player)
    {
        _localPlayer = player;
        RefreshUI();
    }

    public void RefreshUI()
    {
        var database = PlayerSkinDatabase.Instance;
        if (database == null) return;

        if (_localPlayer == null)
        {
            _localPlayer = GetComponentInParent<Player>() ?? FindLocalPlayer();
        }

        int currentSkin = (_localPlayer != null && _localPlayer.PlayerSkinController != null)
            ? _localPlayer.PlayerSkinController.CurrentSkinIndex
            : 0;

        var activeSkin = database.GetSkin(currentSkin);
        if (activeSkin != null)
        {
            if (currentSkinNameText != null) currentSkinNameText.text = activeSkin.skinName;
            if (currentSkinDescText != null) currentSkinDescText.text = activeSkin.description;
        }

        for (int i = 0; i < skinSlots.Count; i++)
        {
            var slot = skinSlots[i];
            if (slot == null || slot.button == null) continue;

            if (i < database.SkinCount)
            {
                var skinData = database.GetSkin(i);
                slot.button.gameObject.SetActive(true);

                if (slot.nameText != null) slot.nameText.text = skinData.skinName;
                if (slot.iconImage != null && skinData.skinIcon != null)
                {
                    slot.iconImage.sprite = skinData.skinIcon;
                    slot.iconImage.gameObject.SetActive(true);
                }

                if (slot.selectedHighlight != null)
                {
                    slot.selectedHighlight.SetActive(i == currentSkin);
                }

                int skinIndex = i;
                slot.button.onClick.RemoveAllListeners();
                slot.button.onClick.AddListener(() => OnSkinSlotClicked(skinIndex));
            }
            else
            {
                slot.button.gameObject.SetActive(false);
            }
        }
    }

    private void OnSkinSlotClicked(int skinIndex)
    {
        if (_localPlayer == null)
        {
            _localPlayer = GetComponentInParent<Player>() ?? FindLocalPlayer();
        }

        if (_localPlayer != null && _localPlayer.PlayerSkinController != null)
        {
            _localPlayer.PlayerSkinController.CmdChangeSkin(skinIndex);
            RefreshUI();
        }
    }

    private Player FindLocalPlayer()
    {
        var players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p.isLocalPlayer || p.isOwned) return p;
        }
        return players.Length > 0 ? players[0] : null;
    }
}
