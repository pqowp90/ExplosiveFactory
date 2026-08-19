using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemEventBehaviour : MonoBehaviour
{
    protected Item Item;
    protected Player Player;
    private void Awake()
    {
        Item = GetComponent<Item>();
        Item.OnItemUsedEvent += OnUse;
        Item.OnItemPickedUpEvent += OnPickup;
        Item.OnItemDroppedEvent += OnDrop;
        Item.OnHandyItemObjectDespawnedEvent += OnHandyObjectDespawn;
        Item.OnHandyItemObjectSpawnedEvent += OnHandyObjectSpawn;
    }

    public virtual void OnHandyObjectSpawn(Item item)
    {
        Player = item.ItemHolder.Player;
    }
    public virtual void OnHandyObjectDespawn(Item item)
    {
        if (Player == null)
            Player = item.ItemHolder.Player;
    }
    public virtual void OnPickup(Item item)
    {
    }
    public virtual void OnDrop(Item item)
    {
    }
    public virtual void OnUse(Item item)
    {
    }
}
