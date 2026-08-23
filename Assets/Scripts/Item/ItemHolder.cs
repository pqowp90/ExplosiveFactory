using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ItemHolder : NetworkBehaviour
{
    [Serializable]
    public class ItemHandyTypeTransform
    {
        public PlayerHandyType PlayerHandyType;
        public Transform HandyTransform;
        public Transform BodyTransform;
    }

    private readonly Dictionary<PlayerHandyType, Transform> _handyTypeTransforms = new();
    private readonly Dictionary<PlayerHandyType, Transform> _bodyTypeTransforms = new();

    [SerializeField]
    private List<ItemHandyTypeTransform> _itemHandyTypeTransforms = new();

    [SerializeField]
    private Transform _itemDropPoint;

    [SerializeField]
    private int _maxHandyItemIndex = 3;

    private Player _player;
    public Player Player => _player ??= GetComponent<Player>();

    private int _currentHandyItemIndex = 0;
    private readonly List<Item?> _holdingItems = new();

    private HandyItemObject _currentHandyItemObject;
    public HandyItemObject CurrentHandyItemObject => _currentHandyItemObject;

    private void Awake()
    {
        _player = GetComponent<Player>();
        if (_itemDropPoint == null)
            _itemDropPoint = transform;

        GetHandyTransformByHandyType();
        if (_player != null && _player.PlayerBodyTransform != null)
        {
            RebindBodySockets(_player.PlayerBodyTransform);
        }
        EnsureHoldingItemsCapacity();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        _holdingItems.Clear();
        for (int i = 0; i < _maxHandyItemIndex; i++)
        {
            _holdingItems.Add(null);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        EnsureHoldingItemsCapacity();
        UpdateHandyObject();
        CmdRequestHolderState();
    }

    [Command(requiresAuthority = false)]
    private void CmdRequestHolderState(NetworkConnectionToClient? conn = null)
    {
        if (conn != null)
        {
            EnsureHoldingItemsCapacity();
            TargetSyncHolderState(conn, _currentHandyItemIndex, _holdingItems.ToArray());
        }
    }

    [TargetRpc]
    private void TargetSyncHolderState(NetworkConnection target, int currentIndex, Item?[] items)
    {
        _currentHandyItemIndex = currentIndex;
        _holdingItems.Clear();
        if (items != null)
        {
            _holdingItems.AddRange(items);
        }
        EnsureHoldingItemsCapacity();

        UpdateHandyObject();
        OnCurrentSlotChanged?.Invoke(currentIndex);
        OnAllSlotsUpdated?.Invoke();

        if (_player != null && _player.PlayerAnimation != null)
        {
            _player.PlayerAnimation.SetHoldingItem(HoldingItem != null);
        }
    }

    public event Action<int>? OnCurrentSlotChanged;
    public event Action<int, Item?>? OnSlotItemChanged;
    public event Action? OnAllSlotsUpdated;

    public int MaxSlotCount => _maxHandyItemIndex;

    public Item? GetItemAtSlot(int index)
    {
        if (index >= 0 && index < _holdingItems.Count)
            return _holdingItems[index];
        return null;
    }

    private void Update()
    {
        if (!isLocalPlayer || _player == null || _player.InputController == null) return;

        // 슬롯 변경 (마우스 휠 스크롤)
        float scroll = _player.InputController.ScrollValue;
        if (scroll > 0.01f && _currentHandyItemIndex > 0)
        {
            CmdChangeHandyItemIndex(-1);
        }
        else if (scroll < -0.01f && _currentHandyItemIndex < _maxHandyItemIndex - 1)
        {
            CmdChangeHandyItemIndex(1);
        }

        // 아이템 사용 (우클릭 - 게임플레이 모드 또는 스마트폰 UI 뒤로가기)
        if (_player.InputController.IsUseSecondaryTriggered)
        {
            CmdUseItem();
        }
        else if (CursorManager.Instance != null && CursorManager.Instance.CurrentCursor == CursorType.UI)
        {
            if (_player.InputController.IsRawSecondaryClickTriggered)
            {
                CmdUseItem();
            }
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdChangeHandyItemIndex(int delta)
    {
        if (_maxHandyItemIndex <= 0) return;

        int newIndex = Mathf.Clamp(_currentHandyItemIndex + delta, 0, _maxHandyItemIndex - 1);
        if (newIndex == _currentHandyItemIndex) return;

        _currentHandyItemIndex = newIndex;
        RpcSetHandyItemIndex(newIndex);
    }

    [ClientRpc]
    private void RpcSetHandyItemIndex(int newIndex)
    {
        _currentHandyItemIndex = newIndex;
        UpdateHandyObject();
        OnCurrentSlotChanged?.Invoke(newIndex);

        if (_player != null && _player.PlayerAnimation != null)
        {
            _player.PlayerAnimation.SetHoldingItem(HoldingItem != null);
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdUseItem()
    {
        if (HoldingItem != null)
        {
            HoldingItem.UseItem();
        }
    }

    public int CurrentHandyItemIndex
    {
        get => _currentHandyItemIndex;
        set
        {
            if (!NetworkServer.active) return;
            if (value < 0 || value >= _maxHandyItemIndex) return;

            _currentHandyItemIndex = value;
            RpcSetHandyItemIndex(value);
        }
    }

    public Item? HoldingItem
    {
        get
        {
            if (_currentHandyItemIndex >= 0 && _currentHandyItemIndex < _holdingItems.Count)
            {
                return _holdingItems[_currentHandyItemIndex];
            }
            return null;
        }
        set
        {
            if (!NetworkServer.active) return;
            SetSlotItemOnServer(_currentHandyItemIndex, value);
        }
    }

    [Server]
    private void SetSlotItemOnServer(int slotIndex, Item? newItem)
    {
        EnsureHoldingItemsCapacity();
        if (slotIndex < 0 || slotIndex >= _maxHandyItemIndex) return;

        var oldItem = _holdingItems[slotIndex];
        if (oldItem != null && oldItem != newItem)
        {
            Vector3 dropPos = _itemDropPoint != null ? _itemDropPoint.position : transform.position + transform.forward * 0.5f + Vector3.up * 0.5f;
            Quaternion dropRot = _itemDropPoint != null ? _itemDropPoint.rotation : transform.rotation;
            Vector3 dropVel = transform.forward * 2f + Vector3.up * 1f;
            oldItem.DropItem(dropPos, dropRot, dropVel);
        }

        _holdingItems[slotIndex] = newItem;

        if (newItem != null)
        {
            newItem.ItemHolder = this;
            newItem.PickUpItem(this);
        }

        RpcSetSlotItem(slotIndex, newItem);
    }

    [ClientRpc]
    private void RpcSetSlotItem(int slotIndex, Item? newItem)
    {
        EnsureHoldingItemsCapacity();
        if (slotIndex < 0 || slotIndex >= _holdingItems.Count) return;

        _holdingItems[slotIndex] = newItem;
        if (newItem != null)
        {
            newItem.ItemHolder = this;
        }

        if (slotIndex == _currentHandyItemIndex)
        {
            UpdateHandyObject();
            if (_player != null && _player.PlayerAnimation != null)
            {
                _player.PlayerAnimation.SetHoldingItem(newItem != null);
            }
        }

        OnSlotItemChanged?.Invoke(slotIndex, newItem);
        OnAllSlotsUpdated?.Invoke();
    }

    private void EnsureHoldingItemsCapacity()
    {
        while (_holdingItems.Count < _maxHandyItemIndex)
        {
            _holdingItems.Add(null);
        }
    }

    [Command(requiresAuthority = false)]
    public void DropItem()
    {
        if (HoldingItem == null) return;
        SetSlotItemOnServer(_currentHandyItemIndex, null);
    }

    [Command(requiresAuthority = false)]
    public void PickUpItem(Item item)
    {
        if (item == null || item.IsPickedUp) return;
        SetSlotItemOnServer(_currentHandyItemIndex, item);
    }

    private void UpdateHandyObject()
    {
        SetHandyObject(HoldingItem);
    }

    private void SetHandyObject(Item item)
    {
        if (_player != null && _player.PlayerAnimation != null)
        {
            _player.PlayerAnimation.ResetJump();
            _player.PlayerAnimation.ResetHandyAnimation();
        }

        // 기존 쥐고 있던 HandyObject 정리
        if (_currentHandyItemObject != null)
        {
            if (_currentHandyItemObject.Item != null)
            {
                _currentHandyItemObject.Item.OnHandyItemObjectDespawned();
            }
            PoolManager.Release(_currentHandyItemObject);
            _currentHandyItemObject = null;
        }

        if (item == null || item.HandyItemObjectPrefab == null)
        {
            if (_player != null && _player.PlayerAnimation != null)
            {
                _player.PlayerAnimation.SetAnimatorController(null);
            }
            return;
        }

        // 새 HandyObject 스폰 및 소켓 부착
        var handyItemObj = PoolManager.Get(item.HandyItemObjectPrefab);
        if (handyItemObj == null) return;

        item.HandyItemObject = handyItemObj;
        _currentHandyItemObject = handyItemObj;
        _currentHandyItemObject.Item = item;
        _currentHandyItemObject.OnSpawned(_player);
        item.OnHandyItemObjectSpawned();

        if (_player != null && _player.PlayerAnimation != null)
        {
            _player.PlayerAnimation.SetAnimatorController(handyItemObj);
        }

        if (isOwned)
        {
            if (_handyTypeTransforms.TryGetValue(handyItemObj.PlayerHandyType, out var handTransform) && handTransform != null)
            {
                handyItemObj.transform.SetParent(handTransform);
                handyItemObj.transform.localPosition = handyItemObj.HandOffset;
                handyItemObj.transform.localRotation = Quaternion.Euler(handyItemObj.HandRotation);
                SetLayerByParent(handyItemObj.transform);
            }
        }
        else
        {
            if (_bodyTypeTransforms.TryGetValue(handyItemObj.PlayerHandyType, out var bodyTransform) && bodyTransform != null)
            {
                handyItemObj.transform.SetParent(bodyTransform);
                handyItemObj.transform.localPosition = handyItemObj.BodyOffset;
                handyItemObj.transform.localRotation = Quaternion.Euler(handyItemObj.BodyRotation);
                SetLayerByParent(handyItemObj.transform);
            }
        }
    }

    private void SetLayerByParent(Transform target)
    {
        if (target == null || target.parent == null) return;
        var layer = target.parent.gameObject.layer;
        target.gameObject.layer = layer;
        foreach (Transform child in target)
        {
            if (child != null)
            {
                child.gameObject.layer = layer;
            }
        }

        var renderers = target.GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
        {
            if (r != null)
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }

    private void GetHandyTransformByHandyType()
    {
        _handyTypeTransforms.Clear();
        _bodyTypeTransforms.Clear();
        foreach (var itemHandyTypeTransform in _itemHandyTypeTransforms)
        {
            if (itemHandyTypeTransform != null)
            {
                if (itemHandyTypeTransform.HandyTransform != null)
                    _handyTypeTransforms[itemHandyTypeTransform.PlayerHandyType] = itemHandyTypeTransform.HandyTransform;
                if (itemHandyTypeTransform.BodyTransform != null)
                    _bodyTypeTransforms[itemHandyTypeTransform.PlayerHandyType] = itemHandyTypeTransform.BodyTransform;
            }
        }
    }

    /// <summary>
    /// 플레이어 모델링 교체 시 새 3인칭 모델의 오른손/왼손 본을 탐색하여 3인칭 소켓을 동적으로 재바인딩합니다.
    /// </summary>
    public void RebindBodySockets(Transform newBodyTransform)
    {
        if (newBodyTransform == null) return;

        Transform rightHand = null;
        Transform leftHand = null;

        var animator = newBodyTransform.GetComponentInChildren<Animator>();
        if (animator != null && animator.isHuman)
        {
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        }

        // 이름 기반 폴백 탐색
        if (rightHand == null || leftHand == null)
        {
            var allTransforms = newBodyTransform.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                string lower = t.name.ToLower();
                if (rightHand == null && (lower.Contains("righthand") || lower.Contains("hand.r") || lower.Contains("hand_r") || lower.EndsWith(":righthand") || lower == "right hand"))
                {
                    rightHand = t;
                }
                if (leftHand == null && (lower.Contains("lefthand") || lower.Contains("hand.l") || lower.Contains("hand_l") || lower.EndsWith(":lefthand") || lower == "left hand"))
                {
                    leftHand = t;
                }
            }
        }

        if (rightHand != null)
        {
            _bodyTypeTransforms[PlayerHandyType.Right] = rightHand;
            SetBodyTransformInList(PlayerHandyType.Right, rightHand);
        }
        if (leftHand != null)
        {
            _bodyTypeTransforms[PlayerHandyType.Left] = leftHand;
            SetBodyTransformInList(PlayerHandyType.Left, leftHand);
        }

        // 현재 들고 있는 아이템이 있다면 새 소켓으로 재배치
        if (_currentHandyItemObject != null)
        {
            UpdateHandyObject();
        }
    }

    private void SetBodyTransformInList(PlayerHandyType type, Transform targetTransform)
    {
        if (targetTransform == null) return;

        bool found = false;
        for (int i = 0; i < _itemHandyTypeTransforms.Count; i++)
        {
            var item = _itemHandyTypeTransforms[i];
            if (item != null && item.PlayerHandyType == type)
            {
                item.BodyTransform = targetTransform;
                found = true;
                break;
            }
        }

        if (!found)
        {
            _itemHandyTypeTransforms.Add(new ItemHandyTypeTransform
            {
                PlayerHandyType = type,
                BodyTransform = targetTransform
            });
        }
    }
}