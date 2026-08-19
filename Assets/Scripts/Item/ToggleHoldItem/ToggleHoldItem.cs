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
        item.ItemHolder.Player.PlayerAnimation.SetHoldableItem(true);
    }
    public override void OnHandyObjectDespawn(Item item)
    {
        item.ItemHolder.Player.PlayerAnimation.SetHoldableItem(false);
        base.OnHandyObjectDespawn(item);
        item.ItemHolder.Player.PlayerAnimation.SwayNBobScript.FixSwayNBobbing(false);
    }

    public override void OnUse(Item item)
    {
        base.OnUse(item);
        if (!isHolding)
        {
            item.ItemHolder.Player.PlayerAnimation.UseItem(0);
            item.ItemHolder.Player.PlayerAnimation.SwayNBobScript.FixSwayNBobbing(true);
        }
        else
        {
            item.ItemHolder.Player.PlayerAnimation.UseItem(1);
            item.ItemHolder.Player.PlayerAnimation.SwayNBobScript.FixSwayNBobbing(false);
        }
        isHolding = !isHolding;
    }
}
