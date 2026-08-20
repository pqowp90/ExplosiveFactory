using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace ExplosiveFactory.UI
{
    using Item = global::Item;

    public class InventorySlotUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Image? backgroundImage;
        [SerializeField] private Image? outlineImage;
        [SerializeField] private Image? itemIconImage;
        [SerializeField] private TextMeshProUGUI? slotNumberText;
        [SerializeField] private TextMeshProUGUI? itemNameText;

        [Header("Colors & Styles")]
        [SerializeField] private Color selectedOutlineColor = new Color(1f, 0.88f, 0.2f, 1f);
        [SerializeField] private Color normalOutlineColor = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        [SerializeField] private Color selectedBgColor = new Color(0.25f, 0.25f, 0.25f, 0.85f);
        [SerializeField] private Color normalBgColor = new Color(0.1f, 0.1f, 0.1f, 0.6f);
        [SerializeField] private Vector3 selectedScale = new Vector3(1.08f, 1.08f, 1.08f);
        [SerializeField] private Vector3 normalScale = Vector3.one;

        private int _slotIndex;
        public int SlotIndex => _slotIndex;

        public void Initialize(Image? bg, Image? outline, Image? icon, TextMeshProUGUI? numText, TextMeshProUGUI? nameText)
        {
            backgroundImage = bg;
            outlineImage = outline;
            itemIconImage = icon;
            slotNumberText = numText;
            itemNameText = nameText;
        }

        /// <summary>
        /// 슬롯의 데이터와 선택 상태를 갱신합니다.
        /// </summary>
        public void UpdateSlot(int slotIndex, global::Item? item, bool isSelected)
        {
            _slotIndex = slotIndex;

            // 슬롯 번호 ([1], [2], [3]...)
            if (slotNumberText != null)
            {
                slotNumberText.text = $"[{slotIndex + 1}]";
                slotNumberText.color = isSelected ? selectedOutlineColor : Color.white;
            }

            // 아이템 이름 및 상태
            if (itemNameText != null)
            {
                if (item != null)
                {
                    string displayName = item.ItemData != null && !string.IsNullOrEmpty(item.ItemData.itemName)
                        ? item.ItemData.itemName
                        : item.name.Replace("(Clone)", "").Trim();
                    itemNameText.text = displayName;
                    itemNameText.color = Color.white;
                }
                else
                {
                    itemNameText.text = "-";
                    itemNameText.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                }
            }

            // 아이콘 이미지 (있는 경우)
            if (itemIconImage != null)
            {
                if (item != null && item.ItemData != null && item.ItemData.icon != null)
                {
                    itemIconImage.sprite = item.ItemData.icon;
                    itemIconImage.color = Color.white;
                    itemIconImage.gameObject.SetActive(true);
                }
                else
                {
                    itemIconImage.gameObject.SetActive(false);
                }
            }

            // 선택 하이라이트 스타일 적용
            if (outlineImage != null)
            {
                outlineImage.color = isSelected ? selectedOutlineColor : normalOutlineColor;
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = isSelected ? selectedBgColor : normalBgColor;
            }

            transform.localScale = isSelected ? selectedScale : normalScale;
        }

        /// <summary>
        /// 선택 상태만 빠르게 갱신합니다.
        /// </summary>
        public void SetSelected(bool isSelected)
        {
            if (outlineImage != null)
                outlineImage.color = isSelected ? selectedOutlineColor : normalOutlineColor;

            if (backgroundImage != null)
                backgroundImage.color = isSelected ? selectedBgColor : normalBgColor;

            if (slotNumberText != null)
                slotNumberText.color = isSelected ? selectedOutlineColor : Color.white;

            transform.localScale = isSelected ? selectedScale : normalScale;
        }
    }
}
