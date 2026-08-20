using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleHoldItem : ItemEventBehaviour
{
    protected bool isHolding = false;

    public override void OnHandyObjectSpawn(Item item)
    {
        base.OnHandyObjectSpawn(item);
        isHolding = false;
        if (item != null && item.ItemHolder != null && item.ItemHolder.Player != null && item.ItemHolder.Player.PlayerAnimation != null)
        {
            item.ItemHolder.Player.PlayerAnimation.SetHoldableItem(true);
        }
    }

    public override void OnHandyObjectDespawn(Item item)
    {
        if (item != null && item.ItemHolder != null && item.ItemHolder.Player != null && item.ItemHolder.Player.PlayerAnimation != null)
        {
            item.ItemHolder.Player.PlayerAnimation.SetHoldableItem(false);
            if (item.ItemHolder.Player.PlayerAnimation.SwayNBobScript != null)
            {
                item.ItemHolder.Player.PlayerAnimation.SwayNBobScript.FixSwayNBobbing(false);
            }
        }
        base.OnHandyObjectDespawn(item);
    }

    public override void OnUse(Item item)
    {
        base.OnUse(item);
        if (item == null || item.ItemHolder == null || item.ItemHolder.Player == null || item.ItemHolder.Player.PlayerAnimation == null) return;

        if (!isHolding)
        {
            item.ItemHolder.Player.PlayerAnimation.UseItem(0);
            if (item.ItemHolder.Player.PlayerAnimation.SwayNBobScript != null)
                item.ItemHolder.Player.PlayerAnimation.SwayNBobScript.FixSwayNBobbing(true);
        }
        else
        {
            item.ItemHolder.Player.PlayerAnimation.UseItem(1);
            if (item.ItemHolder.Player.PlayerAnimation.SwayNBobScript != null)
                item.ItemHolder.Player.PlayerAnimation.SwayNBobScript.FixSwayNBobbing(false);
        }
        isHolding = !isHolding;
    }
}
