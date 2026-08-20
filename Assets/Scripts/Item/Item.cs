using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[PrefabLabel(nameof(Item))]
[RequireComponent(typeof(NetworkIdentity))]
public class Item : InteractableObject, IPoolable
{
    private bool _isPickedUp = false;
    public bool IsPickedUp => _isPickedUp;

    public HandyItemObject HandyItemObjectPrefab;

    [SerializeField]
    private ExplosiveFactory.Item.Data.ItemData? _itemData;
    public ExplosiveFactory.Item.Data.ItemData? ItemData
    {
        get => _itemData;
        set => _itemData = value;
    }

    private ItemHolder? _itemHolder;
    [HideInInspector]
    public ItemHolder? ItemHolder
    {
        get => _itemHolder;
        set => _itemHolder = value;
    }

    [HideInInspector]
    public HandyItemObject HandyItemObject;

    private Rigidbody _rigidbody;
    private Collider[] _colliders = Array.Empty<Collider>();

    protected override void Awake()
    {
        base.Awake();
        _rigidbody = GetComponentInChildren<Rigidbody>();
        _colliders = GetComponentsInChildren<Collider>(true);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        ApplyPickupVisualAndPhysics(_isPickedUp);
    }

    private void ApplyPickupVisualAndPhysics(bool pickedUp)
    {
        if (RendererObject != null)
        {
            RendererObject.SetActive(!pickedUp);
        }

        if (_colliders != null)
        {
            foreach (var col in _colliders)
            {
                if (col != null)
                {
                    col.enabled = !pickedUp;
                }
            }
        }

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = pickedUp;
            if (pickedUp)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
        }
    }

    public void UseItem()
    {
        if (NetworkServer.active)
        {
            RpcUseItem();
        }
    }

    [ClientRpc]
    private void RpcUseItem()
    {
        OnItemUsedEvent?.Invoke(this);
    }

    [Server]
    public void DropItem(Vector3 pos, Quaternion rot, Vector3? initialVelocity = null)
    {
        _isPickedUp = false;
        ItemHolder = null;

        transform.position = pos;
        transform.rotation = rot;

        Vector3 vel = initialVelocity ?? Vector3.zero;
        RpcOnItemDropped(pos, rot, vel);
        OnItemDroppedEvent?.Invoke(this);
    }

    [ClientRpc]
    private void RpcOnItemDropped(Vector3 pos, Quaternion rot, Vector3 velocity)
    {
        _isPickedUp = false;
        ItemHolder = null;

        transform.position = pos;
        transform.rotation = rot;

        if (RendererObject != null)
        {
            RendererObject.transform.position = pos;
            RendererObject.transform.rotation = rot;
        }

        ApplyPickupVisualAndPhysics(false);

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = velocity;
        }

        OnItemDroppedEvent?.Invoke(this);
    }

    [Server]
    public void PickUpItem(ItemHolder? holder = null)
    {
        if (_isPickedUp) return;
        _isPickedUp = true;
        ItemHolder = holder;

        RpcOnItemPickedUp();
        OnItemPickedUpEvent?.Invoke(this);
    }

    [ClientRpc]
    private void RpcOnItemPickedUp()
    {
        _isPickedUp = true;
        ApplyPickupVisualAndPhysics(true);
        OnItemPickedUpEvent?.Invoke(this);
    }

    public void OnHandyItemObjectSpawned()
    {
        OnHandyItemObjectSpawnedEvent?.Invoke(this);
    }

    public void OnHandyItemObjectDespawned()
    {
        OnHandyItemObjectDespawnedEvent?.Invoke(this);
    }

    public void OnSpawned()
    {
        _isPickedUp = false;
        ApplyPickupVisualAndPhysics(false);
    }

    public void OnDespawned()
    {
    }

    public event Action<Item> OnItemDroppedEvent;
    public event Action<Item> OnItemPickedUpEvent;
    public event Action<Item> OnItemUsedEvent;
    public event Action<Item> OnHandyItemObjectSpawnedEvent;
    public event Action<Item> OnHandyItemObjectDespawnedEvent;
}
