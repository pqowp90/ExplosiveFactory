using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NomalItem : ItemEventBehaviour
{
    public override void OnUse(Item item)
    {
        base.OnUse(item);
        item.ItemHolder.Player.PlayerAnimation.UseItem();
    }
}
