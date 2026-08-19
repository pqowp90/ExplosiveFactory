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
        _itemSoList.Sort((x, y) => x.itemID.CompareTo(y.itemID));
    }
}