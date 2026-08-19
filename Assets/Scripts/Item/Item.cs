using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Mirror;
using UnityEngine;
[PrefabLabel(nameof(Item))]
public class Item : InteractableObject, IPoolable
{
    [SyncVar]
    private bool _isPickedUp = false;
    public bool IsPickedUp => _isPickedUp;

    public HandyItemObject HandyItemObjectPrefab;
    [SyncVar]
    private ItemHolder _syncedItemHolder;
    private ItemHolder _itemHolder;
    [HideInInspector]
    public ItemHolder ItemHolder
    {
        get
        {
            return _itemHolder ??= _syncedItemHolder;
        }
        set
        {
            if (NetworkServer.active)
            {
                _syncedItemHolder = value;
                RpcSyncItemHolder(value);
            }
        }
    }
    [ClientRpc]
    private void RpcSyncItemHolder(ItemHolder itemHolder)
    {
        _itemHolder = itemHolder;
    }
    [HideInInspector]
    public HandyItemObject HandyItemObject;

    private void Awake()
    {
        _rigidbody = GetComponentInChildren<Rigidbody>();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        _itemHolder = _syncedItemHolder;
        RendererObject.SetActive(!_isPickedUp);
    }


    public void UseItem()
    {
        RpcUseItem();
    }
    [ClientRpc]
    private void RpcUseItem()
    {
        OnItemUsedEvent?.Invoke(this);
    }
    public void DropItem(Vector3 pos, Quaternion rot)
    {
        _isPickedUp = false;
        SpawnItemObject(pos, rot);

        OnItemDroppedEvent?.Invoke(this);
    }
    public void PickUpItem()
    {
        if (_isPickedUp) return;
        _isPickedUp = true;
        DespawnItemObject();

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
    public override void OnInteract()
    {
        base.OnInteract();
    }
    private Rigidbody _rigidbody;

    [ClientRpc]
    private void SpawnItemObject(Vector3 pos, Quaternion rot)
    {
        transform.position = pos;
        transform.rotation = rot;
        RendererObject.transform.position = pos;
        RendererObject.transform.rotation = rot;
        _rigidbody.linearVelocity = Vector3.zero;

        RendererObject.SetActive(true);
    }

    [ClientRpc]
    private void DespawnItemObject()
    {
        RendererObject.SetActive(false);
    }
    public void OnSpawned()
    {
        _isPickedUp = false;
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
