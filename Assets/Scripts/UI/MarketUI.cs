using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using TMPro;
using UnityEngine;

public class MarketUI : MonoBehaviour, IPoolable
{
    private static readonly int CartHash = Animator.StringToHash("Cart");
    private static readonly int DecreaseHash = Animator.StringToHash("DecreaseMoney");
    [SerializeField] private GameObject _marketItemPrefab;
    private List<MarketItemUI> _marketItems = new List<MarketItemUI>();
    private class CartItem
    {
        public MarketItemUI marketItemUI;
        public int count;
    }
    private List<CartItem> _cartItems = new List<CartItem>();
    [SerializeField] private Animator _marketAnimator;
    [SerializeField] private Animator _decreaseAnimator;
    [SerializeField] private TextMeshProUGUI _totalPriceText;
    [SerializeField] private TextMeshProUGUI _totalItemsText;
    [SerializeField] private TextMeshProUGUI _playerCurMoneyText;
    [SerializeField] private TextMeshProUGUI _decreaseText;
    private int _totalPrice;
    private int _playerCurMoney;
    private bool IsBuyingItem => _cartItems != null && _cartItems.Count > 0;

    public void OnDespawned()
    {
        foreach (var child in _marketItems)
        {
            PoolManager.Release(child.transform.gameObject);
        }
        _marketItems.Clear();
        _cartItems.Clear();
        _totalPrice = 0;
        _totalPriceText.text = string.Format("${0:n0}", _totalPrice);
        _totalItemsText.text = "";
    }
    public void OnSpawned()
    {
        _marketAnimator.SetBool(CartHash, false);
        foreach (var item in ItemSOManager.Instance.ItemSoList)
        {
            _marketItems.Add(CreateMarketItem(item.itemID, item.itemName, item.itemPrice));
        }
    }

    private void Awake()
    {
        _marketAnimator.writeDefaultValuesOnDisable = true;
        _marketAnimator.keepAnimatorStateOnDisable = true;
        _marketItemPrefab.SetActive(false);

        _playerCurMoney = 1000000;
        _playerCurMoneyText.text = string.Format("${0:n0}", _playerCurMoney);
    }
    private MarketItemUI CreateMarketItem(int itemID, string itemName, int itemPrice)
    {
        GameObject marketItem = PoolManager.Get(_marketItemPrefab, _marketItemPrefab.transform.parent);
        marketItem.transform.SetAsLastSibling();
        MarketItemUI marketItemUI = marketItem.GetComponent<MarketItemUI>();
        marketItemUI.InitUI(itemID, itemName, itemPrice);
        marketItemUI.OnChangeItemCountEvent += ApplyTotalPrice;
        marketItemUI.gameObject.SetActive(true);
        return marketItemUI;
    }
    private void ApplyTotalPrice(MarketItemUI marketItemUI, int count)
    {
        var cartItem = _cartItems.Find(x => x.marketItemUI == marketItemUI);
        if (cartItem == null)
        {
            _cartItems.Add(new CartItem { marketItemUI = marketItemUI, count = count });
        }
        else
        {
            if (count == 0)
            {
                _cartItems.Remove(cartItem);
            }
            else
                cartItem.count = count;
        }
        ApplyUI();
    }

    private void ApplyUI()
    {
        _totalPrice = 0;
        int totalItemCount = 0;
        string items = "";
        foreach (var item in _cartItems)
        {
            _totalPrice += item.marketItemUI.ItemPrice * item.count;
            totalItemCount += item.count;
            items += $"{item.marketItemUI.ItemName} x {item.count}\n";
        }
        _totalPriceText.text = string.Format("${0:n0}", _totalPrice);
        _totalItemsText.text = items;
        _marketAnimator.SetBool(CartHash, totalItemCount > 0);
    }

    public void BuyItem()
    {
        if (!IsBuyingItem) return;
        if (_totalPrice > _playerCurMoney) return;
        _playerCurMoney -= _totalPrice;
        _playerCurMoneyText.text = string.Format("${0:n0}", _playerCurMoney);

        SpendMoney(_totalPrice);
        foreach (var item in _marketItems)
        {
            item.ClearItemFromCart();
        }

        //ApplyUI();
    }
    public void SpendMoney(int _decreaseMoney)
    {
        string displayText = string.Format("-${0:n0}", _decreaseMoney);
        _decreaseText.text = displayText;
        _decreaseAnimator.SetTrigger(DecreaseHash);
    }
}
