using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    private readonly Dictionary<PlayerHandyType, Transform> _handyTypeTransforms = new Dictionary<PlayerHandyType, Transform>();
    private readonly Dictionary<PlayerHandyType, Transform> _bodyTypeTransforms = new Dictionary<PlayerHandyType, Transform>();
    [SerializeField]
    private List<ItemHandyTypeTransform> _itemHandyTypeTransforms;

    [SerializeField]
    private Transform _itemDropPoint;
    private Player _player;
    public Player Player => _player ??= GetComponent<Player>();
    private void Awake()
    {
        _player = GetComponent<Player>();
        if (_itemDropPoint == null)
            _itemDropPoint = transform;



        GetHandyTransformByHandyType();
        SettingHolder();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        _player.InputController.MouseScrollAction.performed += x => _mouseScroll = x.ReadValue<float>();
        _player.InputController.MouseScrollAction.Enable();
    }
    public override void OnStartClient()
    {
        base.OnStartClient();

        if (HoldingItem != null && _currentHandyItemObject == null)
        {
            // 방에 들어왔을때 다른 사람이 들고있는 아이템 적용 
            SetHandyObject(HoldingItem);
        }
    }
    [SerializeField]
    private float _mouseScroll;
    [Command(requiresAuthority = false)]
    private void CmdSyncCurrentHandyItemIndex(int index)
    {
        CurrentHandyItemIndex += index;
    }
    [Command(requiresAuthority = false)]
    private void CmdUseItem()
    {
        Debug.Log(HoldingItem);
        if (HoldingItem != null)
        {
            HoldingItem.UseItem();
        }
    }
    private void Update()
    {
        if (!isLocalPlayer) return;
        if (CursorManager.Instance.CurrentCursor == CursorType.Player)
        {
            if (_mouseScroll > 0)
            {
                CmdSyncCurrentHandyItemIndex(1);
            }
            else if (_mouseScroll < 0)
            {
                CmdSyncCurrentHandyItemIndex(-1);
            }
        }
        if (_player.InputController.MouseLeftClickAction.triggered)
        {
            CmdUseItem();
        }
    }
    private void SettingHolder()
    {
        for (int i = 0; i < _maxHandyItemIndex - _holdingItems.Count; i++)
        {
            _holdingItems.Add(null);
        }
    }
    [SerializeField]
    private int _maxHandyItemIndex = 3;
    private int _currentHandyItemIndex;


    public int CurrentHandyItemIndex
    {
        get => _currentHandyItemIndex;
        set
        {
            if (!NetworkServer.active) return;
            if (_maxHandyItemIndex != _holdingItems.Count)
            {
                SettingHolder();
            }
            if (value < 0 || value >= _maxHandyItemIndex) return;
            _currentHandyItemIndex = value;
            RpcSetCurrentHandyItemIndex(value);

            RpcSetHandyObject(null);
            RpcSetHandyObject(HoldingItem);
            _player.PlayerAnimation.SetHoldingItem(HoldingItem);
        }
    }
    [ClientRpc]
    private void RpcSetCurrentHandyItemIndex(int value)
    {
        _currentHandyItemIndex = value;
    }

    private List<Item> _holdingItems = new List<Item>();

    public Item HoldingItem
    {
        get => _holdingItems[_currentHandyItemIndex];
        set
        {

            if (value == null)
            {

                HoldingItem.DropItem(_itemDropPoint.position, _itemDropPoint.rotation);
                RpcSetHandyObject(null);
                HoldingItem.ItemHolder = null;
            }
            _holdingItems[_currentHandyItemIndex] = value;
            RpcSetHoldingItem(value, _currentHandyItemIndex);
            if (value != null)
            {
                value.ItemHolder = this;
                value.PickUpItem();
                RpcSetHandyObject(value);
            }
            _player.PlayerAnimation.SetHoldingItem(value);
        }
    }
    [ClientRpc]
    private void RpcSetHoldingItem(Item value, int currentHandyItemIndex)
    {
        _holdingItems[currentHandyItemIndex] = value;
    }

    [Command(requiresAuthority = false)]
    public void DropItem()
    {

        if (HoldingItem == null) return;
        HoldingItem = null;
    }
    [Command(requiresAuthority = false)]
    public void PickUpItem(Item item)
    {
        if (item.IsPickedUp) return;
        if (HoldingItem != null)
            HoldingItem = null;
        HoldingItem = item;
    }
    private void SetHandyObject(Item item)
    {
        _player.PlayerAnimation.ResetJump();
        if (item == null)
        {
            if (_currentHandyItemObject == null) return;
            _currentHandyItemObject.Item.OnHandyItemObjectDespawned();
            PoolManager.Release(_currentHandyItemObject);
            _currentHandyItemObject = null;
            return;
        }
        else
        {
            if (item.HandyItemObjectPrefab == null) return;
            var HandyItemObject = PoolManager.Get(item.HandyItemObjectPrefab);
            item.HandyItemObject = HandyItemObject;
            _currentHandyItemObject = HandyItemObject;
            _currentHandyItemObject.Item = item;
            _currentHandyItemObject.OnSpawned(_player);
            item.OnHandyItemObjectSpawned();

            _player.PlayerAnimation.SetAnimatorController(HandyItemObject);
            if (isOwned)
            {
                _handyTypeTransforms.TryGetValue(HandyItemObject.PlayerHandyType, out var itemHandyTypeTransform);
                HandyItemObject.transform.SetParent(itemHandyTypeTransform);
                HandyItemObject.transform.localPosition = HandyItemObject.HandOffset;
                HandyItemObject.transform.localRotation = Quaternion.Euler(HandyItemObject.HandRotation);
                SetLayerByParent(HandyItemObject.transform);
            }
            else
            {
                _bodyTypeTransforms.TryGetValue(HandyItemObject.PlayerHandyType, out var itemBodyTypeTransform);
                HandyItemObject.transform.SetParent(itemBodyTypeTransform);
                HandyItemObject.transform.localPosition = HandyItemObject.BodyOffset;
                HandyItemObject.transform.localRotation = Quaternion.Euler(HandyItemObject.BodyRotation);
                SetLayerByParent(HandyItemObject.transform);
            }

            return;
        }
    }
    [ClientRpc]
    private void RpcSetHandyObject(Item item)
    {
        SetHandyObject(item);
    }
    private void SetLayerByParent(Transform target)
    {
        var layer = target.parent.gameObject.layer;
        target.gameObject.layer = layer;
        foreach (Transform child in target)
        {
            child.gameObject.layer = layer;
        }
    }

    private HandyItemObject _currentHandyItemObject;
    public HandyItemObject CurrentHandyItemObject => _currentHandyItemObject;
    private void GetHandyTransformByHandyType()
    {
        foreach (var itemHandyTypeTransform in _itemHandyTypeTransforms)
        {
            _handyTypeTransforms.Add(itemHandyTypeTransform.PlayerHandyType, itemHandyTypeTransform.HandyTransform);
            _bodyTypeTransforms.Add(itemHandyTypeTransform.PlayerHandyType, itemHandyTypeTransform.BodyTransform);
        }
    }
}