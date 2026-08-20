using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace ExplosiveFactory.UI
{
    using Item = global::Item;

    public class InventoryUI : MonoBehaviour
    {
        private static InventoryUI? _instance;
        public static InventoryUI? Instance => _instance;

        [Header("Slot Configuration")]
        [SerializeField] private Transform? slotsParent;
        [SerializeField] private GameObject? slotPrefab;
        [SerializeField] private List<InventorySlotUI> slots = new();

        private ItemHolder? _boundItemHolder;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (slotsParent == null)
                slotsParent = transform.Find("SlotsContainer") ?? transform;
        }

        private void Start()
        {
            // 아직 바인딩된 ItemHolder가 없다면 로컬 플레이어 검색 시도
            if (_boundItemHolder == null)
            {
                TryBindLocalPlayer();
            }
        }

        public void TryBindLocalPlayer()
        {
            var players = FindObjectsByType<Player>(FindObjectsSortMode.None);
            foreach (var p in players)
            {
                if (p.isLocalPlayer && p.ItemHolder != null)
                {
                    Bind(p.ItemHolder);
                    break;
                }
            }
        }

        /// <summary>
        /// 로컬 플레이어의 ItemHolder와 인벤토리 UI를 연동합니다.
        /// </summary>
        public void Bind(ItemHolder itemHolder)
        {
            if (_boundItemHolder == itemHolder) return;

            Unbind();

            _boundItemHolder = itemHolder;
            _boundItemHolder.OnCurrentSlotChanged += HandleCurrentSlotChanged;
            _boundItemHolder.OnSlotItemChanged += HandleSlotItemChanged;
            _boundItemHolder.OnAllSlotsUpdated += HandleAllSlotsUpdated;

            EnsureSlotsCount(_boundItemHolder.MaxSlotCount);
            RefreshAllSlots();
        }

        public void Unbind()
        {
            if (_boundItemHolder != null)
            {
                _boundItemHolder.OnCurrentSlotChanged -= HandleCurrentSlotChanged;
                _boundItemHolder.OnSlotItemChanged -= HandleSlotItemChanged;
                _boundItemHolder.OnAllSlotsUpdated -= HandleAllSlotsUpdated;
                _boundItemHolder = null;
            }
        }

        private void OnDestroy()
        {
            Unbind();
            if (_instance == this)
                _instance = null;
        }

        private void HandleCurrentSlotChanged(int newIndex)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null)
                {
                    slots[i].SetSelected(i == newIndex);
                }
            }
        }

        private void HandleSlotItemChanged(int slotIndex, global::Item? item)
        {
            if (slotIndex >= 0 && slotIndex < slots.Count && slots[slotIndex] != null && _boundItemHolder != null)
            {
                slots[slotIndex].UpdateSlot(slotIndex, item, slotIndex == _boundItemHolder.CurrentHandyItemIndex);
            }
        }

        private void HandleAllSlotsUpdated()
        {
            RefreshAllSlots();
        }

        public void RefreshAllSlots()
        {
            if (_boundItemHolder == null) return;

            int currentIndex = _boundItemHolder.CurrentHandyItemIndex;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null)
                {
                    var item = _boundItemHolder.GetItemAtSlot(i);
                    slots[i].UpdateSlot(i, item, i == currentIndex);
                }
            }
        }

        private void EnsureSlotsCount(int count)
        {
            // 인스펙터에 슬롯이 이미 배치되어 있다면 그대로 사용
            if (slots.Count >= count) return;

            // 슬롯 부모에 있는 기존 SlotUI 탐색
            if (slotsParent != null)
            {
                var existing = slotsParent.GetComponentsInChildren<InventorySlotUI>(true);
                slots.Clear();
                slots.AddRange(existing);
            }

            // 그래도 부족하면 동적으로 슬롯 생성
            while (slots.Count < count)
            {
                int newIndex = slots.Count;
                var slotObj = CreateDefaultSlotObject(newIndex);
                var slotUI = slotObj.GetComponent<InventorySlotUI>();
                if (slotUI != null)
                {
                    slots.Add(slotUI);
                }
            }
        }

        private GameObject CreateDefaultSlotObject(int index)
        {
            var parent = slotsParent ?? transform;
            var go = new GameObject($"Slot_{index + 1}", typeof(RectTransform), typeof(Image), typeof(InventorySlotUI));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(90f, 90f);

            var bgImage = go.GetComponent<Image>();
            bgImage.color = new Color(0.12f, 0.12f, 0.12f, 0.75f);

            // 외곽선 (Outline)
            var outlineObj = new GameObject("Outline", typeof(RectTransform), typeof(Image));
            outlineObj.transform.SetParent(go.transform, false);
            var outlineRect = outlineObj.GetComponent<RectTransform>();
            outlineRect.anchorMin = Vector2.zero;
            outlineRect.anchorMax = Vector2.one;
            outlineRect.sizeDelta = Vector2.zero;
            var outlineImg = outlineObj.GetComponent<Image>();
            outlineImg.color = new Color(0.4f, 0.4f, 0.4f, 0.5f);
            outlineImg.raycastTarget = false;

            // 슬롯 번호 ([1], [2], [3])
            var numObj = new GameObject("SlotNumberText", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            numObj.transform.SetParent(go.transform, false);
            var numRect = numObj.GetComponent<RectTransform>();
            numRect.anchorMin = new Vector2(0f, 0.65f);
            numRect.anchorMax = new Vector2(1f, 1f);
            numRect.sizeDelta = Vector2.zero;
            var numText = numObj.GetComponent<TMPro.TextMeshProUGUI>();
            numText.text = $"[{index + 1}]";
            numText.fontSize = 16;
            numText.alignment = TMPro.TextAlignmentOptions.Center;
            numText.fontStyle = TMPro.FontStyles.Bold;
            numText.raycastTarget = false;

            // 아이콘 이미지
            var iconObj = new GameObject("ItemIcon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(go.transform, false);
            var iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.45f);
            iconRect.anchorMax = new Vector2(0.5f, 0.45f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(48f, 48f);
            var iconImg = iconObj.GetComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;
            iconObj.SetActive(false);

            // 아이템 이름 텍스트
            var nameObj = new GameObject("ItemNameText", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            nameObj.transform.SetParent(go.transform, false);
            var nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(1f, 0.35f);
            nameRect.sizeDelta = Vector2.zero;
            var nameText = nameObj.GetComponent<TMPro.TextMeshProUGUI>();
            nameText.text = "-";
            nameText.fontSize = 12;
            nameText.alignment = TMPro.TextAlignmentOptions.Center;
            nameText.textWrappingMode = TMPro.TextWrappingModes.Normal;
            nameText.raycastTarget = false;

            var slotUI = go.GetComponent<InventorySlotUI>();
            slotUI.Initialize(bgImage, outlineImg, iconImg, numText, nameText);

            return go;
        }
    }
}
