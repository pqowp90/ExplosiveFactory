using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarketItemUI : MonoBehaviour, IPoolable
{
    private int itemID;
    private string itemName;
    public string ItemName => itemName;
    private int itemPrice;
    public int ItemPrice => itemPrice;
    private int itemCount;
    [SerializeField] private TMPro.TextMeshProUGUI itemNameText;
    [SerializeField] private TMPro.TextMeshProUGUI itemPriceText;
    [SerializeField] private TMPro.TextMeshProUGUI itemCountText;
    private const int MAX_ITEM_COUNT = 10;
    public void InitUI(int itemID, string itemName, int itemPrice)
    {
        this.itemID = itemID;
        this.itemName = itemName;
        this.itemPrice = itemPrice;
        itemNameText.text = itemName;
        itemPriceText.text = $"${itemPrice}";
        itemCountText.text = itemCount.ToString();
    }
    public void AddItemToCart()
    {
        if (itemCount >= MAX_ITEM_COUNT) return;
        itemCount++;
        ApplyItemCart();
    }
    public void SubtractItemFromCart()
    {
        if (itemCount > 0)
        {
            itemCount--;
            ApplyItemCart();
        }
    }
    public void ClearItemFromCart()
    {
        itemCount = 0;
        ApplyItemCart();
    }
    private void ApplyItemCart()
    {
        itemCountText.text = itemCount.ToString();
        OnChangeItemCountEvent?.Invoke(this, itemCount);
    }

    public void OnSpawned()
    {
    }

    public void OnDespawned()
    {
        itemID = 0;
        itemName = "";
        itemPrice = 0;
        itemCount = 0;
    }
    public event Action<MarketItemUI, int> OnChangeItemCountEvent;
}
