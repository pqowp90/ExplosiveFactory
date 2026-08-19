using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[SingletonLifeTime(LifeTime.Application)]
public class ItemSOManager : MonoSingleton<ItemSOManager>
{
    private static readonly string ItemSoPath = "SO/ItemSo";
    private List<ItemSo> _itemSoList = new List<ItemSo>();
    public List<ItemSo> ItemSoList => _itemSoList;
    protected override void Awake()
    {
        base.Awake();
        LoadItemSoList();
    }
    private void LoadItemSoList()
    {
        _itemSoList.Clear();
        var itemSoArray = Resources.LoadAll<ItemSo>(ItemSoPath);
        foreach (var itemSo in itemSoArray)
        {
            _itemSoList.Add(itemSo);
        }

        if (_itemSoList.Count == 0)
        {
            // SO가 없을 경우 기본 아이템 생성
            var flashlight = ScriptableObject.CreateInstance<ItemSo>();
            flashlight.itemID = 0;
            flashlight.itemName = "Flashlight";
            flashlight.itemPrice = 100;
            _itemSoList.Add(flashlight);

            var phone = ScriptableObject.CreateInstance<ItemSo>();
            phone.itemID = 1;
            phone.itemName = "Phone";
            phone.itemPrice = 250;
            _itemSoList.Add(phone);
        }

        _itemSoList.Sort((x, y) => x.itemID.CompareTo(y.itemID));
    }
}