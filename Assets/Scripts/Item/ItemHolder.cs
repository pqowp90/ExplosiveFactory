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

    [SyncVar(hook = nameof(OnCurrentHandyItemIndexChanged))]
    private int _currentHandyItemIndex = 0;

    public readonly SyncList<Item> _holdingItems = new();

    private float _mouseScroll;
    private HandyItemObject _currentHandyItemObject;
    public HandyItemObject CurrentHandyItemObject => _currentHandyItemObject;

    private void Awake()
    {
        _player = GetComponent<Player>();
        if (_itemDropPoint == null)
            _itemDropPoint = transform;

        GetHandyTransformByHandyType();
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

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        if (_player != null && _player.InputController != null && _player.InputController.MouseScrollAction != null)
        {
            _player.InputController.MouseScrollAction.performed += ctx => _mouseScroll = ctx.ReadValue<float>();
            _player.InputController.MouseScrollAction.Enable();
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        _holdingItems.Callback += OnHoldingItemsChanged;
        UpdateHandyObject();
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

    private void OnHoldingItemsChanged(SyncList<Item>.Operation op, int index, Item oldItem, Item newItem)
    {
        if (newItem != null)
        {
            newItem.ItemHolder = this;
        }

        if (index == _currentHandyItemIndex)
        {
            UpdateHandyObject();
        }

        OnSlotItemChanged?.Invoke(index, newItem);
        OnAllSlotsUpdated?.Invoke();
    }

    private void OnCurrentHandyItemIndexChanged(int oldIndex, int newIndex)
    {
        _currentHandyItemIndex = newIndex;
        UpdateHandyObject();
        OnCurrentSlotChanged?.Invoke(newIndex);
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        // 슬롯 변경 (마우스 휠 스크롤)
        if (CursorManager.Instance == null || CursorManager.Instance.CurrentCursor == CursorType.Player)
        {
            if (_mouseScroll > 0.01f)
            {
                CmdChangeHandyItemIndex(-1);
                _mouseScroll = 0f;
            }
            else if (_mouseScroll < -0.01f)
            {
                CmdChangeHandyItemIndex(1);
                _mouseScroll = 0f;
            }
        }

        // 아이템 사용 (좌클릭)
        if (_player != null && _player.InputController != null && _player.InputController.MouseLeftClickAction != null)
        {
            if (_player.InputController.MouseLeftClickAction.triggered)
            {
                CmdUseItem();
            }
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdChangeHandyItemIndex(int delta)
    {
        if (_maxHandyItemIndex <= 0) return;

        int newIndex = (_currentHandyItemIndex + delta) % _maxHandyItemIndex;
        if (newIndex < 0) newIndex += _maxHandyItemIndex;

        CurrentHandyItemIndex = newIndex;
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
            UpdateHandyObject();
            if (_player != null && _player.PlayerAnimation != null)
            {
                _player.PlayerAnimation.SetHoldingItem(HoldingItem != null);
            }
        }
    }

    public Item HoldingItem
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
            EnsureHoldingItemsCapacity();

            var oldItem = HoldingItem;
            if (oldItem != null && oldItem != value)
            {
                Vector3 dropPos = _itemDropPoint != null ? _itemDropPoint.position : transform.position + transform.forward * 0.5f + Vector3.up * 0.5f;
                Quaternion dropRot = _itemDropPoint != null ? _itemDropPoint.rotation : transform.rotation;
                Vector3 dropVel = transform.forward * 2f + Vector3.up * 1f;
                oldItem.DropItem(dropPos, dropRot, dropVel);
            }

            if (value != null)
            {
                value.ItemHolder = this;
                value.PickUpItem();
            }

            _holdingItems[_currentHandyItemIndex] = value;

            UpdateHandyObject();
            if (_player != null && _player.PlayerAnimation != null)
            {
                _player.PlayerAnimation.SetHoldingItem(value != null);
            }
        }
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
        var currentItem = HoldingItem;
        if (currentItem == null) return;

        EnsureHoldingItemsCapacity();
        _holdingItems[_currentHandyItemIndex] = null;

        Vector3 dropPos = _itemDropPoint != null ? _itemDropPoint.position : transform.position + transform.forward * 0.5f + Vector3.up * 0.5f;
        Quaternion dropRot = _itemDropPoint != null ? _itemDropPoint.rotation : transform.rotation;
        Vector3 dropVel = transform.forward * 2f + Vector3.up * 1f;

        currentItem.DropItem(dropPos, dropRot, dropVel);

        UpdateHandyObject();
        if (_player != null && _player.PlayerAnimation != null)
        {
            _player.PlayerAnimation.SetHoldingItem(false);
        }
    }

    [Command(requiresAuthority = false)]
    public void PickUpItem(Item item)
    {
        if (item == null || item.IsPickedUp) return;

        HoldingItem = item;
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
}