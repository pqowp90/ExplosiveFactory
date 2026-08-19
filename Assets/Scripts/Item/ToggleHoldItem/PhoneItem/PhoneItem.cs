using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhoneItem : ItemEventBehaviour
{
    private HandyPhoneUI handyPhoneUI;
    protected bool isHolding = false;

    public override void OnPickup(Item item)
    {
        base.OnPickup(item);
    }

    public override void OnHandyObjectSpawn(Item item)
    {
        base.OnHandyObjectSpawn(item);
        isHolding = false;
        if (Player != null && Player.PlayerAnimation != null)
        {
            Player.PlayerAnimation.SetHoldableItem(true);
        }

        if (item.HandyItemObject != null)
        {
            handyPhoneUI = item.HandyItemObject.GetComponent<HandyPhoneUI>();
        }
    }

    public override void OnHandyObjectDespawn(Item item)
    {
        base.OnHandyObjectDespawn(item);
        if (Player != null && Player.PlayerAnimation != null && Player.PlayerAnimation.SwayNBobScript != null)
        {
            Player.PlayerAnimation.SwayNBobScript.FixSwayNBobbing(false);
        }
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.UnsetCursorFromSource(this);
        }
        if (Player != null && Player.PlayerAnimation != null)
        {
            Player.PlayerAnimation.SetHoldableItem(false);
        }

        handyPhoneUI = null;
    }

    public override void OnUse(Item item)
    {
        if (item.ItemHolder != null && !item.ItemHolder.isOwned)
        {
            return;
        }
        base.OnUse(item);

        if (handyPhoneUI == null)
        {
            if (item.HandyItemObject != null)
                handyPhoneUI = item.HandyItemObject.GetComponent<HandyPhoneUI>();
            else if (item.ItemHolder != null && item.ItemHolder.CurrentHandyItemObject != null)
                handyPhoneUI = item.ItemHolder.CurrentHandyItemObject.GetComponent<HandyPhoneUI>();
            else if (Player != null && Player.ItemHolder != null && Player.ItemHolder.CurrentHandyItemObject != null)
                handyPhoneUI = Player.ItemHolder.CurrentHandyItemObject.GetComponent<HandyPhoneUI>();
        }

        Debug.Log($"[PhoneItem] OnUse. isHolding={isHolding}, handyPhoneUI={handyPhoneUI != null}");

        if (!isHolding)
        {
            if (handyPhoneUI != null)
            {
                handyPhoneUI.OpenHomeUI();
            }
            isHolding = true;
            if (Player != null && Player.PlayerAnimation != null)
            {
                Player.PlayerAnimation.UseItem(0);
                if (Player.PlayerAnimation.SwayNBobScript != null)
                {
                    Player.PlayerAnimation.SwayNBobScript.FixSwayNBobbing(true);
                }
            }
            if (CursorManager.Instance != null)
            {
                CursorManager.Instance.SetCursor(CursorType.UI, this);
            }
        }
        else if (handyPhoneUI == null || !handyPhoneUI.CloseUI())
        {
            isHolding = false;
            if (Player != null && Player.PlayerAnimation != null)
            {
                Player.PlayerAnimation.UseItem(1);
                if (Player.PlayerAnimation.SwayNBobScript != null)
                {
                    Player.PlayerAnimation.SwayNBobScript.FixSwayNBobbing(false);
                }
            }
            if (CursorManager.Instance != null)
            {
                CursorManager.Instance.UnsetCursorFromSource(this);
            }
        }
    }
}
